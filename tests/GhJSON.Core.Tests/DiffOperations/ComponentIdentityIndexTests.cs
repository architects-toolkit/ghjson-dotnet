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
using GhJSON.Core.DiffOperations;
using GhJSON.Core.PatchModels;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.DiffOperations
{
    public class ComponentIdentityIndexTests
    {
        [Fact]
        public void TryMatch_ByInstanceGuid_WinsOverId()
        {
            var guid = Guid.NewGuid();
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid },
                new GhJsonComponent { Name = "B", Id = 1 }
            };

            var index = ComponentIdentityIndex.Build(components);
            var candidate = new GhJsonComponent { Name = "C", Id = 99, InstanceGuid = guid };

            var matched = index.TryMatch(candidate, out var match);

            Assert.True(matched);
            Assert.Equal("A", match!.Name);
        }

        [Fact]
        public void TryMatch_ById_WinsOverFingerprint()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, ComponentGuid = Guid.NewGuid() },
                new GhJsonComponent { Name = "A", Id = 2, ComponentGuid = Guid.NewGuid() }
            };

            var index = ComponentIdentityIndex.Build(components);
            var candidate = new GhJsonComponent { Name = "A", Id = 1, ComponentGuid = components[1].ComponentGuid };

            var matched = index.TryMatch(candidate, out var match);

            Assert.True(matched);
            Assert.Equal(1, match!.Id);
        }

        [Fact]
        public void TryMatch_ByFingerprint_RequiresUniqueMatch()
        {
            var guid = Guid.NewGuid();
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, ComponentGuid = guid },
                new GhJsonComponent { Name = "A", Id = 2, ComponentGuid = guid }
            };

            var index = ComponentIdentityIndex.Build(components);
            var candidate = new GhJsonComponent { Name = "A", Id = 99, ComponentGuid = guid };

            var matched = index.TryMatch(candidate, out var match);

            Assert.False(matched);
            Assert.Null(match);
        }

        [Fact]
        public void TryMatch_ByFingerprint_NonUnique_ReturnsFalse()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, Pivot = new GhJsonPivot { X = 0, Y = 0 } },
                new GhJsonComponent { Name = "A", Id = 2, Pivot = new GhJsonPivot { X = 0, Y = 0 } }
            };

            var index = ComponentIdentityIndex.Build(components);
            var candidate = new GhJsonComponent { Name = "A", Pivot = new GhJsonPivot { X = 0, Y = 0 } };

            var matched = index.TryMatch(candidate, out var match);

            Assert.False(matched);
            Assert.Null(match);
        }

        [Fact]
        public void TryMatch_Descriptor_ByInstanceGuid_ReturnsMatch()
        {
            var guid = Guid.NewGuid();
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid }
            };

            var index = ComponentIdentityIndex.Build(components);
            var descriptor = new GhPatchComponentMatch { InstanceGuid = guid };

            var matched = index.TryMatch(descriptor, out var match, out var count);

            Assert.True(matched);
            Assert.Equal(1, count);
            Assert.Equal("A", match!.Name);
        }

        [Fact]
        public void TryMatch_Descriptor_ById_ReturnsMatch()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1 }
            };

            var index = ComponentIdentityIndex.Build(components);
            var descriptor = new GhPatchComponentMatch { Id = 1 };

            var matched = index.TryMatch(descriptor, out var match, out var count);

            Assert.True(matched);
            Assert.Equal(1, count);
            Assert.Equal("A", match!.Name);
        }

        [Fact]
        public void TryMatch_Descriptor_ByComponentGuidNamePivot_FiltersCorrectly()
        {
            var guid = Guid.NewGuid();
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, ComponentGuid = guid, Pivot = new GhJsonPivot { X = 10, Y = 20 } },
                new GhJsonComponent { Name = "B", Id = 2, ComponentGuid = guid, Pivot = new GhJsonPivot { X = 30, Y = 40 } }
            };

            var index = ComponentIdentityIndex.Build(components);
            var descriptor = new GhPatchComponentMatch
            {
                ComponentGuid = guid,
                Name = "A",
                Pivot = new GhJsonPivot { X = 10, Y = 20 }
            };

            var matched = index.TryMatch(descriptor, out var match, out var count);

            Assert.True(matched);
            Assert.Equal(1, count);
            Assert.Equal("A", match!.Name);
        }

        [Fact]
        public void TryMatch_Descriptor_NoMatch_ReturnsFalse()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1 }
            };

            var index = ComponentIdentityIndex.Build(components);
            var descriptor = new GhPatchComponentMatch { Id = 999 };

            var matched = index.TryMatch(descriptor, out var match, out var count);

            Assert.False(matched);
            Assert.Equal(0, count);
            Assert.Null(match);
        }

        [Fact]
        public void TryMatch_Descriptor_AmbiguousMatch_ReturnsFalseWithCount()
        {
            var guid = Guid.NewGuid();
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1, ComponentGuid = guid },
                new GhJsonComponent { Name = "A", Id = 2, ComponentGuid = guid }
            };

            var index = ComponentIdentityIndex.Build(components);
            var descriptor = new GhPatchComponentMatch { ComponentGuid = guid };

            var matched = index.TryMatch(descriptor, out var match, out var count);

            Assert.False(matched);
            Assert.Equal(2, count);
            Assert.Null(match);
        }

        [Fact]
        public void ContainsInstanceGuid_WorksCorrectly()
        {
            var guid = Guid.NewGuid();
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", InstanceGuid = guid }
            };

            var index = ComponentIdentityIndex.Build(components);

            Assert.True(index.ContainsInstanceGuid(guid));
            Assert.False(index.ContainsInstanceGuid(Guid.NewGuid()));
        }

        [Fact]
        public void ContainsId_WorksCorrectly()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1 }
            };

            var index = ComponentIdentityIndex.Build(components);

            Assert.True(index.ContainsId(1));
            Assert.False(index.ContainsId(999));
        }

        [Fact]
        public void Build_WithEmptyList_IsValid()
        {
            var index = ComponentIdentityIndex.Build(new List<GhJsonComponent>());

            Assert.Empty(index.All);
            Assert.False(index.ContainsId(1));
            Assert.False(index.ContainsInstanceGuid(Guid.NewGuid()));
        }
    }
}
