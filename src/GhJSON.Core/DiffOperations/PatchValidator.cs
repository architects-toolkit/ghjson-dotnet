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
using GhJSON.Core.PatchModels;
using GhJSON.Core.Validation;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Structural validation for <see cref="GhPatchDocument"/> instances.
    /// </summary>
    internal static class PatchValidator
    {
        public static ValidationResult Validate(GhPatchDocument patch)
        {
            var result = new ValidationResult { IsValid = true };

            if (!string.Equals(patch.Kind, "ghpatch", StringComparison.Ordinal))
            {
                result.Errors.Add(new ValidationMessage($"Patch kind must be 'ghpatch', got '{patch.Kind}'.", "kind"));
            }

            if (patch.Patch == null)
            {
                result.Errors.Add(new ValidationMessage("Patch body is missing.", "patch"));
                result.IsValid = false;
                return result;
            }

            ValidateComponentsOp(patch.Patch.Components, result);
            ValidateConnectionsOp(patch.Patch.Connections, result);
            ValidateGroupsOp(patch.Patch.Groups, result);

            result.IsValid = !result.HasErrors;
            return result;
        }

        public static ValidationResult Validate(string json)
        {
            try
            {
                var patch = PatchSerializer.Deserialize(json);
                return Validate(patch);
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Invalid patch JSON: {ex.Message}");
            }
        }

        private static void ValidateComponentsOp(GhPatchComponentsOp? op, ValidationResult result)
        {
            if (op == null)
            {
                return;
            }

            if (op.Modify != null)
            {
                for (var i = 0; i < op.Modify.Count; i++)
                {
                    var modify = op.Modify[i];
                    if (!HasAnyIdentity(modify.Match))
                    {
                        result.Errors.Add(new ValidationMessage(
                            "Component modify must have at least one identity field in 'match'.",
                            $"patch.components.modify[{i}].match"));
                    }
                }
            }

            if (op.Remove != null)
            {
                for (var i = 0; i < op.Remove.Count; i++)
                {
                    if (!HasAnyIdentity(op.Remove[i]))
                    {
                        result.Errors.Add(new ValidationMessage(
                            "Component remove must have at least one identity field.",
                            $"patch.components.remove[{i}]"));
                    }
                }
            }
        }

        private static void ValidateConnectionsOp(GhPatchConnectionsOp? op, ValidationResult result)
        {
            if (op == null)
            {
                return;
            }

            // Each connection must have both 'from' and 'to' set with at least an id.
            void Check(System.Collections.Generic.List<SchemaModels.GhJsonConnection>? list, string path)
            {
                if (list == null)
                {
                    return;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    var connection = list[i];
                    if (connection.From == null || connection.To == null)
                    {
                        result.Errors.Add(new ValidationMessage($"Connection must have both 'from' and 'to'.", $"{path}[{i}]"));
                    }
                }
            }

            Check(op.Add, "patch.connections.add");
            Check(op.Remove, "patch.connections.remove");
        }

        private static void ValidateGroupsOp(GhPatchGroupsOp? op, ValidationResult result)
        {
            if (op == null)
            {
                return;
            }

            if (op.Modify != null)
            {
                for (var i = 0; i < op.Modify.Count; i++)
                {
                    var modify = op.Modify[i];
                    if (!HasAnyIdentity(modify.Match))
                    {
                        result.Errors.Add(new ValidationMessage(
                            "Group modify must have at least one identity field in 'match'.",
                            $"patch.groups.modify[{i}].match"));
                    }
                }
            }

            if (op.Remove != null)
            {
                for (var i = 0; i < op.Remove.Count; i++)
                {
                    if (!HasAnyIdentity(op.Remove[i]))
                    {
                        result.Errors.Add(new ValidationMessage(
                            "Group remove must have at least one identity field.",
                            $"patch.groups.remove[{i}]"));
                    }
                }
            }
        }

        private static bool HasAnyIdentity(GhPatchComponentMatch match)
        {
            return (match.InstanceGuid.HasValue && match.InstanceGuid != Guid.Empty)
                || match.Id.HasValue
                || match.ComponentGuid.HasValue
                || !string.IsNullOrEmpty(match.Name);
        }

        private static bool HasAnyIdentity(GhPatchGroupMatch match)
        {
            return (match.InstanceGuid.HasValue && match.InstanceGuid != Guid.Empty)
                || match.Id.HasValue;
        }
    }
}
