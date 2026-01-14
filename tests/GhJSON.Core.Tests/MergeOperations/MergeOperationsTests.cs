/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 */

using System.Linq;
using GhJSON.Core;
using GhJSON.Core.MergeOperations;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.MergeOperations
{
    public class MergeOperationsTests
    {
        [Fact]
        public void Merge_TwoDocuments_CombinesComponents()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 1 });

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Components.Count);
        }

        [Fact]
        public void Merge_ReassignsIncomingIds()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 1 });

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(1, result.Document.Components[0].Id);
            Assert.Equal(2, result.Document.Components[1].Id);
        }

        [Fact]
        public void Merge_CombinesConnections()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });
            baseDoc.Connections = new System.Collections.Generic.List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 }
                }
            };

            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 1 });
            incomingDoc.Connections = new System.Collections.Generic.List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 }
                }
            };

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Connections.Count);
        }

        [Fact]
        public void Merge_UpdatesConnectionReferences()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Panel", Id = 1 });
            incomingDoc.Connections = new System.Collections.Generic.List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Value" }
                }
            };

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Connections[0].From.Id);
            Assert.Equal(2, result.Document.Connections[0].To.Id);
        }

        [Fact]
        public void Merge_CombinesGroups()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });
            baseDoc.Groups = new System.Collections.Generic.List<GhJsonGroup>
            {
                new GhJsonGroup { Id = 1, Members = new System.Collections.Generic.List<int> { 1 } }
            };

            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 1 });
            incomingDoc.Groups = new System.Collections.Generic.List<GhJsonGroup>
            {
                new GhJsonGroup { Id = 1, Members = new System.Collections.Generic.List<int> { 1 } }
            };

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Groups.Count);
        }

        [Fact]
        public void Merge_WithOptions_AppliesCustomBehavior()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Subtraction", Id = 1 });

            var options = new MergeOptions
            {
                RegenerateIds = true,
                RegenerateInstanceGuids = true
            };

            var result = GhJson.Merge(baseDoc, incomingDoc, options);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Components.Count);
        }

        [Fact]
        public void Merge_EmptyBase_ReturnsIncoming()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            
            var incomingDoc = GhJson.CreateDocumentBuilder().Build();
            incomingDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Single(result.Document.Components);
        }

        [Fact]
        public void Merge_EmptyIncoming_ReturnsBase()
        {
            var baseDoc = GhJson.CreateDocumentBuilder().Build();
            baseDoc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });
            
            var incomingDoc = GhJson.CreateDocumentBuilder().Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Single(result.Document.Components);
        }
    }
}
