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
using System.Drawing;
using GhJSON.Core.LayoutRefinements;
using GhJSON.Grasshopper.GetOperations;
using GhJSON.Grasshopper.Shared;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Adapter between live Grasshopper objects and the host-independent
    /// <see cref="LayoutNodeMetrics"/> contract consumed by the core refinement passes.
    /// This is the only place where refinement-time measurement touches Grasshopper:
    /// it resolves a layout key to an <see cref="IGH_DocumentObject"/>, reads its
    /// rendered <c>Attributes.Bounds</c>, classifies component vs. floating parameter,
    /// and converts per-parameter bounds into the port-center deltas the passes expect.
    /// </summary>
    /// <remarks>
    /// Resolution mirrors the historical measurement chain: the caller-owned object
    /// provider first (freshly instantiated or selected objects not yet on the canvas),
    /// then the explicit size provider for bounds only, then the live document.
    /// Port deltas are always read from the resolved object itself so bounds and ports
    /// can never describe different instances.
    /// </remarks>
    public static class CanvasNodeMetricsProvider
    {
        /// <summary>
        /// Creates a metrics provider over the given document (or the active canvas
        /// document when null), with the caller's object and size providers consulted
        /// first. Unknown or unmeasurable objects return null so refinement passes
        /// degrade gracefully.
        /// </summary>
        /// <param name="document">The document to measure; when null the active canvas document is used per call.</param>
        /// <param name="objectProvider">Optional caller-owned object lookup consulted before the document.</param>
        /// <param name="sizeProvider">Optional size lookup consulted between the object provider and the document.</param>
        /// <returns>A provider suitable for <see cref="LayoutRefinementOptions.NodeMetricsProvider"/>.</returns>
        public static Func<Guid, LayoutNodeMetrics?> Create(
            GH_Document? document = null,
            Func<Guid, IGH_DocumentObject?>? objectProvider = null,
            Func<Guid, SizeF?>? sizeProvider = null)
        {
            return guid =>
            {
                try
                {
                    var doc = document ?? CanvasReader.GetActiveDocument();
                    return Measure(guid, doc, objectProvider, sizeProvider);
                }
                catch
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// Resolves a node's metrics through every available source, in priority order:
        /// the caller-owned object provider, the explicit size provider (bounds only),
        /// then the live document. This is the single measurement path feeding the
        /// core refinement passes.
        /// </summary>
        internal static LayoutNodeMetrics? Measure(
            Guid id,
            GH_Document? document,
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Func<Guid, SizeF?>? sizeProvider)
        {
            try
            {
                var providerObj = ResolveProvidedObject(objectProvider, id);
                var documentObj = ResolveDocumentObject(document, id);

                // Size follows the historical priority chain: provider object bounds
                // first, then the explicit size provider, then live document bounds.
                var size = CanvasNodeSizeProvider.Measure(providerObj)
                    ?? ResolveSize(sizeProvider, id)
                    ?? CanvasNodeSizeProvider.Measure(documentObj);

                // Kind and port deltas come from the resolved object (provider before
                // document), but only when it exposes rendered attributes — an object
                // without attributes cannot vote on port positions.
                var obj = providerObj ?? documentObj;
                if (obj?.Attributes == null)
                {
                    return size == null
                        ? null
                        : new LayoutNodeMetrics(size, LayoutNodeKind.Other);
                }

                var kind = LayoutNodeKind.Other;
                IReadOnlyList<float>? inputDeltas = null;
                IReadOnlyList<float>? outputDeltas = null;

                if (obj is IGH_Component component)
                {
                    kind = LayoutNodeKind.Component;
                    var centerY = PivotSemantics.BoundsCenter(obj.Attributes.Bounds).Y;
                    inputDeltas = PortDeltas(component.Params.Input, centerY);
                    outputDeltas = PortDeltas(component.Params.Output, centerY);
                }
                else if (obj is IGH_Param)
                {
                    kind = LayoutNodeKind.Parameter;
                }

                return new LayoutNodeMetrics(size, kind, inputDeltas, outputDeltas);
            }
            catch
            {
                // Attribute access can throw for objects without a fully initialized UI.
                return null;
            }
        }

        /// <summary>
        /// Vertical offset from the node's bounds center to each port's bounds center,
        /// in the same order as the component's parameter list. Ports without rendered
        /// attributes contribute a zero delta, matching the refinement contract.
        /// </summary>
        private static IReadOnlyList<float> PortDeltas(IList<IGH_Param> parameters, float centerY)
        {
            var deltas = new float[parameters.Count];
            for (var i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                deltas[i] = param?.Attributes != null
                    ? PivotSemantics.BoundsCenter(param.Attributes.Bounds).Y - centerY
                    : 0f;
            }

            return deltas;
        }

        private static IGH_DocumentObject? ResolveProvidedObject(
            Func<Guid, IGH_DocumentObject?>? objectProvider,
            Guid id)
        {
            if (objectProvider == null)
            {
                return null;
            }

            try
            {
                return objectProvider(id);
            }
            catch
            {
                return null;
            }
        }

        private static IGH_DocumentObject? ResolveDocumentObject(GH_Document? document, Guid id)
        {
            try
            {
                return document?.FindObject(id, false);
            }
            catch
            {
                return null;
            }
        }

        private static SizeF? ResolveSize(Func<Guid, SizeF?>? sizeProvider, Guid id)
        {
            if (sizeProvider == null)
            {
                return null;
            }

            try
            {
                return sizeProvider(id);
            }
            catch
            {
                return null;
            }
        }
    }
}
