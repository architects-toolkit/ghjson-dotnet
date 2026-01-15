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

using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    internal sealed class PythonScriptHandler : BaseScriptHandler
    {
        private static readonly Guid Python3Guid = new Guid("719467e6-7cf5-4848-99b0-c5dd57e5442c");

        public override string ExtensionKey => "gh.python";

        protected override Guid ComponentGuid => Python3Guid;

        protected override string ComponentName => "Python";
    }
}
