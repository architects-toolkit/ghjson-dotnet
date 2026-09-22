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

namespace GhJSON.Core.DependencyGraph.Internal
{
    /// <summary>
    /// One layout edge reduced to the two absolute port points its wire travels between.
    /// Only forward edges spanning at least one intermediate column participate in
    /// corridor planning: adjacent-column wires cannot touch foreign bounds.
    /// </summary>
    internal sealed class CorridorEdge
    {
        /// <summary>Layout key of the source node.</summary>
        public Guid From { get; set; }

        /// <summary>Layout key of the target node.</summary>
        public Guid To { get; set; }

        /// <summary>Absolute canvas point where the wire leaves the source.</summary>
        public PointF FromPort { get; set; }

        /// <summary>Absolute canvas point where the wire enters the target.</summary>
        public PointF ToPort { get; set; }

        /// <summary>Zero-based column index of the source node.</summary>
        public int FromColumn { get; set; }

        /// <summary>Zero-based column index of the target node.</summary>
        public int ToColumn { get; set; }
    }

    /// <summary>
    /// Pure-geometry planner that keeps skip-edge wires clear of non-endpoint component
    /// bounds. For every wire spanning two or more columns it computes per-node vertical
    /// deltas: intermediate nodes are pushed toward their nearer edge relative to the
    /// wire, and the wire's target is nudged part-way into the freed corridor, so the
    /// correction is shared instead of landing on one side. Wires are modelled as
    /// straight chords; Grasshopper's actual bezier bulge stays close to the chord for
    /// intermediate columns, which is the only region examined.
    /// </summary>
    internal static class WireCorridorPlanner
    {
        /// <summary>Largest vertical shift a single node may receive.</summary>
        public const float MaxNodeDelta = 150f;

        /// <summary>Largest vertical shift applied to a skip edge's target port.</summary>
        public const float MaxTargetNudge = 60f;

        /// <summary>
        /// Computes per-node vertical deltas (positive = move down) that keep every skip
        /// wire at least <paramref name="clearance"/> pixels from the bounds of nodes in
        /// intermediate columns. Deltas are evaluated against already-shifted bounds so
        /// edges processed later see earlier corrections (deterministic sequential order).
        /// </summary>
        /// <param name="nodeBounds">Current bounds rect per layout key.</param>
        /// <param name="edges">Port-to-port edges with resolved column indices.</param>
        /// <param name="columns">Per-island column membership, ordered left to right.</param>
        /// <param name="clearance">Minimum required wire-to-bounds distance in pixels.</param>
        /// <returns>Vertical delta per node guid; only nodes that need to move appear.</returns>
        public static Dictionary<Guid, float> PlanVerticalDeltas(
            IReadOnlyDictionary<Guid, RectangleF> nodeBounds,
            IReadOnlyList<CorridorEdge> edges,
            IReadOnlyList<IReadOnlyList<Guid>> columns,
            float clearance)
        {
            var deltas = new Dictionary<Guid, float>();
            if (nodeBounds == null || edges == null || columns == null || clearance <= 0f)
            {
                return deltas;
            }

            // Working bounds reflect deltas already assigned, so later edges measure
            // against the corrected geometry rather than the stale input.
            var working = nodeBounds.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Deepest spans first: the longest wires establish their corridors before
            // shorter skip edges route through the space that remains. Processing a
            // short edge first could nudge its target into a corridor a longer edge
            // needs to carve out later.
            var skipEdges = edges
                .Where(e => e.ToColumn - e.FromColumn >= 2)
                .OrderByDescending(e => e.ToColumn - e.FromColumn)
                .ThenBy(e => e.FromColumn)
                .ThenBy(e => e.FromPort.Y)
                .ThenBy(e => e.To)
                .ToList();

            foreach (var edge in skipEdges)
            {
                PlanEdge(edge, columns, working, deltas, clearance);
            }

            return deltas;
        }

