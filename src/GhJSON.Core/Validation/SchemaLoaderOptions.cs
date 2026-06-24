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

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Specifies how the <see cref="SchemaLoader"/> should resolve schemas.
    /// </summary>
    public sealed class SchemaLoaderOptions
    {
        /// <summary>
        /// The schema version to load (e.g. "1.0"). Defaults to the current version.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// When <c>true</c>, attempts to download the schema from the official online
        /// repository first. If the network is unreachable or the download fails,
        /// falls back to the embedded snapshot. Defaults to <c>false</c>.
        /// </summary>
        public bool PreferOnline { get; set; } = false;

        /// <summary>
        /// Base URL for online schema resolution. Defaults to the official
        /// ghjson-spec GitHub Pages endpoint.
        /// </summary>
        public string BaseUrl { get; set; } = "https://architects-toolkit.github.io/ghjson-spec/schema/";

        /// <summary>
        /// Timeout for online schema requests. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan OnlineTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}
