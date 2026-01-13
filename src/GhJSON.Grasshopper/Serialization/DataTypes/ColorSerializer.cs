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
using System.Drawing;

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Serializer for Color values.
    /// Format: argb:A,R,G,B
    /// </summary>
    internal sealed class ColorSerializer : IDataTypeSerializer<Color>
    {
        /// <inheritdoc/>
        public string TypeName => "Color";

        /// <inheritdoc/>
        public Type TargetType => typeof(Color);

        /// <inheritdoc/>
        public string Prefix => "argb";

        /// <inheritdoc/>
        public string Serialize(Color value)
        {
            return $"{this.Prefix}:{value.A},{value.R},{value.G},{value.B}";
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((Color)value);
        }

        /// <inheritdoc/>
        public Color Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid color format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split(',');
            return Color.FromArgb(
                int.Parse(parts[0]),
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                int.Parse(parts[3]));
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
            var parts = data.Split(',');
            if (parts.Length != 4)
            {
                return false;
            }

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var num) || num < 0 || num > 255)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
