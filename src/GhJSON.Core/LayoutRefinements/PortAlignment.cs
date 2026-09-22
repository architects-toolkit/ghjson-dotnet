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
using System.Diagnostics;
using System.Drawing;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Post-layout refinement that aligns source nodes to their target component's
    /// input ports. Port geometry resolves through the caller-supplied metrics
    /// provider; without one the pass degrades to a no-op.
    /// </summary>
    internal static class PortAlignment
    {
        /// <summary>
        /// Single, coherent port-alignment pass. For every connection it computes the Y the
        /// source <em>would need</em> so its wire enters the target's specific input port
        /// horizontally, then moves each source to the median of all such desired positions.
        /// Wires are aligned port-to-port: the target's input port offset and the source's
        /// output port offset (via <c>conn.From.ParamIndex</c>) are both accounted for, so
        /// multi-output sources (e.g. Deconstruct, larger-than) can satisfy several ports at
        /// once. Floating parameter targets (panels, params) contribute through their own
        /// bounds, and floating parameter sources through their right-edge output grip.
        /// </summary>
        /// <param name="positions">Current bounds-center positions per layout key.</param>
        /// <param name="document">The GhJSON document supplying the connections.</param>
        /// <param name="metricsProvider">
        /// Optional lookup resolving a layout key to its measured node metrics; without
        /// it the pass skips so unmeasurable geometry never drives positions.
        /// </param>
        public static Dictionary<Guid, PointF> AlignToPorts(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document,
            Func<Guid, LayoutNodeMetrics?>? metricsProvider = null)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            if (metricsProvider == null)
            {
                Debug.WriteLine("[PortAlignment.AlignToPorts] No metrics provider; skipping.");
                return result;
            }

            if (document.Connections == null)
            {
                return result;
            }

            // Map connection endpoint ids to the same stable keys the layout engine emits,
            // so id-only components (no InstanceGuid) still participate in alignment.
            var idToGuidMap = ConnectionKeyMap.Build(document);

            var desired = new Dictionary<Guid, List<float>>();

            foreach (var conn in document.Connections)
            {
                try
                {
                    if (!idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) ||
                        !idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                    {
                        continue;
                    }

                    if (!result.TryGetValue(toGuid, out var targetPos) ||
                        !result.ContainsKey(fromGuid))
                    {
                        continue;
                    }

                    var targetMetrics = LayoutNodeMetrics.Resolve(metricsProvider, toGuid);
                    if (!TryGetInputPortDelta(targetMetrics, conn.To.ParamIndex, out var targetDelta))
                    {
                        continue;
                    }

                    var sourceMetrics = LayoutNodeMetrics.Resolve(metricsProvider, fromGuid);
                    var sourceDelta = GetOutputPortDelta(sourceMetrics, conn.From.ParamIndex);

                    // The wire is horizontal when
                    // sourcePivotY + sourceDelta == targetPivotY + targetDelta.
                    var desiredSourceY = targetPos.Y + targetDelta - sourceDelta;

                    if (!desired.TryGetValue(fromGuid, out var list))
                    {
                        list = new List<float>();
                        desired[fromGuid] = list;
                    }

                    list.Add(desiredSourceY);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PortAlignment.AlignToPorts] Skipping connection: {ex.Message}");
                }
            }

            foreach (var kvp in desired)
            {
                if (result.TryGetValue(kvp.Key, out var pos))
                {
                    result[kvp.Key] = new PointF(pos.X, Median(kvp.Value));
                }
            }

            return result;
        }

        /// <summary>
        /// Vertical distance between the center of the target's input port receiving the
        /// connection and the center of the target node itself. For component targets this
        /// is the connected input port's delta; for floating parameter targets the input
        /// grip is vertically centered on the param's own bounds. Returns false when the
        /// target cannot supply a port position at all.
        /// </summary>
        internal static bool TryGetInputPortDelta(
            LayoutNodeMetrics? targetMetrics,
            int? paramIndex,
            out float delta)
        {
            delta = 0f;

            if (targetMetrics == null)
            {
                return false;
            }

            if (targetMetrics.Kind == LayoutNodeKind.Component)
            {
                var index = paramIndex ?? -1;
                if (index >= 0 && index < targetMetrics.InputPortDeltas.Count)
                {
                    delta = targetMetrics.InputPortDeltas[index];
                }

                // Unknown input port: the component center is the mean of its ports,
                // so a zero delta still contributes a reasonable vote.
                return true;
            }

            // Floating parameters (panels, value params…) take wires at their left-edge
            // input grip, which is vertically centered on the param's own bounds.
            return targetMetrics.Kind == LayoutNodeKind.Parameter;
        }

        /// <summary>
        /// Vertical distance between the center of the source's output port feeding the
        /// connection and the center of the source node itself. Component sources use the
        /// connected output port's delta; floating parameter sources emit from their
        /// right-edge output grip, vertically centered on their own bounds. Returns 0 when
        /// the source cannot be measured so the connection still votes for the target port.
        /// </summary>
        internal static float GetOutputPortDelta(
            LayoutNodeMetrics? sourceMetrics,
            int? paramIndex)
        {
            if (sourceMetrics == null || sourceMetrics.Kind != LayoutNodeKind.Component)
            {
                return 0f;
            }

            var index = paramIndex ?? -1;
            if (index < 0 || index >= sourceMetrics.OutputPortDeltas.Count)
            {
                return 0f;
            }

            return sourceMetrics.OutputPortDeltas[index];
        }

        private static float Median(List<float> values)
        {
            values.Sort();
            var mid = values.Count / 2;
            if (values.Count % 2 == 1)
            {
                return values[mid];
            }

            return (values[mid - 1] + values[mid]) / 2f;
        }
    }
}
