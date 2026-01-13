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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Serialization.DataTypes;
using GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyFilters;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Default component handler that provides basic serialization/deserialization
    /// for components without specialized handlers.
    /// </summary>
    public class DefaultComponentHandler : ComponentHandlerBase
    {
        public DefaultComponentHandler()
            : base()
        {
        }

        /// <inheritdoc/>
        public override int Priority => 0;

        /// <inheritdoc/>
        public override bool CanHandle(IGH_DocumentObject obj) => true;

        /// <inheritdoc/>
        public override ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj == null)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract Locked state for active objects
            if (obj is IGH_ActiveObject activeObj && activeObj.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract Hidden state for preview objects
            if (obj is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            if (obj is IGH_Param param)
            {
                var additional = ExtractParameterDefaults(param, includePersistentData: false);
                if (additional != null && additional.Count > 0)
                {
                    state.AdditionalProperties = additional;
                    hasState = true;
                }
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public override void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj == null || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue && obj is IGH_ActiveObject activeObj)
            {
                activeObj.Locked = state.Locked.Value;
            }

            // Apply hidden state
            if (state.Hidden.HasValue && obj is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            if (state.AdditionalProperties != null && state.AdditionalProperties.Count > 0)
            {
                ApplyAdditionalProperties(obj, state.AdditionalProperties);
            }
        }

        internal static Dictionary<string, object>? ExtractParameterDefaults(IGH_Param param, bool includePersistentData)
        {
            if (param == null)
                return null;

            var additional = new Dictionary<string, object>();

            TryAddProperty(additional, param, "DataMapping");
            TryAddProperty(additional, param, "Simplify");
            TryAddProperty(additional, param, "Reverse");
            TryAddProperty(additional, param, "Expression");
            TryAddProperty(additional, param, "Invert");
            TryAddProperty(additional, param, "Unitize");

            if (includePersistentData)
            {
                var persistentData = ExtractPersistentDataForSchemaProperties(param);
                if (persistentData != null)
                {
                    additional["PersistentData"] = persistentData;
                }
            }

            return additional.Count > 0 ? additional : null;
        }

        internal static Dictionary<string, object>? ExtractSchemaPropertiesForAdditionalProperties(
            IGH_DocumentObject obj,
            SerializationContext context,
            bool includePersistentData)
        {
            if (obj == null)
                return null;

            var filter = new PropertyFilter(context);
            var names = filter.GetAllowedProperties(obj).ToList();
            if (names.Count == 0)
                return null;

            var dict = new Dictionary<string, object>();
            foreach (var name in names)
            {
                if (!includePersistentData && (name == "PersistentData" || name == "VolatileData"))
                    continue;

                if (name == "NickName")
                    continue;

                object? value = null;
                try
                {
                    if ((name == "PersistentData" || name == "VolatileData") && obj is IGH_Param p)
                    {
                        value = ExtractPersistentDataForSchemaProperties(p);
                    }
                    else
                    {
                        var prop = obj.GetType().GetProperty(name);
                        if (prop != null && prop.CanRead)
                        {
                            value = prop.GetValue(obj);
                        }
                    }
                }
                catch
                {
                    value = null;
                }

                if (value == null)
                    continue;

                if (value is System.Drawing.Color c)
                {
                    value = DataTypeSerializer.Serialize(c);
                }

                if (value is bool b && !b)
                    continue;
                if (value is int i && i == 0)
                    continue;

                dict[name] = value;
            }

            return dict.Count > 0 ? dict : null;
        }

        private static void TryAddProperty(Dictionary<string, object> dict, object obj, string propertyName)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop == null || !prop.CanRead)
                    return;

                var value = prop.GetValue(obj);
                if (value == null)
                    return;

                if (value is bool b && !b)
                    return;

                if (value is int i && i == 0)
                    return;

                dict[propertyName] = value;
            }
            catch
            {
            }
        }

        private static void ApplyAdditionalProperties(IGH_DocumentObject obj, Dictionary<string, object> additional)
        {
            foreach (var kvp in additional)
            {
                var name = kvp.Key;
                var value = kvp.Value;

                if (string.Equals(name, "PersistentData", StringComparison.OrdinalIgnoreCase) && obj is IGH_DocumentObject doc)
                {
                    if (value is JObject jo)
                    {
                        ApplyPersistentDataForSchemaProperties(doc, jo);
                        continue;
                    }

                    if (value is JToken jt && jt.Type == JTokenType.Object)
                    {
                        ApplyPersistentDataForSchemaProperties(doc, (JObject)jt);
                        continue;
                    }

                    continue;
                }

                try
                {
                    var prop = obj.GetType().GetProperty(name);
                    if (prop == null || !prop.CanWrite)
                        continue;

                    var converted = ConvertToType(value, prop.PropertyType);
                    prop.SetValue(obj, converted);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DefaultComponentHandler] Failed to apply '{name}': {ex.Message}");
                }
            }
        }

        private static object? ConvertToType(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (value is JValue jv)
            {
                value = jv.Value;
                if (value == null)
                    return null;
            }

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType.IsEnum)
            {
                if (value is string s && Enum.TryParse(targetType, s, true, out var enumVal))
                    return enumVal;

                if (value is int i)
                    return Enum.ToObject(targetType, i);
            }

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return value;
            }
        }

        internal static object? ExtractPersistentDataForSchemaProperties(IGH_Param param)
        {
            IGH_Structure? dataTree = null;

            try
            {
                var persistentProp = param.GetType().GetProperty("PersistentData");
                if (persistentProp != null && typeof(IGH_Structure).IsAssignableFrom(persistentProp.PropertyType))
                {
                    dataTree = persistentProp.GetValue(param) as IGH_Structure;
                }
            }
            catch
            {
            }

            dataTree ??= param.VolatileData;

            if (dataTree == null)
                return null;

            var dictionary = SchemaProperties.DataTreeConverter.IGHStructureToDictionary(dataTree);
            return SchemaProperties.DataTreeConverter.IGHStructureDictionaryTo1DDictionary(dictionary);
        }

        internal static void ApplyPersistentDataForSchemaProperties(IGH_DocumentObject instance, JObject persistentDataDict)
        {
            var values = new List<JToken>();

            foreach (var path in persistentDataDict)
            {
                if (path.Value is JObject pathData)
                {
                    foreach (var item in pathData)
                    {
                        if (item.Value is JObject itemData && itemData.ContainsKey("value"))
                        {
                            values.Add(itemData["value"]!);
                        }
                        else
                        {
                            values.Add(item.Value ?? JValue.CreateNull());
                        }
                    }
                }
            }

            var arrayData = new JArray(values);

            switch (instance)
            {
                // case GH_Panel panel:
                //     ApplyPersistentDataToPanel(panel, arrayData);
                //     break;

                case Param_Number paramNumber:
                    ApplyPersistentDataToParam(paramNumber, arrayData, token =>
                    {
                        if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? numResult) && numResult is double doubleValue)
                            return new GH_Number(doubleValue);
                        return new GH_Number(token.Value<double>());
                    });
                    break;

                case Param_Integer paramInt:
                    ApplyPersistentDataToParam(paramInt, arrayData, token =>
                    {
                        if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? intResult) && intResult is int intValue)
                            return new GH_Integer(intValue);
                        return new GH_Integer(token.Value<int>());
                    });
                    break;

                case Param_String paramString:
                    ApplyPersistentDataToParam(paramString, arrayData, token =>
                    {
                        string stringValue = token.ToString();
                        if (DataTypeSerializer.TryDeserializeFromPrefix(stringValue, out object? strResult) && strResult is string deserializedStr)
                            return new GH_String(deserializedStr);
                        return new GH_String(stringValue);
                    });
                    break;

                case Param_Boolean paramBoolean:
                    ApplyPersistentDataToParam(paramBoolean, arrayData, token =>
                    {
                        if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? boolResult) && boolResult is bool boolValue)
                            return new GH_Boolean(boolValue);
                        return new GH_Boolean(token.Value<bool>());
                    });
                    break;

                case Param_Colour paramColour:
                    ApplyPersistentDataToParam(paramColour, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? colorResult) && colorResult is System.Drawing.Color color)
                            return new GH_Colour(color);
                        throw new InvalidOperationException($"Failed to deserialize color: {token}");
                    });
                    break;

                case Param_Point paramPoint:
                    ApplyPersistentDataToParam(paramPoint, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? pointResult) && pointResult is Point3d point)
                            return new GH_Point(point);
                        throw new InvalidOperationException($"Failed to deserialize point: {token}");
                    });
                    break;

                case Param_Vector paramVector:
                    ApplyPersistentDataToParam(paramVector, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? vectorResult) && vectorResult is Vector3d vector)
                            return new GH_Vector(vector);
                        throw new InvalidOperationException($"Failed to deserialize vector: {token}");
                    });
                    break;

                case Param_Line paramLine:
                    ApplyPersistentDataToParam(paramLine, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? lineResult) && lineResult is Line line)
                            return new GH_Line(line);
                        throw new InvalidOperationException($"Failed to deserialize line: {token}");
                    });
                    break;

                case Param_Plane paramPlane:
                    ApplyPersistentDataToParam(paramPlane, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? planeResult) && planeResult is Plane plane)
                            return new GH_Plane(plane);
                        throw new InvalidOperationException($"Failed to deserialize plane: {token}");
                    });
                    break;

                case Param_Arc paramArc:
                    ApplyPersistentDataToParam(paramArc, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? arcResult) && arcResult is Arc arc)
                            return new GH_Arc(arc);
                        throw new InvalidOperationException($"Failed to deserialize arc: {token}");
                    });
                    break;

                case Param_Box paramBox:
                    ApplyPersistentDataToParam(paramBox, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? boxResult) && boxResult is Box box)
                            return new GH_Box(box);
                        throw new InvalidOperationException($"Failed to deserialize box: {token}");
                    });
                    break;

                case Param_Circle paramCircle:
                    ApplyPersistentDataToParam(paramCircle, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? circleResult) && circleResult is Circle circle)
                            return new GH_Circle(circle);
                        throw new InvalidOperationException($"Failed to deserialize circle: {token}");
                    });
                    break;

                case Param_Rectangle paramRectangle:
                    ApplyPersistentDataToParam(paramRectangle, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? rectangleResult) && rectangleResult is Rectangle3d rectangle)
                            return new GH_Rectangle(rectangle);
                        throw new InvalidOperationException($"Failed to deserialize rectangle: {token}");
                    });
                    break;

                case Param_Interval paramInterval:
                    ApplyPersistentDataToParam(paramInterval, arrayData, token =>
                    {
                        if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? intervalResult) && intervalResult is Interval interval)
                            return new GH_Interval(interval);
                        throw new InvalidOperationException($"Failed to deserialize interval: {token}");
                    });
                    break;
            }
        }

        // private static void ApplyPersistentDataToPanel(GH_Panel panel, JArray arrayData)
        // {
        //     var lines = new List<string>();
        //     foreach (var token in arrayData)
        //     {
        //         if (token.Type == JTokenType.String &&
        //             DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? strResult) &&
        //             strResult is string deserializedStr)
        //         {
        //             lines.Add(deserializedStr);
        //         }
        //         else
        //         {
        //             lines.Add(token.ToString());
        //         }
        //     }

        //     if (lines.Count > 0)
        //     {
        //         panel.UserText = string.Join(Environment.NewLine, lines);
        //     }
        // }

        private static void ApplyPersistentDataToParam<T>(GH_PersistentParam<T> param, JArray arrayData, Func<JToken, T> converter)
            where T : class, IGH_Goo
        {
            var pData = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, converter);
            param.SetPersistentData(pData);
        }
    }
}
