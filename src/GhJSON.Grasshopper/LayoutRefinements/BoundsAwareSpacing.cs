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
using GhJSON.Grasshopper.GetOperations;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Re-spaces layout columns and rows using the real measured bounds of the canvas
    /// objects they contain, so <c>spacingX</c>/<c>spacingY</c> act as true edge-to-edge
    /// gaps even when the core layout ran without live bounds. Positions are component
    /// centers (pivots). Degrades to a no-op without an active document.
    /// </summary>
    internal static class BoundsAwareSpacing
    {
        public static Dictionary<Guid, PointF> ApplyBoundsAwareSpacing(
            Dictionary<Guid, PointF> positions,
            float spacingX,
            float spacingY)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var document = CanvasReader.GetActiveDocument();
            if (document == null)
            {
                Debug.WriteLine("[BoundsAwareSpacing.ApplyBoundsAwareSpacing] No active Grasshopper document; skipping.");
                return result;
            }

            var columnX = new Dictionary<int, float>();
            var rowY = new Dictionary<int, float>();

            var columns = PositionClustering.Cluster(positions, p => p.X);
            var xCursor = 0f;
            for (var i = 0; i < columns.Count; i++)
            {
                var maxWidth = MaxMeasuredWidth(columns[i], document);
                columnX[i] = xCursor + (maxWidth / 2f);
                xCursor += maxWidth + spacingX;
            }

            var rows = PositionClustering.Cluster(positions, p => p.Y);
            var yCursor = 0f;
            for (var i = 0; i < rows.Count; i++)
            {
                var maxHeight = MaxMeasuredHeight(rows[i], document);
                rowY[i] = yCursor + (maxHeight / 2f);
                yCursor += maxHeight + spacingY;
            }

            for (var i = 0; i < columns.Count; i++)
            {
                foreach (var kvp in columns[i])
                {
                    if (result.TryGetValue(kvp.Key, out var pos))
                    {
                        result[kvp.Key] = new PointF(columnX[i], pos.Y);
                    }
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                foreach (var kvp in rows[i])
                {
                    if (result.TryGetValue(kvp.Key, out var pos))
                    {
                        result[kvp.Key] = new PointF(pos.X, rowY[i]);
                    }
                }
            }

            return result;
        }

        private static float MaxMeasuredWidth(List<KeyValuePair<Guid, PointF>> cluster, GH_Document document)
        {
            var max = 0f;
            foreach (var kvp in cluster)
            {
                var bounds = document.FindObject(kvp.Key, false)?.Attributes?.Bounds;
                if (bounds.HasValue && bounds.Value.Width > max)
                {
                    max = bounds.Value.Width;
                }
            }

            return max;
        }

        private static float MaxMeasuredHeight(List<KeyValuePair<Guid, PointF>> cluster, GH_Document document)
        {
            var max = 0f;
            foreach (var kvp in cluster)
            {
                var bounds = document.FindObject(kvp.Key, false)?.Attributes?.Bounds;
                if (bounds.HasValue && bounds.Value.Height > max)
                {
                    max = bounds.Value.Height;
                }
            }

            return max;
        }
    }
}
