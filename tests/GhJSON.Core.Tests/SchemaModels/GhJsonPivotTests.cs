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

using System.Drawing;
using System.Globalization;
using System.Threading;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    /// <summary>
    /// Invariants for <see cref="GhJsonPivot"/>: compact/object representations,
    /// culture-invariance, PointF conversion, and graceful handling of malformed
    /// compact strings.
    /// </summary>
    public class GhJsonPivotTests
    {
        [Fact]
        public void FromCompact_ValidString_ParsesCoordinates()
        {
            var pivot = GhJsonPivot.FromCompact("100.5,-42.25");

            Assert.NotNull(pivot);
            Assert.Equal(100.5, pivot!.X);
            Assert.Equal(-42.25, pivot.Y);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("100")]
        [InlineData("100,200,300")]
        [InlineData("abc,def")]
        public void FromCompact_Invalid_ReturnsNull(string input)
        {
            Assert.Null(GhJsonPivot.FromCompact(input));
        }

        [Fact]
        public void ToCompact_UsesInvariantCulture()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var pivot = new GhJsonPivot(1.5, 2.5);

                // Invariant culture → "." decimal separator, not "," as de-DE would use.
                Assert.Equal("1.5,2.5", pivot.ToCompact());
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void RoundTrip_CompactStringPreservesValue()
        {
            var original = new GhJsonPivot(-123.456, 789.012);

            var roundTripped = GhJsonPivot.FromCompact(original.ToCompact());

            Assert.NotNull(roundTripped);
            Assert.Equal(original.X, roundTripped!.X);
            Assert.Equal(original.Y, roundTripped.Y);
        }

        [Fact]
        public void PointFConversion_IsBidirectional()
        {
            var pivot = new GhJsonPivot(10, 20);
            var point = pivot.ToPointF();
            var back = GhJsonPivot.FromPointF(point);

            Assert.Equal(pivot.X, back.X);
            Assert.Equal(pivot.Y, back.Y);
        }

        [Fact]
        public void FromPointF_PreservesCoordinates()
        {
            var pivot = GhJsonPivot.FromPointF(new PointF(3.25f, 4.5f));

            Assert.Equal(3.25, pivot.X);
            Assert.Equal(4.5, pivot.Y);
        }
    }
}
