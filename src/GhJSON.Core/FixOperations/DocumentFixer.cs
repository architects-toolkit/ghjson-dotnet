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

namespace GhJSON.Core.FixOperations
{
	/// <summary>
	/// Fixes GhJSON documents by applying various repair operations.
	/// </summary>
	internal static class DocumentFixer
	{
		/// <summary>
		/// Fixes a GhJSON document using the specified options.
		/// </summary>
		/// <param name="document">The document to fix.</param>
		/// <param name="options">The fix options.</param>
		/// <returns>The fix result.</returns>
		public static FixResult Fix(GhJsonDocument document, FixOptions? options = null)
		{
			options ??= FixOptions.Default;
			var workingComponents = document.Components.ToList();
			var workingConnections = document.Connections?.ToList();
			var workingGroups = document.Groups?.ToList();
			var workingMetadata = document.Metadata;
			var result = new FixResult
			{
				Document = document
			};

			if (options.ReassignIds)
			{
				ReassignIds(workingComponents, workingConnections, workingGroups, result);
			}
			else if (options.AssignMissingIds)
			{
				AssignMissingIds(workingComponents, result);
			}

			if (options.RegenerateInstanceGuids)
			{
				RegenerateInstanceGuids(workingComponents, result);
			}
			else if (options.GenerateMissingInstanceGuids)
			{
				GenerateMissingInstanceGuids(workingComponents, result);
			}

			if (options.RemoveInvalidConnections)
			{
				RemoveInvalidConnections(workingComponents, ref workingConnections, result);
			}

			if (options.RemoveInvalidGroupMembers)
			{
				RemoveInvalidGroupMembers(workingComponents, workingGroups, result);
			}

			if (options.FixMetadata)
			{
				FixMetadata(ref workingMetadata, workingComponents, workingConnections, workingGroups, result);
			}

			if (options.NormalizeEnumCasing)
			{
				NormalizeDataMapping(workingComponents, result);
			}

			result.Document = new GhJsonDocument(
				schema: document.Schema,
				metadata: workingMetadata,
				components: workingComponents,
				connections: workingConnections?.Count > 0 ? workingConnections : null,
				groups: workingGroups?.Count > 0 ? workingGroups : null);

			return result;
		}

		/// <summary>
		/// Assigns missing IDs to components that don't have them.
		/// </summary>
		public static FixResult AssignMissingIds(GhJsonDocument document)
		{
			return Fix(document, new FixOptions
			{
				AssignMissingIds = true,
				ReassignIds = false,
				GenerateMissingInstanceGuids = false,
				RegenerateInstanceGuids = false,
				RemoveInvalidConnections = false,
				RemoveInvalidGroupMembers = false,
				FixMetadata = false,
				NormalizeEnumCasing = false,
			});
		}

		/// <summary>
		/// Reassigns all component IDs sequentially starting from 1.
		/// </summary>
		public static FixResult ReassignIds(GhJsonDocument document)
		{
			return Fix(document, new FixOptions
			{
				AssignMissingIds = false,
				ReassignIds = true,
				GenerateMissingInstanceGuids = false,
				RegenerateInstanceGuids = false,
				RemoveInvalidConnections = false,
				RemoveInvalidGroupMembers = false,
				FixMetadata = false,
				NormalizeEnumCasing = false,
			});
		}

		/// <summary>
		/// Generates missing instance GUIDs for components.
		/// </summary>
		public static FixResult GenerateMissingInstanceGuids(GhJsonDocument document)
		{
			return Fix(document, new FixOptions
			{
				AssignMissingIds = false,
				ReassignIds = false,
				GenerateMissingInstanceGuids = true,
				RegenerateInstanceGuids = false,
				RemoveInvalidConnections = false,
				RemoveInvalidGroupMembers = false,
				FixMetadata = false,
				NormalizeEnumCasing = false,
			});
		}

		/// <summary>
		/// Regenerates all instance GUIDs.
		/// </summary>
		public static FixResult RegenerateInstanceGuids(GhJsonDocument document)
		{
			return Fix(document, new FixOptions
			{
				AssignMissingIds = false,
				ReassignIds = false,
				GenerateMissingInstanceGuids = false,
				RegenerateInstanceGuids = true,
				RemoveInvalidConnections = false,
				RemoveInvalidGroupMembers = false,
				FixMetadata = false,
				NormalizeEnumCasing = false,
			});
		}

		/// <summary>
		/// Fixes document metadata (counts, timestamps).
		/// </summary>
		public static FixResult FixMetadata(GhJsonDocument document)
		{
			return Fix(document, new FixOptions
			{
				AssignMissingIds = false,
				ReassignIds = false,
				GenerateMissingInstanceGuids = false,
				RegenerateInstanceGuids = false,
				RemoveInvalidConnections = false,
				RemoveInvalidGroupMembers = false,
				FixMetadata = true,
				NormalizeEnumCasing = false,
			});
		}

