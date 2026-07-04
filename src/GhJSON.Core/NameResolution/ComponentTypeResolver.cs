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
using System.Collections.Generic;
using System.Reflection;

namespace GhJSON.Core.NameResolution
{
    /// <summary>
    /// Provides type-based priority scoring for resolving between multiple Grasshopper
    /// components with the same name. Used to prioritize newer component implementations
    /// over legacy ones.
    /// </summary>
    public static class ComponentTypeResolver
    {
        /// <summary>
        /// Pattern-based type scoring rules. Patterns use wildcards (*).
        /// Format: "*Pattern", "Pattern*" or "*Pattern*" to match anywhere.
        /// </summary>
        private static readonly List<(string Pattern, int Score)> TypePriorityPatterns = new()
        {
            // Script components: new CSharp/Python/VB vs old NET_Script
            ("*NET_Script", -10),        // Old script component pattern (low priority)
            ("*IronPython_Script", -10),  // Old IronPython script pattern
            // Add more patterns here as needed, e.g.:
            // ("Legacy*", -5),
        };

        /// <summary>
        /// Calculates a priority score for a component type. Matching type-name patterns
        /// and obsolete/deprecated markers have their scores summed. Default score is 0.
        /// Obsolete components receive a large penalty so current implementations win
        /// when multiple components share the same display name.
        /// </summary>
        /// <param name="type">The component type to score.</param>
        /// <returns>The calculated priority score.</returns>
        public static int CalculateTypePriorityScore(Type? type)
        {
            if (type == null)
            {
                return 0;
            }

            int score = CalculateTypePriorityScore(type.Name);

            if (IsObsoleteType(type))
            {
                score -= 100;
            }

            return score;
        }

        /// <summary>
        /// Calculates the pattern-matching portion of the priority score for a type name.
        /// This overload does not perform obsolete/deprecated detection because it has no
        /// access to the component type metadata.
        /// </summary>
        /// <param name="typeName">The component type name to score.</param>
        /// <returns>The calculated pattern-based priority score.</returns>
        public static int CalculateTypePriorityScore(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return 0;
            }

            int score = 0;
            foreach (var (pattern, patternScore) in TypePriorityPatterns)
            {
                if (PatternMatches(pattern, typeName))
                {
                    score += patternScore;
                }
            }

            return score;
        }

        /// <summary>
        /// Detects legacy component types by checking for the standard
        /// <see cref="ObsoleteAttribute"/>, obsolete/deprecated naming conventions, or
        /// a public boolean <c>Obsolete</c> property on an instantiated instance.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><c>true</c> if the type appears to be obsolete/deprecated.</returns>
        private static bool IsObsoleteType(Type type)
        {
            if (type.GetCustomAttribute<ObsoleteAttribute>(inherit: false) != null)
            {
                return true;
            }

            var name = type.Name;
            if (name.Contains("OBSOLETE", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("DEPRECATED", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("LEGACY", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback: instantiate the type and query a public bool Obsolete property.
            // This is required because Grasshopper's GH_DocumentObject.Obsolete property
            // is virtual and may be overridden without using the ObsoleteAttribute.
            try
            {
                var obsoleteProperty = type.GetProperty(
                    "Obsolete",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    typeof(bool),
                    Type.EmptyTypes,
                    null);

                if (obsoleteProperty != null &&
                    obsoleteProperty.CanRead &&
                    Activator.CreateInstance(type) is object instance)
                {
                    return obsoleteProperty.GetValue(instance) is true;
                }
            }
            catch
            {
                // Instantiation may fail for types without a parameterless constructor
                // or that require a document context. In that case, treat as non-obsolete.
            }

            return false;
        }

        /// <summary>
        /// Checks if a pattern matches a type name. Supports * wildcards at start, end, or both.
        /// </summary>
        /// <param name="pattern">The pattern to match, may include * wildcards.</param>
        /// <param name="typeName">The type name to check against the pattern.</param>
        /// <returns>True if the pattern matches the type name.</returns>
        private static bool PatternMatches(string pattern, string typeName)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            if (pattern.StartsWith("*") && pattern.EndsWith("*") && pattern.Length > 2)
            {
                // *pattern* - contains match
                var inner = pattern.Substring(1, pattern.Length - 2);
                return typeName.IndexOf(inner, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            else if (pattern.StartsWith("*") && pattern.Length > 1)
            {
                // *pattern - ends with
                var suffix = pattern.Substring(1);
                return typeName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }
            else if (pattern.EndsWith("*") && pattern.Length > 1)
            {
                // pattern* - starts with
                var prefix = pattern.Substring(0, pattern.Length - 1);
                return typeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // exact match
                return typeName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
