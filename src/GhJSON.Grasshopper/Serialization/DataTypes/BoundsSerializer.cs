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
using System.Globalization;

namespace GhJSON.Grasshopper.Serialization.DataTypes
{
    /// <summary>
    /// Serializer for Size (bounds) values.
    /// Format: bounds:WxH
    /// </summary>
    internal sealed class BoundsSerializer : IDataTypeSerializer<SizeF>
    {
        /// <inheritdoc/>
        public string TypeName => "Bounds";

        /// <inheritdoc/>
        public Type TargetType => typeof(SizeF);

        /// <inheritdoc/>
        public string Prefix => "bounds";

        /// <inheritdoc/>
        public string Serialize(SizeF value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}x{2}",
                this.Prefix,
                value.Width,
                value.Height);
        }

        /// <inheritdoc/>
        string IDataTypeSerializer.Serialize(object value)
        {
            return this.Serialize((SizeF)value);
        }

        /// <inheritdoc/>
        public SizeF Deserialize(string value)
        {
            if (!this.IsValid(value))
            {
                throw new ArgumentException($"Invalid Bounds format: {value}");
            }

            var data = value.Substring(this.Prefix.Length + 1);
            var parts = data.Split('x');

            return new SizeF(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture));
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
            var parts = data.Split('x');
            if (parts.Length != 2)
            {
                return false;
            }

            return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
