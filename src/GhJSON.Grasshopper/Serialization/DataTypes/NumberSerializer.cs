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

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Serializer for double/number values.
    /// Format: number:value
    /// </summary>
    internal sealed class NumberSerializer : IDataTypeSerializer<double>
    {
        /// <inheritdoc/>
        public string TypeName => "Number";

        /// <inheritdoc/>
        public Type TargetType => typeof(double);

        /// <inheritdoc/>
        public string Prefix => "number";

        /// <inheritdoc/>
        public string Serialize(double value)
        {
            return $"{this.Prefix}:{value.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public double Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid number format: {value}");
            }

            var numStr = value.Substring(this.Prefix.Length + 1);
            return double.Parse(numStr, CultureInfo.InvariantCulture);
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

            var numStr = value.Substring(this.Prefix.Length + 1);
            return double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
