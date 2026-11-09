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
using System.Drawing;
using System.Linq;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DependencyGraph.Internal
{
    internal static class GraphBuilder
    {
        public static List<LayoutNode> BuildGraph(GhJsonDocument document)
        {
            // Previously: c.InstanceGuid.GetValueOrDefault() collapsed every component that
            // lacked an InstanceGuid into Guid.Empty, causing silent GUID collisions and
            // dropped connections. Synthesize a deterministic GUID from the component's Id
            // when InstanceGuid is absent so layout remains correct for id-only components.
            var nodes = document.Components.Select(c => new LayoutNode
            {
                ComponentId = GetStableKey(c),
                Pivot = c.Pivot?.ToPointF() ?? PointF.Empty,
                Parents = new Dictionary<Guid, int>(),
                Children = new Dictionary<Guid, int>(),
            }).ToList();

            var idToGuidMap = BuildIdToStableKeyMap(document);
            if (document.Connections == null)
            {
                return nodes;
            }

            // Index nodes by their stable key for O(1) endpoint resolution. When several
            // components collapse to the same key (e.g. duplicate ids), the first wins.
            var nodeByKey = new Dictionary<Guid, LayoutNode>(nodes.Count);
            foreach (var node in nodes)
            {
                if (!nodeByKey.ContainsKey(node.ComponentId))
                {
                    nodeByKey[node.ComponentId] = node;
                }
            }

            var componentById = new Dictionary<int, GhJsonComponent>();
            foreach (var c in document.Components)
            {
                if (c.Id.HasValue && !componentById.ContainsKey(c.Id.Value))
                {
                    componentById[c.Id.Value] = c;
                }
            }

            // When neither a declared index nor a settings match resolves a port, the
            // connection's declaration order supplies each sibling endpoint a distinct
            // slot — the only ordering signal an id-and-name-only document carries.
            var inputSlots = new Dictionary<int, int>();
            var outputSlots = new Dictionary<int, int>();
            foreach (var conn in document.Connections)
            {
                if (idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) &&
                    idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                {
                    if (nodeByKey.TryGetValue(toGuid, out var toNode) &&
                        nodeByKey.TryGetValue(fromGuid, out var fromNode))
                    {
                        componentById.TryGetValue(conn.To.Id, out var toComponent);
                        componentById.TryGetValue(conn.From.Id, out var fromComponent);

                        int inputIndex = conn.To.ParamIndex
                            ?? ResolveParamIndex(toComponent?.InputSettings, conn.To.ParamName)
                            ?? NextSlot(inputSlots, conn.To.Id);
                        toNode.Parents[fromGuid] = inputIndex;

                        int outputIndex = conn.From.ParamIndex
                            ?? ResolveParamIndex(fromComponent?.OutputSettings, conn.From.ParamName)
                            ?? NextSlot(outputSlots, conn.From.Id);
                        fromNode.Children[toGuid] = outputIndex;
                    }
                }
            }

            // Port-slot estimates: the largest connected index + 1, raised by the declared
            // parameter count when the document carries settings for unconnected ports.
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var component = document.Components[i];
                node.InputPortCount = EstimatePortCount(node.Parents.Values, component.InputSettings?.Count ?? 0);
                node.OutputPortCount = EstimatePortCount(node.Children.Values, component.OutputSettings?.Count ?? 0);
            }

            return nodes;
        }

        /// <summary>
        /// Resolves a connection endpoint's <c>paramName</c> to its position in the
        /// component's declared parameter settings. Matches <c>parameterName</c> first,
        /// then <c>nickName</c>, so documents that only carry names still feed the
        /// port-aware ordering passes real indices instead of -1.
        /// </summary>
        private static int? ResolveParamIndex(IReadOnlyList<GhJsonParameterSettings>? settings, string? name)
        {
            if (settings == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (var i = 0; i < settings.Count; i++)
            {
                var setting = settings[i];
                if (setting != null &&
                    (string.Equals(setting.ParameterName, name, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(setting.NickName, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the next unassigned port slot for a component id, in connection
        /// declaration order. Distinct sibling endpoints get distinct slots so the
        /// crossing minimizer can still rank their sources.
        /// </summary>
        private static int NextSlot(Dictionary<int, int> slots, int componentId)
        {
            slots.TryGetValue(componentId, out var next);
            slots[componentId] = next + 1;
            return next;
        }

        /// <summary>
        /// Estimates a node's port count as the largest connected port index + 1, never below
        /// the declared parameter count. An unknown (-1) index still implies one port exists.
        /// </summary>
        private static int EstimatePortCount(IEnumerable<int> usedPortIndices, int declaredCount)
        {
            var count = declaredCount;
            foreach (var index in usedPortIndices)
            {
                var implied = index < 0 ? 1 : index + 1;
                if (implied > count)
                {
                    count = implied;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns a deterministic GUID for <paramref name="component"/>: the component's
        /// <see cref="GhJsonComponent.InstanceGuid"/> when present, otherwise a GUID
        /// derived from <see cref="GhJsonComponent.Id"/>. Components with neither
        /// identifier fall back to <see cref="Guid.Empty"/> (validation catches these
        /// earlier).
        /// </summary>
        internal static Guid GetStableKey(GhJsonComponent component)
        {
            if (component.InstanceGuid.HasValue && component.InstanceGuid.Value != Guid.Empty)
            {
                return component.InstanceGuid.Value;
            }

            if (component.Id.HasValue)
            {
                return SynthesizeGuidFromId(component.Id.Value);
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Builds a map from component <c>Id</c> to stable layout key, using the same
        /// synthesis rule as <see cref="GetStableKey"/> so connection endpoints resolve
        /// consistently whether or not a component exposes an <c>InstanceGuid</c>.
        /// </summary>
        private static Dictionary<int, Guid> BuildIdToStableKeyMap(GhJsonDocument document)
        {
            var map = new Dictionary<int, Guid>();
            foreach (var c in document.Components)
            {
                if (c.Id.HasValue)
                {
                    // First component wins on duplicate ids (invalid input); validation
                    // reports the duplicate separately. Avoids an ArgumentException here.
                    map[c.Id.Value] = GetStableKey(c);
                }
            }

            return map;
        }

        /// <summary>
        /// Produces a stable, reserved GUID from an integer id by encoding the id into
        /// the low 32 bits of the GUID. Reserved prefix (<c>47534a4c-0000-…</c>,
        /// ASCII "GJL"-style marker) makes synthesized keys distinguishable from real
        /// Grasshopper instance GUIDs when debugging.
        /// </summary>
        private static Guid SynthesizeGuidFromId(int id)
        {
            var bytes = new byte[16];
            // Fixed reserved prefix to clearly mark synthetic keys.
            bytes[0] = 0x47; // 'G'
            bytes[1] = 0x4A; // 'J'
            bytes[2] = 0x4C; // 'L'
            bytes[3] = 0x00;
            // Encode id into the trailing 4 bytes (big-endian for readability).
            bytes[12] = (byte)((id >> 24) & 0xFF);
            bytes[13] = (byte)((id >> 16) & 0xFF);
            bytes[14] = (byte)((id >> 8) & 0xFF);
            bytes[15] = (byte)(id & 0xFF);
            return new Guid(bytes);
        }
    }
}
