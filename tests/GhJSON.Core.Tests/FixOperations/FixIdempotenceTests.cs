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

using GhJSON.Core.FixOperations;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.FixOperations
{
    /// <summary>
    /// Applying default fixes to an already-fixed document must not produce
    /// further changes — i.e. <c>Fix(Fix(x))</c> is structurally equivalent to
    /// <c>Fix(x)</c>. Also verifies a fixed document passes validation.
    /// </summary>
    public class FixIdempotenceTests
    {
        private static GhJsonDocument BuildBrokenDocument()
        {
            return GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A" })          // missing Id
                .AddComponent(new GhJsonComponent { Name = "B", Id = 5 })  // has Id but no InstanceGuid
                .Build();
        }

        [Fact]
        public void Fix_TwiceWithDefaults_ProducesNoFurtherModification()
        {
            var doc = BuildBrokenDocument();

            var first = GhJson.Fix(doc);
            // Non-metadata fixes must no longer trigger; disable metadata-only fixes
            // (timestamps, counts) that would always fire on a freshly-fixed doc.
            var second = GhJson.Fix(
                first.Document,
                new FixOptions { FixMetadata = false });

            Assert.True(first.WasModified);
            Assert.False(
                second.WasModified,
                "Fix should be idempotent for non-metadata operations once structural issues are resolved.");
        }

        [Fact]
        public void Fix_ProducesValidDocument()
        {
            var doc = BuildBrokenDocument();

            var fix = GhJson.Fix(doc);

            Assert.True(
                GhJson.IsValid(fix.Document),
                $"Fixed document still invalid. Unfixed: {string.Join(";", fix.UnfixedIssues)}");
        }

        [Fact]
        public void AssignMissingIds_DoesNotRenumberExistingIds()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 42 })
                .AddComponent(new GhJsonComponent { Name = "B" }) // no Id
                .Build();

            var fix = GhJson.AssignMissingIds(doc);

            Assert.Equal(42, fix.Document.Components[0].Id);
            Assert.NotNull(fix.Document.Components[1].Id);
            Assert.NotEqual(42, fix.Document.Components[1].Id);
        }
    }
}
