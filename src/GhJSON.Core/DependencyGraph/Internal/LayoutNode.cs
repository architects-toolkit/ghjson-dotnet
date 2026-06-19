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

        public Dictionary<Guid, int> Parents { get; set; } = new Dictionary<Guid, int>();

        public Dictionary<Guid, int> Children { get; set; } = new Dictionary<Guid, int>();
    }
}
