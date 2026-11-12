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
using System.Diagnostics;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;
using Xunit.Abstractions;

namespace GhJSON.Core.Tests.DependencyGraph
{
    /// <summary>
    /// Performance regression coverage for the document pipeline at the scale observed
    /// in real generator output (~400 components, ~600 connections, several islands).
    /// A `put` previously exceeded the caller's 300 s tool budget; these tests pin the
    /// core-side cost (deserialize, fix, serialize, validate, layout) so regressions
    /// surface here rather than inside a live host.
    /// </summary>
    public class LayoutPerformanceTests
    {
        private const int ComponentCount = 400;
        private const int IslandCount = 8;
        private const int GenerousMilliseconds = 20000;

        private readonly ITestOutputHelper output;

        public LayoutPerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Pipeline_LargeConnectedDocument_CompletesWithinBudget()
        {
            var json = BuildLargeDocumentJson();
            var total = Stopwatch.StartNew();

            var parse = Stopwatch.StartNew();
            var document = GhJson.FromJson(json);
            parse.Stop();
            output.WriteLine($"FromJson: {parse.ElapsedMilliseconds} ms");

            Assert.NotNull(document);

            var fix = Stopwatch.StartNew();
            var fixResult = GhJson.Fix(document!);
            fix.Stop();
            output.WriteLine($"Fix: {fix.ElapsedMilliseconds} ms");

            var roundtrip = Stopwatch.StartNew();
            var normalizedJson = GhJson.ToJson(fixResult.Document);
            roundtrip.Stop();
            output.WriteLine($"ToJson: {roundtrip.ElapsedMilliseconds} ms");

            var validate = Stopwatch.StartNew();
            var isValid = GhJson.IsValid(normalizedJson, out var analysisMessage);
            validate.Stop();
            output.WriteLine($"IsValid: {validate.ElapsedMilliseconds} ms — {analysisMessage}");

            var layout = Stopwatch.StartNew();
            var layoutResult = GhJson.CalculateLayout(fixResult.Document);
            layout.Stop();
            output.WriteLine($"CalculateLayout: {layout.ElapsedMilliseconds} ms");

            total.Stop();
            output.WriteLine($"Total: {total.ElapsedMilliseconds} ms for {ComponentCount} components");

            Assert.True(isValid, $"Document should validate: {analysisMessage}");
            Assert.NotNull(layoutResult);
            Assert.True(
                total.ElapsedMilliseconds < GenerousMilliseconds,
                $"Core pipeline took {total.ElapsedMilliseconds} ms — exceeds the {GenerousMilliseconds} ms budget " +
                $"for {ComponentCount} components (parse={parse.ElapsedMilliseconds}, fix={fix.ElapsedMilliseconds}, " +
                $"serialize={roundtrip.ElapsedMilliseconds}, validate={validate.ElapsedMilliseconds}, " +
                $"layout={layout.ElapsedMilliseconds}).");
        }

        [Fact]
        public void Layout_LargeConnectedDocument_ProducesPositionsForAllNodes()
        {
            var document = GhJson.FromJson(BuildLargeDocumentJson());

            var layoutResult = GhJson.CalculateLayout(document!);

            Assert.Equal(ComponentCount, layoutResult.Positions.Count);
        }

        /// <summary>
        /// Builds a document shaped like observed generator output: a long main chain
        /// with fan-out plus several disconnected islands, all pivotless so layout does
        /// the full graph analysis.
        /// </summary>
        private static string BuildLargeDocumentJson()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var perIsland = ComponentCount / IslandCount;
            var id = 0;

            for (var island = 0; island < IslandCount; island++)
            {
                var islandStart = id + 1;
                for (var i = 0; i < perIsland; i++)
                {
                    id++;
                    builder = builder.AddComponent(new GhJsonComponent
                    {
                        Name = $"C{island}_{i}",
                        Id = id,
                        ComponentGuid = Guid.NewGuid(),
                    });
                }

                // Main chain within the island.
                for (var i = islandStart; i < islandStart + perIsland - 1; i++)
                {
                    builder = builder.AddConnection(Connect(i, i + 1));
                }

                // Extra fan-out edges: each node also feeds the node two ahead, giving
                // the ordering and crossing-reduction passes real work.
                for (var i = islandStart; i < islandStart + perIsland - 2; i += 2)
                {
                    builder = builder.AddConnection(Connect(i, i + 2));
                }
            }

            return GhJson.ToJson(builder.Build());
        }

        private static GhJsonConnection Connect(int fromId, int toId)
        {
            return new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = fromId, ParamIndex = 0 },
                To = new GhJsonConnectionEndpoint { Id = toId, ParamIndex = 0 },
            };
        }
    }
}
