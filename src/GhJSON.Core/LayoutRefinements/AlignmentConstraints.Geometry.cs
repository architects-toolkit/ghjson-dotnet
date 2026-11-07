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
using System.Linq;

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>Measured obstacle and corridor constraints for rigid alignment groups.</summary>
    internal sealed partial class AlignmentConstraints
    {
        /// <summary>Checks only newly introduced internal node and wire interactions.</summary>
        private bool InternallyClear(Group merged, Group left, Group right)
        {
            foreach (var a in left.Offsets.Keys)
            {
                foreach (var b in right.Offsets.Keys)
                {
                    if (this.BodyConflict(a, this.Bounds(a, merged.Origin + merged.Offsets[a]),
                            b, this.Bounds(b, merged.Origin + merged.Offsets[b])))
                    {
                        return false;
                    }
                }
            }

            var existing = new HashSet<Edge>(left.Edges.Concat(right.Edges));
            return this.EdgesClear(left.Edges, right.Offsets.Keys, merged) &&
                   this.EdgesClear(right.Edges, left.Offsets.Keys, merged) &&
                   this.EdgesClear(merged.Edges.Where(edge => !existing.Contains(edge)), merged.Offsets.Keys, merged);
        }

        /// <summary>Checks that accepted horizontal wires clear unrelated member bounds.</summary>
        private bool EdgesClear(IEnumerable<Edge> edges, IEnumerable<Guid> nodes, Group group)
        {
            if (!this.options.EnsureWireClearance)
            {
                return true;
            }

            foreach (var edge in edges)
            {
                var wire = this.Wire(edge, group, group.Origin);
                foreach (var id in nodes)
                {
                    if (id != edge.From && id != edge.To &&
                        this.WireBlocked(wire, this.Bounds(id, group.Origin + group.Offsets[id])))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Tests whether the second pinned group forbids the candidate origin.</summary>
        private bool Conflicts(Group candidate, Group obstacle)
        {
            return this.ForbiddenOrigins(candidate, new[] { obstacle })
                .Any(interval => candidate.Origin > interval.Top && candidate.Origin < interval.Bottom);
        }

        /// <summary>Converts body and horizontal-corridor obstacles into forbidden translations.</summary>
        private List<(float Top, float Bottom)> ForbiddenOrigins(Group group, IEnumerable<Group> obstacles)
        {
            var intervals = new List<(float Top, float Bottom)>();
            foreach (var obstacle in obstacles)
            {
                foreach (var a in group.Offsets.Keys)
                {
                    var moving = this.Bounds(a, group.Offsets[a]);
                    foreach (var b in obstacle.Offsets.Keys)
                    {
                        var fixedBounds = this.Bounds(b, obstacle.Origin + obstacle.Offsets[b]);
                        if (this.options.AvoidCollisions && HasArea(moving) && HasArea(fixedBounds) &&
                            !this.BothAuthored(a, b) &&
                            HorizontalOverlap(moving, fixedBounds, Math.Max(0f, this.options.SpacingX - 1f)))
                        {
                            intervals.Add((fixedBounds.Top - moving.Bottom - this.options.SpacingY,
                                fixedBounds.Bottom - moving.Top + this.options.SpacingY));
                        }
                    }
                }

                if (!this.options.EnsureWireClearance)
                {
                    continue;
                }

                foreach (var edge in group.Edges)
                {
                    var wire = this.Wire(edge, group, 0f);
                    foreach (var id in obstacle.Offsets.Keys)
                    {
                        var bounds = this.Bounds(id, obstacle.Origin + obstacle.Offsets[id]);
                        if (HasArea(bounds) && HorizontalOverlap(wire, bounds, 0f))
                        {
                            intervals.Add((bounds.Top - this.options.WireClearance - wire.Y,
                                bounds.Bottom + this.options.WireClearance - wire.Y));
                        }
                    }
                }

                foreach (var edge in obstacle.Edges)
                {
                    var wire = this.Wire(edge, obstacle, obstacle.Origin);
                    foreach (var id in group.Offsets.Keys)
                    {
                        var bounds = this.Bounds(id, group.Offsets[id]);
                        if (HasArea(bounds) && HorizontalOverlap(wire, bounds, 0f))
                        {
                            intervals.Add((wire.Y - this.options.WireClearance - bounds.Bottom,
                                wire.Y + this.options.WireClearance - bounds.Top));
                        }
                    }
                }
            }

            return intervals;
        }

        /// <summary>Chooses the nearest boundary of the merged open forbidden intervals.</summary>
        private static float NearestFree(float preferred, List<(float Top, float Bottom)> intervals)
        {
            var merged = new List<(float Top, float Bottom)>();
            foreach (var interval in intervals.OrderBy(value => value.Top).ThenBy(value => value.Bottom))
            {
                if (merged.Count > 0 && interval.Top < merged[merged.Count - 1].Bottom)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = (last.Top, Math.Max(last.Bottom, interval.Bottom));
                }
                else
                {
                    merged.Add(interval);
                }
            }

            foreach (var interval in merged)
            {
                if (preferred > interval.Top && preferred < interval.Bottom)
                {
                    return preferred - interval.Top <= interval.Bottom - preferred ? interval.Top : interval.Bottom;
                }
            }

            return preferred;
        }

        /// <summary>Tests measured body separation without attempting to repair authored overlaps.</summary>
        private bool BodyConflict(Guid a, RectangleF first, Guid b, RectangleF second)
        {
            return this.options.AvoidCollisions && HasArea(first) && HasArea(second) && !this.BothAuthored(a, b) &&
                   HorizontalOverlap(first, second, Math.Max(0f, this.options.SpacingX - 1f)) &&
                   first.Top < second.Bottom + this.options.SpacingY &&
                   second.Top < first.Bottom + this.options.SpacingY;
        }

        /// <summary>Identifies pairs whose positions are both authoritative.</summary>
        private bool BothAuthored(Guid a, Guid b)
        {
            return this.options.FixedPositions?.ContainsKey(a) == true &&
                   this.options.FixedPositions.ContainsKey(b);
        }

        /// <summary>Returns measured bounds; only hard anchors retain conservative fallback obstacles.</summary>
        private RectangleF Bounds(Guid id, float y)
        {
            var size = this.HasMeasuredBounds(id) ? this.metrics[id]!.Size!.Value
                : this.options.FixedPositions?.ContainsKey(id) == true ? new SizeF(100f, 60f) : SizeF.Empty;
            return new RectangleF(this.positions[id].X - size.Width / 2f, y - size.Height / 2f, size.Width, size.Height);
        }

        /// <summary>Rejects unavailable or non-finite measurements instead of inventing movable bounds.</summary>
        private bool HasMeasuredBounds(Guid id)
        {
            var size = this.metrics[id]?.Size;
            return size.HasValue && size.Value.Width > 0f && size.Value.Height > 0f &&
                   !float.IsInfinity(size.Value.Width) && !float.IsInfinity(size.Value.Height);
        }

        /// <summary>Distinguishes real body rectangles from unmeasurable placeholders.</summary>
        private static bool HasArea(RectangleF bounds) => bounds.Width > 0f && bounds.Height > 0f;

        /// <summary>Returns a horizontal occupied-port segment at the requested group origin.</summary>
        private RectangleF Wire(Edge edge, Group group, float origin)
        {
            var source = this.Bounds(edge.From, 0f);
            var target = this.Bounds(edge.To, 0f);
            return new RectangleF(source.Right, origin + group.Offsets[edge.From] + edge.FromDelta,
                Math.Max(0f, target.Left - source.Right), 0f);
        }

        /// <summary>Tests a horizontal wire against a measured body and the configured corridor.</summary>
        private bool WireBlocked(RectangleF wire, RectangleF bounds)
        {
            return HasArea(bounds) && HorizontalOverlap(wire, bounds, 0f) &&
                   wire.Y > bounds.Top - this.options.WireClearance &&
                   wire.Y < bounds.Bottom + this.options.WireClearance;
        }

        /// <summary>Tests strict horizontal overlap with an optional edge gap.</summary>
        private static bool HorizontalOverlap(RectangleF first, RectangleF second, float gap)
        {
            return first.Left < second.Right + gap && second.Left < first.Right + gap;
        }
    }
}