		/// <summary>
		/// Normalizes enum casing (e.g. <c>"Flatten"</c> → <c>"flatten"</c>) in parameter settings and extensions.
		/// </summary>
		public static FixResult NormalizeEnumCasing(GhJsonDocument document)
		{
			return Fix(document, new FixOptions
			{
				AssignMissingIds = false,
				ReassignIds = false,
				GenerateMissingInstanceGuids = false,
				RegenerateInstanceGuids = false,
				RemoveInvalidConnections = false,
				RemoveInvalidGroupMembers = false,
				FixMetadata = false,
				NormalizeEnumCasing = true,
			});
		}

		private static void AssignMissingIds(List<GhJsonComponent> components, FixResult result)
		{
			var existingIds = new HashSet<int>(
				components
					.Where(c => c.Id.HasValue)
					.Select(c => c.Id!.Value));

			var nextId = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

			foreach (var component in components.Where(c => !c.Id.HasValue))
			{
				component.Id = nextId++;
				result.AppliedActions.Add($"Assigned ID {component.Id} to component '{component.Name}'");
				result.WasModified = true;
			}
		}

		private static void ReassignIds(
			List<GhJsonComponent> components,
			List<GhJsonConnection>? connections,
			List<GhJsonGroup>? groups,
			FixResult result)
		{
			var oldToNewMapping = new Dictionary<int, int>();
			var newId = 1;

			foreach (var component in components)
			{
				if (component.Id.HasValue)
				{
					oldToNewMapping[component.Id.Value] = newId;
				}

				component.Id = newId++;
				result.WasModified = true;
			}

			// Update connection references
			if (connections != null)
			{
				foreach (var connection in connections)
				{
					if (oldToNewMapping.TryGetValue(connection.From.Id, out var newFromId))
					{
						connection.From.Id = newFromId;
					}

					if (oldToNewMapping.TryGetValue(connection.To.Id, out var newToId))
					{
						connection.To.Id = newToId;
					}
				}
			}

			// Update group member references
			if (groups != null)
			{
				foreach (var group in groups)
				{
					for (var i = 0; i < group.Members.Count; i++)
					{
						if (oldToNewMapping.TryGetValue(group.Members[i], out var newMemberId))
						{
							group.Members[i] = newMemberId;
						}
					}
				}
			}

			result.AppliedActions.Add($"Reassigned IDs for {components.Count} components");
		}

		private static void GenerateMissingInstanceGuids(List<GhJsonComponent> components, FixResult result)
		{
			foreach (var component in components.Where(c => !c.InstanceGuid.HasValue))
			{
				component.InstanceGuid = Guid.NewGuid();
				result.AppliedActions.Add($"Generated instance GUID for component '{component.Name}'");
				result.WasModified = true;
			}
		}

		private static void RegenerateInstanceGuids(List<GhJsonComponent> components, FixResult result)
		{
			foreach (var component in components)
			{
				component.InstanceGuid = Guid.NewGuid();
				result.WasModified = true;
			}

			result.AppliedActions.Add($"Regenerated instance GUIDs for {components.Count} components");
		}

		private static void RemoveInvalidConnections(
			List<GhJsonComponent> components,
			ref List<GhJsonConnection>? connections,
			FixResult result)
		{
			if (connections == null || connections.Count == 0)
			{
				return;
			}

			var validIds = new HashSet<int>(
				components
					.Where(c => c.Id.HasValue)
					.Select(c => c.Id!.Value));

			var invalidConnections = connections
				.Where(c => !validIds.Contains(c.From.Id) || !validIds.Contains(c.To.Id))
				.ToList();

			foreach (var conn in invalidConnections)
			{
				connections.Remove(conn);
				result.AppliedActions.Add($"Removed invalid connection from {conn.From.Id} to {conn.To.Id}");
				result.WasModified = true;
			}
		}

		private static void RemoveInvalidGroupMembers(
			List<GhJsonComponent> components,
			List<GhJsonGroup>? groups,
			FixResult result)
		{
			if (groups == null || groups.Count == 0)
			{
				return;
			}

			var validIds = new HashSet<int>(
				components
					.Where(c => c.Id.HasValue)
					.Select(c => c.Id!.Value));

			foreach (var group in groups)
			{
				var invalidMembers = group.Members.Where(m => !validIds.Contains(m)).ToList();
				foreach (var member in invalidMembers)
				{
					group.Members.Remove(member);
					result.AppliedActions.Add($"Removed invalid member {member} from group '{group.Name}'");
					result.WasModified = true;
				}
			}
		}

