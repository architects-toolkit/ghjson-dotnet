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
using System.Diagnostics;
using System.Linq;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.GetOperations
{
    /// <summary>
    /// Reads objects from the Grasshopper canvas and converts them to GhJSON format.
    /// </summary>
    internal static class CanvasReader
    {
        /// <summary>
        /// Gets all objects from the current Grasshopper document.
        /// </summary>
        /// <param name="options">Get options.</param>
        /// <returns>A GhJSON document containing the objects.</returns>
        public static GhJsonDocument GetAll(GetOptions? options = null)
        {
            var doc = Instances.ActiveCanvas?.Document;
            if (doc == null)
            {
                return GhJSON.Core.GhJson.CreateDocumentBuilder().Build();
            }

            return GetFromDocument(doc, options);
        }

        /// <summary>
        /// Gets selected objects from the current Grasshopper document.
        /// </summary>
        /// <returns>A GhJSON document containing the selected objects.</returns>
        public static GhJsonDocument GetSelected()
        {
            var options = new GetOptions { SelectedOnly = true };
            return GetAll(options);
        }

        /// <summary>
        /// Gets objects by their GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of objects to get.</param>
        /// <returns>A GhJSON document containing the specified objects.</returns>
        public static GhJsonDocument GetByGuids(IEnumerable<Guid> guids)
        {
            var doc = Instances.ActiveCanvas?.Document;
            if (doc == null)
            {
                return GhJSON.Core.GhJson.CreateDocumentBuilder().Build();
            }

            var guidSet = new HashSet<Guid>(guids);
            var objects = doc.Objects.Where(obj => guidSet.Contains(obj.InstanceGuid)).ToList();

            return CreateDocument(objects, new GetOptions());
        }

        private static GhJsonDocument GetFromDocument(GH_Document doc, GetOptions? options)
        {
            options ??= GetOptions.Default;

            var objects = options.SelectedOnly
                ? doc.SelectedObjects().ToList()
                : doc.Objects.ToList();

#if DEBUG
            Debug.WriteLine($"[CanvasReader.GetFromDocument] SelectedOnly={options.SelectedOnly}, Objects={objects.Count}");
#endif

            return CreateDocument(objects, options);
        }

        internal static GhJsonDocument CreateDocument(List<IGH_DocumentObject> objects, GetOptions options)
        {
            var builder = GhJSON.Core.GhJson.CreateDocumentBuilder();

            // Filter out groups for separate handling
            var components = objects.Where(obj => !(obj is GH_Group)).ToList();
            var groups = objects.OfType<GH_Group>().ToList();

#if DEBUG
            Debug.WriteLine($"[CanvasReader.CreateDocument] Components={components.Count}, Groups={groups.Count}, IncludeConnections={options.IncludeConnections}");
#endif

            // Create ID mappings
            var guidToId = new Dictionary<Guid, int>();
            var nextId = 1;

            // Serialize components
            foreach (var obj in components)
            {
                var component = ObjectHandlerOrchestrator.Serialize(obj);
                component.Id = nextId;
                guidToId[obj.InstanceGuid] = nextId;
                nextId++;

                builder = builder.AddComponent(component);
            }

#if DEBUG
            Debug.WriteLine($"[CanvasReader.CreateDocument] Serialized {components.Count} components");
#endif

            // Extract connections
            if (options.IncludeConnections)
            {
                var connections = ExtractConnections(components, guidToId);
#if DEBUG
                Debug.WriteLine($"[CanvasReader.CreateDocument] Extracted {connections.Count} connections");
#endif
                builder = builder.AddConnections(connections);
            }

            // Extract groups
            if (options.IncludeGroups && groups.Any())
            {
                builder = builder.AddGroups(ExtractGroups(groups, guidToId, ref nextId));
            }

            return builder.Build();
        }

        private static List<GhJsonConnection> ExtractConnections(
            List<IGH_DocumentObject> objects,
            Dictionary<Guid, int> guidToId)
        {
            var connections = new List<GhJsonConnection>();
            var processedConnections = new HashSet<string>();

            foreach (var obj in objects)
            {
                IList<IGH_Param>? outputParams = null;

                if (obj is IGH_Component comp)
                {
                    outputParams = comp.Params.Output;
                }
                else if (obj is IGH_Param param)
                {
                    // Floating parameters have their own recipients
                    outputParams = new List<IGH_Param> { param };
                }

                if (outputParams == null)
                {
                    continue;
                }

                foreach (var output in outputParams)
                {
                    if (!guidToId.TryGetValue(obj.InstanceGuid, out var fromId))
                    {
                        continue;
                    }

                    var fromIndex = outputParams.IndexOf(output);

                    foreach (var recipient in output.Recipients)
                    {
                        var recipientOwner = recipient.Attributes?.GetTopLevel?.DocObject;
                        if (recipientOwner == null)
                        {
                            continue;
                        }

                        if (!guidToId.TryGetValue(recipientOwner.InstanceGuid, out var toId))
                        {
                            continue;
                        }

                        var toIndex = GetParameterIndex(recipientOwner, recipient, isInput: true);

                        // Create connection key to avoid duplicates
                        var connKey = $"{fromId}:{fromIndex}-{toId}:{toIndex}";
                        if (!processedConnections.Add(connKey))
                        {
                            continue;
                        }

                        connections.Add(new GhJsonConnection
                        {
                            From = new GhJsonConnectionEndpoint
                            {
                                Id = fromId,
                                ParamName = output.Name,
                                ParamIndex = fromIndex
                            },
                            To = new GhJsonConnectionEndpoint
                            {
                                Id = toId,
                                ParamName = recipient.Name,
                                ParamIndex = toIndex
                            }
                        });
                    }
                }
            }

            return connections;
        }

        private static int GetParameterIndex(IGH_DocumentObject obj, IGH_Param param, bool isInput)
        {
            if (obj is IGH_Component comp)
            {
                var list = isInput ? comp.Params.Input : comp.Params.Output;
                return list.IndexOf(param);
            }

            return 0;
        }

        private static List<GhJsonGroup> ExtractGroups(
            List<GH_Group> groups,
            Dictionary<Guid, int> guidToId,
            ref int nextId)
        {
            var result = new List<GhJsonGroup>();

            foreach (var group in groups)
            {
                var members = new List<int>();

                foreach (var memberGuid in group.ObjectIDs)
                {
                    if (guidToId.TryGetValue(memberGuid, out var memberId))
                    {
                        members.Add(memberId);
                    }
                }

                if (members.Count == 0)
                {
                    continue;
                }

                result.Add(new GhJsonGroup
                {
                    Id = nextId++,
                    InstanceGuid = group.InstanceGuid,
                    Name = group.NickName,
                    Color = $"argb:{group.Colour.A},{group.Colour.R},{group.Colour.G},{group.Colour.B}",
                    Members = members
                });
            }

            return result;
        }
    }
}
