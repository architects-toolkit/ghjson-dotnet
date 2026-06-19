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
using GhJSON.Core.NameResolution;
using Xunit;

namespace GhJSON.Core.Tests.NameResolution
{
    /// <summary>
    /// Boundary and determinism tests for <see cref="FuzzyMatcher"/>: null/empty
    /// inputs, deterministic tie-breaking, distance disabling, and unicode.
    /// </summary>
    public class FuzzyMatcherEdgeCaseTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FindBestMatch_NullOrWhitespaceInput_ReturnsNull(string? input)
        {
            var result = FuzzyMatcher.FindBestMatch(input!, new[] { "Addition", "Subtraction" });

            Assert.Null(result);
        }

        [Fact]
        public void FindBestMatch_EmptyCandidates_ReturnsNull()
        {
            Assert.Null(FuzzyMatcher.FindBestMatch("Addition", Array.Empty<string>()));
        }

        [Fact]
        public void FindBestMatch_ExactMatch_IsCaseInsensitive()
        {
            var result = FuzzyMatcher.FindBestMatch("ADDITION", new[] { "Addition", "Subtraction" });

            Assert.Equal("Addition", result);
        }

        [Fact]
        public void FindBestMatch_TieOnContains_PrefersShortestDeterministically()
        {
            var forward = new[] { "Addition Extended Edition", "Addition Extended", "Addition Super Extended" };
            var reversed = new[] { "Addition Super Extended", "Addition Extended", "Addition Extended Edition" };

            // Stable across enumeration orders — must pick the shortest (deterministic tie-break).
            var match1 = FuzzyMatcher.FindBestMatch("Addition", forward);
            var match2 = FuzzyMatcher.FindBestMatch("Addition", reversed);

            Assert.Equal(match1, match2);
            Assert.Equal("Addition Extended", match1);
        }

        [Fact]
        public void FindBestMatch_LevenshteinDisabled_DoesNotMatchFar()
        {
            // "Adition" (missing 'd') vs "Addition" → distance 1, but maxLevenshteinDistance=0
            // disables fuzzy matching entirely.
            var result = FuzzyMatcher.FindBestMatch(
                "Adition",
                new[] { "Addition", "Subtraction" },
                maxLevenshteinDistance: 0);

            Assert.Null(result);
        }

        [Fact]
        public void LevenshteinDistance_EmptyStrings_ReturnsZero()
        {
            Assert.Equal(0, FuzzyMatcher.LevenshteinDistance(string.Empty, string.Empty));
            Assert.Equal(0, FuzzyMatcher.LevenshteinDistance(null!, null!));
        }

        [Fact]
        public void LevenshteinDistance_IsSymmetric()
        {
            var forward = FuzzyMatcher.LevenshteinDistance("kitten", "sitting");
            var backward = FuzzyMatcher.LevenshteinDistance("sitting", "kitten");

            Assert.Equal(forward, backward);
        }

        [Fact]
        public void Normalize_StripsSpacesUnderscoresAndHyphens()
        {
            Assert.Equal("numberslider", FuzzyMatcher.Normalize("Number_Slider"));
            Assert.Equal("numberslider", FuzzyMatcher.Normalize("Number-Slider"));
            Assert.Equal("numberslider", FuzzyMatcher.Normalize("Number Slider"));
        }
    }
}
