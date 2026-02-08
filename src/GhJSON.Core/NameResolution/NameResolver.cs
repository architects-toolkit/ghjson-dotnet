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

using System.Collections.Generic;

namespace GhJSON.Core.NameResolution
{
    /// <summary>
    /// Unified entry point for resolving Grasshopper component and parameter names.
    /// Delegates to <see cref="ComponentNameResolver"/> and <see cref="ParameterNameResolver"/>.
    /// </summary>
    public static class NameResolver
    {
        /// <summary>
        /// Resolves a component name using the built-in alias dictionary only.
        /// </summary>
        /// <param name="input">The component name or alias to resolve.</param>
        /// <returns>The canonical component name, or null if no alias matches.</returns>
        public static string? ResolveComponentAlias(string input)
        {
            return ComponentNameResolver.ResolveAlias(input);
        }

        /// <summary>
        /// Resolves a component name by checking aliases first, then fuzzy matching
        /// against the provided list of known component names.
        /// </summary>
        /// <param name="input">The component name or alias to resolve.</param>
        /// <param name="knownComponentNames">The set of known canonical component names.</param>
        /// <param name="maxLevenshteinDistance">Maximum edit distance for fuzzy matching. Defaults to 3.</param>
        /// <returns>The resolved component name, or null if no match is found.</returns>
        public static string? ResolveComponentName(string input, IEnumerable<string> knownComponentNames, int maxLevenshteinDistance = 3)
        {
            return ComponentNameResolver.Resolve(input, knownComponentNames, maxLevenshteinDistance);
        }

        /// <summary>
        /// Resolves a parameter name using the built-in alias dictionary only.
        /// </summary>
        /// <param name="input">The parameter name or alias to resolve.</param>
        /// <returns>The canonical parameter name, or null if no alias matches.</returns>
        public static string? ResolveParameterAlias(string input)
        {
            return ParameterNameResolver.ResolveAlias(input);
        }

        /// <summary>
        /// Resolves a parameter name by checking aliases first, then fuzzy matching
        /// against the provided list of known parameter names.
        /// </summary>
        /// <param name="input">The parameter name or alias to resolve.</param>
        /// <param name="knownParameterNames">The set of known canonical parameter names.</param>
        /// <param name="maxLevenshteinDistance">Maximum edit distance for fuzzy matching. Defaults to 2.</param>
        /// <returns>The resolved parameter name, or null if no match is found.</returns>
        public static string? ResolveParameterName(string input, IEnumerable<string> knownParameterNames, int maxLevenshteinDistance = 2)
        {
            return ParameterNameResolver.Resolve(input, knownParameterNames, maxLevenshteinDistance);
        }
    }
}
