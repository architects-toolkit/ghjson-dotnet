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
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ScriptComponents
{
    /// <summary>
    /// Centralized factory for creating script component definitions.
    /// Provides script component GUIDs, display names, and ComponentProperties builders.
    /// </summary>
    public static class ScriptComponentFactory
    {
        #region Script Component GUIDs

        /// <summary>
        /// Python 3 script component GUID.
        /// </summary>
        public static readonly Guid Python3Guid = new Guid("719467e6-7cf5-4848-99b0-c5dd57e5442c");

        /// <summary>
        /// IronPython 2 script component GUID.
        /// </summary>
        public static readonly Guid IronPython2Guid = new Guid("97aa26ef-88ae-4ba6-98a6-ed6ddeca11d1");

        /// <summary>
        /// C# script component GUID.
        /// </summary>
        public static readonly Guid CSharpGuid = new Guid("b6ba1144-02d6-4a2d-b53c-ec62e290eeb7");

        /// <summary>
        /// VB.NET script component GUID.
        /// </summary>
        public static readonly Guid VBNetGuid = new Guid("079bd9bd-54a0-41d4-98af-db999015f63d");

        #endregion

        #region Language Mapping

        private static readonly Dictionary<string, ScriptComponentInfo> LanguageMap = new Dictionary<string, ScriptComponentInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["python"] = new ScriptComponentInfo(Python3Guid, "Python 3 Script", "python"),
            ["python3"] = new ScriptComponentInfo(Python3Guid, "Python 3 Script", "python"),
            ["ironpython"] = new ScriptComponentInfo(IronPython2Guid, "IronPython 2 Script", "ironpython"),
            ["ironpython2"] = new ScriptComponentInfo(IronPython2Guid, "IronPython 2 Script", "ironpython"),
            ["c#"] = new ScriptComponentInfo(CSharpGuid, "C# Script", "csharp"),
            ["csharp"] = new ScriptComponentInfo(CSharpGuid, "C# Script", "csharp"),
            ["vb"] = new ScriptComponentInfo(VBNetGuid, "VB Script", "vb"),
            ["vb.net"] = new ScriptComponentInfo(VBNetGuid, "VB Script", "vb"),
            ["vbnet"] = new ScriptComponentInfo(VBNetGuid, "VB Script", "vb"),
        };

        #endregion

        #region Public API

        /// <summary>
        /// Gets script component information by language key.
        /// </summary>
        /// <param name="languageKey">Language identifier (e.g., "python", "c#", "vb").</param>
        /// <returns>Script component info, or null if not found.</returns>
        public static ScriptComponentInfo? GetComponentInfo(string? languageKey)
        {
            if (string.IsNullOrEmpty(languageKey))
                return null;

            return LanguageMap.TryGetValue(languageKey.Trim(), out var info) ? info : null;
        }

        /// <summary>
        /// Checks if a language key is supported.
        /// </summary>
        /// <param name="languageKey">Language identifier to check.</param>
        /// <returns>True if the language is supported.</returns>
        public static bool IsLanguageSupported(string? languageKey)
        {
            return !string.IsNullOrEmpty(languageKey) &&
                   LanguageMap.ContainsKey(languageKey.Trim());
        }

        /// <summary>
        /// Normalizes a language identifier to a supported key or returns the provided default.
        /// </summary>
        /// <param name="languageKey">Language identifier (for example, "python", "python3", "c#", "csharp").</param>
        /// <param name="defaultLanguage">Default language key to use when the identifier is null, empty, or not supported.</param>
        /// <returns>Normalized language key (for example, "python", "ironpython", "csharp", "vb") or the default language.</returns>
        public static string NormalizeLanguageKeyOrDefault(string? languageKey, string defaultLanguage = "python")
        {
            if (string.IsNullOrWhiteSpace(languageKey))
            {
                return defaultLanguage;
            }

            if (LanguageMap.TryGetValue(languageKey.Trim(), out var info))
            {
                return info.LanguageKey;
            }

            return defaultLanguage;
        }

        /// <summary>
        /// Gets all supported language keys.
        /// </summary>
        /// <returns>Array of supported language keys.</returns>
        public static string[] GetSupportedLanguages()
        {
            return new[] { "python", "ironpython", "c#", "vb" };
        }

        /// <summary>
        /// Detects language from a component's GUID.
        /// </summary>
        /// <param name="componentGuid">The component GUID to check.</param>
        /// <returns>Normalized language key or "unknown".</returns>
        public static string DetectLanguageFromGuid(Guid componentGuid)
        {
            if (componentGuid == Python3Guid) return "python";
            if (componentGuid == IronPython2Guid) return "ironpython";
            if (componentGuid == CSharpGuid) return "c#";
            if (componentGuid == VBNetGuid) return "vb";
            return "unknown";
        }

        /// <summary>
        /// Detects language from a Grasshopper component.
        /// </summary>
        /// <param name="component">The component to detect language from.</param>
        /// <returns>Normalized language key or "unknown".</returns>
        public static string DetectLanguage(IGH_ActiveObject? component)
        {
            if (component == null)
                return "unknown";

            // First try by GUID
            var byGuid = DetectLanguageFromGuid(component.ComponentGuid);
            if (byGuid != "unknown")
                return byGuid;

            // Fallback to type name detection
            return DetectLanguageFromTypeName(component);
        }

        /// <summary>
        /// Checks if a component is a script component.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>True if the component is a script component.</returns>
        public static bool IsScriptComponent(IGH_ActiveObject? component)
        {
            if (component == null)
                return false;

            var guid = component.ComponentGuid;
            return guid == Python3Guid ||
                   guid == IronPython2Guid ||
                   guid == CSharpGuid ||
                   guid == VBNetGuid;
        }

        /// <summary>
        /// Creates ComponentProperties for a script component.
        /// </summary>
        /// <param name="languageKey">Language identifier.</param>
        /// <param name="scriptCode">Script source code.</param>
        /// <param name="inputs">Input parameter settings.</param>
        /// <param name="outputs">Output parameter settings.</param>
        /// <param name="nickname">Optional component nickname.</param>
        /// <returns>Configured ComponentProperties ready for deserialization.</returns>
        public static ComponentProperties CreateScriptComponent(
            string languageKey,
            string scriptCode,
            List<ParameterSettings>? inputs = null,
            List<ParameterSettings>? outputs = null,
            string? nickname = null)
        {
            var info = GetComponentInfo(languageKey);
            if (info == null)
            {
                throw new ArgumentException($"Unsupported script language: {languageKey}. Supported: python, ironpython, c#, vb.");
            }

            return new ComponentProperties
            {
                Name = info.DisplayName,
                ComponentGuid = info.Guid,
                InstanceGuid = Guid.NewGuid(),
                NickName = nickname,
                ComponentState = new ComponentState
                {
                    Value = scriptCode
                },
                InputSettings = inputs,
                OutputSettings = outputs
            };
        }

        #endregion

        #region Private Helpers

        private static string DetectLanguageFromTypeName(IGH_ActiveObject component)
        {
            try
            {
                var typeName = component.GetType().Name.ToLowerInvariant();
                Debug.WriteLine($"[ScriptComponentFactory] Detecting language from type name: {typeName}");

                if (typeName.Contains("python3"))
                    return "python";
                if (typeName.Contains("ironpython") || typeName.Contains("python2"))
                    return "ironpython";
                if (typeName.Contains("csharp"))
                    return "c#";
                if (typeName.Contains("vb"))
                    return "vb";

                Debug.WriteLine($"[ScriptComponentFactory] Unknown type name: {typeName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScriptComponentFactory] Error detecting language from type name: {ex.Message}");
            }

            return "unknown";
        }

        #endregion
    }

    /// <summary>
    /// Information about a script component type.
    /// </summary>
    public class ScriptComponentInfo
    {
        /// <summary>
        /// Gets the component GUID for instantiation.
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// Gets the display name for the component.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the normalized language key.
        /// </summary>
        public string LanguageKey { get; }

        /// <summary>
        /// Initializes a new instance of ScriptComponentInfo.
        /// </summary>
        public ScriptComponentInfo(Guid guid, string displayName, string languageKey)
        {
            Guid = guid;
            DisplayName = displayName;
            LanguageKey = languageKey;
        }
    }
}
