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
    /// Represents the three code sections of a VB Script component.
    /// VB Script components have separate sections for imports, main code, and additional code.
    /// </summary>
    public class VBScriptCode
    {
        /// <summary>
        /// Gets or sets the imports section (Using statements).
        /// This section appears at the top of the script editor.
        /// Example: "Imports System\r\nImports Rhino"
        /// </summary>
        [JsonProperty("imports", NullValueHandling = NullValueHandling.Ignore)]
        public string? Imports { get; set; }

        /// <summary>
        /// Gets or sets the main RunScript code section.
        /// This is the primary user code that appears in the "Members" region.
        /// Contains the actual logic of the script.
        /// </summary>
        [JsonProperty("script", NullValueHandling = NullValueHandling.Ignore)]
        public string? Script { get; set; }

        /// <summary>
        /// Gets or sets the additional code section.
        /// This section appears after the RunScript method in the "Custom additional code" region.
        /// Used for helper methods, classes, or additional functionality.
        /// </summary>
        [JsonProperty("additional", NullValueHandling = NullValueHandling.Ignore)]
        public string? Additional { get; set; }
    }
}
