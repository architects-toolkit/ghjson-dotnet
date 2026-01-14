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
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonComponentTests
    {
        [Fact]
        public void CreateComponentObject_ReturnsEmptyComponent()
        {
            var component = GhJson.CreateComponentObject();

            Assert.NotNull(component);
            Assert.Null(component.Name);
            Assert.Null(component.Id);
        }

        [Fact]
        public void Component_WithNameAndId_IsValid()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Addition";
            component.Id = 1;

            Assert.Equal("Addition", component.Name);
            Assert.Equal(1, component.Id);
        }

        [Fact]
        public void Component_WithComponentGuid_IsValid()
        {
            var component = GhJson.CreateComponentObject();
            component.ComponentGuid = Guid.NewGuid();
            component.Id = 1;

            Assert.NotEqual(Guid.Empty, component.ComponentGuid);
            Assert.Equal(1, component.Id);
        }

        [Fact]
        public void Component_WithInstanceGuid_IsValid()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Addition";
            component.InstanceGuid = Guid.NewGuid();

            Assert.NotEqual(Guid.Empty, component.InstanceGuid);
        }

        [Fact]
        public void Component_SupportsNickName()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Addition";
            component.NickName = "Add";
            component.Id = 1;

            Assert.Equal("Add", component.NickName);
        }

        [Fact]
        public void Component_SupportsPivot()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Addition";
            component.Id = 1;
            component.Pivot = new GhJsonPivot { X = 100, Y = 200 };

            Assert.NotNull(component.Pivot);
            Assert.Equal(100, component.Pivot.X);
            Assert.Equal(200, component.Pivot.Y);
        }

        [Fact]
        public void Component_SupportsInputSettings()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Addition";
            component.Id = 1;
            
            var paramSettings = GhJson.CreateComponentParameterObject();
            paramSettings.ParameterName = "A";
            paramSettings.DataMapping = "None";
            
            component.InputSettings = new System.Collections.Generic.List<GhJsonParameterSettings> { paramSettings };

            Assert.Single(component.InputSettings);
            Assert.Equal("A", component.InputSettings[0].ParameterName);
        }

        [Fact]
        public void Component_SupportsComponentState()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Number Slider";
            component.Id = 1;
            
            var state = GhJson.CreateComponentStateObject();
            state.Locked = true;
            state.Hidden = false;
            
            component.ComponentState = state;

            Assert.NotNull(component.ComponentState);
            Assert.True(component.ComponentState.Locked);
            Assert.False(component.ComponentState.Hidden);
        }

        [Fact]
        public void Component_SupportsRuntimeMessages()
        {
            var component = GhJson.CreateComponentObject();
            component.Name = "Addition";
            component.Id = 1;
            component.Errors = new System.Collections.Generic.List<string> { "Test error" };
            component.Warnings = new System.Collections.Generic.List<string> { "Test warning" };
            component.Remarks = new System.Collections.Generic.List<string> { "Test remark" };

            Assert.Single(component.Errors);
            Assert.Single(component.Warnings);
            Assert.Single(component.Remarks);
            Assert.Equal("Test error", component.Errors[0]);
        }
    }
}
