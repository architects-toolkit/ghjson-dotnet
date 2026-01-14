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

namespace GhJSON.Grasshopper.TestComponents
{
    public sealed class GhJsonTestInfo : GH_AssemblyInfo
    {
        public override string Name => "GhJSON Test Components";

        public override Bitmap? Icon => null;

        public override string Description => "Grasshopper components for GhJSON integration testing";

        public override Guid Id => new Guid("7D7B6E11-3F7F-4E57-9B2D-8C2BDE2B7D91");

        public override string AuthorName => "GhJSON";

        public override string AuthorContact => "https://github.com/architects-toolkit/ghjson-dotnet";
    }
}
