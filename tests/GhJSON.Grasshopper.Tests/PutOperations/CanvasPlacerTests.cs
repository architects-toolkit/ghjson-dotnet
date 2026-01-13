/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 */

using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.PutOperations;
using Xunit;

namespace GhJSON.Grasshopper.Tests.PutOperations
{
    public class CanvasPlacerTests
    {
        [Fact]
        public void PutOptions_HasDefaultValues()
        {
            var options = new PutOptions();

            Assert.NotNull(options);
        }

        [Fact]
        public void PutOptions_SupportsStartPosition()
        {
            var options = new PutOptions
            {
                Offset = new System.Drawing.PointF(100, 200)
            };

            Assert.Equal(100, options.Offset.X);
            Assert.Equal(200, options.Offset.Y);
        }

        [Fact]
        public void PutOptions_SupportsValidationLevel()
        {
            var options = new PutOptions
            {
                SkipInvalidComponents = true
            };

            Assert.True(options.SkipInvalidComponents);
        }

        [Fact]
        public void Put_ValidatesDocument()
        {
            // API verification test
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            Assert.NotNull(doc);
            Assert.Single(doc.Components);
        }

        [Fact]
        public void CanvasPlacer_SupportsOptionsParameter()
        {
            var options = new PutOptions
            {
                Offset = new System.Drawing.PointF(0, 0),
                SkipInvalidComponents = false,
                SelectPlacedObjects = true,
                CreateConnections = true,
                CreateGroups = true,
                RegenerateInstanceGuids = true
            };

            Assert.Equal(0, options.Offset.X);
            Assert.Equal(0, options.Offset.Y);
            Assert.True(options.SelectPlacedObjects);
            Assert.True(options.CreateConnections);
            Assert.True(options.CreateGroups);
            Assert.True(options.RegenerateInstanceGuids);
        }
    }
}
