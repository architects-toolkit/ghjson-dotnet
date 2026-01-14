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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.SchemaMigration
{
    /// <summary>
    /// Handles schema migrations between GhJSON versions.
    /// </summary>
    internal static class SchemaMigrator
    {
        /// <summary>
        /// The current schema version.
        /// </summary>
        public const string CurrentVersion = "1.0";

        /// <summary>
        /// Migrates a document to the target schema version.
        /// </summary>
        /// <param name="document">The document to migrate.</param>
        /// <param name="targetVersion">The target version (defaults to current).</param>
        /// <returns>The migration result.</returns>
        public static MigrationResult Migrate(GhJsonDocument document, string? targetVersion = null)
        {
            targetVersion ??= CurrentVersion;

            var result = new MigrationResult
            {
                Document = document,
                FromVersion = document.Schema,
                ToVersion = targetVersion
            };

            try
            {
                var sourceVersion = ParseVersion(document.Schema);
                var destVersion = ParseVersion(targetVersion);

                if (sourceVersion == destVersion)
                {
                    result.Success = true;
                    return result;
                }

                // Currently only version 1.0 exists, so no migrations are needed
                // Future migrations would be added here
                result.Document = new GhJsonDocument(
                    schema: targetVersion,
                    metadata: document.Metadata,
                    components: document.Components,
                    connections: document.Connections,
                    groups: document.Groups);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Checks if a document needs migration.
        /// </summary>
        /// <param name="document">The document to check.</param>
        /// <param name="targetVersion">The target version (defaults to current).</param>
        /// <returns>True if migration is needed.</returns>
        public static bool NeedsMigration(GhJsonDocument document, string? targetVersion = null)
        {
            targetVersion ??= CurrentVersion;

            if (string.IsNullOrEmpty(document.Schema))
            {
                return true;
            }

            var sourceVersion = ParseVersion(document.Schema);
            var destVersion = ParseVersion(targetVersion);

            return sourceVersion != destVersion;
        }

        private static Version ParseVersion(string? versionString)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                return new Version(1, 0);
            }

            return Version.TryParse(versionString, out var version)
                ? version
                : new Version(1, 0);
        }
    }
}
