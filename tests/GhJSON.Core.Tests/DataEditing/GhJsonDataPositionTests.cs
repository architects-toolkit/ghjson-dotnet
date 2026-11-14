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

using GhJSON.Core.DataEditing;
using Xunit;

namespace GhJSON.Core.Tests.DataEditing
{
    /// <summary>
    /// Tests for <see cref="GhJsonDataPosition.TryParse"/>.
    /// </summary>
    public class GhJsonDataPositionTests
    {
        [Theory]
        [InlineData("{0}", new[] { 0 }, null)]
        [InlineData("{0;0;1}", new[] { 0, 0, 1 }, null)]
        [InlineData("{2;14;3}", new[] { 2, 14, 3 }, null)]
        [InlineData("{0;0;1}(3)", new[] { 0, 0, 1 }, 3)]
        [InlineData("{0}(0)", new[] { 0 }, 0)]
        [InlineData("{5}(12)", new[] { 5 }, 12)]
        [InlineData("(3)", new[] { 0 }, 3)]
        [InlineData(" {0;1} (2) ", new[] { 0, 1 }, 2)]
        [InlineData("{0;1}(0)", new[] { 0, 1 }, 0)]
        public void TryParse_ValidPositions_Succeeds(string input, int[] expectedPath, int? expectedItem)
        {
            var ok = GhJsonDataPosition.TryParse(input, out var position, out var error);

            Assert.True(ok, $"Expected '{input}' to parse, got error: {error}");
            Assert.Equal(expectedPath, position.Path);
            Assert.Equal(expectedItem, position.ItemIndex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{}")]
        [InlineData("{0;1}(")]
        [InlineData("{0;1}()")]
        [InlineData("{0;1}(2x)")]
        [InlineData("{0;1}(-2)")]
        [InlineData("(-1)")]
        [InlineData("{-1}(0)")]
        [InlineData("{0;a}(1)")]
        [InlineData("{0;}(1)")]
        [InlineData("0;1")]
        [InlineData("0}(1)")]
        [InlineData("{0;1}(1)(2)")]
        [InlineData("(x)")]
        [InlineData("{0;1}extra(1)")]
        public void TryParse_InvalidPositions_FailsWithDefaults(string? input)
        {
            var ok = GhJsonDataPosition.TryParse(input, out var position, out var error);

            Assert.False(ok);
            Assert.NotEmpty(error);
            Assert.False(position.IsItem);
            Assert.Null(position.ItemIndex);
            Assert.Null(position.Path);
        }

        [Fact]
        public void TryParse_ValidPositions_RoundTrip()
        {
            const string input = "{0;0;1}(3)";
            Assert.True(GhJsonDataPosition.TryParse(input, out var position, out _));
            Assert.Equal(input, position.ToString());
        }

        [Fact]
        public void Equals_SamePosition_ReturnsTrue()
        {
            var a = new GhJsonDataPosition(new[] { 0, 1 }, 2);
            var b = new GhJsonDataPosition(new[] { 0, 1 }, 2);
            var c = new GhJsonDataPosition(new[] { 0, 1 }, 3);
            var d = new GhJsonDataPosition(new[] { 0, 1 }, null);

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.False(a.Equals(c));
            Assert.False(a.Equals(d));
        }
    }
}
