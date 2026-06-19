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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.PutOperations
{
    /// <summary>
    /// Utility methods for calculating canvas bounds and positioning.
    /// </summary>
    public static class CanvasBoundsCalculator
    {
        /// <summary>
        /// Represents the bounds of canvas content.
        /// </summary>
        public struct CanvasBounds
        {
            /// <summary>
            /// Lowest Y coordinate (bottom edge) of all objects.
            /// </summary>
            public float LowestY { get; set; }

            /// <summary>
            /// Rightmost X coordinate (right edge) of all objects.
            /// </summary>
            public float RightmostX { get; set; }

            /// <summary>
            /// Topmost Y coordinate (top edge) of all objects.
            /// </summary>
            public float TopmostY { get; set; }

            /// <summary>
            /// Leftmost X coordinate (left edge) of all objects.
            /// </summary>
            public float LeftmostX { get; set; }

            /// <summary>
            /// Whether any objects were found.
            /// </summary>
            public bool IsEmpty { get; set; }
        }

        /// <summary>
        /// Calculates the bounding rectangle of all objects on the canvas.
        /// </summary>
        /// <param name="objects">Canvas objects to analyze.</param>
        /// <returns>Canvas bounds.</returns>
        public static CanvasBounds CalculateBounds(IEnumerable<IGH_DocumentObject> objects)
        {
            var bounds = new CanvasBounds
            {
                LowestY = float.MinValue,
                RightmostX = float.MinValue,
                TopmostY = float.MaxValue,
                LeftmostX = float.MaxValue,
                IsEmpty = true
            };

            foreach (var obj in objects)
            {
                if (obj.Attributes == null) continue;

                bounds.IsEmpty = false;

                float bottomEdge = obj.Attributes.Pivot.Y + obj.Attributes.Bounds.Height;
                if (bottomEdge > bounds.LowestY)
                {
                    bounds.LowestY = bottomEdge;
                }

                float rightEdge = obj.Attributes.Pivot.X + obj.Attributes.Bounds.Width;
                if (rightEdge > bounds.RightmostX)
                {
                    bounds.RightmostX = rightEdge;
                }

                if (obj.Attributes.Pivot.Y < bounds.TopmostY)
                {
                    bounds.TopmostY = obj.Attributes.Pivot.Y;
                }

                if (obj.Attributes.Pivot.X < bounds.LeftmostX)
                {
                    bounds.LeftmostX = obj.Attributes.Pivot.X;
                }
            }

            return bounds;
        }

        /// <summary>
        /// Calculates the starting Y position for placing new content below existing canvas content.
        /// </summary>
        /// <param name="objects">Existing canvas objects.</param>
        /// <param name="spacing">Vertical spacing to add below existing content.</param>
        /// <returns>Y coordinate for new content, or 0 if canvas is empty.</returns>
        public static float CalculateBottomStartY(IEnumerable<IGH_DocumentObject> objects, float spacing)
        {
            var bounds = CalculateBounds(objects);
            return bounds.IsEmpty ? 0 : bounds.LowestY + spacing;
        }
    }
}
