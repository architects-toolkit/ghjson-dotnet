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

using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Migration
{
    /// <summary>
    /// Contract for schema version migrators.
    /// Each migrator handles migration from one schema version to the next.
    /// </summary>
    public interface IMigrator
    {
        /// <summary>
        /// Gets the source schema version this migrator handles.
        /// </summary>
        string FromVersion { get; }

        /// <summary>
        /// Gets the target schema version after migration.
        /// </summary>
        string ToVersion { get; }

        /// <summary>
        /// Gets a description of what this migration does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Determines whether this migrator can handle the given document.
        /// </summary>
        /// <param name="document">The JSON document to check.</param>
        /// <returns>True if this migrator can process the document.</returns>
        bool CanMigrate(JObject document);

        /// <summary>
        /// Migrates the document from the source version to the target version.
        /// </summary>
        /// <param name="document">The JSON document to migrate (modified in place).</param>
        /// <returns>Result of the migration operation.</returns>
        MigrationStepResult Migrate(JObject document);
    }

    /// <summary>
    /// Result of a single migration step.
    /// </summary>
    public class MigrationStepResult
    {
        /// <summary>
        /// Gets or sets whether the migration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets whether any changes were made.
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Gets or sets the number of changes made.
        /// </summary>
        public int ChangeCount { get; set; }

        /// <summary>
        /// Gets or sets a description of changes made.
        /// </summary>
        public string? ChangeDescription { get; set; }

        /// <summary>
        /// Gets or sets any error message if migration failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful result with no changes.
        /// </summary>
        public static MigrationStepResult NoChange() => new MigrationStepResult { Success = true, WasModified = false };

        /// <summary>
        /// Creates a successful result with changes.
        /// </summary>
        public static MigrationStepResult Changed(int changeCount, string description) =>
            new MigrationStepResult
            {
                Success = true,
                WasModified = true,
                ChangeCount = changeCount,
                ChangeDescription = description
            };

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static MigrationStepResult Failed(string errorMessage) =>
            new MigrationStepResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
    }
}
