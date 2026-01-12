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
using System.Linq;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ScriptComponents
{
    /// <summary>
    /// Centralized utility for script component detection and operations.
    /// Provides consistent behavior across GhJSON extraction and placement operations.
    /// Uses reflection to detect script components without requiring RhinoCodePlatform dependencies.
    /// </summary>
    public static class ScriptComponentHelper
    {
        private const string ScriptComponentInterfaceName = "IScriptComponent";

        /// <summary>
        /// Checks if an object implements IScriptComponent via reflection.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object implements IScriptComponent.</returns>
        public static bool IsScriptComponentInstance(object? obj)
        {
            if (obj == null)
                return false;

            return obj.GetType().GetInterfaces()
                .Any(i => i.Name == ScriptComponentInterfaceName);
        }

        /// <summary>
        /// Detects whether an object is a C# script component using reflection.
        /// </summary>
        /// <param name="scriptComp">The script component to check.</param>
        /// <returns>True if the component is a C# script component.</returns>
        public static bool IsCSharpScriptComponent(object? scriptComp)
        {
            if (scriptComp == null || !IsScriptComponentInstance(scriptComp))
                return false;

            try
            {
                var typeName = scriptComp.GetType().Name;
                if (typeName.Contains("CSharp", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var langProp = scriptComp.GetType().GetProperty("Language");
                if (langProp != null)
                {
                    var langValue = langProp.GetValue(scriptComp);
                    if (langValue != null && langValue.ToString().Contains("C#", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Detects whether an object is a Python script component using reflection.
        /// </summary>
        /// <param name="scriptComp">The script component to check.</param>
        /// <returns>True if the component is a Python script component.</returns>
        public static bool IsPythonScriptComponent(object? scriptComp)
        {
            if (scriptComp == null || !IsScriptComponentInstance(scriptComp))
                return false;

            try
            {
                var typeName = scriptComp.GetType().Name;
                if (typeName.Contains("Python", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var langProp = scriptComp.GetType().GetProperty("Language");
                if (langProp != null)
                {
                    var langValue = langProp.GetValue(scriptComp);
                    if (langValue != null)
                    {
                        var langStr = langValue.ToString();
                        if (langStr.Contains("Python", StringComparison.OrdinalIgnoreCase) ||
                            langStr.Contains("IronPython", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Detects whether an object is a VB.NET script component using reflection.
        /// </summary>
        /// <param name="scriptComp">The script component to check.</param>
        /// <returns>True if the component is a VB.NET script component.</returns>
        public static bool IsVBScriptComponent(object? scriptComp)
        {
            if (scriptComp == null || !IsScriptComponentInstance(scriptComp))
                return false;

            try
            {
                var typeName = scriptComp.GetType().Name;
                if (typeName.Contains("VB", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("VisualBasic", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var langProp = scriptComp.GetType().GetProperty("Language");
                if (langProp != null)
                {
                    var langValue = langProp.GetValue(scriptComp);
                    if (langValue != null)
                    {
                        var langStr = langValue.ToString();
                        if (langStr.Contains("VB", StringComparison.OrdinalIgnoreCase) ||
                            langStr.Contains("VisualBasic", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Gets the script language name for a script component using reflection.
        /// </summary>
        /// <param name="scriptComp">The script component to check.</param>
        /// <returns>Language name (e.g., "C#", "Python", "IronPython", "VB") or "Unknown".</returns>
        public static string GetScriptLanguage(object? scriptComp)
        {
            if (scriptComp == null || !IsScriptComponentInstance(scriptComp))
                return "Unknown";

            try
            {
                var langProp = scriptComp.GetType().GetProperty("Language");
                if (langProp != null)
                {
                    var langValue = langProp.GetValue(scriptComp);
                    if (langValue != null)
                    {
                        return langValue.ToString();
                    }
                }

                var typeName = scriptComp.GetType().Name;
                if (typeName.Contains("CSharp", StringComparison.OrdinalIgnoreCase))
                    return "C#";
                if (typeName.Contains("IronPython", StringComparison.OrdinalIgnoreCase))
                    return "IronPython";
                if (typeName.Contains("Python", StringComparison.OrdinalIgnoreCase))
                    return "Python";
                if (typeName.Contains("VB", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("VisualBasic", StringComparison.OrdinalIgnoreCase))
                    return "VB";
            }
            catch { }

            return "Unknown";
        }

        /// <summary>
        /// Gets the script language type enum for a script component using reflection.
        /// </summary>
        /// <param name="scriptComp">The script component to check.</param>
        /// <returns>ScriptLanguage enum value.</returns>
        public static ScriptLanguage GetScriptLanguageType(object? scriptComp)
        {
            if (scriptComp == null || !IsScriptComponentInstance(scriptComp))
                return ScriptLanguage.Unknown;

            if (IsCSharpScriptComponent(scriptComp))
                return ScriptLanguage.CSharp;
            if (IsPythonScriptComponent(scriptComp))
            {
                var langName = GetScriptLanguage(scriptComp);
                if (!string.IsNullOrEmpty(langName) && langName.Contains("IronPython", StringComparison.OrdinalIgnoreCase))
                    return ScriptLanguage.IronPython;
                return ScriptLanguage.Python;
            }

            if (IsVBScriptComponent(scriptComp))
                return ScriptLanguage.VB;

            return ScriptLanguage.Unknown;
        }

        /// <summary>
        /// Gets the script language type enum from an IGH_ActiveObject.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>ScriptLanguage enum value.</returns>
        public static ScriptLanguage GetScriptLanguageTypeFromComponent(IGH_ActiveObject? component)
        {
            if (component == null)
                return ScriptLanguage.Unknown;

            // Try reflection-based detection first
            if (IsScriptComponentInstance(component))
            {
                return GetScriptLanguageType(component);
            }

            // Fallback to GUID-based detection
            var lang = ScriptComponentFactory.DetectLanguage(component);
            return lang switch
            {
                "c#" => ScriptLanguage.CSharp,
                "python" => ScriptLanguage.Python,
                "ironpython" => ScriptLanguage.IronPython,
                "vb" => ScriptLanguage.VB,
                _ => ScriptLanguage.Unknown
            };
        }
    }

    /// <summary>
    /// Enum representing the script language types supported by Grasshopper.
    /// </summary>
    public enum ScriptLanguage
    {
        /// <summary>Unknown or unsupported script language.</summary>
        Unknown,

        /// <summary>C# script component.</summary>
        CSharp,

        /// <summary>Python script component (GhPython or newer Python 3).</summary>
        Python,

        /// <summary>IronPython script component (legacy GhPython).</summary>
        IronPython,

        /// <summary>VB.NET script component.</summary>
        VB
    }
}
