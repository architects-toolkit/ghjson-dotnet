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

namespace GhJSON.Core.Serialization.DataTypes
{
    /// <summary>
    /// Interface for serializing and deserializing specific data types to/from string format.
    /// </summary>
    public interface IDataTypeSerializer
    {
        /// <summary>
        /// Gets the type name used in JSON serialization (e.g., "Point3d", "Color").
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets the target .NET type this serializer handles.
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Serializes a value to a compact string format.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A string representation of the value.</returns>
        string Serialize(object value);

        /// <summary>
        /// Deserializes a string value back to the target type.
        /// </summary>
        /// <param name="value">The string value to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(string value);

        /// <summary>
        /// Validates whether a string value can be deserialized.
        /// </summary>
        /// <param name="value">The string value to validate.</param>
        /// <returns>True if the value is valid, false otherwise.</returns>
        bool Validate(string value);
    }
}
