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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Json.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Utilities to validate GhJSON format.
    /// </summary>
    public static class GhJsonValidator
    {
        private const string DefaultOfficialSchemaUrl = "https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json";

        private const string EmbeddedSchemaResourceName = "GhJSON.Core.Validation.ghjson.schema.v1.0.ghjson.schema.json";

        private static readonly Lazy<JsonSchema> EmbeddedOfficialSchema = new Lazy<JsonSchema>(() =>
            JsonSchema.FromText(ReadEmbeddedSchemaJson()));

        private static readonly Lazy<HttpClient> Http = new Lazy<HttpClient>(() => new HttpClient());

        private static readonly object SchemaLock = new object();
        private static JsonSchema? _cachedOfficialSchema;

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

            // 1) Validate against the official JSON Schema (source of truth)
            if (root != null)
            {
                ValidateAgainstOfficialSchema(json, errors);
            }

            // 2) Semantic checks not expressible in JSON Schema: connection endpoints must reference existing component IDs.
            // Only run semantic checks if schema validation passed.
            if (root != null && !errors.Any())
            {
                ValidateConnectionReferences(root, errors);
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

        private static void ValidateAgainstOfficialSchema(string instanceJson, List<string> errors)
        {
            try
            {
                var schema = GetOfficialSchema();

                var instance = JsonNode.Parse(instanceJson);
                if (instance == null)
                {
                    errors.Add("Schema validation failed: Instance JSON parsed to null.");
                    return;
                }

                var evaluation = schema.Evaluate(instance, new EvaluationOptions
                {
                    OutputFormat = OutputFormat.Hierarchical,
                    RequireFormatValidation = true
                });

                if (!evaluation.IsValid)
                {
                    CollectSchemaErrors(evaluation, errors);
                }
            }
            catch (Exception ex)
            {
                // If schema validation infrastructure fails, treat it as an error (otherwise we'd silently accept invalid docs).
                errors.Add($"Schema validation failed: {ex.Message}");
            }
        }

        private static JsonSchema GetOfficialSchema()
        {
            lock (SchemaLock)
            {
                if (_cachedOfficialSchema != null)
                {
                    return _cachedOfficialSchema;
                }

                // Try to fetch the official schema, but keep this deterministic by using a short timeout.
                // If the fetch fails, we fall back to an embedded copy of the v1.0 schema.
                try
                {
                    var schemaUrl = GetOfficialSchemaUrlFromAssemblyMetadata() ?? DefaultOfficialSchemaUrl;
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var response = Http.Value.GetAsync(schemaUrl, cts.Token).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    var schemaJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    _cachedOfficialSchema = JsonSchema.FromText(schemaJson);
                }
                catch
                {
                    _cachedOfficialSchema = EmbeddedOfficialSchema.Value;
                }

                return _cachedOfficialSchema;
            }
        }

        private static string? GetOfficialSchemaUrlFromAssemblyMetadata()
        {
            try
            {
                var assembly = typeof(GhJsonValidator).Assembly;
                var attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), inherit: false);
                foreach (var attrObj in attributes)
                {
                    if (attrObj is AssemblyMetadataAttribute attr &&
                        string.Equals(attr.Key, "GhJsonOfficialSchemaUrl", StringComparison.OrdinalIgnoreCase))
                    {
                        return string.IsNullOrWhiteSpace(attr.Value) ? null : attr.Value;
                    }
                }
            }
            catch
            {
                // ignore and fall back
            }

            return null;
        }

        private static string ReadEmbeddedSchemaJson()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedSchemaResourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded schema resource not found: '{EmbeddedSchemaResourceName}'.");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void CollectSchemaErrors(EvaluationResults evaluation, List<string> errors)
        {
            if (evaluation.Errors != null)
            {
                foreach (var kv in evaluation.Errors)
                {
                    errors.Add($"Schema: {evaluation.InstanceLocation} - {kv.Key}: {kv.Value}");
                }
            }

            if (evaluation.Details == null)
            {
                return;
            }

            foreach (var detail in evaluation.Details)
            {
                CollectSchemaErrors(detail, errors);
            }
        }

        private static void ValidateConnectionReferences(JObject root, List<string> errors)
        {
            if (!(root["components"] is JArray components))
            {
                return;
            }

            // Treat missing 'connections' as empty array (schema allows it)
            var connections = root["connections"] as JArray ?? new JArray();

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

                    if (ep["id"]?.Type != JTokenType.Integer)
                    {
                        // Schema already validates this; this is defensive.
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
    }
}
