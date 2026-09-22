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
using GhJSON.Core.DependencyGraph.Internal;
using Xunit;

namespace GhJSON.Core.Tests.DependencyGraph
{
    /// <summary>
    /// Tests for <see cref="WireCorridorPlanner"/>: skip-edge corridor clearance,
    /// push direction, per-node caps, and convergence when re-planned.
    /// </summary>
    public class WireCorridorPlannerTests
    {
        private readonly Xunit.Abstractions.ITestOutputHelper output;

        public WireCorridorPlannerTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// The v4 math island: slider A skips two columns to reach Sub.A, crossing the
        /// Add and Mul bounds. Columns: 0=sliders, 1={LT,Add}, 2={GT?,Mul}, 3=Sub,
        /// 4=Result.
        /// </summary>
        private static (
            Dictionary<Guid, RectangleF> Bounds,
            List<CorridorEdge> Edges,
            List<List<Guid>> Columns,
            Dictionary<string, Guid> Ids) BuildMathIsland()
        {
            var ids = new Dictionary<string, Guid>
            {
                ["A"] = Guid.NewGuid(),
                ["B"] = Guid.NewGuid(),
                ["C"] = Guid.NewGuid(),
                ["Add"] = Guid.NewGuid(),
                ["Mul"] = Guid.NewGuid(),
                ["Sub"] = Guid.NewGuid(),
                ["Result"] = Guid.NewGuid(),
                ["LT"] = Guid.NewGuid(),
                ["GT"] = Guid.NewGuid(),
            };

            var bounds = new Dictionary<Guid, RectangleF>
            {
                [ids["A"]] = new RectangleF(700, 314, 160, 20),
                [ids["B"]] = new RectangleF(700, 354, 160, 20),
                [ids["C"]] = new RectangleF(700, 274, 160, 20),
                [ids["Add"]] = new RectangleF(944, 316, 65, 44),
                [ids["Mul"]] = new RectangleF(1100, 306, 65, 44),
                [ids["Sub"]] = new RectangleF(1253, 296, 65, 44),
                [ids["Result"]] = new RectangleF(1398, 300, 80, 38),
                [ids["LT"]] = new RectangleF(940, 217, 73, 44),
                [ids["GT"]] = new RectangleF(1093, 231, 80, 38),
            };

            CorridorEdge E(string from, int fromCol, PointF fromPort, string to, int toCol, PointF toPort)
            {
                return new CorridorEdge
                {
                    From = ids[from],
                    To = ids[to],
                    FromPort = fromPort,
                    ToPort = toPort,
                    FromColumn = fromCol,
                    ToColumn = toCol,
                };
            }

            var edges = new List<CorridorEdge>
            {
                E("A", 0, new PointF(860, 324), "Add", 1, new PointF(944, 327)),
                E("A", 0, new PointF(860, 324), "Sub", 3, new PointF(1253, 307)),
                E("A", 0, new PointF(860, 324), "LT", 1, new PointF(940, 228)),
                E("B", 0, new PointF(860, 364), "Add", 1, new PointF(944, 349)),
                E("C", 0, new PointF(860, 284), "Mul", 2, new PointF(1100, 317)),
                E("C", 0, new PointF(860, 284), "LT", 1, new PointF(940, 250)),
                E("Add", 1, new PointF(1009, 338), "Mul", 2, new PointF(1100, 339)),
                E("Mul", 2, new PointF(1165, 328), "Sub", 3, new PointF(1253, 329)),
                E("Sub", 3, new PointF(1318, 318), "Result", 4, new PointF(1398, 319)),
                E("LT", 1, new PointF(1013, 239), "GT", 2, new PointF(1093, 250)),
            };

            var columns = new List<List<Guid>>
            {
                new List<Guid> { ids["A"], ids["B"], ids["C"] },
                new List<Guid> { ids["LT"], ids["Add"] },
                new List<Guid> { ids["GT"], ids["Mul"] },
                new List<Guid> { ids["Sub"] },
                new List<Guid> { ids["Result"] },
            };

            return (bounds, edges, columns, ids);
        }

        [Fact]
        public void Plan_SkipEdgeThroughUpperHalf_PushesObstaclesDownAndNudgesTargetUp()
        {
            var (bounds, edges, columns, ids) = BuildMathIsland();

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, clearance: 20f);
            foreach (var kv in deltas)
            {
                output.WriteLine($"delta {ids.First(x => x.Value == kv.Key).Key} = {kv.Value}");
            }

