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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class GhJsonDocumentTests
    {
        [Fact]
        public void ToJson_EmptyDocument_ReturnsValidJson()
        {
            var doc = new GhJsonDocument();
            
            var json = doc.ToJson();
            
            Assert.NotNull(json);
            Assert.Contains("\"components\"", json);
            Assert.Contains("\"connections\"", json);
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsDocument()
        {
            var json = @"{
                ""schemaVersion"": ""1.0"",
                ""components"": [
                    {
                        ""name"": ""Addition"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1,
                        ""pivot"": ""100,200""
                    }
                ],
                ""connections"": []
            }";
            
            var doc = GhJsonDocument.FromJson(json);
            
            Assert.NotNull(doc);
            Assert.Equal("1.0", doc.SchemaVersion);
            Assert.Single(doc.Components);
            Assert.Equal("Addition", doc.Components[0].Name);
            Assert.Equal(1, doc.Components[0].Id);
        }

        [Fact]
        public void GetIdToGuidMapping_ReturnsCorrectMapping()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var doc = new GhJsonDocument
            {
                Components = new System.Collections.Generic.List<ComponentProperties>
                {
                    new ComponentProperties { Name = "A", Id = 1, InstanceGuid = guid1, Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Name = "B", Id = 2, InstanceGuid = guid2, Pivot = new CompactPosition(0, 0) }
                }
            };
            
            var mapping = doc.GetIdToGuidMapping();
            
            Assert.Equal(2, mapping.Count);
            Assert.Equal(guid1, mapping[1]);
            Assert.Equal(guid2, mapping[2]);
        }

        [Fact]
        public void GetComponentConnections_ReturnsCorrectConnections()
        {
            var doc = new GhJsonDocument
            {
                Connections = new System.Collections.Generic.List<ConnectionPairing>
                {
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 1, ParamName = "R" },
                        To = new Connection { Id = 2, ParamName = "A" }
                    },
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 3, ParamName = "R" },
                        To = new Connection { Id = 4, ParamName = "A" }
                    }
                }
            };
            
            var connections = doc.GetComponentConnections(1);
            
            Assert.Single(connections);
            Assert.Equal(1, connections[0].From.Id);
        }

        [Fact]
        public void GetComponentsWithIssues_ReturnsComponentsWithWarningsOrErrors()
        {
            var doc = new GhJsonDocument
            {
                Components = new System.Collections.Generic.List<ComponentProperties>
                {
                    new ComponentProperties 
                    { 
                        Name = "A", 
                        Id = 1,
                        Pivot = new CompactPosition(0, 0),
                        Warnings = new System.Collections.Generic.List<string> { "Test warning" }
                    },
                    new ComponentProperties 
                    { 
                        Name = "B", 
                        Id = 2,
                        Pivot = new CompactPosition(0, 0)
                    }
                }
            };
            
            var issues = doc.GetComponentsWithIssues();
            
            Assert.Single(issues);
            Assert.Equal("A", issues[0].Name);
        }
    }
}
