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
        /// Places a GhJSON document on the canvas. Composes
        /// <see cref="Prepare"/> and <see cref="Apply"/>: standalone callers keep the
        /// historical single-call behavior, including the synchronous solution.
        /// </summary>
        /// <param name="document">The document to place.</param>
        /// <param name="options">Put options.</param>
        /// <returns>The put result.</returns>
        public static PutResult Put(GhJsonDocument document, PutOptions? options = null)
        {
            options ??= PutOptions.Default;
            var ghDoc = CanvasReader.GetActiveDocument();
            if (ghDoc == null)
            {
                return new PutResult { Success = false, ErrorMessage = "No active Grasshopper document" };
            }

            using var prepared = Prepare(document, options, ghDoc);
            return Apply(prepared);
        }

        /// <summary>
        /// Prepares a placement against a specific <paramref name="ghDoc"/> without
        /// mutating the canvas: resolves update targets, instantiates off-document
        /// prototypes, measures real bounds, and computes layout positions. The
        /// returned <see cref="PreparedPut"/> owns the prototypes; dispose it when
        /// the plan is refused or pass it to <see cref="Apply"/>.
        /// </summary>
        /// <param name="document">The document to place.</param>
        /// <param name="options">Put options.</param>
        /// <param name="ghDoc">The document the plan binds to.</param>
        /// <returns>The prepared placement handle.</returns>
        public static PreparedPut Prepare(GhJsonDocument document, PutOptions? options, GH_Document ghDoc)
        {
            options ??= PutOptions.Default;
            var prepared = new PreparedPut
            {
                Document = ghDoc,
                Model = document,
                Options = options,
                EffectiveOffset = options.Offset,
            };

            var result = new PutResult { Success = true };
            var deserializationOptions = new DeserializationOptions
            {
                RegenerateInstanceGuids = options.RegenerateInstanceGuids,
                SkipInvalidComponents = options.SkipInvalidComponents
            };
            DeserializationOptions? preserveGuidOptions = null;

            // Phase 1: instantiate every component up front. Created objects carry real
            // attribute bounds before they are added to the canvas, which lets the layout
            // pass below measure actual component sizes instead of default estimates.
            // Components whose instanceGuid already exists on the canvas skip
            // instantiation entirely when UpdateExistingByInstanceGuid is enabled:
            // same-type matches are bound for a later in-place update, different-type
            // matches produce a replacement prototype that keeps the existing
            // instance GUID.
            for (int i = 0; i < document.Components.Count; i++)
            {
                var component = document.Components[i];
                var existing = FindUpdateTarget(component, ghDoc, options);

                if (existing != null && IsSameComponentType(component, existing))
                {
                    prepared.Entries.Add(new PreparedPut.Entry { Component = component, Existing = existing });
                    continue;
                }

                var errorCount = component.Errors?.Count ?? 0;
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
                AppendNewComponentErrorsTo(prepared.Warnings, component, errorCount);

                if (component.ExtensionData?.Count > 0)
                {
                    prepared.Warnings.Add(
                        $"Component '{component.Name ?? component.ComponentGuid?.ToString() ?? "unknown"}' " +
                        $"carries unrecognized top-level properties ({string.Join(", ", component.ExtensionData.Keys)}) — " +
                        "ignored. Component state belongs under componentState.extensions.");
                }

                if (obj == null)
                {
                    if (!options.SkipInvalidComponents)
                    {
                        prepared.PrepareFailure = new PutResult
                        {
                            Success = false,
                            ErrorMessage = $"Failed to create component: {component.Name ?? component.ComponentGuid?.ToString()}",
                            FailedComponents = prepared.FailedComponents,
                            Warnings = prepared.Warnings,
                        };
                        prepared.FailedComponents = new List<string>();
                        prepared.Warnings = new List<string>();
                        return prepared;
                    }

                    prepared.FailedComponents.Add(component.Name ?? component.ComponentGuid?.ToString() ?? "unknown");
                    continue;
                }

                prepared.Entries.Add(new PreparedPut.Entry { Component = component, Prototype = obj, Existing = existing });
            }

            // Replaced pivots are captured at preparation so a pivotless replacement
            // can preserve the previous position without a second canvas read at
            // apply time.
            foreach (var entry in prepared.Entries)
            {
                if (entry.Existing != null && entry.Prototype != null)
                {
                    prepared.ReplacedPivots[entry.Existing.InstanceGuid] = entry.Existing.Attributes?.Pivot;
                }
            }

            // Phase 2: calculate positions for components without pivots using dependency
            // graph layout, measured against the freshly instantiated objects' bounds.
            prepared.LayoutPositions = CalculateLayoutPositions(document, ghDoc, options, prepared.Entries, prepared.ReplacedPivots);
            return prepared;
        }

        /// <summary>
        /// Applies a <see cref="PreparedPut"/> to its bound document in one pass:
        /// revalidates the bound targets, performs updates/removals/placements, seals
        /// undo records, and requests a solution only when the options ask for it.
        /// </summary>
        /// <param name="prepared">The prepared placement handle (consumed by the call).</param>
        /// <returns>The put result.</returns>
        public static PutResult Apply(PreparedPut prepared)
        {
            if (prepared.PrepareFailure != null)
            {
                var failure = prepared.PrepareFailure;
                prepared.Dispose();
                return failure;
            }

            var result = new PutResult { Success = true };
            if (!prepared.TryConsume())
            {
                result.Success = false;
                result.ErrorMessage = "The prepared placement was already consumed or disposed.";
                return result;
            }

            var ghDoc = prepared.Document;
            var document = prepared.Model;
            var options = prepared.Options;
            var effectiveOffset = prepared.EffectiveOffset;
            result.FailedComponents.AddRange(prepared.FailedComponents);
            result.Warnings.AddRange(prepared.Warnings);

            // Revalidate the bound targets before touching anything: a target deleted
            // between preparation and apply must fail the plan rather than silently
            // retargeting a switched or stale document.
            try
            {
                foreach (var entry in prepared.Entries)
                {
                    if (entry.Existing != null &&
                        ghDoc.FindObject(entry.Existing.InstanceGuid, false) == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "A prepared update/replacement target is no longer on the bound document.";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"The bound document is no longer usable: {ex.Message}";
                return result;
            }

            // Never mutate from inside a running solve: a synchronous solution owns
            // the document, so the commit is rejected rather than interleaved.
            if (ghDoc.SolutionDepth > 0)
            {
                result.Success = false;
                result.ErrorMessage = "The bound document is solving; the placement was not applied.";
                return result;
            }

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.Apply] Applying {prepared.Entries.Count} prepared entries");
#endif

            var idToObject = new Dictionary<int, IGH_DocumentObject>();
            var addedObjects = new List<IGH_DocumentObject>();
            GH_UndoRecord? updateUndoRecord = null;

            // Updates in place: document order, one sealed undo record shared by all
            // same-type targets so the whole update undoes as a single step.
            foreach (var entry in prepared.Entries)
            {
                if (entry.Existing == null || entry.Prototype != null)
                {
                    continue;
                }

                var component = entry.Component;
                var existing = entry.Existing;
                if (updateUndoRecord == null)
                {
                    updateUndoRecord = ghDoc.UndoUtil.CreateGenericObjectEvent(
                        "[GhJSON] Update object(s)", existing);
                }
                else
                {
                    existing.RecordUndoEvent(updateUndoRecord);
                }

                var updateErrorCount = component.Errors?.Count ?? 0;
                ObjectHandlerOrchestrator.Deserialize(component, existing);
                ObjectHandlerOrchestrator.PostPlacement(component, existing);
                AppendNewComponentErrors(result, component, updateErrorCount);

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
            }

            // Seal the shared update record before later mutations are recorded so
            // undo order mirrors apply order: adds/removals undo first, updates last.
            if (updateUndoRecord != null)
            {
                ghDoc.UndoUtil.RecordEvent(updateUndoRecord);
            }

            // Replace pass: superseded objects are only removed after their
            // replacements instantiated successfully during preparation, so a failed
            // component never destroys the original. External wires are captured
            // here — the freshest live state — so they can be restored onto the
            // replacements (which keep the same instance GUIDs).
            var replacedObjects = prepared.Entries
                .Where(entry => entry.Existing != null && entry.Prototype != null)
                .Select(entry => entry.Existing!)
                .ToList();
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

            // Assign pivots and add the prepared prototypes to the document.
            foreach (var entry in prepared.Entries)
            {
                if (entry.Prototype == null)
                {
                    continue;
                }

                var component = entry.Component;
                var obj = entry.Prototype;
                var replaces = entry.Existing;
                // Apply position: either from pivot + offset, or calculated layout position
                if (obj.Attributes != null)
                {
                    if (replaces != null)
                    {
                        // Replacements keep the document pivot verbatim (already applied
                        // by PivotHandler during deserialization); when the document
                        // omits a pivot the previous position is preserved.
                        if (component.Pivot == null &&
                            prepared.ReplacedPivots.TryGetValue(replaces.InstanceGuid, out var previousPivot) &&
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
                             prepared.LayoutPositions.TryGetValue(layoutKey, out var calculatedPosition))
                    {
                        // Use dependency graph calculated position for components without pivots.
                        // Use the same stable key as the layout engine so id-only components
                        // (no InstanceGuid) also receive their calculated position.
                        // Layout positions are bounds centers; convert to this object's
                        // pivot semantics (components pivot at center, sliders/panels at
                        // their top-left corner).
                        obj.Attributes.Pivot = PivotSemantics.CenterToPivot(
                            obj,
                            new PointF(calculatedPosition.X + effectiveOffset.X, calculatedPosition.Y + effectiveOffset.Y));
#if DEBUG
                        Debug.WriteLine($"[CanvasPlacer.Put] Applied layout position for '{component.Name}': ({calculatedPosition.X:F2}, {calculatedPosition.Y:F2})");
#endif
                    }
                }

                // Add to document (triggers AddedToDocument which may reset some properties)
                ghDoc.AddObject(obj, false);

                // Apply post-placement fixups for properties that get reset by AddedToDocument
                var postErrorCount = component.Errors?.Count ?? 0;
                ObjectHandlerOrchestrator.PostPlacement(component, obj);
                AppendNewComponentErrors(result, component, postErrorCount);

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

            // Request a solution only when the caller asked for one — a host-managed
            // apply selects false and schedules the downstream solve itself so the
            // acknowledgement is not blocked by arbitrary computation. A put invoked
            // from inside a running solution cannot re-enter NewSolution, so the
            // solve is deferred to the scheduler instead.
            if (options.RequestSolution)
            {
                if (ghDoc.SolutionDepth == 0)
                {
                    ghDoc.NewSolution(true);
                }
                else
                {
                    ghDoc.ScheduleSolution(5);
                }
            }

#if DEBUG
            Debug.WriteLine($"[CanvasPlacer.Put] Placement complete: {result.ComponentsPlaced} components, {result.ConnectionsCreated} connections, {result.GroupsCreated} groups");
#endif

            return result;
        }

        /// <summary>
        /// Appends component errors recorded since <paramref name="errorCount"/> was captured
        /// to the result warnings, so per-handler deserialization failures stay visible even
        /// though they no longer abort the operation.
        /// </summary>
        private static void AppendNewComponentErrors(PutResult result, GhJsonComponent component, int errorCount)
        {
            AppendNewComponentErrorsTo(result.Warnings, component, errorCount);
        }

        /// <summary>
        /// Appends component errors recorded since <paramref name="errorCount"/> was
        /// captured to the given warning list — used during preparation where no
        /// <see cref="PutResult"/> exists yet.
        /// </summary>
        private static void AppendNewComponentErrorsTo(List<string> warnings, GhJsonComponent component, int errorCount)
        {
            if (component.Errors != null && component.Errors.Count > errorCount)
            {
                warnings.AddRange(component.Errors.Skip(errorCount));
            }
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

            var sourceResolution = ConnectionHelper.TryResolveEndpointParam(fromObj, connection.From, input: false);
            var targetResolution = ConnectionHelper.TryResolveEndpointParam(toObj, connection.To, input: true);

            if (!sourceResolution.Resolved || !targetResolution.Resolved)
            {
                var reason = !sourceResolution.Resolved ? sourceResolution.Error : targetResolution.Error;
                result.Warnings.Add($"Connection from '{fromObj.Name}' ({connection.From.ParamName}) to '{toObj.Name}' ({connection.To.ParamName}) lost: {reason}");
                return false;
            }

            var sourceParam = sourceResolution.Param!;
            var targetParam = targetResolution.Param!;

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
            return ConnectionHelper.ResolveEndpointParam(obj, endpoint, isInput);
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
        /// <param name="options">Placement options, including automatic offset for missing pivots.</param>
        /// <param name="entries">Prepared entries; prototypes are measured for real bounds.</param>
        /// <param name="replacedPivots">Preserved pivots of objects being replaced in place.</param>
        /// <returns>Dictionary mapping instance GUID to calculated position.</returns>
        private static Dictionary<Guid, PointF> CalculateLayoutPositions(
            GhJsonDocument document,
            GH_Document ghDoc,
            PutOptions options,
            IReadOnlyList<PreparedPut.Entry> entries,
            IReadOnlyDictionary<Guid, PointF?> replacedPivots)
        {
            var unpositioned = entries
                .Where(entry => entry.Prototype != null && entry.Component.Pivot == null && entry.Existing == null)
                .Select(entry => entry.Component)
                .ToList();
            if (unpositioned.Count == 0)
            {
                return new Dictionary<Guid, PointF>();
            }

            // Real bounds come from the freshly instantiated prototypes first; anything
            // not measurable there falls back to a live canvas lookup (e.g. objects
            // sharing instance GUIDs with the incoming document).
            var measuredByKey = new Dictionary<Guid, IGH_DocumentObject>(entries.Count);
            foreach (var entry in entries)
            {
                if (entry.Prototype == null)
                {
                    continue;
                }

                var key = Core.GhJson.GetLayoutKey(entry.Component);
                if (key != Guid.Empty)
                {
                    measuredByKey[key] = entry.Prototype;
                }
            }

            // Mirror the declared wiring on the placed (not yet added) prototypes so
            // measured bounds reflect the connected state — a wired floating
            // parameter renders taller than an unwired one. The real wiring pass
            // deduplicates already-linked sources.
            var placedById = new Dictionary<int, IGH_DocumentObject>(entries.Count);
            foreach (var entry in entries)
            {
                if (entry.Prototype != null && entry.Component.Id.HasValue)
                {
                    placedById[entry.Component.Id.Value] = entry.Prototype;
                }
            }

            foreach (var connection in document.Connections ?? Enumerable.Empty<GhJsonConnection>())
            {
                try
                {
                    if (!placedById.TryGetValue(connection.From.Id, out var fromObj) ||
                        !placedById.TryGetValue(connection.To.Id, out var toObj))
                    {
                        continue;
                    }

                    var sourceParam = GetParameter(fromObj, connection.From, isInput: false);
                    var targetParam = GetParameter(toObj, connection.To, isInput: true);
                    if (sourceParam != null && targetParam != null && !targetParam.Sources.Contains(sourceParam))
                    {
                        targetParam.AddSource(sourceParam);
                    }
                }
                catch
                {
                    // Measure wiring is best-effort; unresolved endpoints are skipped.
                }
            }

            // Placement grows wired panels to fit streamed data rows; apply the same
            // reservation so measured bounds match the placed result.
            foreach (var entry in entries)
            {
                if (entry.Prototype is GH_Panel panel)
                {
                    PanelHandler.ReserveDataRows(panel);
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
            var fixedPositions = new Dictionary<Guid, PointF>();
            foreach (var component in document.Components.Where(c => !unpositioned.Contains(c)))
            {
                var key = Core.GhJson.GetLayoutKey(component);
                var obj = objectProvider(key);
                if (obj != null)
                {
                    fixedPositions[key] = component.Pivot == null &&
                                          replacedPivots.TryGetValue(obj.InstanceGuid, out var previousPivot) &&
                                          previousPivot.HasValue
                        ? PivotSemantics.PivotToCenter(obj, previousPivot.Value)
                        : PivotSemantics.BoundsCenter(obj);
                }
            }

            var unanchoredTarget = options.AutoOffset
                ? CanvasBoundsCalculator.UnanchoredTargetTopLeft(ghDoc.ActiveObjects(), options.AutoOffsetSpacing)
                : null;
            return GhJsonGrasshopper.ComputeLayout(document, new CanvasLayoutOptions
            {
                NodeSizeProvider = sizeProvider,
                ObjectProvider = objectProvider,
                Refinements = new LayoutRefinementOptions
                {
                    FixedPositions = fixedPositions,
                    UnanchoredTargetTopLeft = unanchoredTarget,
                },
            });
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

    }
}
