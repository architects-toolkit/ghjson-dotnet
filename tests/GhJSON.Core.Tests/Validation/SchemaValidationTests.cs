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
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class SchemaValidationTests
    {
        [Fact]
        public void SchemaLoader_LoadsMainSchemaFromEmbeddedResources()
        {
            Assert.NotNull(SchemaLoader.MainSchema);
            Assert.True(SchemaLoader.RegisteredSchemaIds.Count >= 1,
                "Expected at least the main schema to be registered.");
        }

        [Fact]
        public void SchemaLoader_RegistersExtensionSchemas()
        {
            // Registry + 12 extensions + main = 14.
            Assert.True(SchemaLoader.RegisteredSchemaIds.Count >= 14,
                $"Expected >=14 registered schemas, got {SchemaLoader.RegisteredSchemaIds.Count}.");
            Assert.Contains(
                SchemaLoader.RegisteredSchemaIds,
                uri => uri.ToString().EndsWith("/ghjson.schema.json"));
            Assert.Contains(
                SchemaLoader.RegisteredSchemaIds,
                uri => uri.ToString().EndsWith("/gh.panel.schema.json"));
        }

        [Fact]
        public void Validate_UnknownTopLevelProperty_ReturnsSchemaError()
        {
            const string json = "{\"components\":[{\"name\":\"Addition\",\"id\":1}],\"unknownProp\":true}";

            var result = GhJson.Validate(json, preferOnline: false);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Validate_DocumentWithoutComponents_ReturnsSchemaError()
        {
            // Schema declares "components" as required.
            const string json = "{\"metadata\":{\"title\":\"no components\"}}";

            var result = GhJson.Validate(json, preferOnline: false);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Validate_SchemaFieldMatchesSpecPattern()
        {
            // Schema version must match pattern ^\d+\.\d+(\.\d+)?$
            const string json = "{\"schema\":\"not-a-version\",\"components\":[{\"name\":\"Addition\",\"id\":1}]}";

            var result = GhJson.Validate(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_MinimalLevel_SkipsSchemaValidation()
        {
            // With Minimal level, schema conformance is not enforced; only basic structural checks run.
            const string json = "{\"components\":[{\"name\":\"Addition\",\"id\":1}],\"unknownProp\":true}";

            var result = GhJson.Validate(json, ValidationLevel.Minimal, preferOnline: false);

            // Unknown property does not trigger schema error at Minimal level.
            Assert.True(result.IsValid);
        }
    }
}
