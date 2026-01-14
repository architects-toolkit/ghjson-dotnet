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
using Rhino.Geometry;

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Serializer for Circle values.
    /// Format: circleCNRS:cx,cy,cz;nx,ny,nz;r;sx,sy,sz
    /// </summary>
    internal sealed class CircleSerializer : IDataTypeSerializer<Circle>
    {
        /// <inheritdoc/>
        public string TypeName => "Circle";

        /// <inheritdoc/>
        public Type TargetType => typeof(Circle);

        /// <inheritdoc/>
        public string Prefix => "circleCNRS";

        /// <inheritdoc/>
        public string Serialize(Circle value)
        {
            var startPoint = value.PointAt(0);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1},{2},{3};{4},{5},{6};{7};{8},{9},{10}",
                this.Prefix,
                value.Center.X, value.Center.Y, value.Center.Z,
                value.Normal.X, value.Normal.Y, value.Normal.Z,
                value.Radius,
                startPoint.X, startPoint.Y, startPoint.Z);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Circle)value);
        }

        /// <inheritdoc/>
        public Circle Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Circle format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(';');
            var center = parts[0].Split(',');
            var normal = parts[1].Split(',');
            var radius = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var start = parts[3].Split(',');

            var centerPt = new Point3d(
                double.Parse(center[0], CultureInfo.InvariantCulture),
                double.Parse(center[1], CultureInfo.InvariantCulture),
                double.Parse(center[2], CultureInfo.InvariantCulture));

            var normalVec = new Vector3d(
                double.Parse(normal[0], CultureInfo.InvariantCulture),
                double.Parse(normal[1], CultureInfo.InvariantCulture),
                double.Parse(normal[2], CultureInfo.InvariantCulture));

            var startPt = new Point3d(
                double.Parse(start[0], CultureInfo.InvariantCulture),
                double.Parse(start[1], CultureInfo.InvariantCulture),
                double.Parse(start[2], CultureInfo.InvariantCulture));

            // Create plane from center, normal, and start point
            var xAxis = startPt - centerPt;
            xAxis.Unitize();
            var yAxis = Vector3d.CrossProduct(normalVec, xAxis);
            yAxis.Unitize();
            var plane = new Plane(centerPt, xAxis, yAxis);

            return new Circle(plane, radius);
        }

        /// <inheritdoc/>
        object IDataTypeSerializer.Deserialize(string value)
        {
            return this.Deserialize(value);
        }

        /// <inheritdoc/>
        public bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith($"{this.Prefix}:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(';');
            return parts.Length == 4;
        }
    }
}
