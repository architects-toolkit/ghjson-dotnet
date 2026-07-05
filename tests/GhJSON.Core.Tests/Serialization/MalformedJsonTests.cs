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

using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    /// <summary>
    /// Robustness contract for <see cref="GhJson.Validate(string, ValidationLevel)"/>:
    /// malformed JSON must always return a non-null, invalid
    /// <see cref="ValidationResult"/> with at least one error — never throw.
    /// </summary>
    public class MalformedJsonTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{")]
        [InlineData("{\"components\":")]
        [InlineData("not json at all")]
        [InlineData("{\"components\":[{\"name\":}]}")]
        public void Validate_MalformedJson_ReturnsInvalid(string json)
        {
            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void IsValid_MalformedJson_ReportsMessage()
        {
            var ok = GhJson.IsValid("{not valid", out var message);

            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(message));
        }

        [Fact]
        public void Validate_WithUtf8Bom_ProducesDeterministicError()
        {
            // Newtonsoft.Json's string-based parser does not strip a leading BOM.
            // The validator must therefore surface a readable error rather than
            // throw. This documents the current contract; if BOM handling is
            // added later this test can be flipped to assert success.
            const string bom = "\uFEFF";
            var json = bom + @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }
    }
}
