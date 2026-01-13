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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.SchemaMigration
{
    /// <summary>
    /// Represents the result of a schema migration operation.
    /// </summary>
    public sealed class MigrationResult
    {
        /// <summary>
        /// Gets or sets the migrated document.
        /// </summary>
        public GhJsonDocument Document { get; set; } = new GhJsonDocument();

        /// <summary>
        /// Gets or sets a value indicating whether the migration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the original schema version.
        /// </summary>
        public string? FromVersion { get; set; }

        /// <summary>
        /// Gets or sets the target schema version.
        /// </summary>
        public string? ToVersion { get; set; }

        /// <summary>
        /// Gets or sets the list of migrations that were applied.
        /// </summary>
        public List<string> AppliedMigrations { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of warnings that occurred during migration.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the error message if migration failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
