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
using GhJSON.Core.Migration.Migrators;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests.Migration
{
    public class MigrationTests
    {
        #region V0_9_to_V1_0_PivotMigrator Tests

        [Fact]
        public void PivotMigrator_MigratesObjectToString()
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

            var jObject = JObject.Parse(json);
            var migrator = new V0_9_to_V1_0_PivotMigrator();

            Assert.True(migrator.CanMigrate(jObject));

            var result = migrator.Migrate(jObject);

            Assert.True(result.Success);
            Assert.True(result.WasModified);
            Assert.Equal(1, result.ChangeCount);
            Assert.Equal("100,200", jObject["components"]![0]!["pivot"]!.Value<string>());
        }

        [Fact]
        public void PivotMigrator_SkipsAlreadyStringPivot()
        {
            var json = @"{
                ""schemaVersion"": ""1.0"",
                ""components"": [
                    {
                        ""id"": 1,
                        ""name"": ""Addition"",
                        ""pivot"": ""100,200""
                    }
                ],
                ""connections"": []
            }";

            var jObject = JObject.Parse(json);
            var migrator = new V0_9_to_V1_0_PivotMigrator();

            Assert.False(migrator.CanMigrate(jObject));
        }

        [Fact]
        public void PivotMigrator_HandlesLowerCaseProperties()
        {
            var json = @"{
                ""components"": [
                    {
                        ""id"": 1,
                        ""pivot"": { ""x"": 50.5, ""y"": 100.5 }
                    }
                ]
            }";

            var jObject = JObject.Parse(json);
            var migrator = new V0_9_to_V1_0_PivotMigrator();
            var result = migrator.Migrate(jObject);

            Assert.True(result.WasModified);
            Assert.Equal("50.5,100.5", jObject["components"]![0]!["pivot"]!.Value<string>());
        }

        #endregion

        #region V0_9_to_V1_0_PropertyMigrator Tests

        [Fact]
        public void PropertyMigrator_RenamesLegacyProperties()
        {
            var json = @"{
                ""components"": [
                    {
                        ""id"": 1,
                        ""Name"": ""Addition"",
                        ""component_guid"": ""some-guid"",
                        ""instance_guid"": ""another-guid""
                    }
                ]
            }";

            var jObject = JObject.Parse(json);
            var migrator = new V0_9_to_V1_0_PropertyMigrator();

            Assert.True(migrator.CanMigrate(jObject));

            var result = migrator.Migrate(jObject);

            Assert.True(result.WasModified);
            Assert.NotNull(jObject["components"]![0]!["name"]);
            Assert.NotNull(jObject["components"]![0]!["componentGuid"]);
            Assert.NotNull(jObject["components"]![0]!["instanceGuid"]);
        }

        [Fact]
        public void PropertyMigrator_MigratesConnectionProperties()
        {
            var json = @"{
                ""components"": [],
                ""connections"": [
                    {
                        ""From"": { ""Id"": 1, ""ParamName"": ""Out"" },
                        ""To"": { ""Id"": 2, ""ParamName"": ""In"" }
                    }
                ]
            }";

            var jObject = JObject.Parse(json);
            var migrator = new V0_9_to_V1_0_PropertyMigrator();
            var result = migrator.Migrate(jObject);

            Assert.True(result.WasModified);
            var conn = jObject["connections"]![0];
            Assert.NotNull(conn!["from"]);
            Assert.NotNull(conn["to"]);
        }

        #endregion

        #region MigrationPipeline Tests

        [Fact]
        public void Pipeline_MigratesDocument()
        {
            var json = @"{
                ""schemaVersion"": ""0.9"",
                ""components"": [
                    {
                        ""id"": 1,
                        ""Name"": ""Addition"",
                        ""pivot"": { ""X"": 100, ""Y"": 200 }
                    }
                ],
                ""connections"": []
            }";

            var pipeline = MigrationPipeline.Default;
            var result = pipeline.Migrate(json);

            Assert.True(result.Success);
            Assert.True(result.WasModified);
            Assert.NotNull(result.Document);
            Assert.Equal("1.0", result.ToVersion);
            Assert.True(result.Changes.Count > 0);
        }

        [Fact]
        public void Pipeline_NeedsMigration_ReturnsTrueForOldSchema()
        {
            var json = @"{
                ""schemaVersion"": ""0.9"",
                ""components"": [
                    { ""id"": 1, ""pivot"": { ""X"": 0, ""Y"": 0 } }
                ]
            }";

            var jObject = JObject.Parse(json);
            var pipeline = MigrationPipeline.Default;

            Assert.True(pipeline.NeedsMigration(jObject));
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
