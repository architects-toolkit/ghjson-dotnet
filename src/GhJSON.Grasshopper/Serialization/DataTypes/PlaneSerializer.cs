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
    /// Serializer for Rhino.Geometry.Plane type.
    /// Format: "planeOXY:ox,oy,oz;xx,xy,xz;yx,yy,yz" (origin + X/Y axes).
    /// </summary>
    public class PlaneSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Plane";

        /// <inheritdoc/>
        public Type TargetType => typeof(Plane);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is Plane plane)
            {
                return $"planeOXY:{plane.Origin.X.ToString(CultureInfo.InvariantCulture)},{plane.Origin.Y.ToString(CultureInfo.InvariantCulture)},{plane.Origin.Z.ToString(CultureInfo.InvariantCulture)};" +
                       $"{plane.XAxis.X.ToString(CultureInfo.InvariantCulture)},{plane.XAxis.Y.ToString(CultureInfo.InvariantCulture)},{plane.XAxis.Z.ToString(CultureInfo.InvariantCulture)};" +
                       $"{plane.YAxis.X.ToString(CultureInfo.InvariantCulture)},{plane.YAxis.Y.ToString(CultureInfo.InvariantCulture)},{plane.YAxis.Z.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Plane, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Plane format: '{value}'. Expected format: 'planeOXY:ox,oy,oz;xx,xy,xz;yx,yy,yz' with valid doubles.");
            }

            var valueWithoutPrefix = value.Substring(value.IndexOf(':') + 1);
            var vectors = valueWithoutPrefix.Split(';');
            var originParts = vectors[0].Split(',');
            var xAxisParts = vectors[1].Split(',');
            var yAxisParts = vectors[2].Split(',');

            double ox = double.Parse(originParts[0], CultureInfo.InvariantCulture);
            double oy = double.Parse(originParts[1], CultureInfo.InvariantCulture);
            double oz = double.Parse(originParts[2], CultureInfo.InvariantCulture);
            double xx = double.Parse(xAxisParts[0], CultureInfo.InvariantCulture);
            double xy = double.Parse(xAxisParts[1], CultureInfo.InvariantCulture);
            double xz = double.Parse(xAxisParts[2], CultureInfo.InvariantCulture);
            double yx = double.Parse(yAxisParts[0], CultureInfo.InvariantCulture);
            double yy = double.Parse(yAxisParts[1], CultureInfo.InvariantCulture);
            double yz = double.Parse(yAxisParts[2], CultureInfo.InvariantCulture);

            var origin = new Point3d(ox, oy, oz);
            var xAxis = new Vector3d(xx, xy, xz);
            var yAxis = new Vector3d(yx, yy, yz);

            return new Plane(origin, xAxis, yAxis);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("planeOXY:"))
            {
                return false;
            }

            var valueWithoutPrefix = value.Substring(9); // "planeOXY:".Length
            var vectors = valueWithoutPrefix.Split(';');
            if (vectors.Length != 3)
            {
                return false;
            }

            foreach (var vector in vectors)
            {
                var parts = vector.Split(',');
                if (parts.Length != 3)
                {
                    return false;
                }

                foreach (var part in parts)
                {
                    if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
