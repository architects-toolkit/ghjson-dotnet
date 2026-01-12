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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Registry for component handlers. Provides extensible registration and lookup
    /// of handlers for different component types.
    /// </summary>
    public class ComponentHandlerRegistry
    {
        private static readonly Lazy<ComponentHandlerRegistry> _default =
            new Lazy<ComponentHandlerRegistry>(() =>
            {
                var registry = new ComponentHandlerRegistry();
                registry.RegisterBuiltInHandlers();
                return registry;
            });

        private readonly List<IComponentHandler> _handlers = new List<IComponentHandler>();
        private readonly Dictionary<Guid, IComponentHandler> _guidCache = new Dictionary<Guid, IComponentHandler>();
        private readonly Dictionary<Type, IComponentHandler> _typeCache = new Dictionary<Type, IComponentHandler>();
        private readonly object _lock = new object();
        private IComponentHandler? _defaultHandler;

        /// <summary>
        /// Gets the default registry instance with all built-in handlers registered.
        /// </summary>
        public static ComponentHandlerRegistry Default => _default.Value;

        /// <summary>
        /// Registers a component handler.
        /// </summary>
        /// <param name="handler">The handler to register.</param>
        public void Register(IComponentHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                _handlers.Add(handler);

                // Clear caches when new handlers are registered
                _guidCache.Clear();
                _typeCache.Clear();

                // Sort handlers by priority (descending)
                _handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                Debug.WriteLine($"[ComponentHandlerRegistry] Registered handler: {handler.GetType().Name} (Priority: {handler.Priority})");
            }
        }

        /// <summary>
        /// Unregisters handlers for the specified component GUID.
        /// </summary>
        /// <param name="componentGuid">The component GUID to unregister handlers for.</param>
        public void Unregister(Guid componentGuid)
        {
            lock (_lock)
            {
                _handlers.RemoveAll(h => h.SupportedComponentGuids.Contains(componentGuid));
                _guidCache.Remove(componentGuid);
            }
        }

        /// <summary>
        /// Unregisters handlers for the specified type.
        /// </summary>
        /// <param name="type">The type to unregister handlers for.</param>
        public void Unregister(Type type)
        {
            lock (_lock)
            {
                _handlers.RemoveAll(h => h.SupportedTypes.Contains(type));
                _typeCache.Remove(type);
            }
        }

        /// <summary>
        /// Gets the appropriate handler for a document object.
        /// </summary>
        /// <param name="obj">The document object to get a handler for.</param>
        /// <returns>The handler for the object, or the default handler if no specific handler is found.</returns>
        public IComponentHandler GetHandler(IGH_DocumentObject obj)
        {
            if (obj == null)
                return GetDefaultHandler();

            var componentGuid = obj.ComponentGuid;
            var objType = obj.GetType();

            lock (_lock)
            {
                // Check GUID cache first
                if (_guidCache.TryGetValue(componentGuid, out var cachedHandler))
                {
                    if (cachedHandler.CanHandle(obj))
                        return cachedHandler;
                }

                // Check type cache
                if (_typeCache.TryGetValue(objType, out cachedHandler))
                {
                    if (cachedHandler.CanHandle(obj))
                        return cachedHandler;
                }

                // Search all handlers (already sorted by priority)
                foreach (var handler in _handlers)
                {
                    // Check GUID match
                    if (handler.SupportedComponentGuids.Contains(componentGuid))
                    {
                        if (handler.CanHandle(obj))
                        {
                            _guidCache[componentGuid] = handler;
                            return handler;
                        }
                    }

                    // Check type match
                    if (handler.SupportedTypes.Any(t => t.IsAssignableFrom(objType)))
                    {
                        if (handler.CanHandle(obj))
                        {
                            _typeCache[objType] = handler;
                            return handler;
                        }
                    }
                }
            }

            return GetDefaultHandler();
        }

        /// <summary>
        /// Gets the handler for a specific component GUID.
        /// </summary>
        /// <param name="componentGuid">The component GUID.</param>
        /// <returns>The handler for the GUID, or the default handler if no specific handler is found.</returns>
        public IComponentHandler GetHandler(Guid componentGuid)
        {
            lock (_lock)
            {
                if (_guidCache.TryGetValue(componentGuid, out var cachedHandler))
                    return cachedHandler;

                foreach (var handler in _handlers)
                {
                    if (handler.SupportedComponentGuids.Contains(componentGuid))
                    {
                        _guidCache[componentGuid] = handler;
                        return handler;
                    }
                }
            }

            return GetDefaultHandler();
        }

        /// <summary>
        /// Gets the default handler used when no specific handler matches.
        /// </summary>
        /// <returns>The default handler.</returns>
        public IComponentHandler GetDefaultHandler()
        {
            lock (_lock)
            {
                if (_defaultHandler == null)
                {
                    _defaultHandler = _handlers.FirstOrDefault(h => h.Priority == 0)
                        ?? new DefaultComponentHandler();
                }
                return _defaultHandler;
            }
        }

        /// <summary>
        /// Sets a custom default handler.
        /// </summary>
        /// <param name="handler">The handler to use as default.</param>
        public void SetDefaultHandler(IComponentHandler handler)
        {
            lock (_lock)
            {
                _defaultHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }

        /// <summary>
        /// Gets all registered handlers.
        /// </summary>
        /// <returns>An enumerable of all registered handlers.</returns>
        public IEnumerable<IComponentHandler> GetAllHandlers()
        {
            lock (_lock)
            {
                return _handlers.ToList();
            }
        }

        /// <summary>
        /// Clears all registered handlers and caches.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
                _guidCache.Clear();
                _typeCache.Clear();
                _defaultHandler = null;
            }
        }

        /// <summary>
        /// Registers all built-in component handlers.
        /// </summary>
        private void RegisterBuiltInHandlers()
        {
            // Register in priority order (specific handlers first)
            Register(new SliderHandler());
            Register(new PanelHandler());
            Register(new ValueListHandler());
            Register(new ToggleHandler());
            Register(new ColorSwatchHandler());
            Register(new ButtonHandler());
            Register(new ScribbleHandler());
            Register(new ScriptHandler());

            // Register default handler last (lowest priority)
            Register(new DefaultComponentHandler());

            Debug.WriteLine($"[ComponentHandlerRegistry] Registered {_handlers.Count} built-in handlers");
        }
    }
}
