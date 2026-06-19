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

using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    /// <summary>
    /// Forward-compatibility contract for <see cref="GhJsonComponentState"/>: unknown
    /// properties round-trip via <c>[JsonExtensionData]</c> and core fields
    /// (Locked/Hidden/Selected) survive serialization.
    /// </summary>
    public class GhJsonComponentStateTests
    {
        [Fact]
        public void Defaults_AreAllNull()
        {
            var state = new GhJsonComponentState();

            Assert.Null(state.Locked);
            Assert.Null(state.Hidden);
            Assert.Null(state.Selected);
            Assert.Null(state.Extensions);
            Assert.Null(state.AdditionalProperties);
        }

        [Fact]
        public void CoreFields_RoundTripThroughJson()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Panel",
                    Id = 1,
                    ComponentState = new GhJsonComponentState
                    {
                        Locked = true,
                        Hidden = false,
                        Selected = true,
                    },
                })
                .Build();

            var json = GhJson.ToJson(doc);
            var loaded = GhJson.FromJson(json);

            var state = loaded.Components[0].ComponentState;
            Assert.NotNull(state);
            Assert.True(state!.Locked);
            Assert.False(state.Hidden);
            Assert.True(state.Selected);
        }

        [Fact]
        public void UnknownProperty_IsPreservedViaAdditionalProperties()
        {
            const string json =
                @"{""schema"":""1.0"",""components"":[{""name"":""X"",""id"":1," +
                @"""componentState"":{""locked"":false,""futureKey"":{""flag"":true}}}]}";

            var doc = GhJson.FromJson(json);

            var state = doc.Components[0].ComponentState;
            Assert.NotNull(state);
            Assert.NotNull(state!.AdditionalProperties);
            Assert.True(state.AdditionalProperties!.ContainsKey("futureKey"));
        }
    }
}
