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

using System.Drawing;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Shared
{
    /// <summary>
    /// Converts between the layout engine's canonical coordinate space (component bounds
    /// centers) and Grasshopper's per-object pivot semantics.
    /// </summary>
    /// <remarks>
    /// The dependency-graph layout and all layout refinements work in a uniform space
    /// where a position is the center of the object's rendered bounds. Grasshopper,
    /// however, interprets <c>IGH_Attributes.Pivot</c> differently per object type:
    /// regular components pivot around their bounds center, while number sliders,
    /// panels, and floating parameters pivot at their top-left corner. Writing a
    /// center position straight into <c>Pivot</c> therefore shifts non-component
    /// objects down and right by half their size, which shows up as top-aligned
    /// panels next to center-aligned components and inconsistent gaps. These helpers
    /// perform the conversion using each object's own pivot-to-bounds offset, so no
    /// per-type switch is required.
    /// </remarks>
    public static class PivotSemantics
    {
        /// <summary>
        /// Converts a desired bounds-center position into the pivot value that places
        /// the object's bounds center at that position.
        /// </summary>
        /// <param name="obj">The document object being positioned.</param>
        /// <param name="center">Desired center of the object's bounds, in canvas coordinates.</param>
        /// <returns>
        /// The pivot to assign to <c>obj.Attributes.Pivot</c>. Falls back to
        /// <paramref name="center"/> unchanged when the object has no measurable bounds
        /// (attributes missing or not laid out yet).
        /// </returns>
        public static PointF CenterToPivot(IGH_DocumentObject obj, PointF center)
        {
            var bounds = obj?.Attributes?.Bounds;
            if (!bounds.HasValue || bounds.Value.Width <= 0f || bounds.Value.Height <= 0f)
            {
                return center;
            }

            var boundsCenter = BoundsCenter(bounds.Value);
            var pivot = obj!.Attributes!.Pivot;
            return new PointF(
                center.X + (pivot.X - boundsCenter.X),
                center.Y + (pivot.Y - boundsCenter.Y));
        }

        /// <summary>
        /// Returns the center of an object's current bounds, which is the canonical
        /// position used by the layout engine.
        /// </summary>
        /// <param name="obj">The document object to measure.</param>
        /// <returns>The bounds center, or the object's pivot when bounds are unavailable.</returns>
        public static PointF BoundsCenter(IGH_DocumentObject obj)
        {
            var bounds = obj?.Attributes?.Bounds;
            if (!bounds.HasValue || bounds.Value.Width <= 0f || bounds.Value.Height <= 0f)
            {
                return obj?.Attributes?.Pivot ?? PointF.Empty;
            }

            return BoundsCenter(bounds.Value);
        }

        /// <summary>
        /// Returns the center of a bounds rectangle.
        /// </summary>
        public static PointF BoundsCenter(RectangleF bounds)
        {
            return new PointF(bounds.Left + (bounds.Width / 2f), bounds.Top + (bounds.Height / 2f));
        }
    }
}
