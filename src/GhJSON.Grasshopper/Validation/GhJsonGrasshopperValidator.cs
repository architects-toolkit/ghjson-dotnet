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
using System.Linq;
using System.Text;
using GhJSON.Core.Validation;
using GhJSON.Grasshopper.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Validation
{
    /// <summary>
    /// Grasshopper-specific utilities for validating GhJSON format with component validation.
    /// Extends the core GhJSON validation with Grasshopper component existence and data type compatibility checks.
    /// </summary>
    public static class GhJsonGrasshopperValidator
    {
        /// <summary>
        /// Validates that the given JSON string conforms to the expected GhJSON format,
        /// including Grasshopper-specific component existence and data type compatibility checks.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="errorMessage">Aggregated error, warning and info messages; null if no issues.</param>
        /// <returns>True if no errors; otherwise false.</returns>
        public static bool Validate(string json, out string? errorMessage)
        {
            // First run the general validation from Core
            bool coreValidationPassed = GhJsonValidator.Validate(json, out var coreErrorMessage);

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            // Parse the core validation results
            if (!string.IsNullOrEmpty(coreErrorMessage))
            {
                ParseValidationMessage(coreErrorMessage, errors, warnings, infos);
            }

            // Only proceed with Grasshopper-specific validation if JSON is parseable
            if (coreValidationPassed || !errors.Any(e => e.Contains("Invalid JSON")))
            {
                try
                {
                    var root = JObject.Parse(json);
                    var components = root["components"] as JArray;
                    var connections = root["connections"] as JArray;

                    // Add Grasshopper-specific validations
                    if (components != null)
                    {
                        var componentIssues = ValidateComponentExistence(components);
                        errors.AddRange(componentIssues);
                    }

                    if (components != null && connections != null)
                    {
                        var connectionIssues = ValidateConnectionDataTypes(connections, components);
                        warnings.AddRange(connectionIssues);
                    }
                }
                catch
                {
                    // If JSON parsing fails here, the core validation should have caught it
                }
            }

            // Combine all validation results
            var result = BuildValidationResult(errors, warnings, infos, out errorMessage);
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
            return ParseValidationMessage(message);
        }

        /// <summary>
        /// Parses a validation message string into a ValidationResult.
        /// </summary>
        private static ValidationResult ParseValidationMessage(string? message)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(message))
            {
                return result;
            }

            var currentSection = "";
            foreach (var line in message!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
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

        private static void ParseValidationMessage(string validationMessage, List<string> errors, List<string> warnings, List<string> infos)
        {
            if (string.IsNullOrEmpty(validationMessage))
            {
                return;
            }

            var lines = validationMessage.Split('\n');
            var currentSection = string.Empty;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("Errors:"))
                {
                    currentSection = "errors";
                    continue;
                }
                else if (trimmedLine.StartsWith("Warnings:"))
                {
                    currentSection = "warnings";
                    continue;
                }
                else if (trimmedLine.StartsWith("Information:"))
                {
                    currentSection = "infos";
                    continue;
                }

                switch (currentSection)
                {
                    case "errors":
                        if (trimmedLine.StartsWith("- "))
                        {
                            errors.Add(trimmedLine.Substring(2));
                        }
                        break;
                    case "warnings":
                        if (trimmedLine.StartsWith("- "))
                        {
                            warnings.Add(trimmedLine.Substring(2));
                        }
                        break;
                    case "infos":
                        if (trimmedLine.StartsWith("- "))
                        {
                            infos.Add(trimmedLine.Substring(2));
                        }
                        break;
                }
            }
        }

        private static List<string> ValidateComponentExistence(JArray components)
        {
            var issues = new List<string>();
            if (components == null)
            {
                return issues;
            }

            for (int i = 0; i < components.Count; i++)
            {
                if (!(components[i] is JObject comp))
                {
                    continue;
                }

                if (comp["componentGuid"] != null && comp["componentGuid"].Type != JTokenType.Null)
                {
                    var componentGuid = comp["componentGuid"].ToString();
                    if (Guid.TryParse(componentGuid, out var guid))
                    {
                        if (!ObjectFactory.IsValidComponent(guid))
                        {
                            var componentName = comp["name"]?.ToString() ?? "Unknown";
                            issues.Add($"components[{i}] with name '{componentName}' and GUID '{componentGuid}' does not exist in the Grasshopper system.");
                        }
                    }
                }
            }

            return issues;
        }

        private static bool BuildValidationResult(List<string> errors, List<string> warnings, List<string> infos, out string? errorMessage)
        {
            var result = new StringBuilder();

            if (errors.Any())
            {
                result.AppendLine("Errors:");
                foreach (var error in errors)
                {
                    result.AppendLine($"- {error}");
                }
            }

            if (warnings.Any())
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendLine("Warnings:");
                foreach (var warning in warnings)
                {
                    result.AppendLine($"- {warning}");
                }
            }

            if (infos.Any())
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendLine("Information:");
                foreach (var info in infos)
                {
                    result.AppendLine($"- {info}");
                }
            }

            errorMessage = result.Length > 0 ? result.ToString().TrimEnd() : null;
            return !errors.Any();
        }

        private static List<string> ValidateConnectionDataTypes(JArray connections, JArray components)
        {
            var issues = new List<string>();
            if (connections == null || components == null)
            {
                return issues;
            }

            Debug.WriteLine($"[ValidateConnectionDataTypes] Validating {connections.Count} connections and {components.Count} components...");

            // Create lookup for component information by integer ID
            var componentLookupById = new Dictionary<int, JObject>();
            foreach (var token in components)
            {
                if (token is JObject comp && comp["id"]?.Type == JTokenType.Integer)
                {
                    int id = comp["id"].Value<int>();
                    componentLookupById[id] = comp;
                }
            }

            for (int i = 0; i < connections.Count; i++)
            {
                if (!(connections[i] is JObject conn))
                {
                    continue;
                }

                var fromEndpoint = conn["from"] as JObject;
                var toEndpoint = conn["to"] as JObject;

                if (fromEndpoint == null || toEndpoint == null)
                {
                    continue;
                }

                var fromParameterName = fromEndpoint["paramName"]?.ToString();
                var toParameterName = toEndpoint["paramName"]?.ToString();

                if (string.IsNullOrEmpty(fromParameterName) || string.IsNullOrEmpty(toParameterName))
                {
                    Debug.WriteLine($"[ValidateConnectionDataTypes] Skipping connection {i} due to missing parameter names");
                    continue;
                }

                JObject? fromComponent = null;
                JObject? toComponent = null;

                if (fromEndpoint["id"]?.Type == JTokenType.Integer)
                {
                    int fromId = fromEndpoint["id"].Value<int>();
                    componentLookupById.TryGetValue(fromId, out fromComponent);
                }

                if (toEndpoint["id"]?.Type == JTokenType.Integer)
                {
                    int toId = toEndpoint["id"].Value<int>();
                    componentLookupById.TryGetValue(toId, out toComponent);
                }

                Debug.WriteLine($"[ValidateConnectionDataTypes] Validating connection {i}: {fromParameterName} -> {toParameterName}");

                if (fromComponent != null && toComponent != null)
                {
                    var fromComponentGuid = fromComponent["componentGuid"]?.ToString();
                    var toComponentGuid = toComponent["componentGuid"]?.ToString();
                    var fromComponentName = fromComponent["name"]?.ToString() ?? "Unknown";
                    var toComponentName = toComponent["name"]?.ToString() ?? "Unknown";

                    if (!string.IsNullOrEmpty(fromComponentGuid) && !string.IsNullOrEmpty(toComponentGuid))
                    {
                        if (Guid.TryParse(fromComponentGuid, out var fromGuid) &&
                            Guid.TryParse(toComponentGuid, out var toGuid))
                        {
                            var incompatibilityReason = CheckDataTypeCompatibility(fromGuid, toGuid, fromParameterName, toParameterName);
                            if (!string.IsNullOrEmpty(incompatibilityReason))
                            {
                                issues.Add($"connections[{i}]: Potential data type mismatch between '{fromComponentName}' output '{fromParameterName}' and '{toComponentName}' input '{toParameterName}'. {incompatibilityReason}");
                                Debug.WriteLine($"[ValidateConnectionDataTypes] Connection incompatible: {incompatibilityReason}");
                            }
                            else
                            {
                                Debug.WriteLine($"[ValidateConnectionDataTypes] Connection compatible");
                            }
                        }
                    }
                }
            }

            return issues;
        }

        private static string? CheckDataTypeCompatibility(Guid fromComponentGuid, Guid toComponentGuid, string fromParameterName, string toParameterName)
        {
            try
            {
                var fromProxy = ObjectFactory.FindProxy(fromComponentGuid);
                var toProxy = ObjectFactory.FindProxy(toComponentGuid);

                if (fromProxy == null || toProxy == null)
                {
                    return null;
                }

                var fromObj = ObjectFactory.CreateInstance(fromProxy);
                var toObj = ObjectFactory.CreateInstance(toProxy);

                if (fromObj == null || toObj == null)
                {
                    return null;
                }

                IGH_Param? outputParam = null;
                if (fromObj is IGH_Component fromComponent)
                {
                    outputParam = ParameterAccess.GetOutputByName(fromComponent, fromParameterName);
                }
                else if (fromObj is IGH_Param fromParam)
                {
                    outputParam = fromParam;
                }

                IGH_Param? inputParam = null;
                if (toObj is IGH_Component toComponent)
                {
                    inputParam = ParameterAccess.GetInputByName(toComponent, toParameterName);
                }
                else if (toObj is IGH_Param toParam)
                {
                    inputParam = toParam;
                }

                if (outputParam == null)
                {
                    return $"Output parameter '{fromParameterName}' not found";
                }

                if (inputParam == null)
                {
                    return $"Input parameter '{toParameterName}' not found";
                }

                if (outputParam != null && inputParam != null)
                {
                    var outputType = outputParam.Type;
                    var inputType = inputParam.Type;

                    Debug.WriteLine($"[CheckDataTypeCompatibility] Param types: {fromParameterName}({outputType.Name}) -> {toParameterName}({inputType.Name})");

                    if (!AreTypesCompatible(outputType, inputType))
                    {
                        return $"Output type '{outputType.Name}' may not be compatible with input type '{inputType.Name}'";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool AreTypesCompatible(Type outputType, Type inputType)
        {
            Debug.WriteLine($"[AreTypesCompatible] Checking: {outputType?.Name} -> {inputType?.Name}");

            if (outputType == null || inputType == null)
            {
                Debug.WriteLine("[AreTypesCompatible] Null type(s), allowing connection");
                return true;
            }

            if (outputType == inputType)
            {
                Debug.WriteLine("[AreTypesCompatible] Exact type match - COMPATIBLE");
                return true;
            }

            if (inputType.IsAssignableFrom(outputType))
            {
                Debug.WriteLine($"[AreTypesCompatible] {outputType.Name} is assignable to {inputType.Name} - COMPATIBLE");
                return true;
            }

            if (outputType.IsAssignableFrom(inputType))
            {
                Debug.WriteLine($"[AreTypesCompatible] {inputType.Name} implements {outputType.Name} (general -> specific) - INCOMPATIBLE");
                return false;
            }

            try
            {
                var outputGoo = TryCreateGooInstance(outputType);
                var inputGoo = TryCreateGooInstance(inputType);

                Debug.WriteLine($"[AreTypesCompatible] Created instances: output={outputGoo?.GetType().Name ?? "null"}, input={inputGoo?.GetType().Name ?? "null"}");

                if (outputGoo != null && inputGoo != null)
                {
                    bool castFromResult = inputGoo.CastFrom(outputGoo);
                    Debug.WriteLine($"[AreTypesCompatible] CastFrom test: {castFromResult}");

                    if (castFromResult)
                    {
                        Debug.WriteLine("[AreTypesCompatible] CastFrom succeeded - COMPATIBLE");
                        return true;
                    }

                    if (outputGoo is IGH_QuickCast outputQC && inputGoo is IGH_QuickCast inputQC)
                    {
                        Debug.WriteLine($"[AreTypesCompatible] QuickCast types: output={outputQC.QC_Type}, input={inputQC.QC_Type}");

                        if (outputQC.QC_Type == inputQC.QC_Type)
                        {
                            Debug.WriteLine($"[AreTypesCompatible] Matching QuickCast type: {outputQC.QC_Type} - COMPATIBLE");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AreTypesCompatible] Reflection error: {ex.Message}");
            }

            Debug.WriteLine("[AreTypesCompatible] No compatibility found - INCOMPATIBLE");
            return false;
        }

        private static IGH_Goo? TryCreateGooInstance(Type gooType)
        {
            try
            {
                if (!typeof(IGH_Goo).IsAssignableFrom(gooType))
                {
                    return null;
                }

                var constructor = gooType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    return constructor.Invoke(null) as IGH_Goo;
                }

                return Activator.CreateInstance(gooType) as IGH_Goo;
            }
            catch
            {
                return null;
            }
        }
    }
}
