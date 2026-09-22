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

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Classifies a layout node for the refinement passes. Port votes differ per kind:
    /// components expose indexed input/output ports, floating parameters take wires at
    /// a vertically centered grip, and other nodes cannot contribute port geometry.
    /// </summary>
    public enum LayoutNodeKind
    {
        /// <summary>
        /// A parametric component with indexed input and output ports described by
        /// <see cref="LayoutNodeMetrics.InputPortDeltas"/> and
        /// <see cref="LayoutNodeMetrics.OutputPortDeltas"/>.
        /// </summary>
        Component = 0,

        /// <summary>
        /// A floating parameter (panel, slider, value parameter, ...). Its input and
        /// output grips are vertically centered on the node's own bounds, so port
        /// deltas are always zero.
        /// </summary>
        Parameter = 1,

        /// <summary>
        /// Any other node: unresolvable, size-only, or an object without port
        /// semantics. Such nodes cannot vote on input port positions.
        /// </summary>
        Other = 2,
    }
}
