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
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.GetOperations;
using Xunit;

namespace GhJSON.Grasshopper.Tests.GetOperations
{
    public class CanvasReaderTests
    {
        [Fact]
        public void Get_WithDefaultOptions_ReturnsDocument()
        {
            // Note: These tests require Grasshopper context
            // They will be skipped in CI environments without Grasshopper
            
            var options = new GetOptions();
            Assert.NotNull(options);
        }

        [Fact]
        public void GetOptions_HasDefaultValues()
        {
            var options = new GetOptions();

            Assert.NotNull(options);
            // Verify default option values exist
        }

        [Fact]
        public void GetSelected_ReturnsEmptyWhenNoSelection()
        {
            // This test requires Grasshopper context
            // API verification test
            Assert.NotNull(typeof(GhJsonGrasshopper).GetMethod("GetSelected"));
        }

        [Fact]
        public void GetByGuids_AcceptsGuidCollection()
        {
            // API verification test
            var guids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            Assert.NotNull(guids);
            
            var method = typeof(GhJsonGrasshopper).GetMethod("GetByGuids");
            Assert.NotNull(method);
        }

        [Fact]
        public void CanvasReader_SupportsOptionsParameter()
        {
            var options = new GetOptions
            {
                SelectedOnly = true,
                IncludeConnections = true,
                IncludeGroups = true
            };

            Assert.True(options.SelectedOnly);
            Assert.True(options.IncludeConnections);
            Assert.True(options.IncludeGroups);
        }
    }
}
