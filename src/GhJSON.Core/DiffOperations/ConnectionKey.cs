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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Stable identity key for a connection.
    /// </summary>
    /// <remarks>
    /// Identity is the full <c>(from, to)</c> endpoint pair. Endpoint identity prefers
    /// <c>paramName</c> when present and falls back to <c>paramIndex</c>.
    /// </remarks>
    internal readonly struct ConnectionKey : IEquatable<ConnectionKey>
    {
        public int FromId { get; }

        public string FromParam { get; }

        public int ToId { get; }

        public string ToParam { get; }

        private ConnectionKey(int fromId, string fromParam, int toId, string toParam)
        {
            this.FromId = fromId;
            this.FromParam = fromParam;
            this.ToId = toId;
            this.ToParam = toParam;
        }

        public static ConnectionKey From(GhJsonConnection connection)
        {
            return new ConnectionKey(
                connection.From.Id,
                Param(connection.From.ParamName, connection.From.ParamIndex),
                connection.To.Id,
                Param(connection.To.ParamName, connection.To.ParamIndex));
        }

        private static string Param(string? name, int? index)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return "n:" + name;
            }

            if (index.HasValue)
            {
                return "i:" + index.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return "_";
        }

        public bool Equals(ConnectionKey other)
        {
            return this.FromId == other.FromId
                && this.ToId == other.ToId
                && string.Equals(this.FromParam, other.FromParam, StringComparison.Ordinal)
                && string.Equals(this.ToParam, other.ToParam, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is ConnectionKey other && this.Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + this.FromId.GetHashCode();
                hash = (hash * 31) + (this.FromParam?.GetHashCode() ?? 0);
                hash = (hash * 31) + this.ToId.GetHashCode();
                hash = (hash * 31) + (this.ToParam?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
