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
using GhJSON.Core.MergeOperations;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.MergeOperations
{
    public class PageJoinerTests
    {
        [Fact]
        public void JoinPages_NoPages_ReturnsEmptyDocument()
        {
            var result = GhJson.JoinPages(Array.Empty<GhJsonDocument>());

            Assert.True(result.Success);
            Assert.NotNull(result.Document);
            Assert.Empty(result.Document.Components);
        }

        [Fact]
        public void JoinPages_NullPages_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => GhJson.JoinPages(null!));
        }

        [Fact]
        public void JoinPages_NullPageInCollection_ThrowsArgumentException()
        {
            var page = CreatePage(1, 1);
            Assert.Throws<ArgumentException>(() => GhJson.JoinPages(new[] { page, null! }));
        }

        [Fact]
        public void JoinPages_SinglePage_ReturnsPostProcessedDocument()
        {
            var page = CreatePage(1, 2, withPagination: true);

            var result = GhJson.JoinPages(new[] { page });

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Components.Count);
            Assert.Null(result.Document.Metadata!.Pagination);
            Assert.Equal(2, result.Document.Metadata.ComponentCount);
        }

        [Fact]
        public void JoinPages_TwoPages_CombinesComponents()
        {
            var page1 = CreatePage(1, 2);
            var page2 = CreatePage(3, 2);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Equal(4, result.Document.Components.Count);
            Assert.Contains(result.Document.Components, c => c.Id == 1);
            Assert.Contains(result.Document.Components, c => c.Id == 4);
        }

        [Fact]
        public void JoinPages_PreservesIdsAndInstanceGuids()
        {
            var page1 = CreatePage(1, 2);
            var page2 = CreatePage(3, 2);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.All(result.Document.Components, c =>
            {
                Assert.True(c.Id.HasValue);
                Assert.NotEqual(Guid.Empty, c.InstanceGuid);
            });
        }

        [Fact]
        public void JoinPages_DuplicateComponents_AreRemoved()
        {
            var page1 = CreatePage(1, 2);
            var page2 = CreatePage(1, 2);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Components.Count);
            Assert.Equal(2, result.DuplicateComponentsRemoved);
        }

        [Fact]
        public void JoinPages_BoundaryConnection_BecomesInternal()
        {
            var page1 = CreatePageWithConnection(
                new[] { 1, 2 },
                new[] { (1, 2, true) },
                withPagination: true);

            var page2 = CreatePageWithConnection(
                new[] { 3 },
                Array.Empty<(int, int, bool)>(),
                withPagination: true);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Single(result.Document.Connections);
            Assert.Null(result.Document.Connections![0].Boundary);
            Assert.Equal(1, result.BoundaryConnectionsResolved);
        }

        [Fact]
        public void JoinPages_DuplicateConnections_AreRemoved()
        {
            var page1 = CreatePageWithConnection(
                new[] { 1, 2 },
                new[] { (1, 2, false) },
                withPagination: true);

            var page2 = CreatePageWithConnection(
                new[] { 1, 2 },
                new[] { (1, 2, false) },
                withPagination: true);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Single(result.Document.Connections);
        }

        [Fact]
        public void JoinPages_DanglingNonBoundaryConnection_IsRemoved()
        {
            var page1 = CreatePageWithConnection(
                new[] { 1 },
                new[] { (1, 99, false) },
                withPagination: true);

            var result = GhJson.JoinPages(new[] { page1 });

            Assert.True(result.Success);
            Assert.Null(result.Document.Connections);
            Assert.Equal(1, result.DanglingConnectionsRemoved);
        }

        [Fact]
        public void JoinPages_StripsPaginationMetadata()
        {
            var page1 = CreatePage(1, 2, withPagination: true);
            var page2 = CreatePage(3, 2, withPagination: true);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Null(result.Document.Metadata!.Pagination);
        }

        [Fact]
        public void JoinPages_UpdatesMetadataCounts()
        {
            var page1 = CreatePageWithConnection(
                new[] { 1, 2 },
                new[] { (1, 2, true) },
                withPagination: true);

            var page2 = CreatePageWithConnection(
                new[] { 3 },
                Array.Empty<(int, int, bool)>(),
                withPagination: true);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Equal(3, result.Document.Metadata!.ComponentCount);
            Assert.Equal(1, result.Document.Metadata.ConnectionCount);
            Assert.Equal(0, result.Document.Metadata.GroupCount);
        }

        [Fact]
        public void JoinPages_CleansGroupsWithMissingMembers()
        {
            var page1 = CreatePageWithGroup(new[] { 1, 2 }, new[] { 1, 2 });
            var page2 = CreatePageWithGroup(new[] { 3 }, new[] { 2, 3 });

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Single(result.Document.Groups);
            var group = result.Document.Groups![0];
            Assert.Equal(new[] { 1, 2, 3 }, group.Members!.OrderBy(m => m));
        }

        [Fact]
        public void JoinPages_RemovesEmptyGroups()
        {
            var page1 = CreatePageWithGroup(new[] { 1 }, new[] { 1 }, groupId: 1);
            var page2 = CreatePageWithGroup(new[] { 2 }, new[] { 99 }, groupId: 2);

            var result = GhJson.JoinPages(new[] { page1, page2 });

            Assert.True(result.Success);
            Assert.Single(result.Document.Groups);
            Assert.Equal(1, result.EmptyGroupsRemoved);
        }

        [Fact]
        public void JoinPages_MultiplePages_JoinsSequentially()
        {
            var pages = Enumerable.Range(0, 3)
                .Select(i => CreatePage(i * 2 + 1, 2))
                .ToArray();

            var result = GhJson.JoinPages(pages);

            Assert.True(result.Success);
            Assert.Equal(6, result.Document.Components.Count);
        }

        private static GhJsonDocument CreatePage(int startId, int count, bool withPagination = false)
        {
            var components = Enumerable.Range(startId, count)
                .Select(i => new GhJsonComponent
                {
                    Name = $"C{i}",
                    Id = i,
                    InstanceGuid = Guid.NewGuid(),
                })
                .ToList();

            GhJsonMetadata? metadata = null;
            if (withPagination)
            {
                metadata = new GhJsonMetadata
                {
                    ComponentCount = count,
                    Pagination = new GhJsonPagination
                    {
                        Page = 1,
                        PageSize = 2,
                        TotalPages = 2,
                    },
                };
            }

            return new GhJsonDocument("1.0", metadata, components, null, null);
        }

        private static GhJsonDocument CreatePageWithConnection(
            int[] ids,
            IEnumerable<(int fromId, int toId, bool boundary)> connections,
            bool withPagination = false)
        {
            var components = ids.Select(i => new GhJsonComponent
            {
                Name = $"C{i}",
                Id = i,
                InstanceGuid = Guid.NewGuid(),
            }).ToList();

            var connectionList = connections.Select(c => new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = c.fromId },
                To = new GhJsonConnectionEndpoint { Id = c.toId },
                Boundary = c.boundary ? (bool?)true : null,
            }).ToList();

            GhJsonMetadata? metadata = null;
            if (withPagination)
            {
                metadata = new GhJsonMetadata
                {
                    ComponentCount = ids.Length,
                    Pagination = new GhJsonPagination
                    {
                        Page = 1,
                        PageSize = 2,
                        TotalPages = 2,
                    },
                };
            }

            return new GhJsonDocument("1.0", metadata, components, connectionList, null);
        }

        private static GhJsonDocument CreatePageWithGroup(int[] componentIds, int[] groupMembers, int groupId = 1)
        {
            var components = componentIds.Select(i => new GhJsonComponent
            {
                Name = $"C{i}",
                Id = i,
                InstanceGuid = Guid.NewGuid(),
            }).ToList();

            var groups = new List<GhJsonGroup>
            {
                new GhJsonGroup
                {
                    Id = groupId,
                    Name = "Group",
                    Members = new List<int>(groupMembers),
                },
            };

            return new GhJsonDocument("1.0", null, components, null, groups);
        }
    }
}
