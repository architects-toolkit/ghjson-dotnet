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

using GhJSON.Core.Models.Document;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class DocumentMetadataTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var metadata = new DocumentMetadata();

            Assert.Null(metadata.Title);
            Assert.Null(metadata.Description);
            Assert.Null(metadata.Version);
            Assert.Null(metadata.Author);
            Assert.Null(metadata.Created);
            Assert.Null(metadata.Modified);
            Assert.Null(metadata.RhinoVersion);
            Assert.Null(metadata.GrasshopperVersion);
            Assert.Null(metadata.Tags);
            Assert.Null(metadata.Dependencies);
            Assert.Null(metadata.ComponentCount);
            Assert.Null(metadata.ConnectionCount);
            Assert.Null(metadata.GroupCount);
            Assert.Null(metadata.GeneratorName);
            Assert.Null(metadata.GeneratorVersion);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesAllProperties()
        {
            var original = new DocumentMetadata
            {
                Title = "Test Definition",
                Description = "A test Grasshopper definition",
                Version = "1.0",
                Author = "Test Author",
                Created = "2026-01-10",
                Modified = "2026-01-11",
                RhinoVersion = "8.0",
                GrasshopperVersion = "1.0",
                Tags = new System.Collections.Generic.List<string> { "test", "example", "demo" },
                Dependencies = new System.Collections.Generic.List<string> { "Plugin1", "Plugin2" },
                ComponentCount = 10,
                ConnectionCount = 15,
                GroupCount = 2,
                GeneratorName = "SmartHopper",
                GeneratorVersion = "1.2.4"
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<DocumentMetadata>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("Test Definition", deserialized.Title);
            Assert.Equal("A test Grasshopper definition", deserialized.Description);
            Assert.Equal("1.0", deserialized.Version);
            Assert.Equal("Test Author", deserialized.Author);
            Assert.Equal("2026-01-10", deserialized.Created);
            Assert.Equal("2026-01-11", deserialized.Modified);
            Assert.Equal("8.0", deserialized.RhinoVersion);
            Assert.Equal("1.0", deserialized.GrasshopperVersion);
            Assert.NotNull(deserialized.Tags);
            Assert.Equal(3, deserialized.Tags.Count);
            Assert.Contains("test", deserialized.Tags);
            Assert.Contains("example", deserialized.Tags);
            Assert.Contains("demo", deserialized.Tags);
            Assert.NotNull(deserialized.Dependencies);
            Assert.Equal(2, deserialized.Dependencies.Count);
            Assert.Contains("Plugin1", deserialized.Dependencies);
            Assert.Contains("Plugin2", deserialized.Dependencies);
            Assert.Equal(10, deserialized.ComponentCount);
            Assert.Equal(15, deserialized.ConnectionCount);
            Assert.Equal(2, deserialized.GroupCount);
            Assert.Equal("SmartHopper", deserialized.GeneratorName);
            Assert.Equal("1.2.4", deserialized.GeneratorVersion);
        }

        [Fact]
        public void JsonSerialization_OmitsNullValues()
        {
            var metadata = new DocumentMetadata
            {
                Description = "Test"
            };

            var json = JsonConvert.SerializeObject(metadata);

            Assert.Contains("\"description\"", json);
            Assert.DoesNotContain("\"title\"", json);
            Assert.DoesNotContain("\"version\"", json);
            Assert.DoesNotContain("\"author\"", json);
            Assert.DoesNotContain("\"created\"", json);
            Assert.DoesNotContain("\"modified\"", json);
            Assert.DoesNotContain("\"rhinoVersion\"", json);
            Assert.DoesNotContain("\"grasshopperVersion\"", json);
            Assert.DoesNotContain("\"tags\"", json);
            Assert.DoesNotContain("\"dependencies\"", json);
            Assert.DoesNotContain("\"componentCount\"", json);
            Assert.DoesNotContain("\"connectionCount\"", json);
            Assert.DoesNotContain("\"groupCount\"", json);
            Assert.DoesNotContain("\"plugin\"", json);
            Assert.DoesNotContain("\"pluginVersion\"", json);
        }
    }
}
