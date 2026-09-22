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
using Xunit;

namespace GhJSON.Core.Tests.LayoutRefinements
{
    /// <summary>
    /// Tests for <see cref="BoundsAwareSpacing"/>: measured bounds drive edge-to-edge
    /// column and row spacing without any Grasshopper dependency.
    /// </summary>
    public class BoundsAwareSpacingTests
    {
        [Fact]
        public void ApplyBoundsAwareSpacing_NoProvider_KeepsPositions()
        {
            var positions = new Dictionary<Guid, PointF>
            {
                [Guid.NewGuid()] = new PointF(0f, 0f),
                [Guid.NewGuid()] = new PointF(300f, 0f),
            };

            var result = BoundsAwareSpacing.ApplyBoundsAwareSpacing(positions, 80f, 28f);

            Assert.Equal(positions, result);
        }

        [Fact]
        public void ApplyBoundsAwareSpacing_MeasuredSizes_GiveEdgeToEdgeGap()
        {
            // Column 0 holds a 200-wide node, column 1 a 50-wide node. The left edge
            // stays anchored at -100 (the wide node's left), so column 1's center lands
            // at -100 + 200 + 80 + 25 = 205.
            var g1 = Guid.NewGuid();
            var g2 = Guid.NewGuid();
            var positions = new Dictionary<Guid, PointF>
            {
                [g1] = new PointF(0f, 0f),
                [g2] = new PointF(300f, 0f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [g1] = MetricsProviders.Component(200f, 100f),
                [g2] = MetricsProviders.Component(50f, 50f),
            });

            var result = BoundsAwareSpacing.ApplyBoundsAwareSpacing(positions, 80f, 28f, null, metrics);

            Assert.Equal(0f, result[g1].X);
            Assert.Equal(205f, result[g2].X);
            Assert.Equal(0f, result[g1].Y);
            Assert.Equal(0f, result[g2].Y);
        }

        [Fact]
        public void ApplyBoundsAwareSpacing_Rows_RespectMeasuredHeights()
        {
            // Same column, two rows: the 100-tall node owns row 0, the 20-tall node row 1.
            // topEdge = -50 → row0 center 0, row1 center -50 + 100 + 28 + 10 = 88.
            var g1 = Guid.NewGuid();
            var g2 = Guid.NewGuid();
            var positions = new Dictionary<Guid, PointF>
            {
                [g1] = new PointF(0f, 0f),
                [g2] = new PointF(0f, 60f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [g1] = MetricsProviders.Component(100f, 100f),
                [g2] = MetricsProviders.Component(100f, 20f),
            });

            var result = BoundsAwareSpacing.ApplyBoundsAwareSpacing(positions, 80f, 28f, null, metrics);

            Assert.Equal(0f, result[g1].Y);
            Assert.Equal(88f, result[g2].Y);
        }

        [Fact]
        public void ApplyBoundsAwareSpacing_PerIsland_KeepsSharedLeftEdge()
        {
            // Two islands: the first has a 200-wide node, the second only 50-wide nodes.
            // Each re-spaces around its own bounds origin, so both islands' left edges
            // stay where the core layout put them.
            var a1 = Guid.NewGuid();
            var a2 = Guid.NewGuid();
            var b1 = Guid.NewGuid();
            var positions = new Dictionary<Guid, PointF>
            {
                [a1] = new PointF(0f, 0f),
                [a2] = new PointF(400f, 0f),
                [b1] = new PointF(0f, 500f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [a1] = MetricsProviders.Component(200f, 60f),
                [a2] = MetricsProviders.Component(50f, 60f),
                [b1] = MetricsProviders.Component(50f, 60f),
            });
            var islands = new List<IReadOnlyList<Guid>>
            {
                new List<Guid> { a1, a2 },
                new List<Guid> { b1 },
            };

            var result = BoundsAwareSpacing.ApplyBoundsAwareSpacing(positions, 80f, 28f, islands, metrics);

            // Island A left edge at -100: a1.X = 0, a2.X = -100+200+80+25 = 205.
            // Island B left edge at -25: b1.X = 0 — its left edge aligns with its own
            // origin, not inflated by island A's wide column.
            Assert.Equal(0f, result[a1].X);
            Assert.Equal(205f, result[a2].X);
            Assert.Equal(0f, result[b1].X);
        }
    }
}
