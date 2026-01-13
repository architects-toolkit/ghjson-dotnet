/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 */

using System;
using System.Collections.Generic;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonGroupTests
    {
        [Fact]
        public void CreateGroupObject_ReturnsEmptyGroup()
        {
            var group = GhJson.CreateGroupObject();

            Assert.NotNull(group);
            Assert.Null(group.Name);
            Assert.NotNull(group.Members);
            Assert.Empty(group.Members);
        }

        [Fact]
        public void Group_WithIdAndMembers_IsValid()
        {
            var group = GhJson.CreateGroupObject();
            group.Id = 1;
            group.Members = new List<int> { 10, 20, 30 };

            Assert.Equal(1, group.Id);
            Assert.Equal(3, group.Members.Count);
            Assert.Contains(10, group.Members);
        }

        [Fact]
        public void Group_WithInstanceGuid_IsValid()
        {
            var group = GhJson.CreateGroupObject();
            group.InstanceGuid = Guid.NewGuid();
            group.Members = new List<int> { 1, 2 };

            Assert.NotEqual(Guid.Empty, group.InstanceGuid);
        }

        [Fact]
        public void Group_SupportsName()
        {
            var group = GhJson.CreateGroupObject();
            group.Id = 1;
            group.Name = "My Group";
            group.Members = new List<int> { 1 };

            Assert.Equal("My Group", group.Name);
        }

        [Fact]
        public void Group_SupportsColor()
        {
            var group = GhJson.CreateGroupObject();
            group.Id = 1;
            group.Color = "argb:255,128,64,32";
            group.Members = new List<int> { 1 };

            Assert.Equal("argb:255,128,64,32", group.Color);
        }

        [Fact]
        public void Group_CanBeEmpty()
        {
            var group = GhJson.CreateGroupObject();
            group.Id = 1;
            group.Members = new List<int>();

            Assert.NotNull(group.Members);
            Assert.Empty(group.Members);
        }
    }
}
