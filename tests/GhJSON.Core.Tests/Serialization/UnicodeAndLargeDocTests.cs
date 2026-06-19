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

using System.Diagnostics;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    /// <summary>
    /// Stress tests for Unicode payloads and large documents. Parse/serialize must
    /// complete within a generous time budget and preserve every component.
    /// </summary>
    public class UnicodeAndLargeDocTests
    {
        [Theory]
        [InlineData("日本語")]
        [InlineData("مرحبا")]          // RTL
        [InlineData("🚀 emoji ✨")]
        [InlineData("mixed — dashes and \"quotes\"")]
        public void RoundTrip_PreservesUnicodeStrings(string text)
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = text,
                    NickName = text,
                    Id = 1,
                })
                .Build();

            var json = GhJson.ToJson(doc);
            var reloaded = GhJson.FromJson(json);

            Assert.Equal(text, reloaded.Components[0].Name);
            Assert.Equal(text, reloaded.Components[0].NickName);
        }

        [Fact]
        public void LargeDocument_RoundTripsWithinBudget()
        {
            const int count = 2_000;

            var builder = GhJson.CreateDocumentBuilder();
            for (var i = 1; i <= count; i++)
            {
                builder = builder.AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = i,
                    Pivot = new GhJsonPivot(i * 10, i * 5),
                });
            }

            var doc = builder.Build();

            var sw = Stopwatch.StartNew();
            var json = GhJson.ToJson(doc);
            var reloaded = GhJson.FromJson(json);
            sw.Stop();

            Assert.Equal(count, reloaded.Components.Count);
            // Generous budget — catches O(n^2) regressions without being flaky.
            Assert.True(
                sw.ElapsedMilliseconds < 10_000,
                $"Serialize+Deserialize of {count} components took {sw.ElapsedMilliseconds} ms");
        }
    }
}
