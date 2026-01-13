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

using System.Drawing;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Configuration options for GhJSON deserialization (JSON to components).
    /// Controls how components are created and configured from JSON data.
    /// </summary>
    public class DeserializationOptions
    {
        /// <summary>
        /// Gets the standard deserialization options with all features enabled.
        /// </summary>
        public static DeserializationOptions Standard => new DeserializationOptions
        {
            ApplyProperties = true,
            ApplyParameterSettings = true,
            InjectScriptTypeHints = true,
            ApplyComponentState = true,
            ApplyParameterExpressions = true,
            CreateConnections = true,
            CreateGroups = true,
            ValidateComponentTypes = true,
            ReplaceIntegerIds = true,
            PreserveInstanceGuids = false,
            ApplyAdditionalProperties = true
        };

        /// <summary>
        /// Gets options for component creation only (no placement or connections).
        /// </summary>
        public static DeserializationOptions ComponentsOnly => new DeserializationOptions
        {
            ApplyProperties = true,
            ApplyParameterSettings = true,
            InjectScriptTypeHints = true,
            ApplyComponentState = true,
            ApplyParameterExpressions = true,
            CreateConnections = false,
            CreateGroups = false,
            ValidateComponentTypes = true,
            ReplaceIntegerIds = true,
            PreserveInstanceGuids = false,
            ApplyAdditionalProperties = true
        };

        /// <summary>
        /// Gets options for minimal deserialization (structure only).
        /// </summary>
        public static DeserializationOptions Minimal => new DeserializationOptions
        {
            ApplyProperties = false,
            ApplyParameterSettings = false,
            InjectScriptTypeHints = false,
            ApplyComponentState = false,
            ApplyParameterExpressions = false,
            CreateConnections = true,
            CreateGroups = false,
            ValidateComponentTypes = true,
            ReplaceIntegerIds = true,
            PreserveInstanceGuids = false,
            ApplyAdditionalProperties = false
        };

        /// <summary>
        /// Gets or sets a value indicating whether to apply component properties from the JSON.
        /// </summary>
        public bool ApplyProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to apply parameter settings (nicknames, access modes, etc.).
        /// </summary>
        public bool ApplyParameterSettings { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether schema properties should be applied
        /// from <see cref="GhJSON.Core.Models.Components.ComponentState.AdditionalProperties"/>.
        /// These are component/param-specific properties beyond the core GhJSON schema.
        /// </summary>
        public bool ApplyAdditionalProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to inject type hints into script component code.
        /// </summary>
        public bool InjectScriptTypeHints { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to apply component state (enabled/locked/hidden).
        /// </summary>
        public bool ApplyComponentState { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to apply parameter expressions.
        /// </summary>
        public bool ApplyParameterExpressions { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to create connections between components.
        /// </summary>
        public bool CreateConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to recreate groups.
        /// </summary>
        public bool CreateGroups { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to validate component types before instantiation.
        /// </summary>
        public bool ValidateComponentTypes { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to replace integer IDs with proper GUIDs during deserialization.
        /// </summary>
        public bool ReplaceIntegerIds { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to preserve original instance GUIDs.
        /// </summary>
        public bool PreserveInstanceGuids { get; set; } = false;

        /// <summary>
        /// Gets or sets the starting position for placing components on canvas.
        /// If null, uses default positioning logic.
        /// </summary>
        public PointF? StartPosition { get; set; } = null;

        /// <summary>
        /// Gets or sets the spacing between components when positioning.
        /// </summary>
        public int ComponentSpacing { get; set; } = 100;
    }
}
