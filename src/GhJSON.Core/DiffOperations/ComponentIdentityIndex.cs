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
using GhJSON.Core.PatchModels;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Indexes a list of components by their three identity layers so they can be matched
    /// against a <see cref="GhPatchComponentMatch"/> or another component using
    /// <c>instanceGuid</c> &gt; <c>id</c> &gt; structural fingerprint.
    /// </summary>
    internal sealed class ComponentIdentityIndex
    {
        private readonly Dictionary<Guid, GhJsonComponent> byInstanceGuid = new Dictionary<Guid, GhJsonComponent>();
        private readonly Dictionary<int, GhJsonComponent> byId = new Dictionary<int, GhJsonComponent>();
        private readonly Dictionary<string, List<GhJsonComponent>> byFingerprint = new Dictionary<string, List<GhJsonComponent>>();

        public IReadOnlyList<GhJsonComponent> All { get; }

        private ComponentIdentityIndex(IReadOnlyList<GhJsonComponent> components)
        {
            this.All = components;

            foreach (var component in components)
            {
                if (component.InstanceGuid.HasValue && component.InstanceGuid != Guid.Empty)
                {
                    this.byInstanceGuid[component.InstanceGuid.Value] = component;
                }

                if (component.Id.HasValue)
                {
                    this.byId[component.Id.Value] = component;
                }

                var fingerprint = Fingerprint(component);
                if (!this.byFingerprint.TryGetValue(fingerprint, out var list))
                {
                    list = new List<GhJsonComponent>();
                    this.byFingerprint[fingerprint] = list;
                }

                list.Add(component);
            }
        }

        public static ComponentIdentityIndex Build(IEnumerable<GhJsonComponent> components)
        {
            return new ComponentIdentityIndex(components.ToList());
        }

        /// <summary>
        /// Try to match a component to one in the index.
        /// Identity precedence is <c>instanceGuid</c> &gt; <c>id</c> &gt; structural fingerprint.
        /// </summary>
        public bool TryMatch(GhJsonComponent candidate, out GhJsonComponent? match)
        {
            if (candidate.InstanceGuid.HasValue
                && candidate.InstanceGuid != Guid.Empty
                && this.byInstanceGuid.TryGetValue(candidate.InstanceGuid.Value, out var byGuid))
            {
                match = byGuid;
                return true;
            }

            if (candidate.Id.HasValue && this.byId.TryGetValue(candidate.Id.Value, out var byIdMatch))
            {
                match = byIdMatch;
                return true;
            }

            var fingerprint = Fingerprint(candidate);
            if (this.byFingerprint.TryGetValue(fingerprint, out var list) && list.Count == 1)
            {
                match = list[0];
                return true;
            }

            match = null;
            return false;
        }

        /// <summary>
        /// Try to match a <see cref="GhPatchComponentMatch"/> descriptor to a component in the index.
        /// </summary>
        public bool TryMatch(GhPatchComponentMatch descriptor, out GhJsonComponent? match, out int matchCount)
        {
            match = null;
            matchCount = 0;

            if (descriptor.InstanceGuid.HasValue && descriptor.InstanceGuid != Guid.Empty)
            {
                // Hard precedence: when an instanceGuid is supplied, only match by it.
                if (this.byInstanceGuid.TryGetValue(descriptor.InstanceGuid.Value, out var byGuid))
                {
                    match = byGuid;
                    matchCount = 1;
                    return true;
                }

                return false;
            }

            if (descriptor.Id.HasValue)
            {
                // Hard precedence: when an id is supplied (without instanceGuid), only match by id.
                if (this.byId.TryGetValue(descriptor.Id.Value, out var byIdMatch))
                {
                    match = byIdMatch;
                    matchCount = 1;
                    return true;
                }

                return false;
            }

            // Fingerprint-style lookup. The descriptor may specify any combination of
            // componentGuid + name + pivot; we filter the candidate list by these.
            var candidates = this.All.AsEnumerable();

            if (descriptor.ComponentGuid.HasValue)
            {
                candidates = candidates.Where(c => c.ComponentGuid == descriptor.ComponentGuid);
            }

            if (!string.IsNullOrEmpty(descriptor.Name))
            {
                candidates = candidates.Where(c => string.Equals(c.Name, descriptor.Name, StringComparison.Ordinal));
            }

            if (descriptor.Pivot != null)
            {
                candidates = candidates.Where(c => c.Pivot != null
                    && c.Pivot.X == descriptor.Pivot.X
                    && c.Pivot.Y == descriptor.Pivot.Y);
            }

            var list = candidates.ToList();
            matchCount = list.Count;
            if (list.Count == 1)
            {
                match = list[0];
                return true;
            }

            return false;
        }

        public bool ContainsInstanceGuid(Guid guid) => this.byInstanceGuid.ContainsKey(guid);

        public bool ContainsId(int id) => this.byId.ContainsKey(id);

        private static string Fingerprint(GhJsonComponent component)
        {
            var pivotPart = component.Pivot is null
                ? "_"
                : $"{component.Pivot.X.ToString("R", System.Globalization.CultureInfo.InvariantCulture)},{component.Pivot.Y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}";

            return $"{component.ComponentGuid}|{component.Name}|{pivotPart}";
        }
    }
}
