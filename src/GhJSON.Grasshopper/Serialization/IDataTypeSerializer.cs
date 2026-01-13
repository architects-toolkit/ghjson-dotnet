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

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Interface for data type serializers that convert between Grasshopper/Rhino types
    /// and their string representations using the prefix:value format.
    /// </summary>
    public interface IDataTypeSerializer
    {
        /// <summary>
        /// Gets the friendly name of the type (e.g., "Color", "Point3d").
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets the target .NET type this serializer handles.
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Gets the prefix used in serialization (e.g., "argb", "pointXYZ").
        /// </summary>
        string Prefix { get; }

        /// <summary>
        /// Serializes a value to its string representation.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized string in prefix:value format.</returns>
        string Serialize(object value);

        /// <summary>
        /// Deserializes a string to the target type.
        /// </summary>
        /// <param name="value">The string value to deserialize (including prefix).</param>
        /// <returns>The deserialized value.</returns>
        object Deserialize(string value);

        /// <summary>
        /// Checks if a string value is valid for this serializer.
        /// </summary>
        /// <param name="value">The string value to check.</param>
        /// <returns>True if the value can be deserialized by this serializer.</returns>
        bool IsValid(string value);
    }

    /// <summary>
    /// Generic interface for strongly-typed data type serializers.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    public interface IDataTypeSerializer<T> : IDataTypeSerializer
    {
        /// <summary>
        /// Serializes a value to its string representation.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized string in prefix:value format.</returns>
        string Serialize(T value);

        /// <summary>
        /// Deserializes a string to the target type.
        /// </summary>
        /// <param name="value">The string value to deserialize (including prefix).</param>
        /// <returns>The deserialized value.</returns>
        new T Deserialize(string value);
    }
}
