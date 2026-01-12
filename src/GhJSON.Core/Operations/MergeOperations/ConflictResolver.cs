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

using System.Collections.Generic;
using GhJSON.Core.Models.Components;

namespace GhJSON.Core.Operations.MergeOperations
{
    /// <summary>
    /// Resolves conflicts during document merge operations.
    /// </summary>
    public class ConflictResolver
    {
        private readonly ConflictResolution _strategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResolver"/> class.
        /// </summary>
        /// <param name="strategy">The conflict resolution strategy to use.</param>
        public ConflictResolver(ConflictResolution strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// Resolves a conflict for a component being merged.
        /// </summary>
        /// <param name="sourceComponent">The component from the source document.</param>
        /// <param name="targetComponents">The components in the target document.</param>
        /// <returns>The action to take.</returns>
        public ConflictAction Resolve(ComponentProperties sourceComponent, List<ComponentProperties> targetComponents)
        {
            return _strategy switch
            {
                ConflictResolution.TargetWins => ConflictAction.Skip,
                ConflictResolution.SourceWins => ConflictAction.Replace,
                ConflictResolution.KeepBoth => ConflictAction.KeepBoth,
                ConflictResolution.Fail => ConflictAction.Fail,
                _ => ConflictAction.Skip
            };
        }
    }

    /// <summary>
    /// Actions to take when resolving conflicts.
    /// </summary>
    public enum ConflictAction
    {
        /// <summary>Skip the conflicting source component.</summary>
        Skip,

        /// <summary>Replace the target component with source.</summary>
        Replace,

        /// <summary>Keep both components (generate new GUID for source).</summary>
        KeepBoth,

        /// <summary>Fail the operation.</summary>
        Fail
    }
}
