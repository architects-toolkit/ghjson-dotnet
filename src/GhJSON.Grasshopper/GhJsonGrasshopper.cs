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
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Deserialization;
using GhJSON.Grasshopper.GetOperations;
using GhJSON.Grasshopper.PutOperations;
using GhJSON.Grasshopper.Serialization;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper
{
    /// <summary>
    /// Main entry point for Grasshopper operations.
    /// Provides methods for serializing and deserializing Grasshopper objects,
    /// and for reading from and writing to the canvas.
    /// </summary>
    public static class GhJsonGrasshopper
    {
        #region Serialize (GH → GhJSON)

        /// <summary>
        /// Serializes Grasshopper document objects to a GhJSON document.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Optional serialization options.</param>
        /// <returns>A GhJSON document containing the serialized objects.</returns>
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_DocumentObject> objects,
            SerializationOptions? options = null)
        {
            options ??= SerializationOptions.Default;

            var builder = GhJSON.Core.GhJson.CreateDocumentBuilder();

            var guidToId = new Dictionary<Guid, int>();
            var nextId = 1;

            foreach (var obj in objects)
            {
                var component = ObjectHandlerOrchestrator.Serialize(obj);

                if (options.AssignSequentialIds)
                {
                    component.Id = nextId;
                    guidToId[obj.InstanceGuid] = nextId;
                    nextId++;
                }

                builder = builder.AddComponent(component);
            }

            return builder.Build();
        }

        #endregion

        #region Deserialize (GhJSON → GH objects)

        /// <summary>
        /// Deserializes a GhJSON document to Grasshopper objects (not placed on canvas).
        /// </summary>
        /// <param name="document">The document to deserialize.</param>
        /// <param name="options">Optional deserialization options.</param>
        /// <returns>The deserialization result containing created objects.</returns>
        public static DeserializationResult Deserialize(
            GhJsonDocument document,
            DeserializationOptions? options = null)
        {
            options ??= DeserializationOptions.Default;
            var result = new DeserializationResult { Success = true };

            foreach (var component in document.Components)
            {
                var obj = ComponentInstantiator.Create(component, options);

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

                result.Objects.Add(obj);

                if (component.Id.HasValue)
                {
                    result.IdToObjectMapping[component.Id.Value] = obj;
                }
            }

            return result;
        }

        #endregion

        #region Get (read from canvas)

        /// <summary>
        /// Gets all objects from the current Grasshopper canvas.
        /// </summary>
        /// <param name="options">Optional get options.</param>
        /// <returns>A GhJSON document containing all canvas objects.</returns>
        public static GhJsonDocument Get(GetOptions? options = null)
        {
            return CanvasReader.GetAll(options);
        }

        /// <summary>
        /// Gets only selected objects from the current Grasshopper canvas.
        /// </summary>
        /// <returns>A GhJSON document containing selected objects.</returns>
        public static GhJsonDocument GetSelected()
        {
            return CanvasReader.GetSelected();
        }

        /// <summary>
        /// Gets objects by their GUIDs from the current Grasshopper canvas.
        /// </summary>
        /// <param name="guids">The GUIDs of objects to get.</param>
        /// <returns>A GhJSON document containing the specified objects.</returns>
        public static GhJsonDocument GetByGuids(IEnumerable<Guid> guids)
        {
            return CanvasReader.GetByGuids(guids);
        }

        #endregion

        #region Put (place on canvas)

        /// <summary>
        /// Places a GhJSON document on the Grasshopper canvas.
        /// </summary>
        /// <param name="document">The document to place.</param>
        /// <param name="options">Optional put options.</param>
        /// <returns>The put result.</returns>
        public static PutResult Put(
            GhJsonDocument document,
            PutOptions? options = null)
        {
            return CanvasPlacer.Put(document, options);
        }

        #endregion

        #region Data Type Extensibility

        /// <summary>
        /// Registers a custom data type serializer.
        /// </summary>
        /// <typeparam name="T">The type this serializer handles.</typeparam>
        /// <param name="serializer">The serializer to register.</param>
        public static void RegisterCustomDataTypeSerializer<T>(IDataTypeSerializer<T> serializer)
        {
            DataTypeRegistry.Register(serializer);
        }

        /// <summary>
        /// Unregisters a custom data type serializer.
        /// </summary>
        /// <typeparam name="T">The type to unregister.</typeparam>
        public static void UnregisterCustomDataTypeSerializer<T>()
        {
            DataTypeRegistry.Unregister<T>();
        }

        /// <summary>
        /// Gets all registered data type serializers.
        /// </summary>
        /// <returns>An enumerable of all registered serializers.</returns>
        public static IEnumerable<IDataTypeSerializer> GetRegisteredDataTypeSerializers()
        {
            return DataTypeRegistry.GetAll();
        }

        #endregion

        #region Object Handler Extensibility

        /// <summary>
        /// Registers a custom object handler.
        /// </summary>
        /// <param name="handler">The handler to register.</param>
        public static void RegisterCustomObjectHandler(IObjectHandler handler)
        {
            ObjectHandlerRegistry.Register(handler);
        }

        /// <summary>
        /// Unregisters a custom object handler.
        /// </summary>
        /// <param name="handler">The handler to unregister.</param>
        public static void UnregisterCustomObjectHandler(IObjectHandler handler)
        {
            ObjectHandlerRegistry.Unregister(handler);
        }

        /// <summary>
        /// Gets all registered object handlers.
        /// </summary>
        /// <returns>An enumerable of all registered handlers.</returns>
        public static IEnumerable<IObjectHandler> GetRegisteredObjectHandlers()
        {
            return ObjectHandlerRegistry.GetAll();
        }

        #endregion
    }
}
