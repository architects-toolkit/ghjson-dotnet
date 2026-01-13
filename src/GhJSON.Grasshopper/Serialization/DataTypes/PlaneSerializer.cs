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
    /// Serializer for Plane values.
    /// Format: planeOXY:ox,oy,oz;xx,xy,xz;yx,yy,yz
    /// </summary>
    internal sealed class PlaneSerializer : IDataTypeSerializer<Plane>
    {
        /// <inheritdoc/>
        public string TypeName => "Plane";

        /// <inheritdoc/>
        public Type TargetType => typeof(Plane);

        /// <inheritdoc/>
        public string Prefix => "planeOXY";

        /// <inheritdoc/>
        public string Serialize(Plane value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1},{2},{3};{4},{5},{6};{7},{8},{9}",
                this.Prefix,
                value.Origin.X, value.Origin.Y, value.Origin.Z,
                value.XAxis.X, value.XAxis.Y, value.XAxis.Z,
                value.YAxis.X, value.YAxis.Y, value.YAxis.Z);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Plane)value);
        }

        /// <inheritdoc/>
        public Plane Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Plane format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(';');
            var origin = parts[0].Split(',');
            var xAxis = parts[1].Split(',');
            var yAxis = parts[2].Split(',');

            return new Plane(
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
            if (parts.Length != 3)
            {
                return false;
            }

            foreach (var part in parts)
            {
                var coords = part.Split(',');
                if (coords.Length != 3)
                {
                    return false;
                }

                foreach (var coord in coords)
                {
                    if (!double.TryParse(coord, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
