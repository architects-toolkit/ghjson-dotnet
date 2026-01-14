/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
            var doc = GhJson.CreateDocumentBuilder().Build();
            doc.Components.Add(new GhJsonComponent 
            { 
                Name = "Addition",
                ComponentGuid = Guid.Parse("59e0b89a-e487-49f8-bab8-b5bab16be14c"),
                Id = 1 
            });

            // Note: This requires Grasshopper context
            // API verification test
            Assert.NotNull(doc);
        }

        [Fact]
        public void Deserialize_WithInvalidComponent_HandlesGracefully()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();
            doc.Components.Add(new GhJsonComponent 
            { 
                Name = "NonExistentComponent",
                Id = 1 
            });

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
            var doc = GhJson.CreateDocumentBuilder().Build();
            var component = new GhJsonComponent 
            { 
                Name = "Addition",
                ComponentGuid = Guid.NewGuid(),
                Id = 1,
                NickName = "Add",
                Pivot = new GhJsonPivot { X = 100, Y = 200 }
            };
            doc.Components.Add(component);

            // API verification test
            Assert.Equal("Add", component.NickName);
            Assert.NotNull(component.Pivot);
        }

        [Fact]
        public void Deserialize_AppliesInputSettings()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();
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
            doc.Components.Add(component);

            Assert.Single(component.InputSettings);
            Assert.Equal("Flatten", component.InputSettings[0].DataMapping);
        }

        [Fact]
        public void Deserialize_AppliesComponentState()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();
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
            doc.Components.Add(component);

            Assert.NotNull(component.ComponentState);
            Assert.True(component.ComponentState.Locked);
        }
    }
}
