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
    /// Serializer for integer values.
    /// Format: integer:value
    /// </summary>
    internal sealed class IntegerSerializer : IDataTypeSerializer<int>
    {
        /// <inheritdoc/>
        public string TypeName => "Integer";

        /// <inheritdoc/>
        public Type TargetType => typeof(int);

        /// <inheritdoc/>
        public string Prefix => "integer";

        /// <inheritdoc/>
        public string Serialize(int value)
        {
            return $"{this.Prefix}:{value.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public int Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid integer format: {value}");
            }

            var numStr = value.Substring(this.Prefix.Length + 1);
            return int.Parse(numStr, CultureInfo.InvariantCulture);
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
            return int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }
    }
}
