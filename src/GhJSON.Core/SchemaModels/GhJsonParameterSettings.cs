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
using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents configuration for a component's input or output parameter.
    /// Maps to the parameterSettings definition in the schema.
    /// </summary>
    public sealed class GhJsonParameterSettings
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        [JsonProperty("parameterName")]
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the variable name of the input for Script components.
        /// </summary>
        [JsonProperty("variableName")]
        public string VariableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the custom nickname for the parameter.
        /// </summary>
        [JsonProperty("nickName", NullValueHandling = NullValueHandling.Ignore)]
        public string? NickName { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the data tree mapping mode (None, Flatten, Graft).
        /// </summary>
        [JsonProperty("dataMapping", NullValueHandling = NullValueHandling.Ignore)]
        public string? DataMapping { get; set; }

        /// <summary>
        /// Gets or sets the expression that transforms parameter data.
        /// The presence of this property implies the parameter has an expression.
        /// </summary>
        [JsonProperty("expression", NullValueHandling = NullValueHandling.Ignore)]
        public string? Expression { get; set; }

        /// <summary>
        /// Gets or sets the data access mode for script parameters (item, list, tree).
        /// </summary>
        [JsonProperty("access", NullValueHandling = NullValueHandling.Ignore)]
        public string? Access { get; set; }

        /// <summary>
        /// Gets or sets the type hint for script parameters.
        /// </summary>
        [JsonProperty("typeHint", NullValueHandling = NullValueHandling.Ignore)]
        public string? TypeHint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the principal (master) input parameter.
        /// Affects parameter matching behavior.
        /// </summary>
        [JsonProperty("isPrincipal", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPrincipal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this parameter is required (cannot be removed).
        /// Applicable to variable parameter components.
        /// </summary>
        [JsonProperty("isRequired", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter domain is reparameterized.
        /// </summary>
        [JsonProperty("isReparameterized", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsReparameterized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to reverse the parameter data order.
        /// </summary>
        [JsonProperty("isReversed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsReversed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to simplify the parameter data tree.
        /// </summary>
        [JsonProperty("isSimplified", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsSimplified { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to invert boolean values (Param_Boolean only).
        /// </summary>
        [JsonProperty("isInverted", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsInverted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to unitize vectors (Param_Vector only).
        /// </summary>
        [JsonProperty("isUnitized", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsUnitized { get; set; }

        /// <summary>
        /// Gets or sets the internalized data for the parameter.
        /// Uses the internalizedDataTree format from the schema.
        /// </summary>
        [JsonProperty("internalizedData", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Dictionary<string, string>>? InternalizedData { get; set; }
    }
}
