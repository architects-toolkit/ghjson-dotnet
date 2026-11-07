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

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>Fits compatible port constraints as rigid vertical groups after anchor fitting.</summary>
    internal sealed partial class AlignmentConstraints
    {
        /// <summary>Anchor-fitted candidate bounds centers, never modified in place.</summary>
        private readonly Dictionary<Guid, PointF> positions;

        /// <summary>Shared collision, corridor and authored-anchor contracts.</summary>
        private readonly LayoutRefinementOptions options;

        /// <summary>Stable measured geometry for this refinement invocation.</summary>
        private readonly Dictionary<Guid, LayoutNodeMetrics?> metrics;

        /// <summary>Incident wire counts used to prioritize leaves and central references.</summary>
        private readonly Dictionary<Guid, int> degrees;

        /// <summary>Snapshots measured geometry once for consistent constraint evaluation.</summary>
        private AlignmentConstraints(Dictionary<Guid, PointF> positions, LayoutRefinementOptions options)
        {
            this.positions = positions;
            this.options = options;
            this.metrics = positions.Keys.ToDictionary(id => id, id => LayoutNodeMetrics.Resolve(options.NodeMetricsProvider, id));
            this.degrees = positions.Keys.ToDictionary(id => id, _ => 0);
        }

        /// <summary>Aligns feasible leaves and chains without moving authored positions.</summary>
        internal static Dictionary<Guid, PointF> Apply(
            Dictionary<Guid, PointF> positions, GhJsonDocument document, LayoutRefinementOptions options)
        {
            return options.NodeMetricsProvider == null
                ? positions
                : new AlignmentConstraints(positions, options).Run(document);
        }

        /// <summary>Accepts compatible edges in stable priority order, then places whole groups.</summary>
        private Dictionary<Guid, PointF> Run(GhJsonDocument document)
        {
            var edges = this.BuildEdges(document);
            var owners = this.positions.Keys.ToDictionary(
                id => id,
                id => new Group(new Dictionary<Guid, float> { [id] = 0f }, new List<Edge>(),
                    this.positions[id].Y, this.options.FixedPositions?.ContainsKey(id) == true));

            foreach (var edge in edges.OrderBy(e => Math.Min(this.degrees[e.From], this.degrees[e.To]) == 1 ? 0 : 1)
                         .ThenBy(e => e.From).ThenBy(e => e.FromIndex).ThenBy(e => e.To).ThenBy(e => e.ToIndex))
            {
                var left = owners[edge.From];
                var right = owners[edge.To];
                if (ReferenceEquals(left, right))
                {
                    continue;
                }

                var offsets = new Dictionary<Guid, float>(left.Offsets);
                var shift = left.Offsets[edge.From] + edge.FromDelta - edge.ToDelta - right.Offsets[edge.To];
                foreach (var pair in right.Offsets)
                {
                    offsets.Add(pair.Key, pair.Value + shift);
                }

                var reference = offsets.Keys.OrderByDescending(id => this.degrees[id])
                    .ThenByDescending(id => this.metrics[id]?.Kind == LayoutNodeKind.Component).ThenBy(id => id).First();
                var merged = new Group(offsets, left.Edges.Concat(right.Edges).Concat(new[] { edge }).ToList(),
                    this.positions[reference].Y - offsets[reference], false);
                if (!this.FitAnchors(merged) || !this.InternallyClear(merged, left, right))
                {
                    continue;
                }

                var included = new HashSet<Edge>(merged.Edges);
                var compatible = edges.Where(candidate => !included.Contains(candidate) &&
                    offsets.ContainsKey(candidate.From) && offsets.ContainsKey(candidate.To) &&
                    Math.Abs(offsets[candidate.To] - offsets[candidate.From] -
                             (candidate.FromDelta - candidate.ToDelta)) <= 0.001f).ToList();
                if (!this.EdgesClear(compatible, offsets.Keys, merged))
                {
                    continue;
                }

                merged.Edges.AddRange(compatible);
                if (merged.Pinned && owners.Values.Distinct().Any(other =>
                        other.Pinned && !ReferenceEquals(other, left) && !ReferenceEquals(other, right) &&
                        this.Conflicts(merged, other)))
                {
                    continue;
                }

                foreach (var id in offsets.Keys)
                {
                    owners[id] = merged;
                }
            }

            var placed = new List<Group>();
            foreach (var group in owners.Values.Distinct().OrderByDescending(g => g.Pinned)
                         .ThenByDescending(g => g.Offsets.Count).ThenBy(g => g.Offsets.Keys.Min()))
            {
                if (!group.Pinned)
                {
                    group.Origin = NearestFree(group.Origin, this.ForbiddenOrigins(group, placed));
                }

                placed.Add(group);
            }

            var result = new Dictionary<Guid, PointF>(this.positions);
            foreach (var group in placed)
            {
                foreach (var pair in group.Offsets)
                {
                    result[pair.Key] = new PointF(result[pair.Key].X, group.Origin + pair.Value);
                }
            }

            if (this.options.FixedPositions != null)
            {
                foreach (var pair in this.options.FixedPositions.Where(pair => result.ContainsKey(pair.Key)))
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        /// <summary>Preserves every distinct measured wire, including parallel component-pair edges.</summary>
        private List<Edge> BuildEdges(GhJsonDocument document)
        {
            var keys = ConnectionKeyMap.Build(document);
            var edges = new List<Edge>();
            foreach (var connection in document.Connections ?? Enumerable.Empty<GhJsonConnection>())
            {
                if (!keys.TryGetValue(connection.From.Id, out var from) ||
                    !keys.TryGetValue(connection.To.Id, out var to) || from == to ||
                    !this.positions.ContainsKey(from) || !this.positions.ContainsKey(to))
                {
                    continue;
                }

                this.degrees[from]++;
                this.degrees[to]++;
                if (this.HasMeasuredBounds(from) && this.HasMeasuredBounds(to) &&
                    TryPort(this.metrics[from], connection.From, false, out var fromIndex, out var fromDelta) &&
                    TryPort(this.metrics[to], connection.To, true, out var toIndex, out var toDelta) &&
                    this.Bounds(from, this.positions[from].Y).Right <= this.Bounds(to, this.positions[to].Y).Left)
                {
                    edges.Add(new Edge(from, to, fromDelta, toDelta, fromIndex, toIndex));
                }
            }

            return edges;
        }

        /// <summary>Resolves indices first, then unambiguous measured names; unknown ports do not constrain.</summary>
        private static bool TryPort(LayoutNodeMetrics? metrics, GhJsonConnectionEndpoint endpoint,
            bool input, out int index, out float delta)
        {
            index = 0;
            delta = 0f;
            if (metrics == null || metrics.Kind == LayoutNodeKind.Other)
            {
                return false;
            }

            if (metrics.Kind == LayoutNodeKind.Parameter)
            {
                var grips = input ? metrics.InputPortDeltas : metrics.OutputPortDeltas;
                delta = grips.Count == 0 ? 0f : grips[0];
                return !float.IsNaN(delta) && !float.IsInfinity(delta);
            }

            var deltas = input ? metrics.InputPortDeltas : metrics.OutputPortDeltas;
            var names = input ? metrics.InputPortIndices : metrics.OutputPortIndices;
            if (endpoint.ParamIndex.HasValue)
            {
                index = endpoint.ParamIndex.Value;
            }
            else if (!string.IsNullOrEmpty(endpoint.ParamName))
            {
                if (names == null || !names.TryGetValue(endpoint.ParamName!, out index))
                {
                    return false;
                }
            }
            else if (deltas.Count > 1)
            {
                return false;
            }

            if (index < 0 || (index >= deltas.Count && (deltas.Count != 0 || index != 0)))
            {
                return false;
            }

            delta = deltas.Count == 0 ? 0f : deltas[index];
            return !float.IsNaN(delta) && !float.IsInfinity(delta);
        }

        /// <summary>Finds the single translation compatible with all hard anchors in a group.</summary>
        private bool FitAnchors(Group group)
        {
            foreach (var pair in group.Offsets.OrderBy(pair => pair.Key))
            {
                if (this.options.FixedPositions != null && this.options.FixedPositions.TryGetValue(pair.Key, out var anchor))
                {
                    var origin = anchor.Y - pair.Value;
                    if (group.Pinned && Math.Abs(group.Origin - origin) > 0.001f)
                    {
                        return false;
                    }

                    group.Origin = origin;
                    group.Pinned = true;
                }
            }

            return true;
        }

        /// <summary>A rigid set of node offsets and the horizontal wires it preserves.</summary>
        private sealed class Group
        {
            /// <summary>Member Y offsets relative to the group's common origin.</summary>
            public Dictionary<Guid, float> Offsets { get; }

            /// <summary>All compatible horizontal wires, including parallel occupied ports.</summary>
            public List<Edge> Edges { get; }

            /// <summary>Common vertical translation applied to every member.</summary>
            public float Origin { get; set; }

            /// <summary>Whether an authored node prevents translating this group.</summary>
            public bool Pinned { get; set; }

            /// <summary>Creates a candidate alignment group.</summary>
            public Group(Dictionary<Guid, float> offsets, List<Edge> edges, float origin, bool pinned)
            {
                this.Offsets = offsets;
                this.Edges = edges;
                this.Origin = origin;
                this.Pinned = pinned;
            }
        }

        /// <summary>A measured endpoint-to-endpoint alignment constraint.</summary>
        private sealed class Edge
        {
            /// <summary>Source layout key.</summary>
            public Guid From { get; }

            /// <summary>Target layout key.</summary>
            public Guid To { get; }

            /// <summary>Occupied output grip offset from its source bounds center.</summary>
            public float FromDelta { get; }

            /// <summary>Occupied input grip offset from its target bounds center.</summary>
            public float ToDelta { get; }

            /// <summary>Resolved output index for deterministic edge ordering.</summary>
            public int FromIndex { get; }

            /// <summary>Resolved input index for deterministic edge ordering.</summary>
            public int ToIndex { get; }

            /// <summary>Captures the exact occupied ports of one wire.</summary>
            public Edge(Guid from, Guid to, float fromDelta, float toDelta, int fromIndex, int toIndex)
            {
                this.From = from;
                this.To = to;
                this.FromDelta = fromDelta;
                this.ToDelta = toDelta;
                this.FromIndex = fromIndex;
                this.ToIndex = toIndex;
            }
        }
    }
}
