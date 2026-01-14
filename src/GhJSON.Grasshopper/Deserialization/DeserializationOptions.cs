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

namespace GhJSON.Grasshopper.Deserialization
{
    /// <summary>
    /// Options for deserializing GhJSON to Grasshopper objects.
    /// </summary>
    public sealed class DeserializationOptions
    {
        /// <summary>
        /// Gets the default deserialization options.
        /// </summary>
        public static DeserializationOptions Default { get; } = new DeserializationOptions();

        /// <summary>
        /// Gets or sets a value indicating whether to apply internalized data.
        /// </summary>
        public bool ApplyInternalizedData { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to apply component state.
        /// </summary>
        public bool ApplyComponentState { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to regenerate instance GUIDs.
        /// </summary>
        public bool RegenerateInstanceGuids { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip invalid components.
        /// </summary>
        public bool SkipInvalidComponents { get; set; } = true;
    }
}
