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

namespace GhJSON.Core.DataEditing
{
    /// <summary>
    /// A data-tree address: either a whole branch (<c>Path</c> only) or one
    /// item inside it (<c>Path</c> plus <c>ItemIndex</c>). Indices are
    /// zero-based. Parse from text with <see cref="TryParse"/>.
    /// </summary>
    public readonly struct GhJsonDataPosition : IEquatable<GhJsonDataPosition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GhJsonDataPosition"/> struct.
        /// </summary>
        /// <param name="path">Branch indices, e.g. <c>[0, 0, 1]</c> for <c>{0;0;1}</c>.</param>
        /// <param name="itemIndex">Item index inside the branch, or null for a branch-level address.</param>
        public GhJsonDataPosition(int[] path, int? itemIndex)
        {
            this.Path = path ?? throw new ArgumentNullException(nameof(path));
            this.ItemIndex = itemIndex;
        }

        /// <summary>Gets the branch indices.</summary>
        public int[] Path { get; }

        /// <summary>Gets the item index inside the branch, or null for a branch-level address.</summary>
        public int? ItemIndex { get; }

        /// <summary>Gets a value indicating whether this address targets a single item.</summary>
        public bool IsItem => this.ItemIndex.HasValue;

        /// <summary>
        /// Parses a data-tree position string of the form <c>{a;b;c}</c>
        /// (a whole branch), <c>{a;b;c}(i)</c> (one item inside a branch) or
        /// <c>(i)</c> (shorthand for <c>{0}(i)</c>). Indices are zero-based.
        /// </summary>
        /// <param name="position">The address text, e.g. <c>{0;0;1}(3)</c>.</param>
        /// <param name="result">The parsed position when parsing succeeds.</param>
        /// <param name="error">A human-readable reason when parsing fails.</param>
        /// <returns>True when the position is well formed.</returns>
        public static bool TryParse(string? position, out GhJsonDataPosition result, out string error)
        {
            result = default;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(position))
            {
                error = "Position is empty.";
                return false;
            }

            var s = position.Trim();
            var parenIndex = s.IndexOf('(');
            var pathPart = (parenIndex < 0 ? s : s.Substring(0, parenIndex)).Trim();
            int? parsedItem = null;

            if (parenIndex >= 0)
            {
                var itemPart = s.Substring(parenIndex);
                if (itemPart.Length < 3 || !itemPart.EndsWith(")", StringComparison.Ordinal))
                {
                    error = $"Malformed item index in position '{position}'. Expected '(<index>)'.";
                    return false;
                }

                var digits = itemPart.Substring(1, itemPart.Length - 2);
                if (!int.TryParse(digits, out var index) || index < 0)
                {
                    error = $"Invalid item index in position '{position}'. Expected a non-negative integer.";
                    return false;
                }

                parsedItem = index;
            }

            if (string.IsNullOrEmpty(pathPart))
            {
                // "(i)" shorthand only makes sense as an item of the flat {0} branch.
                if (!parsedItem.HasValue)
                {
                    error = $"Position '{position}' is missing a branch path.";
                    return false;
                }

                result = new GhJsonDataPosition(new[] { 0 }, parsedItem);
                return true;
            }

            if (!pathPart.StartsWith("{", StringComparison.Ordinal)
                || !pathPart.EndsWith("}", StringComparison.Ordinal)
                || pathPart.Length < 3)
            {
                error = $"Malformed branch path in position '{position}'. Expected '{{a;b;c}}'.";
                return false;
            }

            var inner = pathPart.Substring(1, pathPart.Length - 2);
            var parts = inner.Split(';');
            var indices = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var value) || value < 0)
                {
                    error = $"Invalid branch index '{parts[i]}' in position '{position}'. Expected non-negative integers separated by ';'.";
                    return false;
                }

                indices[i] = value;
            }

            result = new GhJsonDataPosition(indices, parsedItem);
            return true;
        }

        /// <inheritdoc/>
        public bool Equals(GhJsonDataPosition other)
        {
            if (this.ItemIndex != other.ItemIndex)
            {
                return false;
            }

            if (this.Path == null || other.Path == null || this.Path.Length != other.Path.Length)
            {
                return this.Path == null && other.Path == null;
            }

            for (var i = 0; i < this.Path.Length; i++)
            {
                if (this.Path[i] != other.Path[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GhJsonDataPosition other && this.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = 17;
            if (this.Path != null)
            {
                foreach (var index in this.Path)
                {
                    hash = (hash * 31) + index;
                }
            }

            return (hash * 31) + this.ItemIndex.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var pathText = this.Path == null ? "{0}" : $"{{{string.Join(";", this.Path)}}}";
            return this.ItemIndex.HasValue ? $"{pathText}({this.ItemIndex.Value})" : pathText;
        }
    }
}
