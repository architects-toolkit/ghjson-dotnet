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
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.LayoutRefinements;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.LayoutRefinements
{
    /// <summary>Regression coverage for rigid port alignment after collision and anchor fitting.</summary>
    public class AlignmentConstraintsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Leaves_AlignOccupiedPortsWithoutMovingTheCentralComponent(bool pinCenter)
        {
            var f = Fixture.Chain();
            f.Positions[f.Key(2)] = new PointF(200f, 300f);
            if (pinCenter)
            {
                f.Pin(2);
            }

            var result = f.Apply();

            Assert.Equal(300f, result[f.Key(2)].Y);
            Assert.Equal(290f, result[f.Key(1)].Y);
            Assert.Equal(310f, result[f.Key(3)].Y);
            Assert.All(result, pair => Assert.Equal(f.Positions[pair.Key].X, pair.Value.X));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void FixedSourceOrSink_AnchorsTheEntireCompatibleChain(int fixedId)
        {
            var f = Fixture.Chain();
            f.Pin(fixedId);
            var anchor = f.Positions[f.Key(fixedId)];

            var result = f.Apply();

            Assert.Equal(anchor, result[f.Key(fixedId)]);
            Assert.Equal(result[f.Key(2)].Y - 10f, result[f.Key(1)].Y);
            Assert.Equal(result[f.Key(2)].Y + 10f, result[f.Key(3)].Y);
        }

        [Fact]
        public void CompatibleMultipleAnchors_AllowExactIntermediateAlignment()
        {
            var f = Fixture.Chain();
            f.Positions[f.Key(1)] = new PointF(0f, 40.25f);
            f.Positions[f.Key(3)] = new PointF(400f, 60.25f);
            f.Pin(1);
            f.Pin(3);

            var result = f.Apply();

            Assert.Equal(f.Fixed[f.Key(1)], result[f.Key(1)]);
            Assert.Equal(f.Fixed[f.Key(3)], result[f.Key(3)]);
            Assert.Equal(50.25f, result[f.Key(2)].Y);
        }

        [Fact]
        public void IncompatibleAnchors_RemainFixedAndRelaxTheConflictingEdge()
        {
            var f = Fixture.Chain();
            f.Pin(1);
            f.Pin(3);

            var result = f.Apply();

            Assert.All(f.Fixed, pair => Assert.Equal(pair.Value, result[pair.Key]));
            Assert.NotEqual(result[f.Key(2)].Y + 10f, result[f.Key(3)].Y);
            Assert.Equal(result[f.Key(2)].Y - 10f, result[f.Key(1)].Y);
        }

        [Fact]
        public void Collision_MovesTheAlignedChainAsAUnit()
        {
            var f = Fixture.Chain(4);
            f.Positions[f.Key(4)] = new PointF(200f, 0f);
            f.Metrics[f.Key(4)] = MetricsProviders.Parameter(80f, 100f);
            f.Pin(4);

            var result = f.Apply();

            Assert.True(Math.Abs(result[f.Key(2)].Y) >= 108f);
            Assert.Equal(result[f.Key(2)].Y - 10f, result[f.Key(1)].Y);
            Assert.Equal(result[f.Key(2)].Y + 10f, result[f.Key(3)].Y);
            Assert.Equal(f.Fixed[f.Key(4)], result[f.Key(4)]);
        }

        [Fact]
        public void BlockedInputLeaf_RelaxesWithoutBreakingTheFeasibleOutputAlignment()
        {
            var f = Fixture.Chain(4);
            f.Positions[f.Key(4)] = new PointF(0f, -10f);
            f.Pin(2);
            f.Pin(4);

            var result = f.Apply();

            Assert.Equal(0f, result[f.Key(2)].Y);
            Assert.Equal(10f, result[f.Key(3)].Y);
            Assert.NotEqual(-10f, result[f.Key(1)].Y);
            Assert.True(Math.Abs(result[f.Key(1)].Y + 10f) >= 68f);
        }

        [Fact]
        public void DistinctOutputPorts_AlignSeparateLeavesWhenTheirBoundsFit()
        {
            var f = new Fixture(3);
            f.Metrics[f.Key(1)] = MetricsProviders.Component(80f, 140f, outputDeltas: new[] { -50f, 50f });
            f.Metrics[f.Key(2)] = MetricsProviders.Parameter(80f, 20f);
            f.Metrics[f.Key(3)] = MetricsProviders.Parameter(80f, 20f);
            f.Positions[f.Key(3)] = new PointF(200f, 300f);
            f.Connect(1, 2);
            f.Connect(1, 3, 1);
            f.Pin(1);

            var result = f.Apply();

            Assert.Equal(-50f, result[f.Key(2)].Y);
            Assert.Equal(50f, result[f.Key(3)].Y);
            Assert.Equal(f.Fixed[f.Key(1)], result[f.Key(1)]);
        }

        [Fact]
        public void CrowdedFanOut_PreservesSeparationInsteadOfForcingBothLeavesOntoOnePort()
        {
            var f = new Fixture(3);
            f.Positions[f.Key(3)] = new PointF(200f, 300f);
            f.Connect(1, 2);
            f.Connect(1, 3);
            f.Pin(1);

            var result = f.Apply();

            Assert.True(Math.Abs(result[f.Key(2)].Y - result[f.Key(3)].Y) >= 68f);
            Assert.Equal(f.Fixed[f.Key(1)], result[f.Key(1)]);
        }

        [Fact]
        public void HorizontalWireCorridor_MovesWithTheGroupAroundFixedObstacles()
        {
            var f = new Fixture(3);
            f.Positions[f.Key(2)] = new PointF(400f, 0f);
            f.Positions[f.Key(3)] = new PointF(200f, 0f);
            f.Connect(1, 2);
            f.Pin(3);

            var result = f.Apply();

            Assert.Equal(result[f.Key(1)].Y, result[f.Key(2)].Y);
            Assert.True(Math.Abs(result[f.Key(1)].Y) >= 40f);
            Assert.Equal(f.Fixed[f.Key(3)], result[f.Key(3)]);
        }

        [Fact]
        public void WithoutMeasurement_LeavesTheInputDictionaryUntouched()
        {
            var f = Fixture.Chain();
            f.Options.NodeMetricsProvider = null;

            Assert.Same(f.Positions, f.Apply());
        }

        [Fact]
        public void FloatingParameters_UseTheirMeasuredNonCenteredGrips()
        {
            var f = Fixture.Chain();
            f.Positions[f.Key(2)] = new PointF(200f, 300f);
            f.Metrics[f.Key(1)] = new LayoutNodeMetrics(new SizeF(80f, 40f),
                LayoutNodeKind.Parameter, outputPortDeltas: new[] { 7f });
            f.Metrics[f.Key(3)] = new LayoutNodeMetrics(new SizeF(80f, 40f),
                LayoutNodeKind.Parameter, inputPortDeltas: new[] { -9f });
            f.Pin(2);

            var result = f.Apply();

            Assert.Equal(283f, result[f.Key(1)].Y);
            Assert.Equal(319f, result[f.Key(3)].Y);
        }

        [Fact]
        public void NamedPorts_UseMeasuredNamesInsteadOfTheNodeCenter()
        {
            var f = new Fixture(2);
            f.Metrics[f.Key(1)] = new LayoutNodeMetrics(new SizeF(80f, 80f), LayoutNodeKind.Component,
                null, new[] { 5f, 30f }, null, new Dictionary<string, int> { ["Result"] = 1 });
            f.Metrics[f.Key(2)] = new LayoutNodeMetrics(new SizeF(80f, 80f), LayoutNodeKind.Component,
                new[] { -20f, 20f }, null, new Dictionary<string, int> { ["B"] = 1 }, null);
            f.AddConnection(new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Result" },
                To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "B" },
            });
            f.Pin(2);

            var result = f.Apply();

            Assert.Equal(90f, result[f.Key(1)].Y);
            Assert.Equal(100f, result[f.Key(2)].Y);
        }

        [Fact]
        public void UnknownNamedPort_DoesNotInventAnAlignmentConstraint()
        {
            var f = new Fixture(2);
            f.Metrics[f.Key(2)] = MetricsProviders.Component(80f, 80f, new[] { -20f, 20f });
            f.AddConnection(new GhJsonConnection
            {
                From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                To = new GhJsonConnectionEndpoint { Id = 2, ParamName = "Unknown" },
            });

            Assert.Equal(f.Positions, f.Apply());
        }

        [Fact]
        public void ExplicitPortIndex_TakesPrecedenceOverTheDisplayName()
        {
            var f = new Fixture(2);
            f.Metrics[f.Key(2)] = new LayoutNodeMetrics(new SizeF(80f, 80f), LayoutNodeKind.Component,
                new[] { -20f, 20f }, null, new Dictionary<string, int> { ["B"] = 1 }, null);
            f.Connect(1, 2);
            f.Document.Connections![0].To.ParamName = "B";
            f.Pin(2);

            Assert.Equal(80f, f.Apply()[f.Key(1)].Y);
        }

        [Fact]
        public void ParallelWires_AllCompatiblePortCorridorsParticipate()
        {
            var f = new Fixture(3);
            f.Positions[f.Key(2)] = new PointF(400f, 0f);
            f.Positions[f.Key(3)] = new PointF(200f, 50f);
            f.Metrics[f.Key(1)] = MetricsProviders.Component(80f, 140f, outputDeltas: new[] { -50f, 50f });
            f.Metrics[f.Key(2)] = MetricsProviders.Component(80f, 140f, new[] { -50f, 50f });
            f.Metrics[f.Key(3)] = MetricsProviders.Parameter(80f, 20f);
            f.Connect(1, 2);
            f.Connect(1, 2, 1, 1);
            f.Pin(3);

            var result = f.Apply();

            Assert.Equal(result[f.Key(1)].Y, result[f.Key(2)].Y);
            Assert.True(Math.Abs(result[f.Key(1)].Y) >= 30f);
            Assert.True(Math.Abs(result[f.Key(1)].Y - 100f) >= 30f);
            Assert.Equal(f.Fixed[f.Key(3)], result[f.Key(3)]);
        }

        [Fact]
        public void DisabledWireClearance_DoesNotReserveWireCorridors()
        {
            var f = new Fixture(3);
            f.Positions[f.Key(2)] = new PointF(400f, 0f);
            f.Positions[f.Key(3)] = new PointF(200f, 0f);
            f.Connect(1, 2);
            f.Pin(3);
            f.Options.EnsureWireClearance = false;

            var result = f.Apply();

            Assert.Equal(0f, result[f.Key(1)].Y);
            Assert.Equal(0f, result[f.Key(2)].Y);
        }

        [Fact]
        public void FractionalPivotsAndPorts_AreNotIndividuallyRoundedOutOfAlignment()
        {
            var f = Fixture.Chain();
            f.Positions[f.Key(2)] = new PointF(200.125f, 0.125f);
            f.Metrics[f.Key(2)] = MetricsProviders.Component(80f, 60f, new[] { -10.25f }, new[] { 10.25f });
            f.Pin(2);

            var result = f.Apply();

            Assert.Equal(f.Fixed[f.Key(2)], result[f.Key(2)]);
            Assert.Equal(-10.125f, result[f.Key(1)].Y);
            Assert.Equal(10.375f, result[f.Key(3)].Y);
        }

        [Fact]
        public void ReorderedNodesAndConnections_ProduceTheSameConstraintSolution()
        {
            var f = Fixture.Chain(4);
            f.Positions[f.Key(4)] = new PointF(200f, 0f);
            f.Pin(4);
            var first = f.Apply();
            f.ReverseDocument();
            var reversed = f.Positions.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value);

            var second = AlignmentConstraints.Apply(reversed, f.Document, f.Options);

            Assert.Equal(first, second);
        }

        [Fact]
        public void MeasurementFailure_DegradesWithoutMovingOrDroppingNodes()
        {
            var f = Fixture.Chain();
            foreach (var id in f.Positions.Keys.ToList())
            {
                f.Positions[id] = PointF.Empty;
            }

            f.Options.NodeMetricsProvider = _ => throw new InvalidOperationException("Measurement unavailable.");

            Assert.Equal(f.Positions, f.Apply());
        }

        [Fact]
        public void SharedPipeline_AlignsLeavesAfterAnchorFitting()
        {
            var f = Fixture.Chain();
            f.Positions[f.Key(2)] = new PointF(200f, 300f);
            f.Pin(2);

            var result = f.Pipeline();

            Assert.Equal(f.Fixed[f.Key(2)], result[f.Key(2)]);
            Assert.Equal(290f, result[f.Key(1)].Y);
            Assert.Equal(310f, result[f.Key(3)].Y);
        }

        [Fact]
        public void FeasibleFanIn_AlignsEachLeafToItsOwnInputPort()
        {
            var f = new Fixture(3);
            f.Positions[f.Key(2)] = new PointF(0f, 200f);
            f.Positions[f.Key(3)] = new PointF(200f, 0f);
            f.Metrics[f.Key(3)] = MetricsProviders.Component(80f, 140f, new[] { -50f, 50f });
            f.Connect(1, 3);
            f.Connect(2, 3, input: 1);
            f.Pin(3);

            var result = f.Apply();

            Assert.Equal(-50f, result[f.Key(1)].Y);
            Assert.Equal(50f, result[f.Key(2)].Y);
        }

        [Fact]
        public void IncompatibleParallelPorts_RelaxOnlyTheUnsatisfiedConstraint()
        {
            var f = new Fixture(2);
            f.Metrics[f.Key(1)] = MetricsProviders.Component(80f, 60f, outputDeltas: new[] { -10f, 10f });
            f.Metrics[f.Key(2)] = MetricsProviders.Component(80f, 60f, new[] { -10f, 0f });
            f.Connect(1, 2);
            f.Connect(1, 2, 1, 1);
            f.Pin(2);

            var result = f.Apply();

            Assert.Equal(f.Fixed[f.Key(2)], result[f.Key(2)]);
            Assert.Equal(result[f.Key(2)].Y, result[f.Key(1)].Y);
            Assert.NotEqual(result[f.Key(2)].Y, result[f.Key(1)].Y + 10f);
        }

        [Fact]
        public void DisabledCollisionHandling_DoesNotRejectOverlappingLeafAlignments()
        {
            var f = new Fixture(3);
            f.Positions[f.Key(3)] = new PointF(200f, 300f);
            f.Connect(1, 2);
            f.Connect(1, 3);
            f.Pin(1);
            f.Options.AvoidCollisions = false;

            var result = f.Apply();

            Assert.Equal(0f, result[f.Key(2)].Y);
            Assert.Equal(0f, result[f.Key(3)].Y);
        }

        [Fact]
        public void RepeatingConstraintRefinement_IsIdempotent()
        {
            var f = Fixture.Chain(4);
            f.Positions[f.Key(4)] = new PointF(200f, 0f);
            f.Pin(4);
            var first = f.Apply();

            var second = AlignmentConstraints.Apply(first, f.Document, f.Options);

            Assert.Equal(first, second);
        }

        [Fact]
        public void AsymmetricLeafHeights_OutputLeafStillAlignsToItsPort()
        {
            // Live canvas geometry: Panel(195x34) -> Addition(65x44, two inputs) -> Panel(195x55).
            var f = new Fixture(3);
            f.Metrics[f.Key(1)] = MetricsProviders.Parameter(195f, 34f);
            f.Metrics[f.Key(2)] = MetricsProviders.Component(65f, 44f, new[] { -11f, 11f }, new[] { 0f });
            f.Metrics[f.Key(3)] = MetricsProviders.Parameter(195f, 55f);
            f.Connect(1, 2);
            f.Connect(2, 3);

            var result = f.Pipeline();

            Assert.Equal(result[f.Key(2)].Y - 11f, result[f.Key(1)].Y);
            Assert.Equal(result[f.Key(2)].Y, result[f.Key(3)].Y);
        }

        /// <summary>Supplies stable keys, measured nodes, authored anchors and real document edges.</summary>
        private sealed class Fixture
        {
            public GhJsonDocument Document { get; private set; }
            public Dictionary<Guid, PointF> Positions { get; } = new Dictionary<Guid, PointF>();
            public Dictionary<Guid, LayoutNodeMetrics> Metrics { get; } = new Dictionary<Guid, LayoutNodeMetrics>();
            public Dictionary<Guid, PointF> Fixed { get; } = new Dictionary<Guid, PointF>();
            public LayoutRefinementOptions Options { get; }

            public Fixture(int count)
            {
                var builder = GhJson.CreateDocumentBuilder();
                for (var id = 1; id <= count; id++)
                {
                    builder = builder.AddComponent(new GhJsonComponent { Name = "Node", Id = id, InstanceGuid = Key(id) });
                    this.Positions.Add(Key(id), new PointF((id - 1) * 200f, (id - 1) * 100f));
                    this.Metrics.Add(Key(id), MetricsProviders.Parameter(80f, 40f));
                }

                this.Document = builder.Build();
                this.Options = new LayoutRefinementOptions
                {
                    NodeMetricsProvider = MetricsProviders.Of(this.Metrics),
                    FixedPositions = this.Fixed,
                };
            }

            public static Fixture Chain(int count = 3)
            {
                var fixture = new Fixture(count);
                fixture.Positions[fixture.Key(2)] = new PointF(200f, 0f);
                fixture.Metrics[fixture.Key(2)] = MetricsProviders.Component(80f, 60f, new[] { -10f }, new[] { 10f });
                fixture.Connect(1, 2);
                fixture.Connect(2, 3);
                return fixture;
            }

            public Guid Key(int id) => new Guid(id, 0, 0, new byte[8]);

            public void Pin(int id) => this.Fixed[this.Key(id)] = this.Positions[this.Key(id)];

            public void Connect(int from, int to, int output = 0, int input = 0)
            {
                this.AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = from, ParamIndex = output },
                    To = new GhJsonConnectionEndpoint { Id = to, ParamIndex = input },
                });
            }

            public Dictionary<Guid, PointF> Apply() => AlignmentConstraints.Apply(this.Positions, this.Document, this.Options);

            public void AddConnection(GhJsonConnection connection)
            {
                this.Document = GhJson.CreateDocumentBuilder(this.Document).AddConnection(connection).Build();
            }

            public void ReverseDocument()
            {
                var builder = GhJson.CreateDocumentBuilder();
                foreach (var component in this.Document.Components.Reverse())
                {
                    builder = builder.AddComponent(component);
                }

                foreach (var connection in this.Document.Connections!.Reverse())
                {
                    builder = builder.AddConnection(connection);
                }

                this.Document = builder.Build();
            }

            public Dictionary<Guid, PointF> Pipeline() => LayoutRefinementEngine.ApplyRefinements(
                GhJson.CalculateLayout(this.Document, new LayoutOptions { NodeSizeProvider = id => this.Metrics[id].Size }),
                this.Document, this.Options);
        }
    }
}
