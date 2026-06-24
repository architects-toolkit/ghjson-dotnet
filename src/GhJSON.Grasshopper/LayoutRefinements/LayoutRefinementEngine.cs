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
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    public static class LayoutRefinementEngine
    {
        public static Dictionary<Guid, PointF> ApplyRefinements(
            LayoutResult layoutResult,
            GhJsonDocument document,
            LayoutRefinementOptions? options = null)
        {
            if (layoutResult == null)
            {
                throw new ArgumentNullException(nameof(layoutResult));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            options ??= LayoutRefinementOptions.Default;

            var positions = new Dictionary<Guid, PointF>();
            foreach (var kvp in layoutResult.Positions)
            {
                positions[kvp.Key] = new PointF((float)kvp.Value.X, (float)kvp.Value.Y);
            }

            if (options.ApplyBoundsAwareSpacing)
            {
                positions = BoundsAwareSpacing.ApplyBoundsAwareSpacing(
                    positions,
                    options.SpacingX,
                    options.SpacingY);
            }

            // Single, coherent port-alignment pass replaces the former trio of competing
            // passes (param-to-port, one-to-one, connection-length minimization) that used to
            // overwrite each other's Y. Any of the legacy flags enables it.
            if (options.AlignParamsToInputPorts ||
                options.AlignOneToOneConnections ||
                options.MinimizeConnectionLengths)
            {
                positions = PortAlignment.AlignToPorts(positions, document);
            }

            // Always the final pass: guarantees no two components in a column overlap.
            if (options.AvoidCollisions)
            {
                positions = CollisionResolver.AvoidCollisions(positions);
            }

            return positions;
        }
    }
}
