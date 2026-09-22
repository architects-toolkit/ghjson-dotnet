/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using GhJSON.Core.LayoutRefinements;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.LayoutRefinements
{
    /// <summary>
    /// Tests for <see cref="ConnectionKeyMap"/>: connection endpoint ids resolve to the
    /// same stable layout keys the layout engine emits, including id-only components.
    /// </summary>
    public class ConnectionKeyMapTests
    {
        [Fact]
        public void Build_MapsIdsToLayoutKeys()
        {
            var guid = Guid.NewGuid();
            var withGuid = new GhJsonComponent { Name = "A", Id = 1, InstanceGuid = guid };
            var idOnly = new GhJsonComponent { Name = "B", Id = 2 };
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(withGuid)
                .AddComponent(idOnly)
                .Build();

            var map = ConnectionKeyMap.Build(doc);

            Assert.Equal(2, map.Count);
            Assert.Equal(guid, map[1]);
            Assert.Equal(GhJson.GetLayoutKey(idOnly), map[2]);
        }

        [Fact]
        public void Build_SkipsComponentsWithoutId()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", InstanceGuid = Guid.NewGuid() })
                .Build();

            Assert.Empty(ConnectionKeyMap.Build(doc));
        }
    }
}
