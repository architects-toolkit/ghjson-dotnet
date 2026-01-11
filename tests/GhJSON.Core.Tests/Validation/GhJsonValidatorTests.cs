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

using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class GhJsonValidatorTests
    {
        [Fact]
        public void Validate_ValidDocument_ReturnsTrue()
        {
            var json = @"{
                ""schemaVersion"": ""1.0"",
                ""components"": [
                    {
                        ""name"": ""Addition"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""componentGuid"": ""d1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1,
                        ""pivot"": ""100,200""
                    }
                ],
                ""connections"": []
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.True(result);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void Validate_EmptyJson_ReturnsFalse()
        {
            var result = GhJsonValidator.Validate("", out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("null or empty", errorMessage);
        }

        [Fact]
        public void Validate_NullJson_ReturnsFalse()
        {
            var result = GhJsonValidator.Validate(null!, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
        }

        [Fact]
        public void Validate_InvalidJson_ReturnsFalse()
        {
            var json = "{ invalid json }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid JSON", errorMessage);
        }

        [Fact]
        public void Validate_MissingComponents_ReturnsFalse()
        {
            var json = @"{ ""connections"": [] }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("components", errorMessage);
        }

        [Fact]
        public void Validate_MissingConnections_ReturnsTrue()
        {
            // Missing connections is treated as empty array - valid
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""Test"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1
                    }
                ]
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.True(result);
        }

        [Fact]
        public void Validate_ComponentMissingName_ReturnsFalse()
        {
            var json = @"{
                ""components"": [
                    {
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890""
                    }
                ],
                ""connections"": []
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("name", errorMessage);
        }

        [Fact]
        public void Validate_ComponentMissingId_ReturnsFalse()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""Test""
                    }
                ],
                ""connections"": []
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("id", errorMessage);
        }

        [Fact]
        public void Validate_ComponentInvalidInstanceGuid_ReturnsWarning()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""Test"",
                        ""id"": 1,
                        ""instanceGuid"": ""not-a-guid""
                    }
                ],
                ""connections"": []
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            // Invalid GUID format is a warning, not an error
            Assert.NotNull(errorMessage);
            Assert.Contains("Warning", errorMessage);
        }

        [Fact]
        public void Validate_ConnectionMissingFromId_ReturnsFalse()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""A"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1
                    },
                    {
                        ""name"": ""B"",
                        ""instanceGuid"": ""b1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 2
                    }
                ],
                ""connections"": [
                    {
                        ""from"": { ""paramName"": ""R"" },
                        ""to"": { ""id"": 2, ""paramName"": ""A"" }
                    }
                ]
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("from", errorMessage);
        }

        [Fact]
        public void Validate_ConnectionReferenceUndefinedId_ReturnsFalse()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""A"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1
                    }
                ],
                ""connections"": [
                    {
                        ""from"": { ""id"": 1, ""paramName"": ""R"" },
                        ""to"": { ""id"": 999, ""paramName"": ""A"" }
                    }
                ]
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.False(result);
            Assert.NotNull(errorMessage);
            Assert.Contains("999", errorMessage);
        }

        [Fact]
        public void Validate_ValidConnections_ReturnsTrue()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""A"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""componentGuid"": ""d1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1
                    },
                    {
                        ""name"": ""B"",
                        ""instanceGuid"": ""b1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""componentGuid"": ""e1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 2
                    }
                ],
                ""connections"": [
                    {
                        ""from"": { ""id"": 1, ""paramName"": ""R"" },
                        ""to"": { ""id"": 2, ""paramName"": ""A"" }
                    }
                ]
            }";

            var result = GhJsonValidator.Validate(json, out var errorMessage);

            Assert.True(result);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateDetailed_ReturnsValidationResult()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""Test"",
                        ""instanceGuid"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""componentGuid"": ""d1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""id"": 1
                    }
                ],
                ""connections"": []
            }";

            var result = GhJsonValidator.ValidateDetailed(json);

            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateDetailed_WithErrors_ParsesCorrectly()
        {
            var json = @"{
                ""components"": [
                    {
                        ""name"": ""Test""
                    }
                ],
                ""connections"": []
            }";

            var result = GhJsonValidator.ValidateDetailed(json);

            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }
    }
}
