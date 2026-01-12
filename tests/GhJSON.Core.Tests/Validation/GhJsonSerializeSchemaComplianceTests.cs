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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class GhJsonSerializeSchemaComplianceTests
    {
        [Fact]
        public void Serialize_MinimalDocument_IsCompliantWithOfficialSchema()
        {
            var doc = new GhJsonDocument
            {
                SchemaVersion = "1.0"
            };

            // Important: Official schema uses oneOf for component identity.
            // We intentionally only emit the name+id variant by not setting ComponentGuid/InstanceGuid.
            doc.Components.Add(new ComponentProperties
            {
                Name = "Addition",
                Id = 1,
                Pivot = CompactPosition.Parse("100,200")
            });

            // No connections
            doc.Connections.Clear();

            var json = GhJson.Serialize(doc);

            var ok = GhJsonValidator.Validate(json, out var message);
            Assert.True(ok, message);
        }
    }
}
