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

namespace GhJSON.Core.DependencyGraph.Internal
{
    internal sealed class LayoutNode
    {
        public Guid ComponentId { get; set; }

        /// <summary>
        /// Final pixel position, produced by <see cref="Sugiyama.CoordinateAssigner"/>.
        /// During the rank/order phases this is unused; ranks live in <see cref="Layer"/>
        /// and within-layer order in <see cref="Order"/>.
        /// </summary>
        public PointF Pivot { get; set; }

        /// <summary>Column index (0 = left). Sources are pinned to layer 0.</summary>
        public int Layer { get; set; }

        /// <summary>Within-layer vertical order index (0 = top).</summary>
        public int Order { get; set; }

        /// <summary>
        /// True for synthetic routing nodes inserted along edges that span more than one
        /// layer. Dummy nodes participate in ordering/crossing minimization but are never
        /// emitted as real component positions.
        /// </summary>
        public bool IsDummy { get; set; }

        /// <summary>Estimated component width, used for bounds-aware column spacing.</summary>
        public float Width { get; set; }

        /// <summary>Estimated component height, used for bounds-aware row spacing.</summary>
        public float Height { get; set; }

        /// <summary>
        /// Estimated number of input port slots (largest connected input index + 1, or the
        /// declared input count when the document carries parameter settings). Each input
        /// port contributes its own ordered row within the node when ordering layers.
        /// </summary>
        public int InputPortCount { get; set; }

        /// <summary>Estimated number of output port slots; see <see cref="InputPortCount"/>.</summary>
        public int OutputPortCount { get; set; }

        /// <summary>Parent node id to this node's input port index (or -1 when unknown).</summary>
        public Dictionary<Guid, int> Parents { get; set; } = new Dictionary<Guid, int>();

        /// <summary>Child node id to this node's output port index (or -1 when unknown).</summary>
        public Dictionary<Guid, int> Children { get; set; } = new Dictionary<Guid, int>();

        /// <summary>
        /// Vertical offset of an input port's center from the node center, expressed as a
        /// fraction of <see cref="Height"/> in (-0.5, 0.5). Ports are approximated as evenly
        /// distributed across the node height; multiply by <see cref="Height"/> for a pixel
        /// offset, or use the raw fraction as an offset within the node's order slot.
        /// </summary>
        public float InputPortCenterOffset(int portIndex)
        {
            return PortCenterOffset(portIndex, this.InputPortCount);
        }

        /// <summary>See <see cref="InputPortCenterOffset"/>; uses <see cref="OutputPortCount"/>.</summary>
        public float OutputPortCenterOffset(int portIndex)
        {
            return PortCenterOffset(portIndex, this.OutputPortCount);
        }

        private static float PortCenterOffset(int portIndex, int portCount)
        {
            if (portIndex < 0 || portCount <= 1)
            {
                return 0f;
            }

            var count = Math.Max(portCount, portIndex + 1);
            return ((portIndex + 0.5f) / count) - 0.5f;
        }
    }
}
