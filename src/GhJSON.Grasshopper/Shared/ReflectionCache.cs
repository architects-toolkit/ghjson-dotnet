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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace GhJSON.Grasshopper.Shared
{
    /// <summary>
    /// Provides caching for reflection operations to improve performance.
    /// PropertyInfo and MethodInfo objects are cached by type to avoid repeated lookups.
    /// </summary>
    internal static class ReflectionCache
    {
        /// <summary>
        /// Cache for PropertyInfo objects keyed by type and property name.
        /// </summary>
        private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

        /// <summary>
        /// Cache for MethodInfo objects keyed by type and method name.
        /// </summary>
        private static readonly ConcurrentDictionary<(Type Type, string MethodName), MethodInfo?> MethodCache = new();

        /// <summary>
        /// Cache for PropertyInfo arrays (all properties of a type).
        /// </summary>
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

        /// <summary>
        /// Gets a cached PropertyInfo for the specified type and property name.
        /// Uses a static dictionary to cache reflection results and avoid repeated lookups.
        /// </summary>
        /// <param name="type">The type to get the property from.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The PropertyInfo if found; otherwise, null.</returns>
        public static PropertyInfo? GetProperty(Type type, string propertyName)
        {
            var key = (type, propertyName);

            if (PropertyCache.TryGetValue(key, out var cachedProperty))
            {
                return cachedProperty;
            }

            var property = type.GetProperty(propertyName);
            PropertyCache[key] = property;
            return property;
        }

        /// <summary>
        /// Gets a cached MethodInfo for the specified type and method name.
        /// Uses a static dictionary to cache reflection results and avoid repeated lookups.
        /// </summary>
        /// <param name="type">The type to get the method from.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <returns>The MethodInfo if found; otherwise, null.</returns>
        public static MethodInfo? GetMethod(Type type, string methodName)
        {
            var key = (type, methodName);

            if (MethodCache.TryGetValue(key, out var cachedMethod))
            {
                return cachedMethod;
            }

            var method = type.GetMethod(methodName);
            MethodCache[key] = method;
            return method;
        }

        /// <summary>
        /// Gets all properties for the specified type, cached for performance.
        /// </summary>
        /// <param name="type">The type to get properties from.</param>
        /// <returns>An array of PropertyInfo objects.</returns>
        public static PropertyInfo[] GetProperties(Type type)
        {
            if (PropertiesCache.TryGetValue(type, out var cachedProperties))
            {
                return cachedProperties;
            }

            var properties = type.GetProperties();
            PropertiesCache[type] = properties;
            return properties;
        }

        /// <summary>
        /// Clears all cached reflection data.
        /// Useful for testing or memory management scenarios.
        /// </summary>
        public static void ClearCache()
        {
            PropertyCache.Clear();
            MethodCache.Clear();
            PropertiesCache.Clear();
        }
    }
}
