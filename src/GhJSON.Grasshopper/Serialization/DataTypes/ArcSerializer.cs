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
    /// Serializer for Arc values.
    /// Format: arc3P:x1,y1,z1;x2,y2,z2;x3,y3,z3 (start, mid, end points)
    /// </summary>
    internal sealed class ArcSerializer : IDataTypeSerializer<Arc>
    {
        /// <inheritdoc/>
        public string TypeName => "Arc";

        /// <inheritdoc/>
        public Type TargetType => typeof(Arc);

        /// <inheritdoc/>
        public string Prefix => "arc3P";

        /// <inheritdoc/>
        public string Serialize(Arc value)
        {
            var start = value.StartPoint;
            var mid = value.MidPoint;
            var end = value.EndPoint;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1},{2},{3};{4},{5},{6};{7},{8},{9}",
                this.Prefix,
                start.X, start.Y, start.Z,
                mid.X, mid.Y, mid.Z,
                end.X, end.Y, end.Z);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Arc)value);
        }

        /// <inheritdoc/>
        public Arc Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Arc format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(';');
            var start = parts[0].Split(',');
            var mid = parts[1].Split(',');
            var end = parts[2].Split(',');

            var startPt = new Point3d(
                double.Parse(start[0], CultureInfo.InvariantCulture),
                double.Parse(start[1], CultureInfo.InvariantCulture),
                double.Parse(start[2], CultureInfo.InvariantCulture));

            var midPt = new Point3d(
                double.Parse(mid[0], CultureInfo.InvariantCulture),
                double.Parse(mid[1], CultureInfo.InvariantCulture),
                double.Parse(mid[2], CultureInfo.InvariantCulture));

            var endPt = new Point3d(
                double.Parse(end[0], CultureInfo.InvariantCulture),
                double.Parse(end[1], CultureInfo.InvariantCulture),
                double.Parse(end[2], CultureInfo.InvariantCulture));

            return new Arc(startPt, midPt, endPt);
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
