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
using System.Threading.Tasks;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class OnlineSchemaValidationTests
    {
        private const string ValidJson = "{\"components\":[{\"name\":\"Addition\",\"id\":1}]}";

        [Fact]
        public void Validate_WithPreferOnline_EndToEnd()
        {
            // With internet: downloads schemas from official repo and validates.
            // Without internet: falls back to embedded schemas. Either way should succeed.
            var result = GhJson.Validate(
                ValidJson,
                ValidationLevel.Standard,
                schemaVersion: "1.0",
                preferOnline: true);

            Assert.True(result.IsValid, $"Expected valid result, got errors: {string.Join("; ", result.Errors)}");
        }

        [Fact]
        public void Validate_WithSchemaVersion_DefaultIsValid()
        {
            var result = GhJson.Validate(ValidJson, ValidationLevel.Standard, schemaVersion: "1.0");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void IsValid_WithPreferOnline_FallsBackToEmbedded()
        {
            var valid = GhJson.IsValid(
                ValidJson,
                ValidationLevel.Standard,
                schemaVersion: "1.0",
                preferOnline: true);

            Assert.True(valid);
        }

        [Fact]
        public void IsValid_WithMessage_AndPreferOnline_ReturnsMessageOnFailure()
        {
            const string invalidJson = "{\"unknownProp\":true}";

            var valid = GhJson.IsValid(
                invalidJson,
                out string? message,
                ValidationLevel.Standard,
                schemaVersion: "1.0",
                preferOnline: true);

            Assert.False(valid);
            Assert.NotNull(message);
            Assert.NotEmpty(message);
        }

        [Fact]
        public void Validate_UnknownSchemaVersion_FallsBackOrThrows()
        {
            // An unknown version should fail to load and surface as a validation error.
            var result = GhJson.Validate(ValidJson, ValidationLevel.Standard, schemaVersion: "99.99");

            // The schema loader throws, which is caught and reported as a validation error.
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Message.Contains("Unable to load GhJSON schema", StringComparison.OrdinalIgnoreCase));
        }

        // ---- Async APIs ----

        [Fact]
        public async Task ValidateAsync_Document_EndToEnd()
        {
            var doc = GhJson.FromJson(ValidJson);
            var result = await GhJson.ValidateAsync(doc, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: false);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateAsync_String_EndToEnd()
        {
            var result = await GhJson.ValidateAsync(ValidJson, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: false);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateAsync_WithPreferOnline_FallsBack()
        {
            var result = await GhJson.ValidateAsync(
                ValidJson,
                ValidationLevel.Standard,
                schemaVersion: "1.0",
                preferOnline: true);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task IsValidAsync_Document_ReturnsTrue()
        {
            var doc = GhJson.FromJson(ValidJson);
            var valid = await GhJson.IsValidAsync(doc, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: false);

            Assert.True(valid);
        }

        [Fact]
        public async Task IsValidAsync_String_ReturnsFalseOnInvalid()
        {
            var valid = await GhJson.IsValidAsync("{}", ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: false);

            Assert.False(valid);
        }

    }
}
