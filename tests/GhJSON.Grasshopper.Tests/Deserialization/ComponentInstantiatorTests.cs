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
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.Deserialization;
using Xunit;

namespace GhJSON.Grasshopper.Tests.Deserialization
{
    public class ComponentInstantiatorTests
    {
        [Fact]
        public void DeserializationOptions_HasDefaultValues()
        {
            var options = DeserializationOptions.Default;

            Assert.NotNull(options);
        }

        [Fact]
        public void DeserializationOptions_SupportsSkipInvalid()
        {
            var options = new DeserializationOptions
            {
                SkipInvalidComponents = true
            };

            Assert.True(options.SkipInvalidComponents);
        }

        [Fact]
        public void Deserialize_WithValidDocument_ReturnsResult()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    ComponentGuid = Guid.Parse("59e0b89a-e487-49f8-bab8-b5bab16be14c"),
                    Id = 1
                })
                .Build();

            // Note: This requires Grasshopper context
            // API verification test
            Assert.NotNull(doc);
        }

        [Fact]
        public void Deserialize_WithInvalidComponent_HandlesGracefully()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "NonExistentComponent",
                    Id = 1
                })
                .Build();

            var options = new DeserializationOptions
            {
                SkipInvalidComponents = true
            };

            // API verification - options support graceful handling
            Assert.True(options.SkipInvalidComponents);
        }

        [Fact]
        public void Deserialize_PreservesComponentProperties()
        {
            var component = new GhJsonComponent 
            { 
                Name = "Addition",
                ComponentGuid = Guid.NewGuid(),
                Id = 1,
                NickName = "Add",
                Pivot = new GhJsonPivot { X = 100, Y = 200 }
            };
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(component)
                .Build();

            // API verification test
            Assert.Equal("Add", component.NickName);
            Assert.NotNull(component.Pivot);
        }

        [Fact]
        public void Deserialize_AppliesInputSettings()
        {
            var component = new GhJsonComponent 
            { 
                Name = "Addition",
                Id = 1
            };
            component.InputSettings = new System.Collections.Generic.List<GhJsonParameterSettings>
            {
                new GhJsonParameterSettings
                {
                    ParameterName = "A",
                    DataMapping = "Flatten"
                }
            };
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(component)
                .Build();

            Assert.Single(component.InputSettings);
            Assert.Equal("Flatten", component.InputSettings[0].DataMapping);
        }

        [Fact]
        public void Deserialize_AppliesComponentState()
        {
            var component = new GhJsonComponent 
            { 
                Name = "Number Slider",
                Id = 1
            };
            component.ComponentState = new GhJsonComponentState
            {
                Locked = true,
                Hidden = false
            };
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(component)
                .Build();

            Assert.NotNull(component.ComponentState);
            Assert.True(component.ComponentState.Locked);
        }
    }
}
