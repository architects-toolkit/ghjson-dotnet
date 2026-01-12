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
using GhJSON.Grasshopper.Validation;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Tests.Components
{
    /// <summary>
    /// Test component for GhJSON.Grasshopper.Validation.GhJsonGrasshopperValidator.
    /// </summary>
    public class GrasshopperValidatorTestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrasshopperValidatorTestComponent"/> class.
        /// </summary>
        public GrasshopperValidatorTestComponent()
            : base("GhJSON Grasshopper Validator Test", "GhJsonGHValTest",
                "Tests Grasshopper-specific GhJSON validation (component existence and message parsing)",
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
                // Case 1: invalid JSON should fail and mention invalid JSON in Errors.
                var invalidJson = "{ this is not valid json";
                bool ok = GhJsonGrasshopperValidator.Validate(invalidJson, out var message);
                if (!ok && message != null && message.Contains("Invalid JSON", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add("✓ Validate(invalid JSON): PASSED");
                }
                else
                {
                    results.Add($"✗ Validate(invalid JSON): FAILED (ok={ok}, msg='{message ?? ""}')");
                    allPassed = false;
                }

                // Case 2: syntactically valid GhJSON with an invalid componentGuid should fail component existence.
                // We keep connections empty to avoid relying on live GH component instantiation.
                var ghjsonInvalidComponent = """
{
  "schemaVersion": "1.0",
  "components": [
    {
      "id": 1,
      "name": "Fake Component",
      "componentGuid": "00000000-0000-0000-0000-000000000000",
      "instanceGuid": "11111111-1111-1111-1111-111111111111",
      "pivot": { "x": 0.0, "y": 0.0 }
    }
  ],
  "connections": []
}
""";

                ok = GhJsonGrasshopperValidator.Validate(ghjsonInvalidComponent, out message);
                if (!ok && message != null && message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add("✓ Validate(invalid componentGuid): PASSED");
                }
                else
                {
                    results.Add($"✗ Validate(invalid componentGuid): FAILED (ok={ok}, msg='{message ?? ""}')");
                    allPassed = false;
                }

                // Case 3: ValidateDetailed should parse into sections.
                var detailed = GhJsonGrasshopperValidator.ValidateDetailed(ghjsonInvalidComponent);
                if (detailed.Errors.Count > 0)
                {
                    results.Add("✓ ValidateDetailed: PASSED");
                }
                else
                {
                    results.Add("✗ ValidateDetailed: FAILED (expected at least one error)");
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
        public override Guid ComponentGuid => new Guid("1C261D0B-FFDB-447E-8D73-954AED631267");
    }
}
