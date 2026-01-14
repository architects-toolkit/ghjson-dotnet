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
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Validates GhJSON documents against the schema and performs structural validation.
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
        /// Validates a JSON string as a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult Validate(string json, ValidationLevel level = ValidationLevel.Standard)
        {
            try
            {
                var document = Serialization.GhJsonSerializer.Deserialize(json);
                return Validate(document, level);
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Invalid JSON: {ex.Message}");
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
