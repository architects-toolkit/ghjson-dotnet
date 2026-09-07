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
using GhJSON.Grasshopper.GetOperations;
using Grasshopper.Kernel;
using Rhino;

namespace GhJSON.Grasshopper.ConnectionOperations
{
    /// <summary>
    /// Thread-safe helpers for wiring Grasshopper components on the active canvas.
    /// <para>
    /// All canvas mutations run on the Rhino UI thread via <see cref="RhinoApp.InvokeOnUiThread"/>
    /// and the caller blocks until the callback completes. This matches the behaviour of
    /// <see cref="DeleteOperations.CanvasDeleter"/>.
    /// </para>
    /// </summary>
    internal static class CanvasConnector
    {
        private static readonly TimeSpan UiInvokeTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Connects two parameters on the active canvas by component instance GUID and
        /// parameter name. Executes on the UI thread.
        /// </summary>
        /// <param name="sourceGuid">Instance GUID of the source component or parameter.</param>
        /// <param name="targetGuid">Instance GUID of the target component or parameter.</param>
        /// <param name="sourceParamName">NickName or Name of the source output parameter. If null or empty, the first output is used.</param>
        /// <param name="targetParamName">NickName or Name of the target input parameter. If null or empty, the first input is used.</param>
        /// <returns><c>true</c> if the connection was created or already existed.</returns>
        public static bool Connect(
            Guid sourceGuid,
            Guid targetGuid,
            string? sourceParamName = null,
            string? targetParamName = null)
        {
            var success = false;

            InvokeOnUiThreadAndWait(() =>
                success = ConnectOnUiThread(sourceGuid, targetGuid, sourceParamName, targetParamName));

            return success;
        }

        private static bool ConnectOnUiThread(
            Guid sourceGuid,
            Guid targetGuid,
            string? sourceParamName,
            string? targetParamName)
        {
            var doc = CanvasReader.GetActiveDocument();
            if (doc == null)
            {
                return false;
            }

            var sourceObj = doc.FindObject(sourceGuid, true);
            var targetObj = doc.FindObject(targetGuid, true);
            if (sourceObj == null || targetObj == null)
            {
                return false;
            }

            var sourceParam = ConnectionHelper.ResolveOutput(sourceObj, sourceParamName);
            var targetParam = ConnectionHelper.ResolveInput(targetObj, targetParamName);

            if (sourceParam == null || targetParam == null)
            {
                return false;
            }

            if (targetParam.Sources.Contains(sourceParam))
            {
                return true;
            }

            targetParam.RecordUndoEvent("[GhJSON] Connect");
            targetParam.AddSource(sourceParam);

            return true;
        }

        /// <summary>
        /// Disconnects two parameters on the active canvas by component instance GUID and
        /// parameter name. Executes on the UI thread.
        /// </summary>
        /// <param name="sourceGuid">Instance GUID of the source component or parameter.</param>
        /// <param name="targetGuid">Instance GUID of the target component or parameter.</param>
        /// <param name="sourceParamName">NickName or Name of the source output parameter. If null or empty, the first output is used.</param>
        /// <param name="targetParamName">NickName or Name of the target input parameter. If null or empty, the first input is used.</param>
        /// <returns><c>true</c> if the connection did not exist or was removed.</returns>
        public static bool Disconnect(
            Guid sourceGuid,
            Guid targetGuid,
            string? sourceParamName = null,
            string? targetParamName = null)
        {
            var success = false;

            InvokeOnUiThreadAndWait(() =>
                success = DisconnectOnUiThread(sourceGuid, targetGuid, sourceParamName, targetParamName));

            return success;
        }

        private static bool DisconnectOnUiThread(
            Guid sourceGuid,
            Guid targetGuid,
            string? sourceParamName,
            string? targetParamName)
        {
            var doc = CanvasReader.GetActiveDocument();
            if (doc == null)
            {
                return false;
            }

            var sourceObj = doc.FindObject(sourceGuid, true);
            var targetObj = doc.FindObject(targetGuid, true);
            if (sourceObj == null || targetObj == null)
            {
                return false;
            }

            var sourceParam = ConnectionHelper.ResolveOutput(sourceObj, sourceParamName);
            var targetParam = ConnectionHelper.ResolveInput(targetObj, targetParamName);

            if (sourceParam == null || targetParam == null)
            {
                return false;
            }

            if (!targetParam.Sources.Contains(sourceParam))
            {
                return true;
            }

            targetParam.RecordUndoEvent("[GhJSON] Disconnect");
            targetParam.RemoveSource(sourceParam);

            return true;
        }

