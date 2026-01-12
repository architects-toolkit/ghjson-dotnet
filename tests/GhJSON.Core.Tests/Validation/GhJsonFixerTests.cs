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
    public class GhJsonFixerTests
    {
        [Fact]
        public void FixComponentInstanceGuids_ValidGuids_NoChange()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    {
                        ""name"": ""Test"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890""
                    }
                ]
            }");

            var (fixedJson, mapping) = GhJsonFixer.FixComponentInstanceGuids(json);

            Assert.Empty(mapping);
            Assert.Equal("a1b2c3d4-e5f6-7890-abcd-ef1234567890", fixedJson["components"]![0]!["instanceGuid"]!.ToString());
        }

        [Fact]
        public void FixComponentInstanceGuids_InvalidGuids_AssignsNewGuids()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    {
                        ""name"": ""Test"",
                        ""instanceGuid"": ""invalid-guid""
                    }
                ]
            }");

            var (fixedJson, mapping) = GhJsonFixer.FixComponentInstanceGuids(json);

            Assert.Single(mapping);
            Assert.Contains("invalid-guid", mapping.Keys);
            Assert.True(Guid.TryParse(fixedJson["components"]![0]!["instanceGuid"]!.ToString(), out _));
        }

        [Fact]
        public void FixComponentInstanceGuids_MultipleInvalidGuids_AssignsUniqueGuids()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""instanceGuid"": ""comp1"" },
                    { ""name"": ""B"", ""instanceGuid"": ""comp2"" },
                    { ""name"": ""C"", ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"" }
                ]
            }");

            var (fixedJson, mapping) = GhJsonFixer.FixComponentInstanceGuids(json);

            Assert.Equal(2, mapping.Count);
            Assert.Contains("comp1", mapping.Keys);
            Assert.Contains("comp2", mapping.Keys);
            Assert.NotEqual(mapping["comp1"], mapping["comp2"]);
            // Third component should remain unchanged
            Assert.Equal("a1b2c3d4-e5f6-7890-abcd-ef1234567890", fixedJson["components"]![2]!["instanceGuid"]!.ToString());
        }

        [Fact]
        public void RemovePivotsIfIncomplete_AllHavePivots_KeepsPivots()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""pivot"": ""100,200"" },
                    { ""name"": ""B"", ""pivot"": ""300,400"" }
                ]
            }");

            var fixedJson = GhJsonFixer.RemovePivotsIfIncomplete(json);

            Assert.NotNull(fixedJson["components"]![0]!["pivot"]);
            Assert.NotNull(fixedJson["components"]![1]!["pivot"]);
        }

        [Fact]
        public void RemovePivotsIfIncomplete_SomeMissingPivots_RemovesAllPivots()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""pivot"": ""100,200"" },
                    { ""name"": ""B"" }
                ]
            }");

            var fixedJson = GhJsonFixer.RemovePivotsIfIncomplete(json);

            Assert.Null(fixedJson["components"]![0]!["pivot"]);
            Assert.Null(fixedJson["components"]![1]!["pivot"]);
        }

        [Fact]
        public void RemovePivotsIfIncomplete_InvalidPivotFormat_RemovesAllPivots()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""pivot"": ""100,200"" },
                    { ""name"": ""B"", ""pivot"": ""invalid"" }
                ]
            }");

            var fixedJson = GhJsonFixer.RemovePivotsIfIncomplete(json);

            Assert.Null(fixedJson["components"]![0]!["pivot"]);
            Assert.Null(fixedJson["components"]![1]!["pivot"]);
        }

        [Fact]
        public void RemovePivotsIfIncomplete_LegacyObjectFormat_KeepsPivots()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""pivot"": { ""X"": 100, ""Y"": 200 } },
                    { ""name"": ""B"", ""pivot"": { ""X"": 300, ""Y"": 400 } }
                ]
            }");

            var fixedJson = GhJsonFixer.RemovePivotsIfIncomplete(json);

            Assert.NotNull(fixedJson["components"]![0]!["pivot"]);
            Assert.NotNull(fixedJson["components"]![1]!["pivot"]);
        }

        [Fact]
        public void RemovePivotsIfIncomplete_MixedFormats_ValidPivots_KeepsPivots()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""pivot"": ""100,200"" },
                    { ""name"": ""B"", ""pivot"": { ""X"": 300, ""Y"": 400 } }
                ]
            }");

            var fixedJson = GhJsonFixer.RemovePivotsIfIncomplete(json);

            Assert.NotNull(fixedJson["components"]![0]!["pivot"]);
            Assert.NotNull(fixedJson["components"]![1]!["pivot"]);
        }

        [Fact]
        public void FixAll_AppliesAllFixes()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""instanceGuid"": ""invalid-guid"", ""pivot"": ""100,200"" },
                    { ""name"": ""B"", ""instanceGuid"": ""b1b2c3d4-e5f6-7890-abcd-ef1234567890"" }
                ]
            }");

            var (fixedJson, idMapping) = GhJsonFixer.FixAll(json);

            // Invalid GUID should be fixed
            Assert.Single(idMapping);
            Assert.Contains("invalid-guid", idMapping.Keys);
            Assert.True(Guid.TryParse(fixedJson["components"]![0]!["instanceGuid"]!.ToString(), out _));

            // Pivots should be removed (one component missing pivot)
            Assert.Null(fixedJson["components"]![0]!["pivot"]);
            Assert.Null(fixedJson["components"]![1]!["pivot"]);
        }

        [Fact]
        public void FixAll_ValidDocument_NoChanges()
        {
            var json = JObject.Parse(@"{
                ""components"": [
                    { ""name"": ""A"", ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"", ""pivot"": ""100,200"" },
                    { ""name"": ""B"", ""instanceGuid"": ""b1b2c3d4-e5f6-7890-abcd-ef1234567890"", ""pivot"": ""300,400"" }
                ]
            }");

            var (fixedJson, idMapping) = GhJsonFixer.FixAll(json);

            Assert.Empty(idMapping);
            Assert.NotNull(fixedJson["components"]![0]!["pivot"]);
            Assert.NotNull(fixedJson["components"]![1]!["pivot"]);
        }
    }
}
