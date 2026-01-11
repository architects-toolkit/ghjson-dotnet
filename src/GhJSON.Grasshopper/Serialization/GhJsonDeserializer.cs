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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Document;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Deserializes GhJSON documents to Grasshopper objects.
    /// </summary>
    public static class GhJsonDeserializer
    {
        /// <summary>
        /// Deserializes a GrasshopperDocument to Grasshopper objects.
        /// </summary>
        /// <param name="document">The document to deserialize.</param>
        /// <param name="options">Deserialization options.</param>
        /// <returns>The deserialization result containing the created components.</returns>
        public static DeserializationResult Deserialize(
            GrasshopperDocument document,
            DeserializationOptions? options = null)
        {
            options ??= DeserializationOptions.Standard;
            var result = new DeserializationResult();
            var idToObject = new Dictionary<int, IGH_DocumentObject>();

            foreach (var componentProps in document.Components)
            {
                try
                {
                    var obj = CreateComponent(componentProps, options);
                    if (obj != null)
                    {
                        result.Components.Add(obj);
                        if (componentProps.Id.HasValue)
                        {
                            idToObject[componentProps.Id.Value] = obj;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to create component '{componentProps.Name}': {ex.Message}");
                }
            }

            // Create connections
            if (options.CreateConnections && document.Connections != null)
            {
                foreach (var connection in document.Connections)
                {
                    try
                    {
                        CreateConnection(connection, idToObject);
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Failed to create connection: {ex.Message}");
                    }
                }
            }

            return result;
        }

        private static IGH_DocumentObject? CreateComponent(
            ComponentProperties props,
            DeserializationOptions options)
        {
            // Try to find the component by GUID first
            var proxy = Instances.ComponentServer.FindObjectByName(props.Name, true, true);
            if (proxy == null && props.ComponentGuid != Guid.Empty)
            {
                proxy = Instances.ComponentServer.EmitObjectProxy(props.ComponentGuid);
            }

            if (proxy == null)
            {
                throw new InvalidOperationException($"Component '{props.Name}' not found");
            }

            var obj = proxy.CreateInstance();
            if (obj == null)
            {
                throw new InvalidOperationException($"Failed to instantiate component '{props.Name}'");
            }

            // Set position
            PointF pivot = props.Pivot;
            obj.CreateAttributes();
            if (obj.Attributes != null)
            {
                obj.Attributes.Pivot = pivot;
            }

            // Set instance GUID if specified
            if (props.InstanceGuid != Guid.Empty && options.PreserveInstanceGuids)
            {
                obj.NewInstanceGuid(props.InstanceGuid);
            }

            // Apply component state
            if (props.ComponentState != null && options.ApplyComponentState)
            {
                ApplyComponentState(obj, props.ComponentState);
            }

            return obj;
        }

        private static void ApplyComponentState(IGH_DocumentObject obj, ComponentState state)
        {
            // Apply locked state
            if (state.Locked.HasValue && obj is IGH_ActiveObject activeObj)
            {
                activeObj.Locked = state.Locked.Value;
            }

            // Apply hidden state
            if (state.Hidden.HasValue && obj is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            // TODO: Apply component-specific state (slider values, panel text, script code, etc.)
        }

        private static void CreateConnection(
            Core.Models.Connections.ConnectionPairing connection,
            Dictionary<int, IGH_DocumentObject> idToObject)
        {
            if (!idToObject.TryGetValue(connection.From.Id, out var fromObj) ||
                !idToObject.TryGetValue(connection.To.Id, out var toObj))
            {
                throw new InvalidOperationException("Connection endpoint not found");
            }

            IGH_Param? sourceParam = null;
            IGH_Param? targetParam = null;

            // Find source parameter
            if (fromObj is IGH_Param directParam)
            {
                sourceParam = directParam;
            }
            else if (fromObj is IGH_Component fromComponent)
            {
                sourceParam = fromComponent.Params.Output.Find(p => p.Name == connection.From.ParamName);
            }

            // Find target parameter
            if (toObj is IGH_Param directTargetParam)
            {
                targetParam = directTargetParam;
            }
            else if (toObj is IGH_Component toComponent)
            {
                targetParam = toComponent.Params.Input.Find(p => p.Name == connection.To.ParamName);
            }

            if (sourceParam == null || targetParam == null)
            {
                throw new InvalidOperationException("Could not find connection parameters");
            }

            targetParam.AddSource(sourceParam);
        }
    }

    /// <summary>
    /// Result of a deserialization operation.
    /// </summary>
    public class DeserializationResult
    {
        /// <summary>
        /// Gets the list of created components.
        /// </summary>
        public List<IGH_DocumentObject> Components { get; } = new List<IGH_DocumentObject>();

        /// <summary>
        /// Gets the list of errors that occurred during deserialization.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the list of warnings that occurred during deserialization.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets a value indicating whether deserialization was successful (no errors).
        /// </summary>
        public bool IsSuccess => Errors.Count == 0;
    }
}
