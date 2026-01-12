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

using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.Models.Components
{
    /// <summary>
    /// Represents settings for input or output parameters of a component.
    /// </summary>
    public class ParameterSettings
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        [JsonProperty("parameterName")]
        [JsonRequired]
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the nickname of the parameter.
        /// </summary>
        [JsonProperty("nickName", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? NickName { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether this parameter is marked as the principal (master) input parameter.
        /// For components, this affects parameter matching behavior and is indicated by a special icon.
        /// Only valid for input parameters.
        /// </summary>
        [JsonProperty("isPrincipal", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPrincipal { get; set; }

        /// <summary>
        /// Gets or sets whether this parameter is required (cannot be removed by the user).
        /// Applicable to VB Script and other variable parameter components.
        /// When false or null, the parameter is optional and can be removed.
        /// </summary>
        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Required { get; set; }

        /// <summary>
        /// Gets or sets the data mapping mode for the parameter.
        /// </summary>
        [JsonProperty("dataMapping", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? DataMapping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is reparameterized.
        /// </summary>
        [JsonProperty("isReparameterized", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsReparameterized { get; set; }

        /// <summary>
        /// Gets or sets the parameter expression that transforms data.
        /// The presence of this property implies hasExpression=true, making that flag redundant.
        /// </summary>
        [JsonProperty("expression", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Expression { get; set; }

        /// <summary>
        /// Gets or sets the variable name for script parameters.
        /// </summary>
        [JsonProperty("variableName", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? VariableName { get; set; }

        /// <summary>
        /// Gets or sets the access mode for the parameter (item, list, tree). Necessary for script components.
        /// </summary>
        [JsonProperty("access", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Access { get; set; }

        /// <summary>
        /// Gets or sets the type hint for script parameters (e.g., "int", "double", "DataTree", etc.).
        /// </summary>
        [JsonProperty("typeHint", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? TypeHint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to reverse the parameter data order.
        /// </summary>
        [JsonProperty("reverse", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Reverse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to simplify the parameter data tree.
        /// </summary>
        [JsonProperty("simplify", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Simplify { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is locked.
        /// </summary>
        [JsonProperty("locked", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Locked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to invert boolean values (Param_Boolean only).
        /// </summary>
        [JsonProperty("invert", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Invert { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to unitize vectors (Param_Vector only).
        /// </summary>
        [JsonProperty("unitize", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Unitize { get; set; }

        /// <summary>
        /// Gets or sets the persistent data stored in the parameter.
        /// This contains the actual values assigned to the parameter.
        /// Excluded when using Optimized serialization to reduce output size.
        /// </summary>
        [JsonProperty("persistentData", NullValueHandling = NullValueHandling.Ignore)]
        public object? PersistentData { get; set; }
    }
}
