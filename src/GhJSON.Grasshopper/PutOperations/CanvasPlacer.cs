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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using GhJSON.Core.NameResolution;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.ConnectionOperations;
using GhJSON.Grasshopper.Deserialization;
using GhJSON.Grasshopper.GetOperations;
using GhJSON.Grasshopper.LayoutRefinements;
using GhJSON.Grasshopper.Serialization;
using GhJSON.Grasshopper.Serialization.ObjectHandlers;
using GhJSON.Grasshopper.Shared;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Undo;

namespace GhJSON.Grasshopper.PutOperations
{
    /// <summary>
    /// Places GhJSON objects on the Grasshopper canvas.
    /// </summary>
    internal static class CanvasPlacer
    {
        /// <summary>
        /// Places a GhJSON document on the canvas.
        /// </summary>
        /// <param name="document">The document to place.</param>
        /// <param name="options">Put options.</param>
        /// <returns>The put result.</returns>
        public static PutResult Put(GhJsonDocument document, PutOptions? options = null)
        {
            options ??= PutOptions.Default;
            var result = new PutResult { Success = true };

            var ghDoc = CanvasReader.GetActiveDocument();
            if (ghDoc == null)
            {
                result.Success = false;
                result.ErrorMessage = "No active Grasshopper document";
                return result;
            }

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.Put] Starting placement of {document.Components?.Count ?? 0} components");
#endif

            var deserializationOptions = new DeserializationOptions
            {
                RegenerateInstanceGuids = options.RegenerateInstanceGuids,
                SkipInvalidComponents = options.SkipInvalidComponents
            };

            // Calculate effective offset
            var effectiveOffset = options.Offset;
            var hasPivots = document.Components.Any(c => c.Pivot != null);

            if (hasPivots && options.AutoOffset && options.Offset.X == 0 && options.Offset.Y == 0)
            {
                var autoOffset = CalculateAutoOffset(document.Components, ghDoc, options.AutoOffsetSpacing);
                if (autoOffset != PointF.Empty)
                {
                    effectiveOffset = autoOffset;
#if DEBUG
                    Debug.WriteLine($"[CanvasPlacer.Put] Using auto-calculated offset: ({effectiveOffset.X:F2}, {effectiveOffset.Y:F2})");
#endif
                }
            }
#if DEBUG
            else if (options.Offset.X != 0 || options.Offset.Y != 0)
            {
                Debug.WriteLine($"[CanvasPlacer.Put] Using explicit offset: ({effectiveOffset.X:F2}, {effectiveOffset.Y:F2})");
            }
#endif

            // Place components
            var idToObject = new Dictionary<int, IGH_DocumentObject>();
            var addedObjects = new List<IGH_DocumentObject>();
            GH_UndoRecord? updateUndoRecord = null;
            DeserializationOptions? preserveGuidOptions = null;

