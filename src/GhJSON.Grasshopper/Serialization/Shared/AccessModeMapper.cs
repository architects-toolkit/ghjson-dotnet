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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.Shared
{
    /// <summary>
    /// Bidirectional mapper for GH_ParamAccess and string representations.
    /// Provides consistent access mode conversion across serialization and deserialization.
    /// </summary>
    public static class AccessModeMapper
    {
        /// <summary>
        /// Converts a GH_ParamAccess enum to its string representation.
        /// </summary>
        /// <param name="access">The access mode to convert.</param>
        /// <returns>String representation ("item", "list", or "tree").</returns>
        public static string ToString(GH_ParamAccess access)
        {
            return access switch
            {
                GH_ParamAccess.item => "item",
                GH_ParamAccess.list => "list",
                GH_ParamAccess.tree => "tree",
                _ => "item"
            };
        }

        /// <summary>
        /// Converts a string representation to GH_ParamAccess enum.
        /// </summary>
        /// <param name="accessString">String representation of access mode.</param>
        /// <returns>GH_ParamAccess value, defaults to item if parsing fails.</returns>
        public static GH_ParamAccess FromString(string? accessString)
        {
            if (string.IsNullOrWhiteSpace(accessString))
                return GH_ParamAccess.item;

            if (Enum.TryParse<GH_ParamAccess>(accessString, true, out var parsedAccess))
                return parsedAccess;

            return GH_ParamAccess.item;
        }

        /// <summary>
        /// Checks if a string represents a valid access mode.
        /// </summary>
        /// <param name="accessString">String to validate.</param>
        /// <returns>True if valid access mode string.</returns>
        public static bool IsValid(string? accessString)
        {
            if (string.IsNullOrWhiteSpace(accessString))
                return false;

            return accessString.Equals("item", StringComparison.OrdinalIgnoreCase) ||
                   accessString.Equals("list", StringComparison.OrdinalIgnoreCase) ||
                   accessString.Equals("tree", StringComparison.OrdinalIgnoreCase);
        }
    }
}
