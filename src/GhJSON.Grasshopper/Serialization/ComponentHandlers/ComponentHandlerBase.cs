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
using System.Linq;
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Base class for component handlers.
    /// Provides default matching logic based on supported component GUIDs and/or supported .NET types.
    /// </summary>
    public abstract class ComponentHandlerBase : IComponentHandler
    {
        private readonly HashSet<Guid> _supportedComponentGuids;
        private readonly List<Type> _supportedTypes;

        protected ComponentHandlerBase(IEnumerable<Guid>? supportedComponentGuids = null, IEnumerable<Type>? supportedTypes = null)
        {
            _supportedComponentGuids = supportedComponentGuids != null
                ? new HashSet<Guid>(supportedComponentGuids)
                : new HashSet<Guid>();

            _supportedTypes = supportedTypes != null
                ? supportedTypes.ToList()
                : new List<Type>();
        }

        /// <inheritdoc/>
        public abstract int Priority { get; }

        /// <summary>
        /// Determines whether this handler can process the given document object.
        /// Default implementation checks supported GUIDs and supported types.
        /// Override for additional runtime checks.
        /// </summary>
        public virtual bool CanHandle(IGH_DocumentObject obj)
        {
            if (obj == null)
                return false;

            if (_supportedComponentGuids.Count > 0 && _supportedComponentGuids.Contains(obj.ComponentGuid))
                return true;

            if (_supportedTypes.Count > 0)
            {
                var objType = obj.GetType();
                return _supportedTypes.Any(t => t.IsAssignableFrom(objType));
            }

            return false;
        }

        internal bool SupportsComponentGuid(Guid componentGuid)
        {
            return _supportedComponentGuids.Contains(componentGuid);
        }

        internal bool SupportsType(Type type)
        {
            return _supportedTypes.Any(t => t == type);
        }

        /// <inheritdoc/>
        public abstract ComponentState? ExtractState(IGH_DocumentObject obj);

        /// <inheritdoc/>
        public abstract void ApplyState(IGH_DocumentObject obj, ComponentState state);
    }
}