            // Phase 1: instantiate every component up front. Created objects carry real
            // attribute bounds before they are added to the canvas, which lets the layout
            // pass below measure actual component sizes instead of default estimates.
            // Components whose instanceGuid already exists on the canvas skip
            // instantiation entirely when UpdateExistingByInstanceGuid is enabled:
            // same-type matches are updated in place, different-type matches produce a
            // replacement object that keeps the existing instance GUID.
            var placed = new List<(GhJsonComponent Component, IGH_DocumentObject Obj, IGH_DocumentObject? Replaces)>(document.Components.Count);
            for (int i = 0; i < document.Components.Count; i++)
            {
                var component = document.Components[i];
                var existing = FindUpdateTarget(component, ghDoc, options);

                if (existing != null && IsSameComponentType(component, existing))
                {
                    // Same-type match: apply the incoming state onto the live object so
                    // its identity, wiring, and position survive the update.
                    if (updateUndoRecord == null)
                    {
                        updateUndoRecord = ghDoc.UndoUtil.CreateGenericObjectEvent(
                            "[GhJSON] Update object(s)", existing);
                    }
                    else
                    {
                        existing.RecordUndoEvent(updateUndoRecord);
                    }

                    ObjectHandlerOrchestrator.Deserialize(component, existing);
                    ObjectHandlerOrchestrator.PostPlacement(component, existing);

                    result.UpdatedObjects.Add(existing);
                    result.ComponentsUpdated++;

                    if (component.Id.HasValue)
                    {
                        idToObject[component.Id.Value] = existing;
                        result.IdToGuidMapping[component.Id.Value] = existing.InstanceGuid;
                    }

                    if (options.SelectPlacedObjects && existing.Attributes != null)
                    {
                        existing.Attributes.Selected = true;
                    }

                    continue;
                }

                var obj = ComponentInstantiator.Create(
                    component,
                    existing != null
                        ? preserveGuidOptions ??= new DeserializationOptions
                        {
                            ApplyInternalizedData = deserializationOptions.ApplyInternalizedData,
                            ApplyComponentState = deserializationOptions.ApplyComponentState,
                            RegenerateInstanceGuids = false,
                            SkipInvalidComponents = options.SkipInvalidComponents,
                        }
                        : deserializationOptions);

                if (obj == null)
                {
                    if (!options.SkipInvalidComponents)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to create component: {component.Name ?? component.ComponentGuid?.ToString()}";
                        return result;
                    }

                    result.FailedComponents.Add(component.Name ?? component.ComponentGuid?.ToString() ?? "unknown");
                    continue;
                }

                placed.Add((component, obj, existing));
            }

            // Replace pass: superseded objects are only removed after their
            // replacements instantiated successfully, so a failed component never
            // destroys the original. External wires are captured up front so they can
            // be restored onto the replacements (which keep the same instance GUIDs).
            var replacedObjects = placed
                .Where(entry => entry.Replaces != null)
                .Select(entry => entry.Replaces!)
                .ToList();
            var replacedPivots = replacedObjects.ToDictionary(
                obj => obj.InstanceGuid,
                obj => obj.Attributes?.Pivot);
            var replacedConnections = new List<ConnectionInfo>();
            if (replacedObjects.Count > 0)
            {
                ghDoc.UndoUtil.RecordRemoveObjectEvent(
                    $"[GhJSON] Replace {replacedObjects.Count} object(s)", replacedObjects);
                replacedConnections.AddRange(CanvasConnector.CaptureExternalConnections(
                    replacedObjects.Select(obj => obj.InstanceGuid)));

                foreach (var replaced in replacedObjects)
                {
                    replaced.IsolateObject();
                    ghDoc.RemoveObject(replaced, false);
                }
            }

            // Phase 2: calculate positions for components without pivots using dependency
            // graph layout, measured against the freshly instantiated objects' bounds.
            var layoutPositions = hasPivots
                ? new Dictionary<Guid, PointF>()
                : CalculateLayoutPositions(document, ghDoc, options.AutoOffsetSpacing, placed);

