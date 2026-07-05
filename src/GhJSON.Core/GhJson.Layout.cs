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
using System.Linq;
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.DependencyGraph.Internal;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core
{
    public static partial class GhJson
    {
        #region Layout Operations

        /// <summary>
        /// Calculates optimal layout positions for components in a GhJSON document using dependency graph analysis.
        /// </summary>
        /// <param name="document">The GhJSON document containing components and connections.</param>
        /// <param name="options">Optional layout configuration. If null, uses default settings.</param>
        /// <returns>Layout result containing calculated positions, island information, and diagnostics.</returns>
        /// <remarks>
        /// This method analyzes the dependency graph formed by component connections and applies
        /// a layout algorithm (default: Sugiyama) to minimize wire crossings and organize components
        /// in a clear, hierarchical structure. Components are grouped into "islands" (disconnected subgraphs)
        /// which are laid out independently.
        /// </remarks>
        public static LayoutResult CalculateLayout(GhJsonDocument document, LayoutOptions? options = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return LayoutEngine.CalculateLayout(document, options);
        }

        /// <summary>
        /// Assigns calculated layout positions as pivots to components in a GhJSON document.
        /// </summary>
        /// <param name="document">The source GhJSON document.</param>
        /// <param name="layoutResult">The layout result containing calculated positions.</param>
        /// <returns>A new GhJSON document with updated component pivots.</returns>
        /// <remarks>
        /// This method creates a new document with the same components and connections,
        /// but updates the Pivot property of each component based on the layout result.
        /// Components not present in the layout result retain their original pivots.
        /// </remarks>
        public static GhJsonDocument AssignPivots(GhJsonDocument document, LayoutResult layoutResult)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (layoutResult == null)
            {
                throw new ArgumentNullException(nameof(layoutResult));
            }

            var builder = CreateDocumentBuilder();
            
            foreach (var component in document.Components)
            {
                // Match by the same stable key used by GraphBuilder so layout flows through
                // for components that only expose an integer Id (no InstanceGuid).
                var key = GraphBuilder.GetStableKey(component);
                if (key != Guid.Empty &&
                    layoutResult.Positions.TryGetValue(key, out var newPivot))
                {
                    var updatedComponent = new GhJsonComponent
                    {
                        Name = component.Name,
                        Library = component.Library,
                        NickName = component.NickName,
                        ComponentGuid = component.ComponentGuid,
                        InstanceGuid = component.InstanceGuid,
                        Id = component.Id,
                        Pivot = newPivot,
                        InputSettings = component.InputSettings,
                        OutputSettings = component.OutputSettings,
                        ComponentState = component.ComponentState,
                        Errors = component.Errors,
                        Warnings = component.Warnings,
                        Remarks = component.Remarks,
                    };
                    builder = builder.AddComponent(updatedComponent);
                }
                else
                {
                    builder = builder.AddComponent(component);
                }
            }

            if (document.Connections != null)
            {
                builder = builder.AddConnections(document.Connections);
            }

            if (document.Groups != null)
            {
                builder = builder.AddGroups(document.Groups);
            }

            if (document.Metadata != null)
            {
                builder = builder.WithMetadata(document.Metadata);
            }

            return builder.Build();
        }

        /// <summary>
        /// Returns the stable layout key used to identify <paramref name="component"/> in a
        /// <see cref="LayoutResult"/>. This is the component's <c>InstanceGuid</c> when present,
        /// otherwise a deterministic GUID synthesized from its integer <c>Id</c>. Callers that
        /// need to look up calculated positions for components that may only expose an <c>Id</c>
        /// (no <c>InstanceGuid</c>) must use this key rather than <c>InstanceGuid</c> directly.
        /// </summary>
        /// <param name="component">The component to compute the key for.</param>
        /// <returns>The stable layout key, or <see cref="Guid.Empty"/> when the component has neither identifier.</returns>
        public static Guid GetLayoutKey(GhJsonComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            return GraphBuilder.GetStableKey(component);
        }

        /// <summary>
        /// Reorganizes component pivots in a GhJSON document by calculating and applying a new layout.
        /// </summary>
        /// <param name="document">The GhJSON document to reorganize.</param>
        /// <param name="options">Optional layout configuration. If null, uses default settings.</param>
        /// <returns>A new GhJSON document with reorganized component positions.</returns>
        /// <remarks>
        /// This is a convenience method that combines CalculateLayout and AssignPivots.
        /// It calculates optimal positions based on the dependency graph and returns a new
        /// document with updated pivots.
        /// </remarks>
        public static GhJsonDocument ReorganizePivots(GhJsonDocument document, LayoutOptions? options = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var layoutResult = CalculateLayout(document, options);
            return AssignPivots(document, layoutResult);
        }

        #endregion
    }
}
