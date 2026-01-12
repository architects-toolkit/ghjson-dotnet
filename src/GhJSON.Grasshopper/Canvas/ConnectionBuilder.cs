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
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Utilities for connecting Grasshopper components.
    /// </summary>
    internal static class ConnectionBuilder
    {
        /// <summary>
        /// Connects two components by creating a wire between an output and input parameter.
        /// </summary>
        /// <param name="sourceGuid">GUID of the source component (output side).</param>
        /// <param name="targetGuid">GUID of the target component (input side).</param>
        /// <param name="sourceParamName">Name or nickname of the output parameter. If null, uses first output.</param>
        /// <param name="targetParamName">Name or nickname of the input parameter. If null, uses first input.</param>
        /// <param name="redraw">True to redraw canvas and trigger solution recalculation.</param>
        /// <returns>True if connection was successful, false otherwise.</returns>
        public static bool ConnectComponents(
            Guid sourceGuid,
            Guid targetGuid,
            string sourceParamName = null,
            string targetParamName = null,
            bool redraw = true)
        {
            var canvas = Instances.ActiveCanvas;
            if (canvas?.Document == null)
            {
                return false;
            }

            var doc = canvas.Document;

            // Find source and target objects
            var sourceObj = doc.Objects.FirstOrDefault(o => o.InstanceGuid == sourceGuid);
            var targetObj = doc.Objects.FirstOrDefault(o => o.InstanceGuid == targetGuid);

            if (sourceObj == null || targetObj == null)
            {
                return false;
            }

            // Get parameters
            var sourceParam = GetOutputParameter(sourceObj, sourceParamName);
            var targetParam = GetInputParameter(targetObj, targetParamName);

            if (sourceParam == null || targetParam == null)
            {
                return false;
            }

            // Check if connection already exists
            if (targetParam.Sources.Contains(sourceParam))
            {
                return true; // Already connected, consider it success
            }

            // Create the connection
            try
            {
                targetParam.AddSource(sourceParam);

                if (redraw)
                {
                    doc.NewSolution(false);
                    Instances.RedrawCanvas();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an output parameter from a document object.
        /// </summary>
        private static IGH_Param GetOutputParameter(IGH_DocumentObject obj, string paramName)
        {
            if (obj is IGH_Component comp)
            {
                if (string.IsNullOrEmpty(paramName))
                {
                    return comp.Params.Output.FirstOrDefault();
                }

                return comp.Params.Output.FirstOrDefault(p =>
                    p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase) ||
                    p.NickName.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            }
            else if (obj is IGH_Param param)
            {
                return param;
            }

            return null;
        }

        /// <summary>
        /// Gets an input parameter from a document object.
        /// </summary>
        private static IGH_Param GetInputParameter(IGH_DocumentObject obj, string paramName)
        {
            if (obj is IGH_Component comp)
            {
                if (string.IsNullOrEmpty(paramName))
                {
                    return comp.Params.Input.FirstOrDefault();
                }

                return comp.Params.Input.FirstOrDefault(p =>
                    p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase) ||
                    p.NickName.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            }
            else if (obj is IGH_Param param)
            {
                return param;
            }

            return null;
        }
    }
}
