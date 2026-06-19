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

namespace GhJSON.Core.Tests._Support
{
    /// <summary>
    /// Canonical JSON snippets used across tests. Kept compact (no indentation) and
    /// deliberately schema-conformant so that individual tests can mutate them to
    /// exercise specific edge cases.
    /// </summary>
    internal static class JsonFixtures
    {
        /// <summary>
        /// A minimal v1.0 document with a single "Addition" component. Valid against
        /// the main GhJSON schema.
        /// </summary>
        public const string MinimalAddition =
            @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]}";

        /// <summary>
        /// Two connected sliders feeding into a Panel. Uses integer IDs only.
        /// </summary>
        public const string TwoSlidersIntoPanel =
            @"{""schema"":""1.0"",""components"":[" +
            @"{""name"":""Number Slider"",""id"":1,""pivot"":""100,100""}," +
            @"{""name"":""Number Slider"",""id"":2,""pivot"":""100,150""}," +
            @"{""name"":""Panel"",""id"":3,""pivot"":""300,125""}" +
            @"],""connections"":[" +
            @"{""from"":{""id"":1,""paramName"":""Number""},""to"":{""id"":3,""paramName"":""Input""}}," +
            @"{""from"":{""id"":2,""paramName"":""Number""},""to"":{""id"":3,""paramName"":""Input""}}" +
            @"]}";
    }
}
