/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
