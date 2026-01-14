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
using System.Drawing;
using System.Linq;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Deserialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.PutOperations
{
    /// <summary>
    /// Places GhJSON objects on the Grasshopper canvas.
    /// </summary>
    internal static class CanvasPlacer
    {
        /// <summary>
        /// Places a GhJSON document on the canvas.
        /// </summary>
        /// <param name="document">The document to place.</param>
        /// <param name="options">Put options.</param>
        /// <returns>The put result.</returns>
        public static PutResult Put(GhJsonDocument document, PutOptions? options = null)
        {
            options ??= PutOptions.Default;
            var result = new PutResult { Success = true };

            var ghDoc = Instances.ActiveCanvas?.Document;
            if (ghDoc == null)
            {
                result.Success = false;
                result.ErrorMessage = "No active Grasshopper document";
                return result;
            }

            var deserializationOptions = new DeserializationOptions
            {
                RegenerateInstanceGuids = options.RegenerateInstanceGuids,
                SkipInvalidComponents = options.SkipInvalidComponents
            };

            // Place components
            var idToObject = new Dictionary<int, IGH_DocumentObject>();

            foreach (var component in document.Components)
            {
                var obj = ComponentInstantiator.Create(component, deserializationOptions);

                if (obj == null)
                {
                    if (!options.SkipInvalidComponents)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to create component: {component.Name ?? component.ComponentGuid?.ToString()}";
                        return result;
                    }

                    result.FailedComponents.Add(component.Name ?? component.ComponentGuid?.ToString() ?? "unknown");
                    continue;
                }

                // Apply offset to pivot
                if (obj.Attributes != null && component.Pivot != null)
                {
                    obj.Attributes.Pivot = new PointF(
                        (float)(component.Pivot.X + options.Offset.X),
                        (float)(component.Pivot.Y + options.Offset.Y));
                }

                // Add to document
                ghDoc.AddObject(obj, false);
                result.PlacedObjects.Add(obj);
                result.ComponentsPlaced++;

                if (component.Id.HasValue)
                {
                    idToObject[component.Id.Value] = obj;
                    result.IdToGuidMapping[component.Id.Value] = obj.InstanceGuid;
                }

                // Select if requested
                if (options.SelectPlacedObjects && obj.Attributes != null)
                {
                    obj.Attributes.Selected = true;
                }
            }

            // Create connections
            if (options.CreateConnections && document.Connections != null)
            {
                foreach (var connection in document.Connections)
                {
                    if (CreateConnection(connection, idToObject))
                    {
                        result.ConnectionsCreated++;
                    }
                    else
                    {
                        result.Warnings.Add($"Failed to create connection from {connection.From.Id} to {connection.To.Id}");
                    }
                }
            }

            // Create groups
            if (options.CreateGroups && document.Groups != null)
            {
                foreach (var group in document.Groups)
                {
                    if (CreateGroup(group, idToObject, ghDoc))
                    {
                        result.GroupsCreated++;
                    }
                    else
                    {
                        result.Warnings.Add($"Failed to create group: {group.Name}");
                    }
                }
            }

            // Expire solution
            ghDoc.NewSolution(true);

            return result;
        }

        private static bool CreateConnection(GhJsonConnection connection, Dictionary<int, IGH_DocumentObject> idToObject)
        {
            if (!idToObject.TryGetValue(connection.From.Id, out var fromObj) ||
                !idToObject.TryGetValue(connection.To.Id, out var toObj))
            {
                return false;
            }

            var sourceParam = GetParameter(fromObj, connection.From, isInput: false);
            var targetParam = GetParameter(toObj, connection.To, isInput: true);

            if (sourceParam == null || targetParam == null)
            {
                return false;
            }

            targetParam.AddSource(sourceParam);
            return true;
        }

        private static IGH_Param? GetParameter(IGH_DocumentObject obj, GhJsonConnectionEndpoint endpoint, bool isInput)
        {
            IList<IGH_Param>? parameters = null;

            if (obj is IGH_Component comp)
            {
                parameters = isInput ? comp.Params.Input : comp.Params.Output;
            }
            else if (obj is IGH_Param param)
            {
                // For floating parameters, return the param itself
                return param;
            }

            if (parameters == null || parameters.Count == 0)
            {
                return null;
            }

            // Try by index first
            if (endpoint.ParamIndex.HasValue && endpoint.ParamIndex.Value < parameters.Count)
            {
                return parameters[endpoint.ParamIndex.Value];
            }

            // Fallback to name
            if (!string.IsNullOrEmpty(endpoint.ParamName))
            {
                return parameters.FirstOrDefault(p =>
                    p.Name.Equals(endpoint.ParamName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private static bool CreateGroup(GhJsonGroup groupDef, Dictionary<int, IGH_DocumentObject> idToObject, GH_Document ghDoc)
        {
            var members = new List<Guid>();

            foreach (var memberId in groupDef.Members)
            {
                if (idToObject.TryGetValue(memberId, out var obj))
                {
                    members.Add(obj.InstanceGuid);
                }
            }

            if (members.Count == 0)
            {
                return false;
            }

            var group = new GH_Group();
            group.CreateAttributes();
            group.NickName = groupDef.Name ?? string.Empty;

            // Parse color
            if (!string.IsNullOrEmpty(groupDef.Color))
            {
                var color = ParseArgbColor(groupDef.Color);
                if (color.HasValue)
                {
                    group.Colour = color.Value;
                }
            }

            // Add members
            foreach (var memberGuid in members)
            {
                group.AddObject(memberGuid);
            }

            ghDoc.AddObject(group, false);
            return true;
        }

        private static Color? ParseArgbColor(string colorString)
        {
            if (!colorString.StartsWith("argb:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var parts = colorString.Substring(5).Split(',');
            if (parts.Length != 4)
            {
                return null;
            }

            if (int.TryParse(parts[0], out var a) &&
                int.TryParse(parts[1], out var r) &&
                int.TryParse(parts[2], out var g) &&
                int.TryParse(parts[3], out var b))
            {
                return Color.FromArgb(a, r, g, b);
            }

            return null;
        }
    }
}