		private static void FixMetadata(
			ref GhJsonMetadata? metadata,
			List<GhJsonComponent> components,
			List<GhJsonConnection>? connections,
			List<GhJsonGroup>? groups,
			FixResult result)
		{
			metadata ??= new GhJsonMetadata();

			var expectedComponentCount = components.Count;
			var expectedConnectionCount = connections?.Count ?? 0;
			var expectedGroupCount = groups?.Count ?? 0;

			if (metadata.ComponentCount != expectedComponentCount)
			{
				metadata.ComponentCount = expectedComponentCount;
				result.AppliedActions.Add($"Updated component count to {expectedComponentCount}");
				result.WasModified = true;
			}

			if (metadata.ConnectionCount != expectedConnectionCount)
			{
				metadata.ConnectionCount = expectedConnectionCount;
				result.AppliedActions.Add($"Updated connection count to {expectedConnectionCount}");
				result.WasModified = true;
			}

			if (metadata.GroupCount != expectedGroupCount)
			{
				metadata.GroupCount = expectedGroupCount;
				result.AppliedActions.Add($"Updated group count to {expectedGroupCount}");
				result.WasModified = true;
			}

			var now = DateTime.UtcNow;
			if (!metadata.Modified.HasValue || metadata.Modified.Value != now)
			{
				metadata.Modified = now;
				result.AppliedActions.Add("Updated modified timestamp");
				result.WasModified = true;
			}
		}

		/// <summary>
		/// Normalizes <c>dataMapping</c> values to lowercase in parameter settings
		/// and extension dictionaries so they conform to the schema enum.
		/// </summary>
		private static void NormalizeDataMapping(List<GhJsonComponent> components, FixResult result)
		{
			var changedCount = 0;

			foreach (var component in components)
			{
				// Normalize input/output parameter settings
				if (component.InputSettings != null)
				{
					foreach (var setting in component.InputSettings)
					{
						if (NormalizeDataMappingValue(setting))
						{
							changedCount++;
						}
					}
				}

				if (component.OutputSettings != null)
				{
					foreach (var setting in component.OutputSettings)
					{
						if (NormalizeDataMappingValue(setting))
						{
							changedCount++;
						}
					}
				}

				// Normalize extension dictionaries recursively
				if (component.ComponentState?.Extensions != null)
				{
					foreach (var kvp in component.ComponentState.Extensions)
					{
						if (kvp.Value is Dictionary<string, object?> dict)
						{
							if (NormalizeExtensionsDictionary(dict))
							{
								changedCount++;
								result.WasModified = true;
							}
						}
					}
				}
			}

			if (changedCount > 0)
			{
				result.AppliedActions.Add($"Normalized dataMapping casing for {changedCount} parameter(s)/extension(s)");
				result.WasModified = true;
			}
		}

		/// <summary>
		/// Normalizes the <see cref="GhJsonParameterSettings.DataMapping"/> value to lowercase.
		/// </summary>
		/// <param name="setting">The parameter settings to modify.</param>
		/// <returns><c>true</c> if the value was changed.</returns>
		private static bool NormalizeDataMappingValue(GhJsonParameterSettings? setting)
		{
			if (setting is null || string.IsNullOrEmpty(setting.DataMapping))
			{
				return false;
			}

			var lower = setting.DataMapping.ToLowerInvariant();
			if (lower != setting.DataMapping)
			{
				setting.DataMapping = lower;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Recursively normalizes <c>dataMapping</c> values inside extension dictionaries.
		/// </summary>
		/// <param name="dictionary">The dictionary to scan.</param>
		/// <returns><c>true</c> if any value was changed.</returns>
		private static bool NormalizeExtensionsDictionary(Dictionary<string, object?> dictionary)
		{
			var changed = false;

			foreach (var key in dictionary.Keys.ToList())
			{
				var value = dictionary[key];

				if (string.Equals(key, "dataMapping", StringComparison.OrdinalIgnoreCase) &&
					value is string s &&
					s != s.ToLowerInvariant())
				{
					dictionary[key] = s.ToLowerInvariant();
					changed = true;
				}
				else if (value is Dictionary<string, object?> childDict)
				{
					if (NormalizeExtensionsDictionary(childDict))
					{
						changed = true;
					}
				}
				else if (value is List<object?> list)
				{
					foreach (var item in list)
					{
						if (item is Dictionary<string, object?> itemDict)
						{
							if (NormalizeExtensionsDictionary(itemDict))
							{
								changed = true;
							}
						}
					}
				}
			}

			return changed;
		}
	}
}
