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
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handles serialization/deserialization for a specific component type.
    /// Implementations provide component-specific logic for extracting and applying
    /// state and values during GhJSON serialization/deserialization.
    /// </summary>
    public interface IComponentHandler
    {
        /// <summary>
        /// Gets the component GUIDs this handler supports.
        /// Return empty to rely on <see cref="SupportedTypes"/> only.
        /// </summary>
        IEnumerable<Guid> SupportedComponentGuids { get; }

        /// <summary>
        /// Gets the .NET types this handler supports.
        /// Return empty to rely on <see cref="SupportedComponentGuids"/> only.
        /// </summary>
        IEnumerable<Type> SupportedTypes { get; }

        /// <summary>
        /// Gets the priority for handler selection. Higher values take precedence.
        /// Use 100 for specific handlers, 0 for the default handler.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if this handler can process the given document object.
        /// This allows handlers to perform additional runtime checks beyond GUID/Type matching.
        /// </summary>
        /// <param name="obj">The document object to check.</param>
        /// <returns>True if this handler can process the object.</returns>
        bool CanHandle(IGH_DocumentObject obj);

        /// <summary>
        /// Extracts the component state from a Grasshopper component.
        /// </summary>
        /// <param name="obj">The document object to extract state from.</param>
        /// <returns>The extracted component state, or null if no state to extract.</returns>
        ComponentState? ExtractState(IGH_DocumentObject obj);

        /// <summary>
        /// Extracts the universal value from a component.
        /// The universal value is the primary data value for components like sliders, panels, etc.
        /// </summary>
        /// <param name="obj">The document object to extract value from.</param>
        /// <returns>The extracted value, or null if no value to extract.</returns>
        object? ExtractValue(IGH_DocumentObject obj);

        /// <summary>
        /// Applies the component state to a Grasshopper component.
        /// </summary>
        /// <param name="obj">The document object to apply state to.</param>
        /// <param name="state">The state to apply.</param>
        void ApplyState(IGH_DocumentObject obj, ComponentState state);

        /// <summary>
        /// Applies the universal value to a component.
        /// </summary>
        /// <param name="obj">The document object to apply value to.</param>
        /// <param name="value">The value to apply.</param>
        void ApplyValue(IGH_DocumentObject obj, object value);
    }
}
