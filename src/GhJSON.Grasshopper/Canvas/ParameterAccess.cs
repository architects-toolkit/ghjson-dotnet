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
using System.Diagnostics;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Utility methods for accessing component parameters.
    /// Provides methods to get inputs/outputs by name or index.
    /// </summary>
    public static class ParameterAccess
    {
        /// <summary>
        /// Gets all input parameters of a component.
        /// </summary>
        /// <param name="component">The component to get inputs from.</param>
        /// <returns>List of input parameters.</returns>
        public static List<IGH_Param> GetAllInputs(IGH_Component component)
        {
            return component.Params.Input;
        }

        /// <summary>
        /// Gets all output parameters of a component.
        /// </summary>
        /// <param name="component">The component to get outputs from.</param>
        /// <returns>List of output parameters.</returns>
        public static List<IGH_Param> GetAllOutputs(IGH_Component component)
        {
            return component.Params.Output;
        }

        /// <summary>
        /// Gets an input parameter by name.
        /// </summary>
        /// <param name="component">The component to search.</param>
        /// <param name="name">The parameter name to find.</param>
        /// <returns>The parameter, or null if not found.</returns>
        public static IGH_Param? GetInputByName(IGH_Component component, string name)
        {
            var paramList = GetAllInputs(component);
            foreach (var param in paramList)
            {
                if (param.Name == name)
                {
                    return param;
                }
            }

            Debug.WriteLine($"[ParameterAccess] Could not find input named '{name}' in component '{component.InstanceGuid}'");
            return null;
        }

        /// <summary>
        /// Gets an output parameter by name.
        /// </summary>
        /// <param name="component">The component to search.</param>
        /// <param name="name">The parameter name to find.</param>
        /// <returns>The parameter, or null if not found.</returns>
        public static IGH_Param? GetOutputByName(IGH_Component component, string name)
        {
            var paramList = GetAllOutputs(component);
            foreach (var param in paramList)
            {
                if (param.Name == name)
                {
                    return param;
                }
            }

            Debug.WriteLine($"[ParameterAccess] Could not find output named '{name}' in component '{component.InstanceGuid}'");
            return null;
        }

        /// <summary>
        /// Gets an input parameter by index.
        /// </summary>
        /// <param name="component">The component to search.</param>
        /// <param name="index">The parameter index.</param>
        /// <returns>The parameter, or null if index is out of range.</returns>
        public static IGH_Param? GetInputByIndex(IGH_Component component, int index)
        {
            var inputs = GetAllInputs(component);
            if (index >= 0 && index < inputs.Count)
            {
                return inputs[index];
            }

            Debug.WriteLine($"[ParameterAccess] Input index {index} out of range for component '{component.InstanceGuid}'");
            return null;
        }

        /// <summary>
        /// Gets an output parameter by index.
        /// </summary>
        /// <param name="component">The component to search.</param>
        /// <param name="index">The parameter index.</param>
        /// <returns>The parameter, or null if index is out of range.</returns>
        public static IGH_Param? GetOutputByIndex(IGH_Component component, int index)
        {
            var outputs = GetAllOutputs(component);
            if (index >= 0 && index < outputs.Count)
            {
                return outputs[index];
            }

            Debug.WriteLine($"[ParameterAccess] Output index {index} out of range for component '{component.InstanceGuid}'");
            return null;
        }

        /// <summary>
        /// Adds a source connection to a parameter.
        /// </summary>
        /// <param name="targetParam">The parameter to add a source to.</param>
        /// <param name="sourceParam">The source parameter to connect.</param>
        public static void SetSource(IGH_Param targetParam, IGH_Param sourceParam)
        {
            targetParam.AddSource(sourceParam);
        }
    }
}
