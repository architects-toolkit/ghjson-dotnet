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
using System.Drawing;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Grasshopper-facing refinement options: extends the host-independent core options
    /// with providers that resolve live <see cref="IGH_DocumentObject"/> instances and
    /// measured sizes. <see cref="GhJsonGrasshopper.ComputeLayout"/> composes them into a
    /// <see cref="GhJSON.Core.LayoutRefinements.LayoutNodeMetrics"/> provider through
    /// <see cref="CanvasNodeMetricsProvider"/>; a directly assigned
    /// <see cref="GhJSON.Core.LayoutRefinements.LayoutRefinementOptions.NodeMetricsProvider"/>
    /// always wins over the composed providers.
    /// </summary>
    public sealed class LayoutRefinementOptions : Core.LayoutRefinements.LayoutRefinementOptions
    {
        /// <summary>
        /// Optional node-size lookup matching <see cref="GhJSON.Core.DependencyGraph.LayoutOptions.NodeSizeProvider"/>.
        /// Consulted after the caller-owned object provider and before live canvas bounds
        /// so refinements can measure objects that are instantiated but not yet added to
        /// the document (e.g. the gh_put flow).
        /// </summary>
        public Func<Guid, SizeF?>? NodeSizeProvider { get; set; }

        /// <summary>
        /// Optional object lookup resolving a layout key to its Grasshopper document
        /// object, supplied by the caller from its own object map (e.g. selected objects
        /// for tidy-up or freshly instantiated objects for gh_put). Consulted before the
        /// live document so refinements can read port geometry from objects that are not
        /// on the canvas yet. In-process only; never exposed through any external schema.
        /// </summary>
        public Func<Guid, IGH_DocumentObject?>? ObjectProvider { get; set; }

        /// <summary>Default refinement options with every pass enabled.</summary>
        public new static LayoutRefinementOptions Default => new LayoutRefinementOptions();

        /// <summary>Refinement options with every pass disabled.</summary>
        public new static LayoutRefinementOptions None => new LayoutRefinementOptions
        {
            ApplyBoundsAwareSpacing = false,
            AlignToPorts = false,
            AvoidCollisions = false,
            EnsureWireClearance = false,
        };
    }
}
