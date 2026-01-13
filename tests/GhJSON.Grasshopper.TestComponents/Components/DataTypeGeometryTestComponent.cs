using System;
using System.Collections.Generic;
using GhJSON.Grasshopper.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GhJSON.Grasshopper.TestComponents.Components
{
    public class DataTypeGeometryTestComponent : GH_Component
    {
        public DataTypeGeometryTestComponent()
            : base(
                "GhJSON Geometry Serializers Test",
                "GhJsonGeoTest",
                "Runs RhinoCommon-dependent serialization/deserialization checks for GhJSON data type serializers.",
                "GhJSON",
                "Tests")
        {
        }

        public override Guid ComponentGuid => new Guid("0F4C90E4-96FD-4E40-9B9A-6B0AD7A3B6D1");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Run the test", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Success", "S", "Whether all tests passed", GH_ParamAccess.item);
            pManager.AddTextParameter("Results", "R", "Test results", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(0, ref run) || !run)
            {
                DA.SetData(0, false);
                DA.SetDataList(1, new List<string> { "Not running - set Run to true" });
                return;
            }

            DataTypeRegistry.EnsureInitialized();

            var results = new List<string>();
            bool allPassed = true;

            allPassed &= this.TryTest("Plane", results, () =>
            {
                var serialized = "planeOXY:0,0,0;1,0,0;0,1,0";
                return DataTypeRegistry.Deserialize(serialized) is Plane;
            });

            allPassed &= this.TryTest("Circle", results, () =>
            {
                var serialized = "circleCNRS:0,0,0;0,0,1;5.0;5,0,0";
                return DataTypeRegistry.Deserialize(serialized) is Circle;
            });

            allPassed &= this.TryTest("Arc", results, () =>
            {
                var serialized = "arc3P:0,0,0;5,5,0;10,0,0";
                return DataTypeRegistry.Deserialize(serialized) is Arc;
            });

            allPassed &= this.TryTest("Box", results, () =>
            {
                var serialized = "boxOXY:0,0,0;1,0,0;0,1,0;-5,5;-5,5;0,10";
                return DataTypeRegistry.Deserialize(serialized) is Box;
            });

            allPassed &= this.TryTest("Rectangle", results, () =>
            {
                var serialized = "rectangleCXY:0,0,0;1,0,0;0,1,0;10,5";
                return DataTypeRegistry.Deserialize(serialized) is Rectangle3d;
            });

            results.Add(string.Empty);
            results.Add(allPassed ? "=== ALL TESTS PASSED ===" : "=== SOME TESTS FAILED ===");

            DA.SetData(0, allPassed);
            DA.SetDataList(1, results);
        }

        private bool TryTest(string name, List<string> results, Func<bool> test)
        {
            try
            {
                if (test())
                {
                    results.Add($"PASS: {name}");
                    return true;
                }

                results.Add($"FAIL: {name} - unexpected type/result");
                return false;
            }
            catch (Exception ex)
            {
                results.Add($"FAIL: {name} - {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }
    }
}
