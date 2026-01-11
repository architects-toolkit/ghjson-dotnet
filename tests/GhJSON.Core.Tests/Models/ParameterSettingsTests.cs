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

using GhJSON.Core.Models.Components;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class ParameterSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var settings = new ParameterSettings();

            Assert.Equal(string.Empty, settings.ParameterName);
            Assert.Null(settings.NickName);
            Assert.Null(settings.Description);
            Assert.Null(settings.IsPrincipal);
            Assert.Null(settings.Required);
            Assert.Null(settings.DataMapping);
            Assert.Null(settings.IsReparameterized);
            Assert.Null(settings.Expression);
            Assert.Null(settings.VariableName);
            Assert.Null(settings.Access);
            Assert.Null(settings.TypeHint);
            Assert.Null(settings.AdditionalSettings);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesAllProperties()
        {
            var original = new ParameterSettings
            {
                ParameterName = "A",
                NickName = "Input A",
                Description = "First input parameter",
                IsPrincipal = true,
                Required = true,
                DataMapping = "None",
                IsReparameterized = false,
                Expression = "x * 2",
                VariableName = "inputA",
                Access = "item",
                TypeHint = "double",
                AdditionalSettings = new AdditionalParameterSettings
                {
                    Reverse = true,
                    Simplify = false
                }
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ParameterSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("A", deserialized.ParameterName);
            Assert.Equal("Input A", deserialized.NickName);
            Assert.Equal("First input parameter", deserialized.Description);
            Assert.True(deserialized.IsPrincipal);
            Assert.True(deserialized.Required);
            Assert.Equal("None", deserialized.DataMapping);
            Assert.False(deserialized.IsReparameterized);
            Assert.Equal("x * 2", deserialized.Expression);
            Assert.Equal("inputA", deserialized.VariableName);
            Assert.Equal("item", deserialized.Access);
            Assert.Equal("double", deserialized.TypeHint);
            Assert.NotNull(deserialized.AdditionalSettings);
            Assert.True(deserialized.AdditionalSettings.Reverse);
            Assert.False(deserialized.AdditionalSettings.Simplify);
        }

        [Fact]
        public void JsonSerialization_OmitsNullValues()
        {
            var settings = new ParameterSettings
            {
                ParameterName = "A"
            };

            var json = JsonConvert.SerializeObject(settings);

            Assert.Contains("\"parameterName\"", json);
            Assert.DoesNotContain("\"nickName\"", json);
            Assert.DoesNotContain("\"description\"", json);
            Assert.DoesNotContain("\"isPrincipal\"", json);
            Assert.DoesNotContain("\"dataMapping\"", json);
            Assert.DoesNotContain("\"expression\"", json);
            Assert.DoesNotContain("\"variableName\"", json);
            Assert.DoesNotContain("\"access\"", json);
            Assert.DoesNotContain("\"typeHint\"", json);
            Assert.DoesNotContain("\"additionalSettings\"", json);
        }

        [Fact]
        public void JsonSerialization_ScriptParameter_RoundTrip()
        {
            var settings = new ParameterSettings
            {
                ParameterName = "x",
                VariableName = "x",
                Access = "list",
                TypeHint = "DataTree<object>"
            };

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<ParameterSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("x", deserialized.ParameterName);
            Assert.Equal("x", deserialized.VariableName);
            Assert.Equal("list", deserialized.Access);
            Assert.Equal("DataTree<object>", deserialized.TypeHint);
        }

        [Fact]
        public void JsonSerialization_ExpressionParameter_RoundTrip()
        {
            var settings = new ParameterSettings
            {
                ParameterName = "Result",
                Expression = "Math.Round(x, 2)"
            };

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<ParameterSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("Result", deserialized.ParameterName);
            Assert.Equal("Math.Round(x, 2)", deserialized.Expression);
        }
    }
}
