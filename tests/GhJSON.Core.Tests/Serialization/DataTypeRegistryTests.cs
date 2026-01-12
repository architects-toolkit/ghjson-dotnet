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
using GhJSON.Core.Serialization.DataTypes;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    public class DataTypeRegistryTests
    {
        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            var instance1 = DataTypeRegistry.Instance;
            var instance2 = DataTypeRegistry.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void GetSerializer_ByTypeName_ReturnsSerializer()
        {
            var serializer = DataTypeRegistry.Instance.GetSerializer("Color");

            Assert.NotNull(serializer);
            Assert.Equal("Color", serializer.TypeName);
        }

        [Fact]
        public void GetSerializer_ByTypeName_CaseInsensitive()
        {
            var serializer1 = DataTypeRegistry.Instance.GetSerializer("color");
            var serializer2 = DataTypeRegistry.Instance.GetSerializer("COLOR");
            var serializer3 = DataTypeRegistry.Instance.GetSerializer("Color");

            Assert.NotNull(serializer1);
            Assert.NotNull(serializer2);
            Assert.NotNull(serializer3);
            Assert.Same(serializer1, serializer2);
            Assert.Same(serializer2, serializer3);
        }

        [Fact]
        public void GetSerializer_ByType_ReturnsSerializer()
        {
            var serializer = DataTypeRegistry.Instance.GetSerializer(typeof(Color));

            Assert.NotNull(serializer);
            Assert.Equal(typeof(Color), serializer.TargetType);
        }

        [Fact]
        public void GetSerializer_UnknownTypeName_ReturnsNull()
        {
            var serializer = DataTypeRegistry.Instance.GetSerializer("UnknownType");

            Assert.Null(serializer);
        }

        [Fact]
        public void GetSerializer_NullTypeName_ReturnsNull()
        {
            var serializer = DataTypeRegistry.Instance.GetSerializer((string)null!);

            Assert.Null(serializer);
        }

        [Fact]
        public void GetSerializer_EmptyTypeName_ReturnsNull()
        {
            var serializer = DataTypeRegistry.Instance.GetSerializer("");

            Assert.Null(serializer);
        }

        [Fact]
        public void GetSerializer_NullType_ReturnsNull()
        {
            var serializer = DataTypeRegistry.Instance.GetSerializer((Type)null!);

            Assert.Null(serializer);
        }

        [Fact]
        public void IsRegistered_ByTypeName_RegisteredType_ReturnsTrue()
        {
            Assert.True(DataTypeRegistry.Instance.IsRegistered("Color"));
            Assert.True(DataTypeRegistry.Instance.IsRegistered("Text"));
            Assert.True(DataTypeRegistry.Instance.IsRegistered("Number"));
            Assert.True(DataTypeRegistry.Instance.IsRegistered("Integer"));
            Assert.True(DataTypeRegistry.Instance.IsRegistered("Boolean"));
        }

        [Fact]
        public void IsRegistered_ByTypeName_UnregisteredType_ReturnsFalse()
        {
            Assert.False(DataTypeRegistry.Instance.IsRegistered("UnknownType"));
        }

        [Fact]
        public void IsRegistered_ByTypeName_NullOrEmpty_ReturnsFalse()
        {
            Assert.False(DataTypeRegistry.Instance.IsRegistered((string)null!));
            Assert.False(DataTypeRegistry.Instance.IsRegistered(""));
            Assert.False(DataTypeRegistry.Instance.IsRegistered("   "));
        }

        [Fact]
        public void IsRegistered_ByType_RegisteredType_ReturnsTrue()
        {
            Assert.True(DataTypeRegistry.Instance.IsRegistered(typeof(Color)));
            Assert.True(DataTypeRegistry.Instance.IsRegistered(typeof(string)));
            Assert.True(DataTypeRegistry.Instance.IsRegistered(typeof(double)));
            Assert.True(DataTypeRegistry.Instance.IsRegistered(typeof(int)));
            Assert.True(DataTypeRegistry.Instance.IsRegistered(typeof(bool)));
        }

        [Fact]
        public void IsRegistered_ByType_UnregisteredType_ReturnsFalse()
        {
            Assert.False(DataTypeRegistry.Instance.IsRegistered(typeof(DateTime)));
        }

        [Fact]
        public void IsRegistered_ByType_Null_ReturnsFalse()
        {
            Assert.False(DataTypeRegistry.Instance.IsRegistered((Type)null!));
        }

        [Fact]
        public void RegisterSerializer_ValidSerializer_Succeeds()
        {
            var customSerializer = new TestCustomSerializer();

            DataTypeRegistry.Instance.RegisterSerializer(customSerializer);

            Assert.True(DataTypeRegistry.Instance.IsRegistered("TestCustomType"));
            Assert.True(DataTypeRegistry.Instance.IsRegistered(typeof(TestCustomType)));
        }

        [Fact]
        public void RegisterSerializer_NullSerializer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => DataTypeRegistry.Instance.RegisterSerializer(null!));
        }

        // Test helper types
        private class TestCustomType
        {
            public int Value { get; set; }
        }

        private class TestCustomSerializer : IDataTypeSerializer
        {
            public string TypeName => "TestCustomType";
            public Type TargetType => typeof(TestCustomType);

            public string Serialize(object value)
            {
                if (value is TestCustomType t)
                {
                    return $"custom:{t.Value}";
                }

                throw new ArgumentException("Invalid type");
            }

            public object Deserialize(string value)
            {
                var parts = value.Split(':');
                return new TestCustomType { Value = int.Parse(parts[1]) };
            }

            public bool Validate(string value)
            {
                return value.StartsWith("custom:");
            }
        }
    }
}
