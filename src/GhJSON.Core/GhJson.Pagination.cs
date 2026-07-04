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
using System.Collections.Generic;
using System.Linq;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core
{
    public static partial class GhJson
    {
        /// <summary>
        /// Returns a single page of a GhJSON document.
        /// Keeps the components in the requested page, plus any connection that touches the page.
        /// Connections that cross the page boundary are marked with <c>boundary: true</c>.
        /// Groups are only kept when all of their members are on the page.
        /// </summary>
        /// <param name="document">The full document to segment.</param>
        /// <param name="page">Zero-based page index.</param>
        /// <param name="pageSize">Number of components per page. Must be greater than zero.</param>
        /// <returns>A new <see cref="GhJsonDocument"/> containing only the requested page.</returns>
        public static GhJsonDocument SegmentDocument(GhJsonDocument document, int page, int pageSize)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (page < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to zero.");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than zero.");
            }

            var allComponents = document.Components?.ToList() ?? new List<GhJsonComponent>();
            var skip = page * pageSize;
            var totalComponents = allComponents.Count;
            var totalPages = (int)Math.Ceiling((double)totalComponents / pageSize);
            var oneBasedPage = page + 1;

            if (skip >= allComponents.Count)
            {
                return new GhJsonDocument(
                    document.Schema,
                    CopyMetadata(document.Metadata, totalComponents, 0, 0, oneBasedPage, pageSize, totalPages),
                    Array.Empty<GhJsonComponent>(),
                    null,
                    null);
            }

            var pageComponents = allComponents.Skip(skip).Take(pageSize).ToList();
            var pageIds = new HashSet<int>(pageComponents.Where(c => c.Id.HasValue).Select(c => c.Id!.Value));

            List<GhJsonConnection>? pageConnections = null;
            if (document.Connections != null)
            {
                pageConnections = document.Connections
                    .Where(c => pageIds.Contains(c.From.Id) || pageIds.Contains(c.To.Id))
                    .Select(c =>
                    {
                        var fromOnPage = pageIds.Contains(c.From.Id);
                        var toOnPage = pageIds.Contains(c.To.Id);
                        return new GhJsonConnection
                        {
                            From = new GhJsonConnectionEndpoint
                            {
                                Id = c.From.Id,
                                ParamName = c.From.ParamName,
                                ParamIndex = c.From.ParamIndex,
                            },
                            To = new GhJsonConnectionEndpoint
                            {
                                Id = c.To.Id,
                                ParamName = c.To.ParamName,
                                ParamIndex = c.To.ParamIndex,
                            },
                            Boundary = fromOnPage && toOnPage ? null : (bool?)true,
                        };
                    })
                    .ToList();
            }

            List<GhJsonGroup>? pageGroups = null;
            if (document.Groups != null)
            {
                pageGroups = document.Groups
                    .Where(g => g.Members?.Count > 0 && g.Members.All(id => pageIds.Contains(id)))
                    .ToList();
            }

            var metadata = CopyMetadata(
                document.Metadata,
                totalComponents,
                pageConnections?.Count ?? 0,
                pageGroups?.Count ?? 0,
                oneBasedPage,
                pageSize,
                totalPages);

            return new GhJsonDocument(
                document.Schema,
                metadata,
                pageComponents,
                pageConnections,
                pageGroups);
        }

        /// <summary>
        /// Creates a shallow copy of metadata with updated counts and optional pagination info.
        /// Pagination is omitted when the document fits in a single page.
        /// </summary>
        private static GhJsonMetadata? CopyMetadata(
            GhJsonMetadata? source,
            int componentCount,
            int connectionCount,
            int groupCount,
            int page,
            int pageSize,
            int totalPages)
        {
            GhJsonPagination? pagination = null;
            if (totalPages > 1)
            {
                pagination = new GhJsonPagination
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                };
            }

            if (source == null)
            {
                return new GhJsonMetadata
                {
                    ComponentCount = componentCount,
                    ConnectionCount = connectionCount,
                    GroupCount = groupCount,
                    Pagination = pagination,
                };
            }

            return new GhJsonMetadata
            {
                Title = source.Title,
                Description = source.Description,
                Version = source.Version,
                Author = source.Author,
                Tags = source.Tags?.ToList(),
                Dependencies = source.Dependencies?.ToList(),
                GeneratorName = source.GeneratorName,
                GeneratorVersion = source.GeneratorVersion,
                RhinoVersion = source.RhinoVersion,
                GrasshopperVersion = source.GrasshopperVersion,
                Created = source.Created,
                Modified = source.Modified,
                ComponentCount = componentCount,
                ConnectionCount = connectionCount,
                GroupCount = groupCount,
                Pagination = pagination,
            };
        }
    }
}
