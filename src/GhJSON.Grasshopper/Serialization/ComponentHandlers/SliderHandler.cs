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
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for GH_NumberSlider components.
    /// Serializes slider value, minimum, and maximum in compact format.
    /// </summary>
    public class SliderHandler : IComponentHandler
    {
        /// <summary>
        /// Known GUID for GH_NumberSlider component.
        /// </summary>
        public static readonly Guid SliderGuid = new Guid("57da07bd-ecab-415d-9d86-af36d7073abc");

        /// <inheritdoc/>
        public IEnumerable<Guid> SupportedComponentGuids => new[] { SliderGuid };

        /// <inheritdoc/>
        public IEnumerable<Type> SupportedTypes => new[] { typeof(GH_NumberSlider) };

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj) => obj is GH_NumberSlider;

        /// <inheritdoc/>
        public ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_NumberSlider slider)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value
            var value = ExtractValue(obj);
            if (value != null)
            {
                state.Value = value;
                hasState = true;
            }

            // Extract locked state
            if (slider.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state (via IGH_PreviewObject)
            if (slider is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            // Extract rounding mode if not default
            try
            {
                var roundingMode = slider.Slider.Type.ToString();
                if (!string.IsNullOrEmpty(roundingMode) && roundingMode != "Float")
                {
                    state.Rounding = roundingMode;
                    hasState = true;
                }
            }
            catch
            {
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public object? ExtractValue(IGH_DocumentObject obj)
        {
            if (obj is not GH_NumberSlider slider)
                return null;

            try
            {
                var current = slider.CurrentValue;
                var min = slider.Slider.Minimum;
                var max = slider.Slider.Maximum;

                // Format as "current<min~max>" for compact representation
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}<{1}~{2}>",
                    current,
                    min,
                    max);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SliderHandler] Error extracting value: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_NumberSlider slider || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue)
            {
                slider.Locked = state.Locked.Value;
            }

            // Apply hidden state (via IGH_PreviewObject)
            if (state.Hidden.HasValue && slider is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            // Apply rounding mode
            if (!string.IsNullOrEmpty(state.Rounding))
            {
                try
                {
                    // Try to set the slider type via reflection to avoid namespace issues
                    var sliderType = slider.Slider.GetType();
                    var typeProp = sliderType.GetProperty("Type");
                    if (typeProp != null && typeProp.CanWrite)
                    {
                        var enumType = typeProp.PropertyType;
                        if (Enum.TryParse(enumType, state.Rounding, true, out var accuracy))
                        {
                            typeProp.SetValue(slider.Slider, accuracy);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SliderHandler] Error applying rounding: {ex.Message}");
                }
            }

            // Apply value
            if (state.Value != null)
            {
                ApplyValue(obj, state.Value);
            }
        }

        /// <inheritdoc/>
        public void ApplyValue(IGH_DocumentObject obj, object value)
        {
            if (obj is not GH_NumberSlider slider || value == null)
                return;

            var valueStr = value.ToString();
            if (string.IsNullOrEmpty(valueStr))
                return;

            try
            {
                // Parse format "current<min~max>"
                var match = Regex.Match(valueStr, @"^([\d.\-]+)<([\d.\-]+)~([\d.\-]+)>$");
                if (match.Success)
                {
                    var current = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    var min = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    var max = decimal.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                    slider.Slider.Minimum = min;
                    slider.Slider.Maximum = max;
                    slider.SetSliderValue(current);
                }
                else if (decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var simpleValue))
                {
                    // Simple numeric value only
                    slider.SetSliderValue(simpleValue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SliderHandler] Error applying value '{valueStr}': {ex.Message}");
            }
        }
    }
}
