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

            var islands = IslandDetector.DetectIslands(nodes);
            diagnostics.Add($"Detected {islands.Count} disconnected island(s)");

            var allPositions = new Dictionary<Guid, GhJsonPivot>();
            var islandIds = new List<IReadOnlyList<Guid>>();
            float currentYOffset = 0f;

            foreach (var island in islands)
            {
                var islandNodes = new List<LayoutNode>(island);
                
                switch (options.Algorithm)
                {
                    case LayoutAlgorithm.Sugiyama:
                        ApplySugiyamaLayout(islandNodes, options);
                        break;
                    default:
                        throw new NotSupportedException($"Layout algorithm {options.Algorithm} is not supported");
                }

                foreach (var node in islandNodes)
                {
                    node.Pivot = new PointF(node.Pivot.X, node.Pivot.Y + currentYOffset);
                }

                var minX = islandNodes.Min(n => n.Pivot.X);
                var minY = islandNodes.Min(n => n.Pivot.Y);
                foreach (var node in islandNodes)
                {
                    var offsetPivot = new PointF(node.Pivot.X - minX, node.Pivot.Y - minY);
                    if (options.Origin != null)
                    {
                        offsetPivot = new PointF(
                            offsetPivot.X + (float)options.Origin.X,
                            offsetPivot.Y + (float)options.Origin.Y);
                    }

                    allPositions[node.ComponentId] = GhJsonPivot.FromPointF(offsetPivot);
                }

                islandIds.Add(islandNodes.Select(n => n.ComponentId).ToList());

                var maxY = islandNodes.Max(n => n.Pivot.Y);
                currentYOffset = maxY + options.IslandSpacingY;
            }

            return new LayoutResult(allPositions, islandIds, diagnostics);
        }

        private static void ApplySugiyamaLayout(List<LayoutNode> nodes, LayoutOptions options)
        {
            LayerAssignment.AssignLayers(nodes);
            nodes = EdgeConcentration.InsertHubNodes(nodes);
            RowOrdering.AssignRows(nodes);
            CrossingMinimizer.MinimizeCrossings(nodes);
            CoordinateAssigner.AssignCoordinates(nodes, options.SpacingX, options.SpacingY);
        }
    }
}
