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
using GhJSON.Core.LayoutRefinements;

namespace GhJSON.Core.Tests.LayoutRefinements
{
    /// <summary>
    /// Helpers building <see cref="LayoutNodeMetrics"/> providers from in-memory maps
    /// so the host-independent refinement passes can be tested without a canvas.
    /// </summary>
    internal static class MetricsProviders
    {
        /// <summary>Creates a metrics provider over the given id → metrics map.</summary>
        public static Func<Guid, LayoutNodeMetrics?> Of(Dictionary<Guid, LayoutNodeMetrics> map)
        {
            return id => map.TryGetValue(id, out var metrics) ? metrics : null;
        }

        /// <summary>Size-only metrics for a node that cannot contribute port geometry.</summary>
        public static LayoutNodeMetrics Size(float width, float height)
        {
            return new LayoutNodeMetrics(new SizeF(width, height), LayoutNodeKind.Other);
        }

        /// <summary>Parameter metrics: centered grips, no port deltas.</summary>
        public static LayoutNodeMetrics Parameter(float width, float height)
        {
            return new LayoutNodeMetrics(new SizeF(width, height), LayoutNodeKind.Parameter);
        }

        /// <summary>Component metrics with explicit input/output port deltas.</summary>
        public static LayoutNodeMetrics Component(
            float width,
            float height,
            IReadOnlyList<float>? inputDeltas = null,
            IReadOnlyList<float>? outputDeltas = null)
        {
            return new LayoutNodeMetrics(
                new SizeF(width, height),
                LayoutNodeKind.Component,
                inputDeltas,
                outputDeltas);
        }
    }
}
