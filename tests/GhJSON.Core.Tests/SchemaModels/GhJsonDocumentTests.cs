/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
            var doc = GhJson.CreateDocumentBuilder().Build();
            doc.Components.Add(new GhJsonComponent
            {
                Name = "Addition",
                Id = 1
            });

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
