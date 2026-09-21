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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Re-spaces layout columns and rows using the real measured bounds of the canvas
    /// objects they contain, so <c>spacingX</c>/<c>spacingY</c> act as true edge-to-edge
    /// gaps even when the core layout ran without live bounds. Positions are component
    /// centers. When <paramref name="islands"/> is provided each island is re-spaced
    /// independently around its own bounds origin, so one island's wide column does not
    /// inflate another island's pitch and their left edges stay aligned.
    /// Degrades to a no-op without an active document.
    /// </summary>
    internal static class BoundsAwareSpacing
    {
        public static Dictionary<Guid, PointF> ApplyBoundsAwareSpacing(
            Dictionary<Guid, PointF> positions,
            float spacingX,
            float spacingY,
            IReadOnlyList<IReadOnlyList<Guid>>? islands = null,
            Func<Guid, SizeF?>? sizeProvider = null)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var document = CanvasReader.GetActiveDocument();
            if (document == null && sizeProvider == null)
            {
                Debug.WriteLine("[BoundsAwareSpacing.ApplyBoundsAwareSpacing] No active Grasshopper document and no size provider; skipping.");
                return result;
            }

            if (islands == null || islands.Count == 0)
            {
                RespaceGroup(result, result, document, sizeProvider, spacingX, spacingY);
                return result;
            }

            var covered = new HashSet<Guid>();
            foreach (var island in islands)
            {
                var subset = island
                    .Where(result.ContainsKey)
                    .ToDictionary(id => id, id => result[id]);
                foreach (var id in subset.Keys)
                {
                    covered.Add(id);
                }

                RespaceGroup(subset, result, document, sizeProvider, spacingX, spacingY);
            }

            // Nodes not claimed by any island (should not happen, but stay safe) are
            // re-spaced together as one extra group.
            var leftovers = result.Keys.Where(id => !covered.Contains(id)).ToList();
            if (leftovers.Count > 0)
            {
                var subset = leftovers.ToDictionary(id => id, id => result[id]);
                RespaceGroup(subset, result, document, sizeProvider, spacingX, spacingY);
            }

            return result;
        }

        /// <summary>
        /// Re-spaces one group's columns and rows in place, keeping the group's bounds
        /// top-left edge where it was so island packing done upstream is preserved.
        /// </summary>
        private static void RespaceGroup(
            IReadOnlyDictionary<Guid, PointF> subset,
            Dictionary<Guid, PointF> result,
            GH_Document? document,
            Func<Guid, SizeF?>? sizeProvider,
            float spacingX,
            float spacingY)
        {
            if (subset.Count == 0)
            {
                return;
            }

            var leftEdge = subset.Min(kvp =>
                kvp.Value.X - (MeasuredWidth(kvp.Key, document, sizeProvider) / 2f));
            var topEdge = subset.Min(kvp =>
                kvp.Value.Y - (MeasuredHeight(kvp.Key, document, sizeProvider) / 2f));

            var columns = PositionClustering.Cluster(subset, p => p.X);
            var columnX = new Dictionary<int, float>();
            var xCursor = leftEdge;
            for (var i = 0; i < columns.Count; i++)
            {
                var maxWidth = MaxMeasuredWidth(columns[i], document, sizeProvider);
                columnX[i] = xCursor + (maxWidth / 2f);
                xCursor += maxWidth + spacingX;
            }

            var rows = PositionClustering.Cluster(subset, p => p.Y);
            var rowY = new Dictionary<int, float>();
            var yCursor = topEdge;
            for (var i = 0; i < rows.Count; i++)
            {
                var maxHeight = MaxMeasuredHeight(rows[i], document, sizeProvider);
                rowY[i] = yCursor + (maxHeight / 2f);
                yCursor += maxHeight + spacingY;
            }

            for (var i = 0; i < columns.Count; i++)
            {
                foreach (var kvp in columns[i])
                {
                    result[kvp.Key] = new PointF(columnX[i], kvp.Value.Y);
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                foreach (var kvp in rows[i])
                {
                    result[kvp.Key] = new PointF(result[kvp.Key].X, rowY[i]);
                }
            }
        }

        private static float MeasuredWidth(Guid id, GH_Document? document, Func<Guid, SizeF?>? sizeProvider)
        {
            return Measure(id, document, sizeProvider)?.Width ?? 0f;
        }

        private static float MeasuredHeight(Guid id, GH_Document? document, Func<Guid, SizeF?>? sizeProvider)
        {
            return Measure(id, document, sizeProvider)?.Height ?? 0f;
        }

        /// <summary>
        /// Resolves a node's size, preferring the supplied provider (freshly instantiated
        /// objects not yet on the canvas) and falling back to live document bounds.
        /// </summary>
        private static SizeF? Measure(Guid id, GH_Document? document, Func<Guid, SizeF?>? sizeProvider)
        {
            if (sizeProvider != null)
            {
                try
                {
                    if (sizeProvider(id) is SizeF provided)
                    {
                        return provided;
                    }
                }
                catch
                {
                    // Provider failure falls through to live bounds.
                }
            }

            return document?.FindObject(id, false)?.Attributes?.Bounds.Size;
        }

        private static float MaxMeasuredWidth(List<KeyValuePair<Guid, PointF>> cluster, GH_Document? document, Func<Guid, SizeF?>? sizeProvider)
        {
            var max = 0f;
            foreach (var kvp in cluster)
            {
                var width = MeasuredWidth(kvp.Key, document, sizeProvider);
                if (width > max)
                {
                    max = width;
                }
            }

            return max;
        }

        private static float MaxMeasuredHeight(List<KeyValuePair<Guid, PointF>> cluster, GH_Document? document, Func<Guid, SizeF?>? sizeProvider)
        {
            var max = 0f;
            foreach (var kvp in cluster)
            {
                var height = MeasuredHeight(kvp.Key, document, sizeProvider);
                if (height > max)
                {
                    max = height;
                }
            }

            return max;
        }
    }
}
