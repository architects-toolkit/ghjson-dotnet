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

/*
 * Portions of this code adapted from:
 * https://github.com/alfredatnycu/grasshopper-mcp
 * MIT License
 * Copyright (c) 2025 Alfred Chen
 */

using System;
using System.Collections.Generic;

namespace GhJSON.Core.NameResolution
{
    /// <summary>
    /// Resolves informal or abbreviated Grasshopper parameter names to their canonical form.
    /// Uses a built-in alias dictionary and falls back to <see cref="FuzzyMatcher"/> for
    /// approximate matching against a set of known parameter names.
    /// </summary>
    public static class ParameterNameResolver
    {
        /// <summary>
        /// Built-in alias map from common shorthand parameter names to canonical Grasshopper parameter names.
        /// Keys are normalized (lowercase, no separators).
        /// </summary>
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Plane parameters
            { "plane", "Plane" },
            { "base", "Base" },
            { "origin", "Origin" },

            // Dimension parameters
            { "radius", "Radius" },
            { "r", "Radius" },
            { "size", "Size" },
            { "xsize", "X Size" },
            { "ysize", "Y Size" },
            { "zsize", "Z Size" },
            { "width", "X Size" },
            { "length", "Y Size" },
            { "height", "Z Size" },
            { "x", "X" },
            { "y", "Y" },
            { "z", "Z" },

            // Point parameters
            { "point", "Point" },
            { "pt", "Point" },
            { "center", "Center" },
            { "start", "Start" },
            { "end", "End" },

            // Numeric parameters
            { "number", "Number" },
            { "num", "Number" },
            { "value", "Value" },
            { "count", "Count" },
            { "step", "Step" },
            { "factor", "Factor" },
            { "angle", "Angle" },
            { "degree", "Degree" },
            { "degrees", "Degree" },

            // Math parameters
            { "a", "A" },
            { "b", "B" },
            { "result", "Result" },

            // Geometry parameters
            { "geometry", "Geometry" },
            { "geo", "Geometry" },
            { "brep", "Brep" },
            { "surface", "Surface" },
            { "srf", "Surface" },
            { "curve", "Curve" },
            { "crv", "Curve" },
            { "mesh", "Mesh" },
            { "line", "Line" },

            // Output parameters
            { "output", "Output" },
            { "out", "Output" },
            { "input", "Input" },
            { "in", "Input" },

            // Direction parameters
            { "direction", "Direction" },
            { "dir", "Direction" },
            { "normal", "Normal" },
            { "vector", "Vector" },
            { "vec", "Vector" },

            // Domain parameters
            { "domain", "Domain" },
            { "interval", "Domain" },

            // Boolean parameters
            { "boolean", "Boolean" },
            { "bool", "Boolean" },
            { "toggle", "Toggle" },

            // Pattern parameters
            { "pattern", "Pattern" },
            { "list", "List" },
            { "tree", "Tree" },
            { "data", "Data" },
            { "index", "Index" },
            { "i", "Index" },
        };

        /// <summary>
        /// Resolves a parameter name using the built-in alias dictionary.
        /// Returns the canonical name if an alias is found, or null otherwise.
        /// </summary>
        /// <param name="input">The parameter name or alias to resolve.</param>
        /// <returns>The canonical parameter name, or null if no alias matches.</returns>
        public static string? ResolveAlias(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var normalized = FuzzyMatcher.Normalize(input);
            return Aliases.TryGetValue(normalized, out var canonical) ? canonical : null;
        }

        /// <summary>
        /// Resolves a parameter name by first checking aliases, then falling back to
        /// fuzzy matching against the provided list of known parameter names.
        /// </summary>
        /// <param name="input">The parameter name or alias to resolve.</param>
        /// <param name="knownParameterNames">The set of known canonical parameter names to match against.</param>
        /// <param name="maxLevenshteinDistance">Maximum edit distance for fuzzy matching. Defaults to 2.</param>
        /// <returns>The resolved parameter name, or null if no match is found.</returns>
        public static string? Resolve(string input, IEnumerable<string> knownParameterNames, int maxLevenshteinDistance = 2)
        {
            // Try alias first
            var alias = ResolveAlias(input);
            if (alias != null)
            {
                return alias;
            }

            // Fuzzy match against known names
            return FuzzyMatcher.FindBestMatch(input, knownParameterNames, maxLevenshteinDistance);
        }
    }
}