            // Phase 3: assign pivots and add the objects to the document.
            foreach (var (component, obj, replaces) in placed)
            {
                // Apply position: either from pivot + offset, or calculated layout position
                if (obj.Attributes != null)
                {
                    if (replaces != null)
                    {
                        // Replacements keep the document pivot verbatim (already applied
                        // by PivotHandler during deserialization); when the document
                        // omits a pivot the previous position is preserved.
                        if (component.Pivot == null &&
                            replacedPivots.TryGetValue(replaces.InstanceGuid, out var previousPivot) &&
                            previousPivot.HasValue)
                        {
                            obj.Attributes.Pivot = previousPivot.Value;
                        }
                    }
                    else if (component.Pivot != null)
                    {
                        // Use pivot with offset
                        obj.Attributes.Pivot = new PointF(
                            (float)(component.Pivot.X + effectiveOffset.X),
                            (float)(component.Pivot.Y + effectiveOffset.Y));
                    }
                    else if (Core.GhJson.GetLayoutKey(component) is var layoutKey &&
                             layoutKey != Guid.Empty &&
                             layoutPositions.TryGetValue(layoutKey, out var calculatedPosition))
                    {
                        // Use dependency graph calculated position for components without pivots.
                        // Use the same stable key as the layout engine so id-only components
                        // (no InstanceGuid) also receive their calculated position.
                        // Layout positions are bounds centers; convert to this object's
                        // pivot semantics (components pivot at center, sliders/panels at
                        // their top-left corner).
                        obj.Attributes.Pivot = PivotSemantics.CenterToPivot(obj, calculatedPosition);
#if DEBUG
                        Debug.WriteLine($"[CanvasPlacer.Put] Applied layout position for '{component.Name}': ({calculatedPosition.X:F2}, {calculatedPosition.Y:F2})");
#endif
                    }
                }

                // Add to document (triggers AddedToDocument which may reset some properties)
                ghDoc.AddObject(obj, false);

                // Apply post-placement fixups for properties that get reset by AddedToDocument
                ObjectHandlerOrchestrator.PostPlacement(component, obj);

                result.PlacedObjects.Add(obj);
                addedObjects.Add(obj);
                result.ComponentsPlaced++;

                if (component.Id.HasValue)
                {
                    idToObject[component.Id.Value] = obj;
                    result.IdToGuidMapping[component.Id.Value] = obj.InstanceGuid;
                }

                // Select if requested
                if (options.SelectPlacedObjects && obj.Attributes != null)
                {
                    obj.Attributes.Selected = true;
                }
            }

