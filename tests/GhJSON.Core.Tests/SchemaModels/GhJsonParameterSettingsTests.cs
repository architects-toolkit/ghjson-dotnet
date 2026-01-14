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

using System.Collections.Generic;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonParameterSettingsTests
    {
        [Fact]
        public void CreateComponentParameterObject_ReturnsEmptyParameter()
        {
            var param = GhJson.CreateComponentParameterObject();

            Assert.NotNull(param);
            Assert.Equal(string.Empty, param.ParameterName);
        }

        [Fact]
        public void Parameter_WithName_IsValid()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";

            Assert.Equal("A", param.ParameterName);
        }

        [Fact]
        public void Parameter_SupportsNickName()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";
            param.NickName = "Input A";

            Assert.Equal("Input A", param.NickName);
        }

        [Fact]
        public void Parameter_SupportsDescription()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";
            param.Description = "First input value";

            Assert.Equal("First input value", param.Description);
        }

        [Fact]
        public void Parameter_SupportsDataMapping()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";
            param.DataMapping = "Flatten";

            Assert.Equal("Flatten", param.DataMapping);
        }

        [Fact]
        public void Parameter_SupportsExpression()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";
            param.Expression = "x * 2";

            Assert.Equal("x * 2", param.Expression);
        }

        [Fact]
        public void Parameter_SupportsAccessMode()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "points";
            param.Access = "list";

            Assert.Equal("list", param.Access);
        }

        [Fact]
        public void Parameter_SupportsTypeHint()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "x";
            param.TypeHint = "double";

            Assert.Equal("double", param.TypeHint);
        }

        [Fact]
        public void Parameter_SupportsBooleanFlags()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";
            param.IsPrincipal = true;
            param.IsRequired = false;
            param.IsReparameterized = true;
            param.IsReversed = false;
            param.IsSimplified = true;
            param.IsInverted = false;

            Assert.True(param.IsPrincipal);
            Assert.False(param.IsRequired);
            Assert.True(param.IsReparameterized);
            Assert.False(param.IsReversed);
            Assert.True(param.IsSimplified);
            Assert.False(param.IsInverted);
        }

        [Fact]
        public void Parameter_SupportsInternalizedData()
        {
            var param = GhJson.CreateComponentParameterObject();
            param.ParameterName = "A";
            param.InternalizedData = new Dictionary<string, Dictionary<string, string>>
            {
                ["{0}"] = new Dictionary<string, string>
                {
                    ["{0}(0)"] = "number:5.0"
                }
            };

            Assert.NotNull(param.InternalizedData);
            Assert.Single(param.InternalizedData);
            Assert.True(param.InternalizedData.ContainsKey("{0}"));
        }
    }
}
