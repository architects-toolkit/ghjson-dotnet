/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
using System.Linq;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Operations.TidyOperations;
using GhJSON.Core.Serialization;
using Xunit;

namespace GhJSON.Core.Tests.Operations
{
    public class TidyOperationsTests
    {
        #region LayoutAnalyzer Tests

        [Fact]
        public void LayoutAnalyzer_IdentifiesSourceAndSinkNodes()
        {
            var doc = CreateLinearChainDocument();
            var analyzer = new LayoutAnalyzer();

            var analysis = analyzer.Analyze(doc);

            Assert.Single(analysis.SourceNodes);
            Assert.Single(analysis.SinkNodes);
            Assert.Equal(1, analysis.SourceNodes[0]);
            Assert.Equal(3, analysis.SinkNodes[0]);
        }

        [Fact]
        public void LayoutAnalyzer_CalculatesDepths()
        {
            var doc = CreateLinearChainDocument();
            var analyzer = new LayoutAnalyzer();

            var analysis = analyzer.Analyze(doc);

            Assert.Equal(0, analysis.NodeDepths[1]);
            Assert.Equal(1, analysis.NodeDepths[2]);
            Assert.Equal(2, analysis.NodeDepths[3]);
            Assert.Equal(2, analysis.MaxDepth);
        }

        [Fact]
        public void LayoutAnalyzer_GroupsNodesByDepth()
        {
            var doc = CreateLinearChainDocument();
            var analyzer = new LayoutAnalyzer();

            var analysis = analyzer.Analyze(doc);

            Assert.Equal(3, analysis.DepthLevels.Count);
            Assert.Contains(1, analysis.DepthLevels[0]);
            Assert.Contains(2, analysis.DepthLevels[1]);
            Assert.Contains(3, analysis.DepthLevels[2]);
        }

        [Fact]
        public void LayoutAnalyzer_IdentifiesIslands()
        {
            var doc = CreateTwoIslandsDocument();
            var analyzer = new LayoutAnalyzer();

            var analysis = analyzer.Analyze(doc);

            Assert.Equal(2, analysis.Islands.Count);
        }

        [Fact]
        public void LayoutAnalyzer_HandlesEmptyDocument()
        {
            var doc = new GhJsonDocument();
            var analyzer = new LayoutAnalyzer();

            var analysis = analyzer.Analyze(doc);

            Assert.Empty(analysis.SourceNodes);
            Assert.Empty(analysis.SinkNodes);
            Assert.Equal(0, analysis.MaxDepth);
        }

        #endregion

        #region PivotOrganizer Tests

        [Fact]
        public void PivotOrganizer_OrganizesLinearChain()
        {
            var doc = CreateLinearChainDocument();
            var organizer = new PivotOrganizer(new PivotOrganizerOptions
            {
                HorizontalSpacing = 100,
                VerticalSpacing = 50
            });

            var result = organizer.Organize(doc);

            Assert.True(result.Success);
            Assert.True(result.WasModified);
            Assert.Equal(3, result.NodesOrganized);

            // Verify horizontal arrangement by depth
            var comp1 = doc.Components.First(c => c.Id == 1);
            var comp2 = doc.Components.First(c => c.Id == 2);
            var comp3 = doc.Components.First(c => c.Id == 3);

            Assert.True(comp1.Pivot!.X < comp2.Pivot!.X);
            Assert.True(comp2.Pivot.X < comp3.Pivot!.X);
        }

        [Fact]
        public void PivotOrganizer_OrganizesIslandsSeparately()
        {
            var doc = CreateTwoIslandsDocument();
            var organizer = new PivotOrganizer(new PivotOrganizerOptions
            {
                IslandSpacing = 200
            });

            var result = organizer.Organize(doc);

            Assert.True(result.Success);
            Assert.Equal(4, result.NodesOrganized);

            // Components from different islands should have different Y ranges
            var comp1 = doc.Components.First(c => c.Id == 1);
            var comp3 = doc.Components.First(c => c.Id == 3);

            // Island 2 should be offset from island 1
            Assert.NotEqual(comp1.Pivot!.Y, comp3.Pivot!.Y);
        }

        #endregion

        #region DocumentTidier Tests

        [Fact]
        public void DocumentTidier_TidiesDocument()
        {
            var doc = CreateLinearChainDocument();

            var result = DocumentTidier.TidyAll(doc);

            Assert.True(result.Success);
            Assert.True(result.WasModified);
        }

        [Fact]
        public void DocumentTidier_AnalyzeLayout_ReturnsAnalysis()
        {
            var doc = CreateLinearChainDocument();

            var analysis = DocumentTidier.AnalyzeLayout(doc);

            Assert.NotNull(analysis);
            Assert.Equal(2, analysis.MaxDepth);
        }

        [Fact]
        public void DocumentTidier_HandlesNullDocument()
        {
            var result = new DocumentTidier().Tidy(null!);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a linear chain: A -> B -> C
        /// </summary>
        private static GhJsonDocument CreateLinearChainDocument()
        {
            return new GhJsonDocument
            {
                SchemaVersion = "1.0",
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "A", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 2, Name = "B", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 3, Name = "C", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) }
                },
                Connections = new List<ConnectionPairing>
                {
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 1, ParamName = "Out" },
                        To = new Connection { Id = 2, ParamName = "In" }
                    },
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 2, ParamName = "Out" },
                        To = new Connection { Id = 3, ParamName = "In" }
                    }
                }
            };
        }

        /// <summary>
        /// Creates two disconnected islands: A -> B and C -> D
        /// </summary>
        private static GhJsonDocument CreateTwoIslandsDocument()
        {
            return new GhJsonDocument
            {
                SchemaVersion = "1.0",
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "A", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 2, Name = "B", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 3, Name = "C", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 4, Name = "D", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) }
                },
                Connections = new List<ConnectionPairing>
                {
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 1, ParamName = "Out" },
                        To = new Connection { Id = 2, ParamName = "In" }
                    },
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 3, ParamName = "Out" },
                        To = new Connection { Id = 4, ParamName = "In" }
                    }
                }
            };
        }

        #endregion
    }
}
