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
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.LayoutRefinements;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.LayoutRefinements
{
    /// <summary>Verifies fixed-position boundaries and relative automatic placement.</summary>
    public class AnchoredPlacementTests
    {
        /// <summary>The shared pipeline anchors positions without changing the document.</summary>
        [Fact]
        public void RefinementPipeline_ForwardsAnchorsAndPreservesConnections()
        {
            var document = Fixture();
            var first = GhJson.GetLayoutKey(document.Components[0]);
            var second = GhJson.GetLayoutKey(document.Components[1]);
            var third = GhJson.GetLayoutKey(document.Components[2]);
            var layout = GhJson.CalculateLayout(document, new LayoutOptions
            {
                NodeSizeProvider = _ => new SizeF(60f, 40f),
            });
            var options = Options(new Dictionary<Guid, PointF> { [first] = new PointF(1000f, 400f) });

            var result = LayoutRefinementEngine.ApplyRefinements(layout, document, options);

            Assert.Equal(new PointF(1000f, 400f), result[first]);
            Assert.Equal(new PointF(1088f, 400f), result[second]);
            Assert.Equal(new PointF(1176f, 400f), result[third]);
            Assert.Equal(2, document.Connections!.Count);
            Assert.All(document.Components, component => Assert.Null(component.Pivot));
        }

        /// <summary>A fixed source or sink anchors the whole connected chain.</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void SingleAnchor_TranslatesConnectedNodesInEitherDirection(int anchorId)
        {
            var document = Fixture();
            var positions = Positions(document);
            var anchor = GhJson.GetLayoutKey(document.Components[anchorId - 1]);
            var options = Options(new Dictionary<Guid, PointF> { [anchor] = new PointF(1000f, 400f) });

            var result = AnchoredPlacement.Apply(positions, document, options);

            foreach (var pair in positions)
            {
                Assert.Equal(pair.Value.X + 1000f - positions[anchor].X, result[pair.Key].X);
                Assert.Equal(400f, result[pair.Key].Y);
            }
            Assert.Equal(new PointF(1000f, 400f), result[anchor]);
        }

        /// <summary>Missing nodes interpolate between surrounding fixed boundaries.</summary>
        [Fact]
        public void MultipleAnchors_InterpolatesBetweenFixedEndpoints()
        {
            var document = Fixture();
            var positions = Positions(document);
            var a = GhJson.GetLayoutKey(document.Components[0]);
            var b = GhJson.GetLayoutKey(document.Components[1]);
            var c = GhJson.GetLayoutKey(document.Components[2]);
            var options = Options(new Dictionary<Guid, PointF>
            {
                [a] = new PointF(1000f, 400f),
                [c] = new PointF(1400f, 400f),
            });

            var result = AnchoredPlacement.Apply(positions, document, options);

            Assert.Equal(new PointF(1200f, 400f), result[b]);
            Assert.Equal(options.FixedPositions![a], result[a]);
            Assert.Equal(options.FixedPositions[c], result[c]);
            Assert.Equal(result, AnchoredPlacement.Apply(positions, document, options));
        }

        /// <summary>An intervening fixed node shields its downstream subgraph.</summary>
        [Fact]
        public void InterveningFixedAnchor_BlocksInfluenceFromDistantAnchors()
        {
            var document = Fixture();
            var positions = Positions(document);
            var a = GhJson.GetLayoutKey(document.Components[0]);
            var b = GhJson.GetLayoutKey(document.Components[1]);
            var c = GhJson.GetLayoutKey(document.Components[2]);
            var options = Options(new Dictionary<Guid, PointF>
            {
                [a] = new PointF(0f, 400f),
                [b] = new PointF(1000f, 400f),
            });

            var result = AnchoredPlacement.Apply(positions, document, options);

            Assert.Equal(new PointF(1088f, 400f), result[c]);
            Assert.Equal(options.FixedPositions![a], result[a]);
            Assert.Equal(options.FixedPositions[b], result[b]);
        }

        /// <summary>Only islands without fixed boundaries use the free-space origin.</summary>
        [Fact]
        public void DisconnectedNodes_UseFreeSpaceOffsetOnlyWhenUnanchored()
        {
            var original = Fixture();
            var document = new GhJsonDocument("1.0", null, new[]
            {
                original.Components[0], original.Components[1], original.Components[2],
                new GhJsonComponent { Id = 4, Name = "Disconnected" },
            }, original.Connections, null);
            var positions = Positions(original);
            var a = GhJson.GetLayoutKey(document.Components[0]);
            var isolated = GhJson.GetLayoutKey(document.Components[3]);
            positions[isolated] = new PointF(30f, 300f);
            var options = Options(new Dictionary<Guid, PointF> { [a] = new PointF(1000f, 400f) });

            var result = AnchoredPlacement.Apply(positions, document, options);

            Assert.Equal(400f, result[a].Y);
            Assert.Equal(3020f, result[isolated].Y);
        }

        /// <summary>Collision avoidance must leave authored centers unchanged.</summary>
        [Fact]
        public void Collision_MovesOnlyUnfixedNodeToNearestFreeSlot()
        {
            var document = Fixture();
            var positions = Positions(document);
            var a = GhJson.GetLayoutKey(document.Components[0]);
            var b = GhJson.GetLayoutKey(document.Components[1]);
            positions[b] = new PointF(40f, 20f);
            var options = Options(new Dictionary<Guid, PointF> { [a] = new PointF(1000f, 400f) });

            var result = AnchoredPlacement.Apply(positions, document, options);

            Assert.Equal(new PointF(1000f, 400f), result[a]);
            Assert.Equal(68f, Math.Abs(result[b].Y - result[a].Y));
        }

        /// <summary>Pixel rounding must not force a short horizontal wire into another row.</summary>
        [Fact]
        public void Collision_ToleratesRoundedCompactGap()
        {
            var document = Fixture();
            var positions = Positions(document);
            var a = GhJson.GetLayoutKey(document.Components[0]);
            var b = GhJson.GetLayoutKey(document.Components[1]);
            positions[b] = new PointF(117.5f, 20f);
            var options = Options(new Dictionary<Guid, PointF> { [a] = new PointF(1000f, 400f) });

            var result = AnchoredPlacement.Apply(positions, document, options);

            Assert.Equal(new PointF(1087.5f, 400f), result[b]);
        }

        /// <summary>The collision flag remains effective when anchors are configured.</summary>
        [Fact]
        public void Collision_CanBeDisabledWithoutMovingAnchors()
        {
            var document = Fixture();
            var positions = Positions(document);
            var a = GhJson.GetLayoutKey(document.Components[0]);
            var b = GhJson.GetLayoutKey(document.Components[1]);
            positions[b] = new PointF(40f, 20f);
            var options = Options(new Dictionary<Guid, PointF> { [a] = new PointF(1000f, 400f) });
            options.AvoidCollisions = false;

            var result = AnchoredPlacement.Apply(positions, document, options);

            Assert.Equal(new PointF(1000f, 400f), result[a]);
            Assert.Equal(new PointF(1010f, 400f), result[b]);
        }

        /// <summary>Creates an id-only three-node chain.</summary>
        private static GhJsonDocument Fixture()
        {
            return GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Id = 1, Name = "A" })
                .AddComponent(new GhJsonComponent { Id = 2, Name = "B" })
                .AddComponent(new GhJsonComponent { Id = 3, Name = "C" })
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
                .Build();
        }

        /// <summary>Provides compact bounds centers for 60px-wide nodes.</summary>
        private static Dictionary<Guid, PointF> Positions(GhJsonDocument document)
        {
            return new Dictionary<Guid, PointF>
            {
                [GhJson.GetLayoutKey(document.Components[0])] = new PointF(30f, 20f),
                [GhJson.GetLayoutKey(document.Components[1])] = new PointF(118f, 20f),
                [GhJson.GetLayoutKey(document.Components[2])] = new PointF(206f, 20f),
            };
        }

        /// <summary>Combines fixed centers with deterministic host-independent metrics.</summary>
        private static LayoutRefinementOptions Options(IReadOnlyDictionary<Guid, PointF> anchors)
        {
            return new LayoutRefinementOptions
            {
                FixedPositions = anchors,
                UnanchoredOffset = new PointF(0f, 3000f),
                NodeMetricsProvider = _ => MetricsProviders.Parameter(60f, 40f),
            };
        }
    }
}
