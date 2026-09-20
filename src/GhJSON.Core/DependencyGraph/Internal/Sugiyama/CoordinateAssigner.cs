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
            // Used as the fallback slot position for nodes without resolvable parents.
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

            // Port-slot Y assignment: sweep layers left to right and place each node so its
            // input ports land on their connected parents' output port rows
            // (pivotY = parentPortY − inputPortCenterOffset). Within a layer the clamp keeps
            // the order chosen by the crossing minimizer and guarantees no vertical overlap.
            var byId = nodes.ToDictionary(n => n.ComponentId, n => n);
            foreach (var layerGroup in nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key))
            {
                var x = columnX.TryGetValue(layerGroup.Key, out var cx) ? cx : layerGroup.Key * spacingX;
                var prevBottom = float.MinValue;
                var prevIsDummy = false;

                foreach (var node in layerGroup.OrderBy(n => n.Order))
                {
                    var desired = new List<float>();
                    foreach (var kv in node.Parents)
                    {
                        if (!byId.TryGetValue(kv.Key, out var parent) || parent.Layer >= node.Layer)
                        {
                            continue; // Unresolved or backward (cycle) edge.
                        }

                        var outIndex = parent.Children.TryGetValue(node.ComponentId, out var oi) ? oi : -1;
                        desired.Add(
                            parent.Pivot.Y + (parent.OutputPortCenterOffset(outIndex) * parent.Height)
                            - (node.InputPortCenterOffset(kv.Value) * node.Height));
                    }

                    var y = desired.Count > 0
                        ? Median(desired)
                        : rowCenterY.TryGetValue(node.Order, out var cy) ? cy : node.Order * spacingY;

                    // Dummy routing nodes are invisible, so they need no gap at all; this
                    // keeps long-edge port rows from drifting away from their true slot.
                    var gap = node.IsDummy || prevIsDummy ? 0f : spacingY;
                    var minY = prevBottom + gap + (node.Height / 2f);
                    if (y < minY)
                    {
                        y = minY;
                    }

                    node.Pivot = new PointF(x, y);
                    prevBottom = y + (node.Height / 2f);
                    prevIsDummy = node.IsDummy;
                }
            }
        }

        private static float Median(List<float> values)
        {
            values.Sort();
            var mid = values.Count / 2;
            return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2f;
        }
    }
}
