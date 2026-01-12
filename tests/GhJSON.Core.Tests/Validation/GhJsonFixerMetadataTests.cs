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
using GhJSON.Core.Validation;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class GhJsonFixerMetadataTests
    {
        [Fact]
        public void PopulateMetadata_CreatesMetadataIfMissing()
        {
            var json = JObject.Parse(@"{
                ""components"": []
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            Assert.NotNull(fixedJson["metadata"]);
        }

        [Fact]
        public void PopulateMetadata_SetsCreatedDateIfMissing()
        {
            var json = JObject.Parse(@"{
                ""components"": []
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            Assert.NotNull(fixedJson["metadata"]!["created"]);
            var created = fixedJson["metadata"]!["created"]!.ToString();
            Assert.True(DateTime.TryParse(created, out _));
        }

        [Fact]
        public void PopulateMetadata_SetsModifiedDateIfMissing()
        {
            var json = JObject.Parse(@"{
                ""components"": []
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            Assert.NotNull(fixedJson["metadata"]!["modified"]);
            var modified = fixedJson["metadata"]!["modified"]!.ToString();
            Assert.True(DateTime.TryParse(modified, out _));
        }

        [Fact]
        public void PopulateMetadata_PreservesExistingCreatedDate()
        {
            var testDate = "2025-01-01T00:00:00Z";
            var json = JObject.Parse($@"{{
                ""metadata"": {{
                    ""created"": ""{testDate}""
                }},
                ""components"": []
            }}");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            // Verify the date was not overwritten - parse and check it's still 2025
            Assert.NotNull(fixedJson["metadata"]!["created"]);
            var createdDate = DateTime.Parse(fixedJson["metadata"]!["created"]!.ToString());
            Assert.Equal(2025, createdDate.Year);
            Assert.Equal(1, createdDate.Month);
            Assert.Equal(1, createdDate.Day);
        }

        [Fact]
        public void PopulateMetadata_PreservesExistingModifiedDate()
        {
            var testDate = "2025-01-01T00:00:00Z";
            var json = JObject.Parse($@"{{
                ""metadata"": {{
                    ""modified"": ""{testDate}""
                }},
                ""components"": []
            }}");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            // Verify the date was not overwritten - parse and check it's still 2025
            Assert.NotNull(fixedJson["metadata"]!["modified"]);
            var modifiedDate = DateTime.Parse(fixedJson["metadata"]!["modified"]!.ToString());
            Assert.Equal(2025, modifiedDate.Year);
            Assert.Equal(1, modifiedDate.Month);
            Assert.Equal(1, modifiedDate.Day);
        }

        [Fact]
        public void PopulateMetadata_SetsComponentCount()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""id"": 1 },
                    { ""name"": ""B"", ""id"": 2 },
                    { ""name"": ""C"", ""id"": 3 }
                ]
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            Assert.Equal(3, fixedJson["metadata"]!["componentCount"]!.Value<int>());
        }

        [Fact]
        public void PopulateMetadata_SetsConnectionCount()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""id"": 1 },
                    { ""name"": ""B"", ""id"": 2 }
                ],
                ""connections"": [
                    {
                        ""from"": { ""id"": 1, ""paramName"": ""R"" },
                        ""to"": { ""id"": 2, ""paramName"": ""A"" }
                    },
                    {
                        ""from"": { ""id"": 1, ""paramName"": ""R"" },
                        ""to"": { ""id"": 2, ""paramName"": ""B"" }
                    }
                ]
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            Assert.Equal(2, fixedJson["metadata"]!["connectionCount"]!.Value<int>());
        }

        [Fact]
        public void PopulateMetadata_SetsGroupCount()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""id"": 1 },
                    { ""name"": ""B"", ""id"": 2 }
                ],
                ""groups"": [
                    {
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""name"": ""Group 1"",
                        ""members"": [1, 2]
                    }
                ]
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            Assert.Equal(1, fixedJson["metadata"]!["groupCount"]!.Value<int>());
        }

        [Fact]
        public void PopulateMetadata_HandlesDocumentWithoutConnectionsOrGroups()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""id"": 1 },
                    { ""name"": ""B"", ""id"": 2 }
                ]
            }");

            var fixedJson = GhJsonFixer.PopulateMetadata(json);

            // Should not have connectionCount or groupCount if arrays don't exist
            Assert.Null(fixedJson["metadata"]!["connectionCount"]);
            Assert.Null(fixedJson["metadata"]!["groupCount"]);
        }

        [Fact]
        public void FixAll_PopulatesMetadataByDefault()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""id"": 1 }
                ]
            }");

            var (fixedJson, _) = GhJsonFixer.FixAll(json);

            Assert.NotNull(fixedJson["metadata"]);
            Assert.NotNull(fixedJson["metadata"]!["created"]);
            Assert.NotNull(fixedJson["metadata"]!["modified"]);
            Assert.Equal(1, fixedJson["metadata"]!["componentCount"]!.Value<int>());
        }

        [Fact]
        public void FixAll_CanSkipMetadataPopulation()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""id"": 1 }
                ]
            }");

            var (fixedJson, _) = GhJsonFixer.FixAll(json, populateMetadata: false);

            Assert.Null(fixedJson["metadata"]);
        }
    }
}
