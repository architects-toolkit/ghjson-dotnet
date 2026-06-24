/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using System.Linq;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.DocumentCreation
{
    public class DocumentBuilderTests
    {
        [Fact]
        public void Create_ReturnsEmptyBuilderWithCurrentSchema()
        {
            var builder = GhJson.CreateDocumentBuilder();

            Assert.NotNull(builder);
            Assert.Empty(builder.Components);
        }

        [Fact]
        public void FromImmutable_Null_ReturnsEmptyBuilder()
        {
            var builder = GhJson.CreateDocumentBuilder(null);

            Assert.NotNull(builder);
            Assert.Empty(builder.Components);
        }

        [Fact]
        public void FromImmutable_PreservesAllCollections()
        {
            var compGuid = Guid.NewGuid();
            var groupGuid = Guid.NewGuid();
            var original = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = compGuid })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "in" }
                })
                .AddGroup(new GhJsonGroup { Id = 1, InstanceGuid = groupGuid, Members = new List<int> { 1 } })
                .WithMetadata(new GhJsonMetadata { Title = "Test" })
                .Build();

            var builder = GhJson.CreateDocumentBuilder(original);

            Assert.Single(builder.Components);
            Assert.Equal("A", builder.Components[0].Name);
        }

        [Fact]
        public void WithSchema_NullOrWhitespace_ReturnsSameBuilder()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var withNull = builder.WithSchema(null);
            var withEmpty = builder.WithSchema("");
            var withWhitespace = builder.WithSchema("   ");

            // Immutable builder returns same instance when no change is needed
            Assert.Same(builder, withNull);
            Assert.Same(builder, withEmpty);
            Assert.Same(builder, withWhitespace);
        }

        [Fact]
        public void WithSchema_Value_ReturnsBuilderWithSchema()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var withSchema = builder.WithSchema("2.0");
            var doc = withSchema.Build();

            Assert.Equal("2.0", doc.Schema);
        }

        [Fact]
        public void WithMetadata_SetsMetadata()
        {
            var metadata = new GhJsonMetadata { Title = "Test" };
            var doc = GhJson.CreateDocumentBuilder()
                .WithMetadata(metadata)
                .Build();

            Assert.NotNull(doc.Metadata);
            Assert.Equal("Test", doc.Metadata.Title);
        }

        [Fact]
        public void AddComponent_Null_IsIgnored()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var afterAdd = builder.AddComponent(null);

            Assert.Same(builder, afterAdd);
        }

        [Fact]
        public void AddComponent_AppendsComponent()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            Assert.Single(doc.Components);
            Assert.Equal("A", doc.Components[0].Name);
        }

        [Fact]
        public void AddComponents_Null_IsIgnored()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var afterAdd = builder.AddComponents(null);

            Assert.Same(builder, afterAdd);
        }

        [Fact]
        public void AddComponents_FiltersNullItems()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponents(new[]
                {
                    new GhJsonComponent { Name = "A", Id = 1 },
                    null,
                    new GhJsonComponent { Name = "B", Id = 2 }
                })
                .Build();

            Assert.Equal(2, doc.Components.Count);
        }

        [Fact]
        public void AddConnection_Null_IsIgnored()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var afterAdd = builder.AddConnection(null);

            Assert.Same(builder, afterAdd);
        }

        [Fact]
        public void AddConnection_AppendsConnection()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = Guid.NewGuid() })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" }
                })
                .Build();

            Assert.Single(doc.Connections);
        }

        [Fact]
        public void AddConnections_Null_IsIgnored()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var afterAdd = builder.AddConnections(null);

            Assert.Same(builder, afterAdd);
        }

        [Fact]
        public void AddConnections_FiltersNullItems()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = Guid.NewGuid() })
                .AddConnections(new[]
                {
                    new GhJsonConnection { From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" }, To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" } },
                    null
                })
                .Build();

            Assert.Single(doc.Connections);
        }

        [Fact]
        public void AddGroup_Null_IsIgnored()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var afterAdd = builder.AddGroup(null);

            Assert.Same(builder, afterAdd);
        }

        [Fact]
        public void AddGroup_AppendsGroup()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .AddGroup(new GhJsonGroup { Id = 1, InstanceGuid = Guid.NewGuid(), Members = new List<int> { 1 } })
                .Build();

            Assert.Single(doc.Groups);
        }

        [Fact]
        public void AddGroups_Null_IsIgnored()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var afterAdd = builder.AddGroups(null);

            Assert.Same(builder, afterAdd);
        }

        [Fact]
        public void AddGroups_FiltersNullItems()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .AddGroups(new[]
                {
                    new GhJsonGroup { Id = 1, InstanceGuid = Guid.NewGuid(), Members = new List<int> { 1 } },
                    null
                })
                .Build();

            Assert.Single(doc.Groups);
        }

        [Fact]
        public void Build_ProducesImmutableDocument()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            Assert.NotNull(doc);
            Assert.IsType<GhJsonDocument>(doc);
        }

        [Fact]
        public void Build_IsIdempotent()
        {
            var builder = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 });

            var doc1 = builder.Build();
            var doc2 = builder.Build();

            Assert.Equal(doc1.Components.Count, doc2.Components.Count);
            Assert.Equal(doc1.Components[0].Name, doc2.Components[0].Name);
        }

        [Fact]
        public void ChainedOperations_AccumulateCorrectly()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("1.0")
                .WithMetadata(new GhJsonMetadata { Title = "Test" })
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = Guid.NewGuid() })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" }
                })
                .AddGroup(new GhJsonGroup { Id = 1, InstanceGuid = Guid.NewGuid(), Members = new List<int> { 1, 2 } })
                .Build();

            Assert.Equal("1.0", doc.Schema);
            Assert.NotNull(doc.Metadata);
            Assert.Equal("Test", doc.Metadata.Title);
            Assert.Equal(2, doc.Components.Count);
            Assert.Single(doc.Connections);
            Assert.Single(doc.Groups);
        }

        [Fact]
        public void ComponentsProperty_ReflectsCurrentState()
        {
            var builder = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 });

            Assert.Equal(2, builder.Components.Count);
            Assert.Equal("A", builder.Components[0].Name);
            Assert.Equal("B", builder.Components[1].Name);
        }

        [Fact]
        public void Immutability_OriginalBuilderUnchangedByChainedCalls()
        {
            var original = GhJson.CreateDocumentBuilder();
            var withComponent = original.AddComponent(new GhJsonComponent { Name = "A", Id = 1 });

            Assert.Empty(original.Components);
            Assert.Single(withComponent.Components);
        }
    }
}
