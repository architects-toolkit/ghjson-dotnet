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
using System.Collections.Generic;

namespace GhJSON.Grasshopper.Query
{
    /// <summary>
    /// Parses include/exclude filter tokens and resolves synonyms.
    /// Tokens use <c>+</c>/<c>-</c> prefix syntax (e.g. <c>"+selected"</c>, <c>"-error"</c>).
    /// A token without a prefix is treated as an include.
    /// </summary>
    public static class FilterParser
    {
        /// <summary>
        /// Canonical synonyms for attribute filter tags.
        /// </summary>
        public static readonly Dictionary<string, string> AttributeSynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "LOCKED", "DISABLED" },
            { "UNLOCKED", "ENABLED" },
            { "REMARKS", "REMARK" },
            { "INFO", "REMARK" },
            { "WARN", "WARNING" },
            { "WARNINGS", "WARNING" },
            { "ERRORS", "ERROR" },
            { "VISIBLE", "PREVIEWON" },
            { "HIDDEN", "PREVIEWOFF" },
        };

        /// <summary>
        /// Canonical synonyms for type filter tags.
        /// </summary>
        public static readonly Dictionary<string, string> TypeSynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PARAM", "PARAMS" },
            { "PARAMETER", "PARAMS" },
            { "COMPONENT", "COMPONENTS" },
            { "COMP", "COMPONENTS" },
            { "INPUT", "STARTNODES" },
            { "INPUTS", "STARTNODES" },
            { "INPUTCOMPONENTS", "STARTNODES" },
            { "OUTPUT", "ENDNODES" },
            { "OUTPUTS", "ENDNODES" },
            { "OUTPUTCOMPONENTS", "ENDNODES" },
            { "PROCESSING", "MIDDLENODES" },
            { "PROCESSINGCOMPONENTS", "MIDDLENODES" },
            { "INTERMEDIATE", "MIDDLENODES" },
            { "MIDDLE", "MIDDLENODES" },
            { "MIDDLECOMPONENTS", "MIDDLENODES" },
            { "ISOLATED", "ISOLATEDNODES" },
            { "ISOLATEDCOMPONENTS", "ISOLATEDNODES" },
        };

        /// <summary>
        /// Canonical synonyms for category filter tags.
        /// </summary>
        public static readonly Dictionary<string, string> CategorySynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PARAM", "PARAMS" },
            { "PARAMETERS", "PARAMS" },
            { "MATH", "MATHS" },
            { "VEC", "VECTOR" },
            { "VECTORS", "VECTOR" },
            { "CRV", "CURVE" },
            { "CURVES", "CURVE" },
            { "SURF", "SURFACE" },
            { "SURFS", "SURFACE" },
            { "MESHES", "MESH" },
            { "INT", "INTERSECT" },
            { "TRANS", "TRANSFORM" },
            { "TREE", "SETS" },
            { "TREES", "SETS" },
            { "DATA", "SETS" },
            { "DATASETS", "SETS" },
            { "DIS", "DISPLAY" },
            { "DISP", "DISPLAY" },
            { "VISUALIZATION", "DISPLAY" },
            { "RH", "RHINO" },
            { "RHINOCEROS", "RHINO" },
            { "KANGAROOPHYSICS", "KANGAROO" },
            { "SCRIPTS", "SCRIPT" },
        };

        /// <summary>
        /// Parses an enumerable of raw filter tokens into include/exclude sets.
        /// Each token may contain <c>+</c>/<c>-</c> prefix and may be comma/semicolon/space separated.
        /// </summary>
        /// <param name="rawTokens">Raw filter strings (e.g. <c>"+selected"</c>, <c>"-error,warning"</c>).</param>
        /// <param name="synonyms">Optional synonym dictionary for canonical resolution.</param>
        /// <returns>A tuple of (Include, Exclude) sets with canonicalized uppercase tags.</returns>
        public static (HashSet<string> Include, HashSet<string> Exclude) ParseIncludeExclude(
            IEnumerable<string>? rawTokens,
            Dictionary<string, string>? synonyms = null)
        {
            var include = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (rawTokens == null)
            {
                return (include, exclude);
            }

            foreach (var rawGroup in rawTokens)
            {
                if (string.IsNullOrWhiteSpace(rawGroup))
                {
                    continue;
                }

                var parts = rawGroup.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var tok = part.Trim();
                    if (string.IsNullOrEmpty(tok))
                    {
                        continue;
                    }

                    bool isInclude = !tok.StartsWith("-");
                    var tag = tok.TrimStart('+', '-');

                    if (synonyms != null && synonyms.TryGetValue(tag, out var mapped))
                    {
                        tag = mapped;
                    }
                    else
                    {
                        // Canonical tags (not in synonyms) must be uppercased
                        // to match the switch labels in CanvasSelector.MatchesAttribute()
                        tag = tag.ToUpperInvariant();
                    }

                    if (isInclude)
                    {
                        include.Add(tag);
                    }
                    else
                    {
                        exclude.Add(tag);
                    }
                }
            }

            return (include, exclude);
        }

        /// <summary>
        /// Checks whether a component passes category/subcategory include/exclude filters.
        /// Comparison is case-insensitive.
        /// </summary>
        /// <param name="category">Grasshopper category name.</param>
        /// <param name="subCategory">Grasshopper subcategory name.</param>
        /// <param name="includeCategories">Set of canonical include category tokens.</param>
        /// <param name="excludeCategories">Set of canonical exclude category tokens.</param>
        /// <returns><c>true</c> if the component should be kept; otherwise <c>false</c>.</returns>
        public static bool PassesCategoryFilter(
            string? category,
            string? subCategory,
            HashSet<string> includeCategories,
            HashSet<string> excludeCategories)
        {
            var hasIncludeList = includeCategories != null && includeCategories.Count > 0;
            var hasExcludeList = excludeCategories != null && excludeCategories.Count > 0;

            var includeMatch = hasIncludeList &&
                ((!string.IsNullOrEmpty(category) && includeCategories!.Contains(category)) ||
                 (!string.IsNullOrEmpty(subCategory) && includeCategories!.Contains(subCategory)));

            // When includeCategories is provided, require at least one include match.
            if (hasIncludeList && !includeMatch)
            {
                return false;
            }

            var excludeMatch = hasExcludeList &&
                ((!string.IsNullOrEmpty(category) && excludeCategories!.Contains(category)) ||
                 (!string.IsNullOrEmpty(subCategory) && excludeCategories!.Contains(subCategory)));

            // Be permissive: if there is both an include and an exclude match (e.g. category in include,
            // subcategory in exclude), the include wins and the component is kept.
            if (!includeMatch && excludeMatch)
            {
                return false;
            }

            return true;
        }
    }
}
