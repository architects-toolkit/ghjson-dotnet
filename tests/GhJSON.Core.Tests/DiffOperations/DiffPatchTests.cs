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
using GhJSON.Core;
using GhJSON.Core.DiffOperations;
using GhJSON.Core.PatchModels;
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests.DiffOperations
{
    public class DiffPatchTests
    {
        // ---- Diff: detects component added ----
        [Fact]
        public void Diff_AddedComponent_ProducesAddOp()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .Build();

            var diff = GhJson.Diff(left, right);

            Assert.True(diff.HasChanges);
            Assert.NotNull(diff.Patch.Patch.Components);
            Assert.Single(diff.Patch.Patch.Components!.Add!);
            Assert.Equal("B", diff.Patch.Patch.Components.Add![0].Name);
        }

        [Fact]
        public void Diff_RemovedComponent_ProducesRemoveOp()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var diff = GhJson.Diff(left, right);

            Assert.True(diff.HasChanges);
            Assert.NotNull(diff.Patch.Patch.Components);
            Assert.Single(diff.Patch.Patch.Components!.Remove!);
        }

        [Fact]
        public void Diff_ModifiedComponentName_ProducesModifyOp()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-renamed", Id = 1 })
                .Build();

            var diff = GhJson.Diff(left, right);

            Assert.True(diff.HasChanges);
            Assert.NotNull(diff.Patch.Patch.Components);
            Assert.Single(diff.Patch.Patch.Components!.Modify!);
            Assert.NotNull(diff.Patch.Patch.Components.Modify![0].Set);
            Assert.Equal("A-renamed", (string?)diff.Patch.Patch.Components.Modify[0].Set!["name"]);
        }

        // ---- Roundtrip: Diff -> ApplyPatch -> equality ----

        [Fact]
        public void Roundtrip_AddedComponent_AppliedYieldsRight()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .Build();

            var diff = GhJson.Diff(left, right);
            var apply = GhJson.ApplyPatch(left, diff.Patch);

            Assert.True(apply.Success);
            Assert.False(apply.HasConflicts);
            Assert.Equal(2, apply.Document.Components.Count);
            Assert.Contains(apply.Document.Components, c => c.Name == "B");
        }

        [Fact]
        public void Roundtrip_ModifyComponent_AppliedYieldsRight()
        {
            var guid = Guid.NewGuid();
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-renamed", Id = 1, InstanceGuid = guid })
                .Build();

            var diff = GhJson.Diff(left, right);
            var apply = GhJson.ApplyPatch(left, diff.Patch);

            Assert.True(apply.Success);
            Assert.False(apply.HasConflicts);
            Assert.Equal("A-renamed", apply.Document.Components[0].Name);
        }

        [Fact]
        public void Roundtrip_AddedConnection_AppliedYieldsRight()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" },
                })
                .Build();

            var diff = GhJson.Diff(left, right);
            var apply = GhJson.ApplyPatch(left, diff.Patch);

            Assert.True(apply.Success);
            Assert.NotNull(apply.Document.Connections);
            Assert.Single(apply.Document.Connections!);
        }

        [Fact]
        public void Roundtrip_RemoveConnection_AppliedYieldsRight()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" },
                })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .Build();

            var diff = GhJson.Diff(left, right);
            var apply = GhJson.ApplyPatch(left, diff.Patch);

            Assert.True(apply.Success);
            Assert.True(apply.Document.Connections == null || apply.Document.Connections.Count == 0);
            Assert.Equal(1, apply.ConnectionsRemoved);
        }

        [Fact]
        public void Roundtrip_GroupRename_AppliedYieldsRight()
        {
            var guid = Guid.NewGuid();
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddGroup(new GhJsonGroup { InstanceGuid = guid, Id = 1, Name = "Group1", Members = new List<int> { 1 } })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddGroup(new GhJsonGroup { InstanceGuid = guid, Id = 1, Name = "Renamed", Members = new List<int> { 1 } })
                .Build();

            var diff = GhJson.Diff(left, right);
            var apply = GhJson.ApplyPatch(left, diff.Patch);

            Assert.True(apply.Success);
            Assert.Equal("Renamed", apply.Document.Groups![0].Name);
        }

        // ---- Identity precedence ----

        [Fact]
        public void IdentityPrecedence_InstanceGuidWinsOverId()
        {
            var leftGuid = Guid.NewGuid();
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = leftGuid })
                .Build();

            // Same InstanceGuid but a different Id — should match by instanceGuid.
            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-renamed", Id = 99, InstanceGuid = leftGuid })
                .Build();

            var diff = GhJson.Diff(left, right);

            Assert.Single(diff.Patch.Patch.Components!.Modify!);
            Assert.Empty(diff.Patch.Patch.Components!.Add ?? new List<GhJsonComponent>());
            Assert.Empty(diff.Patch.Patch.Components!.Remove ?? new List<GhPatchComponentMatch>());
        }

        // ---- Base checksum verification ----

        [Fact]
        public void ApplyPatch_BaseChecksumMatches_Succeeds()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-2", Id = 1 })
                .Build();

            var diff = GhJson.Diff(left, right);
            Assert.NotNull(diff.Patch.Patch.Base?.Checksum);

            var apply = GhJson.ApplyPatch(left, diff.Patch);
            Assert.True(apply.Success);
            Assert.DoesNotContain(apply.Conflicts, c => c.Kind == PatchConflictKind.BaseChecksumMismatch);
        }

        [Fact]
        public void ApplyPatch_BaseChecksumMismatch_RefusesApply()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-2", Id = 1 })
                .Build();

            var diff = GhJson.Diff(left, right);

            var differentBase = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "DIFFERENT", Id = 1 })
                .Build();

            var apply = GhJson.ApplyPatch(differentBase, diff.Patch);

            Assert.False(apply.Success);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.BaseChecksumMismatch);
            // Refused — no mutations applied.
            Assert.Equal(0, apply.ComponentsAdded);
            Assert.Equal(0, apply.ComponentsModified);
            Assert.Equal(0, apply.ComponentsRemoved);
        }

        [Fact]
        public void ApplyPatch_VerifyBaseFalse_ProceedsDespiteChecksumMismatch()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-2", Id = 1 })
                .Build();

            var diff = GhJson.Diff(left, right);

            var differentBase = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "DIFFERENT", Id = 1 })
                .Build();

            var apply = GhJson.ApplyPatch(differentBase, diff.Patch, new ApplyPatchOptions { VerifyBase = false });

            // VerifyBase off — apply proceeds and modifies the differing base.
            Assert.DoesNotContain(apply.Conflicts, c => c.Kind == PatchConflictKind.BaseChecksumMismatch);
            Assert.Equal(1, apply.ComponentsModified);
        }

        // ---- Conflict: match not found ----

        [Fact]
        public void ApplyPatch_MatchNotFound_RecordsConflict()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Modify = new List<GhPatchComponentModify>
                        {
                            new GhPatchComponentModify
                            {
                                Match = new GhPatchComponentMatch { Id = 999 },
                                Set = new JObject { ["name"] = "should-not-apply" },
                            },
                        },
                    },
                },
            };

            var apply = GhJson.ApplyPatch(doc, patch);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.MatchNotFound);
        }

        // ---- Conflict: connection already present ----

        [Fact]
        public void ApplyPatch_ConnectionAlreadyPresent_RecordsConflict()
        {
            var conn = new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" },
            };

            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddConnection(conn)
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Connections = new GhPatchConnectionsOp
                    {
                        Add = new List<GhJsonConnection> { conn },
                    },
                },
            };

            var apply = GhJson.ApplyPatch(doc, patch);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.ConnectionAlreadyPresent);
        }

        // ---- Idempotence ----

        [Fact]
        public void Idempotence_ApplyTwice_SameAsApplyOnce()
        {
            var guid = Guid.NewGuid();
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A-renamed", Id = 1, InstanceGuid = guid })
                .Build();

            var diff = GhJson.Diff(left, right);
            var apply1 = GhJson.ApplyPatch(left, diff.Patch);
            // Second apply must not reverify checksum (base now differs).
            var apply2 = GhJson.ApplyPatch(apply1.Document, diff.Patch, new ApplyPatchOptions { VerifyBase = false });

            Assert.Equal("A-renamed", apply1.Document.Components[0].Name);
            Assert.Equal("A-renamed", apply2.Document.Components[0].Name);
            Assert.Equal(apply1.Document.Components.Count, apply2.Document.Components.Count);
        }

        // ---- Connection identity: paramName preferred, fallback to paramIndex ----

        [Fact]
        public void Diff_ConnectionByParamName_NoChangeWhenSame()
        {
            var conn1 = new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out", ParamIndex = 0 },
                To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in", ParamIndex = 0 },
            };

            var conn2 = new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out", ParamIndex = 0 },
                To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in", ParamIndex = 0 },
            };

            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddConnection(conn1)
                .Build();
            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .AddConnection(conn2)
                .Build();

            var diff = GhJson.Diff(left, right);
            Assert.Null(diff.Patch.Patch.Connections);
        }

        // ---- Serialization roundtrip ----

        [Fact]
        public void PatchSerialization_Roundtrip_PreservesOps()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .Build();

            var diff = GhJson.Diff(left, right);
            var json = GhJson.PatchToJson(diff.Patch);
            var reparsed = GhJson.PatchFromJson(json);

            Assert.Equal("ghpatch", reparsed.Kind);
            Assert.NotNull(reparsed.Patch.Components);
            Assert.Single(reparsed.Patch.Components!.Add!);
        }

        // ---- Validation ----

        [Fact]
        public void ValidatePatch_GoodPatch_ReturnsValid()
        {
            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "A", Id = 1 },
                        },
                    },
                },
            };

            var result = GhJson.ValidatePatch(patch);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_RemoveWithoutIdentity_ReturnsError()
        {
            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Remove = new List<GhPatchComponentMatch>
                        {
                            new GhPatchComponentMatch(),
                        },
                    },
                },
            };

            var result = GhJson.ValidatePatch(patch);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_WithPreferOnline_Succeeds()
        {
            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "A", Id = 1 },
                        },
                    },
                },
            };

            var result = GhJson.ValidatePatch(patch, preferOnline: true);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_WithSchemaVersion_Succeeds()
        {
            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "A", Id = 1 },
                        },
                    },
                },
            };

            var result = GhJson.ValidatePatch(patch, schemaVersion: "1.0");
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_StringOverload_WithPreferOnline_Succeeds()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{}}";
            var result = GhJson.ValidatePatch(json, preferOnline: true);
            Assert.True(result.IsValid);
        }

        // ---- ID-collision renumbering ----

        [Fact]
        public void ApplyPatch_AddedComponentWithCollidingId_Renumbers()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "B", Id = 1 },
                        },
                    },
                },
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.Success);
            Assert.Equal(2, apply.Document.Components.Count);
            Assert.Equal(2, apply.Document.Components.First(c => c.Name == "B").Id);
        }

        // ---- DiffOptions ----

        [Fact]
        public void Diff_WithIgnorePivots_DoesNotProduceModifyForPivotChange()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, Pivot = new GhJsonPivot { X = 0, Y = 0 } })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, Pivot = new GhJsonPivot { X = 100, Y = 200 } })
                .Build();

            var options = new DiffOptions { IgnorePivots = true };
            var diff = GhJson.Diff(left, right, options);

            Assert.False(diff.HasChanges);
        }

        [Fact]
        public void Diff_WithIgnoreRuntimeMessages_DoesNotProduceModifyForErrors()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, Errors = new List<string> { "error1" } })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, Errors = new List<string> { "error2" } })
                .Build();

            var options = new DiffOptions { IgnoreRuntimeMessages = true };
            var diff = GhJson.Diff(left, right, options);

            Assert.False(diff.HasChanges);
        }

        [Fact]
        public void Diff_WithIgnoreMetadataCounters_DoesNotProduceModifyForCounts()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .WithMetadata(new GhJsonMetadata { Title = "Test", ComponentCount = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2 })
                .WithMetadata(new GhJsonMetadata { Title = "Test", ComponentCount = 2 })
                .Build();

            var options = new DiffOptions { IgnoreMetadataCounters = true };
            var diff = GhJson.Diff(left, right, options);

            // Should only detect the added component, not metadata counter change
            Assert.True(diff.HasChanges);
            Assert.NotNull(diff.Patch.Patch.Components);
            Assert.Single(diff.Patch.Patch.Components!.Add!);
            Assert.Null(diff.Patch.Patch.Metadata);
        }

        [Fact]
        public void Diff_WithIgnoreMetadataTimestamps_DoesNotProduceModifyForModified()
        {
            var now = DateTime.UtcNow;
            var later = now.AddHours(1);

            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .WithMetadata(new GhJsonMetadata { Title = "Test", Modified = now })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .WithMetadata(new GhJsonMetadata { Title = "Test", Modified = later })
                .Build();

            var options = new DiffOptions { IgnoreMetadataTimestamps = true };
            var diff = GhJson.Diff(left, right, options);

            Assert.False(diff.HasChanges);
        }

        [Fact]
        public void Diff_WithIncludeBaseChecksum_ProducesChecksumInPatch()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var options = new DiffOptions { IncludeBaseChecksum = true };
            var diff = GhJson.Diff(left, right, options);

            Assert.NotNull(diff.Patch.Patch.Base);
            Assert.False(string.IsNullOrEmpty(diff.Patch.Patch.Base!.Checksum));
        }

        [Fact]
        public void Diff_WithoutIncludeBaseChecksum_OmitsChecksum()
        {
            var left = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var right = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var options = new DiffOptions { IncludeBaseChecksum = false };
            var diff = GhJson.Diff(left, right, options);

            Assert.True(diff.Patch.Patch.Base == null || string.IsNullOrEmpty(diff.Patch.Patch.Base.Checksum));
        }

        // ---- PatchApplier edge cases ----

        [Fact]
        public void ApplyPatch_SchemaMismatch_WithContinueOnConflict_RecordsConflictButContinues()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("1.0")
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new GhPatchBody
                {
                    Base = new GhPatchBaseRef { Schema = "2.0" },
                    Components = new GhPatchComponentsOp
                    {
                        Modify = new List<GhPatchComponentModify>
                        {
                            new GhPatchComponentModify
                            {
                                Match = new GhPatchComponentMatch { Id = 1 },
                                Set = new JObject { ["name"] = "Renamed" }
                            }
                        }
                    }
                }
            };

            var options = new ApplyPatchOptions { ContinueOnConflict = true };
            var apply = GhJson.ApplyPatch(doc, patch, options);

            Assert.True(apply.HasConflicts);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.SchemaVersionMismatch);
            // Should still apply the modify because ContinueOnConflict is true
            Assert.Equal("Renamed", apply.Document.Components[0].Name);
        }

        [Fact]
        public void ApplyPatch_SchemaMismatch_WithoutContinueOnConflict_ReturnsFailure()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .WithSchema("1.0")
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new GhPatchBody
                {
                    Base = new GhPatchBaseRef { Schema = "2.0" },
                    Components = new GhPatchComponentsOp
                    {
                        Modify = new List<GhPatchComponentModify>
                        {
                            new GhPatchComponentModify
                            {
                                Match = new GhPatchComponentMatch { Id = 1 },
                                Set = new JObject { ["name"] = "Renamed" }
                            }
                        }
                    }
                }
            };

            var options = new ApplyPatchOptions { ContinueOnConflict = false };
            var apply = GhJson.ApplyPatch(doc, patch, options);

            Assert.False(apply.Success);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.SchemaVersionMismatch);
            Assert.Equal(0, apply.ComponentsModified);
        }

        [Fact]
        public void ApplyPatch_ComponentAdd_WithIdCollision_AndRenumberCollidingAddedIds_Renumbers()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "B", Id = 1 }
                        }
                    }
                }
            };

            var options = new ApplyPatchOptions { RenumberCollidingAddedIds = true };
            var apply = GhJson.ApplyPatch(doc, patch, options);

            Assert.True(apply.Success);
            Assert.Equal(2, apply.Document.Components.Count);
            Assert.Equal(2, apply.Document.Components.First(c => c.Name == "B").Id);
        }

        [Fact]
        public void ApplyPatch_ComponentAdd_WithIdCollision_WithoutRenumber_ReturnsConflict()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "B", Id = 1 }
                        }
                    }
                }
            };

            var options = new ApplyPatchOptions { RenumberCollidingAddedIds = false };
            var apply = GhJson.ApplyPatch(doc, patch, options);

            Assert.True(apply.HasConflicts);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.MatchAmbiguous || c.Message.Contains("already exists"));
        }

        [Fact]
        public void ApplyPatch_ComponentAdd_WithGuidCollision_ReturnsConflict()
        {
            var guid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guid }
                        }
                    }
                }
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.HasConflicts);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.InstanceGuidCollision);
        }

        [Fact]
        public void ApplyPatch_MatchAmbiguous_RecordsConflict()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, ComponentGuid = Guid.NewGuid() })
                .AddComponent(new GhJsonComponent { Name = "A", Id = 2, ComponentGuid = Guid.NewGuid() })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Modify = new List<GhPatchComponentModify>
                        {
                            new GhPatchComponentModify
                            {
                                Match = new GhPatchComponentMatch { Name = "A" },
                                Set = new JObject { ["name"] = "Renamed" }
                            }
                        }
                    }
                }
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.HasConflicts);
            Assert.Contains(apply.Conflicts, c => c.Kind == PatchConflictKind.MatchAmbiguous);
        }

        [Fact]
        public void ApplyPatch_ConnectionAdd_ReferencesNewlyAddedComponent()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Components = new GhPatchComponentsOp
                    {
                        Add = new List<GhJsonComponent>
                        {
                            new GhJsonComponent { Name = "B", Id = 2 }
                        }
                    },
                    Connections = new GhPatchConnectionsOp
                    {
                        Add = new List<GhJsonConnection>
                        {
                            new GhJsonConnection
                            {
                                From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                                To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" }
                            }
                        }
                    }
                }
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.Success);
            Assert.Equal(2, apply.Document.Components.Count);
            Assert.NotNull(apply.Document.Connections);
            Assert.Single(apply.Document.Connections!);
        }

        [Fact]
        public void ApplyPatch_GroupRemove_RemovesGroupAndMembersReference()
        {
            var guid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .AddGroup(new GhJsonGroup { InstanceGuid = guid, Id = 1, Name = "Group1", Members = new List<int> { 1 } })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Groups = new GhPatchGroupsOp
                    {
                        Remove = new List<GhPatchGroupMatch>
                        {
                            new GhPatchGroupMatch { InstanceGuid = guid }
                        }
                    }
                }
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.Success);
            Assert.Equal(1, apply.GroupsRemoved);
            Assert.True(apply.Document.Groups == null || apply.Document.Groups.Count == 0);
        }

        [Fact]
        public void ApplyPatch_MetadataSet_UpdatesMetadata()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .WithMetadata(new GhJsonMetadata { Title = "Old" })
                .Build();

            var patch = new GhPatchDocument
            {
                Patch = new GhPatchBody
                {
                    Metadata = new GhPatchMetadataOp
                    {
                        Set = new JObject { ["title"] = "New" }
                    }
                }
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.Success);
            Assert.NotNull(apply.Document.Metadata);
            Assert.Equal("New", apply.Document.Metadata.Title);
        }

        [Fact]
        public void ApplyPatch_EmptyPatch_IsNoOp()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var patch = new GhPatchDocument
            {
                Kind = "ghpatch",
                Patch = new GhPatchBody()
            };

            var apply = GhJson.ApplyPatch(doc, patch);

            Assert.True(apply.Success);
            Assert.False(apply.HasConflicts);
            Assert.Single(apply.Document.Components);
            Assert.Equal("A", apply.Document.Components[0].Name);
        }
    }
}
