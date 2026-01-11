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

using GhJSON.Core.Models.Components;
using Newtonsoft.Json;
using Xunit;

namespace GhJSON.Core.Tests.Models
{
    public class AdditionalParameterSettingsTests
    {
        [Fact]
        public void Constructor_SetsAllPropertiesToNull()
        {
            var settings = new AdditionalParameterSettings();

            Assert.Null(settings.Reverse);
            Assert.Null(settings.Simplify);
            Assert.Null(settings.IsPrincipal);
            Assert.Null(settings.Locked);
            Assert.Null(settings.Invert);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesAllProperties()
        {
            var original = new AdditionalParameterSettings
            {
                Reverse = true,
                Simplify = true,
                IsPrincipal = true,
                Locked = false,
                Invert = true
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<AdditionalParameterSettings>(json);

            Assert.NotNull(deserialized);
            Assert.True(deserialized.Reverse);
            Assert.True(deserialized.Simplify);
            Assert.True(deserialized.IsPrincipal);
            Assert.False(deserialized.Locked);
            Assert.True(deserialized.Invert);
        }

        [Fact]
        public void JsonSerialization_OmitsNullValues()
        {
            var settings = new AdditionalParameterSettings
            {
                Reverse = true
            };

            var json = JsonConvert.SerializeObject(settings);

            Assert.Contains("\"reverse\"", json);
            Assert.DoesNotContain("\"simplify\"", json);
            Assert.DoesNotContain("\"isPrincipal\"", json);
            Assert.DoesNotContain("\"locked\"", json);
            Assert.DoesNotContain("\"invert\"", json);
        }

        [Fact]
        public void JsonSerialization_FalseValuesArePreserved()
        {
            var settings = new AdditionalParameterSettings
            {
                Reverse = false,
                Simplify = false
            };

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<AdditionalParameterSettings>(json);

            Assert.NotNull(deserialized);
            Assert.False(deserialized.Reverse);
            Assert.False(deserialized.Simplify);
        }
    }
}
