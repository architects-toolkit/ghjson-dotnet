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
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.DependencyGraph
{
    /// <summary>
    /// Regression tests for the Sugiyama layer assignment.
    /// Covers deep chains (stack-overflow safety) and cyclic graphs (infinite-recursion
    /// safety) by exercising the public <see cref="GhJson.CalculateLayout"/> surface.
    /// </summary>
    public class LayerAssignmentTests
    {
        [Fact]
        public void CalculateLayout_DeepChain_DoesNotStackOverflow()
        {
            // A chain of several thousand components would blow the stack under the old
            // recursive DFS. With the iterative rewrite this must complete.
            const int depth = 5000;
            var builder = GhJson.CreateDocumentBuilder();
            for (var i = 1; i <= depth; i++)
            {
                builder = builder.AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = i,
                    InstanceGuid = Guid.NewGuid(),
                });
            }

            for (var i = 1; i < depth; i++)
            {
                builder = builder.AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = i, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = i + 1, ParamIndex = 0 },
                });
            }

            var doc = builder.Build();

            var result = GhJson.CalculateLayout(doc);

            Assert.NotNull(result);
            Assert.Equal(depth, result.Positions.Count);
        }

        [Fact]
        public void CalculateLayout_CyclicGraph_TerminatesAndReportsCycle()
        {
            // 1 -> 2 -> 3 -> 1 (three-node cycle)
            var ids = new[] { 1, 2, 3 };
            var builder = GhJson.CreateDocumentBuilder();
            foreach (var id in ids)
            {
                builder = builder.AddComponent(new GhJsonComponent
                {
                    Name = "Addition",
                    Id = id,
                    InstanceGuid = Guid.NewGuid(),
                });
            }

            builder = builder
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 3, ParamIndex = 0 },
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 3, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                });

            var doc = builder.Build();

            // Must terminate rather than stack-overflow or loop forever.
            var result = GhJson.CalculateLayout(doc);

            Assert.Equal(3, result.Positions.Count);
            Assert.Contains(
                result.Diagnostics,
                d => d.IndexOf("cycle", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void CalculateLayout_WithCustomSpacing_RespectsSpacing()
        {
            var builder = GhJson.CreateDocumentBuilder();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            builder = builder.AddComponent(new GhJsonComponent
            {
                Name = "A",
                Id = 1,
                InstanceGuid = guid1,
            });
            builder = builder.AddComponent(new GhJsonComponent
            {
                Name = "B",
                Id = 2,
                InstanceGuid = guid2,
            });
            builder = builder.AddConnection(new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                To = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
            });

            var doc = builder.Build();

            var options = new LayoutOptions { SpacingX = 300f, SpacingY = 150f };
            var result = GhJson.CalculateLayout(doc, options);

            Assert.Equal(2, result.Positions.Count);
            Assert.True(result.Positions[guid2].X >= result.Positions[guid1].X + 250);
        }

        [Fact]
        public void CalculateLayout_WithOrigin_AppliesOrigin()
        {
            var guid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "A",
                    Id = 1,
                    InstanceGuid = guid,
                })
                .Build();

            var options = new LayoutOptions { Origin = new GhJsonPivot { X = 500, Y = 600 } };
            var result = GhJson.CalculateLayout(doc, options);

            Assert.Equal(1, result.Positions.Count);
            Assert.Equal(500, result.Positions[guid].X);
            Assert.Equal(600, result.Positions[guid].Y);
        }

        [Fact]
        public void CalculateLayout_WithMultipleIslands_DetectsIslands()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guid2 })
                .Build();

            var result = GhJson.CalculateLayout(doc);

            Assert.Equal(2, result.Islands.Count);
        }

        [Fact]
        public void AssignPivots_UpdatesOnlyMatchedComponents()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guid2, Pivot = new GhJsonPivot { X = 999, Y = 999 } })
                .Build();

            var positions = new Dictionary<Guid, GhJsonPivot> { [guid1] = new GhJsonPivot { X = 100, Y = 200 } };
            var layoutResult = new LayoutResult(positions, new List<IReadOnlyList<Guid>>(), new List<string>());

            var updated = GhJson.AssignPivots(doc, layoutResult);

            Assert.Equal(100, updated.Components[0].Pivot!.X);
            Assert.Equal(200, updated.Components[0].Pivot!.Y);
            Assert.Equal(999, updated.Components[1].Pivot!.X);
            Assert.Equal(999, updated.Components[1].Pivot!.Y);
        }

        [Fact]
        public void AssignPivots_KeepsUnmatchedComponentsUnchanged()
        {
            var guid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid, Pivot = new GhJsonPivot { X = 50, Y = 50 } })
                .Build();

            var emptyLayout = new LayoutResult(new Dictionary<Guid, GhJsonPivot>(), new List<IReadOnlyList<Guid>>(), new List<string>());
            var updated = GhJson.AssignPivots(doc, emptyLayout);

            Assert.Equal(50, updated.Components[0].Pivot!.X);
            Assert.Equal(50, updated.Components[0].Pivot!.Y);
        }

        [Fact]
        public void GetLayoutKey_WithInstanceGuid_ReturnsGuid()
        {
            var guid = Guid.NewGuid();
            var component = new GhJsonComponent { Name = "A", InstanceGuid = guid };
            var key = GhJson.GetLayoutKey(component);

            Assert.Equal(guid, key);
        }

        [Fact]
        public void GetLayoutKey_WithoutInstanceGuid_ReturnsDerivedGuid()
        {
            var component = new GhJsonComponent { Name = "A", Id = 1 };
            var key = GhJson.GetLayoutKey(component);

            Assert.NotEqual(Guid.Empty, key);
        }

        [Fact]
        public void ReorganizePivots_CombinesCalculateAndAssign()
        {
            var guid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid })
                .Build();

            var updated = GhJson.ReorganizePivots(doc);

            Assert.NotNull(updated.Components[0].Pivot);
        }

        [Fact]
        public void CalculateLayout_LayoutResult_ContainsDiagnosticsForCycles()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = Guid.NewGuid() })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = Guid.NewGuid() })
                .AddConnection(new GhJsonConnection { From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" }, To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" } })
                .AddConnection(new GhJsonConnection { From = new GhJsonConnectionEndpoint { Id = 2, ParamName = "out" }, To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "in" } })
                .Build();

            var result = GhJson.CalculateLayout(doc);

            Assert.NotEmpty(result.Diagnostics);
            Assert.Contains(result.Diagnostics, d => d.IndexOf("cycle", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
