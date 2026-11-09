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
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for component pivot (position) property.
    /// </summary>
    internal sealed class PivotHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 1000;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj.Attributes != null;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (component.Pivot == null && obj.Attributes != null)
            {
                component.Pivot = GhJsonPivot.FromPointF(obj.Attributes.Pivot);
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (component.Pivot != null && obj.Attributes != null)
            {
                var boundsBefore = obj.Attributes.Bounds;
                var pivotBefore = obj.Attributes.Pivot;
                var newPivot = component.Pivot.ToPointF();
                obj.Attributes.Pivot = newPivot;

                // Some attribute types (floating parameters such as Param_Number) keep
                // Bounds independent of Pivot: assigning the pivot leaves the stored
                // rectangle where it was, so bounds-center measurements on off-canvas
                // objects resolve to the stale location instead of the authored one.
                // Re-anchor the stored bounds with the object's own pivot-to-center
                // offset, which is consistent on a freshly instantiated object.
                var boundsAfter = obj.Attributes.Bounds;
                if (boundsBefore.Width > 0f && boundsBefore.Height > 0f &&
                    (newPivot.X != pivotBefore.X || newPivot.Y != pivotBefore.Y) &&
                    boundsAfter.Location == boundsBefore.Location)
                {
                    var centerOffsetX = (boundsBefore.Left + (boundsBefore.Width / 2f)) - pivotBefore.X;
                    var centerOffsetY = (boundsBefore.Top + (boundsBefore.Height / 2f)) - pivotBefore.Y;
                    obj.Attributes.Bounds = new RectangleF(
                        (newPivot.X + centerOffsetX) - (boundsBefore.Width / 2f),
                        (newPivot.Y + centerOffsetY) - (boundsBefore.Height / 2f),
                        boundsBefore.Width,
                        boundsBefore.Height);
                }
            }
        }
    }
}
