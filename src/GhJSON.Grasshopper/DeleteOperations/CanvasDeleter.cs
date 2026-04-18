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
using System.Threading;
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
    /// Undo batching uses <c>GH_UndoUtil.CreateGenericObjectEvent</c> with every affected
    /// object calling <c>RecordUndoEvent(undo)</c>, ensuring a single Ctrl+Z reverts the
    /// whole batch.
    /// </para>
    /// </summary>
    internal static class CanvasDeleter
    {
        /// <summary>
        /// Maximum time the caller will wait for the UI-thread deletion callback to finish.
        /// Protects against deadlocks when the UI thread is unresponsive; in practice the
        /// callback completes in milliseconds.
        /// </summary>
        private static readonly TimeSpan UiInvokeTimeout = TimeSpan.FromSeconds(30);

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

            var doc = GetActiveDocument();
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
                var obj = FindObject(doc, guid);
                if (obj == null)
                {
                    result.Failed.Add((guid, "Object not found"));
                    continue;
                }

                objectsToDelete.Add(obj);
                result.Deleted.Add(guid);
            }

            if (objectsToDelete.Count == 0)
            {
                result.Success = result.Failed.Count == 0;
                return result;
            }

            InvokeOnUiThreadAndWait(() =>
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

            var doc = GetActiveDocument();
            if (doc == null)
            {
                result.Success = false;
                result.ErrorMessage = "No active Grasshopper document";
                return result;
            }

            var objectsToDelete = doc.Objects.OfType<IGH_DocumentObject>().ToList();

            foreach (var obj in objectsToDelete)
            {
                result.Deleted.Add(obj.InstanceGuid);
            }

            if (objectsToDelete.Count == 0)
            {
                return result;
            }

            InvokeOnUiThreadAndWait(() =>
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
                    obj.RecordUndoEvent(singleLabel);
                    doc.RemoveObject(obj, false);
                }
                else
                {
                    var undo = doc.UndoUtil.CreateGenericObjectEvent(batchLabel, objectsToDelete[0]);

                    // Every object in the batch — including the first — must record against
                    // the same undo event so that a single Ctrl+Z reverts them all together.
                    foreach (var obj in objectsToDelete)
                    {
                        obj.RecordUndoEvent(undo);
                    }

                    foreach (var obj in objectsToDelete)
                    {
                        doc.RemoveObject(obj, false);
                    }

                    doc.UndoUtil.RecordEvent(undo);
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
                foreach (var guid in result.Deleted.ToList())
                {
                    result.Failed.Add((guid, ex.Message));
                }

                result.Deleted.Clear();
            }
        }

        /// <summary>
        /// Queues <paramref name="action"/> on the Rhino UI thread and blocks until it
        /// returns (or <see cref="UiInvokeTimeout"/> elapses). If we are already on the UI
        /// thread the action runs inline.
        /// </summary>
        private static void InvokeOnUiThreadAndWait(Action action)
        {
            if (RhinoApp.InvokeRequired == false)
            {
                action();
                return;
            }

            Exception? captured = null;
            using var done = new ManualResetEventSlim(false);

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
                finally
                {
                    done.Set();
                }
            }));

            if (!done.Wait(UiInvokeTimeout))
            {
                throw new TimeoutException(
                    $"Grasshopper UI thread did not process the deletion within {UiInvokeTimeout.TotalSeconds:n0} s.");
            }

            if (captured != null)
            {
                throw new InvalidOperationException(
                    "Deletion on the Grasshopper UI thread failed.", captured);
            }
        }

        private static GH_Document? GetActiveDocument()
        {
            try
            {
                return Instances.ActiveCanvas?.Document;
            }
            catch
            {
                return null;
            }
        }

        private static IGH_DocumentObject? FindObject(GH_Document doc, Guid guid)
        {
            return doc.Objects.FirstOrDefault(o => o.InstanceGuid == guid);
        }
    }
}
