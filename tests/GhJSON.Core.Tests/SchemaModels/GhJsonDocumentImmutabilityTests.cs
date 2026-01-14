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
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.SchemaModels
{
    public class GhJsonDocumentImmutabilityTests
    {
        [Fact]
        public void Components_AreReadOnly()
        {
            var doc = GhJson.CreateDocumentBuilder().Build();

            var list = Assert.IsAssignableFrom<IList<GhJsonComponent>>(doc.Components);
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(new GhJsonComponent { Name = "X", Id = 1 }));
        }

        [Fact]
        public void Constructor_MakesDefensiveCopyOfComponents()
        {
            var source = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1 },
            };

            var doc = new GhJsonDocument(
                schema: GhJson.CurrentVersion,
                metadata: null,
                components: source,
                connections: null,
                groups: null);

            source.Add(new GhJsonComponent { Name = "B", Id = 2 });

            Assert.Single(doc.Components);
            Assert.Equal("A", doc.Components[0].Name);
        }

        [Fact]
        public void Builder_DoesNotHoldReferenceToInputCollections()
        {
            var components = new List<GhJsonComponent>
            {
                new GhJsonComponent { Name = "A", Id = 1 },
            };

            var builder = GhJson.CreateDocumentBuilder().AddComponents(components);
            components.Add(new GhJsonComponent { Name = "B", Id = 2 });

            var doc = builder.Build();

            Assert.Single(doc.Components);
            Assert.Equal("A", doc.Components[0].Name);
        }

        [Fact]
        public void JsonRoundTrip_PreservesReadOnlyCollections()
        {
            var doc = GhJson.CreateDocumentBuilder()
                .AddComponent(new GhJsonComponent { Name = "A", Id = 1 })
                .Build();

            var json = GhJson.ToJson(doc);
            var loaded = GhJson.FromJson(json);

            var list = Assert.IsAssignableFrom<IList<GhJsonComponent>>(loaded.Components);
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(new GhJsonComponent { Name = "X", Id = 2 }));
        }
    }
}
