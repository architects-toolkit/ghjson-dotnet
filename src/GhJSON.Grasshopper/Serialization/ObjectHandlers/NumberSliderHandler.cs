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
using System.Globalization;
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
        private const string ExtensionKey = "numberSlider";

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

                var sliderData = new Dictionary<string, object>
                {
                    ["value"] = slider.CurrentValue,
                    ["min"] = slider.Slider.Minimum,
                    ["max"] = slider.Slider.Maximum,
                    ["decimals"] = slider.Slider.DecimalPlaces,
                    ["rounding"] = slider.Slider.Type.ToString()
                };

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
            if (data.TryGetValue("min", out var minVal))
            {
                slider.Slider.Minimum = Convert.ToDecimal(minVal, CultureInfo.InvariantCulture);
            }

            if (data.TryGetValue("max", out var maxVal))
            {
                slider.Slider.Maximum = Convert.ToDecimal(maxVal, CultureInfo.InvariantCulture);
            }

            if (data.TryGetValue("decimals", out var decVal))
            {
                slider.Slider.DecimalPlaces = Convert.ToInt32(decVal, CultureInfo.InvariantCulture);
            }

            if (data.TryGetValue("rounding", out var roundVal))
            {
                if (Enum.TryParse<GH_SliderAccuracy>(roundVal?.ToString(), out var rounding))
                {
                    slider.Slider.Type = rounding;
                }
            }

            if (data.TryGetValue("value", out var val))
            {
                slider.SetSliderValue(Convert.ToDecimal(val, CultureInfo.InvariantCulture));
            }
        }
    }
}
