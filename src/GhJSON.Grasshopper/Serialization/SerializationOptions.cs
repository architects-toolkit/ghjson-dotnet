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
            IncludeGroups = true,
            IncludePersistentData = true
        };

        /// <summary>
        /// Gets lite serialization options with minimal output.
        /// Excludes metadata, component state, parameter settings, groups, and persistent data.
        /// </summary>
        public static SerializationOptions Lite => new SerializationOptions
        {
            IncludeConnections = true,
            IncludeMetadata = false,
            IncludeComponentState = false,
            IncludeParameterSettings = false,
            IncludeGroups = false,
            IncludePersistentData = false
        };

        /// <summary>
        /// Gets optimized serialization options for AI use.
        /// Similar to <see cref="Standard"/>, but excludes bulky PersistentData fields
        /// to reduce token usage while maintaining structural schema.
        /// </summary>
        public static SerializationOptions Optimized => new SerializationOptions
        {
            IncludeConnections = true,
            IncludeMetadata = true,
            IncludeComponentState = true,
            IncludeParameterSettings = true,
            IncludeGroups = true,
            IncludePersistentData = false
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

        /// <summary>
        /// Gets or sets a value indicating whether to include persistent data values.
        /// When false, PersistentData is excluded to reduce output size (useful for AI context).
        /// Default is true for Standard, false for Optimized and Lite.
        /// </summary>
        public bool IncludePersistentData { get; set; } = true;
    }
}
