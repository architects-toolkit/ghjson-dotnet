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

namespace GhJSON.Core.Tests.Serialization
{
    /// <summary>
    /// Guarantees byte-stable output of the GhJSON serializer across repeated
    /// invocations on the same document, and across a full serialize → deserialize
    /// → serialize round trip. Stability is essential for diff-friendly storage
    /// and version-control workflows.
    /// </summary>
    public class DeterministicSerializationTests
    {
        private static GhJsonDocument BuildSample()
        {
            return GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = 1,
                    NickName = "Add",
                    Pivot = new GhJsonPivot(100, 200),
                })
                .AddComponent(new GhJsonComponent
                {
                    Name = "Number Slider",
                    Id = 2,
                    Pivot = new GhJsonPivot(0, 0),
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 2, ParamName = "Number" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "A" },
                })
                .Build();
        }

        [Fact]
        public void Serialize_TwiceOnSameDocument_ProducesIdenticalOutput()
        {
            var doc = BuildSample();

            var first = GhJson.ToJson(doc);
            var second = GhJson.ToJson(doc);

            Assert.Equal(first, second);
        }

        [Fact]
        public void RoundTrip_Serialize_Deserialize_Serialize_Stable()
        {
            var doc = BuildSample();

            var firstJson = GhJson.ToJson(doc);
            var reloaded = GhJson.FromJson(firstJson);
            var secondJson = GhJson.ToJson(reloaded);

            Assert.Equal(firstJson, secondJson);
        }
    }
}
