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
using GhJSON.Grasshopper.Introspection;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Tests.Components
{
    /// <summary>
    /// Test component for ComponentSpecBuilder.
    /// </summary>
    public class ComponentSpecBuilderTestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentSpecBuilderTestComponent"/> class.
        /// </summary>
        public ComponentSpecBuilderTestComponent()
            : base("GhJSON Component Spec Builder Test", "GhJsonSpecTest",
                "Tests component spec generation for AI tooling scenarios",
                "GhJSON", "Tests")
        {
        }

        /// <inheritdoc/>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Run the test", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Component Name", "C", "Component name or nickname to test (defaults to 'Panel')", GH_ParamAccess.item, "Panel");
        }

        /// <inheritdoc/>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Success", "S", "Whether all tests passed", GH_ParamAccess.item);
            pManager.AddTextParameter("Results", "R", "Test results", GH_ParamAccess.list);
            pManager.AddTextParameter("Spec", "J", "Generated component spec JSON", GH_ParamAccess.item);
            pManager.AddTextParameter("Document", "D", "Generated document JSON", GH_ParamAccess.item);
        }

        /// <inheritdoc/>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            string componentName = "Panel";

            DA.GetData(0, ref run);
            DA.GetData(1, ref componentName);

            if (!run)
            {
                DA.SetData(0, false);
                DA.SetDataList(1, new List<string> { "Not running - set Run to true" });
                DA.SetData(2, string.Empty);
                DA.SetData(3, string.Empty);
                return;
            }

            var results = new List<string>();
            bool allPassed = true;
            string specJson = string.Empty;
            string docJson = string.Empty;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "UserText", "Hello" },
                    { "ExampleNumber", 5 },
                };

                JObject? spec = ComponentSpecBuilder.GenerateComponentSpec(componentName, parameters);
                if (spec == null)
                {
                    results.Add($"✗ GenerateComponentSpec: FAILED (component not found: '{componentName}')");
                    allPassed = false;
                }
                else
                {
                    specJson = spec.ToString();
                    results.Add("✓ GenerateComponentSpec: PASSED");

                    bool hasGuid = spec["guid"] != null;
                    bool hasName = spec["name"] != null;
                    bool hasNickname = spec["nickname"] != null;
                    bool hasInstanceGuid = spec["instanceGuid"] != null;

                    if (!hasGuid || !hasName || !hasNickname || !hasInstanceGuid)
                    {
                        results.Add("✗ GenerateComponentSpec: FAILED (missing required fields)");
                        allPassed = false;
                    }
                }

                if (spec != null)
                {
                    var doc = ComponentSpecBuilder.GenerateGhJsonDocument(new List<JObject> { spec });
                    if (doc == null)
                    {
                        results.Add("✗ GenerateGhJsonDocument: FAILED (returned null)");
                        allPassed = false;
                    }
                    else
                    {
                        docJson = doc.ToString();
                        bool hasComponents = doc["components"] is JArray arr && arr.Count == 1;
                        bool hasConnections = doc["connections"] is JArray;

                        if (hasComponents && hasConnections)
                        {
                            results.Add("✓ GenerateGhJsonDocument: PASSED");
                        }
                        else
                        {
                            results.Add("✗ GenerateGhJsonDocument: FAILED (unexpected structure)");
                            allPassed = false;
                        }
                    }
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
            DA.SetData(2, specJson);
            DA.SetData(3, docJson);
        }

        /// <inheritdoc/>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <inheritdoc/>
        public override Guid ComponentGuid => new Guid("4D28D2DB-4AFB-40F2-8D6E-5CBF678F7498");
    }
}
