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
    /// into pixel <see cref="LayoutNode.Pivot"/> coordinates. Column X positions account for
    /// the widest component in each preceding column, and the row unit accounts for the
    /// tallest component, so the raw layout never overlaps even before Grasshopper-specific
    /// bounds refinements run.
    /// </summary>
    internal static class CoordinateAssigner
    {
        public static void AssignCoordinates(List<LayoutNode> nodes, float spacingX, float spacingY)
        {
            if (nodes.Count == 0)
            {
                return;
            }

            // Column X: cumulative offset using the widest node per layer.
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
                columnX[layer] = cursor;
                cursor += maxWidthByLayer[layer] + spacingX;
            }

            // Row unit: uniform band tall enough for the tallest component, so equal Order
            // indices align horizontally across columns and never overlap vertically.
            var maxHeight = nodes.Max(n => n.Height > 0 ? n.Height : 0f);
            var rowUnit = maxHeight + spacingY;

            foreach (var node in nodes)
            {
                var x = columnX.TryGetValue(node.Layer, out var cx) ? cx : node.Layer * spacingX;
                node.Pivot = new PointF(x, node.Order * rowUnit);
            }
        }
    }
}
