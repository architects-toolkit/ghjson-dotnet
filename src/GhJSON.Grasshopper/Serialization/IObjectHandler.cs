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

using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Interface for object handlers that serialize and deserialize specific Grasshopper objects.
    /// Handlers are registered with the ObjectHandlerRegistry and processed in priority order.
    /// </summary>
    public interface IObjectHandler
    {
        /// <summary>
        /// Gets the priority of this handler. Lower values are processed first.
        /// Built-in handlers use priority 0, extension handlers should use priority >= 100.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the schema extension URL if this handler adds a schema extension.
        /// Return null if no extension is added.
        /// </summary>
        string? SchemaExtensionUrl { get; }

        /// <summary>
        /// Determines if this handler can process the given document object during serialization.
        /// </summary>
        /// <param name="obj">The document object to check.</param>
        /// <returns>True if this handler can process the object.</returns>
        bool CanHandle(IGH_DocumentObject obj);

        /// <summary>
        /// Determines if this handler can process the given serialized component during deserialization.
        /// </summary>
        /// <param name="component">The serialized component to check.</param>
        /// <returns>True if this handler can process the component.</returns>
        bool CanHandle(GhJsonComponent component);

        /// <summary>
        /// Serializes properties from the document object to the component.
        /// The handler should only set properties it is responsible for.
        /// Properties already set by higher-priority handlers should not be overwritten.
        /// </summary>
        /// <param name="obj">The source document object.</param>
        /// <param name="component">The target component to populate.</param>
        void Serialize(IGH_DocumentObject obj, GhJsonComponent component);

        /// <summary>
        /// Deserializes properties from the component to the document object.
        /// The handler should only set properties it is responsible for.
        /// Properties already set by higher-priority handlers should not be overwritten.
        /// </summary>
        /// <param name="component">The source component.</param>
        /// <param name="obj">The target document object to configure.</param>
        void Deserialize(GhJsonComponent component, IGH_DocumentObject obj);
    }
}