        /// <summary>
        /// Resolves one skip edge: nudge its target part-way toward a clear corridor
        /// (bounded), then push every still-violating intermediate node toward its
        /// nearer edge so the segment gains the required clearance.
        /// </summary>
        private static void PlanEdge(
            CorridorEdge edge,
            IReadOnlyList<IReadOnlyList<Guid>> columns,
            Dictionary<Guid, RectangleF> working,
            Dictionary<Guid, float> deltas,
            float clearance)
        {
            var obstacles = new List<(Guid Id, RectangleF Bounds)>();
            for (var c = edge.FromColumn + 1; c < edge.ToColumn && c < columns.Count; c++)
            {
                foreach (var id in columns[c])
                {
                    if (working.TryGetValue(id, out var rect))
                    {
                        obstacles.Add((id, rect));
                    }
                }
            }

            if (obstacles.Count == 0)
            {
                return;
            }

            var from = edge.FromPort;
            var to = edge.ToPort;

            // 1) Target nudge: raise/lower the wire's endpoint toward the y that would
            //    clear the deepest obstacle, covering only half the distance so the
            //    remaining correction is shared with the obstacle pushes.
            var targetNudge = RequiredTargetNudge(edge, obstacles, clearance);
            if (Math.Abs(targetNudge) > 0.01f)
            {
                to = new PointF(to.X, to.Y + targetNudge);
                ApplyDelta(edge.To, targetNudge, MaxTargetNudge, working, deltas);
            }

            // 2) Obstacle pushes: each still-violating node moves toward its nearer
            //    edge so the (updated) segment clears it.
            foreach (var (id, rect) in obstacles)
            {
                var current = working[id];
                var distance = SegmentToRectDistance(from, to, current);
                if (distance >= clearance)
                {
                    continue;
                }

                var dy = NearerEdgePush(from, to, current, clearance);
                if (Math.Abs(dy) > 0.01f)
                {
                    ApplyDelta(id, dy, MaxNodeDelta, working, deltas);
                }
            }
        }

        /// <summary>
        /// Vertical shift of the edge's target port that would make the segment (pivoting
        /// around the source port) help clear the violated obstacles. Only obstacles the
        /// wire currently violates contribute; the nudge direction follows the wire's
        /// side of the obstacle (wire in the upper half → lift the wire above it, lower
        /// half → drop it below). The smallest required correction is chosen and halved
        /// so endpoint movement shares the work with the obstacle pushes instead of
        /// overshooting past nearer obstacles.
        /// </summary>
        private static float RequiredTargetNudge(
            CorridorEdge edge,
            List<(Guid Id, RectangleF Bounds)> obstacles,
            float clearance)
        {
            var from = edge.FromPort;
            var to = edge.ToPort;
            var spanX = to.X - from.X;
            if (Math.Abs(spanX) < 1f)
            {
                return 0f;
            }

            var best = 0f;
            var bestMag = float.MaxValue;
            foreach (var (_, rect) in obstacles)
            {
                if (SegmentToRectDistance(from, to, rect) >= clearance)
                {
                    continue;
                }

                // Parameter range of the segment inside this rect's x-extent.
                var t0 = Math.Max(0f, (rect.Left - from.X) / spanX);
                var t1 = Math.Min(1f, (rect.Right - from.X) / spanX);
                if (t1 <= t0)
                {
                    continue;
                }

                var yAtLeft = SegmentY(from, to, rect.Left);
                var yAtRight = SegmentY(from, to, rect.Right);
                var tEdge = Math.Abs(yAtRight - yAtLeft) < 0.001f
                    ? t1
                    : (yAtRight > yAtLeft ? t1 : t0);
                if (tEdge <= 0.001f)
                {
                    continue;
                }

                // The wire pivots around the source port, so a needed y difference at
                // the rect edge maps to a target-port delta of need/t.
                float candidate;
                var wireMidY = SegmentY(from, to, (rect.Left + rect.Right) / 2f);
                if (wireMidY <= rect.Top + rect.Height / 2f)
                {
                    // Wire in the rect's upper half → lift it above the top edge.
                    var need = Math.Max(yAtLeft, yAtRight) - (rect.Top - clearance);
                    candidate = -(need / tEdge);
                }
                else
                {
                    // Wire in the lower half → drop it below the bottom edge.
                    var need = (rect.Bottom + clearance) - Math.Min(yAtLeft, yAtRight);
                    candidate = need / tEdge;
                }

                if (Math.Abs(candidate) < bestMag)
                {
                    bestMag = Math.Abs(candidate);
                    best = candidate;
                }
            }

            return best * 0.5f;
        }

        /// <summary>
        /// Push the rect through the edge nearer the wire so the segment exits with
        /// <paramref name="clearance"/>. The direction follows the wire's side of the
        /// rect: wire in the upper half → the rect moves down and the wire exits through
        /// the top edge; wire in the lower half → the rect moves up. Positive dy moves
        /// the node down.
        /// </summary>
        private static float NearerEdgePush(
            PointF from,
            PointF to,
            RectangleF rect,
            float clearance)
        {
            // Highest/lowest wire y across the rect's x-extent.
            var yLeft = SegmentY(from, to, rect.Left);
            var yRight = SegmentY(from, to, rect.Right);
            var wireMax = Math.Max(yLeft, yRight);
            var wireMin = Math.Min(yLeft, yRight);

            var wireMidY = SegmentY(from, to, (rect.Left + rect.Right) / 2f);
            if (wireMidY <= rect.Top + rect.Height / 2f)
            {
                // Wire runs through/above the upper half: push the rect down until its
                // top sits `clearance` below the wire's lowest point. dy is >= 0; a
                // negative value means the wire already clears the top.
                return Math.Max(0f, wireMax + clearance - rect.Top);
            }

            // Wire runs through/below the lower half: push the rect up until its
            // bottom sits `clearance` above the wire's highest point. dy is <= 0.
            return Math.Min(0f, wireMin - clearance - rect.Bottom);
        }

