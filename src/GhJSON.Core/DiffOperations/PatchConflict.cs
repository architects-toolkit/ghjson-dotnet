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

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Kinds of conflict that can be produced while applying a GhPatch document.
    /// </summary>
    public enum PatchConflictKind
    {
        /// <summary>The patch targeted an entity that does not exist on the base.</summary>
        MatchNotFound,

        /// <summary>The match descriptor resolved to more than one entity.</summary>
        MatchAmbiguous,

        /// <summary>Adding a component whose <c>instanceGuid</c> already exists on the base.</summary>
        InstanceGuidCollision,

        /// <summary>Adding a connection that already exists.</summary>
        ConnectionAlreadyPresent,

        /// <summary>Removing a connection that does not exist.</summary>
        ConnectionNotFound,

        /// <summary>A group member references a component that is not present on the base.</summary>
        DanglingMember,

        /// <summary>The base-document checksum mismatched the patch's reference.</summary>
        BaseChecksumMismatch,

        /// <summary>The schema version of the patch does not match the base document.</summary>
        SchemaVersionMismatch,
    }

    /// <summary>
    /// A single conflict recorded while applying a patch.
    /// </summary>
    public sealed class PatchConflict
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PatchConflict"/> class.
        /// </summary>
        /// <param name="kind">The conflict kind.</param>
        /// <param name="message">A human-readable description.</param>
        /// <param name="path">An optional patch path describing where the conflict occurred.</param>
        public PatchConflict(PatchConflictKind kind, string message, string? path = null)
        {
            this.Kind = kind;
            this.Message = message;
            this.Path = path;
        }

        /// <summary>Gets the kind of conflict.</summary>
        public PatchConflictKind Kind { get; }

        /// <summary>Gets the human-readable description.</summary>
        public string Message { get; }

        /// <summary>Gets the patch path describing where the conflict occurred.</summary>
        public string? Path { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Path)
                ? $"{this.Kind}: {this.Message}"
                : $"{this.Kind} at {this.Path}: {this.Message}";
        }
    }
}
