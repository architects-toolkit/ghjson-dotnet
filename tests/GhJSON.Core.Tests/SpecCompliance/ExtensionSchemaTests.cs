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

using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.SpecCompliance
{
    /// <summary>
    /// Validates that known extension schemas embedded in <c>componentState.extensions</c>
    /// are enforced (correct shape passes, unexpected fields/types fail) and that
    /// unknown extension keys are accepted (forward-compatibility).
    /// </summary>
    public class ExtensionSchemaTests
    {
        private static string Wrap(string extensionsJson) =>
            @"{""schema"":""1.0"",""components"":[{""name"":""X"",""id"":1,""componentState"":{""extensions"":" +
            extensionsJson + "}}]}";

        [Fact]
        public void NumberSlider_MinimalValid_Passes()
        {
            var json = Wrap(@"{""gh.numberslider"":{""value"":""5<0~10>""}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.True(result.IsValid, string.Join(";", result.Errors));
        }

        [Fact]
        public void Panel_MinimalValid_Passes()
        {
            var json = Wrap(@"{""gh.panel"":{""text"":""hi"",""multiline"":false,""wrap"":false}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.True(result.IsValid, string.Join(";", result.Errors));
        }

        [Fact]
        public void Panel_UnknownField_Fails()
        {
            var json = Wrap(@"{""gh.panel"":{""text"":""hi"",""bogus"":1}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Panel_BoundsAsString_Passes()
        {
            var json = Wrap(@"{""gh.panel"":{""text"":""hi"",""multiline"":false,""wrap"":false,""bounds"":""50x40""}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.True(result.IsValid, string.Join(";", result.Errors));
        }

        [Fact]
        public void Panel_BoundsAsArray_Fails()
        {
            var json = Wrap(@"{""gh.panel"":{""text"":""hi"",""multiline"":false,""wrap"":false,""bounds"":[50,40]}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Panel_BoundsInvalidFormat_Fails()
        {
            var json = Wrap(@"{""gh.panel"":{""text"":""hi"",""multiline"":false,""wrap"":false,""bounds"":""invalid""}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void UnknownExtensionKey_IsAccepted()
        {
            // additionalProperties: { type: object } in extensions registry.
            var json = Wrap(@"{""gh.future_key"":{""anything"":""goes""}}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.True(result.IsValid, string.Join(";", result.Errors));
        }

        [Fact]
        public void UnknownExtensionKey_WithNonObjectValue_Fails()
        {
            // Registry's additionalProperties requires object values.
            var json = Wrap(@"{""gh.future_key"":""not an object""}");

            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.False(result.IsValid);
        }
    }
}
