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
using GhJSON.Core.Models.Connections;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class ConnectionTests
    {
        [Fact]
        public void Connection_IsValid_ReturnsTrueForValidConnection()
        {
            var conn = new Connection { Id = 1, ParamName = "R" };
            
            Assert.True(conn.IsValid());
        }

        [Fact]
        public void Connection_IsValid_ReturnsFalseForInvalidId()
        {
            var conn = new Connection { Id = 0, ParamName = "R" };
            
            Assert.False(conn.IsValid());
        }

        [Fact]
        public void Connection_IsValid_ReturnsFalseForEmptyParamName()
        {
            var conn = new Connection { Id = 1, ParamName = "" };
            
            Assert.False(conn.IsValid());
        }

        [Fact]
        public void ConnectionPairing_IsValid_ReturnsTrueForValidPairing()
        {
            var pairing = new ConnectionPairing
            {
                From = new Connection { Id = 1, ParamName = "R" },
                To = new Connection { Id = 2, ParamName = "A" }
            };
            
            Assert.True(pairing.IsValid());
        }

        [Fact]
        public void ConnectionPairing_IsValid_ReturnsFalseForInvalidEndpoint()
        {
            var pairing = new ConnectionPairing
            {
                From = new Connection { Id = 0, ParamName = "R" },
                To = new Connection { Id = 2, ParamName = "A" }
            };
            
            Assert.False(pairing.IsValid());
        }

        [Fact]
        public void ConnectionPairing_TryResolveGuids_ResolvesCorrectly()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var mapping = new Dictionary<int, Guid>
            {
                { 1, guid1 },
                { 2, guid2 }
            };
            
            var pairing = new ConnectionPairing
            {
                From = new Connection { Id = 1, ParamName = "R" },
                To = new Connection { Id = 2, ParamName = "A" }
            };
            
            var result = pairing.TryResolveGuids(mapping, out var fromGuid, out var toGuid);
            
            Assert.True(result);
            Assert.Equal(guid1, fromGuid);
            Assert.Equal(guid2, toGuid);
        }

        [Fact]
        public void ConnectionPairing_TryResolveGuids_ReturnsFalseForMissingId()
        {
            var mapping = new Dictionary<int, Guid>
            {
                { 1, Guid.NewGuid() }
            };
            
            var pairing = new ConnectionPairing
            {
                From = new Connection { Id = 1, ParamName = "R" },
                To = new Connection { Id = 2, ParamName = "A" }
            };
            
            var result = pairing.TryResolveGuids(mapping, out _, out _);
            
            Assert.False(result);
        }

        [Fact]
        public void ConnectionPairing_JsonSerialization_RoundTrip()
        {
            var pairing = new ConnectionPairing
            {
                From = new Connection { Id = 1, ParamName = "Result", ParamIndex = 0 },
                To = new Connection { Id = 2, ParamName = "A", ParamIndex = 0 }
            };
            
            var json = JsonConvert.SerializeObject(pairing);
            var deserialized = JsonConvert.DeserializeObject<ConnectionPairing>(json);
            
            Assert.NotNull(deserialized);
            Assert.Equal(1, deserialized.From.Id);
            Assert.Equal("Result", deserialized.From.ParamName);
            Assert.Equal(2, deserialized.To.Id);
            Assert.Equal("A", deserialized.To.ParamName);
        }
    }
}
