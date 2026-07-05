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

            foreach (var conn in document.Connections)
            {
                if (idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) &&
                    idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                {
                    if (nodeByKey.TryGetValue(toGuid, out var toNode) &&
                        nodeByKey.TryGetValue(fromGuid, out var fromNode))
                    {
                        int inputIndex = conn.To.ParamIndex ?? -1;
                        toNode.Parents[fromGuid] = inputIndex;

                        int outputIndex = conn.From.ParamIndex ?? -1;
                        fromNode.Children[toGuid] = outputIndex;
                    }
                }
            }

            return nodes;
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
