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
using System.Drawing;
using System.Globalization;
using System.Linq;
using GhJSON.Core.Serialization.DataTypes;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyHandlers
{
    /// <summary>
    /// Container namespace for default specialized property handlers.
    /// </summary>
    public static class SpecializedPropertyHandlers
    {
        public class PersistentDataPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 100;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return (propertyName == "PersistentData" || propertyName == "VolatileData") && sourceObject is IGH_Param;
            }

            public override object? ExtractProperty(object sourceObject, string propertyName)
            {
                if (sourceObject is IGH_Param param)
                {
                    // IGH_Param does not expose PersistentData directly; use reflection when available
                    // and otherwise fall back to VolatileData (which, for internalized params, equals persistent).
                    IGH_Structure dataTree = null;

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
                        // ignore reflection errors and fall back to VolatileData
                    }

                    if (dataTree == null)
                    {
                        dataTree = param.VolatileData;
                    }

                    if (dataTree != null)
                    {
                        var dictionary = SchemaProperties.DataTreeConverter.IGHStructureToDictionary(dataTree);
                        return SchemaProperties.DataTreeConverter.IGHStructureDictionaryTo1DDictionary(dictionary);
                    }
                }

                return null;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                if (propertyName == "VolatileData")
                {
                    return true;
                }

                if (targetObject is IGH_DocumentObject docObj && value is JObject persistentDataObj)
                {
                    SetPersistentData(docObj, persistentDataObj);
                    return true;
                }

                return false;
            }

            private static void SetPersistentData(IGH_DocumentObject instance, JObject persistentDataDict)
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
                    case Param_Number paramNumber:
                        var pDataNumber = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? numResult) && numResult is double doubleValue)
                            {
                                return new GH_Number(doubleValue);
                            }

                            return new GH_Number(token.Value<double>());
                        });
                        paramNumber.SetPersistentData(pDataNumber);
                        break;

                    case Param_Integer paramInt:
                        var pDataInt = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? intResult) && intResult is int intValue)
                            {
                                return new GH_Integer(intValue);
                            }

                            return new GH_Integer(token.Value<int>());
                        });
                        paramInt.SetPersistentData(pDataInt);
                        break;

                    case Param_String paramString:
                        var pDataString = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            string stringValue = token.ToString();
                            if (DataTypeSerializer.TryDeserializeFromPrefix(stringValue, out object? strResult) && strResult is string deserializedStr)
                            {
                                return new GH_String(deserializedStr);
                            }

                            return new GH_String(stringValue);
                        });
                        paramString.SetPersistentData(pDataString);
                        break;

                    case Param_Boolean paramBoolean:
                        var pDataBoolean = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? boolResult) && boolResult is bool boolValue)
                            {
                                return new GH_Boolean(boolValue);
                            }

                            return new GH_Boolean(token.Value<bool>());
                        });
                        paramBoolean.SetPersistentData(pDataBoolean);
                        break;

                    case Param_Colour paramColour:
                        var pDataColour = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? colorResult) && colorResult is Color color)
                            {
                                return new GH_Colour(color);
                            }

                            throw new InvalidOperationException($"Failed to deserialize color: {token}");
                        });
                        paramColour.SetPersistentData(pDataColour);
                        break;

                    case Param_Point paramPoint:
                        var pDataPoint = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? pointResult) && pointResult is Point3d point)
                            {
                                return new GH_Point(point);
                            }

                            throw new InvalidOperationException($"Failed to deserialize point: {token}");
                        });
                        paramPoint.SetPersistentData(pDataPoint);
                        break;

                    case Param_Vector paramVector:
                        var pDataVector = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? vectorResult) && vectorResult is Vector3d vector)
                            {
                                return new GH_Vector(vector);
                            }

                            throw new InvalidOperationException($"Failed to deserialize vector: {token}");
                        });
                        paramVector.SetPersistentData(pDataVector);
                        break;

                    case Param_Line paramLine:
                        var pDataLine = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? lineResult) && lineResult is Line line)
                            {
                                return new GH_Line(line);
                            }

                            throw new InvalidOperationException($"Failed to deserialize line: {token}");
                        });
                        paramLine.SetPersistentData(pDataLine);
                        break;

                    case Param_Plane paramPlane:
                        var pDataPlane = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? planeResult) && planeResult is Plane plane)
                            {
                                return new GH_Plane(plane);
                            }

                            throw new InvalidOperationException($"Failed to deserialize plane: {token}");
                        });
                        paramPlane.SetPersistentData(pDataPlane);
                        break;

                    case Param_Arc paramArc:
                        var pDataArc = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? arcResult) && arcResult is Arc arc)
                            {
                                return new GH_Arc(arc);
                            }

                            throw new InvalidOperationException($"Failed to deserialize arc: {token}");
                        });
                        paramArc.SetPersistentData(pDataArc);
                        break;

                    case Param_Box paramBox:
                        var pDataBox = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? boxResult) && boxResult is Box box)
                            {
                                return new GH_Box(box);
                            }

                            throw new InvalidOperationException($"Failed to deserialize box: {token}");
                        });
                        paramBox.SetPersistentData(pDataBox);
                        break;

                    case Param_Circle paramCircle:
                        var pDataCircle = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? circleResult) && circleResult is Circle circle)
                            {
                                return new GH_Circle(circle);
                            }

                            throw new InvalidOperationException($"Failed to deserialize circle: {token}");
                        });
                        paramCircle.SetPersistentData(pDataCircle);
                        break;

                    case Param_Rectangle paramRectangle:
                        var pDataRectangle = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? rectangleResult) && rectangleResult is Rectangle3d rectangle)
                            {
                                return new GH_Rectangle(rectangle);
                            }

                            throw new InvalidOperationException($"Failed to deserialize rectangle: {token}");
                        });
                        paramRectangle.SetPersistentData(pDataRectangle);
                        break;

                    case Param_Interval paramInterval:
                        var pDataInterval = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, token =>
                        {
                            if (DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? intervalResult) && intervalResult is Interval interval)
                            {
                                return new GH_Interval(interval);
                            }

                            throw new InvalidOperationException($"Failed to deserialize interval: {token}");
                        });
                        paramInterval.SetPersistentData(pDataInterval);
                        break;
                }
            }
        }

        public class PanelPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 60;

            private static readonly HashSet<string> Supported = new()
            {
                "Alignment", "Multiline", "DrawIndices", "DrawPaths", "SpecialCodes"
            };

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return sourceObject is GH_Panel && Supported.Contains(propertyName);
            }

            public override object? ExtractProperty(object sourceObject, string propertyName)
            {
                if (sourceObject is GH_Panel panel)
                {
                    var props = panel.Properties;
                    return propertyName switch
                    {
                        "Alignment" => props.Alignment.ToString(),
                        "Multiline" => props.Multiline,
                        "DrawIndices" => props.DrawIndices,
                        "DrawPaths" => props.DrawPaths,
                        "SpecialCodes" => props.SpecialCodes,
                        _ => null
                    };
                }

                return null;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                if (targetObject is not GH_Panel panel)
                    return false;

                var props = panel.Properties;
                try
                {
                    switch (propertyName)
                    {
                        case "Alignment":
                            if (value != null)
                            {
                                var s = value.ToString();
                                if (Enum.TryParse(typeof(GH_Panel.Alignment), s, out var enumVal))
                                {
                                    props.Alignment = (GH_Panel.Alignment)enumVal;
                                    return true;
                                }
                            }
                            break;
                        case "Multiline":
                            props.Multiline = Convert.ToBoolean(value);
                            return true;
                        case "DrawIndices":
                            props.DrawIndices = Convert.ToBoolean(value);
                            return true;
                        case "DrawPaths":
                            props.DrawPaths = Convert.ToBoolean(value);
                            return true;
                        case "SpecialCodes":
                            props.SpecialCodes = Convert.ToBoolean(value);
                            return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PanelPropertyHandler] Failed to apply {propertyName}: {ex.Message}");
                }

                return false;
            }
        }

        public class SliderCurrentValuePropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 90;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return propertyName == "CurrentValue" && sourceObject is GH_NumberSlider;
            }

            protected override object? ProcessExtractedValue(object? value, object sourceObject, string propertyName)
            {
                if (sourceObject is GH_NumberSlider slider && value != null)
                {
                    var currentValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    var min = slider.Slider.Minimum;
                    var max = slider.Slider.Maximum;
                    var decimals = Math.Max(0, slider.Slider.DecimalPlaces);
                    var format = decimals == 0 ? "F0" : $"F{decimals}";
                    return $"{currentValue.ToString(format, CultureInfo.InvariantCulture)}<{min.ToString(CultureInfo.InvariantCulture)},{max.ToString(CultureInfo.InvariantCulture)}>";
                }

                return value;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                if (targetObject is GH_NumberSlider slider && value is string formatted)
                {
                    try
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(formatted, @"^(.+)<(.+),(.+)>$");
                        if (match.Success)
                        {
                            if (decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var current) &&
                                decimal.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var min) &&
                                decimal.TryParse(match.Groups[3].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var max))
                            {
                                var decimals = GetDecimalPlaces(match.Groups[1].Value);
                                slider.Slider.DecimalPlaces = Math.Max(0, decimals);
                                slider.Slider.Minimum = min;
                                slider.Slider.Maximum = max;
                                slider.SetSliderValue(current);
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing slider value '{formatted}': {ex.Message}");
                    }
                }

                return false;
            }

            private static int GetDecimalPlaces(string s)
            {
                if (string.IsNullOrEmpty(s)) return 0;
                var idx = s.IndexOf('.', StringComparison.Ordinal);
                if (idx < 0) return 0;
                var end = s.IndexOfAny(new[] { 'e', 'E' }, idx + 1);
                var decimals = (end > idx ? end : s.Length) - idx - 1;
                return decimals < 0 ? 0 : decimals;
            }

            public override IEnumerable<string> GetRelatedProperties(object sourceObject, string propertyName)
            {
                return new[] { "Minimum", "Maximum", "Range", "Decimals", "Rounding" };
            }
        }

        public class SliderRoundingPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 85;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return propertyName == "Rounding" && sourceObject is GH_NumberSlider;
            }

            public override object? ExtractProperty(object sourceObject, string propertyName)
            {
                try
                {
                    if (sourceObject is GH_NumberSlider slider)
                    {
                        var sliderCore = slider.Slider;
                        if (sliderCore == null) return null;

                        var coreType = sliderCore.GetType();
                        var roundingProp = coreType.GetProperty("Rounding") ?? coreType.GetProperty("Type");
                        if (roundingProp != null)
                        {
                            var enumVal = roundingProp.GetValue(sliderCore);
                            var name = enumVal?.ToString() ?? string.Empty;
                            return MapRoundingNameToCode(name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SliderRoundingPropertyHandler] Extract error: {ex.Message}");
                }

                return null;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                try
                {
                    if (targetObject is GH_NumberSlider slider && value is string code)
                    {
                        var name = MapCodeToRoundingName(code);
                        var sliderCore = slider.Slider;
                        if (sliderCore == null) return false;
                        var coreType = sliderCore.GetType();

                        var roundingProp = coreType.GetProperty("Rounding") ?? coreType.GetProperty("Type");
                        if (roundingProp != null && roundingProp.CanWrite)
                        {
                            var enumType = roundingProp.PropertyType;
                            var enumValue = Enum.GetNames(enumType)
                                .FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                            if (enumValue != null)
                            {
                                var parsed = Enum.Parse(enumType, enumValue);
                                roundingProp.SetValue(sliderCore, parsed);
                                return true;
                            }
                        }

                        var method = coreType.GetMethod($"Set{name}")
                                     ?? coreType.GetMethod($"Set{name.ToLowerInvariant().First().ToString().ToUpperInvariant()}{name.Substring(1).ToLowerInvariant()}");
                        if (method != null)
                        {
                            method.Invoke(sliderCore, null);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SliderRoundingPropertyHandler] Apply error: {ex.Message}");
                }

                return false;
            }

            private static string? MapRoundingNameToCode(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;
                switch (name.Trim().ToLowerInvariant())
                {
                    case "float": return "R";
                    case "integer": return "N";
                    case "even": return "E";
                    case "odd": return "O";
                    default: return null;
                }
            }

            private static string MapCodeToRoundingName(string code)
            {
                if (string.IsNullOrEmpty(code)) return "Float";
                switch (code.Trim().ToUpperInvariant())
                {
                    case "R": return "Float";
                    case "N": return "Integer";
                    case "E": return "Even";
                    case "O": return "Odd";
                    default: return "Float";
                }
            }
        }

        public class ExpressionPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 80;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return propertyName == "Expression" && sourceObject is IGH_Param;
            }

            public override object? ExtractProperty(object sourceObject, string propertyName)
            {
                try
                {
                    var expressionProperty = sourceObject.GetType().GetProperty("Expression");
                    if (expressionProperty != null)
                    {
                        return expressionProperty.GetValue(sourceObject) as string;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error extracting expression from parameter: {ex.Message}");
                }

                return null;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                try
                {
                    var expressionProperty = targetObject.GetType().GetProperty("Expression");
                    if (expressionProperty != null && expressionProperty.CanWrite && value is string expression)
                    {
                        expressionProperty.SetValue(targetObject, expression);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying expression to parameter: {ex.Message}");
                }

                return false;
            }
        }

        public class ColorPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 70;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                var propertyInfo = sourceObject.GetType().GetProperty(propertyName);
                return propertyInfo?.PropertyType == typeof(Color);
            }

            protected override object? ProcessValueForApplication(object? value, Type targetType, object targetObject, string propertyName)
            {
                if (value is string stringValue)
                {
                    if (DataTypeSerializer.TryDeserialize("Color", stringValue, out object? colorResult))
                    {
                        return colorResult;
                    }

                    return SchemaProperties.StringConverter.StringToColor(stringValue);
                }

                return base.ProcessValueForApplication(value, targetType, targetObject, propertyName);
            }
        }

        public class FontPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 70;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                var propertyInfo = sourceObject.GetType().GetProperty(propertyName);
                return propertyInfo?.PropertyType == typeof(Font);
            }

            protected override object? ProcessValueForApplication(object? value, Type targetType, object targetObject, string propertyName)
            {
                if (value is string stringValue)
                {
                    return SchemaProperties.StringConverter.StringToFont(stringValue);
                }

                return base.ProcessValueForApplication(value, targetType, targetObject, propertyName);
            }
        }

        public class DataMappingPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 70;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                var propertyInfo = sourceObject.GetType().GetProperty(propertyName);
                return propertyInfo?.PropertyType == typeof(GH_DataMapping);
            }

            protected override object? ProcessValueForApplication(object? value, Type targetType, object targetObject, string propertyName)
            {
                return SchemaProperties.StringConverter.StringToGHDataMapping(value ?? string.Empty);
            }
        }

        public class ValueListItemsPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 95;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return propertyName == "ListItems" && sourceObject is GH_ValueList;
            }

            public override object? ExtractProperty(object sourceObject, string propertyName)
            {
                if (sourceObject is GH_ValueList valueList)
                {
                    var simplifiedItems = new List<object>();

                    foreach (var item in valueList.ListItems)
                    {
                        simplifiedItems.Add(new
                        {
                            Name = item.Name,
                            Expression = item.Expression,
                            Selected = item.Selected
                        });
                    }

                    return simplifiedItems;
                }

                return null;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                if (targetObject is GH_ValueList valueList && value != null)
                {
                    try
                    {
                        JArray? itemsArray = null;
                        if (value is JArray directArray)
                        {
                            itemsArray = directArray;
                        }
                        else if (value is JObject listItemsObj)
                        {
                            itemsArray = listItemsObj["value"] as JArray;
                        }

                        if (itemsArray != null)
                        {
                            valueList.ListItems.Clear();

                            int firstSelectedIndex = -1;
                            int index = 0;
                            foreach (var itemToken in itemsArray)
                            {
                                var itemObj = itemToken as JObject;
                                if (itemObj != null)
                                {
                                    var name = itemObj["Name"]?.ToString();
                                    var expression = itemObj["Expression"]?.ToString();
                                    var selected = itemObj["Selected"]?.Value<bool>() ?? false;

                                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(expression))
                                    {
                                        var item = new GH_ValueListItem(name, expression);
                                        item.Selected = selected;
                                        valueList.ListItems.Add(item);
                                        if (selected && firstSelectedIndex == -1)
                                            firstSelectedIndex = index;
                                        index++;
                                    }
                                }
                            }

                            bool anySelected = valueList.ListItems.Any(it => it.Selected);
                            if (!anySelected && valueList.ListItems.Count > 0)
                            {
                                valueList.SelectItem(0);
                            }
                            else if (anySelected && valueList.ListMode != GH_ValueListMode.CheckList)
                            {
                                int idxSel = firstSelectedIndex >= 0 ? firstSelectedIndex : valueList.ListItems.FindIndex(it => it.Selected);
                                if (idxSel < 0) idxSel = 0;
                                valueList.SelectItem(idxSel);
                            }

                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ValueListItemsPropertyHandler] Error applying ListItems: {ex.Message}");
                    }
                }

                return false;
            }

            public override IEnumerable<string> GetRelatedProperties(object sourceObject, string propertyName)
            {
                return new[] { "ListMode" };
            }
        }

        public class ValueListModePropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 94;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return propertyName == "ListMode" && sourceObject is GH_ValueList;
            }

            public override object? ExtractProperty(object sourceObject, string propertyName)
            {
                if (sourceObject is GH_ValueList valueList)
                {
                    return valueList.ListMode.ToString();
                }

                return null;
            }

            public override bool ApplyProperty(object targetObject, string propertyName, object? value)
            {
                if (targetObject is GH_ValueList valueList && value != null)
                {
                    try
                    {
                        if (value is int i)
                        {
                            valueList.ListMode = (GH_ValueListMode)i;
                            return true;
                        }

                        var s = value.ToString();
                        if (Enum.TryParse(typeof(GH_ValueListMode), s, true, out var enumVal))
                        {
                            valueList.ListMode = (GH_ValueListMode)enumVal;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ValueListModePropertyHandler] Error applying ListMode: {ex.Message}");
                    }
                }

                return false;
            }
        }

        public class DefaultPropertyHandler : PropertyHandlerBase
        {
            public override int Priority => 0;

            public override bool CanHandle(object sourceObject, string propertyName)
            {
                return sourceObject.GetType().GetProperty(propertyName) != null;
            }
        }
    }
}
