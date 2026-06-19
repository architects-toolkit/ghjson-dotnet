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

namespace GhJSON.Core.Tests.Validation
{
    /// <summary>
    /// Contract for <see cref="ValidationMessage"/> formatting and
    /// <see cref="ValidationResult"/> aggregation: paths appear in ToString when
    /// provided, HasErrors/HasWarnings reflect list contents, and factory helpers
    /// produce the expected IsValid value.
    /// </summary>
    public class ValidationMessageTests
    {
        [Fact]
        public void ToString_WithPath_PrependsPath()
        {
            var msg = new ValidationMessage("something went wrong", "components[2].id");

            Assert.Equal("components[2].id: something went wrong", msg.ToString());
        }

        [Fact]
        public void ToString_WithoutPath_ReturnsMessageOnly()
        {
            var msg = new ValidationMessage("bad");

            Assert.Equal("bad", msg.ToString());
        }

        [Fact]
        public void Success_Factory_ProducesValidResult()
        {
            var r = ValidationResult.Success();

            Assert.True(r.IsValid);
            Assert.False(r.HasErrors);
            Assert.False(r.HasWarnings);
        }

        [Fact]
        public void Failure_Factory_ProducesInvalidResultWithError()
        {
            var r = ValidationResult.Failure("boom", "path.x");

            Assert.False(r.IsValid);
            Assert.True(r.HasErrors);
            Assert.Single(r.Errors);
            Assert.Equal("path.x", r.Errors[0].Path);
        }

        [Fact]
        public void Validate_DuplicateIds_EmitsPath()
        {
            // Builder validates eagerly; construct directly to hit the validator.
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[]
                {
                    new GhJsonComponent { Name = "A", Id = 1 },
                    new GhJsonComponent { Name = "B", Id = 1 },
                },
                connections: null,
                groups: null);

            var result = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => !string.IsNullOrEmpty(e.Path));
        }
    }
}