            // Restore external wires captured before replaced objects were removed.
            // Replacements keep the original instance GUIDs, so surviving endpoints
            // resolve; failures are surfaced as warnings rather than dropping silently.
            foreach (var connection in replacedConnections)
            {
                if (!CanvasConnector.Connect(
                        connection.SourceGuid,
                        connection.TargetGuid,
                        connection.SourceParamName,
                        connection.TargetParamName))
                {
                    result.Warnings.Add(
                        $"External connection from {connection.SourceGuid}.{connection.SourceParamName} " +
                        $"to {connection.TargetGuid}.{connection.TargetParamName} could not be restored.");
                }
            }

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.Put] Placed {result.ComponentsPlaced} components, {result.FailedComponents.Count} failed");
#endif

            // Late post-placement: resolve SmartHopper selectedObjects IDs to objects
            foreach (var component in document.Components)
            {
                if (component.Id.HasValue && idToObject.TryGetValue(component.Id.Value, out var placedObj))
                {
                    SmartHopperStateHandler.ApplySelectedObjects(component, placedObj, idToObject);
                }
            }

            // Create connections
            if (options.CreateConnections && document.Connections != null)
            {
                foreach (var connection in document.Connections)
                {
                    if (CreateConnection(connection, idToObject, document, result))
                    {
                        result.ConnectionsCreated++;
                    }
                }

#if DEBUG
                Debug.WriteLine($"[CanvasPlacer.Put] Created {result.ConnectionsCreated} connections, {result.Warnings.Count} warnings");
#endif
            }

            // Connected panels render streamed data rows (path + value) instead of user
            // text; reserve a data row now that sources exist so one-line panels don't
            // clip the first entry.
            foreach (var obj in addedObjects)
            {
                if (obj is GH_Panel panel)
                {
                    PanelHandler.ReserveDataRows(panel);
                }
            }

            // Create groups
            if (options.CreateGroups && document.Groups != null)
            {
                foreach (var group in document.Groups)
                {
                    var placedGroup = CreateGroup(group, idToObject, ghDoc);
                    if (placedGroup != null)
                    {
                        addedObjects.Add(placedGroup);
                        result.GroupsCreated++;
                    }
                    else
                    {
                        result.Warnings.Add($"Failed to create group: {group.Name}");
                    }
                }
            }

            if (addedObjects.Count > 0)
            {
                ghDoc.UndoUtil.RecordAddObjectEvent($"[GhJSON] Place {addedObjects.Count} object(s)", addedObjects);
            }

            // Expire solution
            ghDoc.NewSolution(true);

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.Put] Placement complete: {result.ComponentsPlaced} components, {result.ConnectionsCreated} connections, {result.GroupsCreated} groups");
#endif

            return result;
        }

        private static bool CreateConnection(GhJsonConnection connection, Dictionary<int, IGH_DocumentObject> idToObject, GhJsonDocument document, PutResult result)
        {
            if (!idToObject.TryGetValue(connection.From.Id, out var fromObj) ||
                !idToObject.TryGetValue(connection.To.Id, out var toObj))
            {
                var missingFrom = document.Components.FirstOrDefault(c => c.Id == connection.From.Id);
                var missingTo = document.Components.FirstOrDefault(c => c.Id == connection.To.Id);
                var fromName = missingFrom?.Name ?? $"id:{connection.From.Id}";
                var toName = missingTo?.Name ?? $"id:{connection.To.Id}";
                result.Warnings.Add($"Connection from '{fromName}' to '{toName}' lost: component not installed.");
                return false;
            }

            var sourceParam = GetParameter(fromObj, connection.From, isInput: false);
            var targetParam = GetParameter(toObj, connection.To, isInput: true);

            if (sourceParam == null || targetParam == null)
            {
                result.Warnings.Add($"Connection from '{fromObj.Name}' ({connection.From.ParamName}) to '{toObj.Name}' ({connection.To.ParamName}) lost: parameter not found.");
                return false;
            }

            if (targetParam.Sources.Contains(sourceParam))
            {
                // Already wired: happens when an in-place update kept the existing
                // wire while the document still lists the connection.
                return true;
            }

            targetParam.AddSource(sourceParam);
            return true;
        }

        private static IGH_Param? GetParameter(IGH_DocumentObject obj, GhJsonConnectionEndpoint endpoint, bool isInput)
        {
            IList<IGH_Param>? parameters = null;

            if (obj is IGH_Component comp)
            {
                parameters = isInput ? comp.Params.Input : comp.Params.Output;
            }
            else if (obj is IGH_Param param)
            {
                // For floating parameters, return the param itself
                return param;
            }

            if (parameters == null || parameters.Count == 0)
            {
                return null;
            }

            // Try index first, then name/nickname via shared helper
            var matchedParam = ConnectionHelper.FindParamByIndexNameOrNickName(parameters, endpoint.ParamName, endpoint.ParamIndex);
            if (matchedParam != null)
            {
                return matchedParam;
            }

            // Fuzzy name resolution fallback
            if (!string.IsNullOrEmpty(endpoint.ParamName))
            {
                var knownNames = parameters.Select(p => p.Name);
                var resolvedName = ParameterNameResolver.Resolve(endpoint.ParamName, knownNames);
                if (resolvedName != null)
                {
#if DEBUG
                    Debug.WriteLine($"[CanvasPlacer.GetParameter] Fuzzy resolved param '{endpoint.ParamName}' \u2192 '{resolvedName}'");
#endif
                    return parameters.FirstOrDefault(p =>
                        p.Name.Equals(resolvedName, StringComparison.OrdinalIgnoreCase));
                }
            }

            return null;
        }

        private static GH_Group? CreateGroup(GhJsonGroup groupDef, Dictionary<int, IGH_DocumentObject> idToObject, GH_Document ghDoc)
        {
            var members = new List<Guid>();

            foreach (var memberId in groupDef.Members)
            {
                if (idToObject.TryGetValue(memberId, out var obj))
                {
                    members.Add(obj.InstanceGuid);
                }
            }

            if (members.Count == 0)
            {
                return null;
            }

            var group = new GH_Group();
            group.CreateAttributes();
            group.NickName = groupDef.Name ?? string.Empty;

            // Parse color
            if (!string.IsNullOrEmpty(groupDef.Color))
            {
                var color = ParseArgbColor(groupDef.Color);
                if (color.HasValue)
                {
                    group.Colour = color.Value;
                }
            }

            // Add members
            foreach (var memberGuid in members)
            {
                group.AddObject(memberGuid);
            }

            ghDoc.AddObject(group, false);
            return group;
        }

        private static Color? ParseArgbColor(string colorString)
        {
            if (!colorString.StartsWith("argb:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var parts = colorString.Substring(5).Split(',');
            if (parts.Length != 4)
            {
                return null;
            }

            if (int.TryParse(parts[0], out var a) &&
                int.TryParse(parts[1], out var r) &&
                int.TryParse(parts[2], out var g) &&
                int.TryParse(parts[3], out var b))
            {
                return Color.FromArgb(a, r, g, b);
            }

            return null;
        }

        /// <summary>
        /// Calculates layout positions for components without pivots using dependency graph analysis.
        /// </summary>
        /// <param name="document">The GhJSON document.</param>
        /// <param name="ghDoc">The Grasshopper document.</param>
        /// <param name="spacing">Vertical spacing to add below existing content.</param>
        /// <param name="placed">Freshly instantiated (not yet added) objects, measured for real bounds.</param>
        /// <returns>Dictionary mapping instance GUID to calculated position.</returns>
        private static Dictionary<Guid, PointF> CalculateLayoutPositions(
            GhJsonDocument document,
            GH_Document ghDoc,
            float spacing,
            IReadOnlyList<(GhJsonComponent Component, IGH_DocumentObject Obj, IGH_DocumentObject? Replaces)> placed)
        {
            // Real bounds come from the freshly instantiated objects first; anything not
            // measurable there falls back to a live canvas lookup (e.g. objects sharing
            // instance GUIDs with the incoming document).
            var measuredByKey = new Dictionary<Guid, IGH_DocumentObject>(placed.Count);
            foreach (var (component, obj, _) in placed)
            {
                var key = Core.GhJson.GetLayoutKey(component);
                if (key != Guid.Empty)
                {
                    measuredByKey[key] = obj;
                }
            }

            var canvasProvider = CanvasNodeSizeProvider.Create(ghDoc);
            Func<Guid, SizeF?> sizeProvider = guid =>
            {
                if (measuredByKey.TryGetValue(guid, out var obj) &&
                    CanvasNodeSizeProvider.Measure(obj) is SizeF measured)
                {
                    return measured;
                }

                return canvasProvider(guid);
            };

            // Fresh objects resolve through the caller-owned map so refinements can read
            // their port geometry before they reach the canvas.
            Func<Guid, IGH_DocumentObject?> objectProvider = guid =>
                measuredByKey.TryGetValue(guid, out var obj) ? obj : ghDoc.FindObject(guid, false);

            // Shared layout pipeline: core dependency-graph pass + Grasshopper-aware
            // refinements (bounds-aware spacing, port alignment, wire corridors,
            // collision avoidance). The same measured-bounds and object providers feed
            // every stage so freshly instantiated objects — not yet on the canvas —
            // still contribute their real geometry.
            var refinedPositions = GhJsonGrasshopper.ComputeLayout(document, new CanvasLayoutOptions
            {
                NodeSizeProvider = sizeProvider,
                ObjectProvider = objectProvider,
            });

            // Offset positions to place below existing canvas content
            return OffsetPositionsBelowExistingContent(refinedPositions, ghDoc, spacing);
        }

        /// <summary>
        /// Finds the live canvas object a component should update in place, or
        /// <c>null</c> when the component introduces a new object. A target only
        /// exists when <see cref="PutOptions.UpdateExistingByInstanceGuid"/> is
        /// enabled and the component carries a non-empty <c>instanceGuid</c> that
        /// matches an object already on the canvas.
        /// </summary>
        /// <param name="component">The incoming component definition.</param>
        /// <param name="ghDoc">The Grasshopper document to search.</param>
        /// <param name="options">The active put options.</param>
        /// <returns>The matched canvas object, or <c>null</c>.</returns>
        private static IGH_DocumentObject? FindUpdateTarget(
            GhJsonComponent component,
            GH_Document ghDoc,
            PutOptions options)
        {
            if (!options.UpdateExistingByInstanceGuid ||
                !component.InstanceGuid.HasValue ||
                component.InstanceGuid.Value == Guid.Empty)
            {
                return null;
            }

            return CanvasReader.FindObject(ghDoc, component.InstanceGuid.Value);
        }

        /// <summary>
        /// Determines whether an incoming component represents the same Grasshopper
        /// type as an existing canvas object. An explicit <c>componentGuid</c> takes
        /// precedence; otherwise the component is resolved through the shared
        /// name-resolution chain and the resolved proxy GUID is compared.
        /// </summary>
        /// <param name="component">The incoming component definition.</param>
        /// <param name="existing">The live canvas object matched by instance GUID.</param>
        /// <returns><c>true</c> when the incoming component is the same type.</returns>
        private static bool IsSameComponentType(GhJsonComponent component, IGH_DocumentObject existing)
        {
            if (component.ComponentGuid.HasValue && component.ComponentGuid.Value != Guid.Empty)
            {
                return component.ComponentGuid.Value == existing.ComponentGuid;
            }

            return ComponentInstantiator.Resolve(component)?.Guid == existing.ComponentGuid;
        }

        /// <summary>
        /// Offsets calculated positions to place them below existing canvas content.
        /// </summary>
        private static Dictionary<Guid, PointF> OffsetPositionsBelowExistingContent(
            Dictionary<Guid, PointF> positions,
            GH_Document ghDoc,
            float spacing)
        {
            var existingObjects = ghDoc.ActiveObjects().ToList();
            float startY = CanvasBoundsCalculator.CalculateBottomStartY(existingObjects, spacing);

            var offsetPositions = new Dictionary<Guid, PointF>(positions.Count);
            foreach (var kvp in positions)
            {
                offsetPositions[kvp.Key] = new PointF(kvp.Value.X, kvp.Value.Y + startY);
            }

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.CalculateLayoutPositions] Calculated {offsetPositions.Count} positions starting at Y={startY:F2}");
#endif

            return offsetPositions;
        }

        /// <summary>
        /// Calculates automatic offset to place new components below existing canvas content.
        /// </summary>
        /// <param name="components">The components to be placed.</param>
        /// <param name="ghDoc">The Grasshopper document.</param>
        /// <param name="spacing">Vertical spacing between existing content and new components.</param>
        /// <returns>The calculated offset, or PointF.Empty if canvas is empty or no pivots exist.</returns>
        private static PointF CalculateAutoOffset(
            IReadOnlyList<GhJsonComponent> components,
            GH_Document ghDoc,
            float spacing)
        {
            // Find components that have pivots
            var componentsWithPivots = components.Where(c => c.Pivot != null).ToList();
            if (componentsWithPivots.Count == 0)
            {
#if DEBUG
                Debug.WriteLine("[CanvasPlacer.CalculateAutoOffset] No components with pivots, skipping auto-offset");
#endif
                return PointF.Empty;
            }

            // Calculate bounds of existing canvas content
            var existingObjects = ghDoc.ActiveObjects().ToList();
            var bounds = CanvasBoundsCalculator.CalculateBounds(existingObjects);
            
            if (bounds.IsEmpty)
            {
#if DEBUG
                Debug.WriteLine("[CanvasPlacer.CalculateAutoOffset] Canvas is empty, no offset needed");
#endif
                return PointF.Empty;
            }

            float lowestY = bounds.LowestY + spacing;

            // Find the topmost Y coordinate among new components
            float minComponentY = float.MaxValue;
            foreach (var component in componentsWithPivots)
            {
                if (component.Pivot!.Y < minComponentY)
                {
                    minComponentY = (float)component.Pivot.Y;
                }
            }

            // Calculate offset to align topmost new component with lowestY
            float offsetY = lowestY - minComponentY;

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.CalculateAutoOffset] Calculated offset: (0, {offsetY:F2}) - lowestY={lowestY:F2}, minComponentY={minComponentY:F2}, spacing={spacing}");
#endif

            return new PointF(0, offsetY);
        }
    }
}
