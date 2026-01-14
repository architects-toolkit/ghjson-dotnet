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

using System;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class ValidationTests
    {
        [Fact]
        public void Validate_ValidDocument_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void IsValid_ValidDocument_ReturnsTrue()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            Assert.True(GhJson.IsValid(doc));
        }

        [Fact]
        public void Validate_EmptyDocument_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_JsonString_ReturnsResult()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]}";

            var result = GhJson.Validate(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_MissingRequiredComponents_ReturnsFalse()
        {
            var json = @"{""schema"":""1.0""}";

            var result = GhJson.Validate(json);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.NotEmpty(result.Warnings);
        }

        [Fact]
        public void Validate_InvalidComponentDefinition_ReturnsErrors()
        {
            var json = @"{""schema"":""1.0"",""components"":[{}]}";

            var result = GhJson.Validate(json);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Validate_DuplicateComponentIds_ReturnsWarning()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[]
                {
                    new GhJsonComponent { Name = "Addition", Id = 1 },
                    new GhJsonComponent { Name = "Subtraction", Id = 1 },
                },
                connections: null,
                groups: null);

            var result = GhJson.Validate(doc, ValidationLevel.Strict);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_InvalidConnectionReference_ReturnsError()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "Addition", Id = 1 } },
                connections: new[]
                {
                    new GhJsonConnection
                    {
                        From = new GhJsonConnectionEndpoint { Id = 99, ParamName = "Result" },
                        To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "A" }
                    }
                },
                groups: null);

            var result = GhJson.Validate(doc, ValidationLevel.Strict);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_ValidConnection_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "Panel", Id = 2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "Value" }
                })
                .Build();

            var result = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_InvalidGroupReference_ReturnsError()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "Addition", Id = 1 } },
                connections: null,
                groups: new[]
                {
                    new GhJsonGroup
                    {
                        Id = 1,
                        Members = new System.Collections.Generic.List<int> { 99 }
                    }
                });

            var result = GhJson.Validate(doc, ValidationLevel.Strict);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_StandardLevel_PerformsBasicValidation()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_StrictLevel_PerformsComprehensiveValidation()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = GhJson.Validate(doc, ValidationLevel.Strict);

            Assert.True(result.IsValid);
        }
    }
}