            // Add and Mul must move down (the A→Sub wire runs through their upper half).
            Assert.True(deltas[ids["Add"]] > 0f, $"Add should move down, got {deltas.GetValueOrDefault(ids["Add"])}");
            Assert.True(deltas[ids["Mul"]] > 0f, $"Mul should move down, got {deltas.GetValueOrDefault(ids["Mul"])}");

            // The skip-edge target (Sub) is nudged up so the wire gains slope.
            Assert.True(deltas[ids["Sub"]] < 0f, $"Sub should move up, got {deltas.GetValueOrDefault(ids["Sub"])}");
        }

        [Fact]
        public void Plan_SkipEdge_ConvergesOnReplan()
        {
            var (bounds, edges, columns, ids) = BuildMathIsland();
            const float clearance = 20f;

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, clearance);
            foreach (var kv in deltas)
            {
                output.WriteLine($"pass1 {ids.First(x => x.Value == kv.Key).Key} = {kv.Value}");
            }

            // Apply the plan: shift bounds and port points by each node's delta.
            float Delta(Guid g) => deltas.TryGetValue(g, out var d) ? d : 0f;
            var bounds2 = bounds.ToDictionary(
                kv => kv.Key,
                kv => new RectangleF(kv.Value.X, kv.Value.Y + Delta(kv.Key), kv.Value.Width, kv.Value.Height));
            var edges2 = edges
                .Select(e => new CorridorEdge
                {
                    From = e.From,
                    To = e.To,
                    FromColumn = e.FromColumn,
                    ToColumn = e.ToColumn,
                    FromPort = new PointF(e.FromPort.X, e.FromPort.Y + Delta(e.From)),
                    ToPort = new PointF(e.ToPort.X, e.ToPort.Y + Delta(e.To)),
                })
                .ToList();

            var deltas2 = WireCorridorPlanner.PlanVerticalDeltas(bounds2, edges2, columns, clearance);
            foreach (var kv in deltas2)
            {
                output.WriteLine($"pass2 {ids.First(x => x.Value == kv.Key).Key} = {kv.Value}");
            }

