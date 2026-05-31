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
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    /// <summary>
    /// Tests patch validation against the GhPatch JSON Schema using raw JSON strings.
    /// These bypass the typed models to test the validator itself.
    /// </summary>
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
    }
}
