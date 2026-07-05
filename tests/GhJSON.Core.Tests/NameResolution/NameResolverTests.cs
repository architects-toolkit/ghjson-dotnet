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
    public class NameResolverTests
    {
        #region ResolveComponentAlias

        [Theory]
        [InlineData("slider", "Number Slider")]
        [InlineData("panel", "Panel")]
        [InlineData("add", "Addition")]
        public void ResolveComponentAlias_DelegatesToComponentNameResolver(string input, string expected)
        {
            Assert.Equal(expected, NameResolver.ResolveComponentAlias(input));
        }

        [Fact]
        public void ResolveComponentAlias_Unknown_ReturnsNull()
        {
            Assert.Null(NameResolver.ResolveComponentAlias("SomeUnknownComponent"));
        }

        #endregion

        #region ResolveComponentName

        [Fact]
        public void ResolveComponentName_WithKnownAlias_SkipsFuzzy()
        {
            var known = new[] { "Addition", "Subtraction", "Number Slider" };
            Assert.Equal("Number Slider", NameResolver.ResolveComponentName("slider", known));
        }

        [Fact]
        public void ResolveComponentName_WithoutAlias_UsesFuzzy()
        {
            var known = new[] { "Addition", "Subtraction", "Multiplication" };
            // "Addtion" is a typo — Levenshtein distance 1 from "Addition"
            Assert.Equal("Addition", NameResolver.ResolveComponentName("Addtion", known));
        }

        [Fact]
        public void ResolveComponentName_ExactMatchInKnown_ReturnsMatch()
        {
            var known = new[] { "Custom Component", "Another Component" };
            Assert.Equal("Custom Component", NameResolver.ResolveComponentName("Custom Component", known));
        }

        [Fact]
        public void ResolveComponentName_NoMatch_ReturnsNull()
        {
            var known = new[] { "Addition", "Subtraction" };
            Assert.Null(NameResolver.ResolveComponentName("CompletelyUnrelated", known));
        }

        [Fact]
        public void ResolveComponentName_CustomMaxDistance_IsRespected()
        {
            var known = new[] { "Addition", "Subtraction" };
            // "Add" has distance > 0 from "Addition", but with a larger max distance it should match
            Assert.NotNull(NameResolver.ResolveComponentName("Add", known, maxLevenshteinDistance: 5));
        }

        #endregion

        #region ResolveParameterAlias

        [Theory]
        [InlineData("r", "Radius")]
        [InlineData("radius", "Radius")]
        [InlineData("num", "Number")]
        public void ResolveParameterAlias_DelegatesToParameterNameResolver(string input, string expected)
        {
            Assert.Equal(expected, NameResolver.ResolveParameterAlias(input));
        }

        [Fact]
        public void ResolveParameterAlias_Unknown_ReturnsNull()
        {
            Assert.Null(NameResolver.ResolveParameterAlias("SomeUnknownParam"));
        }

        #endregion

        #region ResolveParameterName

        [Fact]
        public void ResolveParameterName_WithKnownAlias_SkipsFuzzy()
        {
            var known = new[] { "Radius", "Plane", "Point" };
            Assert.Equal("Radius", NameResolver.ResolveParameterName("r", known));
        }

        [Fact]
        public void ResolveParameterName_WithoutAlias_UsesFuzzy()
        {
            var known = new[] { "Radius", "Plane", "Point" };
            // "Radus" is a typo — Levenshtein distance 1 from "Radius"
            Assert.Equal("Radius", NameResolver.ResolveParameterName("Radus", known));
        }

        [Fact]
        public void ResolveParameterName_ExactMatchInKnown_ReturnsMatch()
        {
            var known = new[] { "Custom Param", "Another Param" };
            Assert.Equal("Custom Param", NameResolver.ResolveParameterName("Custom Param", known));
        }

        [Fact]
        public void ResolveParameterName_NoMatch_ReturnsNull()
        {
            var known = new[] { "Radius", "Plane" };
            Assert.Null(NameResolver.ResolveParameterName("CompletelyUnrelated", known));
        }

        [Fact]
        public void ResolveParameterName_CustomMaxDistance_IsRespected()
        {
            var known = new[] { "Radius", "Plane", "Point" };
            // "Rads" has distance 2 from "Radius" — should fail with max 1
            Assert.Null(NameResolver.ResolveParameterName("Rads", known, maxLevenshteinDistance: 1));
        }

        #endregion
    }
}