            // The planner is iterative by design: a second pass may find residual
            // sub-pixel corrections, but must never produce large new moves.
            Assert.All(
                deltas2.Values,
                d => Assert.True(Math.Abs(d) < 2f, $"Residual delta {d}px should be sub-pixel"));
        }

        [Fact]
        public void Plan_AdjacentColumns_NoDeltas()
        {
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var bounds = new Dictionary<Guid, RectangleF>
            {
                [a] = new RectangleF(0, 0, 100, 20),
                [b] = new RectangleF(200, 0, 60, 40),
            };
            var edges = new List<CorridorEdge>
            {
                new CorridorEdge
                {
                    From = a, To = b,
                    FromPort = new PointF(100, 10), ToPort = new PointF(200, 20),
                    FromColumn = 0, ToColumn = 1,
                },
            };
            var columns = new List<List<Guid>> { new List<Guid> { a }, new List<Guid> { b } };

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, 20f);

            Assert.Empty(deltas);
        }

        [Fact]
        public void Plan_WireThroughLowerHalf_PushesObstacleUp()
        {
            var s = Guid.NewGuid();
            var t = Guid.NewGuid();
            var m = Guid.NewGuid();
            var bounds = new Dictionary<Guid, RectangleF>
            {
                [s] = new RectangleF(0, 0, 10, 10),
                [t] = new RectangleF(200, 0, 10, 10),
                [m] = new RectangleF(80, -30, 40, 40), // spans -30..10; wire y=5 sits in lower half
            };
            var edges = new List<CorridorEdge>
            {
                new CorridorEdge
                {
                    From = s, To = t,
                    FromPort = new PointF(10, 5), ToPort = new PointF(200, 5),
                    FromColumn = 0, ToColumn = 2,
                },
            };
            var columns = new List<List<Guid>> { new List<Guid> { s }, new List<Guid> { m }, new List<Guid> { t } };

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, 20f);

            Assert.True(
                deltas.TryGetValue(m, out var dy) && dy < 0f,
                $"Obstacle below wire should move up, got {deltas.GetValueOrDefault(m)}");
        }

        [Fact]
        public void Plan_DeepViolation_CappedAtMaxNodeDelta()
        {
            var s = Guid.NewGuid();
            var t = Guid.NewGuid();
            var m = Guid.NewGuid();
            var bounds = new Dictionary<Guid, RectangleF>
            {
                [s] = new RectangleF(0, 0, 10, 10),
                [t] = new RectangleF(200, 0, 10, 10),
                [m] = new RectangleF(80, -100, 40, 150), // huge obstacle, wire deep inside
            };
            var edges = new List<CorridorEdge>
            {
                new CorridorEdge
                {
                    From = s, To = t,
                    FromPort = new PointF(10, 5), ToPort = new PointF(200, 5),
                    FromColumn = 0, ToColumn = 2,
                },
            };
            var columns = new List<List<Guid>> { new List<Guid> { s }, new List<Guid> { m }, new List<Guid> { t } };

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, 20f);

            Assert.True(
                Math.Abs(deltas.GetValueOrDefault(m)) <= WireCorridorPlanner.MaxNodeDelta,
                $"Delta must respect the {WireCorridorPlanner.MaxNodeDelta}px cap, got {deltas.GetValueOrDefault(m)}");
        }

        [Fact]
        public void Plan_WireAlreadyClear_NoDeltas()
        {
            var s = Guid.NewGuid();
            var t = Guid.NewGuid();
            var m = Guid.NewGuid();
            var bounds = new Dictionary<Guid, RectangleF>
            {
                [s] = new RectangleF(0, 0, 10, 10),
                [t] = new RectangleF(200, 0, 10, 10),
                [m] = new RectangleF(80, 60, 40, 40), // far below the wire
            };
            var edges = new List<CorridorEdge>
            {
                new CorridorEdge
                {
                    From = s, To = t,
                    FromPort = new PointF(10, 5), ToPort = new PointF(200, 5),
                    FromColumn = 0, ToColumn = 2,
                },
            };
            var columns = new List<List<Guid>> { new List<Guid> { s }, new List<Guid> { m }, new List<Guid> { t } };

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, 20f);

            Assert.Empty(deltas);
        }

        [Fact]
        public void Plan_IslandsIsolated_UnrelatedNodesUntouched()
        {
            var (bounds, edges, columns, ids) = BuildMathIsland();

            // Second island far below — must not collect deltas from island 1's pass.
            var x = Guid.NewGuid();
            var y = Guid.NewGuid();
            bounds[x] = new RectangleF(700, 600, 160, 20);
            bounds[y] = new RectangleF(900, 600, 60, 40);
            columns.Add(new List<Guid> { x });
            columns.Add(new List<Guid> { y });
            edges.Add(new CorridorEdge
            {
                From = x, To = y,
                FromPort = new PointF(860, 610), ToPort = new PointF(900, 620),
                FromColumn = 5, ToColumn = 6,
            });

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, 20f);

            Assert.False(deltas.ContainsKey(x));
            Assert.False(deltas.ContainsKey(y));
        }

        [Fact]
        public void Plan_TargetNudgeCapped()
        {
            // An extremely deep obstacle forces a large theoretical nudge; the target
            // must stay within MaxTargetNudge.
            var s = Guid.NewGuid();
            var t = Guid.NewGuid();
            var m = Guid.NewGuid();
            var bounds = new Dictionary<Guid, RectangleF>
            {
                [s] = new RectangleF(0, 0, 10, 10),
                [t] = new RectangleF(600, 0, 10, 10),
                [m] = new RectangleF(200, -10, 40, 40), // wire y=5 inside, upper half
            };
            var edges = new List<CorridorEdge>
            {
                new CorridorEdge
                {
                    From = s, To = t,
                    FromPort = new PointF(10, 5), ToPort = new PointF(600, 5),
                    FromColumn = 0, ToColumn = 2,
                },
            };
            var columns = new List<List<Guid>> { new List<Guid> { s }, new List<Guid> { m }, new List<Guid> { t } };

            var deltas = WireCorridorPlanner.PlanVerticalDeltas(bounds, edges, columns, 20f);

            Assert.True(
                Math.Abs(deltas.GetValueOrDefault(t)) <= WireCorridorPlanner.MaxTargetNudge,
                $"Target nudge must respect the {WireCorridorPlanner.MaxTargetNudge}px cap, got {deltas.GetValueOrDefault(t)}");
        }
    }
}
