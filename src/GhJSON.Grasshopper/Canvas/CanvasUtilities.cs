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
using System.Drawing;
using System.Linq;
using GhJSON.Core.Models.Document;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Shared utility methods for canvas operations.
    /// Provides common functionality used by ComponentPlacer, ConnectionManager, and GroupManager.
    /// </summary>
    public static class CanvasUtilities
    {
        /// <summary>
        /// Gets the current active Grasshopper document from the canvas.
        /// </summary>
        /// <returns>The active GH_Document, or null if not available.</returns>
        public static GH_Document? GetCurrentCanvas()
        {
            try
            {
                return Instances.ActiveCanvas?.Document;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds mapping from integer IDs to component instances.
        /// Used by ConnectionManager and GroupManager to resolve component references.
        /// </summary>
        /// <param name="document">GhJSON document containing component definitions</param>
        /// <param name="guidMapping">Dictionary mapping GUIDs to component instances</param>
        /// <returns>Dictionary mapping integer IDs to component instances</returns>
        public static Dictionary<int, IGH_DocumentObject> BuildIdMapping(
            GrasshopperDocument? document,
            Dictionary<Guid, IGH_DocumentObject>? guidMapping)
        {
            var idToComponent = new Dictionary<int, IGH_DocumentObject>();

            if (document?.Components != null && guidMapping != null)
            {
                foreach (var compProps in document.Components)
                {
                    if (compProps.Id > 0 && compProps.InstanceGuid.HasValue && 
                        guidMapping.TryGetValue(compProps.InstanceGuid.Value, out var instance))
                    {
                        idToComponent[compProps.Id] = instance;
                    }
                }
            }

            return idToComponent;
        }

        /// <summary>
        /// Gets all objects currently on the active Grasshopper canvas.
        /// </summary>
        /// <returns>List of document objects on the canvas</returns>
        public static List<IGH_DocumentObject> GetCurrentObjects()
        {
            var document = GetCurrentCanvas();
            if (document == null)
            {
                return new List<IGH_DocumentObject>();
            }

            return document.Objects.ToList();
        }

        /// <summary>
        /// Gets all active objects in the current document.
        /// </summary>
        /// <returns>List of active objects on the canvas.</returns>
        public static List<IGH_ActiveObject> GetActiveObjects()
        {
            var doc = GetCurrentCanvas();
            if (doc == null)
            {
                return new List<IGH_ActiveObject>();
            }

            try
            {
                return doc.ActiveObjects() ?? new List<IGH_ActiveObject>();
            }
            catch
            {
                return new List<IGH_ActiveObject>();
            }
        }

        /// <summary>
        /// Finds a document object instance by its GUID.
        /// </summary>
        /// <param name="guid">The instance GUID.</param>
        /// <returns>The matching document object or null if not found.</returns>
        public static IGH_DocumentObject? FindInstance(Guid guid)
        {
            var obj = GetActiveObjects().FirstOrDefault(o => o.InstanceGuid == guid);
            return obj;
        }

        /// <summary>
        /// Calculates a starting point for placing new components to avoid overlap.
        /// </summary>
        /// <param name="spacing">Minimum spacing from existing components</param>
        /// <returns>A point below all existing objects on the canvas</returns>
        public static PointF CalculateStartPoint(int spacing = 100)
        {
            var currentObjects = GetCurrentObjects();

            if (currentObjects.Count == 0)
            {
                return new PointF(50, 50);
            }

            float lowestY = currentObjects.Max(o => o.Attributes.Pivot.Y + o.Attributes.Bounds.Height);
            return new PointF(50, lowestY + spacing);
        }

        /// <summary>
        /// Computes the bounding box of all objects in the current document.
        /// </summary>
        /// <returns>The bounding rectangle of all objects.</returns>
        public static RectangleF BoundingBox()
        {
            var doc = GetCurrentCanvas();
            if (doc == null)
            {
                return RectangleF.Empty;
            }

            return doc.BoundingBox(false);
        }

        /// <summary>
        /// Gets the bounding rectangle of a Grasshopper component or parameter on the canvas.
        /// </summary>
        /// <param name="guid">GUID of the component or parameter.</param>
        /// <returns>The bounding rectangle, or RectangleF.Empty if not found.</returns>
        public static RectangleF GetComponentBounds(Guid guid)
        {
            var obj = FindInstance(guid);
            if (obj != null)
            {
                return obj.Attributes.Bounds;
            }

            return RectangleF.Empty;
        }

        /// <summary>
        /// Adds a document object to the active Grasshopper canvas.
        /// </summary>
        /// <param name="obj">The object to add</param>
        /// <param name="position">Position for the object's pivot</param>
        /// <param name="redraw">Whether to redraw the canvas after adding</param>
        public static void AddObjectToCanvas(IGH_DocumentObject obj, PointF position, bool redraw = true)
        {
            var document = GetCurrentCanvas();
            if (document == null)
            {
                throw new InvalidOperationException("No active Grasshopper document");
            }

            obj.CreateAttributes();
            obj.Attributes.Pivot = position;
            document.AddObject(obj, false);

            if (redraw)
            {
                Instances.ActiveCanvas?.Refresh();
            }
        }

        /// <summary>
        /// Adds an object to the canvas at the specified position and expires its layout.
        /// </summary>
        /// <param name="obj">The object to add.</param>
        /// <param name="position">The pivot position.</param>
        /// <param name="redraw">Whether to redraw the canvas.</param>
        public static void AddObjectToCanvasWithExpire(IGH_DocumentObject obj, PointF position, bool redraw = true)
        {
            var doc = GetCurrentCanvas();
            if (doc == null)
            {
                throw new InvalidOperationException("No active Grasshopper document");
            }

            obj.Attributes.Pivot = position;
            doc.AddObject(obj, false);

            if (redraw)
            {
                obj.Attributes.ExpireLayout();
                Instances.RedrawCanvas();
            }
        }
    }
}
