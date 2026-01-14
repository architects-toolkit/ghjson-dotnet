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
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 1 })
                .Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Components.Count);
        }

        [Fact]
        public void Merge_ReassignsIncomingIds()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 1 })
                .Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(1, result.Document.Components[0].Id);
            Assert.Equal(2, result.Document.Components[1].Id);
        }

        [Fact]
        public void Merge_CombinesConnections()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 }
                })
                .Build();

            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 1 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 }
                })
                .Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Connections.Count);
        }

        [Fact]
        public void Merge_UpdatesConnectionReferences()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Panel", Id = 1 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Value" }
                })
                .Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Connections[0].From.Id);
            Assert.Equal(2, result.Document.Connections[0].To.Id);
        }

        [Fact]
        public void Merge_CombinesGroups()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddGroup(new GhJsonGroup { Id = 1, Members = new System.Collections.Generic.List<int> { 1 } })
                .Build();

            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 1 })
                .AddGroup(new GhJsonGroup { Id = 1, Members = new System.Collections.Generic.List<int> { 1 } })
                .Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Groups.Count);
        }

        [Fact]
        public void Merge_WithOptions_AppliesCustomBehavior()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Subtraction", Id = 1 })
                .Build();

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
            
            var incomingDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Single(result.Document.Components);
        }

        [Fact]
        public void Merge_EmptyIncoming_ReturnsBase()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();
            
            var incomingDoc = GhJson.CreateDocumentBuilder().Build();

            var result = GhJson.Merge(baseDoc, incomingDoc);

            Assert.True(result.Success);
            Assert.Single(result.Document.Components);
        }
    }
}
