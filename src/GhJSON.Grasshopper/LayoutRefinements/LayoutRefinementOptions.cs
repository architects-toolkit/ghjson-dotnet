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

namespace GhJSON.Grasshopper.LayoutRefinements
{
    public sealed class LayoutRefinementOptions
    {
        public bool ApplyBoundsAwareSpacing { get; set; } = true;

        public bool AlignParamsToInputPorts { get; set; } = true;

        public bool AlignOneToOneConnections { get; set; } = true;

        public bool MinimizeConnectionLengths { get; set; } = true;

        public bool AvoidCollisions { get; set; } = true;

        public float SpacingX { get; set; } = 200f;

        public float SpacingY { get; set; } = 100f;

        public static LayoutRefinementOptions Default => new LayoutRefinementOptions();

        public static LayoutRefinementOptions None => new LayoutRefinementOptions
        {
            ApplyBoundsAwareSpacing = false,
            AlignParamsToInputPorts = false,
            AlignOneToOneConnections = false,
            MinimizeConnectionLengths = false,
            AvoidCollisions = false,
        };
    }
}
