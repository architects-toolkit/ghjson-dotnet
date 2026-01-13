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

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// Options for writing GhJSON documents.
    /// </summary>
    public sealed class WriteOptions
    {
        /// <summary>
        /// Gets the default write options.
        /// </summary>
        public static WriteOptions Default { get; } = new WriteOptions();

        /// <summary>
        /// Gets or sets a value indicating whether the output should be indented.
        /// </summary>
        public bool Indented { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether null values should be included.
        /// </summary>
        public bool IncludeNullValues { get; set; } = false;
    }
}
