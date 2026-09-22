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
using System.Drawing;
using GhJSON.Core.LayoutRefinements;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.LayoutRefinements
{
    /// <summary>
    /// Tests for <see cref="WireClearance"/>: wires skipping a column gain a clear
    /// corridor through measured bounds and port deltas alone.
    /// </summary>
    public class WireClearanceTests
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
        public void EnsureWireClearance_NoProvider_KeepsPositions()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();
            var positions = new Dictionary<Guid, PointF> { [Guid.NewGuid()] = new PointF(0f, 0f) };

            var result = WireClearance.EnsureWireClearance(positions, doc, null, null, 20f);

            Assert.Equal(positions, result);
        }

        [Fact]
        public void EnsureWireClearance_SkipEdgeWire_ClearsIntermediateNode()
        {
            // A → C skips column 1 where B sits on the wire's path: either B is pushed
            // through its nearer edge or C is nudged — afterwards the wire clears B's
            // bounds by at least the requested clearance.
            const float clearance = 20f;
            const float halfHeight = 30f;
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var c = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = a })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = b })
                .AddComponent(new GhJsonComponent { Name = "C", Id = 3, InstanceGuid = c })
                .AddConnection(Connect(1, 0, 3, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [a] = new PointF(0f, 0f),
                [b] = new PointF(200f, 0f),
                [c] = new PointF(400f, 0f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [a] = MetricsProviders.Component(100f, 60f),
                [b] = MetricsProviders.Component(100f, 60f),
                [c] = MetricsProviders.Component(100f, 60f),
            });

            var result = WireClearance.EnsureWireClearance(positions, doc, null, metrics, clearance);

            // The wire runs from A's right edge to C's left edge; evaluate its height at
            // B's column and verify it clears B's bounds.
            var fromPort = new PointF(result[a].X + 50f, result[a].Y);
            var toPort = new PointF(result[c].X - 50f, result[c].Y);
            var t = (result[b].X - fromPort.X) / (toPort.X - fromPort.X);
            var wireY = fromPort.Y + ((toPort.Y - fromPort.Y) * t);
            var distance = Math.Abs(wireY - result[b].Y);
            Assert.True(
                distance >= halfHeight + clearance - 0.5f,
                $"Wire should clear B's bounds by {clearance}px; got distance {distance:F2}");
        }

        [Fact]
        public void EnsureWireClearance_AdjacentColumns_NoMovement()
        {
            // A → B spans consecutive columns only: no intermediate node to clear.
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = a })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = b })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [a] = new PointF(0f, 0f),
                [b] = new PointF(200f, 0f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [a] = MetricsProviders.Component(100f, 60f),
                [b] = MetricsProviders.Component(100f, 60f),
            });

            var result = WireClearance.EnsureWireClearance(positions, doc, null, metrics, 20f);

            Assert.Equal(positions, result);
        }
    }
}
