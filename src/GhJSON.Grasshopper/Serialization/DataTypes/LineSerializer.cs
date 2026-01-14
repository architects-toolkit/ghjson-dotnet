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
    /// Serializer for Line values.
    /// Format: line2p:x1,y1,z1;x2,y2,z2
    /// </summary>
    internal sealed class LineSerializer : IDataTypeSerializer<Line>
    {
        /// <inheritdoc/>
        public string TypeName => "Line";

        /// <inheritdoc/>
        public Type TargetType => typeof(Line);

        /// <inheritdoc/>
        public string Prefix => "line2p";

        /// <inheritdoc/>
        public string Serialize(Line value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1},{2},{3};{4},{5},{6}",
                this.Prefix,
                value.From.X, value.From.Y, value.From.Z,
                value.To.X, value.To.Y, value.To.Z);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Line)value);
        }

        /// <inheritdoc/>
        public Line Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Line format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var points = data.Split(';');
            var from = points[0].Split(',');
            var to = points[1].Split(',');

            return new Line(
                new Point3d(
                    double.Parse(from[0], CultureInfo.InvariantCulture),
                    double.Parse(from[1], CultureInfo.InvariantCulture),
                    double.Parse(from[2], CultureInfo.InvariantCulture)),
                new Point3d(
                    double.Parse(to[0], CultureInfo.InvariantCulture),
                    double.Parse(to[1], CultureInfo.InvariantCulture),
                    double.Parse(to[2], CultureInfo.InvariantCulture)));
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
            var points = data.Split(';');
            if (points.Length != 2)
            {
                return false;
            }

            foreach (var point in points)
            {
                var coords = point.Split(',');
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
