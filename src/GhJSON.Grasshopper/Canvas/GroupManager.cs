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
using System.Diagnostics;
using System.Drawing;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization.DataTypes;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Handles recreation of Grasshopper groups from GhJSON definitions.
    /// Manages group creation, member assignment, and styling.
    /// </summary>
    public static class GroupManager
    {
        /// <summary>
        /// Creates all groups from a GhJSON document.
        /// </summary>
        /// <param name="ghDocument">GhJSON document with group definitions</param>
        /// <param name="guidMapping">Dictionary mapping GUIDs to component instances</param>
        /// <returns>Number of groups created</returns>
        public static int CreateGroups(
            GrasshopperDocument? ghDocument,
            Dictionary<Guid, IGH_DocumentObject>? guidMapping)
        {
            if (ghDocument?.Groups == null || ghDocument.Groups.Count == 0)
            {
                return 0;
            }

            var document = Instances.ActiveCanvas?.Document;
            if (document == null)
            {
                Debug.WriteLine("[GroupManager] No active Grasshopper document");
                return 0;
            }

            var groupsCreated = 0;
            var idToComponent = CanvasUtilities.BuildIdMapping(ghDocument, guidMapping);

            foreach (var groupInfo in ghDocument.Groups)
            {
                try
                {
                    if (CreateGroup(groupInfo, idToComponent, document))
                    {
                        groupsCreated++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GroupManager] Error creating group '{groupInfo.Name}': {ex.Message}");
                }
            }

            Debug.WriteLine($"[GroupManager] Created {groupsCreated} groups");
            return groupsCreated;
        }

        /// <summary>
        /// Creates a single group with members.
        /// </summary>
        private static bool CreateGroup(
            GroupInfo groupInfo,
            Dictionary<int, IGH_DocumentObject> idToComponent,
            GH_Document document)
        {
            // Get member components
            var members = new List<IGH_DocumentObject>();
            if (groupInfo.Members != null)
            {
                foreach (var memberId in groupInfo.Members)
                {
                    if (idToComponent.TryGetValue(memberId, out var component))
                    {
                        members.Add(component);
                    }
                }
            }

            if (members.Count == 0)
            {
                Debug.WriteLine($"[GroupManager] No valid members for group '{groupInfo.Name}'");
                return false;
            }

            // Create group
            var group = new GH_Group();
            group.NickName = groupInfo.Name ?? "Group";

            // Set color if provided
            if (!string.IsNullOrEmpty(groupInfo.Color))
            {
                try
                {
                    if (DataTypeSerializer.TryDeserializeFromPrefix(groupInfo.Color, out var colorObj) && colorObj is Color color)
                    {
                        group.Colour = color;
                        Debug.WriteLine($"[GroupManager] Set group color to {color}");
                    }
                    else
                    {
                        Debug.WriteLine($"[GroupManager] Failed to parse color '{groupInfo.Color}' - using default");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GroupManager] Error parsing color '{groupInfo.Color}': {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"[GroupManager] No color specified - using default group color");
            }

            // Add members to group
            foreach (var member in members)
            {
                group.AddObject(member.InstanceGuid);
            }

            // Add group to document
            document.AddObject(group, false);

            Debug.WriteLine($"[GroupManager] Created group '{group.NickName}' with {members.Count} members");
            return true;
        }
    }
}
