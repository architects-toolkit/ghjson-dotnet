using System;
using System.Collections.Generic;
using GhJSON.Grasshopper.Deserialization;
using GhJSON.Grasshopper.PutOperations;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.TestComponents.Components
{
    public class PutAndDeserializeSmokeTestComponent : GH_Component
    {
        public PutAndDeserializeSmokeTestComponent()
            : base(
                "GhJSON Put/Deserialize Smoke Test",
                "GhJsonPutSmoke",
                "Runs Grasshopper-dependent smoke tests for PutResult and DeserializationResult models.",
                "GhJSON",
                "Tests")
        {
        }

        public override Guid ComponentGuid => new Guid("B2B0A4A5-6C99-4C0E-B7A1-6E8E6E3C0A6E");

        public override GH_Exposure Exposure => GH_Exposure.secondary;

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

            var results = new List<string>();
            bool allPassed = true;

            allPassed &= this.TryTest("PutResult defaults", results, () =>
            {
                var r = new PutResult();
                return r.PlacedObjects != null && r.IdToGuidMapping != null && r.FailedComponents != null && r.Warnings != null;
            });

            allPassed &= this.TryTest("DeserializationResult defaults", results, () =>
            {
                var r = new DeserializationResult();
                return r.Objects != null && r.IdToObjectMapping != null && r.FailedComponents != null && r.Warnings != null;
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

                results.Add($"FAIL: {name} - unexpected result");
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
