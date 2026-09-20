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
using GhJSON.Core.NameResolution;
using Xunit;

namespace GhJSON.Core.Tests.NameResolution
{
    /// <summary>
    /// Tests for <see cref="ComponentTypeResolver"/> type-priority scoring: obsolete and
    /// legacy implementations must lose to current ones when several components share a
    /// display name.
    /// </summary>
    public class ComponentTypeResolverTests
    {
        private sealed class PlainComponent
        {
        }

        [Obsolete("Use the new component instead")]
        private sealed class ObsoleteByAttribute
        {
        }

        private sealed class OBSOLETE_LegacyComponent
        {
        }

        // Note: fixture names must avoid OBSOLETE/DEPRECATED/LEGACY substrings, which the
        // resolver itself treats as stale markers.
        private sealed class PropertyMarkedOutdated
        {
            public bool Obsolete => true;
        }

        private sealed class PropertyMarkedCurrent
        {
            public bool Obsolete => false;
        }

        private sealed class ThrowingPropertyComponent
        {
            public bool Obsolete => throw new InvalidOperationException("no context");
        }

        [Fact]
        public void CalculateTypePriorityScore_NullType_ReturnsZero()
        {
            Assert.Equal(0, ComponentTypeResolver.CalculateTypePriorityScore((Type?)null));
        }

        [Fact]
        public void CalculateTypePriorityScore_PlainType_ReturnsZero()
        {
            Assert.Equal(0, ComponentTypeResolver.CalculateTypePriorityScore(typeof(PlainComponent)));
        }

        [Fact]
        public void CalculateTypePriorityScore_ObsoleteAttribute_IsPenalized()
        {
#pragma warning disable CS0618 // Type is intentionally obsolete: it is the test subject.
            var score = ComponentTypeResolver.CalculateTypePriorityScore(typeof(ObsoleteByAttribute));
#pragma warning restore CS0618

            Assert.True(score <= -100, $"Expected obsolete penalty, got {score}");
            Assert.True(score < ComponentTypeResolver.CalculateTypePriorityScore(typeof(PlainComponent)));
        }

        [Fact]
        public void CalculateTypePriorityScore_ObsoleteNaming_IsPenalized()
        {
            var score = ComponentTypeResolver.CalculateTypePriorityScore(typeof(OBSOLETE_LegacyComponent));

            Assert.True(score <= -100, $"Expected obsolete penalty, got {score}");
        }

        [Fact]
        public void CalculateTypePriorityScore_ObsoleteInstanceProperty_IsPenalized()
        {
            var score = ComponentTypeResolver.CalculateTypePriorityScore(typeof(PropertyMarkedOutdated));

            Assert.True(score <= -100, $"Expected obsolete penalty via Obsolete property, got {score}");
        }

        [Fact]
        public void CalculateTypePriorityScore_FalseObsoleteProperty_NotPenalized()
        {
            Assert.Equal(0, ComponentTypeResolver.CalculateTypePriorityScore(typeof(PropertyMarkedCurrent)));
        }

        [Fact]
        public void CalculateTypePriorityScore_ThrowingObsoleteProperty_TreatedAsCurrent()
        {
            // A property that throws during probing must not condemn the type.
            Assert.Equal(0, ComponentTypeResolver.CalculateTypePriorityScore(typeof(ThrowingPropertyComponent)));
        }

        [Fact]
        public void CalculateTypePriorityScore_LegacyScriptPattern_IsPenalized()
        {
            var score = ComponentTypeResolver.CalculateTypePriorityScore("GhNET_Script");

            Assert.True(score < 0, $"Expected negative pattern score, got {score}");
        }

        [Fact]
        public void CalculateTypePriorityScore_EmptyTypeName_ReturnsZero()
        {
            Assert.Equal(0, ComponentTypeResolver.CalculateTypePriorityScore(string.Empty));
        }
    }
}
