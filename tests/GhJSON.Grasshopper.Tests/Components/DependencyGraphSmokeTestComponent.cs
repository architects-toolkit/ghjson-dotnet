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
using GhJSON.Core.Serialization;
using GhJSON.Grasshopper.Graph;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Tests.Components
{
    /// <summary>
    /// Smoke test component for DependencyGraphUtils.
    /// Builds a small synthetic GhJSON document and ensures layout produces pivots.
    /// </summary>
    public class DependencyGraphSmokeTestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyGraphSmokeTestComponent"/> class.
        /// </summary>
        public DependencyGraphSmokeTestComponent()
            : base("GhJSON Dependency Graph Smoke Test", "GhJsonGraphTest",
                "Smoke-tests DependencyGraphUtils layout on a small synthetic graph",
                "GhJSON", "Tests")
        {
        }

        /// <inheritdoc/>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Run the test", GH_ParamAccess.item, false);
        }

        /// <inheritdoc/>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Success", "S", "Whether all tests passed", GH_ParamAccess.item);
            pManager.AddTextParameter("Results", "R", "Test results", GH_ParamAccess.list);
        }

        /// <inheritdoc/>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(0, ref run) || !run)
            {
                DA.SetData(0, false);
                DA.SetDataList(1, new List<string> { "Not running - set Run to true" });
                return;
            }

            var results = new List<string>();
            bool allPassed = true;

            try
            {
                // Synthetic doc: 2 nodes, 1 connection (A -> B).
                // We don't rely on live Grasshopper Instances; CreateComponentGrid should still return a grid.
                var doc = new GrasshopperDocument
                {
                    SchemaVersion = "1.0",
                    Components = new List<ComponentProperties>
                    {
                        new ComponentProperties
                        {
                            Id = 1,
                            Name = "A",
                            ComponentGuid = Guid.NewGuid(),
                            InstanceGuid = Guid.NewGuid(),
                            Pivot = new CompactPosition(0, 0),
                        },
                        new ComponentProperties
                        {
                            Id = 2,
                            Name = "B",
                            ComponentGuid = Guid.NewGuid(),
                            InstanceGuid = Guid.NewGuid(),
                            Pivot = new CompactPosition(0, 0),
                        },
                    },
                    Connections = new List<ConnectionPairing>
                    {
                        new ConnectionPairing
                        {
                            From = new Connection { Id = 1, ParamName = "Out" },
                            To = new Connection { Id = 2, ParamName = "In" },
                        },
                    },
                };

                var grid = DependencyGraphUtils.CreateComponentGrid(doc, force: true);
                if (grid.Count == 2 && grid.TrueForAll(n => n.Pivot != System.Drawing.PointF.Empty))
                {
                    results.Add("✓ CreateComponentGrid: PASSED");
                }
                else
                {
                    results.Add($"✗ CreateComponentGrid: FAILED (count={grid.Count})");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Exception: {ex.Message}");
                allPassed = false;
            }

            results.Add("");
            results.Add(allPassed ? "=== ALL TESTS PASSED ===" : "=== SOME TESTS FAILED ===");

            DA.SetData(0, allPassed);
            DA.SetDataList(1, results);
        }

        /// <inheritdoc/>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <inheritdoc/>
        public override Guid ComponentGuid => new Guid("24A23231-D0C9-400B-B11B-7AFEBD739CF9");
    }
}
