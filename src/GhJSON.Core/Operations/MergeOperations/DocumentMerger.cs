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
using System.Linq;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;

namespace GhJSON.Core.Operations.MergeOperations
{
    /// <summary>
    /// Merges two GhJSON documents into one.
    /// </summary>
    public class DocumentMerger
    {
        private readonly MergeOptions _options;
        private readonly ConflictResolver _conflictResolver;
        private readonly PositionAdjuster _positionAdjuster;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentMerger"/> class.
        /// </summary>
        /// <param name="options">Merge options.</param>
        public DocumentMerger(MergeOptions? options = null)
        {
            _options = options ?? MergeOptions.Default;
            _conflictResolver = new ConflictResolver(_options.ConflictResolution);
            _positionAdjuster = new PositionAdjuster(_options.PositionOffset);
        }

        /// <summary>
        /// Merges source document into target document.
        /// Target document is modified in place.
        /// </summary>
        /// <param name="target">The target document (modified in place).</param>
        /// <param name="source">The source document to merge from.</param>
        /// <returns>Result of the merge operation.</returns>
        public MergeResult Merge(GhJsonDocument target, GhJsonDocument source)
        {
            var result = new MergeResult(target);

            if (source == null)
                return result;

            // Initialize collections if needed
            target.Components ??= new List<ComponentProperties>();
            target.Connections ??= new List<ConnectionPairing>();

            // Build existing GUID set for conflict detection
            var existingGuids = new HashSet<Guid>(
                target.Components
                    .Where(c => c.InstanceGuid.HasValue)
                    .Select(c => c.InstanceGuid!.Value));

            // Calculate ID offset if needed
            int maxId = target.Components.Any() ? target.Components.Max(c => c.Id) : 0;
            var idMapping = new Dictionary<int, int>(); // Source ID -> New ID

            // Calculate position offset if needed
            float maxX = 0;
            if (_options.AdjustPositions && target.Components.Any())
            {
                maxX = target.Components
                    .Where(c => c.Pivot != null)
                    .Select(c => c.Pivot!.X)
                    .DefaultIfEmpty(0)
                    .Max();
            }

            // Process source components
            if (source.Components != null)
            {
                foreach (var sourceComp in source.Components)
                {
                    var newComp = CloneComponent(sourceComp);
                    var oldId = newComp.Id;

                    // Handle ID conflicts
                    if (_options.ReassignIds)
                    {
                        newComp.Id = ++maxId;
                        idMapping[oldId] = newComp.Id;
                    }

                    // Handle GUID conflicts
                    if (newComp.InstanceGuid.HasValue && existingGuids.Contains(newComp.InstanceGuid.Value))
                    {
                        var resolution = _conflictResolver.Resolve(newComp, target.Components);
                        switch (resolution)
                        {
                            case ConflictAction.Skip:
                                result.Skipped++;
                                continue;
                            case ConflictAction.Replace:
                                var existing = target.Components.FirstOrDefault(c => c.InstanceGuid == newComp.InstanceGuid);
                                if (existing != null)
                                {
                                    target.Components.Remove(existing);
                                    result.Replaced++;
                                }
                                break;
                            case ConflictAction.KeepBoth:
                                newComp.InstanceGuid = Guid.NewGuid();
                                break;
                            case ConflictAction.Fail:
                                throw new InvalidOperationException($"GUID conflict for component {newComp.Name} ({newComp.InstanceGuid})");
                        }
                    }

                    // Adjust position
                    if (_options.AdjustPositions && newComp.Pivot != null)
                    {
                        _positionAdjuster.AdjustPosition(newComp, maxX);
                    }

                    target.Components.Add(newComp);
                    if (newComp.InstanceGuid.HasValue)
                    {
                        existingGuids.Add(newComp.InstanceGuid.Value);
                    }
                    result.ComponentsAdded++;
                }
            }

            // Process source connections (remap IDs)
            if (source.Connections != null)
            {
                foreach (var sourceConn in source.Connections)
                {
                    var newConn = CloneConnection(sourceConn);

                    // Remap IDs if reassigned
                    if (_options.ReassignIds)
                    {
                        if (idMapping.TryGetValue(newConn.From.Id, out var newFromId))
                            newConn.From.Id = newFromId;
                        if (idMapping.TryGetValue(newConn.To.Id, out var newToId))
                            newConn.To.Id = newToId;
                    }

                    target.Connections.Add(newConn);
                    result.ConnectionsAdded++;
                }
            }

            // Process source groups (remap IDs)
            if (_options.MergeGroups && source.Groups != null)
            {
                target.Groups ??= new List<GroupInfo>();

                foreach (var sourceGroup in source.Groups)
                {
                    var newGroup = CloneGroup(sourceGroup);

                    // Remap member IDs
                    if (_options.ReassignIds && newGroup.Members != null)
                    {
                        newGroup.Members = newGroup.Members
                            .Select(id => idMapping.TryGetValue(id, out var newId) ? newId : id)
                            .ToList();
                    }

                    target.Groups.Add(newGroup);
                    result.GroupsAdded++;
                }
            }

            return result;
        }

        private static ComponentProperties CloneComponent(ComponentProperties source)
        {
            return new ComponentProperties
            {
                Id = source.Id,
                Name = source.Name,
                NickName = source.NickName,
                ComponentGuid = source.ComponentGuid,
                InstanceGuid = source.InstanceGuid,
                Pivot = source.Pivot,
                ComponentState = source.ComponentState,
                InputSettings = source.InputSettings,
                OutputSettings = source.OutputSettings,
                Properties = source.Properties,
                Warnings = source.Warnings,
                Errors = source.Errors
            };
        }

        private static ConnectionPairing CloneConnection(ConnectionPairing source)
        {
            return new ConnectionPairing
            {
                From = new Connection
                {
                    Id = source.From.Id,
                    ParamName = source.From.ParamName,
                    ParamIndex = source.From.ParamIndex
                },
                To = new Connection
                {
                    Id = source.To.Id,
                    ParamName = source.To.ParamName,
                    ParamIndex = source.To.ParamIndex
                }
            };
        }

        private static GroupInfo CloneGroup(GroupInfo source)
        {
            return new GroupInfo
            {
                InstanceGuid = source.InstanceGuid,
                Name = source.Name,
                Color = source.Color,
                Members = source.Members?.ToList() ?? new List<int>()
            };
        }
    }

    /// <summary>
    /// Result of a merge operation.
    /// </summary>
    public class MergeResult
    {
        /// <summary>
        /// Gets the merged document.
        /// </summary>
        public GhJsonDocument Document { get; }

        /// <summary>
        /// Gets the number of components added from source.
        /// </summary>
        public int ComponentsAdded { get; set; }

        /// <summary>
        /// Gets the number of components skipped due to conflicts.
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// Gets the number of components replaced due to conflicts.
        /// </summary>
        public int Replaced { get; set; }

        /// <summary>
        /// Gets the number of connections added.
        /// </summary>
        public int ConnectionsAdded { get; set; }

        /// <summary>
        /// Gets the number of groups added.
        /// </summary>
        public int GroupsAdded { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeResult"/> class.
        /// </summary>
        /// <param name="document">The merged document.</param>
        public MergeResult(GhJsonDocument document)
        {
            Document = document;
        }
    }
}
