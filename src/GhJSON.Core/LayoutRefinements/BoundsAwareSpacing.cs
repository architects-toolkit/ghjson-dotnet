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

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Re-spaces layout columns and rows using the real measured bounds of the canvas
    /// objects they contain, so <c>spacingX</c>/<c>spacingY</c> act as true edge-to-edge
    /// gaps even when the core layout ran without live bounds. Positions are component
    /// centers. When <paramref name="islands"/> is provided each island is re-spaced
    /// independently around its own bounds origin, so one island's wide column does not
    /// inflate another island's pitch and their left edges stay aligned.
    /// Degrades to a no-op without a metrics provider.
    /// </summary>
    internal static class BoundsAwareSpacing
    {
        public static Dictionary<Guid, PointF> ApplyBoundsAwareSpacing(
            Dictionary<Guid, PointF> positions,
            float spacingX,
            float spacingY,
            IReadOnlyList<IReadOnlyList<Guid>>? islands = null,
            Func<Guid, LayoutNodeMetrics?>? metricsProvider = null)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            if (metricsProvider == null)
            {
                Debug.WriteLine("[BoundsAwareSpacing.ApplyBoundsAwareSpacing] No metrics provider; skipping.");
                return result;
            }

            if (islands == null || islands.Count == 0)
            {
                RespaceGroup(result, result, metricsProvider, spacingX, spacingY);
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

                RespaceGroup(subset, result, metricsProvider, spacingX, spacingY);
            }

            // Nodes not claimed by any island (should not happen, but stay safe) are
            // re-spaced together as one extra group.
            var leftovers = result.Keys.Where(id => !covered.Contains(id)).ToList();
            if (leftovers.Count > 0)
            {
                var subset = leftovers.ToDictionary(id => id, id => result[id]);
                RespaceGroup(subset, result, metricsProvider, spacingX, spacingY);
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
            Func<Guid, LayoutNodeMetrics?> metricsProvider,
            float spacingX,
            float spacingY)
        {
            if (subset.Count == 0)
            {
                return;
            }

            var leftEdge = subset.Min(kvp =>
                kvp.Value.X - (MeasuredWidth(kvp.Key, metricsProvider) / 2f));
            var topEdge = subset.Min(kvp =>
                kvp.Value.Y - (MeasuredHeight(kvp.Key, metricsProvider) / 2f));

            var columns = PositionClustering.Cluster(subset, p => p.X);
            var columnX = new Dictionary<int, float>();
            var xCursor = leftEdge;
            for (var i = 0; i < columns.Count; i++)
            {
                var maxWidth = MaxMeasuredWidth(columns[i], metricsProvider);
                columnX[i] = xCursor + (maxWidth / 2f);
                xCursor += maxWidth + spacingX;
            }

            var rows = PositionClustering.Cluster(subset, p => p.Y);
            var rowY = new Dictionary<int, float>();
            var yCursor = topEdge;
            for (var i = 0; i < rows.Count; i++)
            {
                var maxHeight = MaxMeasuredHeight(rows[i], metricsProvider);
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

        private static float MeasuredWidth(Guid id, Func<Guid, LayoutNodeMetrics?> metricsProvider)
        {
            return LayoutNodeMetrics.Resolve(metricsProvider, id)?.Size?.Width ?? 0f;
        }

        private static float MeasuredHeight(Guid id, Func<Guid, LayoutNodeMetrics?> metricsProvider)
        {
            return LayoutNodeMetrics.Resolve(metricsProvider, id)?.Size?.Height ?? 0f;
        }

        private static float MaxMeasuredWidth(List<KeyValuePair<Guid, PointF>> cluster, Func<Guid, LayoutNodeMetrics?> metricsProvider)
        {
            var max = 0f;
            foreach (var kvp in cluster)
            {
                var width = MeasuredWidth(kvp.Key, metricsProvider);
                if (width > max)
                {
                    max = width;
                }
            }

            return max;
        }

        private static float MaxMeasuredHeight(List<KeyValuePair<Guid, PointF>> cluster, Func<Guid, LayoutNodeMetrics?> metricsProvider)
        {
            var max = 0f;
            foreach (var kvp in cluster)
            {
                var height = MeasuredHeight(kvp.Key, metricsProvider);
                if (height > max)
                {
                    max = height;
                }
            }

            return max;
        }
    }
}
