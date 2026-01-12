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

namespace GhJSON.Core.Models.Components
{
    /// <summary>
    /// Represents the properties and metadata of a Grasshopper component.
    /// </summary>
    public class ComponentProperties
    {
        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        [JsonProperty("name")]
        [JsonRequired]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component library/category.
        /// </summary>
        [JsonProperty("library", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Library { get; set; }

        /// <summary>
        /// Gets or sets the nickname of the component.
        /// </summary>
        [JsonProperty("nickName", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? NickName { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the component type.
        /// </summary>
        [JsonProperty("componentGuid")]
        public Guid ComponentGuid { get; set; }

        public bool ShouldSerializeComponentGuid()
        {
            return this.ComponentGuid != Guid.Empty;
        }

        /// <summary>
        /// Gets or sets the unique identifier for this specific component instance.
        /// Optional - can be inferred from the component name if not provided.
        /// </summary>
        [JsonProperty("instanceGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InstanceGuid { get; set; }

        /// <summary>
        /// Gets or sets the pivot point of the component on the canvas.
        /// Uses compact string format "X,Y" instead of object format for optimization.
        /// </summary>
        [JsonProperty("pivot")]
        public CompactPosition Pivot { get; set; }

        /// <summary>
        /// Gets or sets the integer ID for the component (used for group references and connections).
        /// Required for compact reference in connections and groups.
        /// </summary>
        [JsonProperty("id")]
        [JsonRequired]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the input parameter settings array.
        /// </summary>
        [JsonProperty("inputSettings", NullValueHandling = NullValueHandling.Ignore)]
        public List<ParameterSettings>? InputSettings { get; set; }

        /// <summary>
        /// Gets or sets the output parameter settings array.
        /// </summary>
        [JsonProperty("outputSettings", NullValueHandling = NullValueHandling.Ignore)]
        public List<ParameterSettings>? OutputSettings { get; set; }

        /// <summary>
        /// Gets or sets the component-specific UI state.
        /// </summary>
        [JsonProperty("componentState", NullValueHandling = NullValueHandling.Ignore)]
        public ComponentState? ComponentState { get; set; }

        /// <summary>
        /// Gets or sets a list of warnings associated with the component.
        /// </summary>
        [JsonProperty("warnings", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Warnings { get; set; }

        /// <summary>
        /// Gets or sets a list of errors associated with the component.
        /// </summary>
        [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets simple key-value pairs for additional component properties.
        /// </summary>
        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? Properties { get; set; }

        /// <summary>
        /// Gets a value indicating whether the component has any validation errors or warnings.
        /// </summary>
        [JsonIgnore]
        public bool HasIssues => this.Warnings?.Any() == true || this.Errors?.Any() == true;
    }
}
