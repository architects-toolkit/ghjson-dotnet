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

using System.IO;
using GhJSON.Core.Migration;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Operations;
using GhJSON.Core.Serialization;
using GhJSON.Core.Validation;
using Newtonsoft.Json;

namespace GhJSON.Core
{
    /// <summary>
    /// Main entry point for GhJSON document operations.
    /// Provides a unified API for reading, writing, validating, fixing, and migrating GhJSON documents.
    /// </summary>
    public static class GhJson
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = { new CompactPositionConverter() }
        };

        #region Read/Write

        /// <summary>
        /// Reads a GhJSON document from a file path.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument Read(string path)
        {
            var json = File.ReadAllText(path);
            return Parse(json);
        }

        /// <summary>
        /// Reads a GhJSON document from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument Read(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return Parse(json);
        }

        /// <summary>
        /// Parses a JSON string into a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument Parse(string json)
        {
            return JsonConvert.DeserializeObject<GhJsonDocument>(json, _serializerSettings)
                ?? new GhJsonDocument();
        }

        /// <summary>
        /// Writes a GhJSON document to a file path.
        /// </summary>
        /// <param name="document">The document to write.</param>
        /// <param name="path">The file path to write to.</param>
        /// <param name="options">Optional write options.</param>
        public static void Write(GhJsonDocument document, string path, WriteOptions? options = null)
        {
            var json = Serialize(document, options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Serializes a GhJSON document to a JSON string.
        /// </summary>
        /// <param name="document">The document to serialize.</param>
        /// <param name="options">Optional write options.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string Serialize(GhJsonDocument document, WriteOptions? options = null)
        {
            options ??= WriteOptions.Default;
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = options.Indented ? Formatting.Indented : Formatting.None,
                Converters = { new CompactPositionConverter() }
            };
            return JsonConvert.SerializeObject(document, settings);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates a GhJSON document.
        /// </summary>
        /// <param name="document">The document to validate.</param>
        /// <returns>A validation result containing errors, warnings, and info messages.</returns>
        public static ValidationResult Validate(GhJsonDocument document)
        {
            var json = Serialize(document);
            return Validate(json);
        }

        /// <summary>
        /// Validates a JSON string as a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>A validation result containing errors, warnings, and info messages.</returns>
        public static ValidationResult Validate(string json)
        {
            return GhJsonValidator.ValidateDetailed(json);
        }

        /// <summary>
        /// Checks if a GhJSON document is valid.
        /// </summary>
        /// <param name="document">The document to check.</param>
        /// <param name="errorMessage">Output parameter for error messages if invalid.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(GhJsonDocument document, out string? errorMessage)
        {
            var json = Serialize(document);
            return GhJsonValidator.Validate(json, out errorMessage);
        }

        /// <summary>
        /// Checks if a JSON string is a valid GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to check.</param>
        /// <param name="errorMessage">Output parameter for error messages if invalid.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(string json, out string? errorMessage)
        {
            return GhJsonValidator.Validate(json, out errorMessage);
        }

        #endregion

        #region Fix/Repair

        /// <summary>
        /// Fixes a GhJSON document by applying all default fix operations.
        /// </summary>
        /// <param name="document">The document to fix.</param>
        /// <param name="options">Optional fix options.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult Fix(GhJsonDocument document, FixOptions? options = null)
        {
            var fixer = new DocumentFixer(options);
            return fixer.Fix(document);
        }

        /// <summary>
        /// Fixes a GhJSON document with minimal changes (IDs only).
        /// </summary>
        /// <param name="document">The document to fix.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult FixMinimal(GhJsonDocument document)
        {
            return DocumentFixer.FixMinimal(document);
        }

        #endregion

        #region Migration

        /// <summary>
        /// Migrates a GhJSON document to the target schema version.
        /// </summary>
        /// <param name="document">The document to migrate.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>A migration result with the migrated document.</returns>
        public static MigrationResult Migrate(GhJsonDocument document, string? targetVersion = null)
        {
            return MigrationPipeline.Default.Migrate(document, targetVersion);
        }

        /// <summary>
        /// Migrates a JSON string to the target schema version.
        /// </summary>
        /// <param name="json">The JSON string to migrate.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>A migration result with the migrated document.</returns>
        public static MigrationResult Migrate(string json, string? targetVersion = null)
        {
            return MigrationPipeline.Default.Migrate(json, targetVersion);
        }

        /// <summary>
        /// Checks if a document needs migration to the target version.
        /// </summary>
        /// <param name="json">The JSON string to check.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>True if migration is needed.</returns>
        public static bool NeedsMigration(string json, string? targetVersion = null)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);
            return MigrationPipeline.Default.NeedsMigration(jObject, targetVersion);
        }

        /// <summary>
        /// Gets the current schema version.
        /// </summary>
        public static string CurrentVersion => MigrationPipeline.CurrentVersion;

        #endregion
    }

    /// <summary>
    /// Options for writing GhJSON documents.
    /// </summary>
    public class WriteOptions
    {
        /// <summary>
        /// Gets the default write options.
        /// </summary>
        public static WriteOptions Default { get; } = new WriteOptions();

        /// <summary>
        /// Gets or sets whether the output should be indented.
        /// </summary>
        public bool Indented { get; set; } = true;

        /// <summary>
        /// Gets or sets whether null values should be included.
        /// </summary>
        public bool IncludeNullValues { get; set; } = false;
    }
}
