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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    [Collection("SchemaRegistry")]
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
            // The ghjson.schema.json v1.0 schema declares `components` as required, so a
            // document lacking it is now a hard schema error (previously this was just a
            // warning under the legacy structural-only validator).
            var json = @"{""schema"":""1.0""}";

            var result = GhJson.Validate(json);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
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

        [Fact]
        public async Task ValidateAsync_ValidDocument_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1 })
                .Build();

            var result = await GhJson.ValidateAsync(doc);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_String_ReturnsResult()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]}";

            var result = await GhJson.ValidateAsync(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_CompactStringPivot_ReturnsSuccess()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1,""pivot"":""100,200""}]}";

            var result = GhJson.Validate(json);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Validate_DocumentBuilderWithCompactPivot_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "Addition", Id = 1, Pivot = new GhJsonPivot(100, 200) })
                .Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Validate_ManyComponentsWithCompactPivots_ReturnsSuccess()
        {
            var builder = GhJson.CreateDocumentBuilder();
            for (int i = 1; i <= 130; i++)
            {
                builder = builder.AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = i,
                    Pivot = new GhJsonPivot(i * 10, i * 20),
                });
            }

            var doc = builder.Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Validate_ScribbleExtension_ReturnsSuccess()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Scribble"",""id"":1,""pivot"":""100,200"",""componentState"":{""extensions"":{""gh.scribble"":{""text"":""Hello"",""corners"":[""0,0"",""100,0"",""0,50""]}}}}]}";

            var result = GhJson.Validate(json);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Validate_MixedDocumentWithScribbles_ReturnsSuccess()
        {
            var builder = GhJson.CreateDocumentBuilder();
            for (int i = 1; i <= 130; i++)
            {
                var component = new GhJsonComponent
                {
                    Name = i % 33 == 0 ? "Scribble" : "Addition",
                    Id = i,
                    Pivot = new GhJsonPivot(i * 10, i * 20),
                };

                if (i % 33 == 0)
                {
                    component.ComponentState = new GhJsonComponentState
                    {
                        Extensions = new Dictionary<string, object>
                        {
                            ["gh.scribble"] = new Dictionary<string, object>
                            {
                                ["text"] = "Hello",
                                ["corners"] = new[] { "0,0", "100,0", "0,50" },
                            },
                        },
                    };
                }

                builder = builder.AddComponent(component);
            }

            var doc = builder.Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Validate_MinimalLevel_PerformsOnlyBasicChecks()
        {
            // A document with a dangling connection reference would fail Standard,
            // but Minimal should pass because it only does basic structural checks.
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 1 } },
                connections: new[]
                {
                    new GhJsonConnection
                    {
                        From = new GhJsonConnectionEndpoint { Id = 99, ParamName = "X" },
                        To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "A" }
                    }
                },
                groups: null);

            var result = GhJson.Validate(doc, ValidationLevel.Minimal);

            // Minimal should be valid because it skips connection reference validation
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_MinimalLevel_DoesNotValidateConnections()
        {
            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: new[] { new GhJsonComponent { Name = "A", Id = 1 } },
                connections: new[]
                {
                    new GhJsonConnection
                    {
                        From = new GhJsonConnectionEndpoint { Id = 99, ParamName = "X" },
                        To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "A" }
                    }
                },
                groups: null);

            var minimal = GhJson.Validate(doc, ValidationLevel.Minimal);
            var standard = GhJson.Validate(doc, ValidationLevel.Standard);

            Assert.True(minimal.IsValid);
            Assert.False(standard.IsValid);
        }

        [Fact]
        public void IsValid_StringOverload_ReturnsTrueForValidJson()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]}";

            Assert.True(GhJson.IsValid(json));
        }

        [Fact]
        public void Validate_RuntimeData_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    Pivot = new GhJsonPivot(100, 200),
                    OutputSettings = new List<GhJsonParameterSettings>
                    {
                        new GhJsonParameterSettings
                        {
                            ParameterName = "Result",
                            RuntimeData = new Dictionary<string, Dictionary<string, string>>
                            {
                                ["{0}"] = new Dictionary<string, string>
                                {
                                    ["{0}(0)"] = "int:7",
                                },
                            },
                        },
                    },
                })
                .Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Validate_FullComponentWithRuntimeData_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    ComponentGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    InstanceGuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Pivot = new GhJsonPivot(100, 200),
                    InputSettings = new List<GhJsonParameterSettings>
                    {
                        new GhJsonParameterSettings { ParameterName = "A" },
                        new GhJsonParameterSettings { ParameterName = "B" },
                    },
                    OutputSettings = new List<GhJsonParameterSettings>
                    {
                        new GhJsonParameterSettings
                        {
                            ParameterName = "Result",
                            RuntimeData = new Dictionary<string, Dictionary<string, string>>
                            {
                                ["{0}"] = new Dictionary<string, string>
                                {
                                    ["{0}(0)"] = "int:7",
                                },
                            },
                        },
                    },
                })
                .Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void Serialize_RuntimeData_PivotIsCompactString()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    Pivot = new GhJsonPivot(100, 200),
                    OutputSettings = new List<GhJsonParameterSettings>
                    {
                        new GhJsonParameterSettings
                        {
                            ParameterName = "Result",
                            RuntimeData = new Dictionary<string, Dictionary<string, string>>
                            {
                                ["{0}"] = new Dictionary<string, string>
                                {
                                    ["{0}(0)"] = "integer:7",
                                },
                            },
                        },
                    },
                })
                .Build();

            var json = GhJson.ToJson(doc, new GhJSON.Core.Serialization.WriteOptions { Indented = false });

            Assert.Contains("\"pivot\":\"100,200\"", json);
            Assert.Contains("\"runtimeData\"", json);
        }

        [Fact]
        public void Validate_RuntimeDataOnInputSettings_ReturnsSuccess()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    InputSettings = new List<GhJsonParameterSettings>
                    {
                        new GhJsonParameterSettings
                        {
                            ParameterName = "A",
                            RuntimeData = new Dictionary<string, Dictionary<string, string>>
                            {
                                ["{0}"] = new Dictionary<string, string>
                                {
                                    ["{0}(0)"] = "int:7",
                                },
                            },
                        },
                    },
                })
                .Build();

            var result = GhJson.Validate(doc);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        }
    }
}
