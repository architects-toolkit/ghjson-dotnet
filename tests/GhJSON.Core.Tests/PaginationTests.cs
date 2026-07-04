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
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests
{
    public class PaginationTests
    {
        [Fact]
        public void SegmentDocument_NullDocument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => GhJson.SegmentDocument(null!, 0, 10));
        }

        [Fact]
        public void SegmentDocument_NegativePage_ThrowsArgumentOutOfRangeException()
        {
            var doc = CreateDocument(3);
            Assert.Throws<ArgumentOutOfRangeException>(() => GhJson.SegmentDocument(doc, -1, 10));
        }

        [Fact]
        public void SegmentDocument_ZeroPageSize_ThrowsArgumentOutOfRangeException()
        {
            var doc = CreateDocument(3);
            Assert.Throws<ArgumentOutOfRangeException>(() => GhJson.SegmentDocument(doc, 0, 0));
        }

        [Fact]
        public void SegmentDocument_PageOutOfRange_ReturnsEmptyComponents()
        {
            var doc = CreateDocument(5);
            var paged = GhJson.SegmentDocument(doc, 10, 10);

            Assert.NotNull(paged);
            Assert.Empty(paged.Components);
        }

        [Fact]
        public void SegmentDocument_ReturnsCorrectPage()
        {
            var doc = CreateDocument(10);
            var paged = GhJson.SegmentDocument(doc, 0, 3);

            Assert.NotNull(paged);
            Assert.Equal(3, paged.Components.Count);
            Assert.Equal(1, paged.Components[0].Id);
            Assert.Equal(3, paged.Components[2].Id);
        }

        [Fact]
        public void SegmentDocument_ReturnsCorrectSecondPage()
        {
            var doc = CreateDocument(10);
            var paged = GhJson.SegmentDocument(doc, 1, 3);

            Assert.NotNull(paged);
            Assert.Equal(3, paged.Components.Count);
            Assert.Equal(4, paged.Components[0].Id);
            Assert.Equal(6, paged.Components[2].Id);
        }

        [Fact]
        public void SegmentDocument_LastPageMayBePartial()
        {
            var doc = CreateDocument(10);
            var paged = GhJson.SegmentDocument(doc, 3, 3);

            Assert.NotNull(paged);
            Assert.Single(paged.Components);
            Assert.Equal(10, paged.Components[0].Id);
        }

        [Fact]
        public void SegmentDocument_KeepsInternalAndBoundaryConnections()
        {
            var components = Enumerable.Range(1, 5)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            var connections = new List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1 },
                    To = new GhJsonConnectionEndpoint { Id = 2 },
                },
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 2 },
                    To = new GhJsonConnectionEndpoint { Id = 3 },
                },
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 3 },
                    To = new GhJsonConnectionEndpoint { Id = 4 },
                },
            };

            var doc = new GhJsonDocument("1.0", null, components, connections, null);
            var paged = GhJson.SegmentDocument(doc, 0, 2);

            Assert.NotNull(paged);
            Assert.Equal(2, paged.Connections!.Count);
            Assert.Equal(1, paged.Connections[0].From.Id);
            Assert.Equal(2, paged.Connections[0].To.Id);
            Assert.Null(paged.Connections[0].Boundary);
            Assert.Equal(2, paged.Connections[1].From.Id);
            Assert.Equal(3, paged.Connections[1].To.Id);
            Assert.True(paged.Connections[1].Boundary);
        }

        [Fact]
        public void SegmentDocument_FiltersGroupsToPage()
        {
            var components = Enumerable.Range(1, 5)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            var groups = new List<GhJsonGroup>
            {
                new GhJsonGroup { Id = 1, Name = "Inside", Members = new List<int> { 1, 2 } },
                new GhJsonGroup { Id = 2, Name = "CrossPage", Members = new List<int> { 2, 3 } },
                new GhJsonGroup { Id = 3, Name = "Outside", Members = new List<int> { 3, 4 } },
            };

            var doc = new GhJsonDocument("1.0", null, components, null, groups);
            var paged = GhJson.SegmentDocument(doc, 0, 2);

            Assert.NotNull(paged);
            Assert.Single(paged.Groups);
            Assert.Equal("Inside", paged.Groups![0].Name);
        }

        [Fact]
        public void SegmentDocument_UpdatesMetadataCounts()
        {
            var components = Enumerable.Range(1, 5)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            var metadata = new GhJsonMetadata
            {
                Title = "Test",
                ComponentCount = 5,
                ConnectionCount = 0,
                GroupCount = 0,
            };

            var doc = new GhJsonDocument("1.0", metadata, components, null, null);
            var paged = GhJson.SegmentDocument(doc, 0, 2);

            Assert.NotNull(paged.Metadata);
            Assert.Equal("Test", paged.Metadata!.Title);
            Assert.Equal(5, paged.Metadata.ComponentCount);
            Assert.Equal(2, paged.Metadata.ConnectionCount);
            Assert.Equal(0, paged.Metadata.GroupCount);
        }

        [Fact]
        public void SegmentDocument_AddsPaginationMetadata()
        {
            var components = Enumerable.Range(1, 10)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            var doc = new GhJsonDocument("1.0", null, components, null, null);
            var paged = GhJson.SegmentDocument(doc, 1, 3);

            Assert.NotNull(paged.Metadata);
            Assert.NotNull(paged.Metadata!.Pagination);
            Assert.Equal(2, paged.Metadata.Pagination!.Page);
            Assert.Equal(3, paged.Metadata.Pagination.PageSize);
            Assert.Equal(4, paged.Metadata.Pagination.TotalPages);
            Assert.Equal(10, paged.Metadata.ComponentCount);
        }

        [Fact]
        public void SegmentDocument_SkipsPaginationWhenSinglePage()
        {
            var components = Enumerable.Range(1, 5)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            var doc = new GhJsonDocument("1.0", null, components, null, null);
            var paged = GhJson.SegmentDocument(doc, 0, 10);

            Assert.NotNull(paged.Metadata);
            Assert.Null(paged.Metadata!.Pagination);
            Assert.Equal(5, paged.Metadata.ComponentCount);
        }

        [Fact]
        public void SegmentDocument_PaginationMetadataOnEmptyPage()
        {
            var components = Enumerable.Range(1, 10)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            var doc = new GhJsonDocument("1.0", null, components, null, null);
            var paged = GhJson.SegmentDocument(doc, 10, 3);

            Assert.NotNull(paged.Metadata);
            Assert.NotNull(paged.Metadata!.Pagination);
            Assert.Equal(11, paged.Metadata.Pagination!.Page);
            Assert.Equal(3, paged.Metadata.Pagination.PageSize);
            Assert.Equal(4, paged.Metadata.Pagination.TotalPages);
            Assert.Equal(10, paged.Metadata.ComponentCount);
            Assert.Empty(paged.Components);
        }

        [Fact]
        public void Validator_BoundaryConnection_AllowedWithInfo()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "C1", Id = 1 },
            };

            var connections = new List<GhJsonConnection>
            {
                new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "A" },
                    To = new GhJsonConnectionEndpoint { Id = 99, ParamName = "B" },
                    Boundary = true,
                },
            };

            var doc = new GhJsonDocument("1.0", null, components, connections, null);
            var result = GhJsonValidator.Validate(doc);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Contains(result.Info, m => m.Message.Contains("boundary", StringComparison.OrdinalIgnoreCase));
        }

        private static GhJsonDocument CreateDocument(int componentCount)
        {
            var components = Enumerable.Range(1, componentCount)
                .Select(i => new GhJsonComponent { Name = $"C{i}", Id = i })
                .ToList();

            return new GhJsonDocument("1.0", null, components, null, null);
        }
    }
}
