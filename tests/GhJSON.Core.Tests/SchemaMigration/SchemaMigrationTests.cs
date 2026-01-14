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

using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaMigration
{
    public class SchemaMigrationTests
    {
        [Fact]
        public void MigrateSchema_CurrentVersion_NoMigrationNeeded()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.MigrateSchema(doc);

            Assert.True(result.Success);
            Assert.Equal(GhJson.CurrentVersion, result.Document.Schema);
        }

        [Fact]
        public void NeedsMigration_CurrentVersion_ReturnsFalse()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema(GhJson.CurrentVersion)
                .Build();

            Assert.False(GhJson.NeedsMigration(doc));
        }

        [Fact]
        public void NeedsMigration_OldVersion_ReturnsTrue()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("0.9")
                .Build();

            Assert.True(GhJson.NeedsMigration(doc));
        }

        [Fact]
        public void MigrateSchema_OldVersion_UpdatesSchema()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("0.9")
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.MigrateSchema(doc);

            Assert.True(result.Success);
            Assert.Equal(GhJson.CurrentVersion, result.Document.Schema);
        }

        [Fact]
        public void MigrateSchema_WithTargetVersion_MigratesToTarget()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("0.9")
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.MigrateSchema(doc, "1.0");

            Assert.True(result.Success);
            Assert.Equal("1.0", result.Document.Schema);
        }

        [Fact]
        public void MigrateSchema_PreservesData()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("0.9")
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    NickName = "Add",
                    Pivot = new GhJsonPivot { X = 100, Y = 200 }
                })
                .Build();

            var result = GhJson.MigrateSchema(doc);

            Assert.True(result.Success);
            Assert.Single(result.Document.Components);
            Assert.Equal("Addition", result.Document.Components[0].Name);
            Assert.Equal("Add", result.Document.Components[0].NickName);
            Assert.NotNull(result.Document.Components[0].Pivot);
        }

        [Fact]
        public void MigrateSchema_NullVersion_UsesCurrentVersion()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("0.9")
                .Build();

            var result = GhJson.MigrateSchema(doc, null);

            Assert.True(result.Success);
            Assert.Equal(GhJson.CurrentVersion, result.Document.Schema);
        }
    }
}
