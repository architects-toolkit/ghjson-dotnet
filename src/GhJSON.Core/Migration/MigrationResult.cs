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

using System.Collections.Generic;
using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Migration
{
    /// <summary>
    /// Result of a complete migration operation.
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Gets or sets the migrated document.
        /// </summary>
        public GhJsonDocument? Document { get; set; }

        /// <summary>
        /// Gets or sets the original schema version before migration.
        /// </summary>
        public string? FromVersion { get; set; }

        /// <summary>
        /// Gets or sets the schema version after migration.
        /// </summary>
        public string? ToVersion { get; set; }

        /// <summary>
        /// Gets or sets whether the migration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets whether any changes were made during migration.
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Gets the list of migration changes applied.
        /// </summary>
        public List<MigrationChange> Changes { get; } = new List<MigrationChange>();

        /// <summary>
        /// Gets or sets any error message if migration failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Describes a single migration change.
    /// </summary>
    public class MigrationChange
    {
        /// <summary>
        /// Gets or sets the migrator that made this change.
        /// </summary>
        public string MigratorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source version.
        /// </summary>
        public string FromVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target version.
        /// </summary>
        public string ToVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a description of the change.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of items affected.
        /// </summary>
        public int ItemsAffected { get; set; }
    }
}
