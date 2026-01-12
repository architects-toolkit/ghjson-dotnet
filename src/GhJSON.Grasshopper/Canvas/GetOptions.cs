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
using GhJSON.Grasshopper.Serialization;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Options for retrieving objects from the Grasshopper canvas.
    /// </summary>
    public class GetOptions
    {
        /// <summary>
        /// Gets the default get options (all objects, no expansion).
        /// </summary>
        public static GetOptions Default { get; } = new GetOptions();

        /// <summary>
        /// Gets or sets the serialization options for the retrieved objects.
        /// </summary>
        public SerializationOptions? SerializationOptions { get; set; }

        /// <summary>
        /// Gets or sets the connection depth for graph expansion.
        /// 0 = no expansion (default), 1 = include direct neighbors, 2 = two levels, etc.
        /// </summary>
        public int ConnectionDepth { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to trim connections to only include those between returned objects.
        /// </summary>
        public bool TrimConnectionsToResult { get; set; } = true;

        /// <summary>
        /// Gets or sets specific GUIDs to retrieve. If null or empty, uses scope instead.
        /// </summary>
        public IEnumerable<Guid>? GuidsFilter { get; set; }

        /// <summary>
        /// Gets or sets the scope of objects to retrieve when GuidsFilter is not specified.
        /// </summary>
        public GetScope Scope { get; set; } = GetScope.All;

        /// <summary>
        /// Gets or sets strongly-typed attribute filter. When null, no attribute filtering is applied.
        /// </summary>
        public AttributeFilter? AttributeFilter { get; set; }

        /// <summary>
        /// Gets or sets strongly-typed type filter (object kinds and node roles). When null, no type filtering is applied.
        /// </summary>
        public TypeFilter? TypeFilter { get; set; }

        /// <summary>
        /// Gets or sets strongly-typed category filter. When null, no category filtering is applied.
        /// </summary>
        public CategoryFilter? CategoryFilter { get; set; }
    }

    /// <summary>
    /// Scope for retrieving objects from the canvas.
    /// </summary>
    public enum GetScope
    {
        /// <summary>
        /// All objects on the canvas.
        /// </summary>
        All,

        /// <summary>
        /// Only selected objects.
        /// </summary>
        Selected,

        /// <summary>
        /// All objects that currently have runtime errors.
        /// </summary>
        Errors,

        /// <summary>
        /// All objects that currently have runtime warnings.
        /// </summary>
        Warnings,

        /// <summary>
        /// All objects that currently have runtime remarks.
        /// </summary>
        Remarks,

        /// <summary>
        /// All unlocked (enabled) components.
        /// </summary>
        Enabled,

        /// <summary>
        /// All locked (disabled) components.
        /// </summary>
        Disabled,

        /// <summary>
        /// All preview-capable components with preview turned on.
        /// </summary>
        PreviewOn,

        /// <summary>
        /// All preview-capable components with preview turned off.
        /// </summary>
        PreviewOff,

        /// <summary>
        /// Components/params with no incoming connections.
        /// </summary>
        StartNodes,

        /// <summary>
        /// Components/params with no outgoing connections.
        /// </summary>
        EndNodes,

        /// <summary>
        /// Components/params with both incoming and outgoing connections.
        /// </summary>
        MiddleNodes,

        /// <summary>
        /// Components/params with neither incoming nor outgoing connections.
        /// </summary>
        IsolatedNodes,

        /// <summary>
        /// Stand-alone parameters and component parameters.
        /// </summary>
        ParamsOnly,

        /// <summary>
        /// Components only.
        /// </summary>
        ComponentsOnly,
    }

    /// <summary>
    /// Object kind flags for Get operations.
    /// </summary>
    [Flags]
    public enum GetObjectKinds
    {
        /// <summary>
        /// Components.
        /// </summary>
        Components = 1,

        /// <summary>
        /// Parameters.
        /// </summary>
        Params = 2,
    }

    /// <summary>
    /// Node role flags for Get operations.
    /// </summary>
    [Flags]
    public enum GetNodeRoles
    {
        /// <summary>
        /// Nodes with no incoming connections and at least one outgoing connection.
        /// </summary>
        StartNodes = 1,

        /// <summary>
        /// Nodes with no outgoing connections and at least one incoming connection.
        /// </summary>
        EndNodes = 2,

        /// <summary>
        /// Nodes with both incoming and outgoing connections.
        /// </summary>
        MiddleNodes = 4,

        /// <summary>
        /// Nodes with neither incoming nor outgoing connections.
        /// </summary>
        IsolatedNodes = 8,
    }

    /// <summary>
    /// Attribute flags for Get operations.
    /// </summary>
    [Flags]
    public enum GetAttributes
    {
        /// <summary>
        /// Include only selected objects.
        /// </summary>
        Selected = 1,

        /// <summary>
        /// Include only unselected objects.
        /// </summary>
        Unselected = 256,

        /// <summary>
        /// Include only objects that have runtime errors.
        /// </summary>
        HasError = 2,

        /// <summary>
        /// Include only objects that have runtime warnings.
        /// </summary>
        HasWarning = 4,

        /// <summary>
        /// Include only objects that have runtime remarks.
        /// </summary>
        HasRemark = 8,

        /// <summary>
        /// Include only locked components.
        /// </summary>
        Disabled = 16,

        /// <summary>
        /// Include only unlocked components.
        /// </summary>
        Enabled = 32,

        /// <summary>
        /// Include only preview-capable components with preview on.
        /// </summary>
        PreviewOn = 64,

        /// <summary>
        /// Include only preview-capable components with preview off.
        /// </summary>
        PreviewOff = 128,

        /// <summary>
        /// Include only preview-capable components.
        /// </summary>
        PreviewCapable = 512,

        /// <summary>
        /// Include only components that are not preview-capable.
        /// </summary>
        NotPreviewCapable = 1024,
    }

    /// <summary>
    /// Result of a get operation with connection depth expansion.
    /// </summary>
    public class GetResult
    {
        /// <summary>
        /// Gets the GhJSON document containing the retrieved objects.
        /// </summary>
        public Core.Models.Document.GhJsonDocument Document { get; set; }

        /// <summary>
        /// Gets the list of instance GUIDs that were retrieved (before expansion).
        /// </summary>
        public List<Guid> InitialGuids { get; } = new List<Guid>();

        /// <summary>
        /// Gets the list of instance GUIDs after connection depth expansion.
        /// </summary>
        public List<Guid> ExpandedGuids { get; } = new List<Guid>();

        /// <summary>
        /// Gets a value indicating whether connection depth expansion occurred.
        /// </summary>
        public bool WasExpanded => ExpandedGuids.Count > InitialGuids.Count;
    }
}
