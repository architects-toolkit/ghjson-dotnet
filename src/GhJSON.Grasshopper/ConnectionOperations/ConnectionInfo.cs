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

namespace GhJSON.Grasshopper.ConnectionOperations
{
    /// <summary>
    /// Describes a single Grasshopper wire between two document objects.
    /// </summary>
    public sealed class ConnectionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        /// <param name="sourceGuid">Instance GUID of the source component or parameter.</param>
        /// <param name="sourceParamName">NickName of the source output parameter.</param>
        /// <param name="targetGuid">Instance GUID of the target component or parameter.</param>
        /// <param name="targetParamName">NickName of the target input parameter.</param>
        public ConnectionInfo(Guid sourceGuid, string sourceParamName, Guid targetGuid, string targetParamName)
        {
            this.SourceGuid = sourceGuid;
            this.SourceParamName = sourceParamName ?? string.Empty;
            this.TargetGuid = targetGuid;
            this.TargetParamName = targetParamName ?? string.Empty;
        }

        /// <summary>
        /// Gets the instance GUID of the source component or parameter.
        /// </summary>
        public Guid SourceGuid { get; }

        /// <summary>
        /// Gets the NickName of the source output parameter.
        /// </summary>
        public string SourceParamName { get; }

        /// <summary>
        /// Gets the instance GUID of the target component or parameter.
        /// </summary>
        public Guid TargetGuid { get; }

        /// <summary>
        /// Gets the NickName of the target input parameter.
        /// </summary>
        public string TargetParamName { get; }
    }
}
