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
    /// Optional interface for handlers that need to apply properties after the component
    /// has been added to the Grasshopper document. This is necessary for properties that
    /// get reset during <c>AddedToDocument</c> (e.g., <c>IScriptComponent</c> marshalling options).
    /// Handlers implementing this interface must also implement <see cref="IObjectHandler"/>.
    /// </summary>
    public interface IPostPlacementHandler
    {
        /// <summary>
        /// Applies properties that must be set after the component is added to the document.
        /// Called by <see cref="PutOperations.CanvasPlacer"/> after <c>GH_Document.AddObject</c>.
        /// </summary>
        /// <param name="component">The source GhJSON component definition.</param>
        /// <param name="obj">The placed document object (already in the document).</param>
        void PostPlacement(GhJsonComponent component, IGH_DocumentObject obj);
    }
}
