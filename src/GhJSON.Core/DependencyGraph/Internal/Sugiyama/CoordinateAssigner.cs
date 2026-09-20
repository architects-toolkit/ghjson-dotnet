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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GhJSON.Core.DependencyGraph.Internal.Sugiyama
{
    /// <summary>
    /// Converts integer <see cref="LayoutNode.Layer"/> / <see cref="LayoutNode.Order"/> ranks
    /// into pixel <see cref="LayoutNode.Pivot"/> coordinates (component centers). Each column
    /// is a band as wide as its widest node and each row a band as tall as its tallest node,
    /// so <paramref name="spacingX"/>/<paramref name="spacingY"/> act as true edge-to-edge
    /// gaps and a single tall component only inflates its own row.
    /// </summary>
    internal static class CoordinateAssigner
    {
        public static void AssignCoordinates(List<LayoutNode> nodes, float spacingX, float spacingY)
        {
            if (nodes.Count == 0)
            {
                return;
            }

            // Column X: cumulative offset using the widest node per layer. The pivot is the
            // component center, so the pivot X sits at the middle of the column band.
            var maxWidthByLayer = new Dictionary<int, float>();
            foreach (var node in nodes)
            {
                var w = node.Width > 0 ? node.Width : 0f;
                if (!maxWidthByLayer.TryGetValue(node.Layer, out var existing) || w > existing)
                {
                    maxWidthByLayer[node.Layer] = w;
                }
            }

            var columnX = new Dictionary<int, float>();
            var cursor = 0f;
            foreach (var layer in maxWidthByLayer.Keys.OrderBy(k => k))
            {
                columnX[layer] = cursor + (maxWidthByLayer[layer] / 2f);
                cursor += maxWidthByLayer[layer] + spacingX;
            }

            // Row bands: per-row tallest node across all layers, stacked top to bottom.
            var rowHeight = new Dictionary<int, float>();
            foreach (var node in nodes)
            {
                var h = node.Height > 0 ? node.Height : 0f;
                if (!rowHeight.TryGetValue(node.Order, out var existing) || h > existing)
                {
                    rowHeight[node.Order] = h;
                }
            }

            var rowCenterY = new Dictionary<int, float>();
            var yCursor = 0f;
            foreach (var row in rowHeight.Keys.OrderBy(k => k))
            {
                rowCenterY[row] = yCursor + (rowHeight[row] / 2f);
                yCursor += rowHeight[row] + spacingY;
            }

            foreach (var node in nodes)
            {
                var x = columnX.TryGetValue(node.Layer, out var cx) ? cx : node.Layer * spacingX;
                var y = rowCenterY.TryGetValue(node.Order, out var cy) ? cy : node.Order * spacingY;
                node.Pivot = new PointF(x, y);
            }
        }
    }
}
