/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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

namespace GhJSON.Grasshopper.Shared
{
    /// <summary>
    /// Converts deserialized GhJSON values into a target goo type: exact-type
    /// constructor, string constructor, then parameterless constructor plus a
    /// writable <c>Value</c> property. A missing or rejecting <c>Value</c>
    /// setter yields null rather than a default-valued goo, so an incompatible
    /// assignment can never silently succeed.
    /// </summary>
    internal static class GooFactory
    {
        /// <summary>
        /// Creates a goo instance of <paramref name="gooType"/> carrying
        /// <paramref name="value"/>, or null when no conversion path exists.
        /// </summary>
        /// <param name="gooType">The goo type to construct.</param>
        /// <param name="value">The deserialized value.</param>
        /// <returns>The created goo, or null.</returns>
        internal static object? Create(Type gooType, object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                // Try the (object) constructor first
                var constructor = gooType.GetConstructor(new[] { value.GetType() });
                if (constructor != null)
                {
                    return constructor.Invoke(new[] { value });
                }

                // Try a string constructor for text values
                if (value is string strValue)
                {
                    constructor = gooType.GetConstructor(new[] { typeof(string) });
                    if (constructor != null)
                    {
                        return constructor.Invoke(new object?[] { strValue });
                    }
                }

                // Try parameterless constructor and set Value property
                constructor = gooType.GetConstructor(Type.EmptyTypes);
                var valueProperty = gooType.GetProperty("Value");
                if (constructor != null && valueProperty != null && valueProperty.CanWrite)
                {
                    var goo = constructor.Invoke(Array.Empty<object>());
                    valueProperty.SetValue(goo, value);
                    return goo;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GooFactory.Create] Error creating {gooType.Name}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Finds the concrete closed generic base type of <paramref name="type"/>
        /// matching <paramref name="openGenericBaseType"/>, walking the
        /// inheritance chain.
        /// </summary>
        /// <param name="type">The concrete type to inspect.</param>
        /// <param name="openGenericBaseType">The open generic base type (e.g. <c>GH_PersistentParam&lt;&gt;</c>).</param>
        /// <returns>The closed base type, or null.</returns>
        internal static Type? FindGenericBaseType(Type type, Type openGenericBaseType)
        {
            var current = type;
            while (current != null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBaseType)
                {
                    return current;
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}
