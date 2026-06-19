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

namespace GhJSON.Grasshopper.DeleteOperations
{
    /// <summary>
    /// Options for deleting objects from the Grasshopper canvas.
    /// </summary>
    public class DeleteOptions
    {
        /// <summary>
        /// Gets or sets whether to redraw the canvas after deletion.
        /// </summary>
        public bool Redraw { get; set; } = true;

        /// <summary>
        /// Gets the default delete options.
        /// </summary>
        public static DeleteOptions Default { get; } = new DeleteOptions();
    }
}
