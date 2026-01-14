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
using Xunit;

namespace GhJSON.Grasshopper.Tests.Integration
{
    /// <summary>
    /// Integration tests for complete serialize-deserialize-serialize cycles.
    /// These tests verify that data integrity is maintained through the full pipeline.
    /// </summary>
    public class RoundTripTests
    {
        [Fact]
        public void RoundTrip_SimpleDocument_PreservesData()
        {
            // Create document
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    ComponentGuid = Guid.Parse("59e0b89a-e487-49f8-bab8-b5bab16be14c"),
                    InstanceGuid = Guid.NewGuid(),
                    Id = 1,
                    NickName = "Add",
                    Library = "Maths",
                    Pivot = new GhJsonPivot { X = 100, Y = 200 }
                })
                .Build();

            // Serialize
            var json = GhJson.ToJson(doc);
            
            // Deserialize
            var loadedDoc = GhJson.FromJson(json);

            // Verify
            Assert.Equal(doc.Schema, loadedDoc.Schema);
            Assert.Single(loadedDoc.Components);
            Assert.Equal("Addition", loadedDoc.Components[0].Name);
            Assert.Equal("Add", loadedDoc.Components[0].NickName);
            Assert.Equal("Maths", loadedDoc.Components[0].Library);
            Assert.Equal(1, loadedDoc.Components[0].Id);
            Assert.NotNull(loadedDoc.Components[0].Pivot);
        }

        [Fact]
        public void RoundTrip_WithConnections_PreservesData()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "Panel", Id = 2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result", ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "Value", ParamIndex = 0 }
                })
                .Build();

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.Single(loadedDoc.Connections);
            Assert.Equal(1, loadedDoc.Connections[0].From.Id);
            Assert.Equal(2, loadedDoc.Connections[0].To.Id);
            Assert.Equal("Result", loadedDoc.Connections[0].From.ParamName);
            Assert.Equal("Value", loadedDoc.Connections[0].To.ParamName);
        }

        [Fact]
        public void RoundTrip_WithGroups_PreservesData()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 2 })
                .AddGroup(new GhJsonGroup
                {
                    Id = 1,
                    InstanceGuid = Guid.NewGuid(),
                    Name = "Math Operations",
                    Color = "argb:255,128,64,32",
                    Members = new System.Collections.Generic.List<int> { 1, 2 }
                })
                .Build();

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.Single(loadedDoc.Groups);
            Assert.Equal("Math Operations", loadedDoc.Groups[0].Name);
            Assert.Equal(2, loadedDoc.Groups[0].Members.Count);
            Assert.Contains(1, loadedDoc.Groups[0].Members);
            Assert.Contains(2, loadedDoc.Groups[0].Members);
        }

        [Fact]
        public void RoundTrip_WithMetadata_PreservesData()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Title = "Test Definition";
            metadata.Description = "A test Grasshopper definition";
            metadata.Author = "Test Author";
            metadata.Version = "1";
            metadata.Tags = new System.Collections.Generic.List<string> { "test", "sample" };

            var doc = GhJson.CreateDocumentBuilder()
                .WithMetadata(metadata)
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.NotNull(loadedDoc.Metadata);
            Assert.Equal("Test Definition", loadedDoc.Metadata.Title);
            Assert.Equal("A test Grasshopper definition", loadedDoc.Metadata.Description);
            Assert.Equal("Test Author", loadedDoc.Metadata.Author);
            Assert.Equal(2, loadedDoc.Metadata.Tags.Count);
        }

        [Fact]
        public void RoundTrip_WithParameterSettings_PreservesData()
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
                    NickName = "First",
                    DataMapping = "Flatten",
                    Expression = "x * 2",
                    IsSimplified = true,
                    IsReversed = false
                }
            };
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(component)
                .Build();

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.Single(loadedDoc.Components[0].InputSettings);
            var param = loadedDoc.Components[0].InputSettings[0];
            Assert.Equal("A", param.ParameterName);
            Assert.Equal("First", param.NickName);
            Assert.Equal("Flatten", param.DataMapping);
            Assert.Equal("x * 2", param.Expression);
            Assert.True(param.IsSimplified);
        }

        [Fact]
        public void RoundTrip_WithInternalizedData_PreservesData()
        {
            var component = new GhJsonComponent 
            { 
                Name = "Number Slider",
                Id = 1
            };
            component.InputSettings = new System.Collections.Generic.List<GhJsonParameterSettings>
            {
                new GhJsonParameterSettings
                {
                    ParameterName = "Number",
                    InternalizedData = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>
                    {
                        ["{0}"] = new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["{0}(0)"] = "number:5.0"
                        }
                    }
                }
            };

            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(component)
                .Build();

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.Single(loadedDoc.Components[0].InputSettings);
            Assert.NotNull(loadedDoc.Components[0].InputSettings[0].InternalizedData);
            Assert.True(loadedDoc.Components[0].InputSettings[0].InternalizedData.ContainsKey("{0}"));
        }

        [Fact]
        public void RoundTrip_WithComponentState_PreservesData()
        {
            var component = new GhJsonComponent 
            { 
                Name = "Number Slider",
                Id = 1
            };
            component.ComponentState = new GhJsonComponentState
            {
                Selected = true,
                Locked = false,
                Hidden = false
            };

            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(component)
                .Build();

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.NotNull(loadedDoc.Components[0].ComponentState);
            Assert.True(loadedDoc.Components[0].ComponentState.Selected);
            Assert.False(loadedDoc.Components[0].ComponentState.Locked);
        }

        [Fact]
        public void RoundTrip_ComplexDocument_PreservesAllData()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Title = "Complex Definition";
            metadata.Author = "Test";

            var doc = GhJson.CreateDocumentBuilder()
                .WithMetadata(metadata)
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    Pivot = new GhJsonPivot { X = 100, Y = 100 }
                })
                .AddComponent(new GhJsonComponent
                {
                    Name = "Panel",
                    Id = 2,
                    Pivot = new GhJsonPivot { X = 300, Y = 100 }
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 }
                })
                .AddGroup(new GhJsonGroup
                {
                    Id = 1,
                    Name = "Group 1",
                    Members = new System.Collections.Generic.List<int> { 1, 2 }
                })
                .Build();

            // Validate original
            Assert.True(GhJson.IsValid(doc));
            
            // Round trip
            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            // Validate loaded
            Assert.True(GhJson.IsValid(loadedDoc));
            Assert.Equal(2, loadedDoc.Components.Count);
            Assert.Single(loadedDoc.Connections);
            Assert.Single(loadedDoc.Groups);
            Assert.NotNull(loadedDoc.Metadata);
        }
    }
}
