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
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Utilities to validate GhJSON format.
    /// </summary>
    public static class GhJsonValidator
    {
        /// <summary>
        /// Validates that the given JSON string conforms to the expected GhJSON document format.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="errorMessage">Aggregated error, warning and info messages; null if no issues.</param>
        /// <returns>True if no errors; otherwise false.</returns>
        public static bool Validate(string json, out string? errorMessage)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add("JSON input is null or empty.");
            }

            JObject? root = null;
            if (!errors.Any())
            {
                try
                {
                    root = JObject.Parse(json);
                }
                catch (JsonReaderException ex)
                {
                    errors.Add($"Invalid JSON: {ex.Message}");
                }
            }

            JArray? components = null;
            JArray? connections = null;
            if (root != null)
            {
                if (root["components"] is JArray comps)
                {
                    components = comps;
                }
                else
                {
                    errors.Add("'components' property is missing or not an array.");
                }

                // Treat missing 'connections' as empty array (no connections is valid)
                // but error if it exists and is not an array
                if (root["connections"] == null || root["connections"]!.Type == JTokenType.Null)
                {
                    // Missing or null connections is valid - treat as empty array
                    connections = new JArray();
                }
                else if (root["connections"] is JArray conns)
                {
                    connections = conns;
                }
                else
                {
                    errors.Add("'connections' property is not an array.");
                }

                if (components != null)
                {
                    for (int i = 0; i < components.Count; i++)
                    {
                        if (!(components[i] is JObject comp))
                        {
                            errors.Add($"components[{i}] is not a JSON object.");
                            continue;
                        }

                        if (comp["name"] == null || comp["name"]!.Type == JTokenType.Null)
                        {
                            errors.Add($"components[{i}].name is missing or null.");
                        }

                        if (comp["id"] == null || comp["id"]!.Type != JTokenType.Integer)
                        {
                            errors.Add($"components[{i}].id is missing or not an integer.");
                        }

                        if (comp["componentGuid"] == null || comp["componentGuid"]!.Type == JTokenType.Null)
                        {
                            warnings.Add($"components[{i}].componentGuid is missing or null.");
                        }
                        else
                        {
                            var cg = comp["componentGuid"]!.ToString();
                            if (!Guid.TryParse(cg, out _))
                            {
                                warnings.Add($"components[{i}].componentGuid '{cg}' is not a valid GUID.");
                            }
                        }

                        // instanceGuid is now optional - only validate if present
                        if (comp["instanceGuid"] != null && comp["instanceGuid"]!.Type != JTokenType.Null)
                        {
                            var ig = comp["instanceGuid"]!.ToString();
                            if (!Guid.TryParse(ig, out _))
                            {
                                warnings.Add($"components[{i}].instanceGuid '{ig}' is not a valid GUID.");
                            }
                        }
                    }
                }

                if (connections != null && components != null)
                {
                    // Build lookup for new integer id references
                    var definedIntIds = new HashSet<int>();
                    foreach (var token in components)
                    {
                        if (token is JObject compObj && compObj["id"]?.Type == JTokenType.Integer)
                        {
                            definedIntIds.Add(compObj["id"]!.Value<int>());
                        }
                    }

                    for (int i = 0; i < connections.Count; i++)
                    {
                        if (!(connections[i] is JObject conn))
                        {
                            errors.Add($"connections[{i}] is not a JSON object.");
                            continue;
                        }

                        foreach (var endPoint in new[] { "from", "to" })
                        {
                            if (!(conn[endPoint] is JObject ep))
                            {
                                errors.Add($"connections[{i}].{endPoint} is missing or not an object.");
                                continue;
                            }

                            // New schema: require integer 'id' referencing components[].id
                            if (ep["id"] == null || ep["id"]!.Type != JTokenType.Integer)
                            {
                                errors.Add($"connections[{i}].{endPoint}.id is missing or not an integer.");
                                continue;
                            }

                            var intId = ep["id"]!.Value<int>();
                            if (!definedIntIds.Contains(intId))
                            {
                                errors.Add($"connections[{i}].{endPoint}.id '{intId}' is not defined in components[].id.");
                            }
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            if (errors.Any())
            {
                sb.AppendLine("Errors:");
                foreach (var e in errors)
                {
                    sb.AppendLine($"- {e}");
                }
            }

            if (warnings.Any())
            {
                sb.AppendLine("Warnings:");
                foreach (var w in warnings)
                {
                    sb.AppendLine($"- {w}");
                }
            }

            if (infos.Any())
            {
                sb.AppendLine("Information:");
                foreach (var info in infos)
                {
                    sb.AppendLine($"- {info}");
                }
            }

            var result = !errors.Any();
            errorMessage = sb.Length > 0 ? sb.ToString().TrimEnd() : null;
            return result;
        }

        /// <summary>
        /// Validates a GhJSON document and returns a structured validation result.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>A ValidationResult containing all errors, warnings, and info messages.</returns>
        public static ValidationResult ValidateDetailed(string json)
        {
            Validate(json, out var message);
            return ValidationResult.Parse(message);
        }
    }

    /// <summary>
    /// Result of a GhJSON validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets the list of errors.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the list of warnings.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets the list of informational messages.
        /// </summary>
        public List<string> Info { get; } = new List<string>();

        /// <summary>
        /// Gets a value indicating whether the validation passed (no errors).
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Parses a validation message string into a ValidationResult.
        /// </summary>
        internal static ValidationResult Parse(string? message)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(message))
            {
                return result;
            }

            var currentSection = "";
            foreach (var line in message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed == "Errors:")
                {
                    currentSection = "errors";
                }
                else if (trimmed == "Warnings:")
                {
                    currentSection = "warnings";
                }
                else if (trimmed == "Information:")
                {
                    currentSection = "info";
                }
                else if (trimmed.StartsWith("- "))
                {
                    var msg = trimmed.Substring(2);
                    switch (currentSection)
                    {
                        case "errors":
                            result.Errors.Add(msg);
                            break;
                        case "warnings":
                            result.Warnings.Add(msg);
                            break;
                        case "info":
                            result.Info.Add(msg);
                            break;
                    }
                }
            }

            return result;
        }
    }
}
