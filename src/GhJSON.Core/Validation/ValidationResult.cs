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
using System.Linq;

namespace GhJSON.Core.Validation
{
    /// <summary>
    /// Represents the result of a GhJSON document validation.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the document is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of error messages.
        /// </summary>
        public List<ValidationMessage> Errors { get; set; } = new List<ValidationMessage>();

        /// <summary>
        /// Gets or sets the list of warning messages.
        /// </summary>
        public List<ValidationMessage> Warnings { get; set; } = new List<ValidationMessage>();

        /// <summary>
        /// Gets or sets the list of informational messages.
        /// </summary>
        public List<ValidationMessage> Info { get; set; } = new List<ValidationMessage>();

        /// <summary>
        /// Gets a value indicating whether the document has any errors.
        /// </summary>
        public bool HasErrors => this.Errors.Any();

        /// <summary>
        /// Gets a value indicating whether the document has any warnings.
        /// </summary>
        public bool HasWarnings => this.Warnings.Any();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A successful validation result.</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="path">The JSON path where the error occurred.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationResult Failure(string message, string? path = null)
        {
            var result = new ValidationResult { IsValid = false };
            result.Errors.Add(new ValidationMessage(message, path));
            return result;
        }
    }

    /// <summary>
    /// Represents a validation message with optional path information.
    /// </summary>
    public sealed class ValidationMessage
    {
        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the JSON path where the issue occurred.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationMessage"/> class.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="path">The JSON path where the issue occurred.</param>
        public ValidationMessage(string message, string? path = null)
        {
            this.Message = message;
            this.Path = path;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Path)
                ? this.Message
                : $"{this.Path}: {this.Message}";
        }
    }
}
