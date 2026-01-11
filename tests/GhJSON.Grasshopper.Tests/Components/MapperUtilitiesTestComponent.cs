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
using GhJSON.Grasshopper.Serialization.Shared;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Tests.Components
{
    /// <summary>
    /// Test component for mapper utilities (AccessModeMapper and TypeHintMapper).
    /// </summary>
    public class MapperUtilitiesTestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapperUtilitiesTestComponent"/> class.
        /// </summary>
        public MapperUtilitiesTestComponent()
            : base("GhJSON Mapper Utilities Test", "GhJsonMapperTest",
                "Tests mapper utilities used by GhJSON serialization/deserialization",
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
                // AccessModeMapper
                bool accessOk = true;
                accessOk &= AccessModeMapper.ToString(GH_ParamAccess.item) == "item";
                accessOk &= AccessModeMapper.ToString(GH_ParamAccess.list) == "list";
                accessOk &= AccessModeMapper.ToString(GH_ParamAccess.tree) == "tree";

                accessOk &= AccessModeMapper.FromString("item") == GH_ParamAccess.item;
                accessOk &= AccessModeMapper.FromString("list") == GH_ParamAccess.list;
                accessOk &= AccessModeMapper.FromString("tree") == GH_ParamAccess.tree;
                accessOk &= AccessModeMapper.FromString("UNKNOWN") == GH_ParamAccess.item;
                accessOk &= AccessModeMapper.FromString(null) == GH_ParamAccess.item;

                accessOk &= AccessModeMapper.IsValid("item");
                accessOk &= AccessModeMapper.IsValid("LIST");
                accessOk &= !AccessModeMapper.IsValid("banana");

                if (accessOk)
                {
                    results.Add("✓ AccessModeMapper: PASSED");
                }
                else
                {
                    results.Add("✗ AccessModeMapper: FAILED");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ AccessModeMapper: FAILED - {ex.Message}");
                allPassed = false;
            }

            try
            {
                // TypeHintMapper
                bool typeHintOk = true;

                typeHintOk &= TypeHintMapper.FormatTypeHint("Point3d", GH_ParamAccess.item) == "Point3d";
                typeHintOk &= TypeHintMapper.FormatTypeHint("Curve", GH_ParamAccess.list) == "List<Curve>";
                typeHintOk &= TypeHintMapper.FormatTypeHint("Number", GH_ParamAccess.tree) == "DataTree<Number>";

                typeHintOk &= TypeHintMapper.ExtractBaseType("List<Curve>") == "Curve";
                typeHintOk &= TypeHintMapper.ExtractBaseType("DataTree<Point3d>") == "Point3d";
                typeHintOk &= TypeHintMapper.ExtractBaseType("Point3d") == "Point3d";

                typeHintOk &= TypeHintMapper.IsCollectionType("List<Curve>");
                typeHintOk &= TypeHintMapper.IsCollectionType("DataTree<Point3d>");
                typeHintOk &= !TypeHintMapper.IsCollectionType("Point3d");

                typeHintOk &= TypeHintMapper.InferAccessMode("DataTree<Point3d>") == GH_ParamAccess.tree;
                typeHintOk &= TypeHintMapper.InferAccessMode("List<Curve>") == GH_ParamAccess.list;
                typeHintOk &= TypeHintMapper.InferAccessMode("Point3d") == GH_ParamAccess.item;

                typeHintOk &= TypeHintMapper.Normalize("  List< Curve > ") == "List<Curve>";
                typeHintOk &= TypeHintMapper.Normalize(null) == null;

                if (typeHintOk)
                {
                    results.Add("✓ TypeHintMapper: PASSED");
                }
                else
                {
                    results.Add("✗ TypeHintMapper: FAILED");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ TypeHintMapper: FAILED - {ex.Message}");
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
        public override Guid ComponentGuid => new Guid("E24FE4A9-76F2-4D56-BD16-D2D5CA241A72");
    }
}
