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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Json.Schema;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Loads the GhJSON JSON Schema bundle (main schema + extension registry +
    /// per-extension schemas) from embedded resources or from the official online
    /// repository. Loaded schemas are collected in a <see cref="Bundle"/> and can be
    /// registered into an <see cref="EvaluationOptions.SchemaRegistry"/> for isolated
    /// evaluation, eliminating race conditions from shared global state.
    /// <para>
    /// The embedded snapshot under <c>Validation/Schemas/v{version}/</c> is kept in
    /// sync with the published spec by <c>tools/Sync-Schemas.ps1</c>. By default the
    /// loader uses embedded resources so that no network access is required at runtime.
    /// When <see cref="SchemaLoaderOptions.PreferOnline"/> is set, the loader attempts to
    /// fetch the latest schema from the web and falls back to the embedded snapshot on
    /// failure.
    /// </para>
    /// </summary>
    public static class SchemaLoader
    {
        /// <summary>
        /// The default schema version used when none is specified.
        /// </summary>
        public const string DefaultVersion = "1.0";

        private const string MainSchemaFileName = "ghjson.schema.json";
        private const string PatchSchemaFileName = "ghpatch.schema.json";

        private static readonly Lazy<Bundle> LazyDefaultBundle =
            new Lazy<Bundle>(() => LoadEmbedded(DefaultVersion), LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly ConcurrentDictionary<string, Bundle> EmbeddedBundleCache =
            new ConcurrentDictionary<string, Bundle>();

        private static readonly ConcurrentDictionary<string, JsonSchema> PatchSchemaCache =
            new ConcurrentDictionary<string, JsonSchema>();

        private static readonly Lazy<HttpClient> LazyHttpClient = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var uaVersion = typeof(SchemaLoader).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? DefaultVersion;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GhJSON", uaVersion));
            return client;
        });

        /// <summary>
        /// Gets the compiled main GhJSON schema for the default version.
        /// First access triggers the one-time load from embedded resources.
        /// </summary>
        public static JsonSchema MainSchema => LazyDefaultBundle.Value.Main;

        /// <summary>
        /// Gets the <c>$id</c> URIs of every schema registered for the default version
        /// (main + extension registry + per-extension schemas).
        /// </summary>
        public static IReadOnlyList<Uri> RegisteredSchemaIds => LazyDefaultBundle.Value.Ids;

        /// <summary>
        /// Loads the schema bundle using the provided options.
        /// </summary>
        /// <param name="options">Options controlling version selection and online behavior.</param>
        /// <returns>The loaded schema bundle.</returns>
        public static Bundle Load(SchemaLoaderOptions options)
        {
            var effectiveVersion = string.IsNullOrEmpty(options.Version) ? DefaultVersion : options.Version!;

            if (!options.PreferOnline)
            {
                if (effectiveVersion == DefaultVersion)
                {
                    return LazyDefaultBundle.Value;
                }

                return EmbeddedBundleCache.GetOrAdd(effectiveVersion, LoadEmbedded);
            }

            try
            {
                return LoadOnline(effectiveVersion, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchemaLoader] Online load failed, falling back to embedded: {ex.Message}");
                if (effectiveVersion == DefaultVersion)
                {
                    return LazyDefaultBundle.Value;
                }

                return EmbeddedBundleCache.GetOrAdd(effectiveVersion, LoadEmbedded);
            }
        }

        /// <summary>
        /// Async version of <see cref="Load(SchemaLoaderOptions)"/>.
        /// </summary>
        /// <param name="options">Options controlling version selection and online behavior.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded schema bundle.</returns>
        public static async Task<Bundle> LoadAsync(SchemaLoaderOptions options, CancellationToken cancellationToken = default)
        {
            var effectiveVersion = string.IsNullOrEmpty(options.Version) ? DefaultVersion : options.Version!;

            if (!options.PreferOnline)
            {
                if (effectiveVersion == DefaultVersion)
                {
                    return LazyDefaultBundle.Value;
                }

                return EmbeddedBundleCache.GetOrAdd(effectiveVersion, LoadEmbedded);
            }

            try
            {
                return await LoadOnline(effectiveVersion, options, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchemaLoader] Online load failed, falling back to embedded: {ex.Message}");
                if (effectiveVersion == DefaultVersion)
                {
                    return LazyDefaultBundle.Value;
                }

                return EmbeddedBundleCache.GetOrAdd(effectiveVersion, LoadEmbedded);
            }
        }

        /// <summary>
        /// Loads the GhPatch JSON Schema for the specified version.
        /// When <paramref name="preferOnline"/> is <c>true</c>, attempts to download from
        /// the official online repository first, falling back to embedded resources on failure.
        /// </summary>
        /// <param name="version">The schema version (e.g. "1.0"). Defaults to <see cref="DefaultVersion"/>.</param>
        /// <param name="preferOnline">Prefer online schema over embedded snapshot.</param>
        /// <returns>The loaded patch schema.</returns>
        public static JsonSchema LoadPatchSchema(string? version = null, bool preferOnline = false)
        {
            var effectiveVersion = string.IsNullOrEmpty(version) ? DefaultVersion : version!;

            if (!preferOnline)
            {
                return PatchSchemaCache.GetOrAdd(effectiveVersion, LoadPatchSchemaEmbedded);
            }

            try
            {
                return LoadPatchSchemaOnline(effectiveVersion, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchemaLoader] Online patch load failed, falling back to embedded: {ex.Message}");
                return PatchSchemaCache.GetOrAdd(effectiveVersion, LoadPatchSchemaEmbedded);
            }
        }

        /// <summary>
        /// Async version of <see cref="LoadPatchSchema"/>.
        /// </summary>
        /// <param name="version">The schema version (e.g. "1.0"). Defaults to <see cref="DefaultVersion"/>.</param>
        /// <param name="preferOnline">Prefer online schema over embedded snapshot.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded patch schema.</returns>
        public static async Task<JsonSchema> LoadPatchSchemaAsync(string? version = null, bool preferOnline = false, CancellationToken cancellationToken = default)
        {
            var effectiveVersion = string.IsNullOrEmpty(version) ? DefaultVersion : version!;

            if (!preferOnline)
            {
                return PatchSchemaCache.GetOrAdd(effectiveVersion, LoadPatchSchemaEmbedded);
            }

            try
            {
                return await LoadPatchSchemaOnline(effectiveVersion, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchemaLoader] Online patch load failed, falling back to embedded: {ex.Message}");
                return PatchSchemaCache.GetOrAdd(effectiveVersion, LoadPatchSchemaEmbedded);
            }
        }

        /// <summary>
        /// Returns the embedded resource prefix for the given version.
        /// MSBuild transforms path separators to dots, and dots in folder names become
        /// <c>._digit</c> (e.g. v1.0 becomes v1._0).
        /// </summary>
        internal static string GetEmbeddedResourcePrefix(string version)
        {
            var safeVersion = version.Replace(".", "._");
            return $"GhJSON.Core.Validation.Schemas.v{safeVersion}.";
        }

        /// <summary>
        /// Loads the schema bundle from embedded resources.
        /// </summary>
        private static Bundle LoadEmbedded(string version)
        {
            var prefix = GetEmbeddedResourcePrefix(version);
            var assembly = typeof(SchemaLoader).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix, StringComparison.Ordinal)
                            && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (resourceNames.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No embedded GhJSON schema resources found for version '{version}' under '{prefix}*.json'. " +
                    "Run tools/Sync-Schemas.ps1 and rebuild.");
            }

            JsonSchema? main = null;
            var ids = new List<Uri>();
            var schemas = new Dictionary<Uri, JsonSchema>();

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

                schemas[id] = schema;
                ids.Add(id);

                if (resourceName.EndsWith(MainSchemaFileName, StringComparison.OrdinalIgnoreCase))
                {
                    main = schema;
                }
            }

            if (main == null)
            {
                throw new InvalidOperationException(
                    $"Main schema resource '{MainSchemaFileName}' not found for version '{version}'.");
            }

            Debug.WriteLine(
                $"[SchemaLoader] Loaded {ids.Count} GhJSON schema(s) from embedded resources (version {version}).");
            return new Bundle(main, ids, schemas);
        }

        private static JsonSchema LoadPatchSchemaEmbedded(string version)
        {
            var prefix = GetEmbeddedResourcePrefix(version);
            var resourceName = prefix + PatchSchemaFileName;
            var assembly = typeof(SchemaLoader).Assembly;
            var text = ReadResource(assembly, resourceName);
            Debug.WriteLine($"[SchemaLoader] Loaded patch schema from embedded resources (version {version}).");
            return JsonSchema.FromText(text);
        }

        private static async Task<JsonSchema> LoadPatchSchemaOnline(string version, CancellationToken cancellationToken)
        {
            var baseUrl = "https://architects-toolkit.github.io/ghjson-spec/schema/".TrimEnd('/');
            var versionUrl = $"{baseUrl}/v{version}/";
            var httpClient = LazyHttpClient.Value;

            var patchUrl = versionUrl + PatchSchemaFileName;
            try
            {
                using var response = await httpClient.GetAsync(patchUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var patchText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[SchemaLoader] Loaded patch schema from online (version {version}).");
                return JsonSchema.FromText(patchText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchemaLoader] Failed to download patch schema from {patchUrl}: {ex.Message}");
                throw;
            }
        }

        private static async Task<Bundle> LoadOnline(string version, SchemaLoaderOptions options, CancellationToken cancellationToken)
        {
            var baseUrl = options.BaseUrl.TrimEnd('/');
            var versionUrl = $"{baseUrl}/v{version}/";

            using var timeoutCts = new CancellationTokenSource(options.OnlineTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var linkedToken = linkedCts.Token;
            var httpClient = LazyHttpClient.Value;

            var mainUrl = versionUrl + MainSchemaFileName;
            string mainText;

            try
            {
                using var response = await httpClient.GetAsync(mainUrl, linkedToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                mainText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchemaLoader] Failed to download main schema from {mainUrl}: {ex.Message}");
                throw;
            }

            var main = JsonSchema.FromText(mainText);
            var ids = new List<Uri>();
            var schemas = new Dictionary<Uri, JsonSchema>();

            var id = main.GetId();
            if (id != null)
            {
                schemas[id] = main;
                ids.Add(id);
            }

            // Discover and download extension schemas referenced by the main schema
            var extensionSchemas = await DiscoverAndDownloadExtensionsAsync(versionUrl, linkedToken).ConfigureAwait(false);
            foreach (var kvp in extensionSchemas)
            {
                schemas[kvp.Key] = kvp.Value;
                ids.Add(kvp.Key);
            }

            Debug.WriteLine(
                $"[SchemaLoader] Loaded {ids.Count} GhJSON schema(s) from online (version {version}).");
            return new Bundle(main, ids, schemas);
        }

        private static async Task<Dictionary<Uri, JsonSchema>> DiscoverAndDownloadExtensionsAsync(
            string baseUrl, CancellationToken cancellationToken)
        {
            var schemas = new Dictionary<Uri, JsonSchema>();
            var httpClient = LazyHttpClient.Value;

            // Discover extension schemas from the extensions registry
            var registryUrl = baseUrl + "extensions/extensions.schema.json";
            string registryText;
            try
            {
                using var response = await httpClient.GetAsync(registryUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                registryText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return schemas;
            }

            var registrySchema = JsonSchema.FromText(registryText);
            var registryId = registrySchema.GetId();
            if (registryId != null)
            {
                schemas[registryId] = registrySchema;
            }

            // Parse registry to find $refs
            var refs = ExtractRefsFromRegistry(registryText);
            foreach (var refPath in refs)
            {
                var clean = refPath.StartsWith("./", StringComparison.Ordinal) ? refPath.Substring(2) : refPath;
                if (clean.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var extUrl = baseUrl + "extensions/" + clean;
                try
                {
                    using var response = await httpClient.GetAsync(extUrl, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var extText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var extSchema = JsonSchema.FromText(extText);
                    var extId = extSchema.GetId();
                    if (extId != null)
                    {
                        schemas[extId] = extSchema;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SchemaLoader] Failed to download extension {extUrl}: {ex.Message}");
                }
            }

            return schemas;
        }

        private static List<string> ExtractRefsFromRegistry(string registryText)
        {
            var refs = new List<string>();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(registryText);
                if (doc.RootElement.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                    {
                        if (prop.Value.TryGetProperty("$ref", out var refProp))
                        {
                            var refValue = refProp.GetString();
                            if (!string.IsNullOrEmpty(refValue))
                            {
                                refs.Add(refValue);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Defensive: malformed registry shouldn't stop validation.
            }

            return refs;
        }

        private static string ReadResource(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource stream '{resourceName}' could not be opened.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Represents a loaded schema bundle.
        /// </summary>
        public sealed class Bundle
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Bundle"/> class.
            /// </summary>
            /// <param name="main">The main GhJSON schema.</param>
            /// <param name="ids">The registered schema IDs.</param>
            /// <param name="schemas">All loaded schemas keyed by their <c>$id</c> URI.</param>
            public Bundle(JsonSchema main, IReadOnlyList<Uri> ids, IReadOnlyDictionary<Uri, JsonSchema> schemas)
            {
                this.Main = main;
                this.Ids = ids;
                this.Schemas = schemas;
            }

            /// <summary>
            /// The main GhJSON schema.
            /// </summary>
            public JsonSchema Main { get; }

            /// <summary>
            /// The <c>$id</c> URIs of every loaded schema.
            /// </summary>
            public IReadOnlyList<Uri> Ids { get; }

            /// <summary>
            /// All loaded schemas keyed by their <c>$id</c> URI.
            /// </summary>
            public IReadOnlyDictionary<Uri, JsonSchema> Schemas { get; }
        }
    }
}
