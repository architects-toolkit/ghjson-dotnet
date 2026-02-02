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
using System.Diagnostics;
using System.IO;
using System.Linq;
using GhJSON.Core.FixOperations;
using GhJSON.Core.MergeOperations;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.SchemaMigration;
using GhJSON.Core.Serialization;
using GhJSON.Core.Validation;

namespace GhJSON.Core
{
    /// <summary>
    /// Main entry point for GhJSON document operations.
    /// Provides a unified API for reading, writing, validating, fixing, and migrating GhJSON documents.
    /// </summary>
    public static partial class GhJson
    {
        /// <summary>
        /// Gets the current schema version.
        /// </summary>
        public static string CurrentVersion => SchemaMigrator.CurrentVersion;

        #region Document Creation

        /// <summary>
        /// Creates a new fluent builder for immutable GhJSON document creation.
        /// </summary>
        /// <returns>A new builder instance.</returns>
        public static DocumentBuilder CreateDocumentBuilder()
        {
            return DocumentBuilder.Create();
        }

        /// <summary>
        /// Creates a new fluent builder initialized from an existing document.
        /// </summary>
        /// <param name="document">Source document to copy data from.</param>
        /// <returns>A new builder instance initialized from the given document.</returns>
        public static DocumentBuilder CreateDocumentBuilder(GhJsonDocument document)
        {
            return DocumentBuilder.FromImmutable(document);
        }

        /// <summary>
        /// Creates a new metadata builder.
        /// </summary>
        /// <returns>A new metadata instance.</returns>
        public static GhJsonMetadata CreateMetadataProperty()
        {
            return new GhJsonMetadata();
        }

        /// <summary>
        /// Creates a new component builder.
        /// </summary>
        /// <returns>A new component instance.</returns>
        public static GhJsonComponent CreateComponentObject()
        {
            return new GhJsonComponent();
        }

        /// <summary>
        /// Creates a new parameter settings builder.
        /// </summary>
        /// <returns>A new parameter settings instance.</returns>
        public static GhJsonParameterSettings CreateComponentParameterObject()
        {
            return new GhJsonParameterSettings();
        }

        /// <summary>
        /// Creates a new component state builder.
        /// </summary>
        /// <returns>A new component state instance.</returns>
        public static GhJsonComponentState CreateComponentStateObject()
        {
            return new GhJsonComponentState();
        }

        /// <summary>
        /// Creates a new connection builder.
        /// </summary>
        /// <returns>A new connection instance.</returns>
        public static GhJsonConnection CreateConnectionObject()
        {
            return new GhJsonConnection();
        }

        /// <summary>
        /// Creates a new connection endpoint builder.
        /// </summary>
        /// <returns>A new connection endpoint instance.</returns>
        public static GhJsonConnectionEndpoint CreateConnectionEndpointObject()
        {
            return new GhJsonConnectionEndpoint();
        }

        /// <summary>
        /// Creates a new group builder.
        /// </summary>
        /// <returns>A new group instance.</returns>
        public static GhJsonGroup CreateGroupObject()
        {
            return new GhJsonGroup();
        }

        #endregion

        #region Input/Output

        /// <summary>
        /// Reads a GhJSON document from a file path.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument FromFile(string path)
        {
            var json = File.ReadAllText(path);
            return FromJson(json);
        }

        /// <summary>
        /// Reads a GhJSON document from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument FromStream(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return FromJson(json);
        }

        /// <summary>
        /// Parses a JSON string into a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument FromJson(string json)
        {
#if DEBUG
            Debug.WriteLine($"[GhJson.FromJson] Deserializing JSON, length: {json?.Length ?? 0}");
#endif
            var result = Serialization.GhJsonSerializer.Deserialize(json);
#if DEBUG
            Debug.WriteLine($"[GhJson.FromJson] Deserialized document with {result.Components?.Count ?? 0} components");
#endif
            return result;
        }

