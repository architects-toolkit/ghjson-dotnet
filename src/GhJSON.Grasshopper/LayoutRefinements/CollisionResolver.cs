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
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Post-layout refinements that depend on the active Grasshopper canvas for
    /// per-component bounds. When no canvas is available (e.g. headless tests) these
    /// methods degrade into no-ops and emit a single diagnostic, rather than silently
    /// mutating positions based on stale state.
    /// </summary>
    internal static class CollisionResolver
    {
        /// <summary>
        /// Rounds X-coordinates to a stable bucket so that float drift introduced by
        /// previous refinement passes does not fragment the column grouping.
        /// </summary>
        private const int ColumnRoundDigits = 3;

        public static Dictionary<Guid, PointF> AvoidCollisions(Dictionary<Guid, PointF> positions)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var document = Instances.ActiveCanvas?.Document;
            if (document == null)
            {
                Debug.WriteLine("[CollisionResolver.AvoidCollisions] No active Grasshopper document; skipping.");
                return result;
            }

            var byColumn = positions
                .GroupBy(kvp => (float)Math.Round(kvp.Value.X, ColumnRoundDigits))
                .OrderBy(g => g.Key);

            foreach (var col in byColumn)
            {
                var sorted = col.OrderBy(kvp => kvp.Value.Y).ToList();
                float lastBottom = float.MinValue;

                foreach (var kvp in sorted)
                {
                    var obj = document.FindObject(kvp.Key, false);
                    if (obj?.Attributes?.Bounds == null)
                    {
                        continue;
                    }

                    var bounds = obj.Attributes.Bounds;
                    var currentY = kvp.Value.Y;

                    if (currentY < lastBottom)
                    {
                        result[kvp.Key] = new PointF(kvp.Value.X, lastBottom);
                        lastBottom = lastBottom + bounds.Height;
                    }
                    else
                    {
                        lastBottom = currentY + bounds.Height;
                    }
                }
            }

            return result;
        }

        public static Dictionary<Guid, PointF> MinimizeConnectionLengths(
            Dictionary<Guid, PointF> positions,
            GhJSON.Core.SchemaModels.GhJsonDocument document)
        {
            var result = new Dictionary<Guid, PointF>(positions);
            var idToGuidMap = document.GetIdToGuidMapping();

            var byLayer = positions.GroupBy(kvp => kvp.Value.X).OrderBy(g => g.Key).ToList();

            for (int i = 0; i < byLayer.Count - 1; i++)
            {
                var currLayer = byLayer[i].ToList();
                var nextLayer = byLayer[i + 1].ToList();
                var nextLayerX = nextLayer.First().Value.X;
                var deltas = new List<float>();

                if (document.Connections != null)
                {
                    foreach (var conn in document.Connections)
                    {
                        if (idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) &&
                            idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                        {
                            var fromPos = currLayer.FirstOrDefault(kvp => kvp.Key == fromGuid);
                            var toPos = nextLayer.FirstOrDefault(kvp => kvp.Key == toGuid);

                            if (fromPos.Key != Guid.Empty && toPos.Key != Guid.Empty &&
                                Math.Abs(toPos.Value.X - nextLayerX) < 0.001f)
                            {
                                deltas.Add(fromPos.Value.Y - toPos.Value.Y);
                            }
                        }
                    }
                }

                if (deltas.Count == 0)
                {
                    continue;
                }

                var avgDelta = deltas.Sum() / deltas.Count;

                foreach (var kvp in nextLayer)
                {
                    result[kvp.Key] = new PointF(kvp.Value.X, kvp.Value.Y + avgDelta);
                }
            }

            return result;
        }
    }
}
