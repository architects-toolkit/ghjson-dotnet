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
using System.Linq;
using GhJSON.Core;
using GhJSON.Core.FixOperations;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.FixOperations
{
    public class FixOperationsTests
    {
        [Fact]
        public void AssignMissingIds_AssignsIdsToComponents()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[]
                {
                    new GhJsonComponent { Name = "Addition" },
                    new GhJsonComponent { Name = "Subtraction" },
                },
                connections: null,
                groups: null);

            var result = GhJson.AssignMissingIds(doc);

            Assert.True(result.WasModified);
            Assert.All(result.Document.Components, c => Assert.NotNull(c.Id));
            Assert.True(result.Document.Components[0].Id.HasValue);
            Assert.True(result.Document.Components[1].Id.HasValue);
        }

        [Fact]
        public void ReassignIds_ReassignsAllIds()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 10 })
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 20 })
                .Build();

            var result = GhJson.ReassignIds(doc);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.Document.Components[0].Id);
            Assert.Equal(2, result.Document.Components[1].Id);
        }

        [Fact]
        public void GenerateMissingInstanceGuids_AssignsGuids()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 2 })
                .Build();

            var result = GhJson.GenerateMissingInstanceGuids(doc);

            Assert.True(result.WasModified);
            Assert.All(result.Document.Components, c => Assert.NotEqual(Guid.Empty, c.InstanceGuid));
        }

        [Fact]
        public void RegenerateInstanceGuids_RegeneratesAllGuids()
        {
            var originalGuid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1, InstanceGuid = originalGuid })
                .Build();

            var result = GhJson.RegenerateInstanceGuids(doc);

            Assert.True(result.WasModified);
            Assert.NotEqual(originalGuid, result.Document.Components[0].InstanceGuid);
        }

        [Fact]
        public void FixMetadata_UpdatesCounts()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "A" }
                })
                .Build();

            var result = GhJson.FixMetadata(doc);

            Assert.True(result.WasModified);
            Assert.NotNull(result.Document.Metadata);
            Assert.Equal(2, result.Document.Metadata.ComponentCount);
            Assert.Equal(1, result.Document.Metadata.ConnectionCount);
        }

        [Fact]
        public void FixMetadata_UpdatesModifiedTimestamp()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Modified = DateTime.UtcNow.AddDays(-1);

            var doc = GhJson.CreateDocumentBuilder()
                .WithMetadata(metadata)
                .Build();

            var originalModified = doc.Metadata.Modified;
            System.Threading.Thread.Sleep(10);
            
            var result = GhJson.FixMetadata(doc);

            Assert.True(result.WasModified);
            Assert.True(result.Document.Metadata.Modified > originalModified);
        }

        [Fact]
        public void Fix_WithDefaultOptions_AppliesAllFixes()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[]
                {
                    new GhJsonComponent { Name = "Addition" },
                    new GhJsonComponent { Name = "Subtraction" },
                },
                connections: null,
                groups: null);

            var result = GhJson.Fix(doc);

            Assert.True(result.WasModified);
            Assert.All(result.Document.Components, c => Assert.NotNull(c.Id));
            Assert.NotNull(result.Document.Metadata);
        }

        [Fact]
        public void Fix_WithCustomOptions_AppliesSelectedFixes()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 10 })
                .Build();

            var options = new FixOptions
            {
                AssignMissingIds = false,
                ReassignIds = true
            };
            
            var result = GhJson.Fix(doc, options);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.Document.Components[0].Id);
        }

        [Fact]
        public void Fix_UpdatesConnectionReferences()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 10 })
                .AddComponent(new GhJsonComponent { Name = "Panel", Id = 20 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 10, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 20, ParamName = "Value" }
                })
                .Build();

            var options = new FixOptions { ReassignIds = true };
            var result = GhJson.Fix(doc, options);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.Document.Connections[0].From.Id);
            Assert.Equal(2, result.Document.Connections[0].To.Id);
        }

        [Fact]
        public void Fix_UpdatesGroupReferences()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 10 })
                .AddGroup(new GhJsonGroup
                {
                    Id = 1,
                    Members = new System.Collections.Generic.List<int> { 10 }
                })
                .Build();

            var options = new FixOptions { ReassignIds = true };
            var result = GhJson.Fix(doc, options);

            Assert.True(result.WasModified);
            Assert.Contains(1, result.Document.Groups[0].Members);
        }
    }
}
