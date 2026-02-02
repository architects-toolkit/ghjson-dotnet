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
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Orchestrates the serialization and deserialization of Grasshopper objects
    /// using registered object handlers in priority order.
    /// </summary>
    internal static class ObjectHandlerOrchestrator
    {
        /// <summary>
        /// Serializes a Grasshopper document object to a GhJSON component.
        /// Applies all compatible handlers in priority order using shared state.
        /// Handlers build upon each other's work, enabling natural dependency chains.
        /// </summary>
        /// <param name="obj">The document object to serialize.</param>
        /// <returns>The serialized component.</returns>
        public static GhJsonComponent Serialize(IGH_DocumentObject obj)
        {
#if DEBUG
            Debug.WriteLine($"[ObjectHandlerOrchestrator.Serialize] Serializing object: {obj?.Name}, Type: {obj?.GetType().Name}");
#endif

            var component = new GhJsonComponent();

            // Pre-initialize lists to ensure handlers only add to them, never overwrite
            component.InputSettings = new List<GhJsonParameterSettings>();
            component.OutputSettings = new List<GhJsonParameterSettings>();

            var handlerCount = 0;

            foreach (var handler in ObjectHandlerRegistry.GetAll())
            {
                if (handler.CanHandle(obj))
                {
                    // Sequential stateful execution: handlers build on shared component
                    handler.Serialize(obj, component);
                    handlerCount++;
                }
            }

#if DEBUG
            Debug.WriteLine($"[ObjectHandlerOrchestrator.Serialize] Applied {handlerCount} handlers, ComponentName: {component.Name}");
#endif

            return component;
        }

        /// <summary>
        /// Serializes multiple Grasshopper document objects.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <returns>List of serialized components.</returns>
        public static List<GhJsonComponent> Serialize(IEnumerable<IGH_DocumentObject> objects)
        {
            var components = new List<GhJsonComponent>();

            foreach (var obj in objects)
            {
                components.Add(Serialize(obj));
            }

            return components;
        }

        /// <summary>
        /// Deserializes a GhJSON component to configure a Grasshopper document object.
        /// Applies all compatible handlers in priority order.
        /// Note: Deserialization reads from component, so no override protection needed.
        /// </summary>
        /// <param name="component">The component to deserialize.</param>
        /// <param name="obj">The document object to configure.</param>
        public static void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
#if DEBUG
            Debug.WriteLine($"[ObjectHandlerOrchestrator.Deserialize] Deserializing component: {component.Name}, TargetType: {obj?.GetType().Name}");
#endif

            var handlerCount = 0;
            foreach (var handler in ObjectHandlerRegistry.GetAll())
            {
                if (handler.CanHandle(component))
                {
                    handler.Deserialize(component, obj);
                    handlerCount++;
                }
            }

#if DEBUG
            Debug.WriteLine($"[ObjectHandlerOrchestrator.Deserialize] Applied {handlerCount} handlers");
#endif
        }
    }
}
