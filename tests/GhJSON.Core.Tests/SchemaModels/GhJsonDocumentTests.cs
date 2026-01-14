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

using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonDocumentTests
    {
        [Fact]
        public void CreateDocument_ReturnsEmptyDocument()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();

            Assert.NotNull(doc);
            Assert.NotNull(doc.Components);
            Assert.Empty(doc.Components);
            Assert.Equal(GhJson.CurrentVersion, doc.Schema);
        }

        [Fact]
        public void ToJson_SerializesDocument()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1
                })
                .Build();

            var json = GhJson.ToJson(doc);

            Assert.Contains("\"components\"", json);
            Assert.Contains("\"Addition\"", json);
        }

        [Fact]
        public void FromJson_DeserializesDocument()
        {
            var json = @"{
                ""schema"": ""1.0"",
                ""components"": [
                    { ""name"": ""Addition"", ""id"": 1 }
                ]
            }";

            var doc = GhJson.FromJson(json);

            Assert.NotNull(doc);
            Assert.Single(doc.Components);
            Assert.Equal("Addition", doc.Components[0].Name);
            Assert.Equal(1, doc.Components[0].Id);
        }
    }
}
