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

using System.Collections.Generic;

namespace GhJSON.Core.DataEditing
{
    /// <summary>
    /// One replacement inside a data tree: a <see cref="GhJsonDataPosition"/>
    /// address plus the GhJSON-encoded value(s) to store there.
    /// </summary>
    public sealed class GhJsonDataEdit
    {
        /// <summary>
        /// Gets or sets the address text — <c>{a;b;c}</c> for a whole existing
        /// branch or <c>{a;b;c}(i)</c>/<c>(i)</c> for a single item.
        /// </summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the replacement value(s) in GhJSON prefixed encoding
        /// (<c>text:…</c>, <c>number:…</c>, <c>pointXYZ:…</c>, …). Item
        /// positions require exactly one value; branch positions take the
        /// full replacement list (possibly empty).
        /// </summary>
        public IReadOnlyList<string> Values { get; set; } = new List<string>();
    }

    /// <summary>
    /// Outcome of a data-tree edit operation.
    /// </summary>
    public sealed class GhJsonDataEditResult
    {
        /// <summary>Gets a value indicating whether all edits were applied.</summary>
        public bool Success { get; internal set; }

        /// <summary>Gets the failure reason when <see cref="Success"/> is false.</summary>
        public string? Error { get; internal set; }

        /// <summary>Gets the number of edits applied.</summary>
        public int AppliedCount { get; internal set; }

        /// <summary>Gets the resulting tree shape (branch path to item count) after the edit.</summary>
        public IReadOnlyDictionary<string, int>? Shape { get; internal set; }

        /// <summary>Creates a successful result.</summary>
        /// <param name="appliedCount">Number of edits applied.</param>
        /// <param name="shape">Resulting tree shape.</param>
        /// <returns>The result.</returns>
        internal static GhJsonDataEditResult Ok(int appliedCount, IReadOnlyDictionary<string, int>? shape)
        {
            return new GhJsonDataEditResult { Success = true, AppliedCount = appliedCount, Shape = shape };
        }

        /// <summary>Creates a failed result.</summary>
        /// <param name="error">The failure reason.</param>
        /// <returns>The result.</returns>
        internal static GhJsonDataEditResult Fail(string error)
        {
            return new GhJsonDataEditResult { Success = false, Error = error };
        }
    }
}
