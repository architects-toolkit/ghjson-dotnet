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
    /// Serializer for Interval values.
    /// Format: interval:min&lt;max
    /// </summary>
    internal sealed class IntervalSerializer : IDataTypeSerializer<Interval>
    {
        /// <inheritdoc/>
        public string TypeName => "Interval";

        /// <inheritdoc/>
        public Type TargetType => typeof(Interval);

        /// <inheritdoc/>
        public string Prefix => "interval";

        /// <inheritdoc/>
        public string Serialize(Interval value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}<{2}",
                this.Prefix,
                value.T0,
                value.T1);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Interval)value);
        }

        /// <inheritdoc/>
        public Interval Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Interval format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split('<');

            return new Interval(
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture));
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
            var parts = data.Split('<');
            if (parts.Length != 2)
            {
                return false;
            }

            return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                   double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
