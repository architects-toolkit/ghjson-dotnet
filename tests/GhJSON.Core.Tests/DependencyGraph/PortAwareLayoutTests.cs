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
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.DependencyGraph
{
    /// <summary>
    /// Tests for port-aware layout (A6): port-level endpoints in ordering and crossing
    /// counting, port-slot coordinate assignment, and port preservation through dummy
    /// routing chains on long edges.
    /// </summary>
    public class PortAwareLayoutTests
    {
        private static GhJsonConnection Connect(int fromId, int outIndex, int toId, int inIndex)
        {
            return new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = fromId, ParamIndex = outIndex },
                To = new GhJsonConnectionEndpoint { Id = toId, ParamIndex = inIndex },
            };
        }

        [Fact]
        public void CalculateLayout_LowerOutputPortOrdersTargetHigher()
        {
            // S has two outputs; it feeds T1 from the LOWER port (out1) and T2 from the
            // UPPER port (out0). Node-level barycenters tie (both = S's order), so only
            // port-aware ordering can place T2 above T1 deterministically.
            var guidS = Guid.NewGuid();
            var guidT1 = Guid.NewGuid();
            var guidT2 = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = guidS })
                .AddComponent(new GhJsonComponent { Name = "T1", Id = 2, InstanceGuid = guidT1 })
                .AddComponent(new GhJsonComponent { Name = "T2", Id = 3, InstanceGuid = guidT2 })
                .AddConnection(Connect(1, 1, 2, 0))
                .AddConnection(Connect(1, 0, 3, 0))
                .Build();

            var result = GhJson.CalculateLayout(doc);

            Assert.True(
                result.Positions[guidT2].Y < result.Positions[guidT1].Y,
                $"Upper output port should order its target higher (T2={result.Positions[guidT2].Y}, T1={result.Positions[guidT1].Y})");
        }

        [Fact]
        public void CalculateLayout_SourceOutputPortOffsetShiftsTarget()
        {
            // S's only wired port is out1 (lower half of a 2-port node); T's single input
            // is centered. Port-slot assignment pulls T down by the source port's offset:
            // (1 + 0.5)/2 - 0.5 = +0.25 of the 60px default height = 15px.
            var guidS = Guid.NewGuid();
            var guidT = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = guidS })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = guidT })
                .AddConnection(Connect(1, 1, 2, 0))
                .Build();

            var result = GhJson.CalculateLayout(doc);

            var dy = result.Positions[guidT].Y - result.Positions[guidS].Y;
            Assert.True(Math.Abs(dy - 15) <= 1, $"Expected target ~15px below source, got {dy}");
        }

        [Fact]
        public void CalculateLayout_TargetInputPortOffsetLiftsTarget()
        {
            // The wire enters T's LOWER input (in1 of 2), so T's center sits 15px above
            // S's centered output port for the wire to run horizontal.
            var guidS = Guid.NewGuid();
            var guidT = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = guidS })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = guidT })
                .AddConnection(Connect(1, 0, 2, 1))
                .Build();

            var result = GhJson.CalculateLayout(doc);

            var dy = result.Positions[guidS].Y - result.Positions[guidT].Y;
            Assert.True(Math.Abs(dy - 15) <= 1, $"Expected target ~15px above source, got {dy}");
        }

        [Fact]
        public void CalculateLayout_SinglePortChainStaysHorizontal()
        {
            // Centered single ports pull every hop onto the same row.
            var guidA = Guid.NewGuid();
            var guidB = Guid.NewGuid();
            var guidC = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guidA })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guidB })
                .AddComponent(new GhJsonComponent { Name = "C", Id = 3, InstanceGuid = guidC })
                .AddConnection(Connect(1, 0, 2, 0))
                .AddConnection(Connect(2, 0, 3, 0))
                .Build();

            var result = GhJson.CalculateLayout(doc);

            Assert.Equal(result.Positions[guidA].Y, result.Positions[guidB].Y);
            Assert.Equal(result.Positions[guidB].Y, result.Positions[guidC].Y);
        }

        [Fact]
        public void CalculateLayout_SkipEdgePreservesPortRowsThroughDummy()
        {
            // A -> B -> C plus a skip edge A.out1 -> C.in1 (spanning two layers). The
            // dummy chain must carry both port indices so C's lower input lands close to
            // A's lower output row rather than drifting to a generic row band.
            var guidA = Guid.NewGuid();
            var guidB = Guid.NewGuid();
            var guidC = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guidA })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guidB })
                .AddComponent(new GhJsonComponent { Name = "C", Id = 3, InstanceGuid = guidC })
                .AddConnection(Connect(1, 0, 2, 0))
                .AddConnection(Connect(2, 0, 3, 0))
                .AddConnection(Connect(1, 1, 3, 1))
                .Build();

            var result = GhJson.CalculateLayout(doc);

            Assert.True(result.Positions[guidA].X < result.Positions[guidB].X);
            Assert.True(result.Positions[guidB].X < result.Positions[guidC].X);
            var dy = Math.Abs(result.Positions[guidC].Y - result.Positions[guidA].Y);
            Assert.True(dy <= 6, $"Skip edge should keep C near A's row, got dy={dy}");
        }
    }
}
