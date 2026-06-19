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

using System.Globalization;
using System.Threading;
using GhJSON.Core.SchemaModels;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    /// <summary>
    /// Ensures numeric parsing/serialization remains culture-invariant even when
    /// the current thread's culture uses a non-"." decimal separator (e.g. de-DE).
    /// Regression guard for locale-dependent bugs in <see cref="GhJsonPivot"/> and
    /// the main serializer.
    /// </summary>
    public class NumberCultureTests
    {
        [Fact]
        public void FromJson_WithGermanCulture_ParsesInvariantNumbers()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                const string json =
                    @"{""schema"":""1.0"",""components"":[" +
                    @"{""name"":""X"",""id"":1,""pivot"":{""x"":1.5,""y"":-2.25}}]}";

                var doc = GhJson.FromJson(json);

                Assert.Equal(1.5, doc.Components[0].Pivot!.X);
                Assert.Equal(-2.25, doc.Components[0].Pivot!.Y);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void RoundTrip_WithGermanCulture_PreservesValues()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                var doc = GhJson.CreateDocumentBuilder()
                    .AddComponent(new GhJsonComponent
                    {
                        Name = "X",
                        Id = 1,
                        Pivot = new GhJsonPivot(3.14, -0.5),
                    })
                    .Build();

                var json = GhJson.ToJson(doc);
                var reloaded = GhJson.FromJson(json);

                Assert.Equal(3.14, reloaded.Components[0].Pivot!.X);
                Assert.Equal(-0.5, reloaded.Components[0].Pivot!.Y);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }
    }
}
