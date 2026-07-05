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

namespace GhJSON.Core.MergeOperations
{
    /// <summary>
    /// Joins multiple paginated GhJSON documents into a single document.
    /// Reuses <see cref="DocumentMerger"/> for the mechanical merge, then performs
    /// page-specific cleanup: deduplication, boundary resolution, pagination removal,
    /// and metadata recounting.
    /// </summary>
    internal static class PageJoiner
    {
        /// <summary>
        /// Joins the provided pages into a single GhJSON document.
        /// </summary>
        /// <param name="pages">The paginated documents to join.</param>
        /// <returns>A <see cref="PageJoinResult"/> containing the joined document.</returns>
        public static PageJoinResult JoinPages(IEnumerable<GhJsonDocument> pages)
        {
            if (pages == null)
            {
                throw new ArgumentNullException(nameof(pages));
            }

            var pagesList = pages.ToList();
            if (pagesList.Count == 0)
            {
                return new PageJoinResult
                {
                    Document = new GhJsonDocument(),
                    Success = true,
                };
            }

            if (pagesList.Any(p => p == null))
            {
                throw new ArgumentException("All pages must be non-null.", nameof(pages));
            }

            GhJsonDocument mergedDocument;
            var conflicts = new List<string>();

            if (pagesList.Count == 1)
            {
                mergedDocument = pagesList[0];
            }
            else
            {
                var mergeOptions = new MergeOptions
                {
                    RegenerateIds = false,
                    RegenerateInstanceGuids = false,
                    PreserveGroups = true,
                    MergeMetadata = false,
                    OffsetX = 0,
                    OffsetY = 0,
                };

                var mergeResult = DocumentMerger.Merge(pagesList[0], pagesList[1], mergeOptions);
                conflicts.AddRange(mergeResult.Conflicts);

                for (var i = 2; i < pagesList.Count; i++)
                {
                    mergeResult = DocumentMerger.Merge(mergeResult.Document, pagesList[i], mergeOptions);
                    conflicts.AddRange(mergeResult.Conflicts);
                }

                mergedDocument = mergeResult.Document;
            }

            var processResult = ProcessJoinedDocument(mergedDocument);
            processResult.Conflicts.AddRange(conflicts);

            return processResult;
        }

        private static PageJoinResult ProcessJoinedDocument(GhJsonDocument document)
        {
            var duplicateComponentsRemoved = 0;
            var seenIds = new HashSet<int>();
            var seenGuids = new HashSet<Guid>();
            var uniqueComponents = new List<GhJsonComponent>();

            foreach (var component in document.Components ?? Enumerable.Empty<GhJsonComponent>())
            {
                if (IsDuplicateComponent(component, seenIds, seenGuids))
                {
                    duplicateComponentsRemoved++;
                    continue;
                }

                TrackComponent(component, seenIds, seenGuids);
                uniqueComponents.Add(component);
            }

            var validIds = new HashSet<int>(uniqueComponents.Where(c => c.Id.HasValue).Select(c => c.Id!.Value));

            var (uniqueConnections, boundaryResolved, danglingRemoved) = ResolveConnections(
                document.Connections,
                validIds);

            var (cleanedGroups, emptyGroupsRemoved) = CleanGroups(document.Groups, validIds);

            var metadata = BuildCleanMetadata(document.Metadata, uniqueComponents, uniqueConnections, cleanedGroups);

            var joinedDocument = new GhJsonDocument(
                document.Schema,
                metadata,
                uniqueComponents,
                uniqueConnections.Count > 0 ? uniqueConnections : null,
                cleanedGroups.Count > 0 ? cleanedGroups : null);

            return new PageJoinResult
            {
                Document = joinedDocument,
                Success = true,
                DuplicateComponentsRemoved = duplicateComponentsRemoved,
                BoundaryConnectionsResolved = boundaryResolved,
                DanglingConnectionsRemoved = danglingRemoved,
                EmptyGroupsRemoved = emptyGroupsRemoved,
            };
        }

        private static bool IsDuplicateComponent(GhJsonComponent component, HashSet<int> seenIds, HashSet<Guid> seenGuids)
        {
            if (component.Id.HasValue && seenIds.Contains(component.Id.Value))
            {
                return true;
            }

            if (component.InstanceGuid.HasValue && component.InstanceGuid.Value != Guid.Empty && seenGuids.Contains(component.InstanceGuid.Value))
            {
                return true;
            }

            return false;
        }

        private static void TrackComponent(GhJsonComponent component, HashSet<int> seenIds, HashSet<Guid> seenGuids)
        {
            if (component.Id.HasValue)
            {
                seenIds.Add(component.Id.Value);
            }

            if (component.InstanceGuid.HasValue && component.InstanceGuid.Value != Guid.Empty)
            {
                seenGuids.Add(component.InstanceGuid.Value);
            }
        }

