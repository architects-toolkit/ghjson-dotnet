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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class ComponentPropertiesTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var props = new ComponentProperties();

            Assert.Equal(string.Empty, props.Name);
            Assert.Null(props.Library);
            Assert.Null(props.NickName);
            Assert.Equal(Guid.Empty, props.ComponentGuid);
            Assert.Null(props.InstanceGuid);
            Assert.Equal(0, props.Id);
            Assert.Null(props.InputSettings);
            Assert.Null(props.OutputSettings);
            Assert.Null(props.ComponentState);
            Assert.Null(props.Warnings);
            Assert.Null(props.Errors);
            Assert.Null(props.Properties);
        }

        [Fact]
        public void HasIssues_ReturnsFalseWhenNoWarningsOrErrors()
        {
            var props = new ComponentProperties
            {
                Name = "Test",
                Id = 1
            };

            Assert.False(props.HasIssues);
        }

        [Fact]
        public void HasIssues_ReturnsTrueWhenHasWarnings()
        {
            var props = new ComponentProperties
            {
                Name = "Test",
                Id = 1,
                Warnings = new List<string> { "Warning 1" }
            };

            Assert.True(props.HasIssues);
        }

        [Fact]
        public void HasIssues_ReturnsTrueWhenHasErrors()
        {
            var props = new ComponentProperties
            {
                Name = "Test",
                Id = 1,
                Errors = new List<string> { "Error 1" }
            };

            Assert.True(props.HasIssues);
        }

        [Fact]
        public void HasIssues_ReturnsTrueWhenHasBothWarningsAndErrors()
        {
            var props = new ComponentProperties
            {
                Name = "Test",
                Id = 1,
                Warnings = new List<string> { "Warning 1" },
                Errors = new List<string> { "Error 1" }
            };

            Assert.True(props.HasIssues);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesAllProperties()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var original = new ComponentProperties
            {
                Name = "Addition",
                Library = "Math",
                NickName = "Add",
                ComponentGuid = guid1,
                InstanceGuid = guid2,
                Pivot = new CompactPosition(100.5f, 200.75f),
                Id = 1,
                Properties = new Dictionary<string, object> { { "key", "value" } },
                InputSettings = new List<ParameterSettings>
                {
                    new ParameterSettings { ParameterName = "A" }
                },
                OutputSettings = new List<ParameterSettings>
                {
                    new ParameterSettings { ParameterName = "Result" }
                },
                ComponentState = new ComponentState { Selected = true, Locked = true },
                Warnings = new List<string> { "Test warning" },
                Errors = new List<string> { "Test error" }
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ComponentProperties>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("Addition", deserialized.Name);
            Assert.Equal("Math", deserialized.Library);
            Assert.Equal("Add", deserialized.NickName);
            Assert.Equal(guid1, deserialized.ComponentGuid);
            Assert.NotNull(deserialized.InstanceGuid);
            Assert.Equal(guid2, deserialized.InstanceGuid.Value);
            Assert.Equal(100.5f, deserialized.Pivot.X);
            Assert.Equal(200.75f, deserialized.Pivot.Y);
            Assert.Equal(1, deserialized.Id);
            Assert.NotNull(deserialized.Properties);
            Assert.NotNull(deserialized.ComponentState);
            Assert.True(deserialized.ComponentState.Selected);
            Assert.NotNull(deserialized.InputSettings);
            Assert.Single(deserialized.InputSettings);
            Assert.NotNull(deserialized.OutputSettings);
            Assert.Single(deserialized.OutputSettings);
            Assert.NotNull(deserialized.ComponentState);
            Assert.True(deserialized.ComponentState.Locked);
            Assert.NotNull(deserialized.Warnings);
            Assert.Single(deserialized.Warnings);
            Assert.NotNull(deserialized.Errors);
            Assert.Single(deserialized.Errors);
        }

        [Fact]
        public void JsonSerialization_OmitsNullValues()
        {
            var props = new ComponentProperties
            {
                Name = "Test",
                Id = 1,
                Pivot = new CompactPosition(0, 0)
            };

            var json = JsonConvert.SerializeObject(props);

            Assert.DoesNotContain("\"library\"", json);
            Assert.DoesNotContain("\"type\"", json);
            Assert.DoesNotContain("\"nickName\"", json);
            Assert.DoesNotContain("\"selected\"", json);
            Assert.DoesNotContain("\"params\"", json);
            Assert.DoesNotContain("\"inputSettings\"", json);
            Assert.DoesNotContain("\"outputSettings\"", json);
            Assert.DoesNotContain("\"componentState\"", json);
            Assert.DoesNotContain("\"warnings\"", json);
            Assert.DoesNotContain("\"errors\"", json);
        }

        [Fact]
        public void JsonSerialization_RequiredProperties_AreSerialized()
        {
            var props = new ComponentProperties
            {
                Name = "Test",
                Id = 1,
                Pivot = new CompactPosition(0, 0)
            };

            var json = JsonConvert.SerializeObject(props);

            Assert.Contains("\"name\"", json);
            Assert.Contains("\"id\"", json);
        }
    }
}
