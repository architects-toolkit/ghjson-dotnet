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
using Grasshopper;
using Grasshopper.Kernel;
using Rhino;

namespace GhJSON.Grasshopper.DeleteOperations
{
    /// <summary>
    /// Handles deletion of objects from the Grasshopper canvas.
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

            if (!objectsToDelete.Any())
            {
                result.Success = result.Failed.Count == 0;
                return result;
            }

            RhinoApp.InvokeOnUiThread(() =>
            {
                try
                {
                    if (objectsToDelete.Count == 1)
                    {
                        var obj = objectsToDelete[0];
                        obj.RecordUndoEvent("[GhJSON] Delete Object");
                        doc.RemoveObject(obj, false);
                    }
                    else
                    {
                        var undo = doc.UndoUtil.CreateGenericObjectEvent(
                            "[GhJSON] Delete Objects",
                            objectsToDelete[0]);

                        foreach (var obj in objectsToDelete.Skip(1))
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
            });

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

            if (!objectsToDelete.Any())
            {
                result.Success = true;
                return result;
            }

            RhinoApp.InvokeOnUiThread(() =>
            {
                try
                {
                    if (objectsToDelete.Count == 1)
                    {
                        var obj = objectsToDelete[0];
                        obj.RecordUndoEvent("[GhJSON] Clear Canvas");
                        doc.RemoveObject(obj, false);
                    }
                    else
                    {
                        var undo = doc.UndoUtil.CreateGenericObjectEvent(
                            "[GhJSON] Clear Canvas",
                            objectsToDelete[0]);

                        foreach (var obj in objectsToDelete.Skip(1))
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
                    result.ErrorMessage = $"Clear failed: {ex.Message}";
                    result.Deleted.Clear();
                }
            });

            return result;
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
