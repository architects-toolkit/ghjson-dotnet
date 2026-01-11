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
using System.Drawing;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    public class CompactPositionTests
    {
        [Fact]
        public void Constructor_SetsXAndY()
        {
            var pos = new CompactPosition(100.5f, 200.75f);
            
            Assert.Equal(100.5f, pos.X);
            Assert.Equal(200.75f, pos.Y);
        }

        [Fact]
        public void Parse_ValidString_ReturnsCompactPosition()
        {
            var pos = CompactPosition.Parse("100.5,200.75");
            
            Assert.Equal(100.5f, pos.X);
            Assert.Equal(200.75f, pos.Y);
        }

        [Fact]
        public void Parse_NegativeValues_ReturnsCompactPosition()
        {
            var pos = CompactPosition.Parse("-50.5,-100.25");
            
            Assert.Equal(-50.5f, pos.X);
            Assert.Equal(-100.25f, pos.Y);
        }

        [Fact]
        public void Parse_InvalidFormat_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => CompactPosition.Parse("100"));
            Assert.Throws<FormatException>(() => CompactPosition.Parse("100,200,300"));
        }

        [Fact]
        public void Parse_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CompactPosition.Parse(""));
            Assert.Throws<ArgumentException>(() => CompactPosition.Parse(null!));
        }

        [Fact]
        public void ToString_ReturnsCompactFormat()
        {
            var pos = new CompactPosition(100.5f, 200.75f);
            
            Assert.Equal("100.5,200.75", pos.ToString());
        }

        [Fact]
        public void ImplicitConversion_FromPointF_Works()
        {
            PointF point = new PointF(100.5f, 200.75f);
            CompactPosition pos = point;
            
            Assert.Equal(100.5f, pos.X);
            Assert.Equal(200.75f, pos.Y);
        }

        [Fact]
        public void ImplicitConversion_ToPointF_Works()
        {
            var pos = new CompactPosition(100.5f, 200.75f);
            PointF point = pos;
            
            Assert.Equal(100.5f, point.X);
            Assert.Equal(200.75f, point.Y);
        }

        [Fact]
        public void IsEmpty_ReturnsTrueForZeroPosition()
        {
            var pos = new CompactPosition(0, 0);
            Assert.True(pos.IsEmpty);
            
            var nonEmpty = new CompactPosition(1, 0);
            Assert.False(nonEmpty.IsEmpty);
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            var pos1 = new CompactPosition(100.5f, 200.75f);
            var pos2 = new CompactPosition(100.5f, 200.75f);
            
            Assert.True(pos1.Equals(pos2));
            Assert.True(pos1 == pos2);
            Assert.False(pos1 != pos2);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesValue()
        {
            var original = new CompactPosition(100.5f, 200.75f);
            
            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<CompactPosition>(json);
            
            Assert.Equal(original.X, deserialized.X);
            Assert.Equal(original.Y, deserialized.Y);
        }

        [Fact]
        public void JsonSerialization_WritesAsString()
        {
            var pos = new CompactPosition(100.5f, 200.75f);
            
            var json = JsonConvert.SerializeObject(pos);
            
            Assert.Equal("\"100.5,200.75\"", json);
        }

        [Fact]
        public void JsonDeserialization_SupportsLegacyObjectFormat()
        {
            var json = "{\"X\": 100.5, \"Y\": 200.75}";
            
            var pos = JsonConvert.DeserializeObject<CompactPosition>(json);
            
            Assert.Equal(100.5f, pos.X);
            Assert.Equal(200.75f, pos.Y);
        }
    }
}
