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

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Serializer for boolean values.
    /// Format: boolean:true or boolean:false
    /// </summary>
    internal sealed class BooleanSerializer : IDataTypeSerializer<bool>
    {
        /// <inheritdoc/>
        public string TypeName => "Boolean";

        /// <inheritdoc/>
        public Type TargetType => typeof(bool);

        /// <inheritdoc/>
        public string Prefix => "boolean";

        /// <inheritdoc/>
        public string Serialize(bool value)
        {
            return $"{this.Prefix}:{(value ? "true" : "false")}";
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize(Convert.ToBoolean(value));
        }

        /// <inheritdoc/>
        public bool Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid boolean format: {value}");
            }

            var boolStr = value.Substring(this.Prefix.Length + 1);
            return boolStr.Equals("true", StringComparison.OrdinalIgnoreCase);
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

            var boolStr = value.Substring(this.Prefix.Length + 1);
            return boolStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   boolStr.Equals("false", StringComparison.OrdinalIgnoreCase);
        }
    }
}
