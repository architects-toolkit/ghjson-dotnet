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
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    internal static class BoundsAwareSpacing
    {
        public static Dictionary<Guid, PointF> ApplyBoundsAwareSpacing(
            Dictionary<Guid, PointF> positions,
            float spacingX,
            float spacingY)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var byLayer = positions.GroupBy(kvp => (int)kvp.Value.X)
                                   .OrderBy(g => g.Key)
                                   .ToList();

            var colOffsets = new Dictionary<int, float>();
            float xOffset = 0;

            foreach (var layer in byLayer)
            {
                float maxWidth = 0;
                foreach (var kvp in layer)
                {
                    var obj = Instances.ActiveCanvas?.Document?.FindObject(kvp.Key, false);
                    if (obj?.Attributes?.Bounds != null)
                    {
                        maxWidth = Math.Max(maxWidth, obj.Attributes.Bounds.Width);
                    }
                }

                colOffsets[layer.Key] = xOffset;
                xOffset += maxWidth + spacingX;
            }

            var byRow = positions.GroupBy(kvp => (int)kvp.Value.Y)
                                 .OrderBy(g => g.Key)
                                 .ToList();

            var rowOffsets = new Dictionary<int, float>();
            float yOffset = 0;

            foreach (var row in byRow)
            {
                float maxHeight = 0;
                foreach (var kvp in row)
                {
                    var obj = Instances.ActiveCanvas?.Document?.FindObject(kvp.Key, false);
                    if (obj?.Attributes?.Bounds != null)
                    {
                        maxHeight = Math.Max(maxHeight, obj.Attributes.Bounds.Height);
                    }
                }

                rowOffsets[row.Key] = yOffset;
                yOffset += maxHeight + spacingY;
            }

            foreach (var kvp in positions.ToList())
            {
                int col = (int)kvp.Value.X;
                int row = (int)kvp.Value.Y;

                if (colOffsets.TryGetValue(col, out var x) && rowOffsets.TryGetValue(row, out var y))
                {
                    result[kvp.Key] = new PointF(x, y);
                }
            }

            return result;
        }
    }
}
