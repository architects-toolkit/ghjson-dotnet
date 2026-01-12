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

namespace GhJSON.Core.Serialization.DataTypes.Serializers
{
    /// <summary>
    /// Serializer for System.Boolean type.
    /// Format: "boolean:true" or "boolean:false" (lowercase boolean values).
    /// </summary>
    public class BooleanSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Boolean";

        /// <inheritdoc/>
        public Type TargetType => typeof(bool);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is bool boolean)
            {
                return $"boolean:{boolean.ToString().ToLowerInvariant()}";
            }

            throw new ArgumentException($"Value must be of type Boolean, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Boolean format: '{value}'. Expected format: 'boolean:true' or 'boolean:false'.");
            }

            var booleanStr = value.Substring(8); // "boolean:".Length
            return bool.Parse(booleanStr);
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("boolean:"))
            {
                return false;
            }

            var booleanStr = value.Substring(8); // "boolean:".Length
            return booleanStr == "true" || booleanStr == "false";
        }
    }
}
