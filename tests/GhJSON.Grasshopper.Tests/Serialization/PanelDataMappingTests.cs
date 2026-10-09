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

using System.Collections.Generic;
using GhJSON.Grasshopper.Serialization;
using GhJSON.Grasshopper.Serialization.ObjectHandlers;
using Xunit;

namespace GhJSON.Grasshopper.Tests.Serialization
{
    /// <summary>
    /// Tests for the panel data mapping in <see cref="InternalizedDataHandler"/>.
    /// A <c>GH_Panel</c> cannot be constructed without a Grasshopper runtime, so
    /// these cover the Grasshopper-free flattening logic that maps
    /// <c>internalizedData</c>/<c>runtimeData</c> trees onto panel text lines.
    /// </summary>
    public class PanelDataMappingTests
    {
        [Fact]
        public void CollectTextItems_OrdersItemsByIndexWithinPath()
        {
            var tree = new Dictionary<string, Dictionary<string, string>>
            {
                ["{0}"] = new Dictionary<string, string>
                {
                    ["{0}(2)"] = "text:blue",
                    ["{0}(0)"] = "text:red",
                    ["{0}(1)"] = "text:green",
                },
            };

            var items = InternalizedDataHandler.CollectTextItems(tree);

            Assert.Equal(new[] { "red", "green", "blue" }, items);
        }

        [Fact]
        public void CollectTextItems_OrdersPathsLexicographicallyByIndex()
        {
            var tree = new Dictionary<string, Dictionary<string, string>>
            {
                ["{10}"] = new Dictionary<string, string> { ["{10}(0)"] = "text:last" },
                ["{2}"] = new Dictionary<string, string> { ["{2}(0)"] = "text:first" },
            };

            var items = InternalizedDataHandler.CollectTextItems(tree);

            // Numeric ordering, not ordinal string ordering ("{10}" > "{2}").
            Assert.Equal(new[] { "first", "last" }, items);
        }

        [Fact]
        public void CollectTextItems_FlattensMultipleBranchesInPathOrder()
        {
            var tree = new Dictionary<string, Dictionary<string, string>>
            {
                ["{0;1}"] = new Dictionary<string, string> { ["{0;1}(0)"] = "text:c" },
                ["{0;0}"] = new Dictionary<string, string>
                {
                    ["{0;0}(1)"] = "text:b",
                    ["{0;0}(0)"] = "text:a",
                },
            };

            var items = InternalizedDataHandler.CollectTextItems(tree);

            Assert.Equal(new[] { "a", "b", "c" }, items);
        }

        [Fact]
        public void CollectTextItems_FallsBackToRawStringForUnknownPrefixes()
        {
            var tree = new Dictionary<string, Dictionary<string, string>>
            {
                ["{0}"] = new Dictionary<string, string>
                {
                    ["{0}(0)"] = "plain value",
                    ["{0}(1)"] = "number:2.5",
                },
            };

            var items = InternalizedDataHandler.CollectTextItems(tree);

            Assert.Equal(new[] { "plain value", "2.5" }, items);
        }

        [Fact]
        public void CollectTextItems_ReturnsNullForEmptyTree()
        {
            Assert.Null(InternalizedDataHandler.CollectTextItems(null));
            Assert.Null(InternalizedDataHandler.CollectTextItems(new Dictionary<string, Dictionary<string, string>>()));
        }
    }
}
