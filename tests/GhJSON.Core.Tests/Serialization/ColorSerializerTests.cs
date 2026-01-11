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
using GhJSON.Core.Serialization.DataTypes.Serializers;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    public class ColorSerializerTests
    {
        private readonly ColorSerializer _serializer = new ColorSerializer();

        [Fact]
        public void TypeName_ReturnsColor()
        {
            Assert.Equal("Color", _serializer.TypeName);
        }

        [Fact]
        public void TargetType_ReturnsColorType()
        {
            Assert.Equal(typeof(Color), _serializer.TargetType);
        }

        [Theory]
        [InlineData(255, 255, 128, 64, "argb:255,255,128,64")]
        [InlineData(0, 0, 0, 0, "argb:0,0,0,0")]
        [InlineData(128, 100, 150, 200, "argb:128,100,150,200")]
        public void Serialize_ValidColor_ReturnsCorrectFormat(int a, int r, int g, int b, string expected)
        {
            var color = Color.FromArgb(a, r, g, b);

            var result = _serializer.Serialize(color);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Serialize_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _serializer.Serialize("not a color"));
            Assert.Throws<ArgumentException>(() => _serializer.Serialize(123));
        }

        [Theory]
        [InlineData("argb:255,255,128,64", 255, 255, 128, 64)]
        [InlineData("argb:0,0,0,0", 0, 0, 0, 0)]
        [InlineData("argb:128,100,150,200", 128, 100, 150, 200)]
        public void Deserialize_ValidFormat_ReturnsCorrectColor(string input, int a, int r, int g, int b)
        {
            var result = _serializer.Deserialize(input);

            Assert.IsType<Color>(result);
            var color = (Color)result;
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("rgb:255,128,64")]
        [InlineData("argb:255,128")]
        [InlineData("argb:255,128,64,32,16")]
        [InlineData("argb:256,128,64,32")]
        [InlineData("argb:-1,128,64,32")]
        [InlineData("argb:abc,128,64,32")]
        public void Deserialize_InvalidFormat_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => _serializer.Deserialize(input));
        }

        [Theory]
        [InlineData("argb:255,255,128,64", true)]
        [InlineData("argb:0,0,0,0", true)]
        [InlineData("argb:128,100,150,200", true)]
        public void Validate_ValidFormat_ReturnsTrue(string input, bool expected)
        {
            var result = _serializer.Validate(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("   ", false)]
        [InlineData("invalid", false)]
        [InlineData("rgb:255,128,64", false)]
        [InlineData("argb:255,128", false)]
        [InlineData("argb:255,128,64,32,16", false)]
        [InlineData("argb:256,128,64,32", false)]
        [InlineData("argb:-1,128,64,32", false)]
        [InlineData("argb:abc,128,64,32", false)]
        public void Validate_InvalidFormat_ReturnsFalse(string? input, bool expected)
        {
            var result = _serializer.Validate(input!);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RoundTrip_PreservesColor()
        {
            var original = Color.FromArgb(200, 150, 100, 50);

            var serialized = _serializer.Serialize(original);
            var deserialized = (Color)_serializer.Deserialize(serialized);

            Assert.Equal(original.A, deserialized.A);
            Assert.Equal(original.R, deserialized.R);
            Assert.Equal(original.G, deserialized.G);
            Assert.Equal(original.B, deserialized.B);
        }
    }
}
