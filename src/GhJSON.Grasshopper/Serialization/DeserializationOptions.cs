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
    /// Options for controlling GhJSON deserialization behavior.
    /// </summary>
    public class DeserializationOptions
    {
        /// <summary>
        /// Gets the standard deserialization options with all features enabled.
        /// </summary>
        public static DeserializationOptions Standard => new DeserializationOptions
        {
            CreateConnections = true,
            ApplyComponentState = true,
            PreserveInstanceGuids = false
        };

        /// <summary>
        /// Gets or sets a value indicating whether to create connections between components.
        /// </summary>
        public bool CreateConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to apply component state (values, settings).
        /// </summary>
        public bool ApplyComponentState { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to preserve original instance GUIDs.
        /// </summary>
        public bool PreserveInstanceGuids { get; set; } = false;
    }
}
