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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Serializes Grasshopper canvas objects to GhJSON format.
    /// </summary>
    public static class GhJsonSerializer
    {
        /// <summary>
        /// Serializes a collection of Grasshopper objects to a GrasshopperDocument.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>A GrasshopperDocument representing the serialized objects.</returns>
        public static GrasshopperDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options = null)
        {
            options ??= SerializationOptions.Standard;
            var objectList = objects.ToList();
            var document = new GrasshopperDocument
            {
                SchemaVersion = "1.0",
                Components = new List<ComponentProperties>(),
                Connections = new List<ConnectionPairing>()
            };

            // Build GUID to ID mapping
            var guidToId = new Dictionary<Guid, int>();
            int nextId = 1;

            foreach (var obj in objectList)
            {
                guidToId[obj.InstanceGuid] = nextId++;
            }

            // Serialize components
            foreach (var obj in objectList)
            {
                var props = SerializeObject(obj, guidToId, options);
                if (props != null)
                {
                    document.Components.Add(props);
                }
            }

            // Extract connections
            if (options.IncludeConnections)
            {
                document.Connections = ExtractConnections(objectList, guidToId);
            }

            // Add metadata if requested
            if (options.IncludeMetadata)
            {
                document.Metadata = CreateMetadata(objectList);
            }

            return document;
        }

        private static ComponentProperties? SerializeObject(
            IGH_ActiveObject obj,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options)
        {
            if (obj is IGH_Component component)
            {
                return SerializeComponent(component, guidToId, options);
            }
            else if (obj is IGH_Param param)
            {
                return SerializeParameter(param, guidToId, options);
            }

            return null;
        }

        private static ComponentProperties SerializeComponent(
            IGH_Component component,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options)
        {
            var attrs = component.Attributes;
            var pivot = attrs?.Pivot ?? new System.Drawing.PointF(0, 0);

            var props = new ComponentProperties
            {
                Name = component.Name,
                NickName = component.NickName != component.Name ? component.NickName : null,
                ComponentGuid = component.ComponentGuid,
                InstanceGuid = component.InstanceGuid,
                Id = guidToId.TryGetValue(component.InstanceGuid, out var id) ? id : null,
                Pivot = new CompactPosition(pivot.X, pivot.Y)
            };

            // Extract component state
            if (options.IncludeComponentState)
            {
                props.ComponentState = ExtractComponentState(component);
            }

            // Extract parameter settings
            if (options.IncludeParameterSettings)
            {
                props.InputSettings = ExtractParameterSettings(component.Params.Input);
                props.OutputSettings = ExtractParameterSettings(component.Params.Output);
            }

            return props;
        }

        private static ComponentProperties SerializeParameter(
            IGH_Param param,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options)
        {
            var attrs = param.Attributes;
            var pivot = attrs?.Pivot ?? new System.Drawing.PointF(0, 0);

            return new ComponentProperties
            {
                Name = param.Name,
                NickName = param.NickName != param.Name ? param.NickName : null,
                ComponentGuid = param.ComponentGuid,
                InstanceGuid = param.InstanceGuid,
                Id = guidToId.TryGetValue(param.InstanceGuid, out var id) ? id : null,
                Pivot = new CompactPosition(pivot.X, pivot.Y)
            };
        }

        private static List<ConnectionPairing> ExtractConnections(
            List<IGH_ActiveObject> objects,
            Dictionary<Guid, int> guidToId)
        {
            var connections = new List<ConnectionPairing>();

            foreach (var obj in objects)
            {
                if (obj is IGH_Component component)
                {
                    foreach (var input in component.Params.Input)
                    {
                        foreach (var source in input.Sources)
                        {
                            if (guidToId.TryGetValue(source.InstanceGuid, out var fromId) &&
                                guidToId.TryGetValue(component.InstanceGuid, out var toId))
                            {
                                connections.Add(new ConnectionPairing
                                {
                                    From = new Connection { Id = fromId, ParamName = source.Name },
                                    To = new Connection { Id = toId, ParamName = input.Name }
                                });
                            }
                        }
                    }
                }
                else if (obj is IGH_Param param)
                {
                    foreach (var source in param.Sources)
                    {
                        if (guidToId.TryGetValue(source.InstanceGuid, out var fromId) &&
                            guidToId.TryGetValue(param.InstanceGuid, out var toId))
                        {
                            connections.Add(new ConnectionPairing
                            {
                                From = new Connection { Id = fromId, ParamName = source.Name },
                                To = new Connection { Id = toId, ParamName = param.Name }
                            });
                        }
                    }
                }
            }

            return connections;
        }

        private static ComponentState? ExtractComponentState(IGH_Component component)
        {
            // TODO: Extract component-specific state (sliders, panels, scripts, etc.)
            return null;
        }

        private static List<ParameterSettings>? ExtractParameterSettings(List<IGH_Param> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            return parameters.Select(p => new ParameterSettings
            {
                ParameterName = p.Name,
                NickName = p.NickName != p.Name ? p.NickName : null
            }).ToList();
        }

        private static DocumentMetadata CreateMetadata(List<IGH_ActiveObject> objects)
        {
            return new DocumentMetadata
            {
                ComponentCount = objects.Count,
                PluginVersion = "1.0.0"
            };
        }
    }
}
