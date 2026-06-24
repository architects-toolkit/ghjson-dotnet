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
using System.IO;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests
{
    public class GhJsonFacadeTests
    {
        [Fact]
        public void FromFile_ReadsExistingFile()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var tempPath = Path.GetTempFileName();
            try
            {
                GhJson.ToFile(doc, tempPath);
                var loaded = GhJson.FromFile(tempPath);

                Assert.NotNull(loaded);
                Assert.Single(loaded.Components);
                Assert.Equal("A", loaded.Components[0].Name);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void FromFile_MissingFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => GhJson.FromFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json")));
        }

        [Fact]
        public void ToFile_CreatesFile()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var tempPath = Path.GetTempFileName();
            try
            {
                GhJson.ToFile(doc, tempPath);
                Assert.True(File.Exists(tempPath));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void FromStream_ReadsFromStream()
        {
            var json = "{\"schema\":\"1.0\",\"components\":[{\"name\":\"A\",\"id\":1}]}";
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                writer.Write(json);
            }

            stream.Position = 0;
            var doc = GhJson.FromStream(stream);

            Assert.NotNull(doc);
            Assert.Single(doc.Components);
            Assert.Equal("A", doc.Components[0].Name);
        }

        [Fact]
        public void PatchFromFile_AndPatchToFile_RoundTrip()
        {
            var patch = new Core.PatchModels.GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new Core.PatchModels.GhPatchBody
                {
                    Components = new Core.PatchModels.GhPatchComponentsOp
                    {
                        Add = new System.Collections.Generic.List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "A", Id = 1 }
                        }
                    }
                }
            };

            var tempPath = Path.GetTempFileName();
            try
            {
                GhJson.PatchToFile(patch, tempPath);
                var loaded = GhJson.PatchFromFile(tempPath);

                Assert.Equal("ghpatch", loaded.Kind);
                Assert.NotNull(loaded.Patch.Components);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ValidatePatch_WithDocumentOverload_ReturnsResult()
        {
            var patch = new Core.PatchModels.GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new Core.PatchModels.GhPatchBody()
            };

            var result = GhJson.ValidatePatch(patch);

            Assert.NotNull(result);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ApplyPatch_WithStringOverloads_Works()
        {
            var baseDoc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patchDoc = new Core.PatchModels.GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new Core.PatchModels.GhPatchBody
                {
                    Components = new Core.PatchModels.GhPatchComponentsOp
                    {
                        Add = new System.Collections.Generic.List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "B", Id = 2 }
                        }
                    }
                }
            };

            var baseJson = GhJson.ToJson(baseDoc);
            var patchJson = GhJson.PatchToJson(patchDoc);

            var result = GhJson.ApplyPatch(baseJson, patchJson);

            Assert.True(result.Success);
            Assert.Equal(2, result.Document.Components.Count);
        }

        [Fact]
        public void NeedsMigration_NullSchema_ReturnsTrue()
        {
            var doc = new GhJsonDocument(
                schema: null,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 1 } },
                connections: null,
                groups: null);

            Assert.True(GhJson.NeedsMigration(doc));
        }

        [Fact]
        public void NeedsMigration_EmptySchema_ReturnsTrue()
        {
            var doc = new GhJsonDocument(
                schema: "",
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 1 } },
                connections: null,
                groups: null);

            Assert.True(GhJson.NeedsMigration(doc));
        }

        [Fact]
        public void IsValid_ValidDocument_ReturnsTrue()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            Assert.True(GhJson.IsValid(doc));
        }

        [Fact]
        public void IsValid_InvalidDocument_ReturnsFalse()
        {
            var json = "{\"schema\":\"1.0\",\"components\":[{}]}";

            Assert.False(GhJson.IsValid(json));
        }

        [Fact]
        public void CurrentVersion_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrEmpty(GhJson.CurrentVersion));
        }

        [Fact]
        public void CreateDocumentBuilder_IsNewInstance()
        {
            var builder1 = GhJson.CreateDocumentBuilder();
            var builder2 = GhJson.CreateDocumentBuilder();

            Assert.NotSame(builder1, builder2);
        }
    }
}
