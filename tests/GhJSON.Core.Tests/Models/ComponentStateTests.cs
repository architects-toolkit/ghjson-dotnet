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
using GhJSON.Core.Models.Components;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class ComponentStateTests
    {
        [Fact]
        public void Constructor_SetsAllPropertiesToNull()
        {
            var state = new ComponentState();

            Assert.Null(state.Value);
            Assert.Null(state.VBCode);
            Assert.Null(state.Locked);
            Assert.Null(state.Hidden);
            Assert.Null(state.Selected);
            Assert.Null(state.Multiline);
            Assert.Null(state.Wrap);
            Assert.Null(state.Color);
            Assert.Null(state.MarshInputs);
            Assert.Null(state.MarshOutputs);
            Assert.Null(state.ShowStandardOutput);
            Assert.Null(state.ListMode);
            Assert.Null(state.ListItems);
            Assert.Null(state.Font);
            Assert.Null(state.Corners);
            Assert.Null(state.DrawIndices);
            Assert.Null(state.DrawPaths);
            Assert.Null(state.Alignment);
            Assert.Null(state.Bounds);
            Assert.Null(state.Rounding);
            Assert.Null(state.AdditionalProperties);
        }

        [Fact]
        public void JsonSerialization_SliderValue_RoundTrip()
        {
            var state = new ComponentState
            {
                Value = "5<2,10.000>"
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("5<2,10.000>", deserialized.Value?.ToString());
        }

        [Fact]
        public void JsonSerialization_PanelValue_RoundTrip()
        {
            var state = new ComponentState
            {
                Value = "Hello World",
                Multiline = true,
                Wrap = true
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("Hello World", deserialized.Value?.ToString());
            Assert.True(deserialized.Multiline);
            Assert.True(deserialized.Wrap);
        }

        [Fact]
        public void JsonSerialization_ScriptComponentState_RoundTrip()
        {
            var state = new ComponentState
            {
                Value = "import math\nprint(x)",
                MarshInputs = true,
                MarshOutputs = false,
                ShowStandardOutput = true
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("import math\nprint(x)", deserialized.Value?.ToString());
            Assert.True(deserialized.MarshInputs);
            Assert.False(deserialized.MarshOutputs);
            Assert.True(deserialized.ShowStandardOutput);
        }

        [Fact]
        public void JsonSerialization_ValueListState_RoundTrip()
        {
            var state = new ComponentState
            {
                ListMode = "DropDown",
                ListItems = new List<ValueListItem>
                {
                    new ValueListItem { Name = "A", Expression = "0", Selected = true },
                    new ValueListItem { Name = "B", Expression = "1" },
                    new ValueListItem { Name = "C", Expression = "2", Selected = true },
                }
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("DropDown", deserialized.ListMode);
            Assert.NotNull(deserialized.ListItems);
            Assert.Equal(3, deserialized.ListItems.Count);
            Assert.True(deserialized.ListItems[0].Selected);
            Assert.False(deserialized.ListItems[1].Selected);
            Assert.True(deserialized.ListItems[2].Selected);
        }

        [Fact]
        public void JsonSerialization_ColorProperty_RoundTrip()
        {
            var state = new ComponentState
            {
                Color = "255,128,64,255" // ARGB format per schema
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("255,128,64,255", deserialized.Color);
        }

        [Fact]
        public void JsonSerialization_BoundsProperty_RoundTrip()
        {
            var state = new ComponentState
            {
                Bounds = "100x50" // WxH format per schema
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("100x50", deserialized.Bounds);
        }

        [Fact]
        public void JsonSerialization_OmitsNullValues()
        {
            var state = new ComponentState
            {
                Locked = true
            };

            var json = JsonConvert.SerializeObject(state);

            Assert.Contains("\"locked\"", json);
            Assert.DoesNotContain("\"value\"", json);
            Assert.DoesNotContain("\"hidden\"", json);
            Assert.DoesNotContain("\"enabled\"", json);
            Assert.DoesNotContain("\"multiline\"", json);
        }

        [Fact]
        public void JsonSerialization_VBScriptCode_RoundTrip()
        {
            var state = new ComponentState
            {
                VBCode = new VBScriptCode
                {
                    Imports = "Imports System",
                    Script = "A = x + y",
                    Additional = "' Comment"
                }
            };

            var json = JsonConvert.SerializeObject(state);
            var deserialized = JsonConvert.DeserializeObject<ComponentState>(json);

            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.VBCode);
            Assert.Equal("Imports System", deserialized.VBCode.Imports);
            Assert.Equal("A = x + y", deserialized.VBCode.Script);
            Assert.Equal("' Comment", deserialized.VBCode.Additional);
        }
    }
}
