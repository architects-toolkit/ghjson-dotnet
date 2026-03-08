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

namespace GhJSON.Core.DependencyGraph.Internal.Sugiyama
{
    internal static class CrossingMinimizer
    {
        public static void MinimizeCrossings(List<LayoutNode> nodes)
        {
            bool changed;
            do
            {
                var oldY = nodes.ToDictionary(n => n.ComponentId, n => n.Pivot.Y);
                ApplySinglePass(nodes);
                changed = nodes.Any(n => Math.Abs(n.Pivot.Y - oldY[n.ComponentId]) > 0.001f);
            }
            while (changed);
        }

        private static void ApplySinglePass(List<LayoutNode> nodes)
        {
            var byLayer = nodes.GroupBy(n => (int)n.Pivot.X)
                               .OrderBy(g => g.Key)
                               .Select(g => g.ToList())
                               .ToList();

            for (int layerIndex = 1; layerIndex < byLayer.Count; layerIndex++)
            {
                var prevLayer = byLayer[layerIndex - 1];
                var currLayer = byLayer[layerIndex];
                currLayer.Sort((a, b) => CalculateMedian(a, prevLayer, useParents: true)
                                      .CompareTo(CalculateMedian(b, prevLayer, useParents: true)));
                for (int i = 0; i < currLayer.Count; i++)
                {
                    currLayer[i].Pivot = new PointF(currLayer[i].Pivot.X, i);
                }
            }

            for (int layerIndex = byLayer.Count - 2; layerIndex >= 0; layerIndex--)
            {
                var nextLayer = byLayer[layerIndex + 1];
                var currLayer = byLayer[layerIndex];
                currLayer.Sort((a, b) => CalculateMedian(a, nextLayer, useParents: false)
                                      .CompareTo(CalculateMedian(b, nextLayer, useParents: false)));
                for (int i = 0; i < currLayer.Count; i++)
                {
                    currLayer[i].Pivot = new PointF(currLayer[i].Pivot.X, i);
                }
            }
        }

        private static float CalculateMedian(LayoutNode node, List<LayoutNode> adjacentLayer, bool useParents)
        {
            var connected = useParents ? node.Parents.Keys : node.Children.Keys;
            var positions = new List<float>();
            foreach (var id in connected)
            {
                var found = adjacentLayer.FirstOrDefault(n => n.ComponentId == id);
                if (found != null)
                {
                    positions.Add(found.Pivot.Y);
                }
            }

            if (!positions.Any())
            {
                return float.MaxValue;
            }

            positions.Sort();
            int mid = positions.Count / 2;
            if (positions.Count % 2 == 1)
            {
                return positions[mid];
            }

            return (positions[mid - 1] + positions[mid]) / 2f;
        }
    }
}
