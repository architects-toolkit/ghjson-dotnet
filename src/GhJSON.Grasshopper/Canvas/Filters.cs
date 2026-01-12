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
using System.Collections.Generic;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Strongly-typed attribute filter for canvas object retrieval.
    /// </summary>
    public class AttributeFilter
    {
        /// <summary>
        /// Gets or sets the set of attribute flags to include (OR logic).
        /// </summary>
        public GetAttributes Include { get; set; }

        /// <summary>
        /// Gets or sets the set of attribute flags to exclude.
        /// </summary>
        public GetAttributes Exclude { get; set; }

        /// <summary>
        /// Creates an empty attribute filter.
        /// </summary>
        public AttributeFilter()
        {
        }

        /// <summary>
        /// Creates an attribute filter with include and exclude flags.
        /// </summary>
        public AttributeFilter(GetAttributes include, GetAttributes exclude = 0)
        {
            Include = include;
            Exclude = exclude;
        }
    }

    /// <summary>
    /// Strongly-typed type filter for canvas object retrieval.
    /// </summary>
    public class TypeFilter
    {
        /// <summary>
        /// Gets or sets the set of object kind flags to include (OR logic).
        /// </summary>
        public GetObjectKinds Include { get; set; }

        /// <summary>
        /// Gets or sets the set of object kind flags to exclude.
        /// </summary>
        public GetObjectKinds Exclude { get; set; }

        /// <summary>
        /// Gets or sets the set of node role flags to include (OR logic).
        /// </summary>
        public GetNodeRoles IncludeRoles { get; set; }

        /// <summary>
        /// Gets or sets the set of node role flags to exclude.
        /// </summary>
        public GetNodeRoles ExcludeRoles { get; set; }

        /// <summary>
        /// Creates an empty type filter.
        /// </summary>
        public TypeFilter()
        {
        }

        /// <summary>
        /// Creates a type filter with object kind flags.
        /// </summary>
        public TypeFilter(GetObjectKinds include, GetObjectKinds exclude = 0)
        {
            Include = include;
            Exclude = exclude;
        }

        /// <summary>
        /// Creates a type filter with node role flags.
        /// </summary>
        public static TypeFilter FromRoles(GetNodeRoles includeRoles, GetNodeRoles excludeRoles = 0)
        {
            return new TypeFilter
            {
                IncludeRoles = includeRoles,
                ExcludeRoles = excludeRoles,
            };
        }
    }

    /// <summary>
    /// Strongly-typed category filter for canvas object retrieval.
    /// </summary>
    public class CategoryFilter
    {
        /// <summary>
        /// Gets or sets the set of category/subcategory names to include (case-insensitive, OR logic).
        /// </summary>
        public HashSet<string> Include { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the set of category/subcategory names to exclude (case-insensitive).
        /// </summary>
        public HashSet<string> Exclude { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates an empty category filter.
        /// </summary>
        public CategoryFilter()
        {
        }

        /// <summary>
        /// Creates a category filter with include and exclude sets.
        /// </summary>
        public CategoryFilter(IEnumerable<string>? include, IEnumerable<string>? exclude = null)
        {
            if (include != null)
            {
                foreach (var item in include)
                {
                    Include.Add(item);
                }
            }

            if (exclude != null)
            {
                foreach (var item in exclude)
                {
                    Exclude.Add(item);
                }
            }
        }
    }
}
