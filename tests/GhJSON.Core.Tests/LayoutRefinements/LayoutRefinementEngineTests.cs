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
    /// Tests for <see cref="LayoutRefinementEngine"/>: the host-independent refinement
    /// pipeline consumes a metrics provider and preserves every layout key.
    /// </summary>
    public class LayoutRefinementEngineTests
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
        public void ApplyRefinements_NullArguments_Throw()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();
            var layout = GhJson.CalculateLayout(doc);

            Assert.Throws<ArgumentNullException>(() => LayoutRefinementEngine.ApplyRefinements(null!, doc));
            Assert.Throws<ArgumentNullException>(() => LayoutRefinementEngine.ApplyRefinements(layout, null!));
        }

        [Fact]
        public void ApplyRefinements_NoPasses_KeepsLayoutPositions()
        {
            var guidA = Guid.NewGuid();
            var guidB = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guidA })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guidB })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var layout = GhJson.CalculateLayout(doc);

            var result = LayoutRefinementEngine.ApplyRefinements(layout, doc, LayoutRefinementOptions.None);

            Assert.Equal(layout.Positions.Count, result.Count);
            foreach (var kvp in layout.Positions)
            {
                Assert.Equal((float)kvp.Value.X, result[kvp.Key].X);
                Assert.Equal((float)kvp.Value.Y, result[kvp.Key].Y);
            }
        }

        [Fact]
        public void ApplyRefinements_WithMetrics_AlignsSourceToTargetPort()
        {
            // Parameter source → component target whose input port sits 12px above
            // center: after refinement the source rests 12px above the target's row.
            var source = Guid.NewGuid();
            var target = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "S", Id = 1, InstanceGuid = source })
                .AddComponent(new GhJsonComponent { Name = "T", Id = 2, InstanceGuid = target })
                .AddConnection(Connect(1, 0, 2, 0))
                .Build();
            var layout = GhJson.CalculateLayout(doc);
            var options = new LayoutRefinementOptions
            {
                NodeMetricsProvider = MetricsProviders.Of(new Dictionary<Guid, LayoutNodeMetrics>
                {
                    [source] = MetricsProviders.Parameter(80f, 20f),
                    [target] = MetricsProviders.Component(100f, 60f, inputDeltas: new[] { -12f }),
                }),
            };

            var result = LayoutRefinementEngine.ApplyRefinements(layout, doc, options);

            Assert.Equal(result[target].Y - 12f, result[source].Y);
            Assert.True(result[source].X < result[target].X);
        }
    }
}
