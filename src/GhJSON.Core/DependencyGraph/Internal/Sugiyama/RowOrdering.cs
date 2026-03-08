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
    internal static class RowOrdering
    {
        public static void AssignRows(List<LayoutNode> nodes)
        {
            var byLayer = nodes.GroupBy(n => (int)n.Pivot.X)
                               .OrderBy(g => g.Key)
                               .Select(g => g.ToList())
                               .ToList();

            for (int layerIndex = 0; layerIndex < byLayer.Count; layerIndex++)
            {
                var currentLayer = byLayer[layerIndex].ToList();
                if (layerIndex == 0)
                {
                    currentLayer.Sort((a, b) =>
                    {
                        float aOut = a.Children.Any() ? (float)a.Children.Values.Average() : float.MaxValue;
                        float bOut = b.Children.Any() ? (float)b.Children.Values.Average() : float.MaxValue;
                        return aOut.CompareTo(bOut);
                    });
                }
                else
                {
                    SortLayerByBarycenter(currentLayer, byLayer[layerIndex - 1].ToList(), useParents: false);
                }

                for (int i = 0; i < currentLayer.Count; i++)
                {
                    currentLayer[i].Pivot = new PointF(currentLayer[i].Pivot.X, i);
                }
            }

            for (int layerIndex = byLayer.Count - 2; layerIndex >= 0; layerIndex--)
            {
                var currentLayer = byLayer[layerIndex].ToList();
                SortLayerByBarycenter(currentLayer, byLayer[layerIndex + 1].ToList(), useParents: true);
                for (int i = 0; i < currentLayer.Count; i++)
                {
                    currentLayer[i].Pivot = new PointF(currentLayer[i].Pivot.X, i);
                }
            }
        }

        private static void SortLayerByBarycenter(List<LayoutNode> currentLayer,
            List<LayoutNode> adjacentLayer, bool useParents)
        {
            currentLayer.Sort((a, b) =>
            {
                float aKey = CalculateBarycenter(a, adjacentLayer, useParents);
                float bKey = CalculateBarycenter(b, adjacentLayer, useParents);
                return aKey.CompareTo(bKey);
            });
        }

        private static float CalculateBarycenter(LayoutNode node,
            List<LayoutNode> adjacentLayer, bool useParents)
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

            return positions.Any() ? (float)positions.Average() : float.MaxValue;
        }
    }
}
