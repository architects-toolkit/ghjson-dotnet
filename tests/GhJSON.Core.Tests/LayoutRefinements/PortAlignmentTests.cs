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
    /// Tests for <see cref="PortAlignment"/>: sources move so their wires enter the
    /// target's specific input port horizontally, driven purely by metrics port deltas.
    /// </summary>
    public class PortAlignmentTests
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
        public void AlignToPorts_NoProvider_KeepsPositions()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = Guid.NewGuid() })
                .Build();
            var positions = new Dictionary<Guid, PointF> { [Guid.NewGuid()] = new PointF(0f, 0f) };

            var result = PortAlignment.AlignToPorts(positions, doc);

            Assert.Equal(positions, result);
        }

        [Fact]
        public void AlignToPorts_ComponentInputDelta_LiftsSource()
        {
            // Target's input port sits 12px above its bounds center; the parameter
            // source must rise so its centered output grip meets the wire horizontally.
            var source = Guid.NewGuid();
            var target = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = target })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [source] = new PointF(0f, 50f),
                [target] = new PointF(300f, 100f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [source] = MetricsProviders.Parameter(80f, 20f),
                [target] = MetricsProviders.Component(100f, 60f, inputDeltas: new[] { -12f }),
            });

            var result = PortAlignment.AlignToPorts(positions, doc, metrics);

            Assert.Equal(88f, result[source].Y);
            Assert.Equal(100f, result[target].Y);
        }

        [Fact]
        public void AlignToPorts_SourceOutputDelta_SubtractedFromDesired()
        {
            // Both ports offset: desired source Y = targetY + inputDelta - outputDelta.
            var source = Guid.NewGuid();
            var target = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = target })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [source] = new PointF(0f, 50f),
                [target] = new PointF(300f, 100f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [source] = MetricsProviders.Component(100f, 60f, outputDeltas: new[] { 5f }),
                [target] = MetricsProviders.Component(100f, 60f, inputDeltas: new[] { -12f }),
            });

            var result = PortAlignment.AlignToPorts(positions, doc, metrics);

            Assert.Equal(83f, result[source].Y);
        }

        [Fact]
        public void AlignToPorts_ParameterTarget_AlignsToCenter()
        {
            // Parameter targets take wires at a centered grip → delta 0.
            var source = Guid.NewGuid();
            var target = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = target })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [source] = new PointF(0f, 50f),
                [target] = new PointF(300f, 100f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [source] = MetricsProviders.Parameter(80f, 20f),
                [target] = MetricsProviders.Parameter(120f, 40f),
            });

            var result = PortAlignment.AlignToPorts(positions, doc, metrics);

            Assert.Equal(100f, result[source].Y);
        }

        [Fact]
        public void AlignToPorts_MultipleTargets_UseMedian()
        {
            // S feeds T1 (delta -12 at y=100 → desired 88) and T2 (delta 0 at y=200 →
            // desired 200): the median 144 wins.
            var source = Guid.NewGuid();
            var t1 = Guid.NewGuid();
            var t2 = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T1", Id = 2, InstanceGuid = t1 })
                .AddComponent(new GhJsonComponent { Name = "T2", Id = 3, InstanceGuid = t2 })
                .AddConnection(Connect(1, 0, 2, 0))
                .AddConnection(Connect(1, 0, 3, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [source] = new PointF(0f, 50f),
                [t1] = new PointF(300f, 100f),
                [t2] = new PointF(300f, 200f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [source] = MetricsProviders.Parameter(80f, 20f),
                [t1] = MetricsProviders.Component(100f, 60f, inputDeltas: new[] { -12f }),
                [t2] = MetricsProviders.Component(100f, 60f, inputDeltas: new[] { 0f }),
            });

            var result = PortAlignment.AlignToPorts(positions, doc, metrics);

            Assert.Equal(144f, result[source].Y);
        }

        [Fact]
        public void AlignToPorts_UnknownPortIndex_VotesCentered()
        {
            // Out-of-range input index on a component still votes with a zero delta,
            // matching the original behavior (component center approximates its ports).
            var source = Guid.NewGuid();
            var target = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = target })
                .AddConnection(Connect(1, 0, 2, 5))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [source] = new PointF(0f, 50f),
                [target] = new PointF(300f, 100f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [source] = MetricsProviders.Parameter(80f, 20f),
                [target] = MetricsProviders.Component(100f, 60f, inputDeltas: new[] { -12f, 12f }),
            });

            var result = PortAlignment.AlignToPorts(positions, doc, metrics);

            Assert.Equal(100f, result[source].Y);
        }

        [Fact]
        public void AlignToPorts_UnmeasurableTarget_SkipsConnection()
        {
            // The target resolves no metrics at all — the connection cannot vote, so
            // the source keeps its position.
            var source = Guid.NewGuid();
            var target = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = target })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var positions = new Dictionary<Guid, PointF>
            {
                [source] = new PointF(0f, 50f),
                [target] = new PointF(300f, 100f),
            };
            var metrics = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
            {
                [source] = MetricsProviders.Parameter(80f, 20f),
            });

            var result = PortAlignment.AlignToPorts(positions, doc, metrics);

            Assert.Equal(50f, result[source].Y);
        }
    }
}
