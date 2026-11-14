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
using System.Threading;
using Rhino;

namespace GhJSON.Grasshopper.Shared
{
    /// <summary>
    /// Marshals canvas mutations onto the Rhino UI thread. Centralizes the
    /// blocking-invoke contract — including its late-execution guard — so each
    /// operation does not grow its own copy.
    /// </summary>
    internal static class UiThreadDispatcher
    {
        private static readonly TimeSpan UiInvokeTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Queues <paramref name="action"/> on the Rhino UI thread and blocks until it
        /// returns (or 30 seconds elapse). If already on the UI thread the action runs inline.
        /// </summary>
        /// <param name="action">The action to run.</param>
        internal static void RunAndWait(Action action)
        {
            if (RhinoApp.InvokeRequired == false)
            {
                action();
                return;
            }

            Exception? captured = null;
            using var done = new ManualResetEventSlim(false);

            // Ownership flag: 0 = unclaimed. The queued callback claims it to start
            // running; the waiter claims it when the timeout elapses, so a late UI
            // pass can never apply the mutation after the caller already reported
            // a failure.
            int started = 0;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                if (Interlocked.CompareExchange(ref started, 1, 0) != 0)
                {
                    // The caller already timed out — never run the mutation after a
                    // reported failure.
                    return;
                }

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
                if (Interlocked.CompareExchange(ref started, 1, 0) == 0)
                {
                    // We claimed the action before it started — the queued callback is
                    // now a no-op, so it is safe to report a timeout.
                    throw new TimeoutException(
                        $"Grasshopper UI thread did not process the call within {UiInvokeTimeout.TotalSeconds:n0} s.");
                }

                // The action already started on the UI thread — it owns completion now.
                // Wait for it so the caller does not proceed while a mutation is still
                // being applied, then report its real outcome.
                done.Wait();
            }

            if (captured != null)
            {
                throw new InvalidOperationException(
                    "Operation on the Grasshopper UI thread failed.", captured);
            }
        }
    }
}
