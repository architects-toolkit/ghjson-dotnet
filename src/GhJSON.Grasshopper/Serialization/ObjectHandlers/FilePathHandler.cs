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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for the Grasshopper File Path floating parameter.
    /// Captures the file filter and expiration-on-file-event flag.
    /// The actual file path is internalized data on the output parameter and is
    /// handled by <see cref="InternalizedDataHandler"/>.
    /// </summary>
    internal sealed class FilePathHandler : IObjectHandler
    {
        private static readonly Guid FilePathGuid = new Guid("06953bda-1d37-4d58-9b38-4b3c74e54c8f");

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public string ExtensionKey => "gh.filepath";

        /// <inheritdoc/>
        Guid IObjectHandler.ComponentGuid => FilePathGuid;

        /// <inheritdoc/>
        string IObjectHandler.ComponentName => "File Path";

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.ComponentGuid == FilePathGuid)
            {
                return true;
            }

            return obj.Name == "File Path" && obj is IGH_Param;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj == null)
            {
                return;
            }

            var type = obj.GetType();
            var extensionData = new Dictionary<string, object?>();

            TryAddProperty(type, obj, "FileFilter", extensionData, "fileFilter");
            TryAddProperty(type, obj, "ExpireOnFileEvent", extensionData, "expireOnFileEvent");

            if (extensionData.Count == 0)
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();
            component.ComponentState.Extensions[this.ExtensionKey] = extensionData;
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(this.ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object?> extensionData)
            {
                return;
            }

            var type = obj.GetType();

            TrySetProperty(type, obj, "fileFilter", extensionData, "FileFilter");
            TrySetProperty(type, obj, "expireOnFileEvent", extensionData, "ExpireOnFileEvent");
        }

        private static void TryAddProperty(
            Type type,
            object instance,
            string propertyName,
            Dictionary<string, object?> target,
            string key)
        {
            try
            {
                var property = type.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanRead)
                {
                    return;
                }

                var value = property.GetValue(instance);
                if (value != null)
                {
                    target[key] = value;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[FilePathHandler] Error reading {propertyName}: {ex.Message}");
#else
                _ = ex;
#endif
            }
        }

        private static void TrySetProperty(
            Type type,
            object instance,
            string key,
            Dictionary<string, object?> source,
            string propertyName)
        {
            if (!source.TryGetValue(key, out var value) || value == null)
            {
                return;
            }

            try
            {
                var property = type.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                {
                    return;
                }

                var converted = ConvertValue(value, property.PropertyType);
                property.SetValue(instance, converted);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[FilePathHandler] Error writing {propertyName}: {ex.Message}");
#else
                _ = ex;
#endif
            }
        }

        private static object? ConvertValue(object value, Type targetType)
        {
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            var valueString = value.ToString();
            if (string.IsNullOrEmpty(valueString))
            {
                return null;
            }

            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (bool.TryParse(valueString, out var boolValue))
                {
                    return boolValue;
                }
            }

            return valueString;
        }
    }
}
