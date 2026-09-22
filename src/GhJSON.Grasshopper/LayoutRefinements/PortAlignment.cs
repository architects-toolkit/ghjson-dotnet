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
using GhJSON.Grasshopper.GetOperations;
using GhJSON.Grasshopper.Shared;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Post-layout refinements that align source parameter components to their target
    /// component's input ports. Port geometry resolves through the caller-supplied
    /// object provider first, then the active canvas document; with neither available
    /// these methods degrade to no-ops.
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
        /// <param name="objectProvider">
        /// Optional caller-owned lookup resolving a layout key to its Grasshopper object;
        /// consulted before the live document so objects not yet on the canvas (gh_put)
        /// contribute their port geometry.
        /// </param>
        public static Dictionary<Guid, PointF> AlignToPorts(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document,
            Func<Guid, IGH_DocumentObject?>? objectProvider = null)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var ghDocument = CanvasReader.GetActiveDocument();
            if (ghDocument == null && objectProvider == null)
            {
                Debug.WriteLine("[PortAlignment.AlignToPorts] No active Grasshopper document and no object provider; skipping.");
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

                    var targetObj = ResolveObject(ghDocument, objectProvider, toGuid);
                    if (!TryGetInputPortDelta(targetObj, conn.To.ParamIndex, out var targetDelta))
                    {
                        continue;
                    }

                    var sourceObj = ResolveObject(ghDocument, objectProvider, fromGuid);
                    var sourceDelta = GetOutputPortDelta(sourceObj, conn.From.ParamIndex);

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
        /// Resolves a layout key to its Grasshopper object, preferring the caller-supplied
        /// provider (fresh or selected objects) and falling back to the live document.
        /// </summary>
        internal static IGH_DocumentObject? ResolveObject(
            GH_Document? ghDocument,
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Guid guid)
        {
            if (objectProvider != null)
            {
                try
                {
                    if (objectProvider(guid) is IGH_DocumentObject provided)
                    {
                        return provided;
                    }
                }
                catch
                {
                    // Provider failure falls through to the live document lookup.
                }
            }

            return ghDocument?.FindObject(guid, false);
        }

        /// <summary>
        /// Vertical distance between the center of the target's input port receiving the
        /// connection and the center of the target object itself. For component targets this
        /// is the connected input parameter's bounds; for floating parameter targets the
        /// input grip is vertically centered on the param's own bounds. Returns false when
        /// the target cannot supply a port position at all.
        /// </summary>
        internal static bool TryGetInputPortDelta(
            IGH_DocumentObject? targetObj,
            int? paramIndex,
            out float delta)
        {
            delta = 0f;

            if (targetObj?.Attributes == null)
            {
                return false;
            }

            if (targetObj is IGH_Component component)
            {
                var index = paramIndex ?? -1;
                if (index < 0 || index >= component.Params.Input.Count)
                {
                    // Unknown input port: the component center is the mean of its ports,
                    // so a zero delta still contributes a reasonable vote.
                    return true;
                }

                var inputParam = component.Params.Input[index];
                if (inputParam?.Attributes == null)
                {
                    return true;
                }

                delta = PivotSemantics.BoundsCenter(inputParam.Attributes.Bounds).Y
                    - PivotSemantics.BoundsCenter(targetObj.Attributes.Bounds).Y;
                return true;
            }

            // Floating parameters (panels, value params…) take wires at their left-edge
            // input grip, which is vertically centered on the param's own bounds.
            return targetObj is IGH_Param;
        }

        /// <summary>
        /// Vertical distance between the center of the source's output port feeding the
        /// connection and the center of the source object itself. Component sources use the
        /// connected output parameter's bounds; floating parameter sources emit from their
        /// right-edge output grip, vertically centered on their own bounds. Returns 0 when
        /// the source cannot be measured so the connection still votes for the target port.
        /// </summary>
        internal static float GetOutputPortDelta(
            IGH_DocumentObject? sourceObj,
            int? paramIndex)
        {
            if (sourceObj?.Attributes == null || !(sourceObj is IGH_Component component))
            {
                return 0f;
            }

            var index = paramIndex ?? -1;
            if (index < 0 || index >= component.Params.Output.Count)
            {
                return 0f;
            }

            var outputParam = component.Params.Output[index];
            if (outputParam?.Attributes == null)
            {
                return 0f;
            }

            return PivotSemantics.BoundsCenter(outputParam.Attributes.Bounds).Y
                - PivotSemantics.BoundsCenter(sourceObj.Attributes.Bounds).Y;
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
