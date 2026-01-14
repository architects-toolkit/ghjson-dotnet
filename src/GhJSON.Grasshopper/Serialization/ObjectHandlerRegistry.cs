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

using System.Collections.Generic;
using System.Linq;
using GhJSON.Grasshopper.Serialization.ObjectHandlers;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Registry for object handlers.
    /// Manages handlers for serializing and deserializing specific Grasshopper objects.
    /// </summary>
    public static class ObjectHandlerRegistry
    {
        private static readonly List<IObjectHandler> Handlers = new List<IObjectHandler>();
        private static bool _initialized = false;

        /// <summary>
        /// Ensures the registry is initialized with built-in handlers.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (Handlers)
            {
                if (_initialized)
                {
                    return;
                }

                // Register built-in handlers in priority order
                RegisterBuiltInHandlers();
                _initialized = true;
            }
        }

        /// <summary>
        /// Registers an object handler.
        /// </summary>
        /// <param name="handler">The handler to register.</param>
        public static void Register(IObjectHandler handler)
        {
            lock (Handlers)
            {
                Handlers.Add(handler);
                // Sort by priority (lower values first)
                Handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        /// <summary>
        /// Unregisters an object handler.
        /// </summary>
        /// <param name="handler">The handler to unregister.</param>
        public static void Unregister(IObjectHandler handler)
        {
            lock (Handlers)
            {
                Handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Gets all registered handlers in priority order.
        /// </summary>
        /// <returns>An enumerable of all registered handlers.</returns>
        public static IEnumerable<IObjectHandler> GetAll()
        {
            EnsureInitialized();
            lock (Handlers)
            {
                return Handlers.ToList();
            }
        }

        /// <summary>
        /// Gets all schema extension URLs from registered handlers.
        /// </summary>
        /// <returns>An enumerable of schema extension URLs.</returns>
        public static IEnumerable<string> GetSchemaExtensionUrls()
        {
            EnsureInitialized();
            lock (Handlers)
            {
                return Handlers
                    .Where(h => !string.IsNullOrEmpty(h.SchemaExtensionUrl))
                    .Select(h => h.SchemaExtensionUrl!)
                    .Distinct()
                    .ToList();
            }
        }

        private static void RegisterBuiltInHandlers()
        {
            // Core handlers (priority 0) - process in order
            Register(new IdentificationHandler());
            Register(new PivotHandler());
            Register(new SelectedPropertyHandler());
            Register(new LockedPropertyHandler());
            Register(new HiddenPropertyHandler());
            Register(new RuntimeMessagesHandler());
            Register(new IOIdentificationHandler());
            Register(new IOModifiersHandler());
            Register(new InternalizedDataHandler());

            // Extension handlers (priority 100) - component-specific
            Register(new NumberSliderHandler());
            Register(new PanelHandler());
            Register(new ScribbleHandler());
            Register(new ValueListHandler());
        }
    }
}
