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
    /// Serializer for Box values.
    /// Format: boxOXY:ox,oy,oz;xx,xy,xz;yx,yy,yz;x0,x1;y0,y1;z0,z1
    /// </summary>
    internal sealed class BoxSerializer : IDataTypeSerializer<Box>
    {
        /// <inheritdoc/>
        public string TypeName => "Box";

        /// <inheritdoc/>
        public Type TargetType => typeof(Box);

        /// <inheritdoc/>
        public string Prefix => "boxOXY";

        /// <inheritdoc/>
        public string Serialize(Box value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1},{2},{3};{4},{5},{6};{7},{8},{9};{10},{11};{12},{13};{14},{15}",
                this.Prefix,
                value.Plane.Origin.X, value.Plane.Origin.Y, value.Plane.Origin.Z,
                value.Plane.XAxis.X, value.Plane.XAxis.Y, value.Plane.XAxis.Z,
                value.Plane.YAxis.X, value.Plane.YAxis.Y, value.Plane.YAxis.Z,
                value.X.T0, value.X.T1,
                value.Y.T0, value.Y.T1,
                value.Z.T0, value.Z.T1);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Box)value);
        }

        /// <inheritdoc/>
        public Box Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Box format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(';');
            var origin = parts[0].Split(',');
            var xAxis = parts[1].Split(',');
            var yAxis = parts[2].Split(',');
            var xInterval = parts[3].Split(',');
            var yInterval = parts[4].Split(',');
            var zInterval = parts[5].Split(',');

            var plane = new Plane(
                new Point3d(
                    double.Parse(origin[0], CultureInfo.InvariantCulture),
                    double.Parse(origin[1], CultureInfo.InvariantCulture),
                    double.Parse(origin[2], CultureInfo.InvariantCulture)),
                new Vector3d(
                    double.Parse(xAxis[0], CultureInfo.InvariantCulture),
                    double.Parse(xAxis[1], CultureInfo.InvariantCulture),
                    double.Parse(xAxis[2], CultureInfo.InvariantCulture)),
                new Vector3d(
                    double.Parse(yAxis[0], CultureInfo.InvariantCulture),
                    double.Parse(yAxis[1], CultureInfo.InvariantCulture),
                    double.Parse(yAxis[2], CultureInfo.InvariantCulture)));

            return new Box(
                plane,
                new Interval(
                    double.Parse(xInterval[0], CultureInfo.InvariantCulture),
                    double.Parse(xInterval[1], CultureInfo.InvariantCulture)),
                new Interval(
                    double.Parse(yInterval[0], CultureInfo.InvariantCulture),
                    double.Parse(yInterval[1], CultureInfo.InvariantCulture)),
                new Interval(
                    double.Parse(zInterval[0], CultureInfo.InvariantCulture),
                    double.Parse(zInterval[1], CultureInfo.InvariantCulture)));
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
            return parts.Length == 6;
        }
    }
}
