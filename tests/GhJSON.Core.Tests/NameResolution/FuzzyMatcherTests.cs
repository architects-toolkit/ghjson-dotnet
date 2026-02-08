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

using GhJSON.Core.NameResolution;
using Xunit;

namespace GhJSON.Core.Tests.NameResolution
{
    public class FuzzyMatcherTests
    {
        #region Normalize

        [Theory]
        [InlineData("Number Slider", "numberslider")]
        [InlineData("XY_Plane", "xyplane")]
        [InlineData("construct-point", "constructpoint")]
        [InlineData("  spaces  ", "spaces")]
        [InlineData("", "")]
        public void Normalize_RemovesSeparatorsAndLowercases(string input, string expected)
        {
            Assert.Equal(expected, FuzzyMatcher.Normalize(input));
        }

        #endregion

        #region LevenshteinDistance

        [Theory]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("", "", 0)]
        [InlineData("abc", "", 3)]
        [InlineData("", "abc", 3)]
        [InlineData("same", "same", 0)]
        [InlineData("Addition", "Addtion", 1)]
        [InlineData("Sphere", "Sphre", 1)]
        public void LevenshteinDistance_ReturnsCorrectDistance(string a, string b, int expected)
        {
            Assert.Equal(expected, FuzzyMatcher.LevenshteinDistance(a, b));
        }

        [Fact]
        public void LevenshteinDistance_IsCaseInsensitive()
        {
            Assert.Equal(0, FuzzyMatcher.LevenshteinDistance("Addition", "addition"));
        }

        #endregion

        #region FindBestMatch — Exact

        [Fact]
        public void FindBestMatch_ExactMatch_ReturnsCandidatePreservingCase()
        {
            var candidates = new[] { "Addition", "Subtraction", "Multiplication" };
            Assert.Equal("Addition", FuzzyMatcher.FindBestMatch("addition", candidates));
        }

        [Fact]
        public void FindBestMatch_ExactMatchCaseInsensitive()
        {
            var candidates = new[] { "Number Slider", "Panel" };
            Assert.Equal("Number Slider", FuzzyMatcher.FindBestMatch("number slider", candidates));
        }

        #endregion

        #region FindBestMatch — Normalized

        [Fact]
        public void FindBestMatch_NormalizedMatch_StripsSpacesAndUnderscores()
        {
            var candidates = new[] { "Number Slider", "Panel", "Boolean Toggle" };
            Assert.Equal("Number Slider", FuzzyMatcher.FindBestMatch("numberslider", candidates));
        }

        [Fact]
        public void FindBestMatch_NormalizedMatch_StripsHyphens()
        {
            var candidates = new[] { "Construct Point", "Deconstruct Point" };
            Assert.Equal("Construct Point", FuzzyMatcher.FindBestMatch("construct-point", candidates));
        }

        #endregion

        #region FindBestMatch — Prefix

        [Fact]
        public void FindBestMatch_UniquePrefixMatch()
        {
            var candidates = new[] { "Addition", "Subtraction", "Absolute" };
            Assert.Equal("Subtraction", FuzzyMatcher.FindBestMatch("Subtr", candidates));
        }

        [Fact]
        public void FindBestMatch_AmbiguousPrefix_FallsThrough()
        {
            // "Add" is a prefix of both "Addition" and "Address" — should not return from prefix stage
            var candidates = new[] { "Addition", "Address" };
            var result = FuzzyMatcher.FindBestMatch("Add", candidates);
            // Falls through to contains, which finds both, returns shortest
            Assert.Equal("Address", result);
        }

        #endregion

        #region FindBestMatch — Contains

        [Fact]
        public void FindBestMatch_UniqueContainsMatch()
        {
            var candidates = new[] { "Construct Point", "Deconstruct Point", "Circle" };
            Assert.Equal("Circle", FuzzyMatcher.FindBestMatch("ircl", candidates));
        }

        [Fact]
        public void FindBestMatch_MultipleContainsMatches_ReturnsShortest()
        {
            var candidates = new[] { "Construct Point", "Deconstruct Point" };
            // Both contain "Point", returns shortest
            Assert.Equal("Construct Point", FuzzyMatcher.FindBestMatch("Point", candidates));
        }

        #endregion

        #region FindBestMatch — Levenshtein

        [Fact]
        public void FindBestMatch_LevenshteinMatch_SmallTypo()
        {
            var candidates = new[] { "Addition", "Subtraction", "Multiplication" };
            Assert.Equal("Addition", FuzzyMatcher.FindBestMatch("Addtion", candidates));
        }

        [Fact]
        public void FindBestMatch_LevenshteinDisabled_ReturnsNull()
        {
            var candidates = new[] { "Addition", "Subtraction" };
            Assert.Null(FuzzyMatcher.FindBestMatch("Addtion", candidates, maxLevenshteinDistance: 0));
        }

        [Fact]
        public void FindBestMatch_LevenshteinTooFar_ReturnsNull()
        {
            var candidates = new[] { "Addition", "Subtraction" };
            Assert.Null(FuzzyMatcher.FindBestMatch("xyz", candidates, maxLevenshteinDistance: 2));
        }

        #endregion

        #region FindBestMatch — Edge Cases

        [Fact]
        public void FindBestMatch_NullInput_ReturnsNull()
        {
            Assert.Null(FuzzyMatcher.FindBestMatch(null!, new[] { "A" }));
        }

        [Fact]
        public void FindBestMatch_EmptyInput_ReturnsNull()
        {
            Assert.Null(FuzzyMatcher.FindBestMatch("", new[] { "A" }));
        }

        [Fact]
        public void FindBestMatch_NullCandidates_ReturnsNull()
        {
            Assert.Null(FuzzyMatcher.FindBestMatch("test", null!));
        }

        [Fact]
        public void FindBestMatch_EmptyCandidates_ReturnsNull()
        {
            Assert.Null(FuzzyMatcher.FindBestMatch("test", System.Array.Empty<string>()));
        }

        #endregion
    }
}
