/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using GhJSON.Core.PatchModels;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Applies a <see cref="GhPatchDocument"/> to a base <see cref="GhJsonDocument"/>.
    /// </summary>
    /// <remarks>
    /// Apply order is:
    /// <list type="number">
    ///   <item>Base checksum verification (if enabled and present).</item>
    ///   <item>Metadata operations.</item>
    ///   <item>Components.modify.</item>
    ///   <item>Components.remove.</item>
    ///   <item>Components.add (with ID-collision renumbering if enabled).</item>
    ///   <item>Groups.modify, remove, add.</item>
    ///   <item>Connections.remove, then connections.add (so a connection can target a newly added component).</item>
    /// </list>
    /// </remarks>
    internal static class PatchApplier
    {
        private static readonly JsonSerializerSettings BaseSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            Converters = { new PivotConverter() },
        };

        public static ApplyPatchResult Apply(GhJsonDocument baseDoc, GhPatchDocument patch, ApplyPatchOptions? options = null)
        {
            options ??= ApplyPatchOptions.Default;

            var result = new ApplyPatchResult
            {
                Document = baseDoc,
                Success = true,
            };

            // Schema check
            if (!string.IsNullOrEmpty(patch.Patch.Base?.Schema)
                && !string.IsNullOrEmpty(baseDoc.Schema)
                && patch.Patch.Base!.Schema != baseDoc.Schema)
            {
                result.Conflicts.Add(new PatchConflict(
                    PatchConflictKind.SchemaVersionMismatch,
                    $"Patch base schema '{patch.Patch.Base.Schema}' does not match document schema '{baseDoc.Schema}'."));

                if (!options.ContinueOnConflict)
                {
                    result.Success = false;
                    return result;
                }
            }

            // Checksum check
            if (options.VerifyBase && !string.IsNullOrEmpty(patch.Patch.Base?.Checksum))
            {
                var expected = patch.Patch.Base!.Checksum!;
                var actual = DocumentNormalizer.ComputeChecksum(baseDoc, DiffOptions.Default);
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    result.Conflicts.Add(new PatchConflict(
                        PatchConflictKind.BaseChecksumMismatch,
                        $"Base checksum mismatch. Expected '{expected}', actual '{actual}'."));

                    result.Success = false;
                    return result;
                }
            }

            // Build a working mutable document model
            var workingDoc = CloneViaJson(baseDoc);
            var componentsList = workingDoc.Components.ToList();
            var connectionsList = (workingDoc.Connections ?? new List<GhJsonConnection>()).ToList();
            var groupsList = (workingDoc.Groups ?? new List<GhJsonGroup>()).ToList();

            // ---- Metadata ----
            var newMetadata = ApplyMetadata(workingDoc.Metadata, patch.Patch.Metadata);

            // ---- Components.modify ----
            if (patch.Patch.Components?.Modify != null)
            {
                // Modify operations do not change the component list membership, so the
                // identity index can be built once and reused across all modifies.
                var modifyIndex = ComponentIdentityIndex.Build(componentsList);
                foreach (var modify in patch.Patch.Components.Modify)
                {
                    if (!modifyIndex.TryMatch(modify.Match, out var target, out var count))
                    {
                        var kind = count > 1 ? PatchConflictKind.MatchAmbiguous : PatchConflictKind.MatchNotFound;
                        result.Conflicts.Add(new PatchConflict(
                            kind,
                            count > 1
                                ? $"Multiple components match the descriptor ({count} matches)."
                                : "No component matches the descriptor.",
                            "components.modify"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return result;
                        }

                        continue;
                    }

                    ApplyComponentModify(target!, modify);
                    result.ComponentsModified++;
                }
            }

            // ---- Components.remove ----
            if (patch.Patch.Components?.Remove != null)
            {
                foreach (var removeMatch in patch.Patch.Components.Remove)
                {
                    var index = ComponentIdentityIndex.Build(componentsList);
                    if (!index.TryMatch(removeMatch, out var target, out var count))
                    {
                        var kind = count > 1 ? PatchConflictKind.MatchAmbiguous : PatchConflictKind.MatchNotFound;
                        result.Conflicts.Add(new PatchConflict(kind, "No component matches the remove descriptor.", "components.remove"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return result;
                        }

                        continue;
                    }

                    componentsList.Remove(target!);
                    if (target!.Id.HasValue)
                    {
                        RemoveConnectionsReferencing(connectionsList, target.Id.Value);
                        RemoveGroupMembersReferencing(groupsList, target.Id.Value);
                    }

                    result.ComponentsRemoved++;
                }
            }

            // ---- Components.add ----
            if (patch.Patch.Components?.Add != null)
            {
                var existingIds = new HashSet<int>(componentsList.Where(c => c.Id.HasValue).Select(c => c.Id!.Value));
                var existingGuids = new HashSet<Guid>(componentsList.Where(c => c.InstanceGuid.HasValue).Select(c => c.InstanceGuid!.Value));
                var nextId = (existingIds.Count == 0 ? 0 : existingIds.Max()) + 1;

                foreach (var component in patch.Patch.Components.Add)
                {
                    if (component.InstanceGuid.HasValue
                        && component.InstanceGuid != Guid.Empty
                        && existingGuids.Contains(component.InstanceGuid.Value))
                    {
                        result.Conflicts.Add(new PatchConflict(
                            PatchConflictKind.InstanceGuidCollision,
                            $"Component instanceGuid '{component.InstanceGuid}' already exists.",
                            "components.add"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return result;
                        }

                        continue;
                    }

                    if (component.Id.HasValue && existingIds.Contains(component.Id.Value))
                    {
                        if (options.RenumberCollidingAddedIds)
                        {
                            while (existingIds.Contains(nextId))
                            {
                                nextId++;
                            }

                            component.Id = nextId;
                            nextId++;
                        }
                        else
                        {
                            result.Conflicts.Add(new PatchConflict(
                                PatchConflictKind.InstanceGuidCollision,
                                $"Component id '{component.Id}' already exists.",
                                "components.add"));
                            if (!options.ContinueOnConflict)
                            {
                                result.Success = false;
                                return result;
                            }

                            continue;
                        }
                    }

                    componentsList.Add(component);
                    if (component.Id.HasValue)
                    {
                        existingIds.Add(component.Id.Value);
                    }

                    if (component.InstanceGuid.HasValue)
                    {
                        existingGuids.Add(component.InstanceGuid.Value);
                    }

                    result.ComponentsAdded++;
                }
            }

            // ---- Groups (modify, remove, add) ----
            ApplyGroups(patch.Patch.Groups, groupsList, componentsList, options, result);

            // ---- Connections (remove, add) ----
            ApplyConnections(patch.Patch.Connections, connectionsList, options, result);

            result.Document = new GhJsonDocument(
                workingDoc.Schema,
                newMetadata,
                componentsList,
                connectionsList.Count == 0 ? null : connectionsList,
                groupsList.Count == 0 ? null : groupsList);

            return result;
        }

        // ---------------- Metadata ----------------

        private static GhJsonMetadata? ApplyMetadata(GhJsonMetadata? metadata, GhPatchMetadataOp? op)
        {
            if (op == null)
            {
                return metadata;
            }

            var metadataObj = metadata == null
                ? new JObject()
                : JObject.FromObject(metadata, JsonSerializer.Create(BaseSettings));

            if (op.Set != null)
            {
                foreach (var prop in op.Set.Properties())
                {
                    metadataObj[prop.Name] = prop.Value.DeepClone();
                }
            }

            if (op.Remove != null)
            {
                foreach (var key in op.Remove)
                {
                    metadataObj.Remove(key);
                }
            }

            if (metadataObj.Count == 0)
            {
                return null;
            }

            return metadataObj.ToObject<GhJsonMetadata>(JsonSerializer.Create(BaseSettings));
        }

        // ---------------- Component modify ----------------

        private static void ApplyComponentModify(GhJsonComponent target, GhPatchComponentModify modify)
        {
            var obj = JObject.FromObject(target, JsonSerializer.Create(BaseSettings));

            if (modify.Set != null)
            {
                foreach (var prop in modify.Set.Properties())
                {
                    obj[prop.Name] = prop.Value.DeepClone();
                }
            }

            if (modify.Remove != null)
            {
                foreach (var key in modify.Remove)
                {
                    obj.Remove(key);
                }
            }

            if (modify.ComponentState != null)
            {
                var state = obj["componentState"] as JObject ?? new JObject();
                ApplyComponentStateOp(state, modify.ComponentState);
                if (state.Count == 0)
                {
                    obj.Remove("componentState");
                }
                else
                {
                    obj["componentState"] = state;
                }
            }

            if (modify.InputSettings != null)
            {
                var arr = obj["inputSettings"] as JArray ?? new JArray();
                ApplyParameterSettingsOp(arr, modify.InputSettings);
                obj["inputSettings"] = arr;
            }

            if (modify.OutputSettings != null)
            {
                var arr = obj["outputSettings"] as JArray ?? new JArray();
                ApplyParameterSettingsOp(arr, modify.OutputSettings);
                obj["outputSettings"] = arr;
            }

            var rebuilt = obj.ToObject<GhJsonComponent>(JsonSerializer.Create(BaseSettings));
            if (rebuilt == null)
            {
                throw new InvalidOperationException(
                    "Failed to rebuild component after applying modify operation; the resulting JSON was not a valid component.");
            }

            CopyComponentInto(rebuilt, target);
        }

        private static void ApplyComponentStateOp(JObject state, GhPatchComponentStateOp op)
        {
            if (op.Set != null)
            {
                foreach (var prop in op.Set.Properties())
                {
                    state[prop.Name] = prop.Value.DeepClone();
                }
            }

            if (op.Remove != null)
            {
                foreach (var key in op.Remove)
                {
                    state.Remove(key);
                }
            }

            if (op.Extensions != null)
            {
                var extensions = state["extensions"] as JObject ?? new JObject();
                if (op.Extensions.Set != null)
                {
                    foreach (var kvp in op.Extensions.Set)
                    {
                        extensions[kvp.Key] = (JObject)kvp.Value.DeepClone();
                    }
                }

                if (op.Extensions.Remove != null)
                {
                    foreach (var key in op.Extensions.Remove)
                    {
                        extensions.Remove(key);
                    }
                }

                if (extensions.Count == 0)
                {
                    state.Remove("extensions");
                }
                else
                {
                    state["extensions"] = extensions;
                }
            }
        }

        private static void ApplyParameterSettingsOp(JArray settings, GhPatchParameterSettingsOp op)
        {
            if (op.ByParameterName == null)
            {
                return;
            }

            foreach (var kvp in op.ByParameterName)
            {
                var existing = settings.OfType<JObject>().FirstOrDefault(p =>
                    string.Equals((string?)p["parameterName"], kvp.Key, StringComparison.Ordinal));

                if (existing == null)
                {
                    existing = new JObject { ["parameterName"] = kvp.Key };
                    settings.Add(existing);
                }

                if (kvp.Value.Set != null)
                {
                    foreach (var prop in kvp.Value.Set.Properties())
                    {
                        existing[prop.Name] = prop.Value.DeepClone();
                    }
                }

                if (kvp.Value.Remove != null)
                {
                    foreach (var key in kvp.Value.Remove)
                    {
                        existing.Remove(key);
                    }
                }
            }
        }

        // ---------------- Groups ----------------

        private static void ApplyGroups(
            GhPatchGroupsOp? op,
            List<GhJsonGroup> groupsList,
            List<GhJsonComponent> componentsList,
            ApplyPatchOptions options,
            ApplyPatchResult result)
        {
            if (op == null)
            {
                return;
            }

            // Modify
            if (op.Modify != null)
            {
                foreach (var modify in op.Modify)
                {
                    var target = FindGroup(groupsList, modify.Match);
                    if (target == null)
                    {
                        result.Conflicts.Add(new PatchConflict(PatchConflictKind.MatchNotFound, "No group matches the descriptor.", "groups.modify"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return;
                        }

                        continue;
                    }

                    ApplyGroupModify(target, modify, componentsList, result);
                    result.GroupsModified++;
                }
            }

            // Remove
            if (op.Remove != null)
            {
                foreach (var removeMatch in op.Remove)
                {
                    var target = FindGroup(groupsList, removeMatch);
                    if (target == null)
                    {
                        result.Conflicts.Add(new PatchConflict(PatchConflictKind.MatchNotFound, "No group matches the remove descriptor.", "groups.remove"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return;
                        }

                        continue;
                    }

                    groupsList.Remove(target);
                    result.GroupsRemoved++;
                }
            }

            // Add
            if (op.Add != null)
            {
                foreach (var group in op.Add)
                {
                    ValidateGroupMembers(group, componentsList, result, "groups.add");
                    groupsList.Add(group);
                    result.GroupsAdded++;
                }
            }
        }

        private static GhJsonGroup? FindGroup(List<GhJsonGroup> groupsList, GhPatchGroupMatch match)
        {
            if (match.InstanceGuid.HasValue && match.InstanceGuid != Guid.Empty)
            {
                return groupsList.FirstOrDefault(g => g.InstanceGuid == match.InstanceGuid);
            }

            if (match.Id.HasValue)
            {
                return groupsList.FirstOrDefault(g => g.Id == match.Id);
            }

            return null;
        }

        private static void ApplyGroupModify(
            GhJsonGroup target,
            GhPatchGroupModify modify,
            List<GhJsonComponent> componentsList,
            ApplyPatchResult result)
        {
            var obj = JObject.FromObject(target, JsonSerializer.Create(BaseSettings));

            if (modify.Set != null)
            {
                foreach (var prop in modify.Set.Properties())
                {
                    if (prop.Name == "members")
                    {
                        continue;
                    }

                    obj[prop.Name] = prop.Value.DeepClone();
                }
            }

            if (modify.Remove != null)
            {
                foreach (var key in modify.Remove)
                {
                    if (key == "members")
                    {
                        continue;
                    }

                    obj.Remove(key);
                }
            }

            var rebuilt = obj.ToObject<GhJsonGroup>(JsonSerializer.Create(BaseSettings));
            if (rebuilt == null)
            {
                throw new InvalidOperationException(
                    "Failed to rebuild group after applying modify operation; the resulting JSON was not a valid group.");
            }

            target.Color = rebuilt.Color;
            target.Name = rebuilt.Name;
            target.Id = rebuilt.Id;
            target.InstanceGuid = rebuilt.InstanceGuid;

            if (modify.Members != null)
            {
                if (modify.Members.Add != null)
                {
                    foreach (var memberId in modify.Members.Add)
                    {
                        if (!target.Members.Contains(memberId))
                        {
                            target.Members.Add(memberId);
                        }
                    }
                }

                if (modify.Members.Remove != null)
                {
                    foreach (var memberId in modify.Members.Remove)
                    {
                        target.Members.Remove(memberId);
                    }
                }
            }

            ValidateGroupMembers(target, componentsList, result, "groups.modify");
        }

        private static void ValidateGroupMembers(
            GhJsonGroup group,
            List<GhJsonComponent> componentsList,
            ApplyPatchResult result,
            string path)
        {
            var ids = new HashSet<int>(componentsList.Where(c => c.Id.HasValue).Select(c => c.Id!.Value));
            foreach (var member in group.Members)
            {
                if (!ids.Contains(member))
                {
                    result.Conflicts.Add(new PatchConflict(
                        PatchConflictKind.DanglingMember,
                        $"Group member id '{member}' does not reference any component.",
                        path));
                }
            }
        }

        // ---------------- Connections ----------------

        private static void ApplyConnections(
            GhPatchConnectionsOp? op,
            List<GhJsonConnection> connectionsList,
            ApplyPatchOptions options,
            ApplyPatchResult result)
        {
            if (op == null)
            {
                return;
            }

            // Remove first so add can recreate equivalent ones (no-op replace).
            if (op.Remove != null)
            {
                foreach (var connection in op.Remove)
                {
                    var key = ConnectionKey.From(connection);
                    var existing = connectionsList.FirstOrDefault(c => ConnectionKey.From(c).Equals(key));
                    if (existing == null)
                    {
                        result.Conflicts.Add(new PatchConflict(
                            PatchConflictKind.ConnectionNotFound,
                            $"Connection from {connection.From.Id}/{connection.From.ParamName} to {connection.To.Id}/{connection.To.ParamName} not found.",
                            "connections.remove"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return;
                        }

                        continue;
                    }

                    connectionsList.Remove(existing);
                    result.ConnectionsRemoved++;
                }
            }

            if (op.Add != null)
            {
                var existingKeys = new HashSet<ConnectionKey>(connectionsList.Select(ConnectionKey.From));
                foreach (var connection in op.Add)
                {
                    var key = ConnectionKey.From(connection);
                    if (existingKeys.Contains(key))
                    {
                        result.Conflicts.Add(new PatchConflict(
                            PatchConflictKind.ConnectionAlreadyPresent,
                            $"Connection from {connection.From.Id}/{connection.From.ParamName} to {connection.To.Id}/{connection.To.ParamName} already exists.",
                            "connections.add"));
                        if (!options.ContinueOnConflict)
                        {
                            result.Success = false;
                            return;
                        }

                        continue;
                    }

                    connectionsList.Add(connection);
                    existingKeys.Add(key);
                    result.ConnectionsAdded++;
                }
            }
        }

        // ---------------- Helpers ----------------

        private static void RemoveConnectionsReferencing(List<GhJsonConnection> connections, int id)
        {
            for (var i = connections.Count - 1; i >= 0; i--)
            {
                var conn = connections[i];
                if (conn.From.Id == id || conn.To.Id == id)
                {
                    connections.RemoveAt(i);
                }
            }
        }

        private static void RemoveGroupMembersReferencing(List<GhJsonGroup> groups, int id)
        {
            foreach (var group in groups)
            {
                group.Members.RemoveAll(m => m == id);
            }
        }

        private static GhJsonDocument CloneViaJson(GhJsonDocument doc)
        {
            var json = JsonConvert.SerializeObject(doc, BaseSettings);
            return JsonConvert.DeserializeObject<GhJsonDocument>(json, BaseSettings) ?? new GhJsonDocument();
        }

        private static void CopyComponentInto(GhJsonComponent source, GhJsonComponent target)
        {
            target.Name = source.Name;
            target.Library = source.Library;
            target.NickName = source.NickName;
            target.ComponentGuid = source.ComponentGuid;
            target.InstanceGuid = source.InstanceGuid;
            target.Id = source.Id;
            target.Pivot = source.Pivot;
            target.InputSettings = source.InputSettings;
            target.OutputSettings = source.OutputSettings;
            target.ComponentState = source.ComponentState;
            target.Errors = source.Errors;
            target.Warnings = source.Warnings;
            target.Remarks = source.Remarks;
        }
    }
}
