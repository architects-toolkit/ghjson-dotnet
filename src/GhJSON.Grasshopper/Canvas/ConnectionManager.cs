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
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Handles creation of wire connections between components.
    /// Maps connection definitions from GhJSON to actual parameter wiring.
    /// </summary>
    public static class ConnectionManager
    {
        /// <summary>
        /// Creates all connections from a GhJSON document.
        /// </summary>
        /// <param name="document">GhJSON document with connection definitions</param>
        /// <param name="guidMapping">Dictionary mapping GUIDs to component instances</param>
        /// <returns>Number of connections created</returns>
        public static int CreateConnections(
            GhJsonDocument? document,
            Dictionary<Guid, IGH_DocumentObject>? guidMapping)
        {
            if (document?.Connections == null || document.Connections.Count == 0)
            {
                return 0;
            }

            var connectionsCreated = 0;
            var idToComponent = CanvasUtilities.BuildIdMapping(document, guidMapping);

            foreach (var connection in document.Connections)
            {
                try
                {
                    if (CreateConnection(connection, idToComponent))
                    {
                        connectionsCreated++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ConnectionManager] Error creating connection: {ex.Message}");
                }
            }

            Debug.WriteLine($"[ConnectionManager] Created {connectionsCreated} connections");
            return connectionsCreated;
        }

        /// <summary>
        /// Creates a single connection between components.
        /// </summary>
        private static bool CreateConnection(
            ConnectionPairing connection,
            Dictionary<int, IGH_DocumentObject> idToComponent)
        {
            // Get source and target components
            if (!idToComponent.TryGetValue(connection.From.Id, out var sourceObj) ||
                !idToComponent.TryGetValue(connection.To.Id, out var targetObj))
            {
                Debug.WriteLine($"[ConnectionManager] Component not found for connection");
                return false;
            }

            // Handle connections from stand-alone parameters
            IGH_Param? sourceParam = null;
            if (sourceObj is IGH_Param standAloneParam)
            {
                sourceParam = standAloneParam;
            }
            else if (sourceObj is IGH_Component sourceComp)
            {
                // Find source output parameter by index first, then fallback to name
                if (connection.From.ParamIndex.HasValue &&
                    connection.From.ParamIndex.Value >= 0 &&
                    connection.From.ParamIndex.Value < sourceComp.Params.Output.Count)
                {
                    sourceParam = sourceComp.Params.Output[connection.From.ParamIndex.Value];
                }
                else
                {
                    sourceParam = sourceComp.Params.Output.FirstOrDefault(p => p.Name == connection.From.ParamName) ??
                                 sourceComp.Params.Output.FirstOrDefault(p => p.NickName == connection.From.ParamName);
                }

                if (sourceParam == null)
                {
                    Debug.WriteLine($"[ConnectionManager] Source parameter '{connection.From.ParamName}' (index: {connection.From.ParamIndex}) not found");
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Find target input parameter
            IGH_Param? targetParam = null;

            if (targetObj is IGH_Param standAloneTargetParam)
            {
                // Target is a stand-alone parameter - it receives data directly
                targetParam = standAloneTargetParam;
            }
            else if (targetObj is IGH_Component targetComp)
            {
                // Find target input parameter by index first, then fallback to name
                if (connection.To.ParamIndex.HasValue &&
                    connection.To.ParamIndex.Value >= 0 &&
                    connection.To.ParamIndex.Value < targetComp.Params.Input.Count)
                {
                    targetParam = targetComp.Params.Input[connection.To.ParamIndex.Value];
                }
                else
                {
                    targetParam = targetComp.Params.Input.FirstOrDefault(p => p.Name == connection.To.ParamName) ??
                                 targetComp.Params.Input.FirstOrDefault(p => p.NickName == connection.To.ParamName);
                }

                if (targetParam == null)
                {
                    Debug.WriteLine($"[ConnectionManager] Target parameter '{connection.To.ParamName}' (index: {connection.To.ParamIndex}) not found");
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Create the connection
            targetParam.AddSource(sourceParam);

            string sourceName = sourceObj is IGH_Component sc ? sc.Name : sourceParam.Name;
            string targetName = targetObj is IGH_Component tc ? tc.Name : targetParam.Name;
            Debug.WriteLine($"[ConnectionManager] Connected {sourceName}.{sourceParam.Name} → {targetName}.{targetParam.Name}");

            return true;
        }
    }
}
