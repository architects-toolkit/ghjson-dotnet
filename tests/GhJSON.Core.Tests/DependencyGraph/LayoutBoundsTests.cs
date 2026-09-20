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
using System.Drawing;
using GhJSON.Core;
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.DependencyGraph
{
    /// <summary>
    /// Tests for real-bounds-aware layout: <see cref="LayoutOptions.NodeSizeProvider"/>,
    /// per-row heights in coordinate assignment, and the compact default spacing.
    /// </summary>
    public class LayoutBoundsTests
    {
        [Fact]
        public void LayoutOptions_Defaults_AreCompact()
        {
            var options = LayoutOptions.Default;

            Assert.Equal(80f, options.SpacingX);
            Assert.Equal(28f, options.SpacingY);
            Assert.Equal(100f, options.IslandSpacingY);
        }

        [Fact]
        public void CalculateLayout_NodeSizeProvider_WidensColumnGap()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guid2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
                })
                .Build();

            // A measures 500x40; B keeps the 100-wide default. Column pitch becomes
            // 500 + SpacingX, so the pivots (centers) sit (500+100)/2 + 80 = 380 apart.
            var options = new LayoutOptions
            {
                NodeSizeProvider = id => id == guid1 ? new SizeF(500f, 40f) : (SizeF?)null,
            };
            var result = GhJson.CalculateLayout(doc, options);

            var dx = result.Positions[guid2].X - result.Positions[guid1].X;
            Assert.Equal(380, dx);
        }

        [Fact]
        public void CalculateLayout_NodeSizeProvider_NullFallsBackToDefaults()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid1 })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guid2 })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
                })
                .Build();

            var options = new LayoutOptions { NodeSizeProvider = _ => null };
            var result = GhJson.CalculateLayout(doc, options);

            // Both nodes at the 100-wide default: pivots sit 100 + 80 apart.
            Assert.Equal(180, result.Positions[guid2].X - result.Positions[guid1].X);
        }

        [Fact]
        public void CalculateLayout_NodeSizeProvider_ThrowingProviderFallsBack()
        {
            var guid = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid })
                .Build();

            var options = new LayoutOptions
            {
                NodeSizeProvider = _ => throw new InvalidOperationException("no canvas"),
            };
            var result = GhJson.CalculateLayout(doc, options);

            Assert.Single(result.Positions);
        }

        [Fact]
        public void CalculateLayout_PerRowHeights_TallNodeOnlyInflatesItsRow()
        {
            // Two sources A (300 tall) and B (10 tall) feed C. With a single global row
            // unit their centers would sit 300 + SpacingY = 328 apart; with per-row heights
            // the spacing depends on which row each lands in (GUID-seeded order):
            //   A above B -> row0 = max(300,60)=300, row1 = 10 -> dy = 150+28+5 = 183
            //   B above A -> row0 = max(10,60)=60,  row1 = 300 -> dy = 30+28+150 = 208
            // Either way dy < 328, proving the tall node only inflates its own row.
            var guidA = Guid.NewGuid();
            var guidB = Guid.NewGuid();
            var guidC = Guid.NewGuid();
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guidA })
                .AddComponent(new GhJsonComponent { Name = "B", Id = 2, InstanceGuid = guidB })
                .AddComponent(new GhJsonComponent { Name = "C", Id = 3, InstanceGuid = guidC })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 1, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 3, ParamIndex = 0 },
                })
                .AddConnection(new GhJsonConnection
                {
                    From = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
                    To = new GhJsonConnectionEndpoint { Id = 3, ParamIndex = 1 },
                })
                .Build();

            var options = new LayoutOptions
            {
                NodeSizeProvider = id =>
                {
                    if (id == guidA) return new SizeF(100f, 300f);
                    if (id == guidB) return new SizeF(100f, 10f);
                    return new SizeF(100f, 60f);
                },
            };
            var result = GhJson.CalculateLayout(doc, options);

            var dy = Math.Abs(result.Positions[guidA].Y - result.Positions[guidB].Y);
            Assert.True(dy == 183 || dy == 208, $"Expected per-row spacing (183 or 208), got {dy}");
        }
    }
}
