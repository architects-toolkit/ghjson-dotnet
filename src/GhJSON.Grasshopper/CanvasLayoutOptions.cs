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
using GhJSON.Core.DependencyGraph;
using GhJSON.Grasshopper.LayoutRefinements;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper
{
    /// <summary>
    /// Options for <see cref="GhJsonGrasshopper.ComputeLayout"/>: one shared bundle for
    /// the core dependency-graph pass and the Grasshopper-aware refinement passes so
    /// every caller (gh_put, tidy-up) computes positions through the same pipeline.
    /// </summary>
    public sealed class CanvasLayoutOptions
    {
        /// <summary>
        /// Core dependency-graph options (spacing, island packing, node-size provider).
        /// Defaults to <see cref="LayoutOptions"/> defaults.
        /// </summary>
        public LayoutOptions? Layout { get; set; }

        /// <summary>
        /// Refinement options (bounds-aware spacing, port alignment, wire clearance,
        /// collision resolution, object/size providers). Defaults to
        /// <see cref="LayoutRefinementOptions.Default"/>.
        /// </summary>
        public LayoutRefinementOptions? Refinements { get; set; }

        /// <summary>
        /// Shared measured-bounds provider forwarded to <see cref="LayoutOptions.NodeSizeProvider"/>
        /// and <see cref="LayoutRefinementOptions.NodeSizeProvider"/> when those are unset, so a
        /// caller feeds one measurement source to the whole pipeline.
        /// </summary>
        public Func<Guid, SizeF?>? NodeSizeProvider { get; set; }

        /// <summary>
        /// Shared object lookup forwarded to <see cref="LayoutRefinementOptions.ObjectProvider"/>
        /// when unset, so port geometry resolves against the caller's own object map
        /// (freshly instantiated or selected objects) for every refinement pass.
        /// </summary>
        public Func<Guid, IGH_DocumentObject?>? ObjectProvider { get; set; }

        /// <summary>Default layout options for both stages.</summary>
        public static CanvasLayoutOptions Default => new CanvasLayoutOptions();
    }
}
