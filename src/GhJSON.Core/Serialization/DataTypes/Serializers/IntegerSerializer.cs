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
    /// Serializer for System.Int32 type (integer numbers).
    /// Format: "integer:value" (int32 value).
    /// </summary>
    public class IntegerSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Integer";

        /// <inheritdoc/>
        public Type TargetType => typeof(int);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is int integer)
            {
                return $"integer:{integer.ToString(CultureInfo.InvariantCulture)}";
            }

            // Handle other integer types
            if (value is long l)
            {
                return $"integer:{l.ToString(CultureInfo.InvariantCulture)}";
            }

            if (value is short s)
            {
                return $"integer:{s.ToString(CultureInfo.InvariantCulture)}";
            }

            if (value is byte b)
            {
                return $"integer:{b.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new ArgumentException($"Value must be of type Int32, Int64, Int16, or Byte, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Integer format: '{value}'. Expected format: 'integer:value' with valid int32.");
            }

            var integerStr = value.Substring(8); // "integer:".Length
            return int.Parse(integerStr, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("integer:"))
            {
                return false;
            }

            var integerStr = value.Substring(8); // "integer:".Length
            return int.TryParse(integerStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }
    }
}
