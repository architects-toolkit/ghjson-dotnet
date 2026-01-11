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
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyHandlers
{
    /// <summary>
    /// Defines the contract for handling property extraction and application
    /// for specific object types or property categories.
    /// </summary>
    public interface IPropertyHandler
    {
        /// <summary>
        /// Gets the priority of this handler. Higher priority handlers are tried first.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if this handler can process the given object and property.
        /// </summary>
        bool CanHandle(object sourceObject, string propertyName);

        /// <summary>
        /// Extracts the property value from the source object.
        /// </summary>
        object? ExtractProperty(object sourceObject, string propertyName);

        /// <summary>
        /// Applies the property value to the target object.
        /// </summary>
        bool ApplyProperty(object targetObject, string propertyName, object? value);

        /// <summary>
        /// Gets additional properties that should be extracted when this property is encountered.
        /// </summary>
        IEnumerable<string> GetRelatedProperties(object sourceObject, string propertyName);
    }

    /// <summary>
    /// Base implementation of <see cref="IPropertyHandler"/> with common functionality.
    /// </summary>
    public abstract class PropertyHandlerBase : IPropertyHandler
    {
        public abstract int Priority { get; }

        public abstract bool CanHandle(object sourceObject, string propertyName);

        public virtual object? ExtractProperty(object sourceObject, string propertyName)
        {
            try
            {
                var propertyInfo = sourceObject.GetType().GetProperty(propertyName);
                if (propertyInfo == null)
                {
                    return null;
                }

                var value = propertyInfo.GetValue(sourceObject);
                return ProcessExtractedValue(value, sourceObject, propertyName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting property {propertyName}: {ex.Message}");
                return null;
            }
        }

        public virtual bool ApplyProperty(object targetObject, string propertyName, object? value)
        {
            try
            {
                var propertyInfo = targetObject.GetType().GetProperty(propertyName);
                if (propertyInfo == null || !propertyInfo.CanWrite)
                {
                    return false;
                }

                var processedValue = ProcessValueForApplication(value, propertyInfo.PropertyType, targetObject, propertyName);
                propertyInfo.SetValue(targetObject, processedValue);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying property {propertyName}: {ex.Message}");
                return false;
            }
        }

        public virtual IEnumerable<string> GetRelatedProperties(object sourceObject, string propertyName)
        {
            return Array.Empty<string>();
        }

        protected virtual object? ProcessExtractedValue(object? value, object sourceObject, string propertyName)
        {
            return value;
        }

        protected virtual object? ProcessValueForApplication(object? value, Type targetType, object targetObject, string propertyName)
        {
            return ConvertValue(value, targetType);
        }

        protected static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null || (value is JValue jValue && jValue.Value == null))
            {
                return null;
            }

            if (value is JValue jVal)
            {
                value = jVal.Value;
            }

            if (value is JObject jObj)
            {
                return jObj.ToObject(targetType);
            }

            if (value != null && targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            }

            try
            {
                if (targetType == typeof(string))
                    return value?.ToString();

                if (targetType == typeof(int))
                    return Convert.ToInt32(value);

                if (targetType == typeof(double))
                    return Convert.ToDouble(value);

                if (targetType == typeof(float))
                    return Convert.ToSingle(value);

                if (targetType == typeof(bool))
                    return Convert.ToBoolean(value);

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value?.ToString() ?? string.Empty);

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }
    }
}
