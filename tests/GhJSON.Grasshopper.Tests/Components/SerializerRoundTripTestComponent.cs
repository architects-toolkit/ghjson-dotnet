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
using GhJSON.Core.Models.Document;
using GhJSON.Grasshopper.Serialization;
using GhJSON.Grasshopper.Serialization.DataTypes;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Tests.Components
{
    /// <summary>
    /// Test component for GhJSON serialization round-trip.
    /// Serializes selected components to GhJSON and back.
    /// </summary>
    public class SerializerRoundTripTestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerRoundTripTestComponent"/> class.
        /// </summary>
        public SerializerRoundTripTestComponent()
            : base("GhJSON Round-Trip Test", "GhJsonRTTest",
                "Tests GhJSON serialization round-trip for selected components",
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
            pManager.AddTextParameter("JSON", "J", "Serialized GhJSON", GH_ParamAccess.item);
            pManager.AddTextParameter("Results", "R", "Test results", GH_ParamAccess.list);
        }

        /// <inheritdoc/>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(0, ref run) || !run)
            {
                DA.SetData(0, false);
                DA.SetData(1, string.Empty);
                DA.SetDataList(2, new List<string> { "Not running - set Run to true" });
                return;
            }

            var results = new List<string>();
            bool allPassed = true;
            string jsonOutput = string.Empty;

            // Initialize geometric serializers
            GeometricSerializerRegistry.Initialize();

            try
            {
                // Get current document
                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    results.Add("✗ No active Grasshopper document");
                    DA.SetData(0, false);
                    DA.SetData(1, string.Empty);
                    DA.SetDataList(2, results);
                    return;
                }

                // Get selected objects (exclude this component)
                var selectedObjects = doc.SelectedObjects()
                    .Where(o => o.InstanceGuid != this.InstanceGuid)
                    .OfType<IGH_ActiveObject>()
                    .ToList();

                if (!selectedObjects.Any())
                {
                    results.Add("! No components selected. Select some components and run again.");
                    DA.SetData(0, true);
                    DA.SetData(1, string.Empty);
                    DA.SetDataList(2, results);
                    return;
                }

                results.Add($"Found {selectedObjects.Count} selected components");

                // Serialize to GhJSON
                var options = SerializationOptions.Standard;
                var ghjsonDoc = GhJsonSerializer.Serialize(selectedObjects, options);

                // Convert to JSON string
                jsonOutput = ghjsonDoc.ToJson();
                results.Add($"✓ Serialized to JSON ({jsonOutput.Length} characters)");

                // Validate the JSON
                if (GhJSON.Core.Validation.GhJsonValidator.Validate(jsonOutput, out var validationError))
                {
                    results.Add("✓ JSON validation passed");
                }
                else
                {
                    results.Add($"✗ JSON validation failed: {validationError}");
                    allPassed = false;
                }

                // Parse back to GhJsonDocument
                var parsedDoc = GhJsonDocument.FromJson(jsonOutput);
                if (parsedDoc != null)
                {
                    results.Add($"✓ Parsed back to GhJsonDocument");
                    results.Add($"  - Components: {parsedDoc.Components.Count}");
                    results.Add($"  - Connections: {parsedDoc.Connections.Count}");

                    // Verify component count matches
                    if (parsedDoc.Components.Count == selectedObjects.Count)
                    {
                        results.Add("✓ Component count matches");
                    }
                    else
                    {
                        results.Add($"✗ Component count mismatch: expected {selectedObjects.Count}, got {parsedDoc.Components.Count}");
                        allPassed = false;
                    }
                }
                else
                {
                    results.Add("✗ Failed to parse JSON back to GhJsonDocument");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Exception: {ex.Message}");
                allPassed = false;
            }

            // Summary
            results.Add("");
            results.Add(allPassed ? "=== ALL TESTS PASSED ===" : "=== SOME TESTS FAILED ===");

            DA.SetData(0, allPassed);
            DA.SetData(1, jsonOutput);
            DA.SetDataList(2, results);
        }

        /// <inheritdoc/>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <inheritdoc/>
        public override Guid ComponentGuid => new Guid("CFBB2D68-846E-4CDE-8E69-FA6A20AA8C6A");
    }
}
