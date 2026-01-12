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
using GhJSON.Grasshopper.Serialization.DataTypes;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GhJSON.Grasshopper.Tests.Components
{
    /// <summary>
    /// Test component for geometric data type serialization.
    /// Tests serialization and deserialization of geometric types like Point3d, Vector3d, Line, Plane, etc.
    /// </summary>
    public class GeometricSerializerTestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometricSerializerTestComponent"/> class.
        /// </summary>
        public GeometricSerializerTestComponent()
            : base("GhJSON Geometry Test", "GhJsonGeoTest",
                "Tests GhJSON geometric data type serialization and deserialization",
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

            // Initialize geometric serializers
            GeometricSerializerRegistry.Initialize();

            // Test Point3d serialization
            try
            {
                var point = new Point3d(1.5, 2.5, 3.5);
                var serializer = new PointSerializer();
                var serialized = serializer.Serialize(point);
                var deserialized = (Point3d)serializer.Deserialize(serialized);

                if (Math.Abs(point.X - deserialized.X) < 0.001 &&
                    Math.Abs(point.Y - deserialized.Y) < 0.001 &&
                    Math.Abs(point.Z - deserialized.Z) < 0.001)
                {
                    results.Add("✓ Point3d serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Point3d serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Point3d serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Vector3d serialization
            try
            {
                var vector = new Vector3d(1.0, 0.0, 0.0);
                var serializer = new VectorSerializer();
                var serialized = serializer.Serialize(vector);
                var deserialized = (Vector3d)serializer.Deserialize(serialized);

                if (Math.Abs(vector.X - deserialized.X) < 0.001 &&
                    Math.Abs(vector.Y - deserialized.Y) < 0.001 &&
                    Math.Abs(vector.Z - deserialized.Z) < 0.001)
                {
                    results.Add("✓ Vector3d serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Vector3d serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Vector3d serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Line serialization
            try
            {
                var line = new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 10));
                var serializer = new LineSerializer();
                var serialized = serializer.Serialize(line);
                var deserialized = (Line)serializer.Deserialize(serialized);

                if (Math.Abs(line.From.X - deserialized.From.X) < 0.001 &&
                    Math.Abs(line.To.X - deserialized.To.X) < 0.001)
                {
                    results.Add("✓ Line serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Line serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Line serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Plane serialization
            try
            {
                var plane = new Plane(new Point3d(5, 5, 5), new Vector3d(0, 0, 1));
                var serializer = new PlaneSerializer();
                var serialized = serializer.Serialize(plane);
                var deserialized = (Plane)serializer.Deserialize(serialized);

                if (Math.Abs(plane.Origin.X - deserialized.Origin.X) < 0.001 &&
                    Math.Abs(plane.Normal.Z - deserialized.Normal.Z) < 0.001)
                {
                    results.Add("✓ Plane serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Plane serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Plane serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Circle serialization
            try
            {
                var circle = new Circle(Plane.WorldXY, 5.0);
                var serializer = new CircleSerializer();
                var serialized = serializer.Serialize(circle);
                var deserialized = (Circle)serializer.Deserialize(serialized);

                if (Math.Abs(circle.Radius - deserialized.Radius) < 0.001)
                {
                    results.Add("✓ Circle serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Circle serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Circle serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Interval serialization
            try
            {
                var interval = new Interval(3.0, 6.0);
                var serializer = new IntervalSerializer();
                var serialized = serializer.Serialize(interval);
                var deserialized = (Interval)serializer.Deserialize(serialized);

                if (Math.Abs(interval.T0 - deserialized.T0) < 0.001 &&
                    Math.Abs(interval.T1 - deserialized.T1) < 0.001)
                {
                    results.Add("✓ Interval serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Interval serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Interval serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Box serialization
            try
            {
                var box = new Box(Plane.WorldXY, new Interval(-5, 5), new Interval(-5, 5), new Interval(0, 10));
                var serializer = new BoxSerializer();
                var serialized = serializer.Serialize(box);
                var deserialized = (Box)serializer.Deserialize(serialized);

                if (Math.Abs(box.X.Length - deserialized.X.Length) < 0.001 &&
                    Math.Abs(box.Y.Length - deserialized.Y.Length) < 0.001 &&
                    Math.Abs(box.Z.Length - deserialized.Z.Length) < 0.001)
                {
                    results.Add("✓ Box serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Box serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Box serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Arc serialization
            try
            {
                var arc = new Arc(new Point3d(0, 0, 0), new Point3d(5, 5, 0), new Point3d(10, 0, 0));
                var serializer = new ArcSerializer();
                var serialized = serializer.Serialize(arc);
                var deserialized = (Arc)serializer.Deserialize(serialized);

                if (Math.Abs(arc.StartPoint.X - deserialized.StartPoint.X) < 0.001 &&
                    Math.Abs(arc.MidPoint.X - deserialized.MidPoint.X) < 0.001 &&
                    Math.Abs(arc.EndPoint.X - deserialized.EndPoint.X) < 0.001)
                {
                    results.Add("✓ Arc serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Arc serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Arc serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test Rectangle serialization
            try
            {
                var plane = Plane.WorldXY;
                var rectangle = new Rectangle3d(plane, new Interval(-2, 8), new Interval(-3, 7));
                var serializer = new RectangleSerializer();
                var serialized = serializer.Serialize(rectangle);
                var deserialized = (Rectangle3d)serializer.Deserialize(serialized);

                if (Math.Abs(rectangle.Center.X - deserialized.Center.X) < 0.001 &&
                    Math.Abs(rectangle.Center.Y - deserialized.Center.Y) < 0.001 &&
                    Math.Abs(rectangle.Width - deserialized.Width) < 0.001 &&
                    Math.Abs(rectangle.Height - deserialized.Height) < 0.001)
                {
                    results.Add("✓ Rectangle serialization: PASSED");
                }
                else
                {
                    results.Add("✗ Rectangle serialization: FAILED - values don't match");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Rectangle serialization: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Summary
            results.Add("");
            results.Add(allPassed ? "=== ALL TESTS PASSED ===" : "=== SOME TESTS FAILED ===");

            DA.SetData(0, allPassed);
            DA.SetDataList(1, results);
        }

        /// <inheritdoc/>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <inheritdoc/>
        public override Guid ComponentGuid => new Guid("B7C57824-95EF-4BCA-8284-7B11AC9ECC5F");
    }
}
