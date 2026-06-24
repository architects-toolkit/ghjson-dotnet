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
using System.IO;

namespace GhJSON.Core.Tests._Support
{
    /// <summary>
    /// Locates repository paths (sibling <c>ghjson-spec</c> workspace) at test time
    /// by walking up from the test assembly's base directory. This avoids copying
    /// spec examples/schemas into the test project and keeps a single source of truth.
    /// </summary>
    internal static class TestPaths
    {
        /// <summary>
        /// Gets the absolute path to the sibling <c>ghjson-spec</c> repository root,
        /// or <c>null</c> if it cannot be located from the test execution directory.
        /// </summary>
        public static string? GhJsonSpecRoot => LazySpecRoot.Value;

        /// <summary>
        /// Gets the absolute path to <c>ghjson-spec/examples</c>, or <c>null</c> if
        /// the spec root cannot be located.
        /// </summary>
        public static string? SpecExamplesDir =>
            GhJsonSpecRoot is null ? null : Path.Combine(GhJsonSpecRoot, "examples");

        /// <summary>
        /// Gets the absolute path to <c>ghjson-spec/schema/v1.0</c>, or <c>null</c>
        /// if the spec root cannot be located.
        /// </summary>
        public static string? SpecSchemaV1Dir =>
            GhJsonSpecRoot is null ? null : Path.Combine(GhJsonSpecRoot, "schema", "v1.0");

        private static readonly Lazy<string?> LazySpecRoot = new Lazy<string?>(LocateSpecRoot);

        private static string? LocateSpecRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                // Sibling workspace layout: <...>/architects-toolkit/{ghjson-dotnet,ghjson-spec}/
                var parent = dir.Parent;
                if (parent != null)
                {
                    var candidate = Path.Combine(parent.FullName, "ghjson-spec");
                    if (Directory.Exists(candidate) &&
                        Directory.Exists(Path.Combine(candidate, "schema", "v1.0")))
                    {
                        return candidate;
                    }
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
