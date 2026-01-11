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

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Options for controlling GhJSON serialization behavior.
    /// </summary>
    public class SerializationOptions
    {
        /// <summary>
        /// Gets the standard serialization options with all features enabled.
        /// </summary>
        public static SerializationOptions Standard => new SerializationOptions
        {
            IncludeConnections = true,
            IncludeMetadata = true,
            IncludeComponentState = true,
            IncludeParameterSettings = true,
            IncludeGroups = true
        };

        /// <summary>
        /// Gets lite serialization options with minimal output.
        /// </summary>
        public static SerializationOptions Lite => new SerializationOptions
        {
            IncludeConnections = true,
            IncludeMetadata = false,
            IncludeComponentState = false,
            IncludeParameterSettings = false,
            IncludeGroups = false
        };

        /// <summary>
        /// Gets or sets a value indicating whether to include connections in the output.
        /// </summary>
        public bool IncludeConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include document metadata.
        /// </summary>
        public bool IncludeMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include component state (values, settings).
        /// </summary>
        public bool IncludeComponentState { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include parameter settings.
        /// </summary>
        public bool IncludeParameterSettings { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include groups.
        /// </summary>
        public bool IncludeGroups { get; set; } = true;
    }
}
