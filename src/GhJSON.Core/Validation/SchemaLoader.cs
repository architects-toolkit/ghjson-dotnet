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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Json.Schema;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Loads the GhJSON v1.0 JSON Schema bundle (main schema + extension registry +
    /// per-extension schemas) from embedded resources and registers every schema in the
    /// global <see cref="SchemaRegistry"/> by its <c>$id</c> so that cross-file
    /// <c>$ref</c>s resolve offline.
    /// <para>
    /// The embedded snapshot under <c>Validation/Schemas/v1.0/</c> is kept in sync with the
    /// published spec by <c>tools/Sync-Schemas.ps1</c> (see the <c>sync-schema</c>
    /// workflow). At build time the committed snapshot is authoritative — no network
    /// access is required at runtime.
    /// </para>
    /// </summary>
    public static class SchemaLoader
    {
        /// <summary>
        /// Logical namespace prefix under which schema resources are embedded. MSBuild
        /// transforms <c>Validation\Schemas\v1.0\…\*.json</c> into manifest names that
        /// start with this prefix (dots replace path separators, and the <c>1.0</c>
        /// segment becomes <c>1._0</c> because the dot is treated as a separator).
        /// </summary>
        internal const string EmbeddedResourcePrefix = "GhJSON.Core.Validation.Schemas.v1._0.";

        private const string MainSchemaFileName = "ghjson.schema.json";

        private static readonly Lazy<Bundle> LazyBundle =
            new Lazy<Bundle>(Load, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the compiled main GhJSON schema. First access triggers the one-time load.
        /// </summary>
        public static JsonSchema MainSchema => LazyBundle.Value.Main;

        /// <summary>
        /// Gets the <c>$id</c> URIs of every schema registered by the loader (main +
        /// extension registry + per-extension schemas).
        /// </summary>
        public static IReadOnlyList<Uri> RegisteredSchemaIds => LazyBundle.Value.Ids;

        private static Bundle Load()
        {
            var assembly = typeof(SchemaLoader).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(EmbeddedResourcePrefix, StringComparison.Ordinal)
                            && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (resourceNames.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No embedded GhJSON schema resources found under '{EmbeddedResourcePrefix}*.json'. " +
                    "Run tools/Sync-Schemas.ps1 and rebuild.");
            }

            JsonSchema? main = null;
            var ids = new List<Uri>();

            foreach (var resourceName in resourceNames)
            {
                var text = ReadResource(assembly, resourceName);
                var schema = JsonSchema.FromText(text);

                var id = schema.GetId();
                if (id == null)
                {
                    Debug.WriteLine(
                        $"[SchemaLoader] Skipping '{resourceName}': no $id present.");
                    continue;
                }

                // Register globally so $ref resolution works during evaluation, regardless
                // of which EvaluationOptions instance the caller uses.
                SchemaRegistry.Global.Register(id, schema);
                ids.Add(id);

                if (resourceName.EndsWith(MainSchemaFileName, StringComparison.OrdinalIgnoreCase))
                {
                    main = schema;
                }
            }

            if (main == null)
            {
                throw new InvalidOperationException(
                    $"Main schema resource '{MainSchemaFileName}' not found among embedded resources.");
            }

            Debug.WriteLine(
                $"[SchemaLoader] Registered {ids.Count} GhJSON schema(s) from embedded resources.");
            return new Bundle(main, ids);
        }

        private static string ReadResource(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource stream '{resourceName}' could not be opened.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private sealed class Bundle
        {
            public Bundle(JsonSchema main, IReadOnlyList<Uri> ids)
            {
                this.Main = main;
                this.Ids = ids;
            }

            public JsonSchema Main { get; }

            public IReadOnlyList<Uri> Ids { get; }
        }
    }
}
