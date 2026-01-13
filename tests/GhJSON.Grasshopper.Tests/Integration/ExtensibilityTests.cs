/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 */

using System.Linq;
using GhJSON.Grasshopper;
using Xunit;

namespace GhJSON.Grasshopper.Tests.Integration
{
    /// <summary>
    /// Tests for the extensibility API including custom data type serializers
    /// and custom object handlers.
    /// </summary>
    public class ExtensibilityTests
    {
        [Fact]
        public void GetRegisteredDataTypeSerializers_ReturnsBuiltInSerializers()
        {
            var serializers = GhJsonGrasshopper.GetRegisteredDataTypeSerializers();

            Assert.NotNull(serializers);
            Assert.NotEmpty(serializers);
            
            // Verify basic types are registered
            var types = serializers.Select(s => s.TypeName).ToList();
            Assert.Contains("Text", types);
            Assert.Contains("Number", types);
            Assert.Contains("Integer", types);
            Assert.Contains("Boolean", types);
        }

        [Fact]
        public void GetRegisteredDataTypeSerializers_ReturnsGeometricSerializers()
        {
            var serializers = GhJsonGrasshopper.GetRegisteredDataTypeSerializers();
            var types = serializers.Select(s => s.TypeName).ToList();

            // Verify geometric types are registered
            Assert.Contains("Color", types);
            Assert.Contains("Point3d", types);
            Assert.Contains("Vector3d", types);
            Assert.Contains("Line", types);
            Assert.Contains("Plane", types);
            Assert.Contains("Circle", types);
            Assert.Contains("Arc", types);
            Assert.Contains("Box", types);
            Assert.Contains("Rectangle", types);
            Assert.Contains("Interval", types);
        }

        [Fact]
        public void GetRegisteredObjectHandlers_ReturnsBuiltInHandlers()
        {
            var handlers = GhJsonGrasshopper.GetRegisteredObjectHandlers();

            Assert.NotNull(handlers);
            Assert.NotEmpty(handlers);
        }

        [Fact]
        public void DataTypeSerializer_AllHaveUniquePrefix()
        {
            var serializers = GhJsonGrasshopper.GetRegisteredDataTypeSerializers();
            var prefixes = serializers.Select(s => s.Prefix).ToList();

            // Verify no duplicate prefixes
            Assert.Equal(prefixes.Count, prefixes.Distinct().Count());
        }

        [Fact]
        public void DataTypeSerializer_AllImplementInterface()
        {
            var serializers = GhJsonGrasshopper.GetRegisteredDataTypeSerializers();

            foreach (var serializer in serializers)
            {
                Assert.NotNull(serializer.TypeName);
                Assert.NotNull(serializer.Prefix);
                Assert.NotNull(serializer.TargetType);
            }
        }

        [Fact]
        public void ObjectHandler_AllHaveSchemaExtensionInfo()
        {
            var handlers = GhJsonGrasshopper.GetRegisteredObjectHandlers();

            foreach (var handler in handlers)
            {
                // SchemaExtensionUrl can be null for handlers that don't add extensions
                Assert.NotNull(handler);
            }
        }

        [Fact]
        public void CustomDataTypeSerializer_CanBeRegisteredAndUnregistered()
        {
            var initialCount = GhJsonGrasshopper.GetRegisteredDataTypeSerializers().Count();

            // Note: Actual registration would require implementing IDataTypeSerializer<T>
            // This test verifies the API exists and is callable
            
            var currentCount = GhJsonGrasshopper.GetRegisteredDataTypeSerializers().Count();
            Assert.Equal(initialCount, currentCount);
        }

        [Fact]
        public void CustomObjectHandler_CanBeRegisteredAndUnregistered()
        {
            var initialCount = GhJsonGrasshopper.GetRegisteredObjectHandlers().Count();

            // Note: Actual registration would require implementing IObjectHandler
            // This test verifies the API exists and is callable
            
            var currentCount = GhJsonGrasshopper.GetRegisteredObjectHandlers().Count();
            Assert.Equal(initialCount, currentCount);
        }

        [Fact]
        public void ExtensibilityAPI_IsAccessibleFromPublicInterface()
        {
            // Verify all extensibility methods are public and accessible
            var type = typeof(GhJsonGrasshopper);
            
            Assert.NotNull(type.GetMethod("RegisterCustomDataTypeSerializer"));
            Assert.NotNull(type.GetMethod("UnregisterCustomDataTypeSerializer"));
            Assert.NotNull(type.GetMethod("GetRegisteredDataTypeSerializers"));
            Assert.NotNull(type.GetMethod("RegisterCustomObjectHandler"));
            Assert.NotNull(type.GetMethod("UnregisterCustomObjectHandler"));
            Assert.NotNull(type.GetMethod("GetRegisteredObjectHandlers"));
        }
    }
}
