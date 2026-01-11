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

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Provides methods to fix JSON structure issues in GhJSON before deserialization.
    /// </summary>
    public static class GhJsonFixer
    {
        /// <summary>
        /// Fixes invalid component instanceGuids by assigning new GUIDs and recording mappings.
        /// </summary>
        /// <param name="json">The JSON object to fix.</param>
        /// <param name="idMapping">Dictionary to track original to new GUID mappings.</param>
        /// <returns>The fixed JSON object and the updated ID mapping.</returns>
        public static (JObject Json, Dictionary<string, Guid> Mapping) FixComponentInstanceGuids(
            JObject json,
            Dictionary<string, Guid>? idMapping = null)
        {
            idMapping ??= new Dictionary<string, Guid>();

            if (json["components"] is JArray comps)
            {
                foreach (var comp in comps)
                {
                    if (comp["instanceGuid"] is JToken instToken)
                    {
                        var instStr = instToken.ToString();
                        if (!Guid.TryParse(instStr, out _))
                        {
                            var newGuid = Guid.NewGuid();
                            idMapping[instStr] = newGuid;
                            comp["instanceGuid"] = newGuid.ToString();
                        }
                    }
                }
            }

            return (json, idMapping);
        }

        /// <summary>
        /// Removes pivot properties if not all components define a valid pivot.
        /// </summary>
        /// <param name="json">The JSON object to fix.</param>
        /// <returns>The fixed JSON object.</returns>
        public static JObject RemovePivotsIfIncomplete(JObject json)
        {
            if (json["components"] is JArray comps)
            {
                bool allHavePivot = true;
                foreach (var comp in comps)
                {
                    var pivotToken = comp["pivot"];
                    if (pivotToken == null)
                    {
                        allHavePivot = false;
                        break;
                    }

                    // Handle both old object format {"X": ..., "Y": ...} and new compact string format "X,Y"
                    if (pivotToken.Type == JTokenType.Object)
                    {
                        // Old format - check for X and Y properties
                        if (pivotToken["X"] == null || pivotToken["Y"] == null)
                        {
                            allHavePivot = false;
                            break;
                        }
                    }
                    else if (pivotToken.Type == JTokenType.String)
                    {
                        // New compact format - check if string is not empty
                        var pivotStr = pivotToken.ToString();
                        if (string.IsNullOrEmpty(pivotStr) || !pivotStr.Contains(","))
                        {
                            allHavePivot = false;
                            break;
                        }
                    }
                    else
                    {
                        // Unknown format
                        allHavePivot = false;
                        break;
                    }
                }

                if (!allHavePivot)
                {
                    foreach (var comp in comps)
                    {
                        ((JObject)comp).Remove("pivot");
                    }
                }
            }

            return json;
        }

        /// <summary>
        /// Applies all available fixes to a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON object to fix.</param>
        /// <returns>The fixed JSON object and any ID mappings.</returns>
        public static (JObject Json, Dictionary<string, Guid> IdMapping) FixAll(JObject json)
        {
            var (fixedJson, idMapping) = FixComponentInstanceGuids(json);
            fixedJson = RemovePivotsIfIncomplete(fixedJson);
            return (fixedJson, idMapping);
        }
    }
}
