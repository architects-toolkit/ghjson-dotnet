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
using GhJSON.Core.Serialization.DataTypes.Serializers;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    public class TextSerializerTests
    {
        private readonly TextSerializer _serializer = new TextSerializer();

        [Fact]
        public void TypeName_ReturnsText()
        {
            Assert.Equal("Text", _serializer.TypeName);
        }

        [Fact]
        public void TargetType_ReturnsStringType()
        {
            Assert.Equal(typeof(string), _serializer.TargetType);
        }

        [Theory]
        [InlineData("Hello World", "text:Hello World")]
        [InlineData("", "text:")]
        [InlineData("Special chars: !@#$%", "text:Special chars: !@#$%")]
        public void Serialize_ValidString_ReturnsCorrectFormat(string input, string expected)
        {
            var result = _serializer.Serialize(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("text:Hello World", "Hello World")]
        [InlineData("text:", "")]
        [InlineData("text:Special chars: !@#$%", "Special chars: !@#$%")]
        public void Deserialize_ValidFormat_ReturnsCorrectString(string input, string expected)
        {
            var result = _serializer.Deserialize(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Validate_ValidFormat_ReturnsTrue()
        {
            Assert.True(_serializer.Validate("text:Hello"));
            Assert.True(_serializer.Validate("text:"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("Hello")]
        [InlineData("txt:Hello")]
        public void Validate_InvalidFormat_ReturnsFalse(string? input)
        {
            Assert.False(_serializer.Validate(input!));
        }
    }

    public class NumberSerializerTests
    {
        private readonly NumberSerializer _serializer = new NumberSerializer();

        [Fact]
        public void TypeName_ReturnsNumber()
        {
            Assert.Equal("Number", _serializer.TypeName);
        }

        [Fact]
        public void TargetType_ReturnsDoubleType()
        {
            Assert.Equal(typeof(double), _serializer.TargetType);
        }

        [Theory]
        [InlineData(3.14159, "number:3.14159")]
        [InlineData(0.0, "number:0")]
        [InlineData(-42.5, "number:-42.5")]
        [InlineData(1000000.0, "number:1000000")]
        public void Serialize_ValidNumber_ReturnsCorrectFormat(double input, string expected)
        {
            var result = _serializer.Serialize(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("number:3.14159", 3.14159)]
        [InlineData("number:0", 0.0)]
        [InlineData("number:-42.5", -42.5)]
        public void Deserialize_ValidFormat_ReturnsCorrectNumber(string input, double expected)
        {
            var result = _serializer.Deserialize(input);

            Assert.IsType<double>(result);
            Assert.Equal(expected, (double)result, 5);
        }

        [Theory]
        [InlineData("number:3.14", true)]
        [InlineData("number:0", true)]
        [InlineData("number:-42.5", true)]
        public void Validate_ValidFormat_ReturnsTrue(string input, bool expected)
        {
            Assert.Equal(expected, _serializer.Validate(input));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("3.14", false)]
        [InlineData("num:3.14", false)]
        [InlineData("number:abc", false)]
        public void Validate_InvalidFormat_ReturnsFalse(string? input, bool expected)
        {
            Assert.Equal(expected, _serializer.Validate(input!));
        }
    }

    public class IntegerSerializerTests
    {
        private readonly IntegerSerializer _serializer = new IntegerSerializer();

        [Fact]
        public void TypeName_ReturnsInteger()
        {
            Assert.Equal("Integer", _serializer.TypeName);
        }

        [Fact]
        public void TargetType_ReturnsIntType()
        {
            Assert.Equal(typeof(int), _serializer.TargetType);
        }

        [Theory]
        [InlineData(42, "integer:42")]
        [InlineData(0, "integer:0")]
        [InlineData(-100, "integer:-100")]
        public void Serialize_ValidInteger_ReturnsCorrectFormat(int input, string expected)
        {
            var result = _serializer.Serialize(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("integer:42", 42)]
        [InlineData("integer:0", 0)]
        [InlineData("integer:-100", -100)]
        public void Deserialize_ValidFormat_ReturnsCorrectInteger(string input, int expected)
        {
            var result = _serializer.Deserialize(input);

            Assert.IsType<int>(result);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("integer:42", true)]
        [InlineData("integer:0", true)]
        [InlineData("integer:-100", true)]
        public void Validate_ValidFormat_ReturnsTrue(string input, bool expected)
        {
            Assert.Equal(expected, _serializer.Validate(input));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("42", false)]
        [InlineData("int:42", false)]
        [InlineData("integer:3.14", false)]
        [InlineData("integer:abc", false)]
        public void Validate_InvalidFormat_ReturnsFalse(string? input, bool expected)
        {
            Assert.Equal(expected, _serializer.Validate(input!));
        }
    }

    public class BooleanSerializerTests
    {
        private readonly BooleanSerializer _serializer = new BooleanSerializer();

        [Fact]
        public void TypeName_ReturnsBoolean()
        {
            Assert.Equal("Boolean", _serializer.TypeName);
        }

        [Fact]
        public void TargetType_ReturnsBoolType()
        {
            Assert.Equal(typeof(bool), _serializer.TargetType);
        }

        [Theory]
        [InlineData(true, "boolean:true")]
        [InlineData(false, "boolean:false")]
        public void Serialize_ValidBoolean_ReturnsCorrectFormat(bool input, string expected)
        {
            var result = _serializer.Serialize(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("boolean:true", true)]
        [InlineData("boolean:false", false)]
        public void Deserialize_ValidFormat_ReturnsCorrectBoolean(string input, bool expected)
        {
            var result = _serializer.Deserialize(input);

            Assert.IsType<bool>(result);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("boolean:true", true)]
        [InlineData("boolean:false", true)]
        public void Validate_ValidFormat_ReturnsTrue(string input, bool expected)
        {
            Assert.Equal(expected, _serializer.Validate(input));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("true", false)]
        [InlineData("bool:true", false)]
        [InlineData("boolean:1", false)]
        [InlineData("boolean:yes", false)]
        public void Validate_InvalidFormat_ReturnsFalse(string? input, bool expected)
        {
            Assert.Equal(expected, _serializer.Validate(input!));
        }
    }
}
