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

using GhJSON.Core.SchemaModels;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.SpecCompliance
{
    /// <summary>
    /// Exercises the <c>schema</c> version field and migration API to guarantee
    /// predictable behavior for current, unknown, and malformed versions.
    /// </summary>
    public class VersionAndMigrationTests
    {
        [Theory]
        [InlineData("1.0")]
        [InlineData("1.0.0")]
        public void ValidSemVer_Passes(string version)
        {
            var json = @"{""schema"":""" + version + @""",""components"":[{""name"":""Addition"",""id"":1}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.True(result.IsValid, string.Join(";", result.Errors));
        }

        [Theory]
        [InlineData("not-a-version")]
        [InlineData("v1.0")]
        [InlineData("1")]
        public void InvalidSchemaPattern_Fails(string version)
        {
            var json = @"{""schema"":""" + version + @""",""components"":[{""name"":""Addition"",""id"":1}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void MigrateSchema_SameVersion_IsNoOpAndSucceeds()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.MigrateSchema(doc, GhJson.CurrentVersion);

            Assert.True(result.Success);
            Assert.Equal(GhJson.CurrentVersion, result.Document.Schema);
        }

        [Fact]
        public void NeedsMigration_EmptyOrMissingSchema_ReturnsTrue()
        {
            // Document constructed directly (not through the builder) without a schema.
            var doc = new GhJsonDocument(
                schema: null,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "Addition", Id = 1 } },
                connections: null,
                groups: null);

            Assert.True(GhJson.NeedsMigration(doc));
        }
    }
}
