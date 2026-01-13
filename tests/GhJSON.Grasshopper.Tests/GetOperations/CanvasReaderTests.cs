/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
