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

using GhJSON.Core.NameResolution;
using Xunit;

namespace GhJSON.Core.Tests.NameResolution
{
    public class ParameterNameResolverTests
    {
        #region ResolveAlias

        [Theory]
        [InlineData("r", "Radius")]
        [InlineData("radius", "Radius")]
        [InlineData("pt", "Point")]
        [InlineData("point", "Point")]
        [InlineData("num", "Number")]
        [InlineData("number", "Number")]
        [InlineData("geo", "Geometry")]
        [InlineData("geometry", "Geometry")]
        [InlineData("crv", "Curve")]
        [InlineData("srf", "Surface")]
        [InlineData("dir", "Direction")]
        [InlineData("vec", "Vector")]
        [InlineData("bool", "Boolean")]
        [InlineData("a", "A")]
        [InlineData("b", "B")]
        [InlineData("result", "Result")]
        [InlineData("x", "X")]
        [InlineData("y", "Y")]
        [InlineData("z", "Z")]
        [InlineData("width", "X Size")]
        [InlineData("length", "Y Size")]
        [InlineData("height", "Z Size")]
        [InlineData("i", "Index")]
        [InlineData("index", "Index")]
        public void ResolveAlias_KnownAliases_ReturnsCanonicalName(string input, string expected)
        {
            Assert.Equal(expected, ParameterNameResolver.ResolveAlias(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveAlias_NullOrEmpty_ReturnsNull(string? input)
        {
            Assert.Null(ParameterNameResolver.ResolveAlias(input!));
        }

        [Fact]
        public void ResolveAlias_UnknownName_ReturnsNull()
        {
            Assert.Null(ParameterNameResolver.ResolveAlias("SomeUnknownParam"));
        }

        #endregion

        #region Resolve (alias + fuzzy)

        [Fact]
        public void Resolve_AliasHit_ReturnsCanonicalWithoutFuzzy()
        {
            var known = new[] { "Radius", "Plane", "Point" };
            Assert.Equal("Radius", ParameterNameResolver.Resolve("r", known));
        }

        [Fact]
        public void Resolve_NoAlias_FuzzyMatchesKnownName()
        {
            var known = new[] { "Radius", "Plane", "Point" };
            // "Radus" is a typo — Levenshtein distance 1 from "Radius"
            Assert.Equal("Radius", ParameterNameResolver.Resolve("Radus", known));
        }

        [Fact]
        public void Resolve_NoAlias_ExactMatchInKnown()
        {
            var known = new[] { "Custom Param", "Another Param" };
            Assert.Equal("Custom Param", ParameterNameResolver.Resolve("Custom Param", known));
        }

        [Fact]
        public void Resolve_NoMatch_ReturnsNull()
        {
            var known = new[] { "Radius", "Plane" };
            Assert.Null(ParameterNameResolver.Resolve("CompletelyUnrelated", known));
        }

        [Fact]
        public void Resolve_LowerMaxDistance_IsStricter()
        {
            var known = new[] { "Radius", "Plane", "Point" };
            // "Rads" has distance 2 from "Radius" (normalized) — should fail with max 1
            Assert.Null(ParameterNameResolver.Resolve("Rads", known, maxLevenshteinDistance: 1));
        }

        #endregion
    }
}
