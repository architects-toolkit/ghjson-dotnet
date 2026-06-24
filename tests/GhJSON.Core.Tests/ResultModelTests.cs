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

using GhJSON.Core.DiffOperations;
using GhJSON.Core.FixOperations;
using GhJSON.Core.MergeOperations;
using GhJSON.Core.Validation;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GhJSON.Core.Tests
{
    public class ResultModelTests
    {
        #region ValidationResult

        [Fact]
        public void ValidationResult_HasErrors_WithErrors_ReturnsTrue()
        {
            var result = ValidationResult.Failure("test error");
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void ValidationResult_HasErrors_WithoutErrors_ReturnsFalse()
        {
            var result = ValidationResult.Success();
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ValidationResult_HasWarnings_WithWarnings_ReturnsTrue()
        {
            var result = new ValidationResult();
            result.Warnings.Add(new ValidationMessage("warning"));
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void ValidationResult_HasWarnings_WithoutWarnings_ReturnsFalse()
        {
            var result = new ValidationResult();
            Assert.False(result.HasWarnings);
        }

        [Fact]
        public void ValidationResult_Success_SetsIsValidTrue()
        {
            var result = ValidationResult.Success();
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidationResult_Failure_SetsIsValidFalse()
        {
            var result = ValidationResult.Failure("error", "path");
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("error", result.Errors[0].Message);
            Assert.Equal("path", result.Errors[0].Path);
        }

        [Fact]
        public void ValidationMessage_ToString_WithPath_IncludesPath()
        {
            var message = new ValidationMessage("error", "$.components[0]");
            Assert.Contains("$.components[0]", message.ToString());
            Assert.Contains("error", message.ToString());
        }

        [Fact]
        public void ValidationMessage_ToString_WithoutPath_ReturnsMessage()
        {
            var message = new ValidationMessage("error");
            Assert.Equal("error", message.ToString());
        }

        #endregion

        #region DiffResult

        [Fact]
        public void DiffResult_HasChanges_WithComponentOps_ReturnsTrue()
        {
            var result = new DiffResult { ComponentOpCount = 1 };
            Assert.True(result.HasChanges);
        }

        [Fact]
        public void DiffResult_HasChanges_WithConnectionOps_ReturnsTrue()
        {
            var result = new DiffResult { ConnectionOpCount = 1 };
            Assert.True(result.HasChanges);
        }

        [Fact]
        public void DiffResult_HasChanges_WithGroupOps_ReturnsTrue()
        {
            var result = new DiffResult { GroupOpCount = 1 };
            Assert.True(result.HasChanges);
        }

        [Fact]
        public void DiffResult_HasChanges_WithMetadata_ReturnsTrue()
        {
            var result = new DiffResult
            {
                Patch = new PatchModels.GhPatchDocument
                {
                    Patch = new PatchModels.GhPatchBody
                    {
                        Metadata = new PatchModels.GhPatchMetadataOp { Set = new JObject() }
                    }
                }
            };
            Assert.True(result.HasChanges);
        }

        [Fact]
        public void DiffResult_HasChanges_WithNoOps_ReturnsFalse()
        {
            var result = new DiffResult();
            Assert.False(result.HasChanges);
        }

        #endregion

        #region ApplyPatchResult

        [Fact]
        public void ApplyPatchResult_HasConflicts_WithConflicts_ReturnsTrue()
        {
            var result = new ApplyPatchResult();
            result.Conflicts.Add(new PatchConflict(PatchConflictKind.MatchNotFound, "not found"));
            Assert.True(result.HasConflicts);
        }

        [Fact]
        public void ApplyPatchResult_HasConflicts_WithoutConflicts_ReturnsFalse()
        {
            var result = new ApplyPatchResult();
            Assert.False(result.HasConflicts);
        }

        [Fact]
        public void ApplyPatchResult_DefaultDocument_IsNotNull()
        {
            var result = new ApplyPatchResult();
            Assert.NotNull(result.Document);
        }

        #endregion

        #region FixResult

        [Fact]
        public void FixResult_DefaultDocument_IsNotNull()
        {
            var result = new FixResult();
            Assert.NotNull(result.Document);
        }

        [Fact]
        public void FixResult_AppliedActions_IsInitialized()
        {
            var result = new FixResult();
            Assert.NotNull(result.AppliedActions);
        }

        [Fact]
        public void FixResult_UnfixedIssues_IsInitialized()
        {
            var result = new FixResult();
            Assert.NotNull(result.UnfixedIssues);
        }

        #endregion

        #region MergeResult

        [Fact]
        public void MergeResult_DefaultDocument_IsNotNull()
        {
            var result = new MergeResult();
            Assert.NotNull(result.Document);
        }

        [Fact]
        public void MergeResult_IdMapping_IsInitialized()
        {
            var result = new MergeResult();
            Assert.NotNull(result.IdMapping);
        }

        [Fact]
        public void MergeResult_Conflicts_IsInitialized()
        {
            var result = new MergeResult();
            Assert.NotNull(result.Conflicts);
        }

        #endregion

        #region PatchConflict

        [Fact]
        public void PatchConflict_ToString_WithPath_IncludesPath()
        {
            var conflict = new PatchConflict(PatchConflictKind.MatchNotFound, "not found", "components.modify");
            Assert.Contains("components.modify", conflict.ToString());
        }

        [Fact]
        public void PatchConflict_ToString_WithoutPath_ExcludesPath()
        {
            var conflict = new PatchConflict(PatchConflictKind.MatchNotFound, "not found");
            // Format should be "{Kind}: {Message}" without " at {Path}:"
            Assert.Equal("MatchNotFound: not found", conflict.ToString());
        }

        #endregion
    }
}
