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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using GhJSON.Core.DependencyGraph.Internal;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.GetOperations;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Adapter between the refinement pipeline and <see cref="WireCorridorPlanner"/>: it
    /// translates bounds-center positions, measured node sizes, and live port geometry
    /// into the planner's pure-geometry input, then applies the returned vertical deltas
    /// to node centers. Wires that skip one or more columns gain a clear corridor so they
    /// no longer run through unrelated component bounds.
    /// </summary>
    internal static class WireClearance
    {
        /// <summary>
        /// Moves nodes vertically until every wire spanning two or more columns clears
        /// the bounds of intermediate components by <paramref name="clearance"/> pixels.
        /// Runs per island so each island's column structure is resolved independently.
        /// Positions are bounds centers; the planner's deltas apply directly.
        /// </summary>
        /// <param name="positions">Current bounds-center positions per layout key.</param>
        /// <param name="document">The GhJSON document supplying the connections.</param>
        /// <param name="islands">Island membership from the layout result; null treats all nodes as one island.</param>
        /// <param name="objectProvider">Optional caller-owned object lookup for port geometry.</param>
        /// <param name="sizeProvider">Optional size lookup for off-document objects.</param>
        /// <param name="clearance">Minimum wire-to-bounds distance in pixels.</param>
        public static Dictionary<Guid, PointF> EnsureWireClearance(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document,
            IReadOnlyList<IReadOnlyList<Guid>>? islands,
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Func<Guid, SizeF?>? sizeProvider,
            float clearance)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var ghDocument = CanvasReader.GetActiveDocument();
            if (ghDocument == null && objectProvider == null && sizeProvider == null)
            {
                Debug.WriteLine("[WireClearance.EnsureWireClearance] No document or providers; skipping.");
                return result;
            }

            if (document.Connections == null || clearance <= 0f)
            {
                return result;
            }

            var idToGuidMap = ConnectionKeyMap.Build(document);

            var groups = new List<IReadOnlyList<Guid>>();
            if (islands != null && islands.Count > 0)
            {
                var covered = new HashSet<Guid>();
                foreach (var island in islands)
                {
                    var members = island.Where(result.ContainsKey).ToList();
                    foreach (var id in members)
                    {
                        covered.Add(id);
                    }

                    if (members.Count > 0)
                    {
                        groups.Add(members);
                    }
                }

                var leftovers = result.Keys.Where(id => !covered.Contains(id)).ToList();
                if (leftovers.Count > 0)
                {
                    groups.Add(leftovers);
                }
            }
            else
            {
                groups.Add(result.Keys.ToList());
            }

            foreach (var group in groups)
            {
                var memberSet = new HashSet<Guid>(group);
                var subset = group.ToDictionary(id => id, id => result[id]);

                var columns = PositionClustering.Cluster(subset, p => p.X)
                    .Select(cluster => (IReadOnlyList<Guid>)cluster.Select(kvp => kvp.Key).ToList())
                    .ToList();

                var columnOf = new Dictionary<Guid, int>();
                for (var c = 0; c < columns.Count; c++)
                {
                    foreach (var id in columns[c])
                    {
                        columnOf[id] = c;
                    }
                }

                var nodeBounds = new Dictionary<Guid, RectangleF>();
                foreach (var id in group)
                {
                    if (TryMeasureBounds(id, subset[id], ghDocument, objectProvider, sizeProvider, out var rect))
                    {
                        nodeBounds[id] = rect;
                    }
                }

                var edges = new List<CorridorEdge>();
                foreach (var conn in document.Connections)
                {
                    if (!idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) ||
                        !idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                    {
                        continue;
                    }

                    if (!memberSet.Contains(fromGuid) || !memberSet.Contains(toGuid) ||
                        !columnOf.TryGetValue(fromGuid, out var fromColumn) ||
                        !columnOf.TryGetValue(toGuid, out var toColumn))
                    {
                        continue;
                    }

                    var fromPort = GetOutputPortPoint(
                        ghDocument, objectProvider, sizeProvider, fromGuid, conn.From.ParamIndex, result[fromGuid]);
                    var toPort = GetInputPortPoint(
                        ghDocument, objectProvider, sizeProvider, toGuid, conn.To.ParamIndex, result[toGuid]);
                    if (fromPort == null || toPort == null)
                    {
                        continue;
                    }

                    edges.Add(new CorridorEdge
                    {
                        From = fromGuid,
                        To = toGuid,
                        FromPort = fromPort.Value,
                        ToPort = toPort.Value,
                        FromColumn = fromColumn,
                        ToColumn = toColumn,
                    });
                }

                var deltas = WireCorridorPlanner.PlanVerticalDeltas(nodeBounds, edges, columns, clearance);
                foreach (var kvp in deltas)
                {
                    if (result.TryGetValue(kvp.Key, out var pos))
                    {
                        result[kvp.Key] = new PointF(pos.X, pos.Y + kvp.Value);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The rect a node occupies at its current target center: measured size centered
        /// on <paramref name="center"/>.
        /// </summary>
        private static bool TryMeasureBounds(
            Guid id,
            PointF center,
            GH_Document? ghDocument,
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Func<Guid, SizeF?>? sizeProvider,
            out RectangleF rect)
        {
            rect = default;
            var size = CanvasNodeSizeProvider.Measure(id, ghDocument, objectProvider, sizeProvider);
            if (size == null || size.Value.Width <= 0f || size.Value.Height <= 0f)
            {
                return false;
            }

            rect = new RectangleF(
                center.X - size.Value.Width / 2f,
                center.Y - size.Value.Height / 2f,
                size.Value.Width,
                size.Value.Height);
            return true;
        }

        /// <summary>
        /// Absolute point where the wire leaves the source at its target position: the
        /// right edge of the source bounds at the connected output port's height. Falls
        /// back to the bounds center vertically when the specific port is unknown.
        /// </summary>
        private static PointF? GetOutputPortPoint(
            GH_Document? ghDocument,
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Func<Guid, SizeF?>? sizeProvider,
            Guid sourceGuid,
            int? paramIndex,
            PointF center)
        {
            var size = CanvasNodeSizeProvider.Measure(sourceGuid, ghDocument, objectProvider, sizeProvider);
            if (size == null)
            {
                return null;
            }

            var obj = PortAlignment.ResolveObject(ghDocument, objectProvider, sourceGuid);
            var y = center.Y + PortAlignment.GetOutputPortDelta(obj, paramIndex);
            return new PointF(center.X + size.Value.Width / 2f, y);
        }

        /// <summary>
        /// Absolute point where the wire enters the target at its target position: the
        /// left edge of the target bounds at the connected input port's height.
        /// </summary>
        private static PointF? GetInputPortPoint(
            GH_Document? ghDocument,
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Func<Guid, SizeF?>? sizeProvider,
            Guid targetGuid,
            int? paramIndex,
            PointF center)
        {
            var size = CanvasNodeSizeProvider.Measure(targetGuid, ghDocument, objectProvider, sizeProvider);
            if (size == null)
            {
                return null;
            }

            var obj = PortAlignment.ResolveObject(ghDocument, objectProvider, targetGuid);
            var y = center.Y;
            if (PortAlignment.TryGetInputPortDelta(obj, paramIndex, out var delta))
            {
                y += delta;
            }

            return new PointF(center.X - size.Value.Width / 2f, y);
        }
    }
}
