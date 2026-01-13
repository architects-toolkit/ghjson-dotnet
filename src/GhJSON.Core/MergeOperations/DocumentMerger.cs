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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.MergeOperations
{
    /// <summary>
    /// Merges two GhJSON documents into one.
    /// </summary>
    internal static class DocumentMerger
    {
        /// <summary>
        /// Merges the incoming document into the base document.
        /// </summary>
        /// <param name="baseDoc">The base document to merge into.</param>
        /// <param name="incomingDoc">The incoming document to merge from.</param>
        /// <param name="options">The merge options.</param>
        /// <returns>The merge result.</returns>
        public static MergeResult Merge(GhJsonDocument baseDoc, GhJsonDocument incomingDoc, MergeOptions? options = null)
        {
            options ??= MergeOptions.Default;

            var result = new MergeResult
            {
                Document = baseDoc,
                Success = true
            };

            // Calculate the next available ID
            var maxId = baseDoc.Components
                .Where(c => c.Id.HasValue)
                .Select(c => c.Id!.Value)
                .DefaultIfEmpty(0)
                .Max();

            var nextId = maxId + 1;

            // Build ID mapping for incoming components
            foreach (var component in incomingDoc.Components)
            {
                if (component.Id.HasValue)
                {
                    var oldId = component.Id.Value;
                    var newId = options.RegenerateIds ? nextId++ : component.Id.Value;
                    result.IdMapping[oldId] = newId;
                }
            }

            // Add incoming components
            foreach (var component in incomingDoc.Components)
            {
                var newComponent = CloneComponent(component, result.IdMapping, options);
                baseDoc.Components.Add(newComponent);
                result.ComponentsAdded++;
            }

            // Add incoming connections with updated IDs
            if (incomingDoc.Connections != null && incomingDoc.Connections.Count > 0)
            {
                baseDoc.Connections ??= new List<GhJsonConnection>();

                foreach (var connection in incomingDoc.Connections)
                {
                    var newConnection = new GhJsonConnection
                    {
                        From = new GhJsonConnectionEndpoint
                        {
                            Id = result.IdMapping.TryGetValue(connection.From.Id, out var newFromId) ? newFromId : connection.From.Id,
                            ParamName = connection.From.ParamName,
                            ParamIndex = connection.From.ParamIndex
                        },
                        To = new GhJsonConnectionEndpoint
                        {
                            Id = result.IdMapping.TryGetValue(connection.To.Id, out var newToId) ? newToId : connection.To.Id,
                            ParamName = connection.To.ParamName,
                            ParamIndex = connection.To.ParamIndex
                        }
                    };

                    baseDoc.Connections.Add(newConnection);
                    result.ConnectionsAdded++;
                }
            }

            // Add incoming groups with updated member IDs
            if (options.PreserveGroups && incomingDoc.Groups != null && incomingDoc.Groups.Count > 0)
            {
                baseDoc.Groups ??= new List<GhJsonGroup>();

                foreach (var group in incomingDoc.Groups)
                {
                    var newGroup = new GhJsonGroup
                    {
                        Id = options.RegenerateIds ? nextId++ : group.Id,
                        InstanceGuid = options.RegenerateInstanceGuids ? Guid.NewGuid() : group.InstanceGuid,
                        Name = group.Name,
                        Color = group.Color,
                        Members = group.Members
                            .Select(m => result.IdMapping.TryGetValue(m, out var newMemberId) ? newMemberId : m)
                            .ToList()
                    };

                    baseDoc.Groups.Add(newGroup);
                    result.GroupsAdded++;
                }
            }

            return result;
        }

        private static GhJsonComponent CloneComponent(GhJsonComponent original, Dictionary<int, int> idMapping, MergeOptions options)
        {
            var clone = new GhJsonComponent
            {
                Name = original.Name,
                Library = original.Library,
                NickName = original.NickName,
                ComponentGuid = original.ComponentGuid,
                InstanceGuid = options.RegenerateInstanceGuids ? Guid.NewGuid() : original.InstanceGuid,
                Id = original.Id.HasValue && idMapping.TryGetValue(original.Id.Value, out var newId) ? newId : original.Id,
                Errors = original.Errors?.ToList(),
                Warnings = original.Warnings?.ToList(),
                Remarks = original.Remarks?.ToList()
            };

            // Apply position offset
            if (original.Pivot != null)
            {
                clone.Pivot = new GhJsonPivot(
                    original.Pivot.X + options.OffsetX,
                    original.Pivot.Y + options.OffsetY);
            }

            // Clone input/output settings
            if (original.InputSettings != null)
            {
                clone.InputSettings = original.InputSettings.Select(CloneParameterSettings).ToList();
            }

            if (original.OutputSettings != null)
            {
                clone.OutputSettings = original.OutputSettings.Select(CloneParameterSettings).ToList();
            }

            // Clone component state
            if (original.ComponentState != null)
            {
                clone.ComponentState = new GhJsonComponentState
                {
                    Selected = original.ComponentState.Selected,
                    Locked = original.ComponentState.Locked,
                    Hidden = original.ComponentState.Hidden
                };

                // Copy extension data
                if (original.ComponentState.Extensions != null)
                {
                    clone.ComponentState.Extensions = new Dictionary<string, object>(original.ComponentState.Extensions);
                }

                if (original.ComponentState.AdditionalProperties != null)
                {
                    clone.ComponentState.AdditionalProperties = new Dictionary<string, object>(original.ComponentState.AdditionalProperties);
                }
            }

            return clone;
        }

        private static GhJsonParameterSettings CloneParameterSettings(GhJsonParameterSettings original)
        {
            return new GhJsonParameterSettings
            {
                ParameterName = original.ParameterName,
                NickName = original.NickName,
                Description = original.Description,
                DataMapping = original.DataMapping,
                Expression = original.Expression,
                Access = original.Access,
                TypeHint = original.TypeHint,
                IsPrincipal = original.IsPrincipal,
                IsRequired = original.IsRequired,
                IsReparameterized = original.IsReparameterized,
                IsReversed = original.IsReversed,
                IsSimplified = original.IsSimplified,
                IsInverted = original.IsInverted,
                IsUnitized = original.IsUnitized,
                InternalizedData = original.InternalizedData != null
                    ? CloneInternalizedData(original.InternalizedData)
                    : null
            };
        }

        private static Dictionary<string, Dictionary<string, string>> CloneInternalizedData(
            IDictionary<string, Dictionary<string, string>> source)
        {
            var clone = new Dictionary<string, Dictionary<string, string>>(source.Count);

            foreach (var kvp in source)
            {
                clone[kvp.Key] = new Dictionary<string, string>(kvp.Value);
            }

            return clone;
        }
    }
}
