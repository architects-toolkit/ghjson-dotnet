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
using System.Linq;
using GhJSON.Core.SchemaModels;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Post-layout refinements that align source parameter components to their target
    /// component's input ports. Depends on <see cref="Instances.ActiveCanvas"/> for bounds
    /// and port positions; when no canvas is available these methods degrade to no-ops.
    /// </summary>
    internal static class PortAlignment
    {
        /// <summary>
        /// Single, coherent port-alignment pass that replaces the three previously competing
        /// passes (param-to-port, one-to-one, and connection-length minimization). For every
        /// connection it computes the Y the source <em>would need</em> so its wire enters the
        /// target's specific input port horizontally, then moves each source to the median of
        /// all such desired positions. Using the median keeps fan-out sources balanced and
        /// avoids the cumulative drift the old multi-pass approach produced.
        /// </summary>
        public static Dictionary<Guid, PointF> AlignToPorts(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var ghDocument = Instances.ActiveCanvas?.Document;
            if (ghDocument == null)
            {
                Debug.WriteLine("[PortAlignment.AlignToPorts] No active Grasshopper document; skipping.");
                return result;
            }

            if (document.Connections == null)
            {
                return result;
            }

            var idToGuidMap = document.GetIdToGuidMapping();
            var desired = new Dictionary<Guid, List<float>>();

            foreach (var conn in document.Connections)
            {
                if (!idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) ||
                    !idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                {
                    continue;
                }

                if (!result.TryGetValue(toGuid, out var targetPos))
                {
                    continue;
                }

                var childObj = ghDocument.FindObject(toGuid, false);
                if (!(childObj is IGH_Component childComp))
                {
                    continue;
                }

                var inputIdx = conn.To.ParamIndex ?? -1;
                if (inputIdx < 0 || inputIdx >= childComp.Params.Input.Count)
                {
                    continue;
                }

                var inputParam = childComp.Params.Input[inputIdx];
                if (inputParam?.Attributes == null || childObj.Attributes == null)
                {
                    continue;
                }

                var rect = inputParam.Attributes.Bounds;
                var childBounds = childObj.Attributes.Bounds;

                // Port offset relative to the component center is stable regardless of where
                // the component ends up, so apply it to the target's (already laid out) Y.
                var relativeDelta = (rect.Y + (rect.Height / 2f)) - (childBounds.Y + (childBounds.Height / 2f));
                var desiredSourceY = targetPos.Y + relativeDelta;

                if (!desired.TryGetValue(fromGuid, out var list))
                {
                    list = new List<float>();
                    desired[fromGuid] = list;
                }

                list.Add(desiredSourceY);
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

        public static Dictionary<Guid, PointF> AlignParamsToInputPorts(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document,
            float spacingY)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var ghDocument = Instances.ActiveCanvas?.Document;
            if (ghDocument == null)
            {
                Debug.WriteLine("[PortAlignment.AlignParamsToInputPorts] No active Grasshopper document; skipping.");
                return result;
            }

            var idToGuidMap = document.GetIdToGuidMapping();
            var connectionsByTarget = new Dictionary<Guid, List<(Guid sourceGuid, int targetParamIndex)>>();

            if (document.Connections != null)
            {
                foreach (var conn in document.Connections)
                {
                    if (idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) &&
                        idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                    {
                        if (!connectionsByTarget.ContainsKey(toGuid))
                        {
                            connectionsByTarget[toGuid] = new List<(Guid, int)>();
                        }

                        connectionsByTarget[toGuid].Add((fromGuid, conn.To.ParamIndex ?? -1));
                    }
                }
            }

            var byColumn = positions.GroupBy(kvp => kvp.Value.X)
                                   .OrderBy(g => g.Key)
                                   .Select(g => g.ToList())
                                   .ToList();

            for (int i = 1; i < byColumn.Count; i++)
            {
                var prevCol = byColumn[i - 1];
                var currCol = byColumn[i];

                foreach (var childKvp in currCol)
                {
                    if (!connectionsByTarget.TryGetValue(childKvp.Key, out var connections))
                    {
                        continue;
                    }

                    var childObj = ghDocument.FindObject(childKvp.Key, false);
                    if (!(childObj is IGH_Component childComp))
                    {
                        continue;
                    }

                    var parents = connections.Where(c => prevCol.Any(p => p.Key == c.sourceGuid)).ToList();

                    if (parents.Count > 1 &&
                        childComp.Params.Input.Count == parents.Count &&
                        parents.All(p => ghDocument.FindObject(p.sourceGuid, false) is IGH_Param))
                    {
                        foreach (var parent in parents.OrderBy(p => p.targetParamIndex))
                        {
                            int inputIdx = parent.targetParamIndex;
                            if (inputIdx >= 0 && inputIdx < childComp.Params.Input.Count)
                            {
                                var inputParam = childComp.Params.Input[inputIdx];
                                var rect = inputParam.Attributes.Bounds;

                                float inputPivotY = rect.Y + rect.Height / 2f;
                                var childBounds = childObj.Attributes.Bounds;
                                float childCenterY = childBounds.Y + childBounds.Height / 2f;
                                float deltaY = inputPivotY - childCenterY;

                                float targetY = childKvp.Value.Y + deltaY;
                                result[parent.sourceGuid] = new PointF(result[parent.sourceGuid].X, targetY);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static Dictionary<Guid, PointF> AlignOneToOneConnections(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document,
            float spacingY)
        {
            var result = new Dictionary<Guid, PointF>(positions);

            var ghDocument = Instances.ActiveCanvas?.Document;
            if (ghDocument == null)
            {
                Debug.WriteLine("[PortAlignment.AlignOneToOneConnections] No active Grasshopper document; skipping.");
                return result;
            }

            var idToGuidMap = document.GetIdToGuidMapping();
            var childrenByParent = new Dictionary<Guid, List<(Guid childGuid, int inputIndex)>>();
            var parentsByChild = new Dictionary<Guid, List<(Guid parentGuid, int inputIndex)>>();

            if (document.Connections != null)
            {
                foreach (var conn in document.Connections)
                {
                    if (idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) &&
                        idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                    {
                        if (!childrenByParent.ContainsKey(fromGuid))
                        {
                            childrenByParent[fromGuid] = new List<(Guid, int)>();
                        }

                        childrenByParent[fromGuid].Add((toGuid, conn.To.ParamIndex ?? -1));

                        if (!parentsByChild.ContainsKey(toGuid))
                        {
                            parentsByChild[toGuid] = new List<(Guid, int)>();
                        }

                        parentsByChild[toGuid].Add((fromGuid, conn.To.ParamIndex ?? -1));
                    }
                }
            }

            foreach (var parentKvp in positions)
            {
                if (!childrenByParent.TryGetValue(parentKvp.Key, out var children) || children.Count != 1)
                {
                    continue;
                }

                var childGuid = children[0].childGuid;
                var inputIndex = children[0].inputIndex;

                if (!parentsByChild.TryGetValue(childGuid, out var parents) || parents.Count != 1)
                {
                    continue;
                }

                if (inputIndex < 0)
                {
                    continue;
                }

                var childObj = ghDocument.FindObject(childGuid, false);
                if (!(childObj is IGH_Component childComp))
                {
                    continue;
                }

                if (inputIndex >= childComp.Params.Input.Count)
                {
                    continue;
                }

                var inputParam = childComp.Params.Input[inputIndex];
                var rect = inputParam.Attributes.Bounds;

                float inputPivotY = rect.Y + rect.Height / 2f;
                var childBounds = childObj.Attributes.Bounds;
                float childCenterY = childBounds.Y + childBounds.Height / 2f;
                float deltaY = inputPivotY - childCenterY;

                if (result.TryGetValue(childGuid, out var childPos))
                {
                    // Align the parent's vertical center with the target input port.
                    // Previous implementation added spacingY/2 unconditionally, producing
                    // cumulative drift on chained single-wire pairs.
                    float targetY = childPos.Y + deltaY;
                    result[parentKvp.Key] = new PointF(parentKvp.Value.X, targetY);
                }
            }

            return result;
        }
    }
}
