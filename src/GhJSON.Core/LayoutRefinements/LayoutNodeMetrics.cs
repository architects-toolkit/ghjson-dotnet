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

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Measured geometry of a single layout node, supplied to the refinement passes
    /// through <see cref="LayoutRefinementOptions.NodeMetricsProvider"/>. The contract
    /// is deliberately free of any canvas-host types: a host adapter (e.g.
    /// GhJSON.Grasshopper's canvas metrics provider) measures live objects and fills
    /// one metrics value per layout key, so the refinement algorithms stay host- and
    /// license-independent.
    /// </summary>
    /// <remarks>
    /// Positions in the layout pipeline are bounds centers, so port geometry is
    /// expressed as vertical deltas relative to the node's own bounds center: a wire
    /// entering input port <c>i</c> horizontally meets the node at
    /// <c>center.Y + InputPortDeltas[i]</c>. Deltas are indexed by the connection
    /// endpoint's <c>ParamIndex</c>. Coarse alignment retains centered fallback votes;
    /// grouped fitting does not infer constraints for unknown or invalid occupied ports.
    /// Floating parameters may supply their single input/output grip offsets at index
    /// zero; missing offsets retain the centered-grip fallback. Named endpoints resolve
    /// through the optional index maps during grouped constraint fitting.
    /// </remarks>
    public sealed class LayoutNodeMetrics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutNodeMetrics"/> class.
        /// </summary>
        /// <param name="size">Measured bounds size, or null when the node has no measurable bounds.</param>
        /// <param name="kind">Node classification used for port semantics.</param>
        /// <param name="inputPortDeltas">
        /// Vertical offset from the bounds center to each input port's center, indexed
        /// by input parameter index. Null or shorter lists act as zero deltas for
        /// missing indices.
        /// </param>
        /// <param name="outputPortDeltas">
        /// Vertical offset from the bounds center to each output port's center, indexed
        /// by output parameter index. Null or shorter lists act as zero deltas for
        /// missing indices.
        /// </param>
        public LayoutNodeMetrics(
            SizeF? size,
            LayoutNodeKind kind,
            IReadOnlyList<float>? inputPortDeltas = null,
            IReadOnlyList<float>? outputPortDeltas = null)
            : this(size, kind, inputPortDeltas, outputPortDeltas, null, null)
        {
        }

        /// <summary>Initializes measured geometry with optional named-port lookup maps.</summary>
        /// <param name="size">Measured bounds size, or null when unavailable.</param>
        /// <param name="kind">Node classification used for port semantics.</param>
        /// <param name="inputPortDeltas">Input grip offsets from the bounds center.</param>
        /// <param name="outputPortDeltas">Output grip offsets from the bounds center.</param>
        /// <param name="inputPortIndices">Unambiguous input names or aliases mapped to indices.</param>
        /// <param name="outputPortIndices">Unambiguous output names or aliases mapped to indices.</param>
        public LayoutNodeMetrics(
            SizeF? size,
            LayoutNodeKind kind,
            IReadOnlyList<float>? inputPortDeltas,
            IReadOnlyList<float>? outputPortDeltas,
            IReadOnlyDictionary<string, int>? inputPortIndices,
            IReadOnlyDictionary<string, int>? outputPortIndices)
        {
            this.Size = size;
            this.Kind = kind;
            this.InputPortDeltas = inputPortDeltas ?? Array.Empty<float>();
            this.OutputPortDeltas = outputPortDeltas ?? Array.Empty<float>();
            this.InputPortIndices = inputPortIndices;
            this.OutputPortIndices = outputPortIndices;
        }

        /// <summary>
        /// Measured bounds size in canvas units (pixels). Null when the node cannot be
        /// measured; passes that need bounds skip or zero-fill such nodes, matching the
        /// behavior of a canvas object without rendered attributes.
        /// </summary>
        public SizeF? Size { get; }

        /// <summary>
        /// Node classification. <see cref="LayoutNodeKind.Other"/> nodes never vote on
        /// input port positions, matching the pre-refactor behavior for objects that
        /// are neither components nor floating parameters.
        /// </summary>
        public LayoutNodeKind Kind { get; }

        /// <summary>
        /// Vertical offsets from the bounds center to each input port's center,
        /// indexed by input parameter index.
        /// </summary>
        public IReadOnlyList<float> InputPortDeltas { get; }

        /// <summary>
        /// Vertical offsets from the bounds center to each output port's center,
        /// indexed by output parameter index.
        /// </summary>
        public IReadOnlyList<float> OutputPortDeltas { get; }

        /// <summary>Unambiguous input names or aliases mapped to measured port indices.</summary>
        public IReadOnlyDictionary<string, int>? InputPortIndices { get; }

        /// <summary>Unambiguous output names or aliases mapped to measured port indices.</summary>
        public IReadOnlyDictionary<string, int>? OutputPortIndices { get; }

        /// <summary>
        /// Resolves metrics for a layout key through <paramref name="provider"/>,
        /// guarding against provider exceptions so a failing measurement source
        /// degrades to "unmeasurable" instead of aborting the pass.
        /// </summary>
        /// <param name="provider">The metrics provider to invoke; may be null.</param>
        /// <param name="id">The layout key to measure.</param>
        /// <returns>The resolved metrics, or null when unavailable or the provider threw.</returns>
        internal static LayoutNodeMetrics? Resolve(Func<Guid, LayoutNodeMetrics?>? provider, Guid id)
        {
            if (provider == null)
            {
                return null;
            }

            try
            {
                return provider(id);
            }
            catch
            {
                return null;
            }
        }
    }
}
