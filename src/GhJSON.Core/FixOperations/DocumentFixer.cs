/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.FixOperations
{
    /// <summary>
    /// Fixes GhJSON documents by applying various repair operations.
    /// </summary>
    internal static class DocumentFixer
    {
        /// <summary>
        /// Fixes a GhJSON document using the specified options.
        /// </summary>
        /// <param name="document">The document to fix.</param>
        /// <param name="options">The fix options.</param>
        /// <returns>The fix result.</returns>
        public static FixResult Fix(GhJsonDocument document, FixOptions? options = null)
        {
            options ??= FixOptions.Default;
            var result = new FixResult { Document = document };

            if (options.ReassignIds)
            {
                ReassignIds(document, result);
            }
            else if (options.AssignMissingIds)
            {
                AssignMissingIds(document, result);
            }

            if (options.RegenerateInstanceGuids)
            {
                RegenerateInstanceGuids(document, result);
            }
            else if (options.GenerateMissingInstanceGuids)
            {
                GenerateMissingInstanceGuids(document, result);
            }

            if (options.RemoveInvalidConnections)
            {
                RemoveInvalidConnections(document, result);
            }

            if (options.RemoveInvalidGroupMembers)
            {
                RemoveInvalidGroupMembers(document, result);
            }

            if (options.FixMetadata)
            {
                FixMetadata(document, result);
            }

            return result;
        }

        /// <summary>
        /// Assigns missing IDs to components that don't have them.
        /// </summary>
        public static FixResult AssignMissingIds(GhJsonDocument document)
        {
            var result = new FixResult { Document = document };
            AssignMissingIds(document, result);
            return result;
        }

        /// <summary>
        /// Reassigns all component IDs sequentially starting from 1.
        /// </summary>
        public static FixResult ReassignIds(GhJsonDocument document)
        {
            var result = new FixResult { Document = document };
            ReassignIds(document, result);
            return result;
        }

        /// <summary>
        /// Generates missing instance GUIDs for components.
        /// </summary>
        public static FixResult GenerateMissingInstanceGuids(GhJsonDocument document)
        {
            var result = new FixResult { Document = document };
            GenerateMissingInstanceGuids(document, result);
            return result;
        }

        /// <summary>
        /// Regenerates all instance GUIDs.
        /// </summary>
        public static FixResult RegenerateInstanceGuids(GhJsonDocument document)
        {
            var result = new FixResult { Document = document };
            RegenerateInstanceGuids(document, result);
            return result;
        }

        /// <summary>
        /// Fixes document metadata (counts, timestamps).
        /// </summary>
        public static FixResult FixMetadata(GhJsonDocument document)
        {
            var result = new FixResult { Document = document };
            FixMetadata(document, result);
            return result;
        }

        private static void AssignMissingIds(GhJsonDocument document, FixResult result)
        {
            var existingIds = new HashSet<int>(
                document.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value));

            var nextId = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

            foreach (var component in document.Components.Where(c => !c.Id.HasValue))
            {
                component.Id = nextId++;
                result.AppliedActions.Add($"Assigned ID {component.Id} to component '{component.Name}'");
                result.WasModified = true;
            }
        }

        private static void ReassignIds(GhJsonDocument document, FixResult result)
        {
            var oldToNewMapping = new Dictionary<int, int>();
            var newId = 1;

            foreach (var component in document.Components)
            {
                if (component.Id.HasValue)
                {
                    oldToNewMapping[component.Id.Value] = newId;
                }

                component.Id = newId++;
                result.WasModified = true;
            }

            // Update connection references
            if (document.Connections != null)
            {
                foreach (var connection in document.Connections)
                {
                    if (oldToNewMapping.TryGetValue(connection.From.Id, out var newFromId))
                    {
                        connection.From.Id = newFromId;
                    }

                    if (oldToNewMapping.TryGetValue(connection.To.Id, out var newToId))
                    {
                        connection.To.Id = newToId;
                    }
                }
            }

            // Update group member references
            if (document.Groups != null)
            {
                foreach (var group in document.Groups)
                {
                    for (var i = 0; i < group.Members.Count; i++)
                    {
                        if (oldToNewMapping.TryGetValue(group.Members[i], out var newMemberId))
                        {
                            group.Members[i] = newMemberId;
                        }
                    }
                }
            }

            result.AppliedActions.Add($"Reassigned IDs for {document.Components.Count} components");
        }

        private static void GenerateMissingInstanceGuids(GhJsonDocument document, FixResult result)
        {
            foreach (var component in document.Components.Where(c => !c.InstanceGuid.HasValue))
            {
                component.InstanceGuid = Guid.NewGuid();
                result.AppliedActions.Add($"Generated instance GUID for component '{component.Name}'");
                result.WasModified = true;
            }
        }

        private static void RegenerateInstanceGuids(GhJsonDocument document, FixResult result)
        {
            foreach (var component in document.Components)
            {
                component.InstanceGuid = Guid.NewGuid();
                result.WasModified = true;
            }

            result.AppliedActions.Add($"Regenerated instance GUIDs for {document.Components.Count} components");
        }

        private static void RemoveInvalidConnections(GhJsonDocument document, FixResult result)
        {
            if (document.Connections == null || document.Connections.Count == 0)
            {
                return;
            }

            var validIds = new HashSet<int>(
                document.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value));

            var invalidConnections = document.Connections
                .Where(c => !validIds.Contains(c.From.Id) || !validIds.Contains(c.To.Id))
                .ToList();

            foreach (var conn in invalidConnections)
            {
                document.Connections.Remove(conn);
                result.AppliedActions.Add($"Removed invalid connection from {conn.From.Id} to {conn.To.Id}");
                result.WasModified = true;
            }
        }

        private static void RemoveInvalidGroupMembers(GhJsonDocument document, FixResult result)
        {
            if (document.Groups == null || document.Groups.Count == 0)
            {
                return;
            }

            var validIds = new HashSet<int>(
                document.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value));

            foreach (var group in document.Groups)
            {
                var invalidMembers = group.Members.Where(m => !validIds.Contains(m)).ToList();
                foreach (var member in invalidMembers)
                {
                    group.Members.Remove(member);
                    result.AppliedActions.Add($"Removed invalid member {member} from group '{group.Name}'");
                    result.WasModified = true;
                }
            }
        }

        private static void FixMetadata(GhJsonDocument document, FixResult result)
        {
            document.Metadata ??= new GhJsonMetadata();

            var expectedComponentCount = document.Components.Count;
            var expectedConnectionCount = document.Connections?.Count ?? 0;
            var expectedGroupCount = document.Groups?.Count ?? 0;

            if (document.Metadata.ComponentCount != expectedComponentCount)
            {
                document.Metadata.ComponentCount = expectedComponentCount;
                result.AppliedActions.Add($"Updated component count to {expectedComponentCount}");
                result.WasModified = true;
            }

            if (document.Metadata.ConnectionCount != expectedConnectionCount)
            {
                document.Metadata.ConnectionCount = expectedConnectionCount;
                result.AppliedActions.Add($"Updated connection count to {expectedConnectionCount}");
                result.WasModified = true;
            }

            if (document.Metadata.GroupCount != expectedGroupCount)
            {
                document.Metadata.GroupCount = expectedGroupCount;
                result.AppliedActions.Add($"Updated group count to {expectedGroupCount}");
                result.WasModified = true;
            }

            var now = DateTime.UtcNow;
            if (!document.Metadata.Modified.HasValue || document.Metadata.Modified.Value != now)
            {
                document.Metadata.Modified = now;
                result.AppliedActions.Add("Updated modified timestamp");
                result.WasModified = true;
            }
        }
    }
}
