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

            // Seed default bounds so the bounds-aware coordinate pass works even without a
            // Grasshopper canvas. Grasshopper consumers refine these with real bounds later.
            foreach (var node in nodes)
            {
                node.Width = options.DefaultNodeWidth;
                node.Height = options.DefaultNodeHeight;
            }

            var islands = IslandDetector.DetectIslands(nodes);
            diagnostics.Add($"Detected {islands.Count} disconnected island(s)");

            // Deterministic island order: largest first, tie-broken by smallest member GUID.
            if (!options.PreserveIslandOrder)
            {
                islands = islands
                    .OrderByDescending(i => i.Count)
                    .ThenBy(i => i.Min(n => n.ComponentId))
                    .ToList();
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
                var realNodes = islandNodes.Where(n => !n.IsDummy).ToList();
                if (realNodes.Count == 0)
                {
                    continue;
                }

                var minX = realNodes.Min(n => n.Pivot.X);
                var minY = realNodes.Min(n => n.Pivot.Y);

                var positions = new Dictionary<Guid, PointF>(realNodes.Count);
                float width = 0f, height = 0f;
                foreach (var node in realNodes)
                {
                    var p = new PointF(node.Pivot.X - minX, node.Pivot.Y - minY);
                    positions[node.ComponentId] = p;
                    width = Math.Max(width, p.X + node.Width);
                    height = Math.Max(height, p.Y + node.Height);
                }

                laidOut.Add(new IslandLayout(
                    realNodes.Select(n => n.ComponentId).ToList(),
                    positions,
                    width,
                    height));
            }

            // Shelf-pack islands left-to-right, wrapping at IslandWrapWidth, to avoid one tall
            // vertical strip when there are many small disconnected groups.
            var allPositions = new Dictionary<Guid, GhJsonPivot>();
            var islandIds = new List<IReadOnlyList<Guid>>();
            float cursorX = 0f, shelfY = 0f, shelfHeight = 0f;
            float originX = options.Origin != null ? (float)options.Origin.X : 0f;
            float originY = options.Origin != null ? (float)options.Origin.Y : 0f;

            foreach (var island in laidOut)
            {
                if (cursorX > 0f && cursorX + island.Width > options.IslandWrapWidth)
                {
                    cursorX = 0f;
                    shelfY += shelfHeight + options.IslandSpacingY;
                    shelfHeight = 0f;
                }

                foreach (var kvp in island.Positions)
                {
                    var p = new PointF(
                        kvp.Value.X + cursorX + originX,
                        kvp.Value.Y + shelfY + originY);
                    allPositions[kvp.Key] = GhJsonPivot.FromPointF(p);
                }

                islandIds.Add(island.RealIds);
                cursorX += island.Width + options.SpacingX;
                shelfHeight = Math.Max(shelfHeight, island.Height);
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
