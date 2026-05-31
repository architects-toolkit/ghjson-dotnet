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
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using Json.Schema;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Validates GhJSON documents against the official JSON Schema (v1.0) and performs
    /// structural validation. Schema conformance runs at <see cref="ValidationLevel.Standard"/>
    /// and <see cref="ValidationLevel.Strict"/>; <see cref="ValidationLevel.Minimal"/>
    /// performs only basic structural checks.
    /// </summary>
    internal static class GhJsonValidator
    {
        /// <summary>
        /// Validates a GhJSON document.
        /// </summary>
        /// <param name="document">The document to validate.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult Validate(GhJsonDocument document, ValidationLevel level = ValidationLevel.Standard)
        {
            var result = new ValidationResult { IsValid = true };

            // Basic structure validation
            ValidateComponents(document, result);

            if (level >= ValidationLevel.Standard)
            {
                // JSON Schema conformance (ghjson.schema.json v1.0)
                ValidateAgainstSchema(document, result);

                // Connection reference integrity
                ValidateConnections(document, result);

                // Group reference integrity
                ValidateGroups(document, result);
            }

            if (level >= ValidationLevel.Strict)
            {
                // Additional semantic validation
                ValidateSemantics(document, result);
            }

            result.IsValid = !result.HasErrors;
            return result;
        }

        /// <summary>
        /// Validates a JSON string as a GhJSON document. At <see cref="ValidationLevel.Standard"/>
        /// and <see cref="ValidationLevel.Strict"/>, schema conformance runs against the
        /// <em>raw</em> JSON input so that unknown properties (which are dropped by the
        /// strongly-typed deserializer) are still reported.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult Validate(string json, ValidationLevel level = ValidationLevel.Standard)
        {
            GhJsonDocument document;
            try
            {
                document = Serialization.GhJsonSerializer.Deserialize(json);
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Invalid JSON: {ex.Message}");
            }

            var result = new ValidationResult { IsValid = true };

            if (level >= ValidationLevel.Standard)
            {
                // Schema-validate the raw JSON so unknown properties surface.
                ValidateRawJsonAgainstSchema(json, result);

                // Explicitly validate each known extension value against its schema.
                // This ensures additionalProperties constraints in extension schemas are
                // enforced even when the library's $ref chain doesn't propagate them.
                ValidateExtensionSchemas(json, result);
            }

            ValidateComponents(document, result);

            if (level >= ValidationLevel.Standard)
            {
                ValidateConnections(document, result);
                ValidateGroups(document, result);
            }

            if (level >= ValidationLevel.Strict)
            {
                ValidateSemantics(document, result);
            }

            result.IsValid = !result.HasErrors;
            return result;
        }

        /// <summary>
        /// Evaluates the raw JSON text against the official GhJSON schema. Used by the
        /// string-based <see cref="Validate(string, ValidationLevel)"/> overload so that
        /// properties dropped by the strongly-typed deserializer still produce errors.
        /// </summary>
        private static void ValidateRawJsonAgainstSchema(string json, ValidationResult result)
        {
            JsonSchema schema;
            try
            {
                schema = SchemaLoader.MainSchema;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationMessage(
                    $"Unable to load GhJSON schema for validation: {ex.Message}"));
                return;
            }

            JsonNode? instance;
            try
            {
                instance = JsonNode.Parse(json);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationMessage($"Invalid JSON: {ex.Message}"));
                return;
            }

            EvaluateSchema(schema, instance, result);
        }

        /// <summary>
        /// Evaluates the document against the official GhJSON JSON Schema (v1.0, draft 2020-12).
        /// Each non-conforming sub-evaluation is surfaced as a validation error. Failures
        /// of the loader itself (e.g. missing embedded resources) are reported as a single
        /// error so that the caller always gets an actionable result.
        /// </summary>
        private static void ValidateAgainstSchema(GhJsonDocument document, ValidationResult result)
        {
            JsonSchema schema;
            try
            {
                schema = SchemaLoader.MainSchema;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationMessage(
                    $"Unable to load GhJSON schema for validation: {ex.Message}"));
                return;
            }

            JsonNode? instance;
            try
            {
                var json = GhJsonSerializer.Serialize(document);
                instance = JsonNode.Parse(json);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationMessage(
                    $"Unable to serialize document for schema validation: {ex.Message}"));
                return;
            }

            EvaluateSchema(schema, instance, result);
        }

        private static void EvaluateSchema(JsonSchema schema, JsonNode? instance, ValidationResult result)
        {
            var options = new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                EvaluateAs = SpecVersion.Draft202012,
            };

            EvaluationResults evaluation;
            try
            {
                evaluation = schema.Evaluate(instance, options);
            }
            catch (Exception ex)
            {
                // Defensive: never let schema engine exceptions propagate as crashes.
                Debug.WriteLine($"[GhJsonValidator] Schema evaluation threw: {ex}");
                result.Errors.Add(new ValidationMessage(
                    $"Schema evaluation error: {ex.Message}"));
                return;
            }

            if (evaluation.IsValid)
            {
                return;
            }

            var emittedAny = false;
            foreach (var detail in FlattenDetails(evaluation))
            {
                if (detail.IsValid || detail.HasErrors == false || detail.Errors == null)
                {
                    continue;
                }

                var path = detail.InstanceLocation?.ToString();
                foreach (var error in detail.Errors)
                {
                    // error.Key identifies the failed keyword, error.Value is the message.
                    var message = string.IsNullOrWhiteSpace(error.Value)
                        ? $"Schema violation at '{error.Key}'"
                        : error.Value;
                    result.Errors.Add(new ValidationMessage(message, path));
                    emittedAny = true;
                }
            }

            if (!emittedAny)
            {
                // The root evaluation reported failure but no per-keyword message was
                // flattened (e.g. under Basic output mode). Fall back to a generic error.
                result.Errors.Add(new ValidationMessage(
                    "Document does not conform to the GhJSON v1.0 schema."));
            }
        }

        /// <summary>
        /// Validates each known extension value directly against its schema. This provides
        /// deterministic enforcement of extension-level <c>additionalProperties</c> constraints
        /// that may not propagate reliably through <c>$ref</c> chains in all environments.
        /// </summary>
        private static void ValidateExtensionSchemas(string json, ValidationResult result)
        {
            const string baseUri = "https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/extensions/";

            JsonNode? root;
            try
            {
                root = JsonNode.Parse(json);
            }
            catch
            {
                return; // Malformed JSON is handled elsewhere.
            }

            var components = root?["components"]?.AsArray();
            if (components == null)
            {
                return;
            }

            var options = new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                EvaluateAs = SpecVersion.Draft202012,
            };

            for (var i = 0; i < components.Count; i++)
            {
                var extensions = components[i]?["componentState"]?["extensions"]?.AsObject();
                if (extensions == null)
                {
                    continue;
                }

                foreach (var kvp in extensions)
                {
                    var extensionKey = kvp.Key;
                    var extensionValue = kvp.Value;
                    if (extensionValue == null)
                    {
                        continue;
                    }

                    var schemaUri = new Uri($"{baseUri}{extensionKey}.schema.json");
                    JsonSchema schema;
                    try
                    {
                        var schemaDoc = SchemaRegistry.Global.Get(schemaUri);
                        if (schemaDoc is not JsonSchema s)
                        {
                            continue;
                        }

                        schema = s;
                    }
                    catch
                    {
                        continue; // Unknown extension — no schema registered.
                    }

                    EvaluationResults evaluation;
                    try
                    {
                        evaluation = schema.Evaluate(extensionValue, options);
                    }
                    catch
                    {
                        continue; // Defensive: don't crash on evaluation errors.
                    }

                    if (evaluation.IsValid)
                    {
                        continue;
                    }

                    var basePath = $"components[{i}].componentState.extensions.{extensionKey}";
                    foreach (var detail in FlattenDetails(evaluation))
                    {
                        if (detail.IsValid || detail.HasErrors == false || detail.Errors == null)
                        {
                            continue;
                        }

                        var instancePath = detail.InstanceLocation?.ToString();
                        var fullPath = string.IsNullOrEmpty(instancePath) || instancePath == "/"
                            ? basePath
                            : $"{basePath}{instancePath}";

                        foreach (var error in detail.Errors)
                        {
                            var message = string.IsNullOrWhiteSpace(error.Value)
                                ? $"Schema violation at '{error.Key}'"
                                : error.Value;
                            result.Errors.Add(new ValidationMessage(message, fullPath));
                        }
                    }
                }
            }
        }

        private static IEnumerable<EvaluationResults> FlattenDetails(EvaluationResults root)
        {
            yield return root;
            if (root.Details == null)
            {
                yield break;
            }

            foreach (var child in root.Details)
            {
                foreach (var node in FlattenDetails(child))
                {
                    yield return node;
                }
            }
        }

        private static void ValidateComponents(GhJsonDocument document, ValidationResult result)
        {
            if (document.Components == null || document.Components.Count == 0)
            {
                result.Warnings.Add(new ValidationMessage("Document has no components"));
                return;
            }

            var ids = new HashSet<int>();

            for (var i = 0; i < document.Components.Count; i++)
            {
                var component = document.Components[i];
                var path = $"components[{i}]";

                // Check identification requirements (name+id, name+instanceGuid, componentGuid+id, or componentGuid+instanceGuid)
                var hasName = !string.IsNullOrEmpty(component.Name);
                var hasComponentGuid = component.ComponentGuid.HasValue && component.ComponentGuid != Guid.Empty;
                var hasId = component.Id.HasValue;
                var hasInstanceGuid = component.InstanceGuid.HasValue && component.InstanceGuid != Guid.Empty;

                if (!hasName && !hasComponentGuid)
                {
                    result.Errors.Add(new ValidationMessage(
                        "Component must have either 'name' or 'componentGuid'", path));
                }

                if (!hasId && !hasInstanceGuid)
                {
                    result.Errors.Add(new ValidationMessage(
                        "Component must have either 'id' or 'instanceGuid'", path));
                }

                // Check unique IDs
                if (hasId)
                {
                    if (!ids.Add(component.Id!.Value))
                    {
                        result.Errors.Add(new ValidationMessage(
                            $"Duplicate component ID: {component.Id}", path));
                    }

                    if (component.Id!.Value < 1)
                    {
                        result.Errors.Add(new ValidationMessage(
                            $"Component ID must be >= 1, got {component.Id}", path));
                    }
                }
            }
        }

        private static void ValidateConnections(GhJsonDocument document, ValidationResult result)
        {
            if (document.Connections == null || document.Connections.Count == 0)
            {
                return;
            }

            var validIds = new HashSet<int>(
                document.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value));

            for (var i = 0; i < document.Connections.Count; i++)
            {
                var connection = document.Connections[i];
                var path = $"connections[{i}]";

                // Validate from endpoint
                if (!validIds.Contains(connection.From.Id))
                {
                    result.Errors.Add(new ValidationMessage(
                        $"Connection 'from' references non-existent component ID: {connection.From.Id}",
                        $"{path}.from"));
                }

                if (string.IsNullOrEmpty(connection.From.ParamName) && !connection.From.ParamIndex.HasValue)
                {
                    result.Errors.Add(new ValidationMessage(
                        "Connection 'from' must have either 'paramName' or 'paramIndex'",
                        $"{path}.from"));
                }

                // Validate to endpoint
                if (!validIds.Contains(connection.To.Id))
                {
                    result.Errors.Add(new ValidationMessage(
                        $"Connection 'to' references non-existent component ID: {connection.To.Id}",
                        $"{path}.to"));
                }

                if (string.IsNullOrEmpty(connection.To.ParamName) && !connection.To.ParamIndex.HasValue)
                {
                    result.Errors.Add(new ValidationMessage(
                        "Connection 'to' must have either 'paramName' or 'paramIndex'",
                        $"{path}.to"));
                }
            }
        }

        private static void ValidateGroups(GhJsonDocument document, ValidationResult result)
        {
            if (document.Groups == null || document.Groups.Count == 0)
            {
                return;
            }

            var validIds = new HashSet<int>(
                document.Components
                    .Where(c => c.Id.HasValue)
                    .Select(c => c.Id!.Value));

            for (var i = 0; i < document.Groups.Count; i++)
            {
                var group = document.Groups[i];
                var path = $"groups[{i}]";

                // Check group has identification
                if (!group.Id.HasValue && !group.InstanceGuid.HasValue)
                {
                    result.Errors.Add(new ValidationMessage(
                        "Group must have either 'id' or 'instanceGuid'", path));
                }

                // Validate member references
                foreach (var memberId in group.Members)
                {
                    if (!validIds.Contains(memberId))
                    {
                        result.Errors.Add(new ValidationMessage(
                            $"Group member references non-existent component ID: {memberId}",
                            $"{path}.members"));
                    }
                }
            }
        }

        private static void ValidateSemantics(GhJsonDocument document, ValidationResult result)
        {
            // Check for orphaned components (no connections)
            if (document.Connections != null && document.Connections.Count > 0)
            {
                var connectedIds = new HashSet<int>();
                foreach (var conn in document.Connections)
                {
                    connectedIds.Add(conn.From.Id);
                    connectedIds.Add(conn.To.Id);
                }

                for (var i = 0; i < document.Components.Count; i++)
                {
                    var component = document.Components[i];
                    if (component.Id.HasValue && !connectedIds.Contains(component.Id.Value))
                    {
                        result.Info.Add(new ValidationMessage(
                            $"Component '{component.Name ?? component.ComponentGuid?.ToString() ?? "unknown"}' has no connections",
                            $"components[{i}]"));
                    }
                }
            }
        }
    }
}
