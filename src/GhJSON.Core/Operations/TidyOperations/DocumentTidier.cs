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

using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Operations.TidyOperations
{
    /// <summary>
    /// Orchestrates tidy operations on GhJSON documents.
    /// </summary>
    public class DocumentTidier
    {
        private readonly TidyOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTidier"/> class.
        /// </summary>
        /// <param name="options">Tidy options.</param>
        public DocumentTidier(TidyOptions? options = null)
        {
            _options = options ?? TidyOptions.Default;
        }

        /// <summary>
        /// Tidies the document by reorganizing component positions.
        /// </summary>
        /// <param name="document">The document to tidy.</param>
        /// <returns>Tidy result.</returns>
        public TidyResult Tidy(GhJsonDocument document)
        {
            if (document == null)
            {
                return new TidyResult
                {
                    Success = false,
                    ErrorMessage = "Document is null"
                };
            }

            var result = new TidyResult { Success = true };

            if (_options.OrganizePivots)
            {
                var organizerOptions = new PivotOrganizerOptions
                {
                    HorizontalSpacing = _options.HorizontalSpacing,
                    VerticalSpacing = _options.VerticalSpacing,
                    IslandSpacing = _options.IslandSpacing,
                    StartX = _options.StartX,
                    StartY = _options.StartY
                };

                var organizer = new PivotOrganizer(organizerOptions);
                var organizeResult = organizer.Organize(document);

                result.NodesOrganized = organizeResult.NodesOrganized;
                result.WasModified = organizeResult.WasModified;

                if (!organizeResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = organizeResult.ErrorMessage;
                }
            }

            return result;
        }

        /// <summary>
        /// Tidies the document with default options.
        /// </summary>
        /// <param name="document">The document to tidy.</param>
        /// <returns>Tidy result.</returns>
        public static TidyResult TidyAll(GhJsonDocument document)
        {
            var tidier = new DocumentTidier(TidyOptions.Default);
            return tidier.Tidy(document);
        }

        /// <summary>
        /// Analyzes the document layout without modifying it.
        /// </summary>
        /// <param name="document">The document to analyze.</param>
        /// <returns>Layout analysis.</returns>
        public static LayoutAnalysis AnalyzeLayout(GhJsonDocument document)
        {
            var analyzer = new LayoutAnalyzer();
            return analyzer.Analyze(document);
        }
    }

    /// <summary>
    /// Options for tidy operations.
    /// </summary>
    public class TidyOptions
    {
        /// <summary>
        /// Gets or sets whether to organize pivots based on graph flow.
        /// </summary>
        public bool OrganizePivots { get; set; } = true;

        /// <summary>
        /// Gets or sets the horizontal spacing between depth levels.
        /// </summary>
        public float HorizontalSpacing { get; set; } = 200f;

        /// <summary>
        /// Gets or sets the vertical spacing between nodes at the same depth.
        /// </summary>
        public float VerticalSpacing { get; set; } = 100f;

        /// <summary>
        /// Gets or sets the spacing between disconnected islands.
        /// </summary>
        public float IslandSpacing { get; set; } = 150f;

        /// <summary>
        /// Gets or sets the starting X coordinate.
        /// </summary>
        public float StartX { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the starting Y coordinate.
        /// </summary>
        public float StartY { get; set; } = 0f;

        /// <summary>
        /// Gets the default tidy options.
        /// </summary>
        public static TidyOptions Default => new TidyOptions();
    }
}
