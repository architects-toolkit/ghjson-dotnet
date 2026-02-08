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

using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for old GhPython script components (<c>ZuiPythonComponent</c> from GhPython.dll).
    /// This covers:
    /// <list type="bullet">
    ///   <item>The generic GhPython Script component available in the Grasshopper toolbar.</item>
    ///   <item>Ladybug Tools components, which are pre-configured <c>ZuiPythonComponent</c> instances
    ///         registered with unique GUIDs.</item>
    ///   <item>Any other third-party plugin that bundles <c>ZuiPythonComponent</c>-based components
    ///         (e.g., Honeybee, Dragonfly).</item>
    /// </list>
    /// <para>
    /// Unlike Rhino 8 script components (C#, Python 3, IronPython 2) which implement
    /// <c>IScriptComponent</c>, old GhPython components do not expose that interface.
    /// Detection is therefore done by <b>type name</b> rather than by a single component GUID.
    /// </para>
    /// <para>
    /// Marshalling options (<c>MarshGuids</c>, <c>MarshOutputs</c>, <c>MarshInputs</c>) are
    /// <b>not applicable</b> to old GhPython components, so <see cref="PostPlacement"/> is a no-op.
    /// </para>
    /// </summary>
    internal sealed class GhPythonScriptHandler : BaseScriptHandler
    {
        /// <summary>
        /// The type name of the old GhPython component from <c>GhPython.dll</c>.
        /// </summary>
        private const string GhPythonTypeName = "ZuiPythonComponent";

        /// <inheritdoc/>
        public override string ExtensionKey => "gh.ghpython";

        /// <summary>
        /// Not used — detection is by type name, not GUID.
        /// </summary>
        protected override Guid ComponentGuid => Guid.Empty;

        /// <summary>
        /// Not used — detection is by type name, not component name.
        /// </summary>
        protected override string ComponentName => "GhPython Script";

        /// <summary>
        /// Matches any <c>ZuiPythonComponent</c> instance by type name.
        /// This covers the generic GhPython Script as well as Ladybug/Honeybee/Dragonfly
        /// components that are pre-configured <c>ZuiPythonComponent</c> instances.
        /// </summary>
        public override bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_Component && obj.GetType().Name == GhPythonTypeName;
        }

        /// <summary>
        /// Matches components that were serialized with the <c>gh.ghpython</c> extension key.
        /// </summary>
        public override bool CanHandle(GhJsonComponent component)
        {
            return component.ComponentState?.Extensions?.ContainsKey(ExtensionKey) == true;
        }

        /// <summary>
        /// No-op: old GhPython components do not implement <c>IScriptComponent</c>,
        /// so marshalling options are not applicable.
        /// </summary>
        public override void PostPlacement(GhJsonComponent component, IGH_DocumentObject obj)
        {
            // Intentionally empty — IScriptComponent marshalling is not available on ZuiPythonComponent.
        }
    }
}
