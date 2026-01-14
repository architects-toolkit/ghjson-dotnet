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
using System.Linq;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents a single Grasshopper component or floating parameter.
    /// Maps to the componentData definition in the schema.
    /// </summary>
    public sealed class GhJsonComponent
    {
        /// <summary>
        /// Gets or sets the name of the component.
        /// Must match the component's name in the Grasshopper library.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the component library/category.
        /// </summary>
        [JsonProperty("library", NullValueHandling = NullValueHandling.Ignore)]
        public string? Library { get; set; }

        /// <summary>
        /// Gets or sets the custom nickname for the component.
        /// </summary>
        [JsonProperty("nickName", NullValueHandling = NullValueHandling.Ignore)]
        public string? NickName { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the component type.
        /// Used to instantiate the correct component class.
        /// </summary>
        [JsonProperty("componentGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? ComponentGuid { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this specific component instance.
        /// </summary>
        [JsonProperty("instanceGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InstanceGuid { get; set; }

        /// <summary>
        /// Gets or sets the integer ID for compact reference in connections and groups.
        /// Must be unique within the document.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the position of the component on the Grasshopper canvas.
        /// Supports both compact string format "X,Y" and object format.
        /// </summary>
        [JsonProperty("pivot", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(PivotConverter))]
        public GhJsonPivot? Pivot { get; set; }

        /// <summary>
        /// Gets or sets the configuration for input parameters.
        /// </summary>
        [JsonProperty("inputSettings", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhJsonParameterSettings>? InputSettings { get; set; }

        /// <summary>
        /// Gets or sets the configuration for output parameters.
        /// </summary>
        [JsonProperty("outputSettings", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhJsonParameterSettings>? OutputSettings { get; set; }

        /// <summary>
        /// Gets or sets the UI-specific state for the component.
        /// </summary>
        [JsonProperty("componentState", NullValueHandling = NullValueHandling.Ignore)]
        public GhJsonComponentState? ComponentState { get; set; }

        /// <summary>
        /// Gets or sets list of error messages associated with the component.
        /// </summary>
        [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets list of warning messages associated with the component.
        /// </summary>
        [JsonProperty("warnings", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Warnings { get; set; }

        /// <summary>
        /// Gets or sets list of remarks associated with the component.
        /// </summary>
        [JsonProperty("remarks", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remarks { get; set; }

        /// <summary>
        /// Gets a value indicating whether the component has any validation errors or warnings.
        /// </summary>
        [JsonIgnore]
        public bool HasIssues =>
            (this.Warnings?.Any() == true) ||
            (this.Errors?.Any() == true);
    }
}
