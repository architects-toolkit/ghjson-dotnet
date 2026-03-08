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
using System.Linq;

namespace GhJSON.Core.NameResolution
{
    /// <summary>
    /// Provides fuzzy string matching utilities for resolving component and parameter names.
    /// Supports exact, prefix, contains, and Levenshtein distance matching strategies.
    /// </summary>
    public static class FuzzyMatcher
    {
        /// <summary>
        /// Finds the best match for <paramref name="input"/> among <paramref name="candidates"/>
        /// using a multi-strategy approach: exact → prefix → contains → Levenshtein.
        /// </summary>
        /// <param name="input">The input string to match.</param>
        /// <param name="candidates">The candidate strings to match against.</param>
        /// <param name="maxLevenshteinDistance">
        /// Maximum Levenshtein distance to consider a match. Defaults to 3.
        /// Set to 0 to disable Levenshtein matching.
        /// </param>
        /// <returns>The best matching candidate, or null if no match is found.</returns>
        public static string? FindBestMatch(string input, IEnumerable<string> candidates, int maxLevenshteinDistance = 3)
        {
            if (string.IsNullOrWhiteSpace(input) || candidates == null)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Returning null - input is null/whitespace or candidates is null");
                return null;
            }

            var candidateList = candidates as IList<string> ?? candidates.ToList();
            if (candidateList.Count == 0)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Returning null - no candidates provided");
                return null;
            }

            Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Finding match for '{input}' among {candidateList.Count} candidates");

            // 1. Exact match (case-insensitive)
            var exact = candidateList.FirstOrDefault(c =>
                string.Equals(c, input, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Found exact match: '{exact}'");
                return exact;
            }

            // 2. Normalized match (strip spaces, underscores, hyphens)
            var normalizedInput = Normalize(input);
            Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Trying normalized match (input normalized to: '{normalizedInput}')");
            var normalizedMatch = candidateList.FirstOrDefault(c =>
                string.Equals(Normalize(c), normalizedInput, StringComparison.OrdinalIgnoreCase));
            if (normalizedMatch != null)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Found normalized match: '{normalizedMatch}'");
                return normalizedMatch;
            }

            // 3. Prefix match — return shortest if unique prefix
            var prefixMatches = candidateList
                .Where(c => c.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (prefixMatches.Count == 1)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Found unique prefix match: '{prefixMatches[0]}'");
                return prefixMatches[0];
            }

            Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Prefix matches: {prefixMatches.Count}");

            // 4. Contains match — return shortest if unique
            var containsMatches = candidateList
                .Where(c => c.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (containsMatches.Count == 1)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Found unique contains match: '{containsMatches[0]}'");
                return containsMatches[0];
            }

            if (containsMatches.Count > 1)
            {
                var shortest = containsMatches.OrderBy(c => c.Length).First();
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Found {containsMatches.Count} contains matches, selecting shortest: '{shortest}'");
                return shortest;
            }

            Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Contains matches: {containsMatches.Count}");

            // 5. Levenshtein distance (only if enabled)
            if (maxLevenshteinDistance > 0)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Trying Levenshtein distance (max distance: {maxLevenshteinDistance})");
                var levenshteinMatch = FindByLevenshtein(normalizedInput, candidateList, maxLevenshteinDistance);
                if (levenshteinMatch != null)
                {
                    Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] Found Levenshtein match: '{levenshteinMatch}'");
                    return levenshteinMatch;
                }
            }

            Debug.WriteLine($"[FuzzyMatcher.FindBestMatch] No match found for '{input}'");
            return null;
        }

        /// <summary>
        /// Normalizes a string by converting to lowercase and removing spaces, underscores, and hyphens.
        /// </summary>
        /// <param name="value">The string to normalize.</param>
        /// <returns>The normalized string.</returns>
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var chars = new char[value.Length];
            var length = 0;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c != ' ' && c != '_' && c != '-')
                {
                    chars[length++] = char.ToLowerInvariant(c);
                }
            }

            return new string(chars, 0, length);
        }

        /// <summary>
        /// Computes the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="a">The first string.</param>
        /// <param name="b">The second string.</param>
        /// <returns>The edit distance between the two strings.</returns>
        public static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return string.IsNullOrEmpty(b) ? 0 : b.Length;
            }

            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }

            var lengthA = a.Length;
            var lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];

            for (var i = 0; i <= lengthA; i++)
            {
                distances[i, 0] = i;
            }

            for (var j = 0; j <= lengthB; j++)
            {
                distances[0, j] = j;
            }

            for (var i = 1; i <= lengthA; i++)
            {
                for (var j = 1; j <= lengthB; j++)
                {
                    var cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;

                    distances[i, j] = Math.Min(
                        Math.Min(
                            distances[i - 1, j] + 1,       // deletion
                            distances[i, j - 1] + 1),      // insertion
                        distances[i - 1, j - 1] + cost);   // substitution
                }
            }

            return distances[lengthA, lengthB];
        }

        private static string? FindByLevenshtein(string normalizedInput, IList<string> candidates, int maxDistance)
        {
            string? bestMatch = null;
            var bestDistance = int.MaxValue;

            Debug.WriteLine($"[FuzzyMatcher.FindByLevenshtein] Searching among {candidates.Count} candidates, normalized input: '{normalizedInput}', max distance: {maxDistance}");

            foreach (var candidate in candidates)
            {
                var normalizedCandidate = Normalize(candidate);
                var distance = LevenshteinDistance(normalizedInput, normalizedCandidate);

                if (distance < bestDistance && distance <= maxDistance)
                {
                    Debug.WriteLine($"[FuzzyMatcher.FindByLevenshtein] New best match: '{candidate}' (distance: {distance})");
                    bestDistance = distance;
                    bestMatch = candidate;
                }
            }

            if (bestMatch == null)
            {
                Debug.WriteLine($"[FuzzyMatcher.FindByLevenshtein] No match found within distance {maxDistance}");
            }
            else
            {
                Debug.WriteLine($"[FuzzyMatcher.FindByLevenshtein] Final best match: '{bestMatch}' with distance {bestDistance}");
            }

            return bestMatch;
        }
    }
}
