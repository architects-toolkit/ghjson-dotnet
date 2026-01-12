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
using System.Drawing;
using System.Globalization;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties
{
    /// <summary>
    /// String conversion helpers for schema properties.
    /// </summary>
    public static class StringConverter
    {
        public static Color StringToColor(string colorString)
        {
            if (string.IsNullOrWhiteSpace(colorString))
                throw new ArgumentException("Invalid color string format. Use 'R,G,B', 'A,R,G,B', '#RRGGBB', or known color name.");

            if (!colorString.Contains(",", StringComparison.Ordinal))
            {
                var c = ColorTranslator.FromHtml(colorString);
                return Color.FromArgb(255, c.R, c.G, c.B);
            }

            string[] parts = colorString.Split(',');

            if (parts.Length == 3)
            {
                int r = int.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int g = int.Parse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int b = int.Parse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture);

                return Color.FromArgb(r, g, b);
            }

            if (parts.Length == 4)
            {
                int a = int.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int r = int.Parse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int g = int.Parse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int b = int.Parse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture);

                return Color.FromArgb(a, r, g, b);
            }

            throw new ArgumentException("Invalid color string format. Use 'R,G,B' or 'A,R,G,B'.");
        }

        public static Font StringToFont(string fontString)
        {
            string[] parts = fontString.Split(',');

            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid font string format. Use 'FontFamilyName, FontSize'.");
            }

            string fontFamilyName = parts[0].Trim();
            string fontSizeString = parts[1].Trim();

            if (fontSizeString.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                fontSizeString = fontSizeString.Substring(0, fontSizeString.Length - 2).Trim();
            }

            if (!float.TryParse(fontSizeString, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float fontSize))
            {
                throw new ArgumentException("Invalid font size.");
            }

            return new Font(fontFamilyName, fontSize);
        }

        public static GH_DataMapping StringToGHDataMapping(object value)
        {
            if (value is string s)
            {
                if (Enum.TryParse<GH_DataMapping>(s, true, out var namedMapping))
                    return namedMapping;

                if (int.TryParse(s, out var intVal))
                    value = intVal;
                else
                    return GH_DataMapping.None;
            }
            else if (value is long || value is int)
            {
                value = Convert.ToInt32(value);
            }
            else
            {
                try { value = Convert.ToInt32(value); }
                catch { return GH_DataMapping.None; }
            }

            switch (value)
            {
                case 0:
                    return GH_DataMapping.None;
                case 1:
                    return GH_DataMapping.Flatten;
                case 2:
                    return GH_DataMapping.Graft;
                default:
                    return GH_DataMapping.None;
            }
        }
    }
}
