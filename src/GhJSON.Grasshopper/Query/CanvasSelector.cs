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
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Query
{
    /// <summary>
    /// Fluent query builder for selecting Grasshopper canvas objects.
    /// Filters are accumulated and applied lazily when <see cref="Execute"/> is called.
    /// <para>
    /// Example usage:
    /// <code>
    /// var objects = CanvasSelector.FromActiveCanvas()
    ///     .WithAttributes("+selected", "+error")
    ///     .WithCategories("+Maths", "-Curve")
    ///     .WithTypes("+startnodes")
    ///     .WithConnected(depth: 1)
    ///     .Execute();
    /// </code>
    /// </para>
    /// </summary>
    public sealed class CanvasSelector
    {
        private readonly List<IGH_DocumentObject> _source;
        private readonly List<string> _attributeTokens = new List<string>();
        private readonly List<string> _typeTokens = new List<string>();
        private readonly List<string> _categoryTokens = new List<string>();
        private readonly List<Guid> _guidFilter = new List<Guid>();
        private int _connectionDepth;

        private CanvasSelector(List<IGH_DocumentObject> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        #region Factory methods

        /// <summary>
        /// Creates a selector over all objects on the active Grasshopper canvas.
        /// Returns an empty selector if no canvas is open.
        /// </summary>
        /// <returns>A new <see cref="CanvasSelector"/> instance.</returns>
        public static CanvasSelector FromActiveCanvas()
        {
            var doc = Instances.ActiveCanvas?.Document;
            if (doc == null)
            {
                return new CanvasSelector(new List<IGH_DocumentObject>());
            }

            return new CanvasSelector(doc.Objects.ToList());
        }

        /// <summary>
        /// Creates a selector over the objects in a specific <see cref="GH_Document"/>.
        /// </summary>
        /// <param name="document">The Grasshopper document to query.</param>
        /// <returns>A new <see cref="CanvasSelector"/> instance.</returns>
        public static CanvasSelector From(GH_Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return new CanvasSelector(document.Objects.ToList());
        }

        /// <summary>
        /// Creates a selector over an explicit set of objects.
        /// Useful for chaining queries or testing.
        /// </summary>
        /// <param name="objects">The objects to query.</param>
        /// <returns>A new <see cref="CanvasSelector"/> instance.</returns>
        public static CanvasSelector From(IEnumerable<IGH_DocumentObject> objects)
        {
            if (objects == null)
            {
                throw new ArgumentNullException(nameof(objects));
            }

            return new CanvasSelector(objects.ToList());
        }

        #endregion

        #region Fluent filter methods

        /// <summary>
        /// Adds attribute filter tokens (e.g. <c>"+selected"</c>, <c>"-error"</c>).
        /// Multiple calls are cumulative.
        /// </summary>
        /// <param name="tokens">One or more attribute filter tokens.</param>
        /// <returns>This selector for chaining.</returns>
        public CanvasSelector WithAttributes(params string[] tokens)
        {
            _attributeTokens.AddRange(tokens);
            return this;
        }

        /// <summary>
        /// Adds type filter tokens (e.g. <c>"+params"</c>, <c>"+startnodes"</c>).
        /// Multiple calls are cumulative.
        /// </summary>
        /// <param name="tokens">One or more type filter tokens.</param>
        /// <returns>This selector for chaining.</returns>
        public CanvasSelector WithTypes(params string[] tokens)
        {
            _typeTokens.AddRange(tokens);
            return this;
        }

        /// <summary>
        /// Adds category filter tokens (e.g. <c>"+Maths"</c>, <c>"-Curve"</c>).
        /// Multiple calls are cumulative.
        /// </summary>
        /// <param name="tokens">One or more category filter tokens.</param>
        /// <returns>This selector for chaining.</returns>
        public CanvasSelector WithCategories(params string[] tokens)
        {
            _categoryTokens.AddRange(tokens);
            return this;
        }

        /// <summary>
        /// Restricts the initial set to objects matching the given instance GUIDs.
        /// Multiple calls are cumulative.
        /// </summary>
        /// <param name="guids">GUIDs to include.</param>
        /// <returns>This selector for chaining.</returns>
        public CanvasSelector WithGuids(IEnumerable<Guid> guids)
        {
            _guidFilter.AddRange(guids);
            return this;
        }

        /// <summary>
        /// After all filters are applied, expands the result set by walking connections
        /// up to <paramref name="depth"/> hops in either direction.
        /// </summary>
        /// <param name="depth">Number of connection hops (0 = no expansion).</param>
        /// <returns>This selector for chaining.</returns>
        public CanvasSelector WithConnected(int depth)
        {
            _connectionDepth = Math.Max(0, depth);
            return this;
        }

        #endregion

        #region Execution

        /// <summary>
        /// Applies all accumulated filters and returns the matching objects.
        /// <para>
        /// Filter application order:
        /// 1. GUID restriction
        /// 2. Type filters (params/components/startnodes/endnodes/middlenodes/isolatednodes)
        /// 3. Category filters
        /// 4. Attribute filters (selected/enabled/error/warning/remark/preview)
        /// 5. Connection depth expansion
        /// </para>
        /// </summary>
        /// <returns>The filtered list of document objects.</returns>
        public List<IGH_DocumentObject> Execute()
        {
            var objects = new List<IGH_DocumentObject>(_source);

            // 1. GUID restriction
            if (_guidFilter.Count > 0)
            {
                var guidSet = new HashSet<Guid>(_guidFilter);
                objects = objects.Where(o => guidSet.Contains(o.InstanceGuid)).ToList();
            }

            // 2. Type filters
            objects = ApplyTypeFilters(objects);

            // 3. Category filters
            objects = ApplyCategoryFilters(objects);

            // 4. Attribute filters
            objects = ApplyAttributeFilters(objects);

            // 5. Connection depth expansion
            if (_connectionDepth > 0 && objects.Count > 0)
            {
                var seedGuids = objects.Select(o => o.InstanceGuid);
                var expandedGuids = ConnectionWalker.ExpandByDepth(_source, seedGuids, _connectionDepth);
                var lookup = _source.ToDictionary(o => o.InstanceGuid, o => o);
                objects = expandedGuids
                    .Select(g => lookup.TryGetValue(g, out var obj) ? obj : null)
                    .Where(o => o != null)
                    .ToList()!;
            }

            return objects;
        }

        #endregion

        #region Private filter application

        private List<IGH_DocumentObject> ApplyTypeFilters(List<IGH_DocumentObject> objects)
        {
            if (_typeTokens.Count == 0)
            {
                return objects;
            }

            var (includeTypes, excludeTypes) = FilterParser.ParseIncludeExclude(_typeTokens, FilterParser.TypeSynonyms);

            if (includeTypes.Count == 0 && excludeTypes.Count == 0)
            {
                return objects;
            }

            // Lazily compute topology only when needed
            TopologyClassification? topology = null;
            bool NeedTopology() =>
                includeTypes.Overlaps(TopologicalTags) || excludeTypes.Overlaps(TopologicalTags);

            if (NeedTopology())
            {
                topology = ConnectionWalker.Classify(objects);
            }

            // Apply includes
            if (includeTypes.Count > 0)
            {
                var included = new List<IGH_DocumentObject>();
                if (includeTypes.Contains("PARAMS"))
                {
                    included.AddRange(objects.OfType<IGH_Param>());
                }

                if (includeTypes.Contains("COMPONENTS"))
                {
                    included.AddRange(objects.OfType<IGH_Component>());
                }

                if (topology != null)
                {
                    AddTopologyMatches(included, objects, topology, includeTypes);
                }

                objects = included.Distinct().ToList();
            }

            // Apply excludes
            if (excludeTypes.Count > 0)
            {
                if (excludeTypes.Contains("PARAMS"))
                {
                    objects.RemoveAll(o => o is IGH_Param);
                }

                if (excludeTypes.Contains("COMPONENTS"))
                {
                    objects.RemoveAll(o => o is IGH_Component);
                }

                if (topology != null)
                {
                    RemoveTopologyMatches(objects, topology, excludeTypes);
                }
            }

            return objects;
        }

        private List<IGH_DocumentObject> ApplyCategoryFilters(List<IGH_DocumentObject> objects)
        {
            if (_categoryTokens.Count == 0)
            {
                return objects;
            }

            var (includeCats, excludeCats) = FilterParser.ParseIncludeExclude(_categoryTokens, FilterParser.CategorySynonyms);

            if (includeCats.Count == 0 && excludeCats.Count == 0)
            {
                return objects;
            }

            return objects.Where(o =>
            {
                if (o is GH_DocumentObject doc)
                {
                    return FilterParser.PassesCategoryFilter(doc.Category, doc.SubCategory, includeCats, excludeCats);
                }

                // Non-document objects pass only if there's no positive include list
                return includeCats.Count == 0;
            }).ToList();
        }

        private List<IGH_DocumentObject> ApplyAttributeFilters(List<IGH_DocumentObject> objects)
        {
            if (_attributeTokens.Count == 0)
            {
                return objects;
            }

            var (includeTags, excludeTags) = FilterParser.ParseIncludeExclude(_attributeTokens, FilterParser.AttributeSynonyms);

            // Apply includes (union semantics: objects matching ANY include tag are kept)
            List<IGH_DocumentObject> result;
            if (includeTags.Count > 0)
            {
                result = new List<IGH_DocumentObject>();
                foreach (var tag in includeTags)
                {
                    result.AddRange(objects.Where(o => MatchesAttribute(o, tag)));
                }

                result = result.Distinct().ToList();
            }
            else
            {
                result = new List<IGH_DocumentObject>(objects);
            }

            // Apply excludes
            foreach (var tag in excludeTags)
            {
                result.RemoveAll(o => MatchesAttribute(o, tag));
            }

            return result;
        }

        /// <summary>
        /// Tests whether a single object matches a canonical attribute tag.
        /// </summary>
        private static bool MatchesAttribute(IGH_DocumentObject obj, string tag)
        {
            // Active objects have RuntimeMessages; not all IGH_DocumentObject do
            var active = obj as IGH_ActiveObject;

            return tag switch
            {
                "SELECTED" => obj.Attributes?.Selected == true,
                "UNSELECTED" => obj.Attributes?.Selected == false,
                "ENABLED" => obj is IGH_ActiveObject a && !a.Locked,
                "DISABLED" => obj is IGH_ActiveObject a2 && a2.Locked,
                "ERROR" => active?.RuntimeMessages(GH_RuntimeMessageLevel.Error).Any() == true,
                "WARNING" => active?.RuntimeMessages(GH_RuntimeMessageLevel.Warning).Any() == true,
                "REMARK" => active?.RuntimeMessages(GH_RuntimeMessageLevel.Remark).Any() == true,
                "PREVIEWCAPABLE" => obj is IGH_PreviewObject p && p.IsPreviewCapable,
                "NOTPREVIEWCAPABLE" => obj is IGH_PreviewObject p2 && !p2.IsPreviewCapable,
                "PREVIEWON" => obj is IGH_PreviewObject p3 && p3.IsPreviewCapable && !p3.Hidden,
                "PREVIEWOFF" => obj is IGH_PreviewObject p4 && p4.IsPreviewCapable && p4.Hidden,
                _ => false,
            };
        }

        #endregion

        #region Topology helpers

        private static readonly HashSet<string> TopologicalTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "STARTNODES", "ENDNODES", "MIDDLENODES", "ISOLATEDNODES",
        };

        private static void AddTopologyMatches(
            List<IGH_DocumentObject> target,
            List<IGH_DocumentObject> source,
            TopologyClassification topology,
            HashSet<string> tags)
        {
            if (tags.Contains("STARTNODES"))
            {
                target.AddRange(source.Where(o => topology.StartNodes.Contains(o.InstanceGuid)));
            }

            if (tags.Contains("ENDNODES"))
            {
                target.AddRange(source.Where(o => topology.EndNodes.Contains(o.InstanceGuid)));
            }

            if (tags.Contains("MIDDLENODES"))
            {
                target.AddRange(source.Where(o => topology.MiddleNodes.Contains(o.InstanceGuid)));
            }

            if (tags.Contains("ISOLATEDNODES"))
            {
                target.AddRange(source.Where(o => topology.IsolatedNodes.Contains(o.InstanceGuid)));
            }
        }

        private static void RemoveTopologyMatches(
            List<IGH_DocumentObject> target,
            TopologyClassification topology,
            HashSet<string> tags)
        {
            if (tags.Contains("STARTNODES"))
            {
                target.RemoveAll(o => topology.StartNodes.Contains(o.InstanceGuid));
            }

            if (tags.Contains("ENDNODES"))
            {
                target.RemoveAll(o => topology.EndNodes.Contains(o.InstanceGuid));
            }

            if (tags.Contains("MIDDLENODES"))
            {
                target.RemoveAll(o => topology.MiddleNodes.Contains(o.InstanceGuid));
            }

            if (tags.Contains("ISOLATEDNODES"))
            {
                target.RemoveAll(o => topology.IsolatedNodes.Contains(o.InstanceGuid));
            }
        }

        #endregion
    }
}
