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
using System.Drawing;
using System.Linq;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Validation;
using GhJSON.Grasshopper.Canvas;
using GhJSON.Grasshopper.Serialization;
using GhJSON.Grasshopper.Serialization.ComponentHandlers;
using GhJSON.Grasshopper.Serialization.DataTypes;
using GhJSON.Grasshopper.Serialization.ScriptComponents;
using GhJSON.Grasshopper.Validation;
using Grasshopper;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper
{
    /// <summary>
    /// Main entry point for Grasshopper-specific GhJSON operations.
    /// Provides a unified API for serializing, deserializing, and manipulating GhJSON documents
    /// with Grasshopper canvas integration.
    /// </summary>
    public static class GhJsonGrasshopper
    {
        private static bool _initialized;
        private static readonly object _initLock = new object();

        /// <summary>
        /// Ensures all registries are initialized.
        /// Called automatically by Serialize/Deserialize methods.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                // Initialize geometric serializers (registers with DataTypeRegistry)
                GeometricSerializerRegistry.Initialize();

                // Initialize component handlers (lazy-loaded via Default property)
                _ = ComponentHandlerRegistry.Default;

                _initialized = true;
            }
        }

        #region Serialize (GH → GhJSON)

        /// <summary>
        /// Serializes a collection of Grasshopper objects to a GhJSON document.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Optional serialization options.</param>
        /// <returns>A GhJSON document representing the serialized objects.</returns>
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options = null)
        {
            Initialize();
            return GhJsonSerializer.Serialize(objects, options);
        }

        /// <summary>
        /// Serialization options factory helpers.
        /// </summary>
        public static class Options
        {
            /// <summary>
            /// Creates standard serialization options with full metadata and data.
            /// </summary>
            public static SerializationOptions Standard(
                bool includeMetadata = true,
                bool includeGroups = true,
                bool includePersistentData = true)
            {
                var options = SerializationOptions.Standard;
                options.IncludeMetadata = includeMetadata;
                options.IncludeGroups = includeGroups;
                options.IncludePersistentData = includePersistentData;
                return options;
            }

            /// <summary>
            /// Creates optimized serialization options for reduced output size.
            /// </summary>
            public static SerializationOptions Optimized(
                bool includeMetadata = false,
                bool includeGroups = false,
                bool includePersistentData = false)
            {
                var options = SerializationOptions.Optimized;
                options.IncludeMetadata = includeMetadata;
                options.IncludeGroups = includeGroups;
                options.IncludePersistentData = includePersistentData;
                return options;
            }

            /// <summary>
            /// Creates lite serialization options (minimal output, no metadata or persistent data).
            /// </summary>
            public static SerializationOptions Lite()
            {
                return SerializationOptions.Lite;
            }
        }

        /// <summary>
        /// Serializes a collection of Grasshopper objects using a custom handler registry.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <param name="handlerRegistry">Custom handler registry for component serialization.</param>
        /// <returns>A GhJSON document representing the serialized objects.</returns>
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options,
            ComponentHandlerRegistry handlerRegistry)
        {
            Initialize();
            return GhJsonSerializer.Serialize(objects, options, handlerRegistry);
        }

        #endregion

        #region Script

        /// <summary>
        /// Script component operations.
        /// </summary>
        public static class Script
        {
            /// <summary>
            /// Creates a GhJSON document containing a script component.
            /// </summary>
            public static string CreateGhJson(
                string languageKey,
                string scriptCode,
                JArray? inputs = null,
                JArray? outputs = null,
                string? nickname = null,
                Guid? instanceGuid = null,
                PointF? pivot = null,
                bool indented = false)
            {
                var inputSettings = ConvertToParameterSettings(inputs);
                var outputSettings = ConvertToParameterSettings(outputs);
                var comp = ScriptComponentFactory.CreateScriptComponent(languageKey, scriptCode, inputSettings, outputSettings, nickname);

                if (instanceGuid.HasValue)
                {
                    comp.InstanceGuid = instanceGuid;
                }

                if (pivot.HasValue)
                {
                    comp.Pivot = pivot.Value;
                }

                var doc = Core.GhJson.CreateDocument();
                doc.Components.Add(comp);

                return Core.GhJson.Serialize(doc, new Core.WriteOptions { Indented = indented });
            }

            /// <summary>
            /// Converts JArray parameter definitions to ParameterSettings.
            /// </summary>
            public static List<Core.Models.Components.ParameterSettings>? ConvertToParameterSettings(JArray? parameters)
            {
                if (parameters == null || parameters.Count == 0)
                {
                    return null;
                }

                var result = new List<Core.Models.Components.ParameterSettings>();
                foreach (var param in parameters)
                {
                    if (param is not JObject obj)
                    {
                        continue;
                    }

                    var name = obj["name"]?.ToString();
                    var variableName = obj["variableName"]?.ToString() ?? name;

                    var settings = new Core.Models.Components.ParameterSettings
                    {
                        ParameterName = name ?? "param",
                        VariableName = variableName,
                        Description = obj["description"]?.ToString(),
                        TypeHint = obj["type"]?.ToString(),
                        Access = obj["access"]?.ToString(),
                        DataMapping = obj["dataMapping"]?.ToString(),
                        Required = obj["required"]?.ToObject<bool?>(),
                        IsPrincipal = obj["isPrincipal"]?.ToObject<bool?>(),
                        Expression = obj["expression"]?.ToString(),
                        Reverse = obj["reverse"]?.ToObject<bool?>(),
                        Simplify = obj["simplify"]?.ToObject<bool?>(),
                        Invert = obj["invert"]?.ToObject<bool?>(),
                    };

                    result.Add(settings);
                }

                return result.Count > 0 ? result : null;
            }

            /// <summary>
            /// Detects the script language from a component GUID.
            /// </summary>
            public static string DetectLanguageFromGuid(Guid componentGuid)
            {
                return ScriptComponentFactory.DetectLanguageFromGuid(componentGuid);
            }

            /// <summary>
            /// Gets component info for a script language.
            /// </summary>
            public static ScriptComponentInfo? GetComponentInfo(string? languageKey)
            {
                return ScriptComponentFactory.GetComponentInfo(languageKey);
            }

            /// <summary>
            /// Normalizes a language identifier to a supported key or returns the provided default.
            /// </summary>
            public static string NormalizeLanguageKeyOrDefault(string? languageKey, string defaultLanguage = "python")
            {
                return ScriptComponentFactory.NormalizeLanguageKeyOrDefault(languageKey, defaultLanguage);
            }
        }

        #endregion

        #region Runtime Data

        /// <summary>
        /// Extracts runtime data from Grasshopper objects.
        /// </summary>
        public static JObject ExtractRuntimeData(IEnumerable<IGH_ActiveObject> objects)
        {
            Initialize();
            return GhJsonSerializer.ExtractRuntimeData(objects);
        }

        #endregion

        #region Deserialize (GhJSON → GH objects)

        /// <summary>
        /// Deserializes a GhJSON document to Grasshopper objects (not placed on canvas).
        /// </summary>
        /// <param name="document">The document to deserialize.</param>
        /// <param name="options">Optional deserialization options.</param>
        /// <returns>A deserialization result containing the created components.</returns>
        public static DeserializationResult Deserialize(
            GhJsonDocument document,
            DeserializationOptions? options = null)
        {
            Initialize();
            return GhJsonDeserializer.Deserialize(document, options);
        }

        /// <summary>
        /// Deserializes a GhJSON document using a custom handler registry.
        /// </summary>
        /// <param name="document">The document to deserialize.</param>
        /// <param name="options">Deserialization options.</param>
        /// <param name="handlerRegistry">Custom handler registry for component deserialization.</param>
        /// <returns>A deserialization result containing the created components.</returns>
        public static DeserializationResult Deserialize(
            GhJsonDocument document,
            DeserializationOptions? options,
            ComponentHandlerRegistry handlerRegistry)
        {
            Initialize();
            return GhJsonDeserializer.Deserialize(document, options, handlerRegistry);
        }

        #endregion

        #region Get (Read from Canvas)

        /// <summary>
        /// Gets all objects from the active Grasshopper canvas as a GhJSON document.
        /// </summary>
        /// <param name="options">Optional serialization options.</param>
        /// <returns>A GhJSON document representing the canvas contents.</returns>
        public static GhJsonDocument Get(SerializationOptions? options = null)
        {
            Initialize();
            var canvas = Instances.ActiveCanvas;
            if (canvas?.Document == null)
            {
                return new GhJsonDocument();
            }

            var objects = canvas.Document.Objects.OfType<IGH_ActiveObject>().ToList();
            return Serialize(objects, options);
        }

        /// <summary>
        /// Gets the currently selected objects from the canvas as a GhJSON document.
        /// </summary>
        /// <param name="options">Optional serialization options.</param>
        /// <returns>A GhJSON document representing the selected objects.</returns>
        public static GhJsonDocument GetSelected(SerializationOptions? options = null)
        {
            Initialize();
            var canvas = Instances.ActiveCanvas;
            if (canvas?.Document == null)
            {
                return new GhJsonDocument();
            }

            var selected = canvas.Document.SelectedObjects()
                .OfType<IGH_ActiveObject>()
                .ToList();
            return Serialize(selected, options);
        }

        /// <summary>
        /// Gets specific objects by their instance GUIDs as a GhJSON document.
        /// </summary>
        /// <param name="guids">The instance GUIDs of objects to retrieve.</param>
        /// <param name="options">Optional serialization options.</param>
        /// <returns>A GhJSON document representing the specified objects.</returns>
        public static GhJsonDocument GetByGuids(IEnumerable<Guid> guids, SerializationOptions? options = null)
        {
            Initialize();
            var canvas = Instances.ActiveCanvas;
            if (canvas?.Document == null)
            {
                return new GhJsonDocument();
            }

            var guidSet = new HashSet<Guid>(guids);
            var objects = canvas.Document.Objects
                .Where(o => guidSet.Contains(o.InstanceGuid))
                .OfType<IGH_ActiveObject>()
                .ToList();
            return Serialize(objects, options);
        }

        /// <summary>
        /// Gets objects from the canvas with advanced options including connection depth expansion.
        /// </summary>
        /// <param name="options">Get options controlling scope, filters, and expansion.</param>
        /// <returns>A get result containing the document and expansion information.</returns>
        public static Canvas.GetResult GetWithOptions(Canvas.GetOptions options)
        {
            Initialize();
            options ??= Canvas.GetOptions.Default;

            var canvas = Instances.ActiveCanvas;
            if (canvas?.Document == null)
            {
                return new Canvas.GetResult { Document = new GhJsonDocument() };
            }

            static bool MatchesAttributes(IGH_ActiveObject obj, Canvas.GetAttributes attributes)
            {
                if (attributes.HasFlag(Canvas.GetAttributes.Selected) && !obj.Attributes.Selected)
                {
                    return false;
                }

                if (attributes.HasFlag(Canvas.GetAttributes.HasError) && !Canvas.FilterHelpers.HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Error))
                {
                    return false;
                }

                if (attributes.HasFlag(Canvas.GetAttributes.HasWarning) && !Canvas.FilterHelpers.HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Warning))
                {
                    return false;
                }

                if (attributes.HasFlag(Canvas.GetAttributes.HasRemark) && !Canvas.FilterHelpers.HasRuntimeMessage(obj, GH_RuntimeMessageLevel.Remark))
                {
                    return false;
                }

                if (attributes.HasFlag(Canvas.GetAttributes.Enabled))
                {
                    if (obj is GH_Component c)
                    {
                        if (c.Locked)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (attributes.HasFlag(Canvas.GetAttributes.Disabled))
                {
                    if (obj is GH_Component c)
                    {
                        if (!c.Locked)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (attributes.HasFlag(Canvas.GetAttributes.PreviewOn))
                {
                    if (obj is GH_Component c)
                    {
                        if (!c.IsPreviewCapable || c.Hidden)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (attributes.HasFlag(Canvas.GetAttributes.PreviewOff))
                {
                    if (obj is GH_Component c)
                    {
                        if (!c.IsPreviewCapable || !c.Hidden)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }

            static bool MatchesObjectKinds(IGH_ActiveObject obj, Canvas.GetObjectKinds kinds)
            {
                var isComponent = obj is IGH_Component;
                var isParam = obj is IGH_Param;

                var matches = false;
                if (kinds.HasFlag(Canvas.GetObjectKinds.Components) && isComponent)
                {
                    matches = true;
                }

                if (kinds.HasFlag(Canvas.GetObjectKinds.Params) && isParam)
                {
                    matches = true;
                }

                return matches;
            }


            // Determine initial object set based on scope/filters
            List<IGH_ActiveObject> objects;
            if (options.GuidsFilter != null && options.GuidsFilter.Any())
            {
                var guidSet = new HashSet<Guid>(options.GuidsFilter);
                objects = canvas.Document.Objects
                    .Where(o => guidSet.Contains(o.InstanceGuid))
                    .OfType<IGH_ActiveObject>()
                    .ToList();
            }
            else
            {
                objects = options.Scope switch
                {
                    Canvas.GetScope.Selected => canvas.Document.SelectedObjects()
                        .OfType<IGH_ActiveObject>()
                        .ToList(),
                    Canvas.GetScope.Errors => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => Canvas.FilterHelpers.HasRuntimeMessage(o, GH_RuntimeMessageLevel.Error))
                        .ToList(),
                    Canvas.GetScope.Warnings => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => Canvas.FilterHelpers.HasRuntimeMessage(o, GH_RuntimeMessageLevel.Warning))
                        .ToList(),
                    Canvas.GetScope.Remarks => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => Canvas.FilterHelpers.HasRuntimeMessage(o, GH_RuntimeMessageLevel.Remark))
                        .ToList(),
                    Canvas.GetScope.Enabled => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => o is GH_Component c && !c.Locked)
                        .ToList(),
                    Canvas.GetScope.Disabled => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => o is GH_Component c && c.Locked)
                        .ToList(),
                    Canvas.GetScope.PreviewOn => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => o is GH_Component c && c.IsPreviewCapable && !c.Hidden)
                        .ToList(),
                    Canvas.GetScope.PreviewOff => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .Where(o => o is GH_Component c && c.IsPreviewCapable && c.Hidden)
                        .ToList(),
                    _ => canvas.Document.Objects
                        .OfType<IGH_ActiveObject>()
                        .ToList()
                };
            }

            // Scope values that depend on graph degree
            Dictionary<Guid, (int indegree, int outdegree)>? degrees = null;

            if (options.GuidsFilter == null || !options.GuidsFilter.Any())
            {
                if (options.Scope is Canvas.GetScope.StartNodes or Canvas.GetScope.EndNodes or Canvas.GetScope.MiddleNodes or Canvas.GetScope.IsolatedNodes)
                {
                    var allObjects = canvas.Document.Objects.OfType<IGH_ActiveObject>().ToList();
                    var fullDoc = Serialize(allObjects, Options.Lite());
                    degrees = Canvas.FilterHelpers.BuildDegrees(fullDoc);

                    var role = options.Scope switch
                    {
                        Canvas.GetScope.StartNodes => Canvas.GetNodeRoles.StartNodes,
                        Canvas.GetScope.EndNodes => Canvas.GetNodeRoles.EndNodes,
                        Canvas.GetScope.MiddleNodes => Canvas.GetNodeRoles.MiddleNodes,
                        _ => Canvas.GetNodeRoles.IsolatedNodes,
                    };

                    objects = allObjects
                        .Where(o => Canvas.FilterHelpers.MatchesNodeRoles(o.InstanceGuid, role, degrees))
                        .ToList();
                }
                else if (options.Scope == Canvas.GetScope.ParamsOnly)
                {
                    objects = canvas.Document.Objects.OfType<IGH_ActiveObject>().Where(o => o is IGH_Param).ToList();
                }
                else if (options.Scope == Canvas.GetScope.ComponentsOnly)
                {
                    objects = canvas.Document.Objects.OfType<IGH_ActiveObject>().Where(o => o is IGH_Component).ToList();
                }
            }

            // Apply strongly-typed filters
            if (options.TypeFilter != null)
            {
                if (degrees == null)
                {
                    var allObjects = canvas.Document.Objects.OfType<IGH_ActiveObject>().ToList();
                    var fullDoc = Serialize(allObjects, Options.Lite());
                    degrees = Canvas.FilterHelpers.BuildDegrees(fullDoc);
                }

                objects = objects.Where(o => Canvas.FilterHelpers.ApplyTypeFilter(o, options.TypeFilter, degrees)).ToList();
            }

            if (options.AttributeFilter != null)
            {
                objects = objects.Where(o => Canvas.FilterHelpers.ApplyAttributeFilter(o, options.AttributeFilter)).ToList();
            }

            if (options.CategoryFilter != null)
            {
                objects = objects.Where(o => Canvas.FilterHelpers.ApplyCategoryFilter(o, options.CategoryFilter)).ToList();
            }

            var result = new Canvas.GetResult();
            result.InitialGuids.AddRange(objects.Select(o => o.InstanceGuid));

            // Apply connection depth expansion if requested
            if (options.ConnectionDepth > 0)
            {
                // Serialize all canvas objects to build connection graph
                var allObjects = canvas.Document.Objects.OfType<IGH_ActiveObject>().ToList();
                var fullDoc = Serialize(allObjects, Options.Lite());

                // Expand by depth
                var expandedGuids = Canvas.ConnectionDepthExpander.ExpandByDepth(
                    fullDoc,
                    result.InitialGuids,
                    options.ConnectionDepth);

                result.ExpandedGuids.AddRange(expandedGuids);

                // Update object list to include expanded set
                var guidSet = new HashSet<Guid>(expandedGuids);
                objects = allObjects.Where(o => guidSet.Contains(o.InstanceGuid)).ToList();
            }
            else
            {
                result.ExpandedGuids.AddRange(result.InitialGuids);
            }

            // Serialize the final object set
            result.Document = Serialize(objects, options.SerializationOptions);

            // Trim connections if requested
            if (options.TrimConnectionsToResult)
            {
                var allowedGuids = new HashSet<Guid>(result.ExpandedGuids);
                Canvas.ConnectionDepthExpander.TrimConnections(result.Document, allowedGuids);
            }

            return result;
        }

        /// <summary>
        /// Connects two components on the active canvas by creating a wire between an output and an input parameter.
        /// </summary>
        /// <param name="sourceGuid">GUID of the source component (output side).</param>
        /// <param name="targetGuid">GUID of the target component (input side).</param>
        /// <param name="sourceParamName">Name or nickname of the output parameter. If null, uses first output.</param>
        /// <param name="targetParamName">Name or nickname of the input parameter. If null, uses first input.</param>
        /// <param name="redraw">True to redraw canvas and trigger solution recalculation.</param>
        /// <returns>True if the connection was successful (or already existed).</returns>
        public static bool ConnectComponents(
            Guid sourceGuid,
            Guid targetGuid,
            string? sourceParamName = null,
            string? targetParamName = null,
            bool redraw = true)
        {
            Initialize();
            return Canvas.ConnectionBuilder.ConnectComponents(
                sourceGuid,
                targetGuid,
                sourceParamName,
                targetParamName,
                redraw);
        }

        #endregion

        #region Put (Place on Canvas)

        /// <summary>
        /// Places a GhJSON document on the active Grasshopper canvas.
        /// </summary>
        /// <param name="document">The document to place.</param>
        /// <param name="options">Optional put options.</param>
        /// <returns>A put result containing the placed objects and any errors.</returns>
        public static PutResult Put(GhJsonDocument document, PutOptions? options = null)
        {
            Initialize();
            options ??= PutOptions.Default;

            var result = new PutResult();
            var deserializationOptions = options.ToDeserializationOptions();

            // Deserialize components
            var deserializationResult = Deserialize(document, deserializationOptions);
            if (!deserializationResult.IsSuccess)
            {
                result.Errors.AddRange(deserializationResult.Errors);
                return result;
            }

            result.Warnings.AddRange(deserializationResult.Warnings);

            // Place components on canvas
            var placedNames = ComponentPlacer.PlaceComponents(
                deserializationResult,
                document,
                deserializationResult.GuidMapping,
                options.Offset,
                options.Spacing,
                useExactPositions: options.UseExactPositions,
                useDependencyLayout: options.UseDependencyLayout);

            // Build result mappings
            result.PlacedObjects.AddRange(deserializationResult.Components);
            result.IdMapping = deserializationResult.IdMapping;
            result.GuidMapping = deserializationResult.GuidMapping;

            // Create connections
            if (options.CreateConnections && document.Connections != null)
            {
                var connectionsCreated = ConnectionManager.CreateConnections(
                    document,
                    deserializationResult.GuidMapping);
                if (connectionsCreated < document.Connections.Count)
                {
                    result.Warnings.Add($"Created {connectionsCreated} of {document.Connections.Count} connections");
                }
            }

            // Restore external connections if provided
            if (options.PreserveExternalConnections && options.CapturedConnections != null)
            {
                result.ExternalConnectionsRestored = Canvas.ExternalConnectionManager.RestoreExternalConnections(
                    options.CapturedConnections);
                
                if (result.ExternalConnectionsRestored < options.CapturedConnections.Connections.Count)
                {
                    result.Warnings.Add($"Restored {result.ExternalConnectionsRestored} of {options.CapturedConnections.Connections.Count} external connections");
                }
            }

            // Create groups
            if (options.CreateGroups && document.Groups != null)
            {
                var groupsCreated = GroupManager.CreateGroups(
                    document,
                    deserializationResult.GuidMapping);
                if (groupsCreated < document.Groups.Count)
                {
                    result.Warnings.Add($"Created {groupsCreated} of {document.Groups.Count} groups");
                }
            }

            return result;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates a GhJSON document with Grasshopper-specific checks.
        /// </summary>
        /// <param name="document">The document to validate.</param>
        /// <returns>A validation result containing errors, warnings, and info messages.</returns>
        public static ValidationResult Validate(GhJsonDocument document)
        {
            var json = Core.GhJson.Serialize(document);
            return Validate(json);
        }

        /// <summary>
        /// Validates a JSON string as a GhJSON document with Grasshopper-specific checks.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>A validation result containing errors, warnings, and info messages.</returns>
        public static ValidationResult Validate(string json)
        {
            return GhJsonGrasshopperValidator.ValidateDetailed(json);
        }

        /// <summary>
        /// Checks if a GhJSON document is valid with Grasshopper-specific checks.
        /// </summary>
        /// <param name="document">The document to check.</param>
        /// <param name="errorMessage">Output parameter for error messages if invalid.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(GhJsonDocument document, out string? errorMessage)
        {
            var json = Core.GhJson.Serialize(document);
            return GhJsonGrasshopperValidator.Validate(json, out errorMessage);
        }

        /// <summary>
        /// Checks if a JSON string is a valid GhJSON document with Grasshopper-specific checks.
        /// </summary>
        /// <param name="json">The JSON string to check.</param>
        /// <param name="errorMessage">Output parameter for error messages if invalid.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValid(string json, out string? errorMessage)
        {
            return GhJsonGrasshopperValidator.Validate(json, out errorMessage);
        }

        #endregion

        #region Component Handler Registry

        /// <summary>
        /// Gets the default component handler registry.
        /// </summary>
        public static ComponentHandlerRegistry HandlerRegistry => ComponentHandlerRegistry.Default;

        /// <summary>
        /// Registers a custom component handler.
        /// </summary>
        /// <param name="handler">The handler to register.</param>
        public static void RegisterHandler(IComponentHandler handler)
        {
            ComponentHandlerRegistry.Default.Register(handler);
        }

        #endregion
    }

    /// <summary>
    /// Options for placing GhJSON documents on the canvas.
    /// </summary>
    public class PutOptions
    {
        /// <summary>
        /// Gets the default put options.
        /// </summary>
        public static PutOptions Default { get; } = new PutOptions();

        /// <summary>
        /// Gets or sets the position offset for placed components.
        /// </summary>
        public PointF? Offset { get; set; }

        /// <summary>
        /// Gets or sets whether to create connections between components.
        /// </summary>
        public bool CreateConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to create groups.
        /// </summary>
        public bool CreateGroups { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to register an undo action.
        /// </summary>
        public bool RegisterUndo { get; set; } = true;

        /// <summary>
        /// Gets or sets the spacing between components when using auto-layout.
        /// </summary>
        public int Spacing { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to use exact positions from GhJSON without offset.
        /// </summary>
        public bool UseExactPositions { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use dependency-based layout when no pivots exist.
        /// </summary>
        public bool UseDependencyLayout { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to preserve instance GUIDs from the document.
        /// </summary>
        public bool PreserveInstanceGuids { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to apply component state (locked, hidden, value).
        /// </summary>
        public bool ApplyComponentState { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to apply schema properties.
        /// </summary>
        public bool ApplyAdditionalProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to apply parameter settings.
        /// </summary>
        public bool ApplyParameterSettings { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to preserve external connections when replacing existing objects.
        /// When true and PreserveInstanceGuids is true, captures and restores connections to/from objects outside the document.
        /// </summary>
        public bool PreserveExternalConnections { get; set; } = false;

        /// <summary>
        /// Gets or sets captured external connections to restore after placement.
        /// Used internally when PreserveExternalConnections is enabled.
        /// </summary>
        public Canvas.CapturedConnections CapturedConnections { get; set; }

        /// <summary>
        /// Converts to deserialization options.
        /// </summary>
        internal DeserializationOptions ToDeserializationOptions()
        {
            return new DeserializationOptions
            {
                PreserveInstanceGuids = this.PreserveInstanceGuids,
                ApplyComponentState = this.ApplyComponentState,
                ApplyAdditionalProperties = this.ApplyAdditionalProperties,
                ApplyParameterSettings = this.ApplyParameterSettings
            };
        }
    }

    /// <summary>
    /// Result of a put operation.
    /// </summary>
    public class PutResult
    {
        /// <summary>
        /// Gets the list of placed objects.
        /// </summary>
        public List<IGH_DocumentObject> PlacedObjects { get; } = new List<IGH_DocumentObject>();

        /// <summary>
        /// Gets the mapping from integer IDs to placed objects.
        /// </summary>
        public Dictionary<int, IGH_DocumentObject> IdMapping { get; set; } = new Dictionary<int, IGH_DocumentObject>();

        /// <summary>
        /// Gets the mapping from GUIDs to placed objects.
        /// </summary>
        public Dictionary<Guid, IGH_DocumentObject> GuidMapping { get; set; } = new Dictionary<Guid, IGH_DocumentObject>();

        /// <summary>
        /// Gets the list of errors that occurred.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the list of warnings that occurred.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets the number of external connections restored (when applicable).
        /// </summary>
        public int ExternalConnectionsRestored { get; set; }

        /// <summary>
        /// Gets a value indicating whether the operation was successful (no errors).
        /// </summary>
        public bool IsSuccess => Errors.Count == 0;
    }
}
