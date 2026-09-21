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
using GhJSON.Core.DependencyGraph.Internal;
using GhJSON.Core.DependencyGraph.Internal.Sugiyama;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DependencyGraph
{
    public static class LayoutEngine
    {
        public static LayoutResult CalculateLayout(GhJsonDocument document, LayoutOptions? options = null)
        {
            options ??= LayoutOptions.Default;
            var diagnostics = new List<string>();

            var nodes = GraphBuilder.BuildGraph(document);
            if (nodes.Count == 0)
            {
                diagnostics.Add("No components found in document");
                return new LayoutResult(
                    new Dictionary<Guid, GhJsonPivot>(),
                    new List<List<Guid>>(),
                    diagnostics);
            }

            // Seed node bounds: real measured sizes when the caller supplies a
            // NodeSizeProvider (e.g. live canvas bounds), otherwise the defaults so the
            // bounds-aware coordinate pass works even without a Grasshopper canvas.
            var sizeProvider = options.NodeSizeProvider;
            foreach (var node in nodes)
            {
                var size = GetNodeSize(sizeProvider, node.ComponentId);
                node.Width = size?.Width > 0f ? size.Value.Width : options.DefaultNodeWidth;
                node.Height = size?.Height > 0f ? size.Value.Height : options.DefaultNodeHeight;
            }

            // Capture original pivots before layout overwrites them: island ordering
            // follows the canvas reading order (top-to-bottom, left-to-right) so a
            // tidied arrangement keeps each island roughly where the user had it.
            var originalPivots = nodes.ToDictionary(n => n.ComponentId, n => n.Pivot);
            var hasOriginalPositions = nodes.Any(n => n.Pivot != PointF.Empty);

            var islands = IslandDetector.DetectIslands(nodes);
            diagnostics.Add($"Detected {islands.Count} disconnected island(s)");

            if (!options.PreserveIslandOrder)
            {
                if (hasOriginalPositions)
                {
                    // Reading order: sort each island by the topmost-then-leftmost
                    // original pivot of its members. Deterministic and spatially
                    // faithful to the incoming document.
                    islands = islands
                        .OrderBy(i => i.Min(n => originalPivots[n.ComponentId].Y))
                        .ThenBy(i => i.Min(n => originalPivots[n.ComponentId].X))
                        .ToList();
                }
                else
                {
                    // No original positions to read: deterministic order, largest
                    // island first, tie-broken by smallest member GUID.
                    islands = islands
                        .OrderByDescending(i => i.Count)
                        .ThenBy(i => i.Min(n => n.ComponentId))
                        .ToList();
                }
            }

            var laidOut = new List<IslandLayout>();
            var totalCrossings = 0;
            var cycleCount = 0;

            foreach (var island in islands)
            {
                var islandNodes = new List<LayoutNode>(island);

                switch (options.Algorithm)
                {
                    case LayoutAlgorithm.Sugiyama:
                        islandNodes = ApplySugiyamaLayout(islandNodes, options, out var cycles);
                        cycleCount += cycles;
                        break;
                    default:
                        throw new NotSupportedException($"Layout algorithm {options.Algorithm} is not supported");
                }

                totalCrossings += CrossingMinimizer.CountCrossings(islandNodes);

                // Normalize each island to its own origin (0,0) using only real nodes.
                // Positions are bounds centers, so the island origin is the top-left
                // of the island's bounding box — not the leftmost/topmost center — to
                // make island edges line up when they are stacked.
                var realNodes = islandNodes.Where(n => !n.IsDummy).ToList();
                if (realNodes.Count == 0)
                {
                    continue;
                }

                var minLeft = realNodes.Min(n => n.Pivot.X - (n.Width / 2f));
                var minTop = realNodes.Min(n => n.Pivot.Y - (n.Height / 2f));

                var positions = new Dictionary<Guid, PointF>(realNodes.Count);
                float width = 0f, height = 0f;
                foreach (var node in realNodes)
                {
                    var p = new PointF(node.Pivot.X - minLeft, node.Pivot.Y - minTop);
                    positions[node.ComponentId] = p;
                    width = Math.Max(width, p.X + (node.Width / 2f));
                    height = Math.Max(height, p.Y + (node.Height / 2f));
                }

                laidOut.Add(new IslandLayout(
                    realNodes.Select(n => n.ComponentId).ToList(),
                    positions,
                    width,
                    height));
            }

            // Stack islands vertically with a shared left edge. Island origins are
            // bounding-box top-lefts, so stacking at the same X keeps every island's
            // left edge aligned instead of drifting diagonally.
            var allPositions = new Dictionary<Guid, GhJsonPivot>();
            var islandIds = new List<IReadOnlyList<Guid>>();
            float originX = options.Origin != null ? (float)options.Origin.X : 0f;
            float cursorY = options.Origin != null ? (float)options.Origin.Y : 0f;

            foreach (var island in laidOut)
            {
                foreach (var kvp in island.Positions)
                {
                    var p = new PointF(
                        kvp.Value.X + originX,
                        kvp.Value.Y + cursorY);
                    allPositions[kvp.Key] = GhJsonPivot.FromPointF(p);
                }

                islandIds.Add(island.RealIds);
                cursorY += island.Height + options.IslandSpacingY;
            }

            if (cycleCount > 0)
            {
                diagnostics.Add(
                    $"Detected {cycleCount} component(s) participating in dependency cycles; back edges excluded from ranking.");
            }

            diagnostics.Add($"Total edge crossings: {totalCrossings}");
            diagnostics.Add($"Total wire length: {ComputeWireLength(document, allPositions):F0}");

            return new LayoutResult(allPositions, islandIds, diagnostics);
        }

        private static List<LayoutNode> ApplySugiyamaLayout(List<LayoutNode> nodes, LayoutOptions options, out int cycleCount)
        {
            LayerAssignment.AssignLayers(nodes, out var cycleNodes);
            cycleCount = cycleNodes.Count;

            nodes = EdgeConcentration.InsertDummyChains(nodes, options);
            RowOrdering.AssignInitialOrder(nodes);
            CrossingMinimizer.MinimizeCrossings(nodes, options.MaxOrderingIterations);
            CoordinateAssigner.AssignCoordinates(nodes, options.SpacingX, options.SpacingY);
            return nodes;
        }

        /// <summary>
        /// Queries <paramref name="provider"/> for a node's measured size, returning null on
        /// any failure so a throwing provider degrades to the configured defaults.
        /// </summary>
        private static SizeF? GetNodeSize(Func<Guid, SizeF?>? provider, Guid componentId)
        {
            if (provider == null)
            {
                return null;
            }

            try
            {
                return provider(componentId);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sums the Euclidean length of every connection using the final real-node positions.
        /// A lower total indicates a tighter, more readable layout.
        /// </summary>
        private static double ComputeWireLength(GhJsonDocument document, Dictionary<Guid, GhJsonPivot> positions)
        {
            if (document.Connections == null)
            {
                return 0d;
            }

            var idToKey = new Dictionary<int, Guid>();
            foreach (var c in document.Components)
            {
                if (c.Id.HasValue)
                {
                    idToKey[c.Id.Value] = GraphBuilder.GetStableKey(c);
                }
            }

            var total = 0d;
            foreach (var conn in document.Connections)
            {
                if (idToKey.TryGetValue(conn.From.Id, out var fromKey) &&
                    idToKey.TryGetValue(conn.To.Id, out var toKey) &&
                    positions.TryGetValue(fromKey, out var from) &&
                    positions.TryGetValue(toKey, out var to))
                {
                    var dx = from.X - to.X;
                    var dy = from.Y - to.Y;
                    total += Math.Sqrt((dx * dx) + (dy * dy));
                }
            }

            return total;
        }

        private sealed class IslandLayout
        {
            public IslandLayout(
                IReadOnlyList<Guid> realIds,
                Dictionary<Guid, PointF> positions,
                float width,
                float height)
            {
                this.RealIds = realIds;
                this.Positions = positions;
                this.Width = width;
                this.Height = height;
            }

            public IReadOnlyList<Guid> RealIds { get; }

            public Dictionary<Guid, PointF> Positions { get; }

            public float Width { get; }

            public float Height { get; }
        }
    }
}
