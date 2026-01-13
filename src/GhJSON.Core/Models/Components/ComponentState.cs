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
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.Models.Components
{
    /// <summary>
    /// Represents UI-specific state for components.
    /// The Value property stores the primary value for all component types.
    /// </summary>
    public class ComponentState
    {
        /// <summary>
        /// Gets or sets the universal value property for the component.
        /// This stores the primary value for all component types:
        /// - Number Slider: "5&lt;2,10.000&gt;" (value with range; highest decimal count determines precision)
        /// - Panel/Scribble: "Hello World" (plain text)
        /// - Value List: [{"Name":"A","Expression":"0"}] (array of items)
        /// - Script (C#/Python): "import math\nprint(x)" (script code as string)
        /// - VB Script: VBScriptCode object with 3 sections (imports, script, additional)
        /// - Multidimensional Slider: "1.0,2.0,3.0" (coordinate values)
        /// </summary>
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the VB Script code with 3 separate sections.
        /// This is an alternative to Value for VB Script components only.
        /// When present, this takes precedence over Value for VB Script deserialization.
        /// </summary>
        [JsonProperty("vbCode", NullValueHandling = NullValueHandling.Ignore)]
        public VBScriptCode? VBCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component is currently selected on the canvas.
        /// </summary>
        [JsonProperty("selected", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Selected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component is locked (disabled).
        /// </summary>
        [JsonProperty("locked", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Locked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component preview is hidden.
        /// </summary>
        [JsonProperty("hidden", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multiline mode is enabled (for panels, text components).
        /// </summary>
        [JsonProperty("multiline", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Multiline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether text wrapping is enabled.
        /// </summary>
        [JsonProperty("wrap", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Wrap { get; set; }

        /// <summary>
        /// Gets or sets the component color in ARGB format (e.g., '255,128,64,255').
        /// </summary>
        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether inputs should be marshalled (for script components).
        /// </summary>
        [JsonProperty("marshInputs", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MarshInputs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether outputs should be marshalled (for script components).
        /// </summary>
        [JsonProperty("marshOutputs", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MarshOutputs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the standard output/error parameter ("out") should be shown (for script components).
        /// </summary>
        [JsonProperty("showStandardOutput", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowStandardOutput { get; set; }

        /// <summary>
        /// Gets or sets the list mode for value list components (e.g., "DropDown", "CheckList").
        /// </summary>
        [JsonProperty("listMode", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? ListMode { get; set; }

        /// <summary>
        /// Gets or sets the list items for value list components.
        /// Each item contains Name, Expression, and optionally Selected state.
        /// </summary>
        [JsonProperty("listItems", NullValueHandling = NullValueHandling.Ignore)]
        public List<ValueListItem>? ListItems { get; set; }

        /// <summary>
        /// Gets or sets font configuration for text components.
        /// </summary>
        [JsonProperty("font", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? Font { get; set; }

        /// <summary>
        /// Gets or sets corner points for scribble components.
        /// Format: array of strings "x,y" (e.g., ["10,20", "30,40"]).
        /// </summary>
        [JsonProperty("corners", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Corners { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to draw indices (for panels).
        /// </summary>
        [JsonProperty("drawIndices", NullValueHandling = NullValueHandling.Ignore)]
        public bool? DrawIndices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to draw paths (for panels).
        /// </summary>
        [JsonProperty("drawPaths", NullValueHandling = NullValueHandling.Ignore)]
        public bool? DrawPaths { get; set; }

        /// <summary>
        /// Gets or sets the alignment (for panels).
        /// </summary>
        [JsonProperty("alignment", NullValueHandling = NullValueHandling.Ignore)]
        public int? Alignment { get; set; }

        /// <summary>
        /// Gets or sets whether to interpret panel text as special codes (for panels).
        /// </summary>
        [JsonProperty("specialCodes", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SpecialCodes { get; set; }

        /// <summary>
        /// Gets or sets the bounds (size) for panels and other UI components.
        /// Format: 'WxH' (e.g., '100x200').
        /// </summary>
        [JsonProperty("bounds", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Bounds { get; set; }

        /// <summary>
        /// Gets or sets the rounding mode for number sliders.
        /// Values: "Float", "Integer", "Even", "Odd"
        /// </summary>
        [JsonProperty("rounding", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Rounding { get; set; }

        /// <summary>
        /// Gets or sets additional component-specific properties that don't fit into standard fields.
        /// </summary>
        [JsonProperty("additionalProperties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
