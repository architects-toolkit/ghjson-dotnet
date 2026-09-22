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
using GhJSON.Grasshopper.GetOperations;
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
        /// Minimum vertical gap left between stacked components in the same column so they do
        /// not sit flush against each other.
        /// </summary>
        private const float VerticalPadding = 20f;

        public static Dictionary<Guid, PointF> AvoidCollisions(
            Dictionary<Guid, PointF> positions,
            Func<Guid, SizeF?>? sizeProvider = null,
            Func<Guid, IGH_DocumentObject?>? objectProvider = null)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var document = CanvasReader.GetActiveDocument();
            if (document == null && sizeProvider == null && objectProvider == null)
            {
                Debug.WriteLine("[CollisionResolver.AvoidCollisions] No active Grasshopper document and no providers; skipping.");
                return result;
            }

            // Positions are component centers (pivots); compare top edges, not centers.
            foreach (var column in PositionClustering.Cluster(positions, p => p.X))
            {
                var sorted = column.OrderBy(kvp => kvp.Value.Y).ToList();
                float lastBottom = float.MinValue;

                foreach (var kvp in sorted)
                {
                    var height = MeasureHeight(kvp.Key, document, sizeProvider, objectProvider);
                    if (height <= 0f)
                    {
                        continue;
                    }
                    var top = kvp.Value.Y - (height / 2f);

                    if (top < lastBottom)
                    {
                        result[kvp.Key] = new PointF(kvp.Value.X, lastBottom + (height / 2f));
                        top = lastBottom;
                    }

                    lastBottom = top + height + VerticalPadding;
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves a node's height through the shared measurement path (providers
        /// first, then live document bounds).
        /// </summary>
        private static float MeasureHeight(Guid id, GH_Document? document, Func<Guid, SizeF?>? sizeProvider, Func<Guid, IGH_DocumentObject?>? objectProvider)
        {
            return CanvasNodeSizeProvider.Measure(id, document, objectProvider, sizeProvider)?.Height ?? 0f;
        }
    }
}
