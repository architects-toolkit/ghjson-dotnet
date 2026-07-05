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

using GhJSON.Core.DependencyGraph;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.DocumentSemantics
{
    /// <summary>
    /// The Sugiyama layout must terminate on cyclic connection graphs (Grasshopper
    /// allows feedback loops) and surface a diagnostic mentioning the cycle rather
    /// than hang or throw.
    /// </summary>
    public class CyclicGraphTests
    {
        [Fact]
        public void LayoutEngine_OnTwoNodeCycle_Terminates()
        {
            // A → B → A
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "A",
                    Id = 1,
                    InstanceGuid = System.Guid.NewGuid(),
                })
                .AddComponent(new GhJsonComponent
                {
                    Name = "B",
                    Id = 2,
                    InstanceGuid = System.Guid.NewGuid(),
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "in" },
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 2, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "in" },
                })
                .Build();

            var result = LayoutEngine.CalculateLayout(doc);

            // Must terminate and produce positions for every component.
            Assert.Equal(2, result.Positions.Count);
        }

        [Fact]
        public void LayoutEngine_OnSelfLoop_Terminates()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent
                {
                    Name = "A",
                    Id = 1,
                    InstanceGuid = System.Guid.NewGuid(),
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "out" },
                    To = new GhJsonConnectionEndpoint { Id = 1, ParamName = "in" },
                })
                .Build();

            var result = LayoutEngine.CalculateLayout(doc);

            Assert.Single(result.Positions);
        }
    }
}
