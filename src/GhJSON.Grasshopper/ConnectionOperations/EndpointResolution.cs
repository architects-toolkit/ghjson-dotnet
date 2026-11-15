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

using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.ConnectionOperations
{
    /// <summary>
    /// Terminal status of a strict endpoint resolution.
    /// </summary>
    public enum EndpointResolutionStatus
    {
        /// <summary>The endpoint resolved to exactly one parameter.</summary>
        Resolved,

        /// <summary>The endpoint resolved to nothing usable; see the error.</summary>
        Unresolved,
    }

    /// <summary>
    /// Typed result of resolving a connection endpoint to a concrete parameter.
    /// Strict callers reject plans whose endpoints are ambiguous or unresolvable
    /// instead of silently skipping wires.
    /// </summary>
    public sealed class EndpointResolution
    {
        /// <summary>Whether the endpoint resolved to a parameter.</summary>
        public EndpointResolutionStatus Status { get; private set; }

        /// <summary>The resolved parameter when <see cref="Status"/> is <see cref="EndpointResolutionStatus.Resolved"/>.</summary>
        public IGH_Param? Param { get; private set; }

        /// <summary>Human-readable reason when resolution failed.</summary>
        public string? Error { get; private set; }

        /// <summary>Convenience: true when resolved.</summary>
        public bool Resolved => this.Status == EndpointResolutionStatus.Resolved;

        internal static EndpointResolution OfResolved(IGH_Param param)
        {
            return new EndpointResolution { Status = EndpointResolutionStatus.Resolved, Param = param };
        }

        internal static EndpointResolution OfUnresolved(string error)
        {
            return new EndpointResolution { Status = EndpointResolutionStatus.Unresolved, Error = error };
        }
    }
}