        /// <summary>
        /// Captures all wires that connect a set of objects to objects outside the set.
        /// This is useful when replacing components: external connections can be restored
        /// after the new objects are placed.
        /// </summary>
        /// <param name="guids">Instance GUIDs of the objects whose external connections should be captured.</param>
        /// <returns>A list of external connections, each from source to target.</returns>
        public static IReadOnlyList<ConnectionInfo> CaptureExternalConnections(IEnumerable<Guid> guids)
        {
            var result = new List<ConnectionInfo>();

            InvokeOnUiThreadAndWait(() =>
                result = CaptureExternalConnectionsOnUiThread(guids));

            return result;
        }

        private static List<ConnectionInfo> CaptureExternalConnectionsOnUiThread(IEnumerable<Guid> guids)
        {
            var doc = CanvasReader.GetActiveDocument();
            if (doc == null)
            {
                return new List<ConnectionInfo>();
            }

            var replaceSet = new HashSet<Guid>(guids);
            var allObjects = doc.Objects.ToList();
            var ownerCache = new Dictionary<IGH_Param, IGH_DocumentObject?>();
            var seen = new HashSet<(Guid source, string sourceParam, Guid target, string targetParam)>();
            var result = new List<ConnectionInfo>();

            IGH_DocumentObject? FindOwnerCached(IGH_Param param)
            {
                if (param == null)
                {
                    return null;
                }

                if (ownerCache.TryGetValue(param, out var cached))
                {
                    return cached;
                }

                var owner = ConnectionHelper.FindOwner(param, allObjects);
                ownerCache[param] = owner;
                return owner;
            }

            foreach (var guid in replaceSet)
            {
                var obj = CanvasReader.FindObject(doc, guid);
                if (obj == null)
                {
                    continue;
                }

                if (obj is IGH_Component comp)
                {
                    foreach (var outParam in comp.Params.Output)
                    {
                        foreach (var recipient in outParam.Recipients)
                        {
                            var targetOwner = FindOwnerCached(recipient);
                            if (targetOwner == null)
                            {
                                continue;
                            }

                            var targetGuid = targetOwner.InstanceGuid;
                            if (replaceSet.Contains(targetGuid))
                            {
                                continue;
                            }

                            var key = (guid, outParam.NickName, targetGuid, recipient.NickName);
                            if (seen.Add(key))
                            {
                                result.Add(new ConnectionInfo(guid, outParam.NickName, targetGuid, recipient.NickName));
                            }
                        }
                    }

                    foreach (var inParam in comp.Params.Input)
                    {
                        foreach (var source in inParam.Sources)
                        {
                            var sourceOwner = FindOwnerCached(source);
                            if (sourceOwner == null)
                            {
                                continue;
                            }

                            var sourceGuid = sourceOwner.InstanceGuid;
                            if (replaceSet.Contains(sourceGuid))
                            {
                                continue;
                            }

                            var key = (sourceGuid, source.NickName, guid, inParam.NickName);
                            if (seen.Add(key))
                            {
                                result.Add(new ConnectionInfo(sourceGuid, source.NickName, guid, inParam.NickName));
                            }
                        }
                    }
                }
                else if (obj is IGH_Param param)
                {
                    foreach (var source in param.Sources)
                    {
                        var sourceOwner = FindOwnerCached(source);
                        if (sourceOwner == null)
                        {
                            continue;
                        }

                        var sourceGuid = sourceOwner.InstanceGuid;
                        if (replaceSet.Contains(sourceGuid))
                        {
                            continue;
                        }

                        var key = (sourceGuid, source.NickName, guid, param.NickName);
                        if (seen.Add(key))
                        {
                            result.Add(new ConnectionInfo(sourceGuid, source.NickName, guid, param.NickName));
                        }
                    }

                    foreach (var recipient in param.Recipients)
                    {
                        var targetOwner = FindOwnerCached(recipient);
                        if (targetOwner == null)
                        {
                            continue;
                        }

                        var targetGuid = targetOwner.InstanceGuid;
                        if (replaceSet.Contains(targetGuid))
                        {
                            continue;
                        }

                        var key = (guid, param.NickName, targetGuid, recipient.NickName);
                        if (seen.Add(key))
                        {
                            result.Add(new ConnectionInfo(guid, param.NickName, targetGuid, recipient.NickName));
                        }
                    }
                }
            }

            return result;
        }

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
                    $"Grasshopper UI thread did not process the connection within {UiInvokeTimeout.TotalSeconds:n0} s.");
            }

            if (captured != null)
            {
                throw new InvalidOperationException(
                    "Connection on the Grasshopper UI thread failed.", captured);
            }
        }

    }
}
