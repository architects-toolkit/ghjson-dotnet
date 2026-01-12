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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Manages capture and restoration of external connections during component replacement.
    /// External connections are those that cross the boundary between replaced and non-replaced objects.
    /// </summary>
    public static class ExternalConnectionManager
    {
        /// <summary>
        /// Captures external connections for a set of objects to be replaced.
        /// </summary>
        /// <param name="objectsToReplace">The objects that will be replaced.</param>
        /// <param name="allCanvasObjects">All objects currently on the canvas.</param>
        /// <returns>Captured connection information.</returns>
        public static CapturedConnections CaptureExternalConnections(
            IEnumerable<IGH_DocumentObject> objectsToReplace,
            IEnumerable<IGH_ActiveObject> allCanvasObjects)
        {
            var captured = new CapturedConnections();
            var replaceSet = new HashSet<Guid>(objectsToReplace.Select(o => o.InstanceGuid));
            
            // Cache for mapping parameters to their owning document objects
            var ownerCache = new Dictionary<IGH_Param, IGH_DocumentObject>();

            IGH_DocumentObject FindOwner(IGH_Param param)
            {
                if (param == null)
                {
                    return null;
                }

                if (ownerCache.TryGetValue(param, out var cached))
                {
                    return cached;
                }

                // Stand-alone parameters appear directly in the active object list
                IGH_DocumentObject owner = allCanvasObjects.FirstOrDefault(o => ReferenceEquals(o, param));

                // Otherwise, look for a component that owns this parameter
                owner ??= allCanvasObjects
                    .OfType<IGH_Component>()
                    .FirstOrDefault(comp => comp.Params.Input.Contains(param) || comp.Params.Output.Contains(param));

                ownerCache[param] = owner;
                return owner;
            }

            var seen = new HashSet<(Guid, string, Guid, string)>();

            foreach (var obj in objectsToReplace)
            {
                var thisGuid = obj.InstanceGuid;

                if (obj is IGH_Component comp)
                {
                    // Outgoing connections: component → external targets
                    foreach (var output in comp.Params.Output)
                    {
                        var sourceParamName = output.NickName;
                        foreach (var recipient in output.Recipients)
                        {
                            var targetOwner = FindOwner(recipient);
                            if (targetOwner == null)
                            {
                                continue;
                            }

                            var targetGuid = targetOwner.InstanceGuid;

                            // Only keep external connections
                            if (replaceSet.Contains(targetGuid))
                            {
                                continue;
                            }

                            var key = (thisGuid, sourceParamName, targetGuid, recipient.NickName);
                            if (seen.Add(key))
                            {
                                captured.Connections.Add(new ExternalConnection
                                {
                                    SourceGuid = thisGuid,
                                    SourceParam = sourceParamName,
                                    TargetGuid = targetGuid,
                                    TargetParam = recipient.NickName
                                });
                            }
                        }
                    }

                    // Incoming connections: external sources → component
                    foreach (var input in comp.Params.Input)
                    {
                        var targetParamName = input.NickName;
                        foreach (var source in input.Sources)
                        {
                            var sourceOwner = FindOwner(source);
                            if (sourceOwner == null)
                            {
                                continue;
                            }

                            var sourceGuid = sourceOwner.InstanceGuid;

                            if (replaceSet.Contains(sourceGuid))
                            {
                                continue;
                            }

                            var key = (sourceGuid, source.NickName, thisGuid, targetParamName);
                            if (seen.Add(key))
                            {
                                captured.Connections.Add(new ExternalConnection
                                {
                                    SourceGuid = sourceGuid,
                                    SourceParam = source.NickName,
                                    TargetGuid = thisGuid,
                                    TargetParam = targetParamName
                                });
                            }
                        }
                    }
                }
                else if (obj is IGH_Param param)
                {
                    // Stand-alone parameter being replaced

                    // Sources → this parameter
                    foreach (var source in param.Sources)
                    {
                        var sourceOwner = FindOwner(source);
                        if (sourceOwner == null)
                        {
                            continue;
                        }

                        var sourceGuid = sourceOwner.InstanceGuid;

                        if (replaceSet.Contains(sourceGuid))
                        {
                            continue;
                        }

                        var key = (sourceGuid, source.NickName, thisGuid, param.NickName);
                        if (seen.Add(key))
                        {
                            captured.Connections.Add(new ExternalConnection
                            {
                                SourceGuid = sourceGuid,
                                SourceParam = source.NickName,
                                TargetGuid = thisGuid,
                                TargetParam = param.NickName
                            });
                        }
                    }

                    // This parameter → recipients
                    foreach (var recipient in param.Recipients)
                    {
                        var targetOwner = FindOwner(recipient);
                        if (targetOwner == null)
                        {
                            continue;
                        }

                        var targetGuid = targetOwner.InstanceGuid;

                        if (replaceSet.Contains(targetGuid))
                        {
                            continue;
                        }

                        var key = (thisGuid, param.NickName, targetGuid, recipient.NickName);
                        if (seen.Add(key))
                        {
                            captured.Connections.Add(new ExternalConnection
                            {
                                SourceGuid = thisGuid,
                                SourceParam = param.NickName,
                                TargetGuid = targetGuid,
                                TargetParam = recipient.NickName
                            });
                        }
                    }
                }
            }

            return captured;
        }

        /// <summary>
        /// Restores captured external connections after replacement.
        /// </summary>
        /// <param name="captured">The captured connections to restore.</param>
        /// <returns>Number of successfully restored connections.</returns>
        public static int RestoreExternalConnections(CapturedConnections captured)
        {
            int restored = 0;

            foreach (var conn in captured.Connections)
            {
                try
                {
                    var success = ConnectionBuilder.ConnectComponents(
                        sourceGuid: conn.SourceGuid,
                        targetGuid: conn.TargetGuid,
                        sourceParamName: conn.SourceParam,
                        targetParamName: conn.TargetParam,
                        redraw: false);

                    if (success)
                    {
                        restored++;
                    }
                }
                catch
                {
                    // Silently continue on connection failures
                }
            }

            return restored;
        }
    }

    /// <summary>
    /// Represents a captured external connection.
    /// </summary>
    public class ExternalConnection
    {
        /// <summary>
        /// Gets or sets the source component GUID.
        /// </summary>
        public Guid SourceGuid { get; set; }

        /// <summary>
        /// Gets or sets the source parameter name.
        /// </summary>
        public string SourceParam { get; set; }

        /// <summary>
        /// Gets or sets the target component GUID.
        /// </summary>
        public Guid TargetGuid { get; set; }

        /// <summary>
        /// Gets or sets the target parameter name.
        /// </summary>
        public string TargetParam { get; set; }
    }

    /// <summary>
    /// Container for captured external connections.
    /// </summary>
    public class CapturedConnections
    {
        /// <summary>
        /// Gets the list of captured connections.
        /// </summary>
        public List<ExternalConnection> Connections { get; } = new List<ExternalConnection>();
    }
}
