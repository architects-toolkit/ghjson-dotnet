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

using System;
using System.Collections.Generic;
using System.Drawing;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.PutOperations
{
    /// <summary>
    /// Immutable handle produced by <see cref="GhJsonGrasshopper.PreparePut"/> and
    /// consumed by <see cref="GhJsonGrasshopper.ApplyPreparedPut"/>. Preparation
    /// binds a specific <see cref="GH_Document"/>, resolves update targets,
    /// instantiates off-document prototypes, measures real bounds, and computes
    /// layout positions — without mutating the canvas, recording undo, or
    /// requesting a solution. The handle is single-use: a failed or disposed
    /// preparation can never apply, and an applied preparation cannot run twice.
    /// </summary>
    public sealed class PreparedPut : IDisposable
    {
        /// <summary>One prepared document entry.</summary>
        internal sealed class Entry
        {
            /// <summary>The incoming component definition.</summary>
            internal GhJsonComponent Component = null!;

            /// <summary>
            /// Off-document prototype created for new components and different-type
            /// replacements; <c>null</c> for same-type in-place updates.
            /// </summary>
            internal IGH_DocumentObject? Prototype;

            /// <summary>
            /// Live object the component updates in place (same type) or replaces
            /// (different type); <c>null</c> for entirely new components.
            /// </summary>
            internal IGH_DocumentObject? Existing;
        }

        /// <summary>The document the plan is bound to; never retargeted.</summary>
        internal GH_Document Document = null!;

        /// <summary>The source document model.</summary>
        internal GhJsonDocument Model = null!;

        /// <summary>Snapshot of the placement options.</summary>
        internal PutOptions Options = null!;

        /// <summary>Explicit translation applied to authored pivots only.</summary>
        internal PointF EffectiveOffset;

        /// <summary>Entries in document order, covering updates, replacements and new objects.</summary>
        internal List<Entry> Entries = new List<Entry>();

        /// <summary>Layout positions computed from measured prototype bounds.</summary>
        internal Dictionary<Guid, PointF> LayoutPositions = new Dictionary<Guid, PointF>();

        /// <summary>Pivots of replaced objects, captured at preparation time.</summary>
        internal Dictionary<Guid, PointF?> ReplacedPivots = new Dictionary<Guid, PointF?>();

        /// <summary>Component names that failed instantiation under the skip policy.</summary>
        internal List<string> FailedComponents = new List<string>();

        /// <summary>Warnings collected during preparation.</summary>
        internal List<string> Warnings = new List<string>();

        /// <summary>
        /// When preparation itself failed, the terminal <see cref="PutResult"/> to
        /// report instead of applying. Set only when preparation cannot proceed.
        /// </summary>
        internal PutResult? PrepareFailure;

        private bool consumed;

        /// <summary>
        /// Gets a value indicating whether this handle can still be applied: the
        /// preparation succeeded, has not been consumed, and has not been disposed.
        /// </summary>
        public bool IsUsable => this.PrepareFailure == null && !this.consumed && !this.disposed;

        /// <summary>Whether <see cref="Dispose"/> has released the prepared resources.</summary>
        internal bool Disposed => this.disposed;

        private bool disposed;

        /// <summary>Marks the handle consumed by <see cref="GhJsonGrasshopper.ApplyPreparedPut"/>.</summary>
        /// <returns><c>false</c> when the handle was already consumed or disposed.</returns>
        internal bool TryConsume()
        {
            if (this.consumed || this.disposed)
            {
                return false;
            }

            this.consumed = true;
            return true;
        }

        /// <summary>
        /// Drops the prepared off-document prototypes. Live objects referenced for
        /// update/replace are owned by the bound document and are unaffected.
        /// </summary>
        public void Dispose()
        {
            this.disposed = true;
            this.Entries.Clear();
            this.LayoutPositions.Clear();
            this.ReplacedPivots.Clear();
            this.FailedComponents.Clear();
            this.Warnings.Clear();
        }
    }
}
