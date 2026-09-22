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
    /// Tests for <see cref="CollisionResolver"/>: overlapping column members are pushed
    /// apart vertically by their measured heights plus padding.
    /// </summary>
    public class CollisionResolverTests
    {
        [Fact]
        public void AvoidCollisions_NoProvider_KeepsPositions()
        {
            var positions = new Dictionary<Guid, PointF>
            {
                [Guid.NewGuid()] = new PointF(0f, 0f),
                [Guid.NewGuid()] = new PointF(0f, 50f),
            };

            var result = CollisionResolver.AvoidCollisions(positions);

            Assert.Equal(positions, result);
        }

        [Fact]
        public void AvoidCollisions_OverlappingColumn_PushesLowerNodeDown()
        {
            // g1 occupies -50..50; g2 (50 tall) starts at y=60 so its top (35) overlaps.
            // g2 moves to top = 50 + 20 padding → center 70 + 25 = 95.
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
                [g2] = MetricsProviders.Component(100f, 50f),
            });

            var result = CollisionResolver.AvoidCollisions(positions, metrics);

            Assert.Equal(0f, result[g1].Y);
            Assert.Equal(95f, result[g2].Y);
        }

        [Fact]
        public void AvoidCollisions_UnmeasurableNode_DoesNotBlockColumn()
        {
            // A node with no metrics contributes no bounds and keeps its position;
            // measurable siblings still resolve collisions around it.
            var g1 = Guid.NewGuid();
            var ghost = Guid.NewGuid();
            var g2 = Guid.NewGuid();
            var positions = new Dictionary<Guid, PointF>
            {
                [g1] = new PointF(0f, 0f),
                [ghost] = new PointF(0f, 40f),
                [g2] = new PointF(0f, 60f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [g1] = MetricsProviders.Component(100f, 100f),
                [g2] = MetricsProviders.Component(100f, 50f),
            });

            var result = CollisionResolver.AvoidCollisions(positions, metrics);

            Assert.Equal(40f, result[ghost].Y);
            Assert.Equal(95f, result[g2].Y);
        }

        [Fact]
        public void AvoidCollisions_DistinctColumns_DoNotInteract()
        {
            // Same Y but different columns: each cluster resolves independently.
            var g1 = Guid.NewGuid();
            var g2 = Guid.NewGuid();
            var positions = new Dictionary<Guid, PointF>
            {
                [g1] = new PointF(0f, 0f),
                [g2] = new PointF(300f, 0f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [g1] = MetricsProviders.Component(100f, 100f),
                [g2] = MetricsProviders.Component(100f, 100f),
            });

            var result = CollisionResolver.AvoidCollisions(positions, metrics);

            Assert.Equal(positions, result);
        }
    }
}
