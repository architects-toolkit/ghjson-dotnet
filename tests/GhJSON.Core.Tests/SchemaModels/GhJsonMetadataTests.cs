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
using System.Collections.Generic;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonMetadataTests
    {
        [Fact]
        public void CreateMetadataProperty_ReturnsEmptyMetadata()
        {
            var metadata = GhJson.CreateMetadataProperty();

            Assert.NotNull(metadata);
            Assert.Null(metadata.Title);
            Assert.Null(metadata.Description);
        }

        [Fact]
        public void Metadata_SupportsTitleAndDescription()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Title = "Test Definition";
            metadata.Description = "A test Grasshopper definition";

            Assert.Equal("Test Definition", metadata.Title);
            Assert.Equal("A test Grasshopper definition", metadata.Description);
        }

        [Fact]
        public void Metadata_SupportsVersioning()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Version = "3";
            metadata.RhinoVersion = "8.24";
            metadata.GrasshopperVersion = "1.0";

            Assert.Equal("3", metadata.Version);
            Assert.Equal("8.24", metadata.RhinoVersion);
            Assert.Equal("1.0", metadata.GrasshopperVersion);
        }

        [Fact]
        public void Metadata_SupportsAuthorAndTimestamps()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Author = "Test Author";
            metadata.Created = DateTime.UtcNow;
            metadata.Modified = DateTime.UtcNow.AddDays(1);

            Assert.Equal("Test Author", metadata.Author);
            Assert.NotNull(metadata.Created);
            Assert.NotNull(metadata.Modified);
        }

        [Fact]
        public void Metadata_SupportsTags()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Tags = new List<string> { "architecture", "parametric" };

            Assert.NotNull(metadata.Tags);
            Assert.Equal(2, metadata.Tags.Count);
            Assert.Contains("architecture", metadata.Tags);
        }

        [Fact]
        public void Metadata_SupportsDependencies()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.Dependencies = new List<string> { "Kangaroo", "LunchBox" };

            Assert.NotNull(metadata.Dependencies);
            Assert.Equal(2, metadata.Dependencies.Count);
            Assert.Contains("Kangaroo", metadata.Dependencies);
        }

        [Fact]
        public void Metadata_SupportsCounts()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.ComponentCount = 10;
            metadata.ConnectionCount = 15;
            metadata.GroupCount = 2;

            Assert.Equal(10, metadata.ComponentCount);
            Assert.Equal(15, metadata.ConnectionCount);
            Assert.Equal(2, metadata.GroupCount);
        }

        [Fact]
        public void Metadata_SupportsGeneratorInfo()
        {
            var metadata = GhJson.CreateMetadataProperty();
            metadata.GeneratorName = "GhJSON.NET";
            metadata.GeneratorVersion = "1.0.0";

            Assert.Equal("GhJSON.NET", metadata.GeneratorName);
            Assert.Equal("1.0.0", metadata.GeneratorVersion);
        }
    }
}
