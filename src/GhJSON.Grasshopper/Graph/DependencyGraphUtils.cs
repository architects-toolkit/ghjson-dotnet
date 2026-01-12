/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using GhJSON.Core.Models.Document;
using GhJSON.Grasshopper.Canvas;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Graph
{
    /// <summary>
    /// Utilities to build and lay out a dependency graph of Grasshopper components and parameters.
    /// Provides algorithms to compute layers, minimize crossings, align parameters, and generate
    /// a consistent grid of nodes for canvas placement.
    /// </summary>
    public static class DependencyGraphUtils
    {
        /// <summary>
        /// Layout components and produce a unified grid of NodeGridComponent.
        /// </summary>
        /// <param name="doc">The GhJSON document containing components and connections.</param>
        /// <param name="force">If true, forces a full layout recalculation even when pivots exist.</param>
        /// <param name="spacingX">Horizontal spacing between component columns.</param>
        /// <param name="spacingY">Vertical spacing between grid rows.</param>
        /// <param name="islandSpacingY">Vertical spacing between disconnected islands.</param>
        /// <returns>List of NodeGridComponent entries for each component.</returns>
        public static List<NodeGridComponent> CreateComponentGrid(
            GhJsonDocument doc,
            bool force = false,
            float spacingX = 50f,
            float spacingY = 80f,
            float islandSpacingY = 80f)
        {
            Debug.WriteLine("[CreateComponentGrid] Initializing unified grid...");

            // Initialize grid
            var grid = InitializeGrid(doc);

            // Return if pivots are already provided and force is not set
            if (!force)
            {
                if (grid.All(n => n.Pivot != PointF.Empty))
                {
                    var minX = grid.Min(n => n.Pivot.X);
                    var minY = grid.Min(n => n.Pivot.Y);
                    foreach (var n in grid)
                        n.Pivot = UnifyCenterPivot(n.ComponentId, new PointF(n.Pivot.X - minX, n.Pivot.Y - minY));

                    return grid;
                }
            }

            // Island detection: split into connected components by parent/child links
            var idToNode = grid.ToDictionary(n => n.ComponentId, n => n);
            var visited = new HashSet<Guid>();
            var islands = new List<List<NodeGridComponent>>();
            foreach (var node in grid)
            {
                if (visited.Contains(node.ComponentId)) continue;
                var stack = new Stack<Guid>();
                stack.Push(node.ComponentId);
                visited.Add(node.ComponentId);
                var island = new List<NodeGridComponent>();
                while (stack.Count > 0)
                {
                    var id = stack.Pop();
                    var n = idToNode[id];
                    island.Add(n);
                    foreach (var neighbor in n.Children.Keys.Concat(n.Parents.Keys))
                    {
                        if (!visited.Contains(neighbor) && idToNode.ContainsKey(neighbor))
                        {
                            visited.Add(neighbor);
                            stack.Push(neighbor);
                        }
                    }
                }

                islands.Add(island);
            }

            Debug.WriteLine($"[CreateComponentGrid] Found {islands.Count} islands");

            // Layout each island and stack vertically
            var result = new List<NodeGridComponent>();
            float currentYOffset = 0f;
            foreach (var island in islands)
            {
                var sub = SugiyamaAlgorithm(new List<NodeGridComponent>(island));
                sub = ApplySpacing(sub, spacingX, spacingY);

                foreach (var n in sub)
                    n.Pivot = new PointF(n.Pivot.X, n.Pivot.Y + currentYOffset);
                result.AddRange(sub);

                result = MinimizeLayerConnections(result);
                result = OneToOneConnections(result, spacingY);
                result = AlignParamsToInputs(result, spacingY);
                result = AvoidCollisions(result);

                var maxY = sub.Max(n => n.Pivot.Y);
                currentYOffset = maxY + islandSpacingY;
            }

            DebugDumpGrid("Result", result);

            return result;
        }

        private static List<NodeGridComponent> InitializeGrid(GhJsonDocument doc)
        {
            var grid = doc.Components.Select(c => new NodeGridComponent
            {
                ComponentId = c.InstanceGuid ?? Guid.Empty,
                Pivot = c.Pivot,
                Parents = new Dictionary<Guid, int>(),
                Children = new Dictionary<Guid, int>(),
            }).ToList();

            var idToGuidMap = doc.GetIdToGuidMapping();
            foreach (var conn in doc.Connections)
            {
                if (conn.TryResolveGuids(idToGuidMap, out var fromGuid, out var toGuid) &&
                    grid.Any(n => n.ComponentId == toGuid) && grid.Any(n => n.ComponentId == fromGuid))
                {
                    var toNode = grid.First(n => n.ComponentId == toGuid);
                    var fromNode = grid.First(n => n.ComponentId == fromGuid);

                    int inputIndex = -1;
                    if (CanvasUtilities.FindInstance(toNode.ComponentId) is IGH_Component childComp)
                    {
                        var inputs = ParameterAccess.GetAllInputs(childComp);
                        inputIndex = inputs.FindIndex(p => p.NickName == conn.To.ParamName || p.Name == conn.To.ParamName);
                    }

                    toNode.Parents[fromGuid] = inputIndex;

                    int outputIndex = -1;
                    if (CanvasUtilities.FindInstance(fromNode.ComponentId) is IGH_Component parentComp)
                    {
                        var outputs = ParameterAccess.GetAllOutputs(parentComp);
                        outputIndex = outputs.FindIndex(p => p.NickName == conn.From.ParamName || p.Name == conn.From.ParamName);
                    }

                    fromNode.Children[toGuid] = outputIndex;
                }
            }

            return grid;
        }

        private static List<NodeGridComponent> SugiyamaAlgorithm(List<NodeGridComponent> grid)
        {
            Debug.WriteLine($"[SugiyamaAlgorithm] Step 1: Compute layers");
            grid = ComputeLayers(grid);

            Debug.WriteLine($"[SugiyamaAlgorithm] Step 2: Edge concentration");
            grid = EdgeConcentration(grid);

            Debug.WriteLine($"[SugiyamaAlgorithm] Step 3: Compute rows");
            grid = ComputeRows(grid);

            Debug.WriteLine($"[SugiyamaAlgorithm] Step 4: Minimize edge crossings");
            grid = MinimizeEdgeCrossings(grid);

            Debug.WriteLine($"[SugiyamaAlgorithm] Step 5: Multi-layer sweep");
            grid = MultiLayerSweep(grid);

            return grid;
        }

        private static List<NodeGridComponent> ComputeLayers(List<NodeGridComponent> grid)
        {
            var graph = grid.ToDictionary(n => n.ComponentId, n => n.Children);
            var layers = new Dictionary<Guid, int>();
            int Dfs(Guid n)
            {
                if (layers.TryGetValue(n, out var v)) return v;
                var children = graph[n];
                var layer = children.Count == 0 ? 0 : children.Select(child => Dfs(child.Key)).Max() + 1;
                layers[n] = layer;
                return layer;
            }

            foreach (var n in graph.Keys) Dfs(n);
            var maxLayer = layers.Values.DefaultIfEmpty(0).Max();
            foreach (var n in grid)
            {
                if (layers.TryGetValue(n.ComponentId, out var layer))
                {
                    n.Pivot = new PointF(maxLayer - layer, n.Pivot.Y);
                }
            }

            return grid;
        }

        private static List<NodeGridComponent> EdgeConcentration(List<NodeGridComponent> grid)
        {
            var newGrid = new List<NodeGridComponent>(grid);
            var byLayer = grid.GroupBy(n => n.Pivot.X).OrderBy(g => g.Key).ToList();

            for (int li = 0; li < byLayer.Count - 1; li++)
            {
                var left = byLayer[li].ToList();
                var rightIds = new HashSet<Guid>(byLayer[li + 1].Select(n => n.ComponentId));

                var groups = left.Select(n => new
                {
                    Node = n,
                    Targets = n.Children.Keys.Where(id => rightIds.Contains(id)).OrderBy(id => id).ToList(),
                })
                .GroupBy(x => string.Join(",", x.Targets))
                .Where(g => g.Count() > 1 && g.First().Targets.Count > 1);

                foreach (var grp in groups)
                {
                    var S = grp.Select(x => x.Node).ToList();
                    var T = grp.First().Targets;

                    var ec = new NodeGridComponent
                    {
                        ComponentId = Guid.NewGuid(),
                        Pivot = new PointF(left[0].Pivot.X + 0.5f, 0),
                        Parents = new Dictionary<Guid, int>(),
                        Children = new Dictionary<Guid, int>(),
                    };

                    foreach (var s in S)
                    {
                        foreach (var t in T)
                        {
                            s.Children.Remove(t);
                        }
                    }

                    foreach (var t in newGrid.Where(n => T.Contains(n.ComponentId)))
                    {
                        foreach (var s in S)
                        {
                            t.Parents.Remove(s.ComponentId);
                        }
                    }

                    foreach (var s in S)
                    {
                        s.Children[ec.ComponentId] = -1;
                        ec.Parents[s.ComponentId] = -1;
                    }

                    foreach (var t in newGrid.Where(n => T.Contains(n.ComponentId)))
                    {
                        t.Parents[ec.ComponentId] = -1;
                        ec.Children[t.ComponentId] = -1;
                    }

                    newGrid.Add(ec);
                }
            }

            return newGrid;
        }

        private static List<NodeGridComponent> ComputeRows(List<NodeGridComponent> grid)
        {
            var byLayer = grid.GroupBy(n => (int)n.Pivot.X)
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
                    currentLayer[i].Pivot = new PointF(currentLayer[i].Pivot.X, i);
            }

            for (int layerIndex = byLayer.Count - 2; layerIndex >= 0; layerIndex--)
            {
                var currentLayer = byLayer[layerIndex].ToList();
                SortLayerByBarycenter(currentLayer, byLayer[layerIndex + 1].ToList(), useParents: true);
                for (int i = 0; i < currentLayer.Count; i++)
                    currentLayer[i].Pivot = new PointF(currentLayer[i].Pivot.X, i);
            }

            return grid;
        }

        private static void SortLayerByBarycenter(List<NodeGridComponent> currentLayer,
            List<NodeGridComponent> adjacentLayer, bool useParents)
        {
            currentLayer.Sort((a, b) =>
            {
                float aKey = CalculateBarycenter(a, adjacentLayer, useParents);
                float bKey = CalculateBarycenter(b, adjacentLayer, useParents);
                return aKey.CompareTo(bKey);
            });
        }

        private static float CalculateBarycenter(NodeGridComponent node,
            List<NodeGridComponent> adjacentLayer, bool useParents)
        {
            var connected = useParents ? node.Parents.Keys : node.Children.Keys;
            var positions = new List<float>();
            foreach (var id in connected)
            {
                var found = adjacentLayer.FirstOrDefault(n => n.ComponentId == id);
                if (found != null) positions.Add(found.Pivot.Y);
            }

            return positions.Any() ? (float)positions.Average() : float.MaxValue;
        }

        private static List<NodeGridComponent> MinimizeEdgeCrossings(List<NodeGridComponent> grid)
        {
            var byLayer = grid.GroupBy(n => (int)n.Pivot.X)
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
                    currLayer[i].Pivot = new PointF(currLayer[i].Pivot.X, i);
            }

            for (int layerIndex = byLayer.Count - 2; layerIndex >= 0; layerIndex--)
            {
                var nextLayer = byLayer[layerIndex + 1];
                var currLayer = byLayer[layerIndex];
                currLayer.Sort((a, b) => CalculateMedian(a, nextLayer, useParents: false)
                                      .CompareTo(CalculateMedian(b, nextLayer, useParents: false)));
                for (int i = 0; i < currLayer.Count; i++)
                    currLayer[i].Pivot = new PointF(currLayer[i].Pivot.X, i);
            }

            return grid;
        }

        private static float CalculateMedian(NodeGridComponent node, List<NodeGridComponent> adjacentLayer, bool useParents)
        {
            var connected = useParents ? node.Parents.Keys : node.Children.Keys;
            var positions = new List<float>();
            foreach (var id in connected)
            {
                var found = adjacentLayer.FirstOrDefault(n => n.ComponentId == id);
                if (found != null)
                    positions.Add(found.Pivot.Y);
            }

            if (!positions.Any())
                return float.MaxValue;
            positions.Sort();
            int mid = positions.Count / 2;
            if (positions.Count % 2 == 1)
                return positions[mid];
            return (positions[mid - 1] + positions[mid]) / 2f;
        }

        private static List<NodeGridComponent> MultiLayerSweep(List<NodeGridComponent> grid)
        {
            bool changed;
            do
            {
                var oldY = grid.ToDictionary(n => n.ComponentId, n => n.Pivot.Y);
                grid = MinimizeEdgeCrossings(grid);
                changed = grid.Any(n => n.Pivot.Y != oldY[n.ComponentId]);
                Debug.WriteLine($"[MultiLayerSweep] Changing pivot for {grid.Count(n => n.Pivot.Y != oldY[n.ComponentId])} nodes");
            }
            while (changed);

            return grid;
        }

        private static List<NodeGridComponent> ApplySpacing(List<NodeGridComponent> grid, float spacingX, float spacingY)
        {
            foreach (var n in grid)
                n.Pivot = new PointF(n.Pivot.X * spacingX, n.Pivot.Y * spacingY);

            var columns = grid.GroupBy(n => (int)(n.Pivot.X / spacingX)).OrderBy(g => g.Key);
            var colOffsets = new Dictionary<int, float>();
            float xOffset = 0;
            foreach (var group in columns)
            {
                float maxWidth = group.Max(n => CanvasUtilities.GetComponentBounds(n.ComponentId).Width);
                if (maxWidth <= 0) maxWidth = 100;
                colOffsets[group.Key] = xOffset;
                xOffset += maxWidth + spacingX;
            }

            var rows = grid.GroupBy(n => (int)(n.Pivot.Y / spacingY)).OrderBy(g => g.Key);
            var rowOffsets = new Dictionary<int, float>();
            float yOffset = 0;
            foreach (var group in rows)
            {
                float maxHeight = group.Max(n => CanvasUtilities.GetComponentBounds(n.ComponentId).Height);
                if (maxHeight <= 0) maxHeight = 50;
                rowOffsets[group.Key] = yOffset;
                yOffset += maxHeight + spacingY;
            }

            foreach (var n in grid)
            {
                int col = (int)(n.Pivot.X / spacingX);
                int row = (int)(n.Pivot.Y / spacingY);
                if (colOffsets.ContainsKey(col) && rowOffsets.ContainsKey(row))
                    n.Pivot = new PointF(colOffsets[col], rowOffsets[row]);
            }

            return grid;
        }

        private static List<NodeGridComponent> AlignParamsToInputs(List<NodeGridComponent> grid, float spacingY)
        {
            spacingY = spacingY / 2;
            Debug.WriteLine($"[AlignParamsToInputs] Starting alignment with spacingY={spacingY}");

            var byColumn = grid.GroupBy(n => n.Pivot.X)
                                .OrderBy(g => g.Key)
                                .Select(g => g.ToList())
                                .ToList();

            for (int i = 1; i < byColumn.Count; i++)
            {
                Debug.WriteLine($"[AlignParamsToInputs] Processing column {i}, X={byColumn[i].First().Pivot.X}");
                var prevCol = byColumn[i - 1];
                var currCol = byColumn[i];
                foreach (var child in currCol)
                {
                    Debug.WriteLine($"[AlignParamsToInputs] Child {child.ComponentId} at Y={child.Pivot.Y}");

                    var parents = prevCol.Where(p => p.Children.ContainsKey(child.ComponentId)).ToList();

                    if (parents.Count > 1
                        && CanvasUtilities.FindInstance(child.ComponentId) is IGH_Component childComp
                        && childComp.Params.Input.Count == parents.Count
                        && parents.All(p => CanvasUtilities.FindInstance(p.ComponentId) is IGH_Param))
                    {
                        var inputs = ParameterAccess.GetAllInputs(childComp);
                        foreach (var p in parents.OrderBy(p => child.Parents[p.ComponentId]))
                        {
                            int inputIdx = child.Parents[p.ComponentId];
                            if (inputIdx >= 0 && inputIdx < inputs.Count)
                            {
                                var rect = inputs[inputIdx].Attributes.Bounds;
                                float inputPivotY = rect.Y + rect.Height / 2f;
                                var canvasChildBounds = CanvasUtilities.GetComponentBounds(child.ComponentId);
                                float canvasChildCenterY = canvasChildBounds.Y + canvasChildBounds.Height / 2f;
                                float deltaCanvasY = inputPivotY - canvasChildCenterY;
                                float targetY = child.Pivot.Y + deltaCanvasY;
                                Debug.WriteLine($"[AlignParamsToInputs] Param-case: aligning parent {p.ComponentId} relativeGridY={targetY}");
                                p.Pivot = new PointF(p.Pivot.X, targetY);
                            }
                        }
                    }
                }
            }

            return grid;
        }

        private static List<NodeGridComponent> MinimizeLayerConnections(List<NodeGridComponent> grid)
        {
            var byLayer = grid.GroupBy(n => n.Pivot.X).OrderBy(g => g.Key).ToList();
            var idToNode = grid.ToDictionary(n => n.ComponentId, n => n);

            for (int i = 0; i < byLayer.Count - 1; i++)
            {
                var currLayer = byLayer[i].ToList();
                var nextLayer = byLayer[i + 1].ToList();
                var deltas = new List<float>();

                foreach (var u in currLayer)
                {
                    foreach (var childId in u.Children.Keys)
                    {
                        if (idToNode.TryGetValue(childId, out var v) &&
                            Math.Abs(v.Pivot.X - nextLayer[0].Pivot.X) < 0.001f)
                        {
                            deltas.Add(u.Pivot.Y - v.Pivot.Y);
                        }
                    }
                }

                if (deltas.Count == 0) continue;

                var avgDelta = deltas.Sum() / deltas.Count;

                foreach (var v in nextLayer)
                    v.Pivot = new PointF(v.Pivot.X, v.Pivot.Y + avgDelta);
            }

            return grid;
        }

        private static List<NodeGridComponent> OneToOneConnections(List<NodeGridComponent> grid, float spacingY)
        {
            var idToNode = grid.ToDictionary(n => n.ComponentId, n => n);
            Debug.WriteLine("[OneToOneConnections] Aligning single-child parents");
            foreach (var parent in grid.Where(n => n.Children.Count == 1))
            {
                var childId = parent.Children.Keys.First();
                if (!idToNode.TryGetValue(childId, out var child)) continue;
                int inputIndex = child.Parents[parent.ComponentId];
                if (inputIndex < 0) continue;

                if (!(CanvasUtilities.FindInstance(child.ComponentId) is IGH_Component childComp)) continue;
                var inputs = ParameterAccess.GetAllInputs(childComp);
                if (inputIndex >= inputs.Count) continue;
                var port = inputs[inputIndex];
                var rect = port.Attributes.Bounds;

                float inputPivotY = rect.Y + rect.Height / 2f;
                var canvasChildBounds = CanvasUtilities.GetComponentBounds(child.ComponentId);
                float canvasChildCenterY = canvasChildBounds.Y + canvasChildBounds.Height / 2f;
                float deltaCanvasY = inputPivotY - canvasChildCenterY;

                float targetY = child.Pivot.Y + deltaCanvasY + spacingY / 2;
                Debug.WriteLine($"[OneToOneConnections] Align parent {parent.ComponentId} to Y={targetY}");
                parent.Pivot = new PointF(parent.Pivot.X, targetY);
            }

            return grid;
        }

        private static List<NodeGridComponent> AvoidCollisions(List<NodeGridComponent> grid)
        {
            var byColumn = grid.GroupBy(n => n.Pivot.X).OrderBy(g => g.Key);
            foreach (var col in byColumn)
            {
                var sorted = col.OrderBy(n => n.Pivot.Y).ToList();
                float lastBottom = float.MinValue;
                foreach (var node in sorted)
                {
                    var bounds = CanvasUtilities.GetComponentBounds(node.ComponentId);
                    if (node.Pivot.Y < lastBottom)
                        node.Pivot = new PointF(node.Pivot.X, lastBottom);
                    lastBottom = node.Pivot.Y + (bounds.Height > 0 ? bounds.Height : 50);
                }
            }

            return grid;
        }

        private static void DebugDumpGrid(string stage, List<NodeGridComponent> grid)
        {
            Debug.WriteLine($"[DebugDumpGrid:{stage}] Dumping {grid.Count} nodes");
            foreach (var n in grid)
                Debug.WriteLine($"[DebugDumpGrid:{stage}] {n.ComponentId} => Pivot=({n.Pivot.X},{n.Pivot.Y})");
        }

        private static PointF UnifyCenterPivot(Guid id, PointF pivot)
        {
            var obj = CanvasUtilities.FindInstance(id);
            if (obj is IGH_Param)
            {
                var bounds = CanvasUtilities.GetComponentBounds(id);
                if (!bounds.IsEmpty)
                {
                    pivot = new PointF(
                        pivot.X - bounds.Width / 2f,
                        pivot.Y - bounds.Height / 2f);
                }
            }

            return pivot;
        }
    }

    /// <summary>
    /// Represents a node in the component grid for layout purposes.
    /// </summary>
    public class NodeGridComponent
    {
        /// <summary>
        /// Gets or sets the component's instance GUID.
        /// </summary>
        public Guid ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the pivot position for the component.
        /// </summary>
        public PointF Pivot { get; set; }

        /// <summary>
        /// Gets or sets the parent components mapped to their input parameter indices.
        /// </summary>
        public Dictionary<Guid, int> Parents { get; set; } = new Dictionary<Guid, int>();

        /// <summary>
        /// Gets or sets the child components mapped to their output parameter indices.
        /// </summary>
        public Dictionary<Guid, int> Children { get; set; } = new Dictionary<Guid, int>();
    }
}
