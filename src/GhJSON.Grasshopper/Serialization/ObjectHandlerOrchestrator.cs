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
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Orchestrates the serialization and deserialization of Grasshopper objects
    /// using registered object handlers in priority order.
    /// </summary>
    internal static class ObjectHandlerOrchestrator
    {
        /// <summary>
        /// Serializes a Grasshopper document object to a GhJSON component.
        /// Applies all compatible handlers in priority order.
        /// Properties set by higher-priority handlers are protected from being overwritten.
        /// </summary>
        /// <param name="obj">The document object to serialize.</param>
        /// <returns>The serialized component.</returns>
        public static GhJsonComponent Serialize(IGH_DocumentObject obj)
        {
            var component = new GhJsonComponent();

            foreach (var handler in ObjectHandlerRegistry.GetAll())
            {
                if (handler.CanHandle(obj))
                {
                    var patch = new GhJsonComponent();
                    handler.Serialize(obj, patch);
                    MergeFirstWins(component, patch);
                }
            }

            return component;
        }

        /// <summary>
        /// Serializes multiple Grasshopper document objects.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <returns>List of serialized components.</returns>
        public static List<GhJsonComponent> Serialize(IEnumerable<IGH_DocumentObject> objects)
        {
            var components = new List<GhJsonComponent>();

            foreach (var obj in objects)
            {
                components.Add(Serialize(obj));
            }

            return components;
        }

        /// <summary>
        /// Deserializes a GhJSON component to configure a Grasshopper document object.
        /// Applies all compatible handlers in priority order.
        /// Note: Deserialization reads from component, so no override protection needed.
        /// </summary>
        /// <param name="component">The component to deserialize.</param>
        /// <param name="obj">The document object to configure.</param>
        public static void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            foreach (var handler in ObjectHandlerRegistry.GetAll())
            {
                if (handler.CanHandle(component))
                {
                    handler.Deserialize(component, obj);
                }
            }
        }

        private static void MergeFirstWins(GhJsonComponent target, GhJsonComponent patch)
        {
            // Property-level first-wins merge.
            // - Simple properties: first non-null wins; conflicting non-null values throw.
            // - Nested objects: deep-merge with first-wins at sub-property level.
            // - Lists with identity: merge items by identity (e.g., ParameterName) + deep merge per item.

            MergeComponentStateFirstWins(target, patch);
            MergeParameterSettingsListsFirstWins(target, patch);

            foreach (PropertyInfo property in typeof(GhJsonComponent)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => !IsDiagnosticsProperty(p))
                .Where(p => p.Name != nameof(GhJsonComponent.ComponentState))
                .Where(p => p.Name != nameof(GhJsonComponent.InputSettings))
                .Where(p => p.Name != nameof(GhJsonComponent.OutputSettings)))
            {
                MergeScalarPropertyFirstWins(target, patch, property);
            }

            MergeDiagnosticsFirstWins(target, patch);
        }

        private static void MergeScalarPropertyFirstWins(object target, object patch, PropertyInfo property)
        {
            object? targetValue = property.GetValue(target);
            object? patchValue = property.GetValue(patch);

            if (patchValue == null)
            {
                return;
            }

            if (targetValue == null)
            {
                property.SetValue(target, patchValue);
                return;
            }

            if (!AreValuesEqual(targetValue, patchValue))
            {
                throw new InvalidOperationException(
                    $"Handler override detected for property '{property.Name}'. " +
                    $"Existing value '{targetValue}' cannot be overridden by '{patchValue}'.");
            }
        }

        private static void MergeComponentStateFirstWins(GhJsonComponent target, GhJsonComponent patch)
        {
            if (patch.ComponentState == null)
            {
                return;
            }

            if (target.ComponentState == null)
            {
                target.ComponentState = patch.ComponentState;
                return;
            }

            MergeObjectFirstWins(target.ComponentState, patch.ComponentState);
        }

        private static void MergeParameterSettingsListsFirstWins(GhJsonComponent target, GhJsonComponent patch)
        {
            target.InputSettings = MergeParameterSettingsListFirstWins(target.InputSettings, patch.InputSettings);
            target.OutputSettings = MergeParameterSettingsListFirstWins(target.OutputSettings, patch.OutputSettings);
        }

        private static List<GhJsonParameterSettings>? MergeParameterSettingsListFirstWins(
            List<GhJsonParameterSettings>? targetList,
            List<GhJsonParameterSettings>? patchList)
        {
            if (patchList == null || patchList.Count == 0)
            {
                return targetList;
            }

            if (targetList == null || targetList.Count == 0)
            {
                return patchList;
            }

            var index = targetList
                .Where(s => !string.IsNullOrWhiteSpace(s.ParameterName))
                .ToDictionary(s => s.ParameterName, StringComparer.Ordinal);

            foreach (GhJsonParameterSettings patchItem in patchList)
            {
                if (string.IsNullOrWhiteSpace(patchItem.ParameterName))
                {
                    // No stable identity. First-wins at item level: only add if no existing item reference equal.
                    if (!targetList.Contains(patchItem))
                    {
                        targetList.Add(patchItem);
                    }

                    continue;
                }

                if (!index.TryGetValue(patchItem.ParameterName, out GhJsonParameterSettings? targetItem))
                {
                    targetList.Add(patchItem);
                    index[patchItem.ParameterName] = patchItem;
                    continue;
                }

                MergeObjectFirstWins(targetItem, patchItem, excludePropertyNames: new[] { nameof(GhJsonParameterSettings.ParameterName) });
            }

            return targetList;
        }

        private static void MergeObjectFirstWins(object target, object patch, IEnumerable<string>? excludePropertyNames = null)
        {
            var exclude = excludePropertyNames != null
                ? new HashSet<string>(excludePropertyNames, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            foreach (PropertyInfo property in target.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => !exclude.Contains(p.Name)))
            {
                object? patchValue = property.GetValue(patch);
                if (patchValue == null)
                {
                    continue;
                }

                object? targetValue = property.GetValue(target);

                if (targetValue == null)
                {
                    property.SetValue(target, patchValue);
                    continue;
                }

                // Special-case merges.
                if (targetValue is Dictionary<string, object> tObjDict && patchValue is Dictionary<string, object> pObjDict)
                {
                    MergeDictionaryFirstWins(tObjDict, pObjDict, property.Name);
                    continue;
                }

                if (targetValue is Dictionary<string, Dictionary<string, string>> tTree && patchValue is Dictionary<string, Dictionary<string, string>> pTree)
                {
                    MergeInternalizedDataFirstWins(tTree, pTree, property.Name);
                    continue;
                }

                if (IsMergeableComplexObject(targetValue) && IsMergeableComplexObject(patchValue))
                {
                    MergeObjectFirstWins(targetValue, patchValue);
                    continue;
                }

                if (!AreValuesEqual(targetValue, patchValue))
                {
                    throw new InvalidOperationException(
                        $"Handler override detected for property '{target.GetType().Name}.{property.Name}'. " +
                        $"Existing value '{targetValue}' cannot be overridden by '{patchValue}'.");
                }
            }
        }

        private static void MergeDictionaryFirstWins(Dictionary<string, object> target, Dictionary<string, object> patch, string propertyName)
        {
            foreach (var kvp in patch)
            {
                if (target.TryGetValue(kvp.Key, out object? existing))
                {
                    if (!AreValuesEqual(existing, kvp.Value))
                    {
                        throw new InvalidOperationException(
                            $"Handler override detected for dictionary '{propertyName}' key '{kvp.Key}'.");
                    }

                    continue;
                }

                target[kvp.Key] = kvp.Value;
            }
        }

        private static void MergeInternalizedDataFirstWins(
            Dictionary<string, Dictionary<string, string>> target,
            Dictionary<string, Dictionary<string, string>> patch,
            string propertyName)
        {
            foreach (var kvp in patch)
            {
                if (!target.TryGetValue(kvp.Key, out Dictionary<string, string>? existingBranch))
                {
                    target[kvp.Key] = kvp.Value;
                    continue;
                }

                foreach (var leaf in kvp.Value)
                {
                    if (existingBranch.TryGetValue(leaf.Key, out string? existingValue))
                    {
                        if (!string.Equals(existingValue, leaf.Value, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"Handler override detected for '{propertyName}' at '{kvp.Key}/{leaf.Key}'.");
                        }

                        continue;
                    }

                    existingBranch[leaf.Key] = leaf.Value;
                }
            }
        }

        private static bool IsMergeableComplexObject(object value)
        {
            Type t = value.GetType();
            return t.IsClass
                && t != typeof(string)
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(t);
        }

        private static bool AreValuesEqual(object a, object b)
        {
            if (a is string sa && b is string sb)
            {
                return string.Equals(sa, sb, StringComparison.Ordinal);
            }

            return Equals(a, b);
        }

        private static bool IsDiagnosticsProperty(PropertyInfo property)
        {
            return property.Name == nameof(GhJsonComponent.Errors)
                || property.Name == nameof(GhJsonComponent.Warnings)
                || property.Name == nameof(GhJsonComponent.Remarks);
        }

        private static void MergeDiagnosticsFirstWins(GhJsonComponent target, GhJsonComponent patch)
        {
            // Diagnostics are safe to append without violating "do not override" semantics.
            // This allows later handlers to contribute additional issues without replacing earlier ones.

            if (patch.Errors?.Count > 0)
            {
                target.Errors ??= new List<string>();
                target.Errors.AddRange(patch.Errors.Where(e => !target.Errors.Contains(e)));
            }

            if (patch.Warnings?.Count > 0)
            {
                target.Warnings ??= new List<string>();
                target.Warnings.AddRange(patch.Warnings.Where(w => !target.Warnings.Contains(w)));
            }

            if (patch.Remarks?.Count > 0)
            {
                target.Remarks ??= new List<string>();
                target.Remarks.AddRange(patch.Remarks.Where(r => !target.Remarks.Contains(r)));
            }
        }
    }
}
