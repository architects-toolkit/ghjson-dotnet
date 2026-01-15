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
using System.Text.RegularExpressions;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for Number Slider component state.
    /// Serializes value, interval, and rounding settings.
    /// </summary>
    internal sealed class NumberSliderHandler : IObjectHandler
    {
        private const string ExtensionKey = "gh.numberslider";

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_NumberSlider;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Number Slider" ||
                   component.ComponentGuid == new Guid("57da07bd-ecab-415d-9d86-af36d7073abc");
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is GH_NumberSlider slider)
            {
                component.ComponentState ??= new GhJsonComponentState();
                component.ComponentState.Extensions ??= new Dictionary<string, object>();

                var sliderData = new Dictionary<string, object>();

                // Serialize value in compact format with decimals
                try
                {
                    var current = slider.CurrentValue;
                    var min = slider.Slider.Minimum;
                    var max = slider.Slider.Maximum;
                    var decimals = slider.Slider.DecimalPlaces;

                    sliderData["value"] = EncodeSliderValue(
                        current,
                        min,
                        max,
                        decimals);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NumberSliderHandler] Error serializing value: {ex.Message}");
                }

                // Serialize rounding mode if not default
                try
                {
                    var roundingMode = slider.Slider.Type.ToString();
                    if (!string.IsNullOrEmpty(roundingMode) && roundingMode != "Float")
                    {
                        sliderData["rounding"] = roundingMode;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NumberSliderHandler] Error serializing rounding: {ex.Message}");
                }

                component.ComponentState.Extensions[ExtensionKey] = sliderData;
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is GH_NumberSlider slider &&
                component.ComponentState?.Extensions != null &&
                component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData))
            {
                if (extData is Dictionary<string, object> sliderData)
                {
                    ApplySliderData(slider, sliderData);
                }
            }
        }

        private static void ApplySliderData(GH_NumberSlider slider, Dictionary<string, object> data)
        {
            // Apply rounding mode
            if (data.TryGetValue("rounding", out var roundVal))
            {
                try
                {
                    var sliderType = slider.Slider.GetType();
                    var typeProp = sliderType.GetProperty("Type");
                    if (typeProp != null && typeProp.CanWrite)
                    {
                        var enumType = typeProp.PropertyType;
                        if (Enum.TryParse(enumType, roundVal?.ToString(), true, out var accuracy))
                        {
                            typeProp.SetValue(slider.Slider, accuracy);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NumberSliderHandler] Error applying rounding: {ex.Message}");
                }
            }

            // Apply value
            if (data.TryGetValue("value", out var valueVal))
            {
                var valueStr = valueVal?.ToString();
                if (!string.IsNullOrEmpty(valueStr))
                {
                    try
                    {
                        if (TryDecodeSliderValue(valueStr, out var decoded))
                        {
                            slider.Slider.DecimalPlaces = decoded.Decimals;
                            slider.Slider.Minimum = decoded.Minimum;
                            slider.Slider.Maximum = decoded.Maximum;
                            slider.SetSliderValue(decoded.CurrentValue);
                        }
                        else if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var simpleValue))
                        {
                            // Simple numeric value only
                            slider.SetSliderValue(simpleValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[NumberSliderHandler] Error applying value '{valueStr}': {ex.Message}");
                    }
                }
            }
        }

        private static int GetDecimalPlaces(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }

            int idx = s.IndexOf('.', StringComparison.Ordinal);
            if (idx < 0)
            {
                return 0;
            }

            int end = s.IndexOfAny(new[] { 'e', 'E' }, idx + 1);
            int decimals = (end > idx ? end : s.Length) - idx - 1;
            return decimals < 0 ? 0 : decimals;
        }

        private static string EncodeSliderValue(decimal currentValue, decimal minimum, decimal maximum, int decimals)
        {
            var currentText = currentValue.ToString("F" + decimals, CultureInfo.InvariantCulture);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}<{1}~{2}>",
                currentText,
                minimum,
                maximum);
        }

        private static bool TryDecodeSliderValue(string value, out SliderValue decoded)
        {
            decoded = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var match = Regex.Match(value, @"^([\d.\-]+)<([\d.\-]+)~([\d.\-]+)>$");
            if (!match.Success)
            {
                return false;
            }

            var currentText = match.Groups[1].Value;
            var current = decimal.Parse(currentText, CultureInfo.InvariantCulture);
            var min = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var max = decimal.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var decimals = GetDecimalPlaces(currentText);

            decoded = new SliderValue(current, min, max, decimals);
            return true;
        }

        private readonly struct SliderValue
        {
            public SliderValue(decimal currentValue, decimal minimum, decimal maximum, int decimals)
            {
                this.CurrentValue = currentValue;
                this.Minimum = minimum;
                this.Maximum = maximum;
                this.Decimals = decimals;
            }

            public decimal CurrentValue { get; }

            public decimal Minimum { get; }

            public decimal Maximum { get; }

            public int Decimals { get; }
        }
    }
}
