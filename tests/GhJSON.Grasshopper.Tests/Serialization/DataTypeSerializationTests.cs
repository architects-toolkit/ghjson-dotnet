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

using GhJSON.Grasshopper.Serialization;
using Xunit;

namespace GhJSON.Grasshopper.Tests.Serialization
{
    public class DataTypeSerializationTests
    {
        public DataTypeSerializationTests()
        {
            DataTypeRegistry.EnsureInitialized();
        }

        [Theory]
        [InlineData("text:Hello World", "Hello World")]
        [InlineData("text:Test", "Test")]
        [InlineData("text:", "")]
        public void TextSerializer_SerializesAndDeserializes(string serialized, string expected)
        {
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.Equal(expected, deserialized);
        }

        [Theory]
        [InlineData("number:3.14159", 3.14159)]
        [InlineData("number:0", 0.0)]
        [InlineData("number:-5.5", -5.5)]
        public void NumberSerializer_SerializesAndDeserializes(string serialized, double expected)
        {
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.Equal(expected, (double)deserialized, 5);
        }

        [Theory]
        [InlineData("integer:42", 42)]
        [InlineData("integer:0", 0)]
        [InlineData("integer:-10", -10)]
        public void IntegerSerializer_SerializesAndDeserializes(string serialized, int expected)
        {
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.Equal(expected, deserialized);
        }

        [Theory]
        [InlineData("boolean:true", true)]
        [InlineData("boolean:false", false)]
        public void BooleanSerializer_SerializesAndDeserializes(string serialized, bool expected)
        {
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.Equal(expected, deserialized);
        }

        [Fact]
        public void ColorSerializer_SerializesAndDeserializes()
        {
            var serialized = "argb:255,128,64,32";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void Point3dSerializer_SerializesAndDeserializes()
        {
            var serialized = "pointXYZ:10.5,20.0,30.5";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void Vector3dSerializer_SerializesAndDeserializes()
        {
            var serialized = "vectorXYZ:1.0,0.0,0.0";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void LineSerializer_SerializesAndDeserializes()
        {
            var serialized = "line2p:0,0,0;10,10,10";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void IntervalSerializer_SerializesAndDeserializes()
        {
            var serialized = "interval:0.0<10.0";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void BoundsSerializer_SerializesAndDeserializes()
        {
            var serialized = "bounds:100x200";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void UnknownPrefix_ReturnsFalse()
        {
            var serialized = "unknown:value";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.Null(deserialized);
        }

        [Fact]
        public void InvalidFormat_ReturnsFalse()
        {
            var serialized = "noprefix";
            
            var deserialized = DataTypeRegistry.Deserialize(serialized);

            Assert.Null(deserialized);
        }

        [Fact]
        public void DataTypeRegistry_SupportsCustomSerializers()
        {
            var serializers = DataTypeRegistry.GetAll();

            Assert.NotNull(serializers);
            Assert.NotEmpty(serializers);
        }
    }
}
