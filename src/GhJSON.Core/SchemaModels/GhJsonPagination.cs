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

using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Pagination information for a GhJSON document that contains only a subset of components.
    /// </summary>
    public sealed class GhJsonPagination
    {
        /// <summary>
        /// Gets or sets the one-based page index.
        /// </summary>
        [JsonProperty("page", NullValueHandling = NullValueHandling.Ignore)]
        public int? Page { get; set; }

        /// <summary>
        /// Gets or sets the number of components per page.
        /// </summary>
        [JsonProperty("pageSize", NullValueHandling = NullValueHandling.Ignore)]
        public int? PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages available.
        /// </summary>
        [JsonProperty("totalPages", NullValueHandling = NullValueHandling.Ignore)]
        public int? TotalPages { get; set; }
    }
}
