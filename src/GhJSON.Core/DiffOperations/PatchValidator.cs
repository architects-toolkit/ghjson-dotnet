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
using System.Text.Json.Nodes;
using GhJSON.Core.PatchModels;
using GhJSON.Core.Validation;
using Json.Schema;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Validates <see cref="GhPatchDocument"/> instances against the official
    /// <c>ghpatch.schema.json</c>. Supports embedded (offline) and online schema loading
    /// with automatic fallback.
    /// </summary>
    internal static class PatchValidator
    {
        /// <summary>
        /// Validates a patch document against the GhPatch JSON Schema.
        /// </summary>
        /// <param name="patch">The patch to validate.</param>
        /// <param name="preferOnline">When <c>true</c>, attempts to download the schema from the official online repository first, falling back to embedded resources on failure.</param>
        /// <param name="schemaVersion">The schema version to validate against. Defaults to the current version.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult Validate(GhPatchDocument patch, bool preferOnline = false, string? schemaVersion = null)
        {
            var json = PatchSerializer.Serialize(patch);
            return Validate(json, preferOnline, schemaVersion);
        }

        /// <summary>
        /// Validates a patch JSON string against the GhPatch JSON Schema.
        /// </summary>
        /// <param name="json">The patch JSON string.</param>
        /// <param name="preferOnline">When <c>true</c>, attempts to download the schema from the official online repository first, falling back to embedded resources on failure.</param>
        /// <param name="schemaVersion">The schema version to validate against. Defaults to the current version.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult Validate(string json, bool preferOnline = false, string? schemaVersion = null)
        {
            var result = new ValidationResult { IsValid = true };

            JsonNode? instance;
            try
            {
                instance = JsonNode.Parse(json);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationMessage($"Invalid patch JSON: {ex.Message}"));
                result.IsValid = false;
                return result;
            }

            if (instance == null)
            {
                result.Errors.Add(new ValidationMessage("Patch JSON is empty."));
                result.IsValid = false;
                return result;
            }

            JsonSchema schema;
            try
            {
                schema = SchemaLoader.LoadPatchSchema(schemaVersion, preferOnline);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationMessage($"Failed to load patch schema: {ex.Message}"));
                result.IsValid = false;
                return result;
            }

            EvaluateSchema(schema, instance, result);
            result.IsValid = !result.HasErrors;
            return result;
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
                Debug.WriteLine($"[PatchValidator] Schema evaluation threw: {ex}");
                result.Errors.Add(new ValidationMessage($"Schema evaluation error: {ex.Message}"));
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
                    var message = string.IsNullOrWhiteSpace(error.Value)
                        ? $"Schema violation at '{error.Key}'"
                        : error.Value;
                    result.Errors.Add(new ValidationMessage(message, path));
                    emittedAny = true;
                }
            }

            if (!emittedAny)
            {
                result.Errors.Add(new ValidationMessage(
                    "Patch does not conform to the GhPatch schema."));
            }
        }

        private static IEnumerable<EvaluationResults> FlattenDetails(EvaluationResults root, bool skipDescendants = false)
        {
            yield return root;

            if (skipDescendants || root.Details == null)
            {
                yield break;
            }

            bool shouldSkipDescendants = root.IsValid && IsAnyOfOrOneOfKeyword(root);
            foreach (var child in root.Details)
            {
                foreach (var node in FlattenDetails(child, shouldSkipDescendants))
                {
                    yield return node;
                }
            }
        }

        private static bool IsAnyOfOrOneOfKeyword(EvaluationResults result)
        {
            var schemaPath = result.SchemaLocation?.ToString() ?? string.Empty;
            return schemaPath.EndsWith("/anyOf") || schemaPath.EndsWith("/oneOf");
        }
    }
}