        /// <summary>
        /// Accumulates a vertical delta for one node (clamped to <paramref name="cap"/>)
        /// and shifts its working bounds accordingly.
        /// </summary>
        private static void ApplyDelta(
            Guid id,
            float dy,
            float cap,
            Dictionary<Guid, RectangleF> working,
            Dictionary<Guid, float> deltas)
        {
            deltas.TryGetValue(id, out var existing);
            var applied = Clamp(existing + dy, -cap, cap) - existing;
            if (Math.Abs(applied) < 0.01f)
            {
                return;
            }

            deltas[id] = existing + applied;
            var rect = working[id];
            working[id] = new RectangleF(rect.X, rect.Y + applied, rect.Width, rect.Height);
        }

        private static float SegmentY(PointF from, PointF to, float x)
        {
            var spanX = to.X - from.X;
            if (Math.Abs(spanX) < 0.001f)
            {
                return from.Y;
            }

            var t = (x - from.X) / spanX;
            return from.Y + (to.Y - from.Y) * t;
        }

        /// <summary>
        /// Shortest distance between the segment and the rect (0 when they intersect).
        /// Computed in squared space to keep the per-obstacle cost to one sqrt.
        /// </summary>
        private static float SegmentToRectDistance(PointF a, PointF b, RectangleF rect)
        {
            if (rect.Contains(a) || rect.Contains(b))
            {
                return 0f;
            }

            var topLeft = new PointF(rect.Left, rect.Top);
            var topRight = new PointF(rect.Right, rect.Top);
            var bottomLeft = new PointF(rect.Left, rect.Bottom);
            var bottomRight = new PointF(rect.Right, rect.Bottom);

            var dSq = float.MaxValue;
            dSq = Math.Min(dSq, SegmentToSegmentDistanceSq(a, b, topLeft, topRight));
            dSq = Math.Min(dSq, SegmentToSegmentDistanceSq(a, b, bottomLeft, bottomRight));
            dSq = Math.Min(dSq, SegmentToSegmentDistanceSq(a, b, topLeft, bottomLeft));
            dSq = Math.Min(dSq, SegmentToSegmentDistanceSq(a, b, topRight, bottomRight));

            // The wire may pass entirely above or below the rect without touching edges.
            var midX = (a.X + b.X) / 2f;
            if (midX >= rect.Left && midX <= rect.Right)
            {
                var wireY = SegmentY(a, b, midX);
                var dy = wireY < rect.Top ? rect.Top - wireY : wireY - rect.Bottom;
                if (dy > 0f)
                {
                    dSq = Math.Min(dSq, dy * dy);
                }
            }

            return (float)Math.Sqrt(dSq);
        }

        private static float SegmentToSegmentDistanceSq(PointF p1, PointF q1, PointF p2, PointF q2)
        {
            if (SegmentsIntersect(p1, q1, p2, q2))
            {
                return 0f;
            }

            var d = float.MaxValue;
            d = Math.Min(d, PointToSegmentDistanceSq(p1, p2, q2));
            d = Math.Min(d, PointToSegmentDistanceSq(q1, p2, q2));
            d = Math.Min(d, PointToSegmentDistanceSq(p2, p1, q1));
            d = Math.Min(d, PointToSegmentDistanceSq(q2, p1, q1));
            return d;
        }

        private static float PointToSegmentDistanceSq(PointF p, PointF a, PointF b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;
            if (lengthSq < 1e-6f)
            {
                return DistanceSq(p, a);
            }

            var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq;
            t = Clamp(t, 0f, 1f);
            var cx = a.X + dx * t;
            var cy = a.Y + dy * t;
            var ddx = p.X - cx;
            var ddy = p.Y - cy;
            return ddx * ddx + ddy * ddy;
        }

        private static bool SegmentsIntersect(PointF p1, PointF q1, PointF p2, PointF q2)
        {
            var d1 = Cross(p2, q2, p1);
            var d2 = Cross(p2, q2, q1);
            var d3 = Cross(p1, q1, p2);
            var d4 = Cross(p1, q1, q2);
            return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) &&
                   ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
        }

        private static float Cross(PointF a, PointF b, PointF c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static float DistanceSq(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
