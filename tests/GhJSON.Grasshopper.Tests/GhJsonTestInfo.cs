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
using System.Drawing;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Tests
{
    /// <summary>
    /// GHA Info for the GhJSON Grasshopper test components.
    /// </summary>
    public class GhJsonTestInfo : GH_AssemblyInfo
    {
        /// <inheritdoc/>
        public override string Name => "GhJSON Tests";

        /// <inheritdoc/>
        public override Bitmap? Icon => null;

        /// <inheritdoc/>
        public override string Description => "Test components for GhJSON Grasshopper integration";

        /// <inheritdoc/>
        public override Guid Id => new Guid("B7C5D3E1-A2F4-4B8C-9D6E-1A3B5C7D9E2F");

        /// <inheritdoc/>
        public override string AuthorName => "GhJSON";

        /// <inheritdoc/>
        public override string AuthorContact => "https://github.com/architects-toolkit/ghjson-dotnet";
    }
}
