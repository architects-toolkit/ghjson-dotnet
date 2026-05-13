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

using System.IO;
using GhJSON.Core.DiffOperations;
using GhJSON.Core.PatchModels;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using GhJSON.Core.Validation;

namespace GhJSON.Core
{
    /// <summary>
    /// Diff and patch operations on the <see cref="GhJson"/> façade.
    /// </summary>
    public static partial class GhJson
    {
        #region Diff & Patch

        /// <summary>
        /// Compare two documents and produce a <see cref="DiffResult"/> containing a
        /// <see cref="GhPatchDocument"/> that, when applied to <paramref name="left"/>,
        /// yields <paramref name="right"/>.
        /// </summary>
        /// <param name="left">The base document.</param>
        /// <param name="right">The target document.</param>
        /// <param name="options">Optional diff options.</param>
        /// <returns>A <see cref="DiffResult"/> with the generated patch and counts.</returns>
        public static DiffResult Diff(GhJsonDocument left, GhJsonDocument right, DiffOptions? options = null)
        {
            return DocumentDiffer.Diff(left, right, options);
        }

        /// <summary>
        /// Compare two documents and return the generated <see cref="GhPatchDocument"/>.
        /// Convenience wrapper around <see cref="Diff(GhJsonDocument, GhJsonDocument, DiffOptions?)"/>.
        /// </summary>
        /// <param name="left">The base document.</param>
        /// <param name="right">The target document.</param>
        /// <param name="options">Optional diff options.</param>
        /// <returns>The generated patch document.</returns>
        public static GhPatchDocument DiffToPatch(GhJsonDocument left, GhJsonDocument right, DiffOptions? options = null)
        {
            return DocumentDiffer.Diff(left, right, options).Patch;
        }

        /// <summary>
        /// Deserialize a <see cref="GhPatchDocument"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized patch.</returns>
        public static GhPatchDocument PatchFromJson(string json)
        {
            return PatchSerializer.Deserialize(json);
        }

        /// <summary>
        /// Serialize a <see cref="GhPatchDocument"/> to a JSON string.
        /// </summary>
        /// <param name="patch">The patch to serialize.</param>
        /// <param name="options">Optional write options.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string PatchToJson(GhPatchDocument patch, WriteOptions? options = null)
        {
            return PatchSerializer.Serialize(patch, options);
        }

        /// <summary>
        /// Read a patch document from a <c>.ghpatch</c> file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The deserialized patch document.</returns>
        public static GhPatchDocument PatchFromFile(string path)
        {
            return PatchSerializer.Deserialize(File.ReadAllText(path));
        }

        /// <summary>
        /// Write a patch document to a <c>.ghpatch</c> file.
        /// </summary>
        /// <param name="patch">The patch to write.</param>
        /// <param name="path">The destination file path.</param>
        /// <param name="options">Optional write options.</param>
        public static void PatchToFile(GhPatchDocument patch, string path, WriteOptions? options = null)
        {
            File.WriteAllText(path, PatchSerializer.Serialize(patch, options));
        }

        /// <summary>
        /// Apply a patch to a base document.
        /// </summary>
        /// <param name="baseDoc">The base document.</param>
        /// <param name="patch">The patch to apply.</param>
        /// <param name="options">Optional apply options.</param>
        /// <returns>The result of the apply operation.</returns>
        public static ApplyPatchResult ApplyPatch(GhJsonDocument baseDoc, GhPatchDocument patch, ApplyPatchOptions? options = null)
        {
            return PatchApplier.Apply(baseDoc, patch, options);
        }

        /// <summary>
        /// Apply a patch to a base document, both supplied as JSON strings.
        /// </summary>
        /// <param name="baseJson">The base GhJSON document as a JSON string.</param>
        /// <param name="patchJson">The patch as a JSON string.</param>
        /// <param name="options">Optional apply options.</param>
        /// <returns>The result of the apply operation.</returns>
        public static ApplyPatchResult ApplyPatch(string baseJson, string patchJson, ApplyPatchOptions? options = null)
        {
            var baseDoc = GhJsonSerializer.Deserialize(baseJson);
            var patch = PatchSerializer.Deserialize(patchJson);
            return PatchApplier.Apply(baseDoc, patch, options);
        }

        /// <summary>
        /// Validate the structure of a <see cref="GhPatchDocument"/>.
        /// </summary>
        /// <param name="patch">The patch to validate.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult ValidatePatch(GhPatchDocument patch)
        {
            return PatchValidator.Validate(patch);
        }

        /// <summary>
        /// Validate a patch supplied as a JSON string.
        /// </summary>
        /// <param name="patchJson">The patch JSON string.</param>
        /// <returns>The validation result.</returns>
        public static ValidationResult ValidatePatch(string patchJson)
        {
            return PatchValidator.Validate(patchJson);
        }

        #endregion
    }
}
