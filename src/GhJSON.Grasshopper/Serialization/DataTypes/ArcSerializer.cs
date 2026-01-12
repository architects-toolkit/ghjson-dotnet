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
    /// Serializer for Rhino.Geometry.Arc type.
    /// Format: "arc3P:x1,y1,z1;x2,y2,z2;x3,y3,z3" (three points defining the arc).
    /// </summary>
    public class ArcSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Arc";

        /// <inheritdoc/>
        public Type TargetType => typeof(Arc);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is Arc arc)
            {
                // Get three points that define the arc: start, mid, and end
                var startPoint = arc.StartPoint;
                var midPoint = arc.MidPoint;
                var endPoint = arc.EndPoint;

                return $"arc3P:{startPoint.X.ToString(CultureInfo.InvariantCulture)},{startPoint.Y.ToString(CultureInfo.InvariantCulture)},{startPoint.Z.ToString(CultureInfo.InvariantCulture)};" +
                       $"{midPoint.X.ToString(CultureInfo.InvariantCulture)},{midPoint.Y.ToString(CultureInfo.InvariantCulture)},{midPoint.Z.ToString(CultureInfo.InvariantCulture)};" +
                       $"{endPoint.X.ToString(CultureInfo.InvariantCulture)},{endPoint.Y.ToString(CultureInfo.InvariantCulture)},{endPoint.Z.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Arc, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Arc format: '{value}'. Expected format: 'arc3P:x1,y1,z1;x2,y2,z2;x3,y3,z3' with valid doubles.");
            }

            var valueWithoutPrefix = value.Substring(value.IndexOf(':') + 1);
            var parts = valueWithoutPrefix.Split(';');

            // Parse three points
            var point1Parts = parts[0].Split(',');
            var point1 = new Point3d(
                double.Parse(point1Parts[0], CultureInfo.InvariantCulture),
                double.Parse(point1Parts[1], CultureInfo.InvariantCulture),
                double.Parse(point1Parts[2], CultureInfo.InvariantCulture)
            );

            var point2Parts = parts[1].Split(',');
            var point2 = new Point3d(
                double.Parse(point2Parts[0], CultureInfo.InvariantCulture),
                double.Parse(point2Parts[1], CultureInfo.InvariantCulture),
                double.Parse(point2Parts[2], CultureInfo.InvariantCulture)
            );

            var point3Parts = parts[2].Split(',');
            var point3 = new Point3d(
                double.Parse(point3Parts[0], CultureInfo.InvariantCulture),
                double.Parse(point3Parts[1], CultureInfo.InvariantCulture),
                double.Parse(point3Parts[2], CultureInfo.InvariantCulture)
            );

            // Create arc from three points
            return new Arc(point1, point2, point3);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("arc3P:"))
            {
                return false;
            }

            var valueWithoutPrefix = value.Substring(6); // "arc3P:".Length
            var parts = valueWithoutPrefix.Split(';');
            if (parts.Length != 3)
            {
                return false;
            }

            // Validate all three points (3 components each)
            for (int i = 0; i < 3; i++)
            {
                var pointParts = parts[i].Split(',');
                if (pointParts.Length != 3)
                {
                    return false;
                }

                foreach (var part in pointParts)
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
