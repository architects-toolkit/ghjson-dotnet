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
using System.Threading.Tasks;
using GhJSON.Core.Validation;
using Json.Schema;
using Xunit;

namespace GhJSON.Core.Tests.Validation
{
    public class SchemaLoaderTests
    {
        [Fact]
        public void GetEmbeddedResourcePrefix_ReplacesDotsWithUnderscores()
        {
            var prefix = SchemaLoader.GetEmbeddedResourcePrefix("1.0");
            Assert.Contains("v1._0", prefix);
        }

        [Fact]
        public void GetEmbeddedResourcePrefix_HandlesMultiSegmentVersions()
        {
            var prefix = SchemaLoader.GetEmbeddedResourcePrefix("1.2.3");
            Assert.Contains("v1._2._3", prefix);
        }

        [Fact]
        public void Load_DefaultVersion_LoadsEmbeddedResources()
        {
            var bundle = SchemaLoader.Load(new SchemaLoaderOptions { PreferOnline = false });

            Assert.NotNull(bundle);
            Assert.NotNull(bundle.Main);
            Assert.True(bundle.Ids.Count >= 1, "Expected at least one registered schema.");
        }

        [Fact]
        public void Load_ExplicitVersion_LoadsEmbeddedResources()
        {
            var bundle = SchemaLoader.Load(new SchemaLoaderOptions { Version = "1.0", PreferOnline = false });

            Assert.NotNull(bundle);
            Assert.NotNull(bundle.Main);
            Assert.True(bundle.Ids.Count >= 1, "Expected at least one registered schema.");
        }

        [Fact]
        public void Load_UnknownVersion_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => SchemaLoader.Load(new SchemaLoaderOptions { Version = "99.99", PreferOnline = false }));
        }

        [Fact]
        public void Load_WithOptions_LoadsEmbeddedResources()
        {
            var options = new SchemaLoaderOptions { Version = "1.0", PreferOnline = false };
            var bundle = SchemaLoader.Load(options);

            Assert.NotNull(bundle);
            Assert.NotNull(bundle.Main);
        }

        [Fact]
        public void Load_PreferOnline_FallsBackToEmbeddedOnNetworkFailure()
        {
            // Use a non-routable URL to force network failure, which should fall back.
            var options = new SchemaLoaderOptions
            {
                Version = "1.0",
                PreferOnline = true,
                BaseUrl = "http://192.0.2.1/nonexistent/",
                OnlineTimeout = TimeSpan.FromMilliseconds(100)
            };

            var bundle = SchemaLoader.Load(options);

            Assert.NotNull(bundle);
            Assert.NotNull(bundle.Main);
            Assert.True(bundle.Ids.Count >= 1, "Expected fallback to embedded resources to succeed.");
        }

        [Fact]
        public async Task LoadAsync_PreferOnline_FallsBackToEmbeddedOnNetworkFailure()
        {
            var options = new SchemaLoaderOptions
            {
                Version = "1.0",
                PreferOnline = true,
                BaseUrl = "http://192.0.2.1/nonexistent/",
                OnlineTimeout = TimeSpan.FromMilliseconds(100)
            };

            var bundle = await SchemaLoader.LoadAsync(options);

            Assert.NotNull(bundle);
            Assert.NotNull(bundle.Main);
            Assert.True(bundle.Ids.Count >= 1, "Expected fallback to embedded resources to succeed.");
        }

        [Fact]
        public void MainSchema_DefaultVersion_IsAvailable()
        {
            Assert.NotNull(SchemaLoader.MainSchema);
            Assert.NotNull(SchemaLoader.MainSchema.GetId());
        }

        [Fact]
        public void RegisteredSchemaIds_DefaultVersion_HasMainSchema()
        {
            Assert.Contains(
                SchemaLoader.RegisteredSchemaIds,
                uri => uri.ToString().EndsWith("/ghjson.schema.json", StringComparison.Ordinal));
        }
    }
}
