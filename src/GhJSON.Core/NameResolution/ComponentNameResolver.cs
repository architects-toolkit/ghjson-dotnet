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

/*
 * Portions of this code adapted from:
 * https://github.com/alfredatnycu/grasshopper-mcp
 * MIT License
 * Copyright (c) 2025 Alfred Chen
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GhJSON.Core.NameResolution
{
    /// <summary>
    /// Resolves informal or abbreviated Grasshopper component names to their canonical form.
    /// Uses a built-in alias dictionary and falls back to <see cref="FuzzyMatcher"/> for
    /// approximate matching against a set of known component names.
    /// </summary>
    public static class ComponentNameResolver
    {
        /// <summary>
        /// Built-in alias map from common shorthand names to canonical Grasshopper component names.
        /// Keys are normalized (lowercase, no separators).
        /// </summary>
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Plane components
            { "plane", "XY Plane" },
            { "xyplane", "XY Plane" },
            { "xy", "XY Plane" },
            { "xzplane", "XZ Plane" },
            { "xz", "XZ Plane" },
            { "yzplane", "YZ Plane" },
            { "yz", "YZ Plane" },
            { "plane3pt", "Plane 3Pt" },
            { "3ptplane", "Plane 3Pt" },

            // Basic geometry components
            { "box", "Box" },
            { "cube", "Box" },
            { "rectangle", "Rectangle" },
            { "rect", "Rectangle" },
            { "circle", "Circle" },
            { "circ", "Circle" },
            { "sphere", "Sphere" },
            { "cylinder", "Cylinder" },
            { "cyl", "Cylinder" },
            { "cone", "Cone" },
            { "line", "Line" },
            { "ln", "Line" },

            // Parameter components
            { "slider", "Number Slider" },
            { "numberslider", "Number Slider" },
            { "numslider", "Number Slider" },
            { "number slider", "Number Slider" },
            { "panel", "Panel" },
            { "point", "Point" },
            { "pt", "Point" },
            { "curve", "Curve" },
            { "crv", "Curve" },
            { "number", "Number" },
            { "num", "Number" },
            { "integer", "Integer" },
            { "int", "Integer" },
            { "boolean", "Boolean" },
            { "bool", "Boolean" },
            { "str", "Text" },
            { "string", "Text" },
            { "toggle", "Boolean Toggle" },
            { "booleantoggle", "Boolean Toggle" },

            // Construct components
            { "constructpoint", "Construct Point" },
            { "ptxyz", "Construct Point" },
            { "xyz", "Construct Point" },
            { "constructplane", "Construct Plane" },
            { "constructvector", "Construct Vector" },
            { "vec", "Vector XYZ" },
            { "vector", "Vector XYZ" },
            { "vectorxyz", "Vector XYZ" },

            // Math components
            { "add", "Addition" },
            { "addition", "Addition" },
            { "plus", "Addition" },
            { "sub", "Subtraction" },
            { "subtraction", "Subtraction" },
            { "minus", "Subtraction" },
            { "mul", "Multiplication" },
            { "multiplication", "Multiplication" },
            { "multiply", "Multiplication" },
            { "div", "Division" },
            { "division", "Division" },
            { "divide", "Division" },
            { "abs", "Absolute" },
            { "absolute", "Absolute" },
            { "neg", "Negative" },
            { "negative", "Negative" },
            { "pow", "Power" },
            { "power", "Power" },
            { "sqrt", "Square Root" },
            { "squareroot", "Square Root" },

            // List components
            { "listitem", "List Item" },
            { "listlength", "List Length" },
            { "reverse", "Reverse List" },
            { "reverselist", "Reverse List" },
            { "sort", "Sort List" },
            { "sortlist", "Sort List" },
            { "flatten", "Flatten" },
            { "graft", "Graft Tree" },
            { "grafttree", "Graft Tree" },

            // Set components
            { "series", "Series" },
            { "range", "Range" },
            { "random", "Random" },
            { "domain", "Construct Domain" },
            { "constructdomain", "Construct Domain" },

            // Transform components
            { "move", "Move" },
            { "rotate", "Rotate" },
            { "scale", "Scale" },
            { "mirror", "Mirror" },
            { "orient", "Orient" },

            // Surface components
            { "loft", "Loft" },
            { "extrude", "Extrude" },
            { "extrudepoint", "Extrude Point" },
            { "sweep", "Sweep1" },
            { "sweep1", "Sweep1" },
            { "sweep2", "Sweep2" },

            // Mesh components
            { "mesh", "Mesh" },
            { "meshbox", "Mesh Box" },
            { "meshsphere", "Mesh Sphere" },

            // Display components
            { "colour", "Colour Swatch" },
            { "color", "Colour Swatch" },
            { "colourswatch", "Colour Swatch" },
            { "colorswatch", "Colour Swatch" },

            // Stream components
            { "streamfilter", "Stream Filter" },
            { "filter", "Stream Filter" },

            // Script components
            { "c#", "C# Script" },
            { "c#script", "C# Script" },
            { "c# component", "C# Script" },
            { "csharp", "C# Script" },
            { "csharp script", "C# Script" },
            { "cscript", "C# Script" },
            { "python", "Python 3 Script" },
            { "py", "Python 3 Script" },
            { "python3", "Python 3 Script" },
            { "pythonscript", "Python 3 Script" },
            { "python script", "Python 3 Script" },
            { "ghpython", "Python 3 Script" },
            { "vb", "VB Script" },
            { "vbscript", "VB Script" },
            { "ironpython", "IronPython 2 Script" },
        };

        /// <summary>
        /// Resolves a component name using the built-in alias dictionary.
        /// Returns the canonical name if an alias is found, or null otherwise.
        /// </summary>
        /// <param name="input">The component name or alias to resolve.</param>
        /// <returns>The canonical component name, or null if no alias matches.</returns>
        public static string? ResolveAlias(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Debug.WriteLine($"[ComponentNameResolver.ResolveAlias] Returning null - input is null/whitespace");
                return null;
            }

            var normalized = FuzzyMatcher.Normalize(input);
            if (Aliases.TryGetValue(normalized, out var canonical))
            {
                Debug.WriteLine($"[ComponentNameResolver.ResolveAlias] Resolved alias '{input}' (normalized: '{normalized}') → '{canonical}'");
                return canonical;
            }

            Debug.WriteLine($"[ComponentNameResolver.ResolveAlias] No alias found for '{input}' (normalized: '{normalized}')");
            return null;
        }

        /// <summary>
        /// Resolves a component name by first checking aliases, then falling back to
        /// fuzzy matching against the provided list of known component names.
        /// </summary>
        /// <param name="input">The component name or alias to resolve.</param>
        /// <param name="knownComponentNames">The set of known canonical component names to match against.</param>
        /// <param name="maxLevenshteinDistance">Maximum edit distance for fuzzy matching. Defaults to 3.</param>
        /// <returns>The resolved component name, or null if no match is found.</returns>
        public static string? Resolve(string input, IEnumerable<string> knownComponentNames, int maxLevenshteinDistance = 3)
        {
            Debug.WriteLine($"[ComponentNameResolver.Resolve] Resolving '{input}' with maxLevenshteinDistance={maxLevenshteinDistance}");

            // Materialize candidates once so we can probe for exact membership.
            var candidates = knownComponentNames as IList<string> ?? new List<string>(knownComponentNames);

            // Try alias first; only return it directly if it exists exactly in the known set.
            // Otherwise, prefer the alias as a better seed for fuzzy matching against the
            // actual registered names (e.g. "python" → alias "Python 3 Script").
            var alias = ResolveAlias(input);
            string fuzzyInput = input;
            if (alias != null)
            {
                foreach (var candidate in candidates)
                {
                    if (string.Equals(candidate, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"[ComponentNameResolver.Resolve] Alias '{alias}' exists in known names, returning it");
                        return candidate;
                    }
                }

                Debug.WriteLine($"[ComponentNameResolver.Resolve] Alias '{alias}' not in known names, using it as fuzzy seed");
                fuzzyInput = alias;
            }
            else
            {
                Debug.WriteLine($"[ComponentNameResolver.Resolve] No alias found, falling back to fuzzy matching on input");
            }

            // Fuzzy match against known names using the (possibly aliased) input
            var fuzzyResult = FuzzyMatcher.FindBestMatch(fuzzyInput, candidates, maxLevenshteinDistance);
            
            if (fuzzyResult != null)
            {
                Debug.WriteLine($"[ComponentNameResolver.Resolve] Fuzzy match found: '{fuzzyResult}'");
            }
            else
            {
                Debug.WriteLine($"[ComponentNameResolver.Resolve] No fuzzy match found for '{input}'");
            }

            return fuzzyResult;
        }
    }
}
