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
using GhJSON.Core.Migration.Migrators;
using GhJSON.Core.Models.Document;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Migration
{
    /// <summary>
    /// Orchestrates schema version migrations through a chain of migrators.
    /// </summary>
    public class MigrationPipeline
    {
        private readonly List<IMigrator> _migrators = new List<IMigrator>();

        /// <summary>
        /// Gets the current schema version.
        /// </summary>
        public const string CurrentVersion = "1.0";

        /// <summary>
        /// Gets the default pipeline with all built-in migrators registered.
        /// </summary>
        public static MigrationPipeline Default
        {
            get
            {
                var pipeline = new MigrationPipeline();
                pipeline.RegisterBuiltInMigrators();
                return pipeline;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationPipeline"/> class.
        /// </summary>
        public MigrationPipeline()
        {
        }

        /// <summary>
        /// Registers a migrator in the pipeline.
        /// </summary>
        /// <param name="migrator">The migrator to register.</param>
        public void Register(IMigrator migrator)
        {
            _migrators.Add(migrator);
        }

        /// <summary>
        /// Registers all built-in migrators.
        /// </summary>
        public void RegisterBuiltInMigrators()
        {
            Register(new V0_9_to_V1_0_PivotMigrator());
            Register(new V0_9_to_V1_0_PropertyMigrator());
        }

        /// <summary>
        /// Gets all registered migrators.
        /// </summary>
        public IReadOnlyList<IMigrator> Migrators => _migrators.AsReadOnly();

        /// <summary>
        /// Migrates a JSON string to the target schema version.
        /// </summary>
        /// <param name="json">The JSON string to migrate.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>Migration result with the migrated document.</returns>
        public MigrationResult Migrate(string json, string? targetVersion = null)
        {
            try
            {
                var jObject = JObject.Parse(json);
                return Migrate(jObject, targetVersion);
            }
            catch (JsonException ex)
            {
                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse JSON: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Migrates a JObject to the target schema version.
        /// </summary>
        /// <param name="document">The JSON document to migrate.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>Migration result with the migrated document.</returns>
        public MigrationResult Migrate(JObject document, string? targetVersion = null)
        {
            targetVersion ??= CurrentVersion;
            var result = new MigrationResult
            {
                FromVersion = GetSchemaVersion(document),
                ToVersion = targetVersion,
                Success = true
            };

            try
            {
                // Apply all applicable migrators
                foreach (var migrator in _migrators.Where(m => m.CanMigrate(document)))
                {
                    var stepResult = migrator.Migrate(document);

                    if (!stepResult.Success)
                    {
                        result.Success = false;
                        result.ErrorMessage = stepResult.ErrorMessage;
                        return result;
                    }

                    if (stepResult.WasModified)
                    {
                        result.WasModified = true;
                        result.Changes.Add(new MigrationChange
                        {
                            MigratorName = migrator.GetType().Name,
                            FromVersion = migrator.FromVersion,
                            ToVersion = migrator.ToVersion,
                            Description = stepResult.ChangeDescription ?? migrator.Description,
                            ItemsAffected = stepResult.ChangeCount
                        });
                    }
                }

                // Update schema version
                document["schemaVersion"] = targetVersion;

                // Deserialize to GhJsonDocument
                result.Document = document.ToObject<GhJsonDocument>();
                result.ToVersion = targetVersion;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Migration failed: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Migrates a GhJsonDocument to the target schema version.
        /// </summary>
        /// <param name="document">The document to migrate.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>Migration result with the migrated document.</returns>
        public MigrationResult Migrate(GhJsonDocument document, string? targetVersion = null)
        {
            var json = JsonConvert.SerializeObject(document);
            var jObject = JObject.Parse(json);
            return Migrate(jObject, targetVersion);
        }

        /// <summary>
        /// Determines whether a document needs migration.
        /// </summary>
        /// <param name="document">The JSON document to check.</param>
        /// <param name="targetVersion">The target schema version (default: current version).</param>
        /// <returns>True if migration is needed.</returns>
        public bool NeedsMigration(JObject document, string? targetVersion = null)
        {
            targetVersion ??= CurrentVersion;
            var currentVersion = GetSchemaVersion(document);

            if (string.IsNullOrEmpty(currentVersion) || currentVersion != targetVersion)
            {
                return _migrators.Any(m => m.CanMigrate(document));
            }

            return false;
        }

        /// <summary>
        /// Gets the schema version from a document.
        /// </summary>
        private static string? GetSchemaVersion(JObject document)
        {
            return document["schemaVersion"]?.Value<string>();
        }
    }
}
