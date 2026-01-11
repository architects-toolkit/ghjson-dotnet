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
using Newtonsoft.Json;

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// Compact position representation using string format "X,Y" instead of object format.
    /// Optimizes JSON size by ~70% compared to PointF object serialization.
    /// </summary>
    [JsonConverter(typeof(CompactPositionConverter))]
    public struct CompactPosition
    {
        /// <summary>
        /// Gets the X coordinate.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the Y coordinate.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets a value indicating whether this position represents an empty/unset position.
        /// </summary>
        public bool IsEmpty => Math.Abs(X) < float.Epsilon && Math.Abs(Y) < float.Epsilon;

        /// <summary>
        /// Represents an empty position (0,0).
        /// </summary>
        public static CompactPosition Empty => new CompactPosition(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="CompactPosition"/> struct.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public CompactPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Implicit conversion from PointF to CompactPosition.
        /// </summary>
        /// <param name="point">The PointF to convert.</param>
        /// <returns>A new CompactPosition.</returns>
        public static implicit operator CompactPosition(PointF point)
        {
            return new CompactPosition(point.X, point.Y);
        }

        /// <summary>
        /// Implicit conversion from CompactPosition to PointF.
        /// </summary>
        /// <param name="position">The CompactPosition to convert.</param>
        /// <returns>A new PointF.</returns>
        public static implicit operator PointF(CompactPosition position)
        {
            return new PointF(position.X, position.Y);
        }

        /// <summary>
        /// Parses a compact position string in format "X,Y".
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <returns>The parsed CompactPosition.</returns>
        public static CompactPosition Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Position string cannot be null or empty", nameof(value));

            var parts = value.Split(',');
            if (parts.Length != 2)
                throw new FormatException($"Invalid position format: '{value}'. Expected format: 'X,Y'");

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                throw new FormatException($"Invalid X coordinate: '{parts[0]}'");

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                throw new FormatException($"Invalid Y coordinate: '{parts[1]}'");

            return new CompactPosition(x, y);
        }

        /// <summary>
        /// Converts the position to a compact string representation.
        /// </summary>
        /// <returns>String in format "X,Y".</returns>
        public override string ToString()
        {
            return $"{X.ToString(CultureInfo.InvariantCulture)},{Y.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Determines whether two CompactPosition instances are equal.
        /// </summary>
        /// <param name="other">The other CompactPosition to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(CompactPosition other)
        {
            return Math.Abs(X - other.X) < float.Epsilon && Math.Abs(Y - other.Y) < float.Epsilon;
        }

        /// <summary>
        /// Determines whether this instance equals the specified object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public override bool Equals(object? obj)
        {
            return obj is CompactPosition other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + X.GetHashCode();
                hash = (hash * 31) + Y.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(CompactPosition left, CompactPosition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(CompactPosition left, CompactPosition right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// JSON converter for CompactPosition that serializes as "X,Y" string.
    /// </summary>
    public class CompactPositionConverter : JsonConverter<CompactPosition>
    {
        /// <summary>
        /// Writes the JSON representation of the CompactPosition.
        /// </summary>
        public override void WriteJson(JsonWriter writer, CompactPosition value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        /// <summary>
        /// Reads the JSON representation and converts it to CompactPosition.
        /// </summary>
        public override CompactPosition ReadJson(JsonReader reader, Type objectType, CompactPosition existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                if (reader.Value is string value && !string.IsNullOrEmpty(value))
                {
                    return CompactPosition.Parse(value);
                }
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                // Handle legacy PointF format for backward compatibility
                var obj = serializer.Deserialize<PointF>(reader);
                return new CompactPosition(obj.X, obj.Y);
            }

            return new CompactPosition(0, 0);
        }
    }
}
