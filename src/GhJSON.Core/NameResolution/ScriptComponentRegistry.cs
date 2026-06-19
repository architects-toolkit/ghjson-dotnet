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

namespace GhJSON.Core.NameResolution
{
    /// <summary>
    /// Central registry for Rhino 8 script component metadata.
    /// Provides known component GUIDs, display names, and language keys
    /// for C#, Python 3, IronPython 2, and VB Script components.
    /// </summary>
    public static class ScriptComponentRegistry
    {
        /// <summary>
        /// Component GUID for the Rhino 8 Python 3 Script component.
        /// </summary>
        public static readonly Guid Python3 = new Guid("719467e6-7cf5-4848-99b0-c5dd57e5442c");

        /// <summary>
        /// Component GUID for the Rhino 8 IronPython 2 Script component.
        /// </summary>
        public static readonly Guid IronPython2 = new Guid("97aa26ef-88ae-4ba6-98a6-ed6ddeca11d1");

        /// <summary>
        /// Component GUID for the Rhino 8 C# Script component.
        /// </summary>
        public static readonly Guid CSharp = new Guid("b6ba1144-02d6-4a2d-b53c-ec62e290eeb7");

        /// <summary>
        /// Component GUID for the Rhino 8 VB Script component.
        /// </summary>
        public static readonly Guid VB = new Guid("079bd9bd-54a0-41d4-98af-db999015f63d");

        /// <summary>
        /// All known script component GUIDs.
        /// </summary>
        public static readonly HashSet<Guid> AllGuids = new()
        {
            Python3,
            IronPython2,
            CSharp,
            VB,
        };

        /// <summary>
        /// Gets the component GUID for a given normalized language key.
        /// </summary>
        /// <param name="languageKey">Normalized language key (e.g., "python", "c#", "vb").</param>
        /// <returns>The component GUID, or <see cref="Python3"/> as the default fallback.</returns>
        public static Guid GetGuid(string languageKey)
        {
            return languageKey?.Trim().ToLowerInvariant() switch
            {
                "python" => Python3,
                "ironpython" => IronPython2,
                "c#" => CSharp,
                "csharp" => CSharp,
                "vb" => VB,
                "vbscript" => VB,
                _ => Python3,
            };
        }

        /// <summary>
        /// Gets the normalized language key for a given component GUID.
        /// </summary>
        /// <param name="componentGuid">The component GUID.</param>
        /// <returns>The language key (e.g., "python", "c#"), or "unknown" if not recognized.</returns>
        public static string GetLanguageKey(Guid? componentGuid)
        {
            if (componentGuid == null)
            {
                return "unknown";
            }

            var guid = componentGuid.Value;
            if (guid == Python3)
            {
                return "python";
            }

            if (guid == IronPython2)
            {
                return "ironpython";
            }

            if (guid == CSharp)
            {
                return "c#";
            }

            if (guid == VB)
            {
                return "vb";
            }

            return "unknown";
        }

        /// <summary>
        /// Gets the Grasshopper display name for a given normalized language key.
        /// </summary>
        /// <param name="languageKey">Normalized language key.</param>
        /// <returns>The component display name.</returns>
        public static string GetComponentName(string languageKey)
        {
            return languageKey?.Trim().ToLowerInvariant() switch
            {
                "python" => "Python",
                "ironpython" => "IronPython",
                "c#" => "C#",
                "csharp" => "C#",
                "vb" => "VB Script",
                "vbscript" => "VB Script",
                _ => "Python",
            };
        }

        /// <summary>
        /// Gets the GhJSON extension key for a given normalized language key.
        /// </summary>
        /// <param name="languageKey">Normalized language key.</param>
        /// <returns>The extension key (e.g., "gh.python", "gh.csharp").</returns>
        public static string GetExtensionKey(string languageKey)
        {
            return languageKey?.Trim().ToLowerInvariant() switch
            {
                "python" => "gh.python",
                "ironpython" => "gh.ironpython",
                "c#" => "gh.csharp",
                "csharp" => "gh.csharp",
                "vb" => "gh.vbscript",
                "vbscript" => "gh.vbscript",
                _ => "gh.python",
            };
        }

        /// <summary>
        /// Checks whether the given GUID belongs to a known script component.
        /// </summary>
        /// <param name="componentGuid">The GUID to check.</param>
        /// <returns>True if it is a known script component GUID.</returns>
        public static bool IsScriptComponent(Guid? componentGuid)
        {
            return componentGuid.HasValue && AllGuids.Contains(componentGuid.Value);
        }
    }
}
