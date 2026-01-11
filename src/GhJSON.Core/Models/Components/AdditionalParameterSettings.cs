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

using Newtonsoft.Json;

namespace GhJSON.Core.Models.Components
{
    /// <summary>
    /// Represents additional settings for parameters such as flags and modifiers.
    /// </summary>
    public class AdditionalParameterSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the parameter data should be reversed.
        /// </summary>
        [JsonProperty("reverse", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Reverse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter data tree should be simplified.
        /// </summary>
        [JsonProperty("simplify", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Simplify { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is principal for matching.
        /// </summary>
        [JsonProperty("isPrincipal", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPrincipal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is locked.
        /// </summary>
        [JsonProperty("locked", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Locked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter inverts boolean values (Param_Boolean only).
        /// </summary>
        [JsonProperty("invert", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Invert { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vectors should be unitized (Param_Vector only).
        /// </summary>
        [JsonProperty("unitize", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Unitize { get; set; }
    }
}
