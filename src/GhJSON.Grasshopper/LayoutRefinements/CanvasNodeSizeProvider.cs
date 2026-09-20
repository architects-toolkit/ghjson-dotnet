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
using System.Drawing;
using GhJSON.Core.DependencyGraph;
using GhJSON.Grasshopper.GetOperations;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Factory for live-bounds providers consumable through
    /// <see cref="LayoutOptions.NodeSizeProvider"/>: they map a layout node key to the
    /// measured <c>Attributes.Bounds</c> size of the matching canvas object, so the core
    /// layout can reason about real component geometry instead of default sizes.
    /// </summary>
    public static class CanvasNodeSizeProvider
    {
        /// <summary>
        /// Creates a size provider over the active Grasshopper document (or an explicit one).
        /// Unknown, missing, or unmeasurable objects return <c>null</c> so the layout engine
        /// falls back to its configured defaults. All Grasshopper lookups are guarded: with
        /// no document the provider returns null for every key.
        /// </summary>
        /// <param name="document">The document to measure; when null the active canvas document is used per call.</param>
        /// <returns>A provider suitable for <see cref="LayoutOptions.NodeSizeProvider"/>.</returns>
        public static Func<Guid, SizeF?> Create(GH_Document? document = null)
        {
            return guid =>
            {
                try
                {
                    var doc = document ?? CanvasReader.GetActiveDocument();
                    return doc == null ? null : Measure(doc.FindObject(guid, false));
                }
                catch
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// Measures an object's <c>Attributes.Bounds</c>, returning null when the object,
        /// its attributes, or its bounds are unavailable or empty.
        /// </summary>
        internal static SizeF? Measure(IGH_DocumentObject? obj)
        {
            try
            {
                var bounds = obj?.Attributes?.Bounds;
                if (bounds.HasValue && bounds.Value.Width > 0f && bounds.Value.Height > 0f)
                {
                    return bounds.Value.Size;
                }
            }
            catch
            {
                // Attribute access can throw for objects without a fully initialized UI.
            }

            return null;
        }
    }
}
