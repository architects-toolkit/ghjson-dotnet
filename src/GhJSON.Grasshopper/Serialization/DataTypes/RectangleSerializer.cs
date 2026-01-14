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
    /// Serializer for Rectangle3d values.
    /// Format: rectangleCXY:cx,cy,cz;xx,xy,xz;yx,yy,yz;w,h
    /// </summary>
    internal sealed class RectangleSerializer : IDataTypeSerializer<Rectangle3d>
    {
        /// <inheritdoc/>
        public string TypeName => "Rectangle";

        /// <inheritdoc/>
        public Type TargetType => typeof(Rectangle3d);

        /// <inheritdoc/>
        public string Prefix => "rectangleCXY";

        /// <inheritdoc/>
        public string Serialize(Rectangle3d value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1},{2},{3};{4},{5},{6};{7},{8},{9};{10},{11}",
                this.Prefix,
                value.Center.X, value.Center.Y, value.Center.Z,
                value.Plane.XAxis.X, value.Plane.XAxis.Y, value.Plane.XAxis.Z,
                value.Plane.YAxis.X, value.Plane.YAxis.Y, value.Plane.YAxis.Z,
                value.Width, value.Height);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Rectangle3d)value);
        }

        /// <inheritdoc/>
        public Rectangle3d Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Rectangle format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(';');
            var center = parts[0].Split(',');
            var xAxis = parts[1].Split(',');
            var yAxis = parts[2].Split(',');
            var dims = parts[3].Split(',');

            var centerPt = new Point3d(
                double.Parse(center[0], CultureInfo.InvariantCulture),
                double.Parse(center[1], CultureInfo.InvariantCulture),
                double.Parse(center[2], CultureInfo.InvariantCulture));

            var xVec = new Vector3d(
                double.Parse(xAxis[0], CultureInfo.InvariantCulture),
                double.Parse(xAxis[1], CultureInfo.InvariantCulture),
                double.Parse(xAxis[2], CultureInfo.InvariantCulture));

            // Recalculate Y-axis from normal × X-axis for orthogonality
            var normal = Vector3d.CrossProduct(xVec, new Vector3d(
                double.Parse(yAxis[0], CultureInfo.InvariantCulture),
                double.Parse(yAxis[1], CultureInfo.InvariantCulture),
                double.Parse(yAxis[2], CultureInfo.InvariantCulture)));
            var yVec = Vector3d.CrossProduct(normal, xVec);
            yVec.Unitize();

            var plane = new Plane(centerPt, xVec, yVec);
            var width = double.Parse(dims[0], CultureInfo.InvariantCulture);
            var height = double.Parse(dims[1], CultureInfo.InvariantCulture);

            return new Rectangle3d(
                plane,
                new Interval(-width / 2, width / 2),
                new Interval(-height / 2, height / 2));
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
