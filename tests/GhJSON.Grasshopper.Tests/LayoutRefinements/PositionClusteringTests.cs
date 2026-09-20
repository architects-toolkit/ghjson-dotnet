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
using System.Linq;
using GhJSON.Grasshopper.LayoutRefinements;
using Xunit;

namespace GhJSON.Grasshopper.Tests.LayoutRefinements
{
    /// <summary>
    /// Pure-logic tests for tolerance-based position clustering used by the layout
    /// refinement passes. No Grasshopper runtime required.
    /// </summary>
    public class PositionClusteringTests
    {
        [Fact]
        public void Cluster_PositionsWithinTolerance_ShareCluster()
        {
            var positions = new Dictionary<Guid, PointF>
            {
                [Guid.NewGuid()] = new PointF(0f, 0f),
                [Guid.NewGuid()] = new PointF(0.6f, 10f),
                [Guid.NewGuid()] = new PointF(-0.4f, 20f),
            };

            var clusters = PositionClustering.Cluster(positions, p => p.X);

            Assert.Single(clusters);
            Assert.Equal(3, clusters[0].Count);
        }

        [Fact]
        public void Cluster_GapAboveTolerance_SplitsClusters()
        {
            var g1 = Guid.NewGuid();
            var g2 = Guid.NewGuid();
            var g3 = Guid.NewGuid();
            var positions = new Dictionary<Guid, PointF>
            {
                [g1] = new PointF(0f, 0f),
                [g2] = new PointF(0.9f, 0f),
                [g3] = new PointF(50f, 0f),
            };

            var clusters = PositionClustering.Cluster(positions, p => p.X);

            Assert.Equal(2, clusters.Count);
            Assert.Equal(2, clusters[0].Count);
            Assert.Single(clusters[1]);
            Assert.Equal(g3, clusters[1][0].Key);
        }

        [Fact]
        public void Cluster_ChainedDrift_StaysInOneCluster()
        {
            // Each consecutive gap is below the tolerance even though the endpoints differ
            // by more: clustering follows the chain, not the distance to the first member.
            var positions = new Dictionary<Guid, PointF>
            {
                [Guid.NewGuid()] = new PointF(0f, 0f),
                [Guid.NewGuid()] = new PointF(0.9f, 0f),
                [Guid.NewGuid()] = new PointF(1.8f, 0f),
            };

            var clusters = PositionClustering.Cluster(positions, p => p.X);

            Assert.Single(clusters);
        }

        [Fact]
        public void Cluster_UnsortedInput_ReturnsOrderedClusters()
        {
            var positions = new Dictionary<Guid, PointF>
            {
                [Guid.NewGuid()] = new PointF(300f, 0f),
                [Guid.NewGuid()] = new PointF(0f, 0f),
                [Guid.NewGuid()] = new PointF(150f, 0f),
            };

            var clusters = PositionClustering.Cluster(positions, p => p.X);

            Assert.Equal(3, clusters.Count);
            Assert.Equal(0f, clusters[0][0].Value.X);
            Assert.Equal(150f, clusters[1][0].Value.X);
            Assert.Equal(300f, clusters[2][0].Value.X);
        }

        [Fact]
        public void Cluster_ByY_GroupsRows()
        {
            var positions = new Dictionary<Guid, PointF>
            {
                [Guid.NewGuid()] = new PointF(0f, 0f),
                [Guid.NewGuid()] = new PointF(500f, 0.5f),
                [Guid.NewGuid()] = new PointF(200f, 100f),
            };

            var clusters = PositionClustering.Cluster(positions, p => p.Y);

            Assert.Equal(2, clusters.Count);
        }

        [Fact]
        public void Cluster_EmptyInput_ReturnsEmpty()
        {
            var clusters = PositionClustering.Cluster(
                Enumerable.Empty<KeyValuePair<Guid, PointF>>(),
                p => p.X);

            Assert.Empty(clusters);
        }
    }
}