        /// <summary>
        /// Writes a GhJSON document to a file path.
        /// </summary>
        /// <param name="doc">The document to write.</param>
        /// <param name="path">The file path to write to.</param>
        /// <param name="options">Optional write options.</param>
        public static void ToFile(GhJsonDocument doc, string path, WriteOptions? options = null)
        {
            var json = ToJson(doc, options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Serializes a GhJSON document to a JSON string.
        /// </summary>
        /// <param name="doc">The document to serialize.</param>
        /// <param name="options">Optional write options.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string ToJson(GhJsonDocument doc, WriteOptions? options = null)
        {
#if DEBUG
            Debug.WriteLine($"[GhJson.ToJson] Serializing document with {doc?.Components?.Count ?? 0} components");
#endif
            var result = Serialization.GhJsonSerializer.Serialize(doc, options);
#if DEBUG
            Debug.WriteLine($"[GhJson.ToJson] Serialized JSON, length: {result?.Length ?? 0}");
#endif
            return result;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates a GhJSON document.
        /// </summary>
        /// <param name="doc">The document to validate.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>A validation result containing errors, warnings, and info messages.</returns>
        public static ValidationResult Validate(GhJsonDocument doc, ValidationLevel level = ValidationLevel.Standard)
        {
            return GhJsonValidator.Validate(doc, level);
        }

        /// <summary>
        /// Validates a JSON string as a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>A validation result containing errors, warnings, and info messages.</returns>
        public static ValidationResult Validate(string json, ValidationLevel level = ValidationLevel.Standard)
        {
            return GhJsonValidator.Validate(json, level);
        }

        /// <summary>
        /// Checks if a GhJSON document is valid.
        /// </summary>
        /// <param name="doc">The document to check.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(GhJsonDocument doc, ValidationLevel level = ValidationLevel.Standard)
        {
            return Validate(doc, level).IsValid;
        }

        /// <summary>
        /// Checks if a JSON string is a valid GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to check.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(string json, ValidationLevel level = ValidationLevel.Standard)
        {
            return Validate(json, level).IsValid;
        }

        /// <summary>
        /// Checks if a JSON string is a valid GhJSON document, returning a human-readable message.
        /// </summary>
        /// <param name="json">The JSON string to check.</param>
        /// <param name="message">A human-readable message describing validation errors, if any.</param>
        /// <param name="level">The validation level.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(string json, out string? message, ValidationLevel level = ValidationLevel.Standard)
        {
            message = null;

            try
            {
                var result = Validate(json, level);
                if (result.IsValid)
                {
                    return true;
                }

                message = string.Join("; ", result.Errors.Select(e => e.ToString()));
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        #endregion

        #region Fix Operations

        /// <summary>
        /// Fixes a GhJSON document by applying all default fix operations.
        /// </summary>
        /// <param name="doc">The document to fix.</param>
        /// <param name="options">Optional fix options.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult Fix(GhJsonDocument doc, FixOptions? options = null)
        {
            return DocumentFixer.Fix(doc, options);
        }

        /// <summary>
        /// Fixes document metadata (counts, timestamps).
        /// </summary>
        /// <param name="doc">The document to fix.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult FixMetadata(GhJsonDocument doc)
        {
            return DocumentFixer.FixMetadata(doc);
        }

        /// <summary>
        /// Assigns missing IDs to components.
        /// </summary>
        /// <param name="doc">The document to fix.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult AssignMissingIds(GhJsonDocument doc)
        {
            return DocumentFixer.AssignMissingIds(doc);
        }

        /// <summary>
        /// Reassigns all component IDs sequentially.
        /// </summary>
        /// <param name="doc">The document to fix.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult ReassignIds(GhJsonDocument doc)
        {
            return DocumentFixer.ReassignIds(doc);
        }

        /// <summary>
        /// Generates missing instance GUIDs for components.
        /// </summary>
        /// <param name="doc">The document to fix.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult GenerateMissingInstanceGuids(GhJsonDocument doc)
        {
            return DocumentFixer.GenerateMissingInstanceGuids(doc);
        }

        /// <summary>
        /// Regenerates all instance GUIDs.
        /// </summary>
        /// <param name="doc">The document to fix.</param>
        /// <returns>A fix result containing the fixed document and applied actions.</returns>
        public static FixResult RegenerateInstanceGuids(GhJsonDocument doc)
        {
            return DocumentFixer.RegenerateInstanceGuids(doc);
        }

        #endregion

        #region Merge Operations

        /// <summary>
        /// Merges two GhJSON documents.
        /// </summary>
        /// <param name="baseDoc">The base document to merge into.</param>
        /// <param name="incomingDoc">The incoming document to merge from.</param>
        /// <param name="options">Optional merge options.</param>
        /// <returns>A merge result containing the merged document.</returns>
        public static MergeResult Merge(GhJsonDocument baseDoc, GhJsonDocument incomingDoc, MergeOptions? options = null)
        {
            return DocumentMerger.Merge(baseDoc, incomingDoc, options);
        }

        #endregion

        #region Schema Migration

        /// <summary>
        /// Migrates a GhJSON document to the target schema version.
        /// </summary>
        /// <param name="doc">The document to migrate.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>A migration result with the migrated document.</returns>
        public static MigrationResult MigrateSchema(GhJsonDocument doc, string? targetVersion = null)
        {
            return SchemaMigrator.Migrate(doc, targetVersion);
        }

        /// <summary>
        /// Checks if a document needs migration to the target version.
        /// </summary>
        /// <param name="doc">The document to check.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>True if migration is needed.</returns>
        public static bool NeedsMigration(GhJsonDocument doc, string? targetVersion = null)
        {
            return SchemaMigrator.NeedsMigration(doc, targetVersion);
        }

        #endregion
    }
}
