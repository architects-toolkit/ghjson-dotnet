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

using System.Drawing;
using GhJSON.Core.Serialization.DataTypes;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    public class DataTypeSerializerTests
    {
        [Fact]
        public void Serialize_NullValue_ReturnsNull()
        {
            var result = DataTypeSerializer.Serialize(null);

            Assert.Null(result);
        }

        [Fact]
        public void Serialize_Color_ReturnsArgbFormat()
        {
            var color = Color.FromArgb(255, 128, 64, 32);

            var result = DataTypeSerializer.Serialize(color);

            Assert.Equal("argb:255,128,64,32", result);
        }

        [Fact]
        public void Serialize_UnregisteredType_ReturnsToString()
        {
            var value = new { X = 1, Y = 2 }; // Anonymous type, not registered

            var result = DataTypeSerializer.Serialize(value);

            Assert.NotNull(result);
            Assert.Contains("X", result);
            Assert.Contains("Y", result);
        }

        [Fact]
        public void Deserialize_ValidTypeName_ReturnsCorrectType()
        {
            var result = DataTypeSerializer.Deserialize("Color", "argb:255,128,64,32");

            Assert.IsType<Color>(result);
            var color = (Color)result!;
            Assert.Equal(255, color.A);
            Assert.Equal(128, color.R);
            Assert.Equal(64, color.G);
            Assert.Equal(32, color.B);
        }

        [Fact]
        public void Deserialize_InvalidTypeName_ReturnsNull()
        {
            var result = DataTypeSerializer.Deserialize("UnknownType", "somevalue");

            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_EmptyTypeName_ReturnsNull()
        {
            var result = DataTypeSerializer.Deserialize("", "somevalue");

            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_EmptyValue_ReturnsNull()
        {
            var result = DataTypeSerializer.Deserialize("Color", "");

            Assert.Null(result);
        }

        [Fact]
        public void TryDeserialize_ValidInput_ReturnsTrue()
        {
            var success = DataTypeSerializer.TryDeserialize("Color", "argb:255,128,64,32", out var result);

            Assert.True(success);
            Assert.NotNull(result);
            Assert.IsType<Color>(result);
        }

        [Fact]
        public void TryDeserialize_InvalidInput_ReturnsFalse()
        {
            var success = DataTypeSerializer.TryDeserialize("Color", "invalid", out var result);

            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void Validate_ValidInput_ReturnsTrue()
        {
            var result = DataTypeSerializer.Validate("Color", "argb:255,128,64,32");

            Assert.True(result);
        }

        [Fact]
        public void Validate_InvalidInput_ReturnsFalse()
        {
            var result = DataTypeSerializer.Validate("Color", "invalid");

            Assert.False(result);
        }

        [Fact]
        public void Validate_UnknownType_ReturnsFalse()
        {
            var result = DataTypeSerializer.Validate("UnknownType", "somevalue");

            Assert.False(result);
        }

        [Fact]
        public void IsTypeSupported_RegisteredType_ReturnsTrue()
        {
            Assert.True(DataTypeSerializer.IsTypeSupported("Color"));
            Assert.True(DataTypeSerializer.IsTypeSupported("Text"));
            Assert.True(DataTypeSerializer.IsTypeSupported("Number"));
            Assert.True(DataTypeSerializer.IsTypeSupported("Integer"));
            Assert.True(DataTypeSerializer.IsTypeSupported("Boolean"));
        }

        [Fact]
        public void IsTypeSupported_UnregisteredType_ReturnsFalse()
        {
            Assert.False(DataTypeSerializer.IsTypeSupported("UnknownType"));
        }

        [Fact]
        public void IsTypeSupported_ByType_Color_ReturnsTrue()
        {
            Assert.True(DataTypeSerializer.IsTypeSupported(typeof(Color)));
        }

        [Fact]
        public void TryDeserializeFromPrefix_ValidColorPrefix_ReturnsTrue()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("argb:255,128,64,32", out var result);

            Assert.True(success);
            Assert.IsType<Color>(result);
        }

        [Fact]
        public void TryDeserializeFromPrefix_ValidTextPrefix_ReturnsTrue()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("text:Hello World", out var result);

            Assert.True(success);
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void TryDeserializeFromPrefix_ValidNumberPrefix_ReturnsTrue()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("number:3.14159", out var result);

            Assert.True(success);
            Assert.IsType<double>(result);
            Assert.Equal(3.14159, (double)result!, 5);
        }

        [Fact]
        public void TryDeserializeFromPrefix_ValidIntegerPrefix_ReturnsTrue()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("integer:42", out var result);

            Assert.True(success);
            Assert.IsType<int>(result);
            Assert.Equal(42, result);
        }

        [Fact]
        public void TryDeserializeFromPrefix_ValidBooleanPrefix_ReturnsTrue()
        {
            var success1 = DataTypeSerializer.TryDeserializeFromPrefix("boolean:true", out var result1);
            var success2 = DataTypeSerializer.TryDeserializeFromPrefix("boolean:false", out var result2);

            Assert.True(success1);
            Assert.True((bool)result1!);
            Assert.True(success2);
            Assert.False((bool)result2!);
        }

        [Fact]
        public void TryDeserializeFromPrefix_NoPrefix_ReturnsFalse()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("Hello World", out var result);

            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void TryDeserializeFromPrefix_UnknownPrefix_ReturnsFalse()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("unknown:somevalue", out var result);

            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void TryDeserializeFromPrefix_EmptyValue_ReturnsFalse()
        {
            var success = DataTypeSerializer.TryDeserializeFromPrefix("", out var result);

            Assert.False(success);
            Assert.Null(result);
        }
    }
}
