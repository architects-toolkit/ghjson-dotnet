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
using System.Globalization;
using GhJSON.Core.Serialization.DataTypes;
using Rhino.Geometry;

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Serializer for Rhino.Geometry.Circle type.
    /// Format: "circleCNRS:cx,cy,cz;nx,ny,nz;r;sx,sy,sz" (center + normal + radius + start point).
    /// </summary>
    public class CircleSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Circle";

        /// <inheritdoc/>
        public Type TargetType => typeof(Circle);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is Circle circle)
            {
                // Calculate start point (point on circle at angle 0 in the circle's coordinate system)
                var startPoint = circle.Center + circle.Plane.XAxis * circle.Radius;

                return $"circleCNRS:{circle.Center.X.ToString(CultureInfo.InvariantCulture)},{circle.Center.Y.ToString(CultureInfo.InvariantCulture)},{circle.Center.Z.ToString(CultureInfo.InvariantCulture)};" +
                       $"{circle.Normal.X.ToString(CultureInfo.InvariantCulture)},{circle.Normal.Y.ToString(CultureInfo.InvariantCulture)},{circle.Normal.Z.ToString(CultureInfo.InvariantCulture)};" +
                       $"{circle.Radius.ToString(CultureInfo.InvariantCulture)};" +
                       $"{startPoint.X.ToString(CultureInfo.InvariantCulture)},{startPoint.Y.ToString(CultureInfo.InvariantCulture)},{startPoint.Z.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Circle, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Circle format: '{value}'. Expected format: 'circleCNRS:cx,cy,cz;nx,ny,nz;r;sx,sy,sz' with valid doubles and r > 0.");
            }

            var valueWithoutPrefix = value.Substring(value.IndexOf(':') + 1);
            var parts = valueWithoutPrefix.Split(';');
            var centerParts = parts[0].Split(',');
            var normalParts = parts[1].Split(',');
            var startParts = parts[3].Split(',');
            double r = double.Parse(parts[2], CultureInfo.InvariantCulture);

            // Parse center
            var center = new Point3d(
                double.Parse(centerParts[0], CultureInfo.InvariantCulture),
                double.Parse(centerParts[1], CultureInfo.InvariantCulture),
                double.Parse(centerParts[2], CultureInfo.InvariantCulture)
            );

            // Parse normal
            var normal = new Vector3d(
                double.Parse(normalParts[0], CultureInfo.InvariantCulture),
                double.Parse(normalParts[1], CultureInfo.InvariantCulture),
                double.Parse(normalParts[2], CultureInfo.InvariantCulture)
            );

            // Parse start point
            var startPoint = new Point3d(
                double.Parse(startParts[0], CultureInfo.InvariantCulture),
                double.Parse(startParts[1], CultureInfo.InvariantCulture),
                double.Parse(startParts[2], CultureInfo.InvariantCulture)
            );

            // Calculate X-axis from center to start point
            var xAxis = startPoint - center;
            xAxis.Unitize();

            // Normalize the normal vector
            normal.Unitize();

            // Calculate Y-axis as cross product of normal and X-axis
            var yAxis = Vector3d.CrossProduct(normal, xAxis);
            yAxis.Unitize();

            // Create plane with proper orientation (origin, xAxis, yAxis)
            var plane = new Plane(center, xAxis, yAxis);

            return new Circle(plane, r);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("circleCNRS:"))
            {
                return false;
            }

            var valueWithoutPrefix = value.Substring(11); // "circleCNRS:".Length
            var parts = valueWithoutPrefix.Split(';');
            if (parts.Length != 4)
            {
                return false;
            }

            // Validate center (3 components)
            var centerParts = parts[0].Split(',');
            if (centerParts.Length != 3)
            {
                return false;
            }

            foreach (var part in centerParts)
            {
                if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    return false;
                }
            }

            // Validate normal (3 components)
            var normalParts = parts[1].Split(',');
            if (normalParts.Length != 3)
            {
                return false;
            }

            foreach (var part in normalParts)
            {
                if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    return false;
                }
            }

            // Validate radius (positive)
            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double radius))
            {
                return false;
            }

            if (radius <= 0)
            {
                return false;
            }

            // Validate start point (3 components)
            var startParts = parts[3].Split(',');
            if (startParts.Length != 3)
            {
                return false;
            }

            foreach (var part in startParts)
            {
                if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
