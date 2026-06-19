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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using GhJSON.Core.Tests._Support;
using GhJSON.Core.Validation;
using Xunit;

namespace GhJSON.Core.Tests.SpecCompliance
{
    /// <summary>
    /// Enforces that every <c>.ghjson</c> example shipped in the sibling
    /// <c>ghjson-spec</c> repository validates against the current schema. Runs
    /// against the live spec sources so schema/example drift is caught immediately.
    /// When the sibling repo is not available (e.g. CI fetching only one repo) the
    /// tests no-op rather than fail.
    /// </summary>
    public class ExampleFileValidationTests
    {
        public static IEnumerable<object[]> ExampleFiles()
        {
            var dir = TestPaths.SpecExamplesDir;
            if (dir == null || !Directory.Exists(dir))
            {
                // Yield a sentinel so xUnit still reports a (trivially passing) test entry.
                yield return new object[] { "(no spec repo)", string.Empty };
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.ghjson", SearchOption.AllDirectories))
            {
                yield return new object[] { Path.GetFileName(file), file };
            }
        }

        [Theory]
        [MemberData(nameof(ExampleFiles))]
        public void SpecExample_ValidatesAgainstSchema(string fileName, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // Sibling ghjson-spec workspace is not present; nothing to validate.
                return;
            }

            var json = File.ReadAllText(path);
            var result = GhJson.Validate(json, ValidationLevel.Standard);

            Assert.True(
                result.IsValid,
                $"Spec example '{fileName}' failed validation: " +
                string.Join("; ", result.Errors.Select(e => e.ToString())));
        }

        [Fact]
        public void SpecExamplesDirectory_ContainsAtLeastOneFile_WhenAvailable()
        {
            if (TestPaths.SpecExamplesDir == null)
            {
                return;
            }

            var files = Directory.EnumerateFiles(
                TestPaths.SpecExamplesDir,
                "*.ghjson",
                SearchOption.AllDirectories).ToList();
            Assert.NotEmpty(files);
        }
    }
}
