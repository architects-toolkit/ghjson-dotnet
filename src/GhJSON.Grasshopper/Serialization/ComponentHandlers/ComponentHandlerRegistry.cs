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
using GhJSON.Core.Models.Components;
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
        private readonly object _lock = new object();

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
                _handlers.RemoveAll(h => h is ComponentHandlerBase b && b.SupportsComponentGuid(componentGuid));
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
                _handlers.RemoveAll(h => h is ComponentHandlerBase b && b.SupportsType(type));
            }
        }

        public ComponentState? ComponentStateToGhJson(IGH_DocumentObject obj)
        {
            if (obj == null)
                return null;

            var handlers = GetMatchingHandlers(obj);
            if (handlers.Count == 0)
                return null;

            ComponentState? merged = null;
            foreach (var handler in handlers)
            {
                ComponentState? state = null;
                try
                {
                    state = handler.ExtractState(obj);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ComponentHandlerRegistry] Error extracting state in {handler.GetType().Name}: {ex.Message}");
                }

                merged = MergeComponentStates(merged, state);
            }

            return merged;
        }

        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj == null || state == null)
                return;

            // Apply from lowest to highest priority so higher priority wins on conflict.
            var handlers = GetMatchingHandlers(obj);
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];
                try
                {
                    handler.ApplyState(obj, state);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ComponentHandlerRegistry] Error applying state in {handler.GetType().Name}: {ex.Message}");
                }
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
            }
        }

        private List<IComponentHandler> GetMatchingHandlers(IGH_DocumentObject obj)
        {
            lock (_lock)
            {
                // Already kept sorted by Priority descending.
                return _handlers.Where(h => h.CanHandle(obj)).ToList();
            }
        }

        private static ComponentState? MergeComponentStates(ComponentState? baseState, ComponentState? overrideState)
        {
            if (baseState == null)
                return overrideState;
            if (overrideState == null)
                return baseState;

            var merged = new ComponentState
            {
                Value = overrideState.Value ?? baseState.Value,
                PersistentData = overrideState.PersistentData ?? baseState.PersistentData,
                VBCode = overrideState.VBCode ?? baseState.VBCode,
                Selected = overrideState.Selected ?? baseState.Selected,
                Locked = overrideState.Locked ?? baseState.Locked,
                Hidden = overrideState.Hidden ?? baseState.Hidden,
                Multiline = overrideState.Multiline ?? baseState.Multiline,
                Wrap = overrideState.Wrap ?? baseState.Wrap,
                Color = overrideState.Color ?? baseState.Color,
                MarshInputs = overrideState.MarshInputs ?? baseState.MarshInputs,
                MarshOutputs = overrideState.MarshOutputs ?? baseState.MarshOutputs,
                ShowStandardOutput = overrideState.ShowStandardOutput ?? baseState.ShowStandardOutput,
                ListMode = overrideState.ListMode ?? baseState.ListMode,
                ListItems = overrideState.ListItems ?? baseState.ListItems,
                Font = overrideState.Font ?? baseState.Font,
                Corners = overrideState.Corners ?? baseState.Corners,
                DrawIndices = overrideState.DrawIndices ?? baseState.DrawIndices,
                DrawPaths = overrideState.DrawPaths ?? baseState.DrawPaths,
                Alignment = overrideState.Alignment ?? baseState.Alignment,
                SpecialCodes = overrideState.SpecialCodes ?? baseState.SpecialCodes,
                Bounds = overrideState.Bounds ?? baseState.Bounds,
                Rounding = overrideState.Rounding ?? baseState.Rounding,
            };

            if (baseState.AdditionalProperties != null || overrideState.AdditionalProperties != null)
            {
                merged.AdditionalProperties = new Dictionary<string, object>();

                if (baseState.AdditionalProperties != null)
                {
                    foreach (var kvp in baseState.AdditionalProperties)
                    {
                        merged.AdditionalProperties[kvp.Key] = kvp.Value;
                    }
                }

                if (overrideState.AdditionalProperties != null)
                {
                    foreach (var kvp in overrideState.AdditionalProperties)
                    {
                        merged.AdditionalProperties[kvp.Key] = kvp.Value;
                    }
                }

                if (merged.AdditionalProperties.Count == 0)
                {
                    merged.AdditionalProperties = null;
                }
            }

            return merged;
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
            Register(new DefaultComponentHandler());

            Debug.WriteLine($"[ComponentHandlerRegistry] Registered {_handlers.Count} built-in handlers");
        }
    }
}
