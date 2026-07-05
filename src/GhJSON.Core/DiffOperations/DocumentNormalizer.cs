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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Produces a normalised representation of a GhJSON document for diffing and checksumming.
    /// </summary>
    /// <remarks>
    /// Normalisation removes volatile fields (runtime messages, metadata counters/timestamps),
    /// sorts the components / connections / groups arrays by stable identity, and emits
    /// deterministic JSON (no formatting, ignored nulls).
    /// </remarks>
    internal static class DocumentNormalizer
    {
        private static readonly JsonSerializerSettings BaseSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            Converters = { new PivotConverter() },
        };

        /// <summary>
        /// Convert the document to a normalised JObject tree.
        /// </summary>
        public static JObject Normalize(GhJsonDocument document, DiffOptions options)
        {
            var raw = JObject.FromObject(document, JsonSerializer.Create(BaseSettings));

            // Metadata
            if (raw["metadata"] is JObject metadata)
            {
                if (options.IgnoreMetadataCounters)
                {
                    metadata.Remove("componentCount");
                    metadata.Remove("connectionCount");
                    metadata.Remove("groupCount");
                }

                if (options.IgnoreMetadataTimestamps)
                {
                    metadata.Remove("created");
                    metadata.Remove("modified");
                }

                if (metadata.Count == 0)
                {
                    raw.Remove("metadata");
                }
            }

            // Components
            if (raw["components"] is JArray components)
            {
                foreach (var token in components.OfType<JObject>())
                {
                    if (options.IgnoreRuntimeMessages)
                    {
                        token.Remove("errors");
                        token.Remove("warnings");
                        token.Remove("remarks");
                    }

                    if (options.IgnorePivots)
                    {
                        token.Remove("pivot");
                    }
                }
            }

            // Connections — order is non-semantic; sort canonically.
            if (raw["connections"] is JArray connections)
            {
                var sorted = new JArray(connections
                    .OfType<JObject>()
                    .OrderBy(c => (int?)c["from"]?["id"] ?? 0)
                    .ThenBy(c => (string?)c["from"]?["paramName"] ?? string.Empty)
                    .ThenBy(c => (int?)c["from"]?["paramIndex"] ?? -1)
                    .ThenBy(c => (int?)c["to"]?["id"] ?? 0)
                    .ThenBy(c => (string?)c["to"]?["paramName"] ?? string.Empty)
                    .ThenBy(c => (int?)c["to"]?["paramIndex"] ?? -1)
                    .Cast<JToken>()
                    .ToArray());
                raw["connections"] = sorted;
            }

            // Groups — sort canonically by instanceGuid then id.
            if (raw["groups"] is JArray groups)
            {
                var sorted = new JArray(groups
                    .OfType<JObject>()
                    .OrderBy(g => (string?)g["instanceGuid"] ?? string.Empty)
                    .ThenBy(g => (int?)g["id"] ?? int.MaxValue)
                    .Cast<JToken>()
                    .ToArray());

                foreach (var group in sorted.OfType<JObject>())
                {
                    if (group["members"] is JArray members)
                    {
                        var orderedMembers = new JArray(members
                            .OfType<JValue>()
                            .OrderBy(v => (int?)v ?? 0)
                            .Cast<JToken>()
                            .ToArray());
                        group["members"] = orderedMembers;
                    }
                }

                raw["groups"] = sorted;
            }

            // Components — sort by stable identity (instanceGuid > id) so the doc shape
            // is deterministic. This does NOT affect semantic equivalence checks; it only
            // produces a stable normalised form for checksums.
            if (raw["components"] is JArray comps2)
            {
                var sorted = new JArray(comps2
                    .OfType<JObject>()
                    .OrderBy(c => (string?)c["instanceGuid"] ?? string.Empty)
                    .ThenBy(c => (int?)c["id"] ?? int.MaxValue)
                    .ThenBy(c => (string?)c["componentGuid"] ?? string.Empty)
                    .Cast<JToken>()
                    .ToArray());
                raw["components"] = sorted;
            }

            return SortObjectKeys(raw);
        }

        /// <summary>
        /// Stable canonical JSON string used for checksum input.
        /// </summary>
        public static string ToCanonicalJson(JObject normalised)
        {
            return JsonConvert.SerializeObject(normalised, BaseSettings);
        }

        /// <summary>
        /// Compute the canonical sha256 checksum for a document.
        /// </summary>
        public static string ComputeChecksum(GhJsonDocument document, DiffOptions options)
        {
            var normalised = Normalize(document, options);
            var canonical = ToCanonicalJson(normalised);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var sb = new StringBuilder("sha256-", 7 + bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private static JObject SortObjectKeys(JObject obj)
        {
            var result = new JObject();
            foreach (var prop in obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                var value = prop.Value;
                if (value is JObject child)
                {
                    result[prop.Name] = SortObjectKeys(child);
                }
                else if (value is JArray array)
                {
                    result[prop.Name] = SortArrayChildren(array);
                }
                else
                {
                    result[prop.Name] = value;
                }
            }

            return result;
        }

        private static JArray SortArrayChildren(JArray array)
        {
            var result = new JArray();
            foreach (var item in array)
            {
                if (item is JObject child)
                {
                    result.Add(SortObjectKeys(child));
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }
    }
}
