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
    /// Serializer for Rhino.Geometry.Vector3d type.
    /// Format: "vectorXYZ:x,y,z" (e.g., "vectorXYZ:1.0,0.0,0.0").
    /// </summary>
    public class VectorSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Vector";

        /// <inheritdoc/>
        public Type TargetType => typeof(Vector3d);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is Vector3d vector)
            {
                return $"vectorXYZ:{vector.X.ToString(CultureInfo.InvariantCulture)},{vector.Y.ToString(CultureInfo.InvariantCulture)},{vector.Z.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Vector3d, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Vector format: '{value}'. Expected format: 'vectorXYZ:x,y,z' with valid doubles.");
            }

            var valueWithoutPrefix = value.Substring(value.IndexOf(':') + 1);
            var parts = valueWithoutPrefix.Split(',');
            double x = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double y = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double z = double.Parse(parts[2], CultureInfo.InvariantCulture);

            return new Vector3d(x, y, z);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("vectorXYZ:"))
            {
                return false;
            }

            var valueWithoutPrefix = value.Substring(10); // "vectorXYZ:".Length
            var parts = valueWithoutPrefix.Split(',');
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

            return true;
        }
    }
}
