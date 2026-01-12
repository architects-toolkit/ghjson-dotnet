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
    /// Serializer for System.Double type (floating-point numbers).
    /// Format: "number:value" (double value using invariant culture).
    /// </summary>
    public class NumberSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Number";

        /// <inheritdoc/>
        public Type TargetType => typeof(double);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is double number)
            {
                return $"number:{number.ToString(CultureInfo.InvariantCulture)}";
            }

            // Handle other numeric types
            if (value is float f)
            {
                return $"number:{f.ToString(CultureInfo.InvariantCulture)}";
            }

            if (value is decimal d)
            {
                return $"number:{d.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Double, Float, or Decimal, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Number format: '{value}'. Expected format: 'number:value' with valid double.");
            }

            var numberStr = value.Substring(7); // "number:".Length
            return double.Parse(numberStr, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("number:"))
            {
                return false;
            }

            var numberStr = value.Substring(7); // "number:".Length
            return double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
