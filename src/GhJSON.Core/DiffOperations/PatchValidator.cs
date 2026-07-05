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
            ValidateNoInstanceGuidInAdds(instance, result);
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

        private static IEnumerable<EvaluationResults> FlattenDetails(EvaluationResults root)
        {
            var orderedNodes = new List<EvaluationResults>();
            var seenNodes = new HashSet<EvaluationResults>();
            CollectNodes(root, orderedNodes, seenNodes);

            // In List output format, anyOf/oneOf branches may appear as siblings. If at least
            // one branch is valid, the failing siblings are not useful because the schema is
            // satisfied by another branch; suppress them to avoid misleading errors.
            var suppressedNodes = new HashSet<EvaluationResults>();
            var branchesByParent = orderedNodes
                .Where(IsAnyOfOrOneOfBranch)
                .GroupBy(GetAnyOfOrOneOfParentPath)
                .ToList();

            foreach (var group in branchesByParent)
            {
                if (group.Any(b => b.IsValid))
                {
                    foreach (var branch in group)
                    {
                        suppressedNodes.Add(branch);
                    }
                }
            }

            foreach (var node in orderedNodes)
            {
                if (suppressedNodes.Contains(node))
                {
                    continue;
                }

                yield return node;
            }
        }

        private static void CollectNodes(EvaluationResults node, List<EvaluationResults> orderedNodes, HashSet<EvaluationResults> seenNodes)
        {
            if (!seenNodes.Add(node))
            {
                return;
            }

            orderedNodes.Add(node);

            if (node.Details == null)
            {
                return;
            }

            foreach (var child in node.Details)
            {
                CollectNodes(child, orderedNodes, seenNodes);
            }
        }

        private static bool IsAnyOfOrOneOfBranch(EvaluationResults result)
        {
            var schemaPath = result.SchemaLocation?.ToString() ?? string.Empty;
            return AnyOfOrOneOfBranchPattern.IsMatch(schemaPath);
        }

        private static string GetAnyOfOrOneOfParentPath(EvaluationResults result)
        {
            var schemaPath = result.SchemaLocation?.ToString() ?? string.Empty;
            var match = AnyOfOrOneOfBranchPattern.Match(schemaPath);
            return match.Success ? schemaPath.Substring(0, match.Index) : schemaPath;
        }

        private static void ValidateNoInstanceGuidInAdds(JsonNode? instance, ValidationResult result)
        {
            if (instance is not JsonObject root ||
                !root.TryGetPropertyValue("patch", out var patchNode) ||
                patchNode is not JsonObject patch)
            {
                return;
            }

            if (patch.TryGetPropertyValue("components", out var componentsNode) &&
                componentsNode is JsonObject components &&
                components.TryGetPropertyValue("add", out var componentsAddNode) &&
                componentsAddNode is JsonArray componentsAdd)
            {
                for (int i = 0; i < componentsAdd.Count; i++)
                {
                    if (componentsAdd[i] is JsonObject obj && obj.ContainsKey("instanceGuid"))
                    {
                        result.Errors.Add(new ValidationMessage(
                            "New components in 'patch.components.add' must not specify 'instanceGuid'; it is generated when the component is placed on the canvas.",
                            $"patch.components.add[{i}]"));
                    }
                }
            }

            if (patch.TryGetPropertyValue("groups", out var groupsNode) &&
                groupsNode is JsonObject groups &&
                groups.TryGetPropertyValue("add", out var groupsAddNode) &&
                groupsAddNode is JsonArray groupsAdd)
            {
                for (int i = 0; i < groupsAdd.Count; i++)
                {
                    if (groupsAdd[i] is JsonObject obj && obj.ContainsKey("instanceGuid"))
                    {
                        result.Errors.Add(new ValidationMessage(
                            "New groups in 'patch.groups.add' must not specify 'instanceGuid'; it is generated when the group is placed on the canvas.",
                            $"patch.groups.add[{i}]"));
                    }
                }
            }
        }

        private static readonly System.Text.RegularExpressions.Regex AnyOfOrOneOfBranchPattern =
            new System.Text.RegularExpressions.Regex(
                @"/(anyOf|oneOf)/\d+$",
                System.Text.RegularExpressions.RegexOptions.Compiled);
    }
}
