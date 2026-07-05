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

namespace GhJSON.Core.Tests.SpecCompliance
{
    /// <summary>
    /// Asserts that removing required fields from an otherwise valid document
    /// produces a validation error. Documents the structural contract enforced
    /// by the v1.0 schema + structural validator.
    /// </summary>
    public class RequiredFieldsTests
    {
        [Fact]
        public void Components_IsRequired_AtRoot()
        {
            // Missing "components" key entirely.
            const string json = @"{""schema"":""1.0""}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Component_WithoutNameAndComponentGuid_IsInvalid()
        {
            const string json = @"{""schema"":""1.0"",""components"":[{""id"":1}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Component_WithoutIdAndInstanceGuid_IsInvalid()
        {
            const string json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition""}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Connection_WithoutFromEndpoint_IsInvalid()
        {
            // "to" present but "from" missing.
            const string json =
                @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]," +
                @"""connections"":[{""to"":{""id"":1,""paramName"":""A""}}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ConnectionEndpoint_WithoutId_IsInvalid()
        {
            const string json =
                @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]," +
                @"""connections"":[{""from"":{""paramName"":""R""},""to"":{""id"":1,""paramName"":""A""}}]}";

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }
    }
}
