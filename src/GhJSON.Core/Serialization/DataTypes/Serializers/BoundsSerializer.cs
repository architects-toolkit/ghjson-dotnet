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

namespace GhJSON.Core.Serialization.DataTypes.Serializers
{
    /// <summary>
    /// Serializer for simple bounds (width, height) pairs.
    /// Format: "bounds:W,H" (e.g., "bounds:120.5,80.25").
    /// </summary>
    public class BoundsSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Bounds";

        /// <inheritdoc/>
        public Type TargetType => typeof((double width, double height));

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is ValueTuple<double, double> tuple)
            {
                return $"bounds:{tuple.Item1.ToString(CultureInfo.InvariantCulture)},{tuple.Item2.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be a (double width, double height) tuple, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Bounds format: '{value}'. Expected format: 'bounds:width,height' with valid doubles.");
            }

            var valueWithoutPrefix = value.Substring(value.IndexOf(':') + 1);
            var parts = valueWithoutPrefix.Split(',');
            double width = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double height = double.Parse(parts[1], CultureInfo.InvariantCulture);

            return (width, height);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("bounds:"))
            {
                return false;
            }

            var valueWithoutPrefix = value.Substring("bounds:".Length);
            var parts = valueWithoutPrefix.Split(',');
            if (parts.Length != 2)
            {
                return false;
            }

            return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                   double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
