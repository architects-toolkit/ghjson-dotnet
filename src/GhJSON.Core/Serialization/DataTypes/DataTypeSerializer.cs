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
using System.Diagnostics;

namespace GhJSON.Core.Serialization.DataTypes
{
    /// <summary>
    /// Facade for serializing and deserializing data types using registered serializers.
    /// </summary>
    public static class DataTypeSerializer
    {
        /// <summary>
        /// Serializes a value to a string using the appropriate serializer.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A string representation of the value, or the ToString() result if no serializer is found.</returns>
        public static string? Serialize(object? value)
        {
            if (value == null)
            {
                return null;
            }

            var serializer = DataTypeRegistry.Instance.GetSerializer(value.GetType());
            if (serializer != null)
            {
                try
                {
                    return serializer.Serialize(value);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DataTypeSerializer] Error serializing {value.GetType().Name}: {ex.Message}");
                    return value.ToString();
                }
            }

            // Fallback to ToString() for unknown types
            return value.ToString();
        }

        /// <summary>
        /// Deserializes a string value to the specified type.
        /// </summary>
        /// <param name="typeName">The type name (e.g., "Point3d", "Color").</param>
        /// <param name="value">The string value to deserialize.</param>
        /// <returns>The deserialized object, or null if deserialization fails.</returns>
        public static object? Deserialize(string typeName, string value)
        {
            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var serializer = DataTypeRegistry.Instance.GetSerializer(typeName);
            if (serializer != null)
            {
                try
                {
                    return serializer.Deserialize(value);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DataTypeSerializer] Error deserializing {typeName} from '{value}': {ex.Message}");
                    return null;
                }
            }

            Debug.WriteLine($"[DataTypeSerializer] No serializer found for type: {typeName}");
            return null;
        }

        /// <summary>
        /// Attempts to deserialize a string value to the specified type.
        /// </summary>
        /// <param name="typeName">The type name (e.g., "Point3d", "Color").</param>
        /// <param name="value">The string value to deserialize.</param>
        /// <param name="result">The deserialized object if successful.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        public static bool TryDeserialize(string typeName, string value, out object? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var serializer = DataTypeRegistry.Instance.GetSerializer(typeName);
            if (serializer == null)
            {
                return false;
            }

            if (!serializer.Validate(value))
            {
                return false;
            }

            try
            {
                result = serializer.Deserialize(value);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataTypeSerializer] Error deserializing {typeName} from '{value}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates whether a string value can be deserialized to the specified type.
        /// </summary>
        /// <param name="typeName">The type name (e.g., "Point3d", "Color").</param>
        /// <param name="value">The string value to validate.</param>
        /// <returns>True if the value is valid, false otherwise.</returns>
        public static bool Validate(string typeName, string value)
        {
            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var serializer = DataTypeRegistry.Instance.GetSerializer(typeName);
            if (serializer == null)
            {
                return false;
            }

            return serializer.Validate(value);
        }

        /// <summary>
        /// Checks if a serializer is registered for the given type name.
        /// </summary>
        /// <param name="typeName">The type name to check.</param>
        /// <returns>True if a serializer is registered, false otherwise.</returns>
        public static bool IsTypeSupported(string typeName)
        {
            return DataTypeRegistry.Instance.IsRegistered(typeName);
        }

        /// <summary>
        /// Checks if a serializer is registered for the given type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if a serializer is registered, false otherwise.</returns>
        public static bool IsTypeSupported(Type type)
        {
            return DataTypeRegistry.Instance.IsRegistered(type);
        }

        /// <summary>
        /// Attempts to deserialize a string value by automatically detecting the type prefix.
        /// </summary>
        /// <param name="value">The string value with inline type prefix (e.g., "argb:255,128,64,53").</param>
        /// <param name="result">The deserialized object if successful.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        public static bool TryDeserializeFromPrefix(string value, out object? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Check if value contains a colon separator
            int colonIndex = value.IndexOf(':');
            if (colonIndex <= 0)
            {
                return false;
            }

            // Extract the prefix (type indicator)
            string prefix = value.Substring(0, colonIndex);

            // Map known prefixes to type names
            string? typeName = MapPrefixToTypeName(prefix);
            if (typeName is null || typeName.Length == 0)
            {
                return false;
            }

            // Try to deserialize using the detected type
            return TryDeserialize(typeName, value, out result);
        }

        /// <summary>
        /// Maps a type prefix to its corresponding type name.
        /// </summary>
        /// <param name="prefix">The type prefix (e.g., "argb", "pointXYZ").</param>
        /// <returns>The type name, or null if not recognized.</returns>
        private static string? MapPrefixToTypeName(string prefix)
        {
            switch (prefix.ToLowerInvariant())
            {
                // Basic data types
                case "text":
                    return "Text";
                case "number":
                    return "Number";
                case "integer":
                    return "Integer";
                case "boolean":
                    return "Boolean";

                // Geometric data types (registered by GhJSON.Grasshopper)
                case "argb":
                    return "Color";
                case "pointxyz":
                    return "Point";
                case "vectorxyz":
                    return "Vector";
                case "line2p":
                    return "Line";
                case "planeoxy":
                    return "Plane";
                case "circlecnrs":
                    return "Circle";
                case "arc3p":
                    return "Arc";
                case "boxoxy":
                    return "Box";
                case "rectanglecxy":
                    return "Rectangle";
                case "interval":
                    return "Interval";
                case "bounds":
                    return "Bounds";
                default:
                    return null;
            }
        }
    }
}
