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
    public class ComponentNameResolverTests
    {
        #region ResolveAlias

        [Theory]
        [InlineData("slider", "Number Slider")]
        [InlineData("Slider", "Number Slider")]
        [InlineData("numberslider", "Number Slider")]
        [InlineData("numslider", "Number Slider")]
        [InlineData("panel", "Panel")]
        [InlineData("pt", "Point")]
        [InlineData("point", "Point")]
        [InlineData("crv", "Curve")]
        [InlineData("curve", "Curve")]
        [InlineData("bool", "Boolean")]
        [InlineData("toggle", "Boolean Toggle")]
        [InlineData("add", "Addition")]
        [InlineData("plus", "Addition")]
        [InlineData("sub", "Subtraction")]
        [InlineData("minus", "Subtraction")]
        [InlineData("mul", "Multiplication")]
        [InlineData("div", "Division")]
        [InlineData("cube", "Box")]
        [InlineData("rect", "Rectangle")]
        [InlineData("cyl", "Cylinder")]
        [InlineData("circ", "Circle")]
        [InlineData("xyz", "Construct Point")]
        [InlineData("vec", "Vector XYZ")]
        [InlineData("loft", "Loft")]
        [InlineData("series", "Series")]
        public void ResolveAlias_KnownAliases_ReturnsCanonicalName(string input, string expected)
        {
            Assert.Equal(expected, ComponentNameResolver.ResolveAlias(input));
        }

        [Theory]
        [InlineData("Number Slider", "Number Slider")]
        [InlineData("number_slider", "Number Slider")]
        [InlineData("number-slider", "Number Slider")]
        [InlineData("NUMBER SLIDER", "Number Slider")]
        public void ResolveAlias_NormalizedVariants_ReturnsCanonicalName(string input, string expected)
        {
            Assert.Equal(expected, ComponentNameResolver.ResolveAlias(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveAlias_NullOrEmpty_ReturnsNull(string? input)
        {
            Assert.Null(ComponentNameResolver.ResolveAlias(input!));
        }

        [Fact]
        public void ResolveAlias_UnknownName_ReturnsNull()
        {
            Assert.Null(ComponentNameResolver.ResolveAlias("SomeUnknownComponent"));
        }

        #endregion

        #region Resolve (alias + fuzzy)

        [Fact]
        public void Resolve_AliasHit_ReturnsCanonicalWithoutFuzzy()
        {
            var known = new[] { "Addition", "Subtraction", "Number Slider" };
            Assert.Equal("Number Slider", ComponentNameResolver.Resolve("slider", known));
        }

        [Fact]
        public void Resolve_NoAlias_FuzzyMatchesKnownName()
        {
            var known = new[] { "Addition", "Subtraction", "Multiplication" };
            // "Addtion" is a typo — Levenshtein distance 1 from "Addition"
            Assert.Equal("Addition", ComponentNameResolver.Resolve("Addtion", known));
        }

        [Fact]
        public void Resolve_NoAlias_ExactMatchInKnown()
        {
            var known = new[] { "Custom Component", "Another Component" };
            Assert.Equal("Custom Component", ComponentNameResolver.Resolve("Custom Component", known));
        }

        [Fact]
        public void Resolve_NoMatch_ReturnsNull()
        {
            var known = new[] { "Addition", "Subtraction" };
            Assert.Null(ComponentNameResolver.Resolve("CompletelyUnrelated", known));
        }

        #endregion
    }
}
