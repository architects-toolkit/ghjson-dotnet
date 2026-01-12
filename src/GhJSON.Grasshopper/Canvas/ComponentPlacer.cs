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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Document;
using GhJSON.Grasshopper.Graph;
using GhJSON.Grasshopper.Serialization;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Handles placement of deserialized components onto the Grasshopper canvas.
    /// Manages positioning, adding to document, and canvas updates.
    /// </summary>
    public static class ComponentPlacer
    {
        /// <summary>
        /// Places components on the canvas using a deserialization result.
        /// </summary>
        /// <param name="result">Deserialization result with components</param>
        /// <param name="document">GhJSON document with position information</param>
        /// <param name="guidMapping">Dictionary mapping GUIDs to component instances</param>
        /// <param name="startPosition">Starting position for placement (null for auto)</param>
        /// <param name="spacing">Spacing between components when using auto-layout</param>
        /// <param name="useExactPositions">When true, uses exact pivot positions from GhJSON without offset</param>
        /// <param name="useDependencyLayout">When true and no pivots exist, uses dependency-based layout instead of simple grid</param>
        /// <returns>List of component names that were placed</returns>
        public static List<string> PlaceComponents(
            DeserializationResult result,
            GhJsonDocument? document,
            Dictionary<Guid, IGH_DocumentObject>? guidMapping,
            PointF? startPosition = null,
            int spacing = 100,
            bool useExactPositions = false,
            bool useDependencyLayout = false)
        {
            if (result.Components.Count == 0)
            {
                Debug.WriteLine("[ComponentPlacer] No components to place");
                return new List<string>();
            }

            var placedNames = new List<string>();
            var ghDocument = Instances.ActiveCanvas?.Document;

            if (ghDocument == null)
            {
                Debug.WriteLine("[ComponentPlacer] No active Grasshopper document");
                return placedNames;
            }

            guidMapping ??= new Dictionary<Guid, IGH_DocumentObject>();
            var componentProps = document?.Components;
            bool hasPivots = componentProps != null && componentProps.Any(p => !p.Pivot.IsEmpty);

            // Calculate positions based on whether pivots exist in GhJSON
            if (hasPivots)
            {
                if (useExactPositions)
                {
                    // Use exact positions from GhJSON without any offset
                    ApplyOffsettedPositions(result.Components, componentProps!, guidMapping, PointF.Empty);
                    Debug.WriteLine("[ComponentPlacer] Applied exact pivot positions (no offset)");
                }
                else
                {
                    // Pivots exist: offset them to prevent overlap with existing components
                    var offset = CalculatePivotOffset(componentProps!, startPosition);
                    ApplyOffsettedPositions(result.Components, componentProps!, guidMapping, offset);
                    Debug.WriteLine($"[ComponentPlacer] Applied pivot offset: ({offset.X}, {offset.Y})");
                }
            }
            else if (useDependencyLayout && document != null)
            {
                // No pivots: use dependency-based layout (Sugiyama algorithm)
                var gridStartPosition = startPosition ?? CanvasUtilities.CalculateStartPoint(spacing);
                ApplyDependencyLayout(result.Components, document, guidMapping, gridStartPosition, spacing);
                Debug.WriteLine($"[ComponentPlacer] Applied dependency layout starting at ({gridStartPosition.X}, {gridStartPosition.Y})");
            }
            else
            {
                // No pivots: use simple grid layout
                var gridStartPosition = startPosition ?? CanvasUtilities.CalculateStartPoint(spacing);
                ApplySimpleGridLayout(result.Components, gridStartPosition, spacing);
                Debug.WriteLine($"[ComponentPlacer] Applied grid layout starting at ({gridStartPosition.X}, {gridStartPosition.Y})");
            }

            // Add components to canvas
            foreach (var component in result.Components)
            {
                try
                {
                    var compPosition = component.Attributes.Pivot;
                    CanvasUtilities.AddObjectToCanvas(component, compPosition, redraw: false);
                    placedNames.Add(component.Name);

                    Debug.WriteLine($"[ComponentPlacer] Placed component '{component.Name}' at ({compPosition.X}, {compPosition.Y})");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ComponentPlacer] Error placing component '{component.Name}': {ex.Message}");
                }
            }

            // Redraw canvas once at the end
            if (placedNames.Count > 0)
            {
                Instances.ActiveCanvas?.Refresh();
            }

            return placedNames;
        }

        /// <summary>
        /// Calculates the offset to apply to pivots from GhJSON to prevent overlap.
        /// </summary>
        private static PointF CalculatePivotOffset(
            List<ComponentProperties> componentProps,
            PointF? startPosition)
        {
            if (startPosition.HasValue)
            {
                var maxY = componentProps.Where(p => !p.Pivot.IsEmpty).Max(p => ((PointF)p.Pivot).Y);
                return new PointF(startPosition.Value.X, startPosition.Value.Y + maxY);
            }

            // Find the lowest Y position on the current canvas
            var currentObjects = CanvasUtilities.GetCurrentObjects();
            float lowestY = 0f;

            if (currentObjects.Any())
            {
                lowestY = currentObjects.Max(o => o.Attributes.Pivot.Y + o.Attributes.Bounds.Height);
                lowestY += 100f; // Add spacing buffer
            }

            // Calculate offset to move the top of the new components to lowestY
            var minComponentY = componentProps.Where(p => !p.Pivot.IsEmpty).Min(p => ((PointF)p.Pivot).Y);
            return new PointF(0, lowestY - minComponentY);
        }

        /// <summary>
        /// Applies offsetted positions from GhJSON to component instances.
        /// </summary>
        private static void ApplyOffsettedPositions(
            List<IGH_DocumentObject> components,
            List<ComponentProperties> componentProps,
            Dictionary<Guid, IGH_DocumentObject> guidMapping,
            PointF offset)
        {
            foreach (var props in componentProps)
            {
                if (!props.Pivot.IsEmpty && props.InstanceGuid.HasValue && 
                    guidMapping.TryGetValue(props.InstanceGuid.Value, out var instance))
                {
                    var originalPivot = (PointF)props.Pivot;
                    var newPivot = new PointF(originalPivot.X + offset.X, originalPivot.Y + offset.Y);
                    instance.Attributes.Pivot = newPivot;
                    Debug.WriteLine($"[ComponentPlacer] Set position for '{instance.Name}' to ({newPivot.X}, {newPivot.Y})");
                }
            }
        }

        /// <summary>
        /// Applies a simple grid layout when no positions are defined.
        /// </summary>
        private static void ApplySimpleGridLayout(
            List<IGH_DocumentObject> components,
            PointF startPosition,
            int spacing)
        {
            float currentX = startPosition.X;
            float currentY = startPosition.Y;
            float maxHeight = 0;
            int columnCount = 0;
            const int maxColumns = 5;

            foreach (var component in components)
            {
                component.CreateAttributes();
                component.Attributes.Pivot = new PointF(currentX, currentY);

                float componentWidth = component.Attributes.Bounds.Width;
                float componentHeight = component.Attributes.Bounds.Height;

                maxHeight = Math.Max(maxHeight, componentHeight);
                currentX += componentWidth + spacing;
                columnCount++;

                // Move to next row after maxColumns
                if (columnCount >= maxColumns)
                {
                    currentX = startPosition.X;
                    currentY += maxHeight + spacing;
                    maxHeight = 0;
                    columnCount = 0;
                }
            }
        }

        /// <summary>
        /// Applies dependency-based layout using the Sugiyama algorithm.
        /// </summary>
        private static void ApplyDependencyLayout(
            List<IGH_DocumentObject> components,
            GhJsonDocument document,
            Dictionary<Guid, IGH_DocumentObject> guidMapping,
            PointF startPosition,
            int spacing)
        {
            try
            {
                // Use DependencyGraphUtils to calculate optimal positions
                var layoutNodes = DependencyGraphUtils.CreateComponentGrid(document, force: true, spacingX: spacing, spacingY: spacing);

                // Apply calculated positions to components
                foreach (var node in layoutNodes)
                {
                    if (guidMapping.TryGetValue(node.ComponentId, out var instance))
                    {
                        var newPivot = new PointF(
                            startPosition.X + node.Pivot.X,
                            startPosition.Y + node.Pivot.Y);
                        instance.Attributes.Pivot = newPivot;
                        Debug.WriteLine($"[ComponentPlacer] Dependency layout: '{instance.Name}' at ({newPivot.X}, {newPivot.Y})");
                    }
                }

                // Handle any components not in layout (fallback to simple positioning)
                var layoutGuids = new HashSet<Guid>(layoutNodes.Select(n => n.ComponentId));
                float fallbackY = layoutNodes.Any() ? layoutNodes.Max(n => n.Pivot.Y) + spacing : 0;
                float fallbackX = startPosition.X;

                foreach (var component in components)
                {
                    if (!layoutGuids.Contains(component.InstanceGuid))
                    {
                        component.CreateAttributes();
                        component.Attributes.Pivot = new PointF(fallbackX, startPosition.Y + fallbackY);
                        fallbackX += component.Attributes.Bounds.Width + spacing;
                        Debug.WriteLine($"[ComponentPlacer] Fallback position: '{component.Name}' at ({component.Attributes.Pivot.X}, {component.Attributes.Pivot.Y})");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComponentPlacer] Dependency layout failed, falling back to grid: {ex.Message}");
                ApplySimpleGridLayout(components, startPosition, spacing);
            }
        }

        /// <summary>
        /// Places components with automatic layout.
        /// </summary>
        /// <param name="components">Components to place</param>
        /// <param name="startPosition">Starting position</param>
        /// <param name="spacing">Spacing between components</param>
        /// <returns>List of placed component names</returns>
        public static List<string> PlaceWithAutoLayout(
            List<IGH_DocumentObject> components,
            PointF? startPosition = null,
            int spacing = 100)
        {
            var result = new DeserializationResult();
            result.Components.AddRange(components);

            return PlaceComponents(result, null, null, startPosition, spacing);
        }
    }
}
