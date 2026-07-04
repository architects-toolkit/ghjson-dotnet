/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents the integer position of a component on the Grasshopper canvas.
    /// Supports both compact string format "X,Y" and object format with x/y properties.
    /// </summary>
    public sealed class GhJsonPivot
    {
        /// <summary>
        /// Gets or sets the X coordinate on the canvas.
        /// </summary>
        [JsonProperty("x")]
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate on the canvas.
        /// </summary>
        [JsonProperty("y")]
        public int Y { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GhJsonPivot"/> class.
        /// </summary>
        public GhJsonPivot()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GhJsonPivot"/> class with coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public GhJsonPivot(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Creates a pivot from a compact string format "X,Y".
        /// </summary>
        /// <param name="compact">The compact string representation.</param>
        /// <returns>A new pivot instance, or null if parsing fails.</returns>
        public static GhJsonPivot? FromCompact(string compact)
        {
            if (string.IsNullOrWhiteSpace(compact))
            {
                return null;
            }

            var parts = compact.Split(',');
            if (parts.Length != 2)
            {
                return null;
            }

            if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
            {
                return new GhJsonPivot(x, y);
            }

            return null;
        }

        /// <summary>
        /// Converts this pivot to compact string format "X,Y".
        /// Uses an integer format so the output always conforms to the GhJSON schema
        /// pattern for compact pivots.
        /// </summary>
        /// <returns>The compact string representation.</returns>
        public string ToCompact()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}",
                this.X.ToString(CultureInfo.InvariantCulture),
                this.Y.ToString(CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.ToCompact();
        }

        /// <summary>
        /// Converts this pivot to a PointF.
        /// </summary>
        /// <returns>A new PointF instance.</returns>
        public PointF ToPointF()
        {
            return new PointF(this.X, this.Y);
        }

        /// <summary>
        /// Creates a pivot from a PointF, rounding fractional coordinates to the nearest integer.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>A new pivot instance.</returns>
        public static GhJsonPivot FromPointF(PointF point)
        {
            return new GhJsonPivot(Round(point.X), Round(point.Y));
        }

        private static int Round(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
