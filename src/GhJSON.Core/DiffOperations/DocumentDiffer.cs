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
    /// Computes a <see cref="GhPatchDocument"/> describing the differences between two
    /// GhJSON documents.
    /// </summary>
    internal static class DocumentDiffer
    {
        private static readonly JsonSerializerSettings BaseSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            Converters = { new PivotConverter() },
        };

        public static DiffResult Diff(GhJsonDocument left, GhJsonDocument right, DiffOptions? options = null)
        {
            options ??= DiffOptions.Default;

            var result = new DiffResult();
            var patch = result.Patch;

            patch.Schema = right.Schema ?? left.Schema;
            patch.Kind = "ghpatch";

            patch.Patch.Base = new GhPatchBaseRef
            {
                Schema = left.Schema,
                Checksum = options.IncludeBaseChecksum
                    ? DocumentNormalizer.ComputeChecksum(left, options)
                    : null,
            };

            patch.Patch.Metadata = DiffMetadata(left.Metadata, right.Metadata, options);
            patch.Patch.Components = DiffComponents(left, right, options, result);
            patch.Patch.Connections = DiffConnections(left, right, result);
            patch.Patch.Groups = DiffGroups(left, right, result);

            return result;
        }

        // ---------------- Metadata ----------------

        private static GhPatchMetadataOp? DiffMetadata(GhJsonMetadata? left, GhJsonMetadata? right, DiffOptions options)
        {
            var leftObj = left == null ? new JObject() : JObject.FromObject(left, JsonSerializer.Create(BaseSettings));
            var rightObj = right == null ? new JObject() : JObject.FromObject(right, JsonSerializer.Create(BaseSettings));

            if (options.IgnoreMetadataCounters)
            {
                leftObj.Remove("componentCount");
                leftObj.Remove("connectionCount");
                leftObj.Remove("groupCount");
                rightObj.Remove("componentCount");
                rightObj.Remove("connectionCount");
                rightObj.Remove("groupCount");
            }

            if (options.IgnoreMetadataTimestamps)
            {
                leftObj.Remove("created");
                leftObj.Remove("modified");
                rightObj.Remove("created");
                rightObj.Remove("modified");
            }

            var op = DiffJObjectShallow(leftObj, rightObj);
            if (op == null)
            {
                return null;
            }

            return new GhPatchMetadataOp { Set = op.Set, Remove = op.Remove };
        }

        // ---------------- Components ----------------

        private static GhPatchComponentsOp? DiffComponents(
            GhJsonDocument left,
            GhJsonDocument right,
            DiffOptions options,
            DiffResult result)
        {
            var leftMap = ComponentIdentityIndex.Build(left.Components);
            var rightMap = ComponentIdentityIndex.Build(right.Components);

            var add = new List<GhJsonComponent>();
            var remove = new List<GhPatchComponentMatch>();
            var modify = new List<GhPatchComponentModify>();

            var matched = new HashSet<GhJsonComponent>(ReferenceEqualityComparer.Default);

            foreach (var rightComp in right.Components)
            {
                if (leftMap.TryMatch(rightComp, out var leftComp) && leftComp != null)
                {
                    matched.Add(leftComp);
                    var op = BuildComponentModify(leftComp, rightComp, options);
                    if (op != null)
                    {
                        modify.Add(op);
                    }
                }
                else
                {
                    add.Add(rightComp);
                }
            }

            foreach (var leftComp in left.Components)
            {
                if (matched.Contains(leftComp))
                {
                    continue;
                }

                remove.Add(MakeComponentMatch(leftComp));
            }

            if (add.Count == 0 && remove.Count == 0 && modify.Count == 0)
            {
                return null;
            }

            result.ComponentOpCount = add.Count + remove.Count + modify.Count;
            return new GhPatchComponentsOp
            {
                Add = add.Count == 0 ? null : add,
                Remove = remove.Count == 0 ? null : remove,
                Modify = modify.Count == 0 ? null : modify,
            };
        }

        private static GhPatchComponentMatch MakeComponentMatch(GhJsonComponent component)
        {
            if (component.InstanceGuid.HasValue && component.InstanceGuid != Guid.Empty)
            {
                return new GhPatchComponentMatch { InstanceGuid = component.InstanceGuid };
            }

            if (component.Id.HasValue)
            {
                return new GhPatchComponentMatch { Id = component.Id };
            }

            return new GhPatchComponentMatch
            {
                ComponentGuid = component.ComponentGuid,
                Name = component.Name,
                Pivot = component.Pivot,
            };
        }

        private static GhPatchComponentModify? BuildComponentModify(
            GhJsonComponent leftComp,
            GhJsonComponent rightComp,
            DiffOptions options)
        {
            var leftObj = JObject.FromObject(leftComp, JsonSerializer.Create(BaseSettings));
            var rightObj = JObject.FromObject(rightComp, JsonSerializer.Create(BaseSettings));

            if (options.IgnoreRuntimeMessages)
            {
                leftObj.Remove("errors");
                leftObj.Remove("warnings");
                leftObj.Remove("remarks");
                rightObj.Remove("errors");
                rightObj.Remove("warnings");
                rightObj.Remove("remarks");
            }

            if (options.IgnorePivots)
            {
                leftObj.Remove("pivot");
                rightObj.Remove("pivot");
            }

            // Pull out structured sub-objects.
            var leftState = leftObj["componentState"] as JObject;
            var rightState = rightObj["componentState"] as JObject;
            leftObj.Remove("componentState");
            rightObj.Remove("componentState");

            var leftInput = leftObj["inputSettings"] as JArray;
            var rightInput = rightObj["inputSettings"] as JArray;
            leftObj.Remove("inputSettings");
            rightObj.Remove("inputSettings");

            var leftOutput = leftObj["outputSettings"] as JArray;
            var rightOutput = rightObj["outputSettings"] as JArray;
            leftObj.Remove("outputSettings");
            rightObj.Remove("outputSettings");

            var shallow = DiffJObjectShallow(leftObj, rightObj);
            var stateOp = DiffComponentState(leftState, rightState);
            var inputOp = DiffParameterSettings(leftInput, rightInput);
            var outputOp = DiffParameterSettings(leftOutput, rightOutput);

            if (shallow == null && stateOp == null && inputOp == null && outputOp == null)
            {
                return null;
            }

            return new GhPatchComponentModify
            {
                Match = MakeComponentMatch(rightComp),
                Set = shallow?.Set,
                Remove = shallow?.Remove,
                ComponentState = stateOp,
                InputSettings = inputOp,
                OutputSettings = outputOp,
            };
        }

        private static GhPatchComponentStateOp? DiffComponentState(JObject? left, JObject? right)
        {
            left ??= new JObject();
            right ??= new JObject();

            var leftExt = left["extensions"] as JObject;
            var rightExt = right["extensions"] as JObject;
            left.Remove("extensions");
            right.Remove("extensions");

            var shallow = DiffJObjectShallow(left, right);
            var extOp = DiffExtensions(leftExt, rightExt);

            if (shallow == null && extOp == null)
            {
                return null;
            }

            return new GhPatchComponentStateOp
            {
                Set = shallow?.Set,
                Remove = shallow?.Remove,
                Extensions = extOp,
            };
        }

        private static GhPatchExtensionsOp? DiffExtensions(JObject? left, JObject? right)
        {
            left ??= new JObject();
            right ??= new JObject();

            var set = new Dictionary<string, JObject>();
            var remove = new List<string>();

            foreach (var prop in right.Properties())
            {
                if (left[prop.Name] is JObject leftVal && prop.Value is JObject rightVal)
                {
                    if (!JToken.DeepEquals(leftVal, rightVal))
                    {
                        set[prop.Name] = rightVal;
                    }
                }
                else if (prop.Value is JObject onlyRight)
                {
                    set[prop.Name] = onlyRight;
                }
            }

            foreach (var prop in left.Properties())
            {
                if (right[prop.Name] == null)
                {
                    remove.Add(prop.Name);
                }
            }

            if (set.Count == 0 && remove.Count == 0)
            {
                return null;
            }

            return new GhPatchExtensionsOp
            {
                Set = set.Count == 0 ? null : set,
                Remove = remove.Count == 0 ? null : remove,
            };
        }

        private static GhPatchParameterSettingsOp? DiffParameterSettings(JArray? left, JArray? right)
        {
            var leftByName = (left ?? new JArray())
                .OfType<JObject>()
                .Where(p => p["parameterName"] != null)
                .ToDictionary(p => (string)p["parameterName"]!, p => p);
            var rightByName = (right ?? new JArray())
                .OfType<JObject>()
                .Where(p => p["parameterName"] != null)
                .ToDictionary(p => (string)p["parameterName"]!, p => p);

            var byParam = new Dictionary<string, GhPatchParameterSettingsEntryOp>();

            foreach (var kvp in rightByName)
            {
                if (!leftByName.TryGetValue(kvp.Key, out var leftP))
                {
                    // Whole entry is new — treat the entire object (minus parameterName) as a "set"
                    var clone = (JObject)kvp.Value.DeepClone();
                    clone.Remove("parameterName");
                    byParam[kvp.Key] = new GhPatchParameterSettingsEntryOp { Set = clone };
                    continue;
                }

                var leftCopy = (JObject)leftP.DeepClone();
                var rightCopy = (JObject)kvp.Value.DeepClone();
                leftCopy.Remove("parameterName");
                rightCopy.Remove("parameterName");

                var shallow = DiffJObjectShallow(leftCopy, rightCopy);
                if (shallow != null)
                {
                    byParam[kvp.Key] = new GhPatchParameterSettingsEntryOp
                    {
                        Set = shallow.Set,
                        Remove = shallow.Remove,
                    };
                }
            }

            // Note: removed parameters are out of scope for v1 ghpatch (no add/remove of
            // parameterSettings entries).
            if (byParam.Count == 0)
            {
                return null;
            }

            return new GhPatchParameterSettingsOp { ByParameterName = byParam };
        }

        // ---------------- Connections ----------------

        private static GhPatchConnectionsOp? DiffConnections(GhJsonDocument left, GhJsonDocument right, DiffResult result)
        {
            var leftSet = new HashSet<ConnectionKey>(
                (left.Connections ?? Enumerable.Empty<GhJsonConnection>())
                .Select(ConnectionKey.From));
            var rightSet = new HashSet<ConnectionKey>(
                (right.Connections ?? Enumerable.Empty<GhJsonConnection>())
                .Select(ConnectionKey.From));

            var add = (right.Connections ?? Enumerable.Empty<GhJsonConnection>())
                .Where(c => !leftSet.Contains(ConnectionKey.From(c)))
                .ToList();

            var remove = (left.Connections ?? Enumerable.Empty<GhJsonConnection>())
                .Where(c => !rightSet.Contains(ConnectionKey.From(c)))
                .ToList();

            if (add.Count == 0 && remove.Count == 0)
            {
                return null;
            }

            result.ConnectionOpCount = add.Count + remove.Count;
            return new GhPatchConnectionsOp
            {
                Add = add.Count == 0 ? null : add,
                Remove = remove.Count == 0 ? null : remove,
            };
        }

        // ---------------- Groups ----------------

        private static GhPatchGroupsOp? DiffGroups(GhJsonDocument left, GhJsonDocument right, DiffResult result)
        {
            var leftGroups = left.Groups ?? new List<GhJsonGroup>();
            var rightGroups = right.Groups ?? new List<GhJsonGroup>();

            var leftByGuid = leftGroups.Where(g => g.InstanceGuid.HasValue && g.InstanceGuid != Guid.Empty)
                .GroupBy(g => g.InstanceGuid!.Value).ToDictionary(g => g.Key, g => g.First());
            var leftById = leftGroups.Where(g => g.Id.HasValue)
                .GroupBy(g => g.Id!.Value).ToDictionary(g => g.Key, g => g.First());

            var add = new List<GhJsonGroup>();
            var remove = new List<GhPatchGroupMatch>();
            var modify = new List<GhPatchGroupModify>();
            var matched = new HashSet<GhJsonGroup>(ReferenceEqualityComparer.Default);

            foreach (var rightGroup in rightGroups)
            {
                GhJsonGroup? matchedLeft = null;
                if (rightGroup.InstanceGuid.HasValue
                    && rightGroup.InstanceGuid != Guid.Empty
                    && leftByGuid.TryGetValue(rightGroup.InstanceGuid.Value, out var byGuid))
                {
                    matchedLeft = byGuid;
                }
                else if (rightGroup.Id.HasValue && leftById.TryGetValue(rightGroup.Id.Value, out var byId))
                {
                    matchedLeft = byId;
                }

                if (matchedLeft != null)
                {
                    matched.Add(matchedLeft);
                    var op = BuildGroupModify(matchedLeft, rightGroup);
                    if (op != null)
                    {
                        modify.Add(op);
                    }
                }
                else
                {
                    add.Add(rightGroup);
                }
            }

            foreach (var leftGroup in leftGroups)
            {
                if (matched.Contains(leftGroup))
                {
                    continue;
                }

                remove.Add(new GhPatchGroupMatch
                {
                    InstanceGuid = leftGroup.InstanceGuid,
                    Id = leftGroup.Id,
                });
            }

            if (add.Count == 0 && remove.Count == 0 && modify.Count == 0)
            {
                return null;
            }

            result.GroupOpCount = add.Count + remove.Count + modify.Count;
            return new GhPatchGroupsOp
            {
                Add = add.Count == 0 ? null : add,
                Remove = remove.Count == 0 ? null : remove,
                Modify = modify.Count == 0 ? null : modify,
            };
        }

        private static GhPatchGroupModify? BuildGroupModify(GhJsonGroup left, GhJsonGroup right)
        {
            var leftObj = JObject.FromObject(left, JsonSerializer.Create(BaseSettings));
            var rightObj = JObject.FromObject(right, JsonSerializer.Create(BaseSettings));

            var leftMembers = (leftObj["members"] as JArray)?
                .OfType<JValue>()
                .Where(v => v.Type == JTokenType.Integer)
                .Select(v => Convert.ToInt32(v.Value!))
                .ToList() ?? new List<int>();
            var rightMembers = (rightObj["members"] as JArray)?
                .OfType<JValue>()
                .Where(v => v.Type == JTokenType.Integer)
                .Select(v => Convert.ToInt32(v.Value!))
                .ToList() ?? new List<int>();
            leftObj.Remove("members");
            rightObj.Remove("members");

            var shallow = DiffJObjectShallow(leftObj, rightObj);

            var addMembers = rightMembers.Except(leftMembers).ToList();
            var removeMembers = leftMembers.Except(rightMembers).ToList();
            GhPatchGroupMembersOp? membersOp = null;
            if (addMembers.Count > 0 || removeMembers.Count > 0)
            {
                membersOp = new GhPatchGroupMembersOp
                {
                    Add = addMembers.Count == 0 ? null : addMembers,
                    Remove = removeMembers.Count == 0 ? null : removeMembers,
                };
            }

            if (shallow == null && membersOp == null)
            {
                return null;
            }

            return new GhPatchGroupModify
            {
                Match = new GhPatchGroupMatch
                {
                    InstanceGuid = right.InstanceGuid,
                    Id = right.Id,
                },
                Set = shallow?.Set,
                Remove = shallow?.Remove,
                Members = membersOp,
            };
        }

        // ---------------- Shared helpers ----------------

        private sealed class ShallowDiff
        {
            public JObject? Set { get; set; }

            public List<string>? Remove { get; set; }
        }

        private static ShallowDiff? DiffJObjectShallow(JObject left, JObject right)
        {
            var set = new JObject();
            var remove = new List<string>();

            foreach (var prop in right.Properties())
            {
                var leftVal = left[prop.Name];
                if (leftVal == null)
                {
                    set[prop.Name] = prop.Value.DeepClone();
                }
                else if (!JToken.DeepEquals(leftVal, prop.Value))
                {
                    set[prop.Name] = prop.Value.DeepClone();
                }
            }

            foreach (var prop in left.Properties())
            {
                if (right[prop.Name] == null)
                {
                    remove.Add(prop.Name);
                }
            }

            if (set.Count == 0 && remove.Count == 0)
            {
                return null;
            }

            return new ShallowDiff
            {
                Set = set.Count == 0 ? null : set,
                Remove = remove.Count == 0 ? null : remove,
            };
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Default = new ReferenceEqualityComparer();

            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

            int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
