/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
using System.Linq;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;

namespace GhJSON.Core.Operations.MergeOperations
{
    /// <summary>
    /// Adjusts component positions to avoid overlap during merge operations.
    /// </summary>
    public class PositionAdjuster
    {
        private readonly float _offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionAdjuster"/> class.
        /// </summary>
        /// <param name="offset">The offset to apply when adjusting positions.</param>
        public PositionAdjuster(float offset = 300f)
        {
            _offset = offset;
        }

        /// <summary>
        /// Adjusts a component's position by offsetting from the max X.
        /// </summary>
        /// <param name="component">The component to adjust.</param>
        /// <param name="maxX">The maximum X coordinate in the target document.</param>
        public void AdjustPosition(ComponentProperties component, float maxX)
        {
            if (component.Pivot == null)
                return;

            component.Pivot = new CompactPosition(
                component.Pivot.X + maxX + _offset,
                component.Pivot.Y);
        }

        /// <summary>
        /// Adjusts all components' positions by a fixed offset.
        /// </summary>
        /// <param name="components">The components to adjust.</param>
        /// <param name="offsetX">X offset to apply.</param>
        /// <param name="offsetY">Y offset to apply.</param>
        public void AdjustPositions(IEnumerable<ComponentProperties> components, float offsetX, float offsetY)
        {
            foreach (var component in components.Where(c => c.Pivot != null))
            {
                component.Pivot = new CompactPosition(
                    component.Pivot!.X + offsetX,
                    component.Pivot.Y + offsetY);
            }
        }

        /// <summary>
        /// Calculates the bounding box of components.
        /// </summary>
        /// <param name="document">The document containing components.</param>
        /// <returns>Tuple of (minX, minY, maxX, maxY) or null if no components have positions.</returns>
        public (float MinX, float MinY, float MaxX, float MaxY)? GetBoundingBox(GhJsonDocument document)
        {
            if (document?.Components == null || !document.Components.Any(c => c.Pivot != null))
                return null;

            var componentsWithPivot = document.Components.Where(c => c.Pivot != null).ToList();
            
            return (
                componentsWithPivot.Min(c => c.Pivot!.X),
                componentsWithPivot.Min(c => c.Pivot!.Y),
                componentsWithPivot.Max(c => c.Pivot!.X),
                componentsWithPivot.Max(c => c.Pivot!.Y)
            );
        }
    }
}
