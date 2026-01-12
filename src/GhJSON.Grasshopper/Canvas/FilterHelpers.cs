/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
using System.Linq;
using GhJSON.Core.Models.Document;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Internal helper methods for applying filters to canvas objects.
    /// </summary>
    internal static class FilterHelpers
    {
        /// <summary>
        /// Checks if an object has runtime messages at the specified level.
        /// </summary>
        public static bool HasRuntimeMessage(IGH_ActiveObject obj, GH_RuntimeMessageLevel level)
        {
            return obj.RuntimeMessages(level).Any();
        }

        /// <summary>
        /// Applies an attribute filter to an object.
        /// </summary>
        public static bool ApplyAttributeFilter(IGH_ActiveObject obj, AttributeFilter filter)
        {
            if (filter.Exclude != 0)
            {
                if ((filter.Exclude & GetAttributes.Selected) != 0 && obj.Attributes.Selected)
                {
                    return false;
                }

                if ((filter.Exclude & GetAttributes.Unselected) != 0 && !obj.Attributes.Selected)
                {
                    return false;
                }

                if ((filter.Exclude & GetAttributes.HasError) != 0 && HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Error))
                {
                    return false;
                }

                if ((filter.Exclude & GetAttributes.HasWarning) != 0 && HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Warning))
                {
                    return false;
                }

                if ((filter.Exclude & GetAttributes.HasRemark) != 0 && HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Remark))
                {
                    return false;
                }

                if (obj is GH_Component c)
                {
                    if ((filter.Exclude & GetAttributes.Enabled) != 0 && !c.Locked)
                    {
                        return false;
                    }

                    if ((filter.Exclude & GetAttributes.Disabled) != 0 && c.Locked)
                    {
                        return false;
                    }

                    if ((filter.Exclude & GetAttributes.PreviewOn) != 0 && c.IsPreviewCapable && !c.Hidden)
                    {
                        return false;
                    }

                    if ((filter.Exclude & GetAttributes.PreviewOff) != 0 && c.IsPreviewCapable && c.Hidden)
                    {
                        return false;
                    }

                    if ((filter.Exclude & GetAttributes.PreviewCapable) != 0 && c.IsPreviewCapable)
                    {
                        return false;
                    }

                    if ((filter.Exclude & GetAttributes.NotPreviewCapable) != 0 && !c.IsPreviewCapable)
                    {
                        return false;
                    }
                }
            }

            if (filter.Include == 0)
            {
                return true;
            }

            if ((filter.Include & GetAttributes.Selected) != 0 && obj.Attributes.Selected)
            {
                return true;
            }

            if ((filter.Include & GetAttributes.Unselected) != 0 && !obj.Attributes.Selected)
            {
                return true;
            }

            if ((filter.Include & GetAttributes.HasError) != 0 && HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Error))
            {
                return true;
            }

            if ((filter.Include & GetAttributes.HasWarning) != 0 && HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Warning))
            {
                return true;
            }

            if ((filter.Include & GetAttributes.HasRemark) != 0 && HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Remark))
            {
                return true;
            }

            if (obj is GH_Component comp)
            {
                if ((filter.Include & GetAttributes.Enabled) != 0 && !comp.Locked)
                {
                    return true;
                }

                if ((filter.Include & GetAttributes.Disabled) != 0 && comp.Locked)
                {
                    return true;
                }

                if ((filter.Include & GetAttributes.PreviewOn) != 0 && comp.IsPreviewCapable && !comp.Hidden)
                {
                    return true;
                }

                if ((filter.Include & GetAttributes.PreviewOff) != 0 && comp.IsPreviewCapable && comp.Hidden)
                {
                    return true;
                }

                if ((filter.Include & GetAttributes.PreviewCapable) != 0 && comp.IsPreviewCapable)
                {
                    return true;
                }

                if ((filter.Include & GetAttributes.NotPreviewCapable) != 0 && !comp.IsPreviewCapable)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies a category filter to an object.
        /// </summary>
        public static bool ApplyCategoryFilter(IGH_ActiveObject obj, CategoryFilter filter)
        {
            if (obj is not GH_DocumentObject docObj)
            {
                return filter.Include.Count == 0;
            }

            var category = docObj.Category;
            var subCategory = docObj.SubCategory;

            if (filter.Exclude.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(category) && filter.Exclude.Contains(category))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(subCategory) && filter.Exclude.Contains(subCategory))
                {
                    return false;
                }
            }

            if (filter.Include.Count == 0)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(subCategory))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(category) && filter.Include.Contains(category))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(subCategory) && filter.Include.Contains(subCategory))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Applies a type filter to an object.
        /// </summary>
        public static bool ApplyTypeFilter(IGH_ActiveObject obj, TypeFilter filter, Dictionary<Guid, (int indegree, int outdegree)> degrees)
        {
            var isParam = obj is IGH_Param;
            var isComponent = obj is IGH_Component;

            if (filter.Exclude != 0)
            {
                if ((filter.Exclude & GetObjectKinds.Params) != 0 && isParam)
                {
                    return false;
                }

                if ((filter.Exclude & GetObjectKinds.Components) != 0 && isComponent)
                {
                    return false;
                }
            }

            if (filter.ExcludeRoles != 0)
            {
                if (MatchesNodeRoles(obj.InstanceGuid, filter.ExcludeRoles, degrees))
                {
                    return false;
                }
            }

            if (filter.Include == 0 && filter.IncludeRoles == 0)
            {
                return true;
            }

            var matches = false;

            if (filter.Include != 0)
            {
                if ((filter.Include & GetObjectKinds.Params) != 0 && isParam)
                {
                    matches = true;
                }

                if ((filter.Include & GetObjectKinds.Components) != 0 && isComponent)
                {
                    matches = true;
                }
            }

            if (filter.IncludeRoles != 0)
            {
                if (MatchesNodeRoles(obj.InstanceGuid, filter.IncludeRoles, degrees))
                {
                    matches = true;
                }
            }

            return matches;
        }

        /// <summary>
        /// Builds a dictionary of node degrees (incoming/outgoing connections) from a GhJSON document.
        /// </summary>
        public static Dictionary<Guid, (int indegree, int outdegree)> BuildDegrees(GhJsonDocument doc)
        {
            var degrees = new Dictionary<Guid, (int indegree, int outdegree)>();
            if (doc.Connections == null)
            {
                return degrees;
            }

            var idToGuid = doc.GetIdToGuidMapping();
            foreach (var conn in doc.Connections)
            {
                if (!conn.TryResolveGuids(idToGuid, out var fromGuid, out var toGuid))
                {
                    continue;
                }

                if (!degrees.TryGetValue(fromGuid, out var fromDeg))
                {
                    fromDeg = (0, 0);
                }

                if (!degrees.TryGetValue(toGuid, out var toDeg))
                {
                    toDeg = (0, 0);
                }

                degrees[fromGuid] = (fromDeg.indegree, fromDeg.outdegree + 1);
                degrees[toGuid] = (toDeg.indegree + 1, toDeg.outdegree);
            }

            return degrees;
        }

        /// <summary>
        /// Checks if a node matches the specified role flags based on its connection degrees.
        /// </summary>
        public static bool MatchesNodeRoles(Guid guid, GetNodeRoles roles, Dictionary<Guid, (int indegree, int outdegree)> degrees)
        {
            degrees.TryGetValue(guid, out var deg);
            var indeg = deg.indegree;
            var outdeg = deg.outdegree;

            var matches = false;

            if ((roles & GetNodeRoles.StartNodes) != 0 && indeg == 0 && outdeg > 0)
            {
                matches = true;
            }

            if ((roles & GetNodeRoles.EndNodes) != 0 && outdeg == 0 && indeg > 0)
            {
                matches = true;
            }

            if ((roles & GetNodeRoles.MiddleNodes) != 0 && indeg > 0 && outdeg > 0)
            {
                matches = true;
            }

            if ((roles & GetNodeRoles.IsolatedNodes) != 0 && indeg == 0 && outdeg == 0)
            {
                matches = true;
            }

            return matches;
        }
    }
}