        private static (List<GhJsonConnection> Connections, int BoundaryResolved, int DanglingRemoved) ResolveConnections(
            IEnumerable<GhJsonConnection>? connections,
            HashSet<int> validIds)
        {
            var result = new List<GhJsonConnection>();
            var seen = new HashSet<(int fromId, string? fromParamName, int? fromParamIndex, int toId, string? toParamName, int? toParamIndex)>();
            var boundaryResolved = 0;
            var danglingRemoved = 0;

            foreach (var connection in connections ?? Enumerable.Empty<GhJsonConnection>())
            {
                var key = (
                    connection.From.Id,
                    connection.From.ParamName,
                    connection.From.ParamIndex,
                    connection.To.Id,
                    connection.To.ParamName,
                    connection.To.ParamIndex);

                if (!seen.Add(key))
                {
                    continue;
                }

                var fromValid = validIds.Contains(connection.From.Id);
                var toValid = validIds.Contains(connection.To.Id);

                if (fromValid && toValid)
                {
                    if (connection.Boundary == true)
                    {
                        connection.Boundary = null;
                        boundaryResolved++;
                    }

                    result.Add(connection);
                }
                else if (connection.Boundary == true)
                {
                    // Preserve boundary connections that still cross the joined document boundary.
                    result.Add(connection);
                }
                else
                {
                    danglingRemoved++;
                }
            }

            return (result, boundaryResolved, danglingRemoved);
        }

        private static (List<GhJsonGroup> Groups, int EmptyGroupsRemoved) CleanGroups(
            IEnumerable<GhJsonGroup>? groups,
            HashSet<int> validIds)
        {
            var result = new List<GhJsonGroup>();
            var idIndex = new Dictionary<int, GhJsonGroup>();
            var guidIndex = new Dictionary<Guid, GhJsonGroup>();
            var emptyGroupsRemoved = 0;

            foreach (var group in groups ?? Enumerable.Empty<GhJsonGroup>())
            {
                var existing = FindExistingGroup(group, idIndex, guidIndex);
                if (existing != null)
                {
                    MergeGroupMembers(existing, group, validIds);
                    continue;
                }

                var cleaned = CleanGroupMembers(group, validIds);
                if (cleaned.Members == null || cleaned.Members.Count == 0)
                {
                    emptyGroupsRemoved++;
                    continue;
                }

                result.Add(cleaned);
                TrackGroup(cleaned, idIndex, guidIndex);
            }

            return (result, emptyGroupsRemoved);
        }

        private static GhJsonGroup? FindExistingGroup(
            GhJsonGroup group,
            Dictionary<int, GhJsonGroup> idIndex,
            Dictionary<Guid, GhJsonGroup> guidIndex)
        {
            if (group.Id.HasValue && idIndex.TryGetValue(group.Id.Value, out var byId))
            {
                return byId;
            }

            if (group.InstanceGuid.HasValue && group.InstanceGuid.Value != Guid.Empty && guidIndex.TryGetValue(group.InstanceGuid.Value, out var byGuid))
            {
                return byGuid;
            }

            return null;
        }

        private static void MergeGroupMembers(GhJsonGroup existing, GhJsonGroup incoming, HashSet<int> validIds)
        {
            var incomingMembers = incoming.Members?.Where(m => validIds.Contains(m)) ?? Enumerable.Empty<int>();
            var mergedMembers = new HashSet<int>(existing.Members ?? Enumerable.Empty<int>());
            foreach (var member in incomingMembers)
            {
                mergedMembers.Add(member);
            }

            existing.Members = mergedMembers.ToList();
        }

        private static GhJsonGroup CleanGroupMembers(GhJsonGroup group, HashSet<int> validIds)
        {
            var clone = new GhJsonGroup
            {
                Id = group.Id,
                InstanceGuid = group.InstanceGuid,
                Name = group.Name,
                Color = group.Color,
                Members = group.Members?.Where(m => validIds.Contains(m)).ToList() ?? new List<int>(),
            };

            return clone;
        }

        private static void TrackGroup(
            GhJsonGroup group,
            Dictionary<int, GhJsonGroup> idIndex,
            Dictionary<Guid, GhJsonGroup> guidIndex)
        {
            if (group.Id.HasValue)
            {
                idIndex[group.Id.Value] = group;
            }

            if (group.InstanceGuid.HasValue && group.InstanceGuid.Value != Guid.Empty)
            {
                guidIndex[group.InstanceGuid.Value] = group;
            }
        }

        private static GhJsonMetadata BuildCleanMetadata(
            GhJsonMetadata? source,
            List<GhJsonComponent> components,
            List<GhJsonConnection> connections,
            List<GhJsonGroup> groups)
        {
            var metadata = source != null ? CloneMetadata(source) : new GhJsonMetadata();
            metadata.ComponentCount = components.Count;
            metadata.ConnectionCount = connections.Count;
            metadata.GroupCount = groups.Count;
            metadata.Pagination = null;
            return metadata;
        }

        private static GhJsonMetadata CloneMetadata(GhJsonMetadata source)
        {
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
                ComponentCount = source.ComponentCount,
                ConnectionCount = source.ConnectionCount,
                GroupCount = source.GroupCount,
                Pagination = source.Pagination,
            };
        }
    }
}
