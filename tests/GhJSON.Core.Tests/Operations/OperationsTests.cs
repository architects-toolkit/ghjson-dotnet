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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Operations;
using GhJSON.Core.Operations.FixOperations;
using GhJSON.Core.Operations.MergeOperations;
using GhJSON.Core.Serialization;
using Xunit;

namespace GhJSON.Core.Tests.Operations
{
    public class OperationsTests
    {
        #region IdAssigner Tests

        [Fact]
        public void IdAssigner_AssignsIds_ToComponentsWithoutIds()
        {
            var doc = CreateTestDocument(assignIds: false);
            var assigner = new IdAssigner();

            var result = assigner.Apply(doc);

            Assert.True(result.WasModified);
            Assert.Equal(2, result.ItemsAffected);
            Assert.Equal(1, doc.Components[0].Id);
            Assert.Equal(2, doc.Components[1].Id);
        }

        [Fact]
        public void IdAssigner_PreservesExistingIds()
        {
            var doc = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 5, Name = "A", Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 0, Name = "B", Pivot = new CompactPosition(0, 0) }
                }
            };
            var assigner = new IdAssigner();

            var result = assigner.Apply(doc);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.ItemsAffected);
            Assert.Equal(5, doc.Components[0].Id);
            Assert.Equal(6, doc.Components[1].Id);
        }

        [Fact]
        public void IdAssigner_ReassignAll_ReassignsAllIds()
        {
            var doc = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 100, Name = "A", Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 200, Name = "B", Pivot = new CompactPosition(0, 0) }
                }
            };
            var assigner = new IdAssigner();

            var result = assigner.ReassignAll(doc);

            Assert.True(result.WasModified);
            Assert.Equal(1, doc.Components[0].Id);
            Assert.Equal(2, doc.Components[1].Id);
        }

        #endregion

        #region GuidGenerator Tests

        [Fact]
        public void GuidGenerator_GeneratesGuids_ForComponentsWithoutGuids()
        {
            var doc = CreateTestDocument(assignGuids: false);
            var generator = new GuidGenerator();

            var result = generator.Apply(doc);

            Assert.True(result.WasModified);
            Assert.Equal(2, result.ItemsAffected);
            Assert.NotNull(doc.Components[0].InstanceGuid);
            Assert.NotNull(doc.Components[1].InstanceGuid);
            Assert.NotEqual(Guid.Empty, doc.Components[0].InstanceGuid!.Value);
        }

        [Fact]
        public void GuidGenerator_PreservesExistingGuids()
        {
            var existingGuid = Guid.NewGuid();
            var doc = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "A", InstanceGuid = existingGuid, Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 2, Name = "B", InstanceGuid = null, Pivot = new CompactPosition(0, 0) }
                }
            };
            var generator = new GuidGenerator();

            var result = generator.Apply(doc);

            Assert.True(result.WasModified);
            Assert.Equal(1, result.ItemsAffected);
            Assert.Equal(existingGuid, doc.Components[0].InstanceGuid);
        }

        #endregion

        #region MetadataPopulator Tests

        [Fact]
        public void MetadataPopulator_PopulatesMetadata()
        {
            var doc = CreateTestDocument();
            var populator = new MetadataPopulator();

            var result = populator.Apply(doc);

            Assert.True(result.WasModified);
            Assert.NotNull(doc.Metadata);
            Assert.Equal("1.0", doc.SchemaVersion);
            Assert.NotNull(doc.Metadata.Modified);
        }

        [Fact]
        public void MetadataPopulator_UpdatesComponentCount()
        {
            var doc = CreateTestDocument();
            var populator = new MetadataPopulator();

            populator.Apply(doc);

            Assert.Equal(2, doc.Metadata!.ComponentCount);
        }

        #endregion

        #region CountUpdater Tests

        [Fact]
        public void CountUpdater_UpdatesCounts()
        {
            var doc = CreateTestDocument();
            doc.Metadata = new DocumentMetadata { ComponentCount = 0 };
            var updater = new CountUpdater();

            var result = updater.Apply(doc);

            Assert.True(result.WasModified);
            Assert.Equal(2, doc.Metadata.ComponentCount);
        }

        #endregion

        #region DocumentFixer Tests

        [Fact]
        public void DocumentFixer_AppliesAllFixes()
        {
            var doc = CreateTestDocument(assignIds: false, assignGuids: false);
            
            var result = DocumentFixer.FixAll(doc);

            Assert.True(result.WasModified);
            Assert.True(result.Actions.Count >= 2);
            Assert.Equal(1, doc.Components[0].Id);
            Assert.NotNull(doc.Components[0].InstanceGuid);
        }

        [Fact]
        public void DocumentFixer_MinimalFix_OnlyAssignsIds()
        {
            var doc = CreateTestDocument(assignIds: false, assignGuids: false);
            
            var result = DocumentFixer.FixMinimal(doc);

            Assert.True(result.WasModified);
            Assert.Equal(1, doc.Components[0].Id);
            Assert.Null(doc.Components[0].InstanceGuid);
        }

        #endregion

        #region DocumentMerger Tests

        [Fact]
        public void DocumentMerger_MergesComponents()
        {
            var target = CreateTestDocument();
            var source = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "C", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) }
                }
            };
            var merger = new DocumentMerger();

            var result = merger.Merge(target, source);

            Assert.Equal(1, result.ComponentsAdded);
            Assert.Equal(3, target.Components.Count);
        }

        [Fact]
        public void DocumentMerger_TargetWins_SkipsDuplicates()
        {
            var sharedGuid = Guid.NewGuid();
            var target = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "A", InstanceGuid = sharedGuid, Pivot = new CompactPosition(0, 0) }
                }
            };
            var source = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "A-Duplicate", InstanceGuid = sharedGuid, Pivot = new CompactPosition(100, 0) }
                }
            };
            var merger = new DocumentMerger(new MergeOptions { ConflictResolution = ConflictResolution.TargetWins });

            var result = merger.Merge(target, source);

            Assert.Equal(1, result.Skipped);
            Assert.Single(target.Components);
            Assert.Equal("A", target.Components[0].Name);
        }

        [Fact]
        public void DocumentMerger_ReassignsIds()
        {
            var target = CreateTestDocument();
            var source = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "C", InstanceGuid = Guid.NewGuid(), Pivot = new CompactPosition(0, 0) }
                }
            };
            var merger = new DocumentMerger(new MergeOptions { ReassignIds = true });

            merger.Merge(target, source);

            Assert.Equal(3, target.Components[2].Id);
        }

        [Fact]
        public void DocumentMerger_MergesConnections()
        {
            var target = CreateTestDocument();
            var sourceGuid1 = Guid.NewGuid();
            var sourceGuid2 = Guid.NewGuid();
            var source = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Id = 1, Name = "C", InstanceGuid = sourceGuid1, Pivot = new CompactPosition(0, 0) },
                    new ComponentProperties { Id = 2, Name = "D", InstanceGuid = sourceGuid2, Pivot = new CompactPosition(100, 0) }
                },
                Connections = new List<ConnectionPairing>
                {
                    new ConnectionPairing
                    {
                        From = new Connection { Id = 1, ParamName = "Out" },
                        To = new Connection { Id = 2, ParamName = "In" }
                    }
                }
            };
            var merger = new DocumentMerger();

            var result = merger.Merge(target, source);

            Assert.Equal(1, result.ConnectionsAdded);
            Assert.Single(target.Connections);
        }

        #endregion

        #region PositionAdjuster Tests

        [Fact]
        public void PositionAdjuster_AdjustsPosition()
        {
            var component = new ComponentProperties { Pivot = new CompactPosition(50, 100) };
            var adjuster = new PositionAdjuster(offset: 200);

            adjuster.AdjustPosition(component, maxX: 100);

            Assert.Equal(350, component.Pivot!.X);
            Assert.Equal(100, component.Pivot.Y);
        }

        [Fact]
        public void PositionAdjuster_GetBoundingBox_ReturnsCorrectBounds()
        {
            var doc = new GhJsonDocument
            {
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties { Pivot = new CompactPosition(10, 20) },
                    new ComponentProperties { Pivot = new CompactPosition(100, 200) }
                }
            };
            var adjuster = new PositionAdjuster();

            var bounds = adjuster.GetBoundingBox(doc);

            Assert.NotNull(bounds);
            Assert.Equal(10, bounds.Value.MinX);
            Assert.Equal(20, bounds.Value.MinY);
            Assert.Equal(100, bounds.Value.MaxX);
            Assert.Equal(200, bounds.Value.MaxY);
        }

        #endregion

        #region Helper Methods

        private static GhJsonDocument CreateTestDocument(bool assignIds = true, bool assignGuids = true)
        {
            return new GhJsonDocument
            {
                SchemaVersion = "1.0",
                Components = new List<ComponentProperties>
                {
                    new ComponentProperties
                    {
                        Id = assignIds ? 1 : 0,
                        Name = "A",
                        InstanceGuid = assignGuids ? Guid.NewGuid() : null,
                        Pivot = new CompactPosition(0, 0)
                    },
                    new ComponentProperties
                    {
                        Id = assignIds ? 2 : 0,
                        Name = "B",
                        InstanceGuid = assignGuids ? Guid.NewGuid() : null,
                        Pivot = new CompactPosition(100, 0)
                    }
                },
                Connections = new List<ConnectionPairing>()
            };
        }

        #endregion
    }
}
