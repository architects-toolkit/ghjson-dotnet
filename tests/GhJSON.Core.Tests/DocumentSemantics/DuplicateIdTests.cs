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

using System.Linq;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.DocumentSemantics
{
    /// <summary>
    /// Component IDs must be unique and positive within a single document. These
    /// tests bypass <see cref="GhJson.DocumentBuilder"/> (which eagerly validates)
    /// by constructing <see cref="GhJsonDocument"/> directly, so the validator's
    /// behavior on structurally broken documents can be exercised.
    /// </summary>
    public class DuplicateIdTests
    {
        [Fact]
        public void DuplicateComponentIds_ProduceValidationError()
        {
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
            Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate"));
        }

        [Fact]
        public void ZeroOrNegativeId_ProducesValidationError()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 0 } },
                connections: null,
                groups: null);

            var result = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void AllUniqueIds_Pass()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddComponent(new GhJsonComponent { Name = "C", Id = 3 })
                .Build();

            var result = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.True(
                result.IsValid,
                string.Join("; ", result.Errors.Select(e => e.ToString())));
        }
    }
}
