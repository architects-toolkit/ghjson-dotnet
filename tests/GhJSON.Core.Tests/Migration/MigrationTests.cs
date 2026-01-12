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

using GhJSON.Core.Migration;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests.Migration
{
    public class MigrationTests
    {
        #region MigrationPipeline Tests

        [Fact]
        public void Pipeline_MigratesDocument()
        {
            var json = @"{
                ""schemaVersion"": ""0.9"",
                ""components"": [
                    {
                        ""id"": 1,
                        ""name"": ""Addition"",
                        ""pivot"": { ""X"": 100, ""Y"": 200 }
                    }
                ],
                ""connections"": []
            }";

            var pipeline = MigrationPipeline.Default;
            var result = pipeline.Migrate(json);

            Assert.True(result.Success);
            Assert.False(result.WasModified); // no migrators registered yet
            Assert.NotNull(result.Document);
            Assert.Equal("1.0", result.ToVersion);
            Assert.Empty(result.Changes);
        }

        [Fact]
        public void Pipeline_NeedsMigration_ReturnsFalseWhenNoMigrators()
        {
            var json = @"{
                ""schemaVersion"": ""0.9"",
                ""components"": [
                    { ""id"": 1, ""pivot"": { ""X"": 0, ""Y"": 0 } }
                ]
            }";

            var jObject = JObject.Parse(json);
            var pipeline = MigrationPipeline.Default;

            Assert.False(pipeline.NeedsMigration(jObject));
        }

        [Fact]
        public void Pipeline_NeedsMigration_ReturnsFalseForCurrentSchema()
        {
            var json = @"{
                ""schemaVersion"": ""1.0"",
                ""components"": [
                    { ""id"": 1, ""pivot"": ""0,0"" }
                ],
                ""connections"": []
            }";

            var jObject = JObject.Parse(json);
            var pipeline = MigrationPipeline.Default;

            Assert.False(pipeline.NeedsMigration(jObject));
        }

        [Fact]
        public void Pipeline_ReturnsErrorForInvalidJson()
        {
            var invalidJson = "{ invalid json }";
            var pipeline = MigrationPipeline.Default;

            var result = pipeline.Migrate(invalidJson);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        #endregion
    }
}
