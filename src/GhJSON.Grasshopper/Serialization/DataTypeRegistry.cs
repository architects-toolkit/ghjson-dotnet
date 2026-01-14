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
using System.Collections.Generic;
using System.Linq;
using GhJSON.Grasshopper.Serialization.DataTypes;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Registry for data type serializers.
    /// Manages serializers for converting between Grasshopper/Rhino types and string representations.
    /// </summary>
    public static class DataTypeRegistry
    {
        private static readonly Dictionary<Type, IDataTypeSerializer> SerializersByType = new Dictionary<Type, IDataTypeSerializer>();
        private static readonly Dictionary<string, IDataTypeSerializer> SerializersByPrefix = new Dictionary<string, IDataTypeSerializer>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;

        /// <summary>
        /// Ensures the registry is initialized with built-in serializers.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (SerializersByType)
            {
                if (_initialized)
                {
                    return;
                }

                // Register built-in serializers
                RegisterBuiltInSerializers();
                _initialized = true;
            }
        }

        /// <summary>
        /// Registers a data type serializer.
        /// </summary>
        /// <param name="serializer">The serializer to register.</param>
        public static void Register(IDataTypeSerializer serializer)
        {
            lock (SerializersByType)
            {
                SerializersByType[serializer.TargetType] = serializer;
                SerializersByPrefix[serializer.Prefix] = serializer;
            }
        }

        /// <summary>
        /// Unregisters a data type serializer by type.
        /// </summary>
        /// <typeparam name="T">The target type to unregister.</typeparam>
        public static void Unregister<T>()
        {
            Unregister(typeof(T));
        }

        /// <summary>
        /// Unregisters a data type serializer by type.
        /// </summary>
        /// <param name="type">The target type to unregister.</param>
        public static void Unregister(Type type)
        {
            lock (SerializersByType)
            {
                if (SerializersByType.TryGetValue(type, out var serializer))
                {
                    SerializersByType.Remove(type);
                    SerializersByPrefix.Remove(serializer.Prefix);
                }
            }
        }

        /// <summary>
        /// Gets a serializer for the specified type.
        /// </summary>
        /// <param name="type">The type to get a serializer for.</param>
        /// <returns>The serializer, or null if not found.</returns>
        public static IDataTypeSerializer? GetSerializer(Type type)
        {
            EnsureInitialized();
            lock (SerializersByType)
            {
                return SerializersByType.TryGetValue(type, out var serializer) ? serializer : null;
            }
        }

        /// <summary>
        /// Gets a serializer for the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to get a serializer for.</param>
        /// <returns>The serializer, or null if not found.</returns>
        public static IDataTypeSerializer? GetSerializerByPrefix(string prefix)
        {
            EnsureInitialized();
            lock (SerializersByType)
            {
                return SerializersByPrefix.TryGetValue(prefix, out var serializer) ? serializer : null;
            }
        }

        /// <summary>
        /// Tries to get a serializer for a prefixed value string.
        /// </summary>
        /// <param name="value">The value string (e.g., "argb:255,0,0,0").</param>
        /// <param name="serializer">The found serializer.</param>
        /// <returns>True if a serializer was found.</returns>
        public static bool TryGetSerializerForValue(string value, out IDataTypeSerializer? serializer)
        {
            serializer = null;
            EnsureInitialized();

            var colonIndex = value.IndexOf(':');
            if (colonIndex <= 0)
            {
                return false;
            }

            var prefix = value.Substring(0, colonIndex);
            serializer = GetSerializerByPrefix(prefix);
            return serializer != null;
        }

        /// <summary>
        /// Gets all registered serializers.
        /// </summary>
        /// <returns>An enumerable of all registered serializers.</returns>
        public static IEnumerable<IDataTypeSerializer> GetAll()
        {
            EnsureInitialized();
            lock (SerializersByType)
            {
                return SerializersByType.Values.ToList();
            }
        }

        /// <summary>
        /// Serializes a value using the appropriate serializer.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized string, or null if no serializer found.</returns>
        public static string? Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            var serializer = GetSerializer(value.GetType());
            return serializer?.Serialize(value);
        }

        /// <summary>
        /// Deserializes a prefixed string value.
        /// </summary>
        /// <param name="value">The prefixed string value.</param>
        /// <returns>The deserialized object, or null if no serializer found.</returns>
        public static object? Deserialize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (!TryGetSerializerForValue(value, out var serializer) || serializer == null)
            {
                return null;
            }

            return serializer.Deserialize(value);
        }

        private static void RegisterBuiltInSerializers()
        {
            // Basic types
            Register(new TextSerializer());
            Register(new NumberSerializer());
            Register(new IntegerSerializer());
            Register(new BooleanSerializer());

            // Geometric types
            Register(new ColorSerializer());
            Register(new Point3dSerializer());
            Register(new Vector3dSerializer());
            Register(new LineSerializer());
            Register(new PlaneSerializer());
            Register(new CircleSerializer());
            Register(new ArcSerializer());
            Register(new BoxSerializer());
            Register(new RectangleSerializer());
            Register(new IntervalSerializer());
            Register(new BoundsSerializer());
        }
    }
}
