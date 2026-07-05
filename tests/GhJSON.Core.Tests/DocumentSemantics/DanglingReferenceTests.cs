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
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.DocumentSemantics
{
    /// <summary>
    /// Connections and group members that reference missing component IDs must
    /// either fail validation or be corrected by <see cref="DocumentFixer"/>.
    /// </summary>
    public class DanglingReferenceTests
    {
        private static GhJsonDocument DocWithDanglingConnection()
        {
            return new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 1 } },
                connections: new[]
                {
                    new GhJsonConnection
                    {
                        From = new GhJsonConnectionEndpoint { Id = 99, ParamName = "X" },
                        To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "A" },
                    },
                },
                groups: null);
        }

        [Fact]
        public void ConnectionReferencingMissingComponent_FailsValidation()
        {
            var result = GhJson.Validate(DocWithDanglingConnection(), ValidationLevel.Standard);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Message.Contains("non-existent"));
        }

        [Fact]
        public void GroupMemberReferencingMissingComponent_FailsValidation()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 1 } },
                connections: null,
                groups: new[] { new GhJsonGroup { Id = 10, Members = { 1, 42 } } });

            var result = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Fix_RemovesDanglingConnections_WhenConfigured()
        {
            var doc = DocWithDanglingConnection();

            var fix = GhJson.Fix(doc, new FixOptions
            {
                RemoveInvalidConnections = true,
                AssignMissingIds = false,
                GenerateMissingInstanceGuids = false,
                FixMetadata = false,
            });

            Assert.True(fix.WasModified);
            // After fix, validation must succeed (no more dangling connection).
            Assert.True(GhJson.IsValid(fix.Document, ValidationLevel.Standard));
        }
    }
}
