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
using GhJSON.Core.Models.Document;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class GroupInfoTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var group = new GroupInfo();

            Assert.Null(group.Name);
            Assert.Equal(Guid.Empty, group.InstanceGuid);
            Assert.Null(group.Color);
            Assert.NotNull(group.Members);
            Assert.Empty(group.Members);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesAllProperties()
        {
            var guid = Guid.NewGuid();
            var original = new GroupInfo
            {
                Name = "Test Group",
                InstanceGuid = guid,
                Color = "255,0,200,0",
                Members = new List<int> { 1, 2, 3 }
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<GroupInfo>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("Test Group", deserialized.Name);
            Assert.Equal(guid, deserialized.InstanceGuid);
            Assert.Equal("255,0,200,0", deserialized.Color);
            Assert.NotNull(deserialized.Members);
            Assert.Equal(3, deserialized.Members.Count);
            Assert.Equal(1, deserialized.Members[0]);
            Assert.Equal(2, deserialized.Members[1]);
            Assert.Equal(3, deserialized.Members[2]);
        }

        [Fact]
        public void JsonSerialization_OmitsNullValues()
        {
            var group = new GroupInfo
            {
                InstanceGuid = Guid.NewGuid()
            };

            var json = JsonConvert.SerializeObject(group);

            Assert.Contains("\"instanceGuid\"", json);
            Assert.Contains("\"members\"", json); // Members is not null, it's an empty list
            Assert.DoesNotContain("\"name\"", json);
            Assert.DoesNotContain("\"color\"", json);
        }
    }
}
