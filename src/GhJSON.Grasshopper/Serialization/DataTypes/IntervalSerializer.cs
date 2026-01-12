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
    /// Serializer for Rhino.Geometry.Interval type.
    /// Format: "interval:min&lt;max" (e.g., "interval:0.0&lt;10.0").
    /// </summary>
    public class IntervalSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Interval";

        /// <inheritdoc/>
        public Type TargetType => typeof(Interval);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is Interval interval)
            {
                return $"interval:{interval.Min.ToString(CultureInfo.InvariantCulture)}<{interval.Max.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Interval, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Interval format: '{value}'. Expected format: 'interval:min<max' with valid doubles.");
            }

            var valueWithoutPrefix = value.Substring(value.IndexOf(':') + 1);
            var parts = valueWithoutPrefix.Split('<');
            double min = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double max = double.Parse(parts[1], CultureInfo.InvariantCulture);

            return new Interval(min, max);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("interval:"))
            {
                return false;
            }

            var valueWithoutPrefix = value.Substring(9); // "interval:".Length
            var parts = valueWithoutPrefix.Split('<');
            if (parts.Length != 2)
            {
                return false;
            }

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double min))
            {
                return false;
            }

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double max))
            {
                return false;
            }

            // Allow min == max for degenerate intervals
            return min <= max;
        }
    }
}
