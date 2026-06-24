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

using System.Collections.Generic;
using GhJSON.Core;
using GhJSON.Core.DiffOperations;
using GhJSON.Core.PatchModels;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests.DiffOperations
{
    public class PatchSerializerTests
    {
        [Fact]
        public void Serialize_RoundTrip_PreservesData()
        {
            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "A", Id = 1 }
                        }
                    }
                }
            };

            var json = PatchSerializer.Serialize(patch);
            var reparsed = PatchSerializer.Deserialize(json);

            Assert.Equal("ghpatch", reparsed.Kind);
            Assert.NotNull(reparsed.Patch.Components);
            Assert.Single(reparsed.Patch.Components.Add!);
            Assert.Equal("A", reparsed.Patch.Components.Add[0].Name);
            Assert.Equal(1, reparsed.Patch.Components.Add[0].Id);
        }

        [Fact]
        public void Serialize_WithIndentedOption_FormatsJson()
        {
            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new GhPatchBody()
            };

            var options = new WriteOptions { Indented = true };
            var json = PatchSerializer.Serialize(patch, options);

            Assert.Contains("\n", json);
            Assert.Contains("  ", json);
        }

        [Fact]
        public void Deserialize_InvalidJson_ThrowsJsonReaderException()
        {
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => PatchSerializer.Deserialize("not valid json"));
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsPatchWithDefaultKind()
        {
            var result = PatchSerializer.Deserialize("{}");

            Assert.NotNull(result);
            Assert.Equal("ghpatch", result.Kind);
        }

        [Fact]
        public void Deserialize_PreservesPivotInComponents()
        {
            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "A", Id = 1, Pivot = new GhJsonPivot { X = 100.5, Y = 200.5 } }
                        }
                    }
                }
            };

            var json = PatchSerializer.Serialize(patch);
            var reparsed = PatchSerializer.Deserialize(json);

            Assert.NotNull(reparsed.Patch.Components!.Add![0].Pivot);
            Assert.Equal(100.5, reparsed.Patch.Components.Add[0].Pivot.X);
            Assert.Equal(200.5, reparsed.Patch.Components.Add[0].Pivot.Y);
        }

        [Fact]
        public void Serialize_PatchWithAllOperations_PreservesStructure()
        {
            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Schema = "1.0",
                Patch = new GhPatchBody
                {
                    Base = new GhPatchBaseRef { Schema = "1.0", Checksum = "sha256-abc123" },
                    Metadata = new GhPatchMetadataOp { Set = new JObject { ["title"] = "New" } },
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent> { new GhJsonComponent { Name = "New", Id = 2 } },
                        Remove = new List<GhPatchComponentMatch> { new GhPatchComponentMatch { Id = 1 } },
                        Modify = new List<GhPatchComponentModify>
                        {
                            new GhPatchComponentModify
                            {
                                Match = new GhPatchComponentMatch { Id = 3 },
                                Set = new JObject { ["name"] = "Renamed" }
                            }
                        }
                    },
                    Connections = new GhPatchConnectionsOp
                    {
                        Add = new List<GhJsonConnection>
                        {
                            new GhJsonConnection { From = new GhJsonConnectionEndpoint { Id = 1 }, To = new GhJsonConnectionEndpoint { Id = 2 } }
                        },
                        Remove = new List<GhJsonConnection>
                        {
                            new GhJsonConnection { From = new GhJsonConnectionEndpoint { Id = 3 }, To = new GhJsonConnectionEndpoint { Id = 4 } }
                        }
                    },
                    Groups = new GhPatchGroupsOp
                    {
                        Add = new List<GhJsonGroup> { new GhJsonGroup { Id = 1, Members = new List<int> { 1 } } },
                        Remove = new List<GhPatchGroupMatch> { new GhPatchGroupMatch { Id = 2 } },
                        Modify = new List<GhPatchGroupModify>
                        {
                            new GhPatchGroupModify
                            {
                                Match = new GhPatchGroupMatch { Id = 3 },
                                Set = new JObject { ["name"] = "NewName" }
                            }
                        }
                    }
                }
            };

            var json = PatchSerializer.Serialize(patch);
            var reparsed = PatchSerializer.Deserialize(json);

            Assert.Equal("ghpatch", reparsed.Kind);
            Assert.Equal("1.0", reparsed.Schema);
            Assert.NotNull(reparsed.Patch.Base);
            Assert.Equal("sha256-abc123", reparsed.Patch.Base!.Checksum);
            Assert.NotNull(reparsed.Patch.Metadata);
            Assert.NotNull(reparsed.Patch.Components);
            Assert.Single(reparsed.Patch.Components.Add!);
            Assert.Single(reparsed.Patch.Components.Remove!);
            Assert.Single(reparsed.Patch.Components.Modify!);
            Assert.NotNull(reparsed.Patch.Connections);
            Assert.Single(reparsed.Patch.Connections.Add!);
            Assert.Single(reparsed.Patch.Connections.Remove!);
            Assert.NotNull(reparsed.Patch.Groups);
            Assert.Single(reparsed.Patch.Groups.Add!);
            Assert.Single(reparsed.Patch.Groups.Remove!);
            Assert.Single(reparsed.Patch.Groups.Modify!);
        }
    }
}
