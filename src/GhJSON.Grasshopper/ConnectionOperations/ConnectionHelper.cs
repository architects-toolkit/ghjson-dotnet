/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.ConnectionOperations
{
    /// <summary>
    /// Internal helpers for resolving Grasshopper parameters when wiring components together.
    /// </summary>
    internal static class ConnectionHelper
    {
        /// <summary>
        /// Resolves an output parameter on a document object.
        /// </summary>
        /// <param name="obj">The source object (component or standalone parameter).</param>
        /// <param name="paramName">The parameter nickname or name. If null/empty, the first output is used.</param>
        /// <returns>The resolved output parameter, or null if not found.</returns>
        public static IGH_Param? ResolveOutput(IGH_DocumentObject obj, string? paramName)
        {
            if (obj is IGH_Param p)
            {
                return p;
            }

            if (obj is not IGH_Component comp)
            {
                return null;
            }

            var outputs = comp.Params.Output;
            if (string.IsNullOrWhiteSpace(paramName))
            {
                return outputs.FirstOrDefault();
            }

            return FindParamByNickNameOrName(outputs, paramName);
        }

        /// <summary>
        /// Resolves an input parameter on a document object.
        /// </summary>
        /// <param name="obj">The target object (component or standalone parameter).</param>
        /// <param name="paramName">The parameter nickname or name. If null/empty, the first input is used.</param>
        /// <returns>The resolved input parameter, or null if not found.</returns>
        public static IGH_Param? ResolveInput(IGH_DocumentObject obj, string? paramName)
        {
            if (obj is IGH_Param p)
            {
                return p;
            }

            if (obj is not IGH_Component comp)
            {
                return null;
            }

            var inputs = comp.Params.Input;
            if (string.IsNullOrWhiteSpace(paramName))
            {
                return inputs.FirstOrDefault();
            }

            return FindParamByNickNameOrName(inputs, paramName);
        }

        /// <summary>
        /// Finds a parameter by nickname, then by name, and finally by numeric index.
        /// </summary>
        /// <param name="parameters">The list of parameters to search.</param>
        /// <param name="paramName">The nickname, name, or index to match.</param>
        /// <returns>The matching parameter, or null if not found.</returns>
        public static IGH_Param? FindParamByNickNameOrName(IList<IGH_Param> parameters, string paramName)
        {
            if (parameters == null)
            {
                return null;
            }

            var param = parameters.FirstOrDefault(p => string.Equals(p.NickName, paramName, StringComparison.OrdinalIgnoreCase));
            if (param != null)
            {
                return param;
            }

            param = parameters.FirstOrDefault(p => string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase));
            if (param != null)
            {
                return param;
            }

            if (int.TryParse(paramName, out var index) && index >= 0 && index < parameters.Count)
            {
                return parameters[index];
            }

            return null;
        }

        /// <summary>
        /// Finds a parameter by index first, then by name/nickname.
        /// This matches the priority used by GhJSON connection placement.
        /// </summary>
        /// <param name="parameters">The list of parameters to search.</param>
        /// <param name="paramName">The optional nickname or name to match.</param>
        /// <param name="paramIndex">The optional zero-based parameter index.</param>
        /// <returns>The matching parameter, or null if not found.</returns>
        public static IGH_Param? FindParamByIndexNameOrNickName(IList<IGH_Param> parameters, string? paramName, int? paramIndex)
        {
            if (parameters == null)
            {
                return null;
            }

            if (paramIndex.HasValue && paramIndex.Value >= 0 && paramIndex.Value < parameters.Count)
            {
                return parameters[paramIndex.Value];
            }

            if (!string.IsNullOrWhiteSpace(paramName))
            {
                return FindParamByNickNameOrName(parameters, paramName);
            }

            return null;
        }

        /// <summary>
        /// Finds the document object that owns a parameter.
        /// Stand-alone parameters are returned as themselves; parameters owned by a
        /// component return that component.
        /// </summary>
        /// <param name="param">The parameter whose owner is required.</param>
        /// <param name="objects">The document objects to search.</param>
        /// <returns>The owning document object, or null if not found.</returns>
        public static IGH_DocumentObject? FindOwner(IGH_Param param, IEnumerable<IGH_DocumentObject> objects)
        {
            if (param == null)
            {
                return null;
            }

            var owner = objects.FirstOrDefault(o => ReferenceEquals(o, param));
            owner ??= objects
                .OfType<IGH_Component>()
                .FirstOrDefault(comp => comp.Params.Input.Contains(param) || comp.Params.Output.Contains(param));

            return owner;
        }
    }
}
