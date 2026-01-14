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

using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonConnectionTests
    {
        [Fact]
        public void CreateConnectionObject_ReturnsEmptyConnection()
        {
            var connection = GhJson.CreateConnectionObject();

            Assert.NotNull(connection);
            Assert.NotNull(connection.From);
            Assert.NotNull(connection.To);
        }

        [Fact]
        public void Connection_WithFromAndTo_IsValid()
        {
            var connection = GhJson.CreateConnectionObject();
            
            connection.From = new GhJsonConnectionEndpoint
            {
                Id = 1,
                ParamName = "Result"
            };
            
            connection.To = new GhJsonConnectionEndpoint
            {
                Id = 2,
                ParamName = "A"
            };

            Assert.NotNull(connection.From);
            Assert.NotNull(connection.To);
            Assert.Equal(1, connection.From.Id);
            Assert.Equal(2, connection.To.Id);
        }

        [Fact]
        public void ConnectionEndpoint_SupportsParamName()
        {
            var endpoint = GhJson.CreateConnectionEndpointObject();
            endpoint.Id = 1;
            endpoint.ParamName = "Result";

            Assert.Equal(1, endpoint.Id);
            Assert.Equal("Result", endpoint.ParamName);
            Assert.Null(endpoint.ParamIndex);
        }

        [Fact]
        public void ConnectionEndpoint_SupportsParamIndex()
        {
            var endpoint = GhJson.CreateConnectionEndpointObject();
            endpoint.Id = 1;
            endpoint.ParamIndex = 0;

            Assert.Equal(1, endpoint.Id);
            Assert.Equal(0, endpoint.ParamIndex);
            Assert.Null(endpoint.ParamName);
        }

        [Fact]
        public void ConnectionEndpoint_CanHaveBothNameAndIndex()
        {
            var endpoint = GhJson.CreateConnectionEndpointObject();
            endpoint.Id = 1;
            endpoint.ParamName = "A";
            endpoint.ParamIndex = 0;

            Assert.Equal("A", endpoint.ParamName);
            Assert.Equal(0, endpoint.ParamIndex);
        }
    }
}
