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

using System;
using System.Collections.Generic;
using System.Linq;
using GhJSON.Grasshopper.GetOperations;
using GhJSON.Grasshopper.Shared;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino;

namespace GhJSON.Grasshopper.DeleteOperations
{
    /// <summary>
    /// Handles deletion of objects from the Grasshopper canvas.
    /// <para>
    /// All canvas mutations run on the Rhino UI thread via <see cref="RhinoApp.InvokeOnUiThread"/>,
    /// and the caller blocks until the UI callback completes so that the returned
    /// <see cref="DeleteResult"/> reflects the actual outcome (rather than the
    /// fire-and-forget state before the callback had a chance to run).
    /// </para>
    /// <para>
    /// Undo uses <c>GH_UndoUtil.RecordRemoveObjectEvent</c>, which serializes each removed
    /// object into the undo record so a single Ctrl+Z re-adds the whole batch. Generic
    /// object events (<c>RecordUndoEvent</c>) only snapshot state and fail to undo
    /// removals because the target object no longer exists in the document.
    /// </para>
    /// </summary>
    internal static class CanvasDeleter
    {
        /// <summary>
        /// Deletes specific objects from the canvas by their GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of objects to delete.</param>
        /// <param name="options">Optional delete options.</param>
        /// <returns>The delete result.</returns>
        public static DeleteResult DeleteByGuids(IEnumerable<Guid> guids, DeleteOptions? options = null)
        {
            options ??= DeleteOptions.Default;
            var result = new DeleteResult { Success = true };

            var doc = CanvasReader.GetActiveDocument();
            if (doc == null)
            {
                result.Success = false;
                result.ErrorMessage = "No active Grasshopper document";
                foreach (var guid in guids)
                {
                    result.Failed.Add((guid, "No active document"));
                }

                return result;
            }

            var objectsToDelete = new List<IGH_DocumentObject>();

            foreach (var guid in guids)
            {
                var obj = CanvasReader.FindObject(doc, guid);
                if (obj == null)
                {
                    result.Failed.Add((guid, "Object not found"));
                    continue;
                }

                objectsToDelete.Add(obj);
            }

            if (objectsToDelete.Count == 0)
            {
                result.Success = result.Failed.Count == 0;
                return result;
            }

            UiThreadDispatcher.RunAndWait(() =>
                DeleteBatch(doc, objectsToDelete, "[GhJSON] Delete Object", "[GhJSON] Delete Objects", options, result));

            return result;
        }

        /// <summary>
        /// Clears all objects from the canvas.
        /// </summary>
        /// <param name="options">Optional delete options.</param>
        /// <returns>The delete result.</returns>
        public static DeleteResult Clear(DeleteOptions? options = null)
        {
            options ??= DeleteOptions.Default;
            var result = new DeleteResult { Success = true };

            var doc = CanvasReader.GetActiveDocument();
            if (doc == null)
            {
                result.Success = false;
                result.ErrorMessage = "No active Grasshopper document";
                return result;
            }

            var objectsToDelete = doc.Objects.OfType<IGH_DocumentObject>().ToList();

            if (objectsToDelete.Count == 0)
            {
                return result;
            }

            UiThreadDispatcher.RunAndWait(() =>
                DeleteBatch(doc, objectsToDelete, "[GhJSON] Clear Canvas", "[GhJSON] Clear Canvas", options, result));

            return result;
        }

        private static void DeleteBatch(
            GH_Document doc,
            List<IGH_DocumentObject> objectsToDelete,
            string singleLabel,
            string batchLabel,
            DeleteOptions options,
            DeleteResult result)
        {
            try
            {
                if (objectsToDelete.Count == 1)
                {
                    var obj = objectsToDelete[0];
                    doc.UndoUtil.RecordRemoveObjectEvent(singleLabel, obj);
                    doc.RemoveObject(obj, false);
                    result.Deleted.Add(obj.InstanceGuid);
                }
                else
                {
                    // The record serializes every affected object before removal so a
                    // single Ctrl+Z re-adds them all together.
                    doc.UndoUtil.RecordRemoveObjectEvent(batchLabel, objectsToDelete);

                    foreach (var obj in objectsToDelete)
                    {
                        doc.RemoveObject(obj, false);
                        result.Deleted.Add(obj.InstanceGuid);
                    }
                }

                if (options.Redraw)
                {
                    Instances.RedrawCanvas();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Deletion failed: {ex.Message}";

                // Surface every object that was scheduled for deletion as failed, since the
                // batch is aborted as a unit on error.
                foreach (var obj in objectsToDelete)
                {
                    result.Failed.Add((obj.InstanceGuid, ex.Message));
                }

                result.Deleted.Clear();
            }
        }

    }
}
