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
using GhJSON.Core.DiffOperations;
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests.DiffOperations
{
    public class DocumentNormalizerTests
    {
        [Fact]
        public void Normalize_SortsComponentsByStableIdentity()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = Guid.NewGuid() })
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .Build();

            var normalised = DocumentNormalizer.Normalize(doc, DiffOptions.Default);
            var components = normalised["components"] as JArray;

            Assert.NotNull(components);
            Assert.Equal(2, components.Count);
            // Should be sorted by instanceGuid (string comparison)
            var firstGuid = (string?)components[0]["instanceGuid"];
            var secondGuid = (string?)components[1]["instanceGuid"];
            Assert.True(string.CompareOrdinal(firstGuid, secondGuid) <= 0);
        }

        [Fact]
        public void Normalize_SortsConnectionsCanonically()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 2, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "in" }
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" }
                })
                .Build();

            var normalised = DocumentNormalizer.Normalize(doc, DiffOptions.Default);
            var connections = normalised["connections"] as JArray;

            Assert.NotNull(connections);
            Assert.Equal(2, connections.Count);
            // First sorted by from.id
            Assert.Equal(1, (int?)connections[0]["from"]?["id"]);
            Assert.Equal(2, (int?)connections[1]["from"]?["id"]);
        }

        [Fact]
        public void Normalize_SortsGroupsAndMembers()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddGroup(new GhJsonGroup { Id = 2, InstanceGuid = Guid.NewGuid(), Members = new List<int> { 2, 1 } })
                .AddGroup(new GhJsonGroup { Id = 1, InstanceGuid = Guid.NewGuid(), Members = new List<int> { 1, 2 } })
                .Build();

            var normalised = DocumentNormalizer.Normalize(doc, DiffOptions.Default);
            var groups = normalised["groups"] as JArray;

            Assert.NotNull(groups);
            Assert.Equal(2, groups.Count);
            // Groups sorted by instanceGuid
            var firstGuid = (string?)groups[0]["instanceGuid"];
            var secondGuid = (string?)groups[1]["instanceGuid"];
            Assert.True(string.CompareOrdinal(firstGuid, secondGuid) <= 0);

            // Members sorted numerically
            var members = groups[0]["members"] as JArray;
            Assert.NotNull(members);
            Assert.True((int?)members[0] <= (int?)members[1]);
        }

        [Fact]
        public void Normalize_SortsObjectKeysDeterministically()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var normalised = DocumentNormalizer.Normalize(doc, DiffOptions.Default);
            var keys = normalised.Properties().Select(p => p.Name).ToList();

            // Keys should be in ordinal order
            for (int i = 0; i < keys.Count - 1; i++)
            {
                Assert.True(string.CompareOrdinal(keys[i], keys[i + 1]) <= 0);
            }
        }

        [Fact]
        public void Normalize_WithIgnoreRuntimeMessages_RemovesMessages()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "A",
                    Id = 1,
                    Errors = new List<string> { "error" },
                    Warnings = new List<string> { "warning" },
                    Remarks = new List<string> { "remark" }
                })
                .Build();

            var options = new DiffOptions { IgnoreRuntimeMessages = true };
            var normalised = DocumentNormalizer.Normalize(doc, options);
            var component = (normalised["components"] as JArray)?[0] as JObject;

            Assert.NotNull(component);
            Assert.Null(component["errors"]);
            Assert.Null(component["warnings"]);
            Assert.Null(component["remarks"]);
        }

        [Fact]
        public void Normalize_WithIgnorePivots_RemovesPivots()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, Pivot = new GhJsonPivot { X = 100, Y = 200 } })
                .Build();

            var options = new DiffOptions { IgnorePivots = true };
            var normalised = DocumentNormalizer.Normalize(doc, options);
            var component = (normalised["components"] as JArray)?[0] as JObject;

            Assert.NotNull(component);
            Assert.Null(component["pivot"]);
        }

        [Fact]
        public void ComputeChecksum_SameDocument_ProducesSameChecksum()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var checksum1 = DocumentNormalizer.ComputeChecksum(doc, DiffOptions.Default);
            var checksum2 = DocumentNormalizer.ComputeChecksum(doc, DiffOptions.Default);

            Assert.Equal(checksum1, checksum2);
        }

        [Fact]
        public void ComputeChecksum_DifferentDocument_ProducesDifferentChecksum()
        {
            var doc1 = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var doc2 = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "B", Id = 1 })
                .Build();

            var checksum1 = DocumentNormalizer.ComputeChecksum(doc1, DiffOptions.Default);
            var checksum2 = DocumentNormalizer.ComputeChecksum(doc2, DiffOptions.Default);

            Assert.NotEqual(checksum1, checksum2);
        }

        [Fact]
        public void ComputeChecksum_ProducesSha256Prefix()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var checksum = DocumentNormalizer.ComputeChecksum(doc, DiffOptions.Default);

            Assert.StartsWith("sha256-", checksum);
            Assert.Equal(71, checksum.Length); // "sha256-" + 64 hex chars
        }

        [Fact]
        public void Normalize_WithIgnoreMetadataCounters_RemovesCounters()
        {
            var metadata = new GhJsonMetadata
            {
                Title = "Test",
                ComponentCount = 5,
                ConnectionCount = 3,
                GroupCount = 1
            };

            var doc = GhJson.CreateDocumentBuilder()
                .WithMetadata(metadata)
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var options = new DiffOptions { IgnoreMetadataCounters = true };
            var normalised = DocumentNormalizer.Normalize(doc, options);
            var meta = normalised["metadata"] as JObject;

            Assert.NotNull(meta);
            Assert.Null(meta["componentCount"]);
            Assert.Null(meta["connectionCount"]);
            Assert.Null(meta["groupCount"]);
            Assert.Equal("Test", (string?)meta["title"]);
        }

        [Fact]
        public void Normalize_WithIgnoreMetadataTimestamps_RemovesTimestamps()
        {
            var metadata = new GhJsonMetadata
            {
                Title = "Test",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            var doc = GhJson.CreateDocumentBuilder()
                .WithMetadata(metadata)
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var options = new DiffOptions { IgnoreMetadataTimestamps = true };
            var normalised = DocumentNormalizer.Normalize(doc, options);
            var meta = normalised["metadata"] as JObject;

            Assert.NotNull(meta);
            Assert.Null(meta["created"]);
            Assert.Null(meta["modified"]);
            Assert.Equal("Test", (string?)meta["title"]);
        }
    }
}
