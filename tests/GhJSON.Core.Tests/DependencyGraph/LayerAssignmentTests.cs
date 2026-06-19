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
    }
}
