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
    /// Serializer for System.String type.
    /// Format: "text:value" (simple string value).
    /// </summary>
    public class TextSerializer : IDataTypeSerializer
    {
        /// <inheritdoc/>
        public string TypeName => "Text";

        /// <inheritdoc/>
        public Type TargetType => typeof(string);

        /// <inheritdoc/>
        public string Serialize(object value)
        {
            if (value is string text)
            {
                return $"text:{text}";
            }

            throw new ArgumentException($"Value must be of type String, got {value?.GetType().Name ?? "null"}");
        }

        /// <inheritdoc/>
        public object Deserialize(string value)
        {
            if (!Validate(value))
            {
                throw new FormatException($"Invalid Text format: '{value}'. Expected format: 'text:value'.");
            }

            // Return everything after "text:"
            return value.Substring(5); // "text:".Length
        }

        /// <inheritdoc/>
        public bool Validate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.StartsWith("text:");
        }
    }
}
