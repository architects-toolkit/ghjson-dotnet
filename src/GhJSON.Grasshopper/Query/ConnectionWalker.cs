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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Query
{
    /// <summary>
    /// Walks the live Grasshopper connection graph without serialization.
    /// </summary>
    internal static class ConnectionWalker
    {
        /// <summary>
        /// Expands a seed set of objects by following connections up to <paramref name="depth"/> hops.
        /// Traverses the live <see cref="IGH_Param.Sources"/> and <see cref="IGH_Param.Recipients"/>
        /// rather than serializing the document, making it significantly faster than a serialize-then-walk approach.
        /// </summary>
        /// <param name="universe">All objects that may be reached (typically the full canvas).</param>
        /// <param name="seedGuids">Initial GUIDs to expand from.</param>
        /// <param name="depth">Number of connection hops.</param>
        /// <returns>All GUIDs reachable within <paramref name="depth"/>, including the seeds.</returns>
        public static HashSet<Guid> ExpandByDepth(
            IEnumerable<IGH_DocumentObject> universe,
            IEnumerable<Guid> seedGuids,
            int depth)
        {
            if (depth <= 0)
            {
                return new HashSet<Guid>(seedGuids);
            }

            // Build undirected adjacency from live parameter wiring
            var adjacency = BuildAdjacency(universe);

            var visited = new HashSet<Guid>(seedGuids);
            var frontier = new HashSet<Guid>(seedGuids);

            for (int level = 0; level < depth; level++)
            {
                var next = new HashSet<Guid>();
                foreach (var id in frontier)
                {
                    if (adjacency.TryGetValue(id, out var neighbors))
                    {
                        foreach (var n in neighbors)
                        {
                            if (!visited.Contains(n))
                            {
                                next.Add(n);
                            }
                        }
                    }
                }

                if (next.Count == 0)
                {
                    break;
                }

                visited.UnionWith(next);
                frontier = next;
            }

            return visited;
        }

        /// <summary>
        /// Classifies objects within a set into topological roles based on their connections
        /// to other objects <em>within the same set</em>.
        /// </summary>
        /// <param name="objects">The set of objects to classify.</param>
        /// <returns>A classification containing start, end, middle, and isolated node GUIDs.</returns>
        public static TopologyClassification Classify(IEnumerable<IGH_DocumentObject> objects)
        {
            var objectSet = new HashSet<Guid>(objects.Select(o => o.InstanceGuid));
            var hasIncoming = new HashSet<Guid>();
            var hasOutgoing = new HashSet<Guid>();

            foreach (var obj in objects)
            {
                foreach (var neighbor in GetOutputNeighborGuids(obj))
                {
                    if (objectSet.Contains(neighbor))
                    {
                        hasOutgoing.Add(obj.InstanceGuid);
                        hasIncoming.Add(neighbor);
                    }
                }
            }

            var result = new TopologyClassification();
            foreach (var guid in objectSet)
            {
                bool @in = hasIncoming.Contains(guid);
                bool @out = hasOutgoing.Contains(guid);

                if (!@in && @out)
                {
                    result.StartNodes.Add(guid);
                }
                else if (@in && !@out)
                {
                    result.EndNodes.Add(guid);
                }
                else if (@in && @out)
                {
                    result.MiddleNodes.Add(guid);
                }
                else
                {
                    result.IsolatedNodes.Add(guid);
                }
            }

            return result;
        }

        /// <summary>
        /// Builds an undirected adjacency map from live parameter wiring.
        /// Keys and values are top-level document-object GUIDs.
        /// </summary>
        private static Dictionary<Guid, HashSet<Guid>> BuildAdjacency(IEnumerable<IGH_DocumentObject> objects)
        {
            var adjacency = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var obj in objects)
            {
                var guid = obj.InstanceGuid;
                foreach (var neighbor in GetOutputNeighborGuids(obj))
                {
                    // Add both directions (undirected)
                    GetOrCreate(adjacency, guid).Add(neighbor);
                    GetOrCreate(adjacency, neighbor).Add(guid);
                }
            }

            return adjacency;
        }

        /// <summary>
        /// Returns the GUIDs of top-level document objects connected to the outputs of <paramref name="obj"/>.
        /// </summary>
        private static IEnumerable<Guid> GetOutputNeighborGuids(IGH_DocumentObject obj)
        {
            IList<IGH_Param>? outputs = null;

            if (obj is IGH_Component comp)
            {
                outputs = comp.Params.Output;
            }
            else if (obj is IGH_Param param)
            {
                outputs = new[] { param };
            }

            if (outputs == null)
            {
                yield break;
            }

            foreach (var output in outputs)
            {
                foreach (var recipient in output.Recipients)
                {
                    var owner = recipient.Attributes?.GetTopLevel?.DocObject;
                    if (owner != null)
                    {
                        yield return owner.InstanceGuid;
                    }
                }
            }
        }

        private static HashSet<Guid> GetOrCreate(Dictionary<Guid, HashSet<Guid>> dict, Guid key)
        {
            if (!dict.TryGetValue(key, out var set))
            {
                set = new HashSet<Guid>();
                dict[key] = set;
            }

            return set;
        }
    }

    /// <summary>
    /// Holds the topological classification of a set of document objects.
    /// </summary>
    internal sealed class TopologyClassification
    {
        /// <summary>
        /// Gets objects with outgoing but no incoming connections (data sources).
        /// </summary>
        public HashSet<Guid> StartNodes { get; } = new HashSet<Guid>();

        /// <summary>
        /// Gets objects with incoming but no outgoing connections (data sinks).
        /// </summary>
        public HashSet<Guid> EndNodes { get; } = new HashSet<Guid>();

        /// <summary>
        /// Gets objects with both incoming and outgoing connections (processors).
        /// </summary>
        public HashSet<Guid> MiddleNodes { get; } = new HashSet<Guid>();

        /// <summary>
        /// Gets objects with neither incoming nor outgoing connections.
        /// </summary>
        public HashSet<Guid> IsolatedNodes { get; } = new HashSet<Guid>();
    }
}
