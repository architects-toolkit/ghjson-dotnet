/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition" });
            doc.Components.Add(new GhJsonComponent { Name = "Subtraction" });

            var result = GhJson.AssignMissingIds(doc);

            Assert.True(result.WasModified);
            Assert.All(result.Document.Components, c => Assert.NotNull(c.Id));
            Assert.True(result.Document.Components[0].Id.HasValue);
            Assert.True(result.Document.Components[1].Id.HasValue);
        }

        [Fact]
        public void ReassignIds_ReassignsAllIds()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 10 });
            doc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 20 });

            var result = GhJson.ReassignIds(doc);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.Document.Components[0].Id);
            Assert.Equal(2, result.Document.Components[1].Id);
        }

        [Fact]
        public void GenerateMissingInstanceGuids_AssignsGuids()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });
            doc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 2 });

            var result = GhJson.GenerateMissingInstanceGuids(doc);

            Assert.True(result.WasModified);
            Assert.All(result.Document.Components, c => Assert.NotEqual(Guid.Empty, c.InstanceGuid));
        }

        [Fact]
        public void RegenerateInstanceGuids_RegeneratesAllGuids()
        {
            var doc = GhJson.CreateDocument();
            var originalGuid = Guid.NewGuid();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1, InstanceGuid = originalGuid });

            var result = GhJson.RegenerateInstanceGuids(doc);

            Assert.True(result.WasModified);
            Assert.NotEqual(originalGuid, result.Document.Components[0].InstanceGuid);
        }

        [Fact]
        public void FixMetadata_UpdatesCounts()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });
            doc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 2 });
            doc.Connections = new System.Collections.Generic.List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "A" }
                }
            };

            var result = GhJson.FixMetadata(doc);

            Assert.True(result.WasModified);
            Assert.NotNull(result.Document.Metadata);
            Assert.Equal(2, result.Document.Metadata.ComponentCount);
            Assert.Equal(1, result.Document.Metadata.ConnectionCount);
        }

        [Fact]
        public void FixMetadata_UpdatesModifiedTimestamp()
        {
            var doc = GhJson.CreateDocument();
            doc.Metadata = GhJson.CreateMetadataProperty();
            doc.Metadata.Modified = DateTime.UtcNow.AddDays(-1);

            var originalModified = doc.Metadata.Modified;
            System.Threading.Thread.Sleep(10);
            
            var result = GhJson.FixMetadata(doc);

            Assert.True(result.WasModified);
            Assert.True(result.Document.Metadata.Modified > originalModified);
        }

        [Fact]
        public void Fix_WithDefaultOptions_AppliesAllFixes()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition" });
            doc.Components.Add(new GhJsonComponent { Name = "Subtraction" });

            var result = GhJson.Fix(doc);

            Assert.True(result.WasModified);
            Assert.All(result.Document.Components, c => Assert.NotNull(c.Id));
            Assert.NotNull(result.Document.Metadata);
        }

        [Fact]
        public void Fix_WithCustomOptions_AppliesSelectedFixes()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 10 });

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
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 10 });
            doc.Components.Add(new GhJsonComponent { Name = "Panel", Id = 20 });
            doc.Connections = new System.Collections.Generic.List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 10, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 20, ParamName = "Value" }
                }
            };

            var options = new FixOptions { ReassignIds = true };
            var result = GhJson.Fix(doc, options);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.Document.Connections[0].From.Id);
            Assert.Equal(2, result.Document.Connections[0].To.Id);
        }

        [Fact]
        public void Fix_UpdatesGroupReferences()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 10 });
            doc.Groups = new System.Collections.Generic.List<GhJsonGroup>
            {
                new GhJsonGroup
                {
                    Id = 1,
                    Members = new System.Collections.Generic.List<int> { 10 }
                }
            };

            var options = new FixOptions { ReassignIds = true };
            var result = GhJson.Fix(doc, options);

            Assert.True(result.WasModified);
            Assert.Contains(1, result.Document.Groups[0].Members);
        }
    }
}
