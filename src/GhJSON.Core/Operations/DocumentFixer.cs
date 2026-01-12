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

using GhJSON.Core.Models.Document;
using GhJSON.Core.Operations.FixOperations;

namespace GhJSON.Core.Operations
{
    /// <summary>
    /// Orchestrates fix operations on GhJSON documents.
    /// </summary>
    public class DocumentFixer
    {
        private readonly FixOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentFixer"/> class.
        /// </summary>
        /// <param name="options">Fix options.</param>
        public DocumentFixer(FixOptions? options = null)
        {
            _options = options ?? FixOptions.Default;
        }

        /// <summary>
        /// Applies all enabled fix operations to the document.
        /// </summary>
        /// <param name="document">The document to fix.</param>
        /// <returns>Result containing all applied fixes.</returns>
        public FixResult Fix(GhJsonDocument document)
        {
            var result = new FixResult(document);

            if (document == null)
                return result;

            // Apply fix operations in order
            if (_options.AssignIds)
            {
                var idAssigner = new IdAssigner();
                var opResult = idAssigner.Apply(document);
                if (opResult.WasModified)
                {
                    result.Actions.Add(FixAction.Create(
                        FixActionType.IdsAssigned,
                        opResult.ChangeDescription ?? "Assigned IDs",
                        opResult.ItemsAffected));
                }
            }

            if (_options.GenerateInstanceGuids)
            {
                var guidGenerator = new GuidGenerator();
                var opResult = guidGenerator.Apply(document);
                if (opResult.WasModified)
                {
                    result.Actions.Add(FixAction.Create(
                        FixActionType.GuidsGenerated,
                        opResult.ChangeDescription ?? "Generated GUIDs",
                        opResult.ItemsAffected));
                }
            }

            if (_options.UpdateCounts)
            {
                var countUpdater = new CountUpdater();
                var opResult = countUpdater.Apply(document);
                if (opResult.WasModified)
                {
                    result.Actions.Add(FixAction.Create(
                        FixActionType.CountsUpdated,
                        opResult.ChangeDescription ?? "Updated counts",
                        opResult.ItemsAffected));
                }
            }

            if (_options.PopulateMetadata)
            {
                var metadataPopulator = new MetadataPopulator();
                var opResult = metadataPopulator.Apply(document);
                if (opResult.WasModified)
                {
                    result.Actions.Add(FixAction.Create(
                        FixActionType.MetadataPopulated,
                        opResult.ChangeDescription ?? "Populated metadata",
                        opResult.ItemsAffected));
                }
            }

            return result;
        }

        /// <summary>
        /// Applies all fix operations with default options.
        /// </summary>
        /// <param name="document">The document to fix.</param>
        /// <returns>Result containing all applied fixes.</returns>
        public static FixResult FixAll(GhJsonDocument document)
        {
            var fixer = new DocumentFixer(FixOptions.Default);
            return fixer.Fix(document);
        }

        /// <summary>
        /// Applies minimal fix operations (IDs only).
        /// </summary>
        /// <param name="document">The document to fix.</param>
        /// <returns>Result containing all applied fixes.</returns>
        public static FixResult FixMinimal(GhJsonDocument document)
        {
            var fixer = new DocumentFixer(FixOptions.Minimal);
            return fixer.Fix(document);
        }
    }
}
