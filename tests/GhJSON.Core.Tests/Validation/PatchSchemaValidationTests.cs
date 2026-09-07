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
using System.Linq;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    /// <summary>
    /// Tests patch validation against the GhPatch JSON Schema using raw JSON strings.
    /// These bypass the typed models to test the validator itself.
    /// </summary>
    [Collection("SchemaRegistry")]
    public class PatchSchemaValidationTests
    {
        [Fact]
        public void ValidatePatch_ValidRawJson_ReturnsValid()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.ConvertAll(e => e.Message)));
        }

        [Fact]
        public void ValidatePatch_MissingKind_ReturnsError()
        {
            const string json = "{\"patch\":{}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_WrongKind_ReturnsError()
        {
            const string json = "{\"kind\":\"wrong\",\"patch\":{}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_MissingPatch_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\"}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_EmptyInstanceGuidComponentMatch_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"components\":{\"remove\":[{\"instanceGuid\":\"00000000-0000-0000-0000-000000000000\"}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_EmptyComponentGuidMatch_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"components\":{\"remove\":[{\"componentGuid\":\"00000000-0000-0000-0000-000000000000\"}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_EmptyInstanceGuidGroupMatch_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"groups\":{\"remove\":[{\"instanceGuid\":\"00000000-0000-0000-0000-000000000000\"}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_ConnectionMissingFrom_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"connections\":{\"add\":[{\"to\":{}}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_ConnectionMissingTo_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"connections\":{\"add\":[{\"from\":{}}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_ConnectionWithFromAndTo_ReturnsValid()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"connections\":{\"add\":[{\"from\":{},\"to\":{}}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.ConvertAll(e => e.Message)));
        }

        [Fact]
        public void ValidatePatch_ExtraTopLevelProperty_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{},\"extra\":true}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_EmptyPatchBody_ReturnsValid()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.ConvertAll(e => e.Message)));
        }

        [Fact]
        public void ValidatePatch_OnlyMetadataSet_ReturnsValid()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"metadata\":{\"set\":{\"title\":\"Test\"}}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.ConvertAll(e => e.Message)));
        }

        [Fact]
        public void ValidatePatch_InvalidJson_ReturnsError()
        {
            const string json = "{not valid json";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_EmptyString_ReturnsError()
        {
            var result = GhJson.ValidatePatch(string.Empty, preferOnline: false);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePatch_OnlinePrefer_LoadsSchema()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{}}";
            var result = GhJson.ValidatePatch(json, preferOnline: true);

            Assert.True(result.IsValid, string.Join("; ", result.Errors.ConvertAll(e => e.Message)));
        }

        [Fact]
        public void ValidatePatch_ComponentAddWithInstanceGuid_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"components\":{\"add\":[{\"name\":\"Panel\",\"id\":1,\"instanceGuid\":\"33333333-3333-3333-3333-333333333333\"}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
            var messages = string.Join("\n", result.Errors.Select(e => e.ToString()));
            Assert.Contains("instanceGuid", messages);
            Assert.Contains("patch.components.add[0]", messages);
        }

        [Fact]
        public void ValidatePatch_GroupAddWithInstanceGuid_ReturnsError()
        {
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"groups\":{\"add\":[{\"id\":1,\"members\":[2],\"instanceGuid\":\"44444444-4444-4444-4444-444444444444\"}]}}}";
            var result = GhJson.ValidatePatch(json, preferOnline: false);

            Assert.False(result.IsValid);
            var messages = string.Join("\n", result.Errors.Select(e => e.ToString()));
            Assert.Contains("instanceGuid", messages);
            Assert.Contains("patch.groups.add[0]", messages);
        }

        [Fact]
        public void ValidatePatch_UnknownComponentMatchProperty_DoesNotEmitAnyOfBranchErrors()
        {
            // componentMatch uses anyOf for identity (instanceGuid, id, componentGuid) with
            // additionalProperties:false. A match with id plus an unknown property should only
            // report the unknown property, not misleading errors about missing instanceGuid
            // or componentGuid from the failing anyOf branches.
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"components\":{\"remove\":[{\"id\":1,\"unknownField\":true}]}}}";

            var result = GhJson.ValidatePatch(json, preferOnline: false);

            var messages = string.Join("\n", result.Errors.Select(e => e.ToString()));
            Assert.False(result.IsValid);
            Assert.Contains("unknownField", messages);
            Assert.DoesNotContain("instanceGuid", messages);
            Assert.DoesNotContain("componentGuid", messages);
        }

        [Fact]
        public void ValidatePatch_ComponentMatchAnyOfAllBranchesFail_EmitsBranchErrors()
        {
            // When no componentMatch identity branch is satisfied, the failing branch errors
            // should still be surfaced so the caller can see why matching failed.
            const string json = "{\"kind\":\"ghpatch\",\"patch\":{\"components\":{\"remove\":[{\"pivot\":\"100,200\"}]}}}";

            var result = GhJson.ValidatePatch(json, preferOnline: false);

            System.Console.WriteLine("Errors: " + string.Join("\n", result.Errors.Select(e => e.ToString())));
            var messages = string.Join("\n", result.Errors.Select(e => e.Message));
            Assert.False(result.IsValid);
            Assert.Contains("instanceGuid", messages);
            Assert.Contains("componentGuid", messages);
        }
    }
}
