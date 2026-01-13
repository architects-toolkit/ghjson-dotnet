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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Serialization.DataTypes;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    public class ParamIntHandler : ComponentHandlerBase
    {
        public ParamIntHandler()
            : base(supportedTypes: new[] { typeof(Param_Integer) })
        {
        }

        public override int Priority => 50;

        public override bool CanHandle(IGH_DocumentObject obj) => obj is Param_Integer;

        public override ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            return null;
        }

        public override void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not Param_Integer param || state?.PersistentData == null)
                return;

            PersistentDataHandlerUtilities.ApplyPersistentDataToParam(param, state.PersistentData, ConvertToInteger);
        }

        private static GH_Integer ConvertToInteger(JToken token)
        {
            if (token.Type == JTokenType.String && DataTypeSerializer.TryDeserializeFromPrefix(token.ToString(), out object? intResult) && intResult is int intValue)
                return new GH_Integer(intValue);
            return new GH_Integer(token.Value<int>());
        }
    }
}
