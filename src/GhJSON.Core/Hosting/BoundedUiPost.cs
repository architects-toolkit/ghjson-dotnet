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
using System.Threading.Tasks;

namespace GhJSON.Core.Hosting
{
    /// <summary>
    /// Terminal state of a bounded UI post. The <c>*NotStarted</c> states carry a
    /// hard guarantee: the queued work never ran, so reporting them never hides a
    /// mutation that is still in flight.
    /// </summary>
    public enum UiPostState
    {
        /// <summary>The posted work ran to completion on the UI thread.</summary>
        Completed,

        /// <summary>The posted work started and threw. The exception travels in <see cref="UiPostResult.Error"/>.</summary>
        Faulted,

        /// <summary>The caller's cancellation token fired before the queued callback claimed the slot. The work never ran.</summary>
        CancelledNotStarted,

        /// <summary>The queued-start budget elapsed before the callback claimed the slot. The work never ran.</summary>
        BusyNotStarted,

        /// <summary>The host post delegate failed to queue the callback. The work never ran.</summary>
        NotPosted,
    }

    /// <summary>
    /// Outcome of a <see cref="BoundedUiPost.PostAsync(Action{Action}, Func{bool}, Action, TimeSpan, CancellationToken)"/>
    /// call. <see cref="UiPostResult{T}"/> additionally carries the work's return value.
    /// </summary>
    public class UiPostResult
    {
        internal UiPostResult(UiPostState state, Exception? error = null)
        {
            this.State = state;
            this.Error = error;
        }

        /// <summary>Gets the terminal state.</summary>
        public UiPostState State { get; }

        /// <summary>Gets the exception thrown by the posted work when <see cref="State"/> is <see cref="UiPostState.Faulted"/>.</summary>
        public Exception? Error { get; }

        /// <summary>
        /// Gets a value indicating whether the posted work may have produced side
        /// effects. Only <see cref="UiPostState.Completed"/> and
        /// <see cref="UiPostState.Faulted"/> count as started.
        /// </summary>
        public bool Started => this.State == UiPostState.Completed || this.State == UiPostState.Faulted;

        /// <summary>Gets a value indicating whether the work completed without throwing.</summary>
        public bool Succeeded => this.State == UiPostState.Completed;

        internal static UiPostResult OfCompleted()
        {
            return new UiPostResult(UiPostState.Completed);
        }

        internal static UiPostResult OfFaulted(Exception error)
        {
            return new UiPostResult(UiPostState.Faulted, error);
        }

        internal static UiPostResult OfCancelledNotStarted()
        {
            return new UiPostResult(UiPostState.CancelledNotStarted);
        }

        internal static UiPostResult OfBusyNotStarted()
        {
            return new UiPostResult(UiPostState.BusyNotStarted);
        }

        internal static UiPostResult OfNotPosted(Exception error)
        {
            return new UiPostResult(UiPostState.NotPosted, error);
        }
    }

    /// <summary>
    /// Outcome of a bounded UI post carrying a value.
    /// </summary>
    /// <typeparam name="T">The work's result type.</typeparam>
    public sealed class UiPostResult<T> : UiPostResult
    {
        internal UiPostResult(UiPostState state, T? value = default, Exception? error = null)
            : base(state, error)
        {
            this.Value = value;
        }

        /// <summary>Gets the work's return value when <see cref="UiPostResult.State"/> is <see cref="UiPostState.Completed"/>.</summary>
        public T? Value { get; }
    }

    /// <summary>
    /// One asynchronous host-UI invocation primitive. Queues work on the host UI
    /// thread through an injected nonblocking post and bounds the time the work may
    /// sit unclaimed in the queue. Ownership passes through two explicit states:
    /// <c>queued</c> (cancellable with zero side effects) and <c>started</c> (the UI
    /// callback claimed the slot and owns the outcome). A callback whose slot
    /// expired before it claimed becomes a no-op — a late UI pass can never run
    /// work the caller already reported as not started.
    /// </summary>
    /// <remarks>
    /// This type is host-independent: the only ambient dependency is the injected
    /// <c>post</c>/<c>isUiThread</c> pair. Rhino's <c>RhinoApp.InvokeOnUiThread</c>
    /// plugs in through the GhJSON.Grasshopper dispatcher; tests plug in a fake queue.
    /// </remarks>
    public static class BoundedUiPost
    {
        /// <summary>
        /// Queues <paramref name="action"/> for execution on the host UI thread.
        /// </summary>
        /// <param name="post">
        /// Genuine nonblocking UI-thread post (e.g. <c>RhinoApp.InvokeOnUiThread</c>).
        /// Must not run the callback synchronously on the calling thread and must not
        /// be <c>Task.Run</c>.
        /// </param>
        /// <param name="isUiThread">
        /// Returns whether the calling thread is the host UI thread; when true the
        /// action runs inline. May be null to always post.
        /// </param>
        /// <param name="action">The work to run on the UI thread.</param>
        /// <param name="queuedStartBudget">
        /// Maximum time the work may wait in the UI queue before the post reports
        /// <see cref="UiPostState.BusyNotStarted"/>. Once the callback has started,
        /// the caller waits for the real outcome — this budget bounds queue
        /// admission only, never started work.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancels the post while still queued; once the callback has started the
        /// token no longer affects the wait.
        /// </param>
        /// <returns>A task completing with the post's terminal state.</returns>
        public static async Task<UiPostResult> PostAsync(
            Action<Action> post,
            Func<bool> isUiThread,
            Action action,
            TimeSpan queuedStartBudget,
            CancellationToken cancellationToken)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var result = await PostAsync<object?>(
                post,
                isUiThread,
                () =>
                {
                    action();
                    return null;
                },
                queuedStartBudget,
                cancellationToken).ConfigureAwait(false);
            return new UiPostResult(result.State, result.Error);
        }

        /// <summary>
        /// Queues <paramref name="work"/> for execution on the host UI thread and
        /// returns its value. See the untyped overload for the ownership contract.
        /// </summary>
        /// <typeparam name="T">The work's result type.</typeparam>
        public static Task<UiPostResult<T>> PostAsync<T>(
            Action<Action> post,
            Func<bool> isUiThread,
            Func<T> work,
            TimeSpan queuedStartBudget,
            CancellationToken cancellationToken)
        {
            if (post == null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            // Reentrant UI-thread callers run inline — they already hold UI access,
            // so no queue admission applies.
            if (isUiThread != null && isUiThread())
            {
                try
                {
                    return Task.FromResult(new UiPostResult<T>(UiPostState.Completed, work()));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new UiPostResult<T>(UiPostState.Faulted, default, ex));
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(new UiPostResult<T>(UiPostState.CancelledNotStarted));
            }

            var completion = new TaskCompletionSource<UiPostResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var claimed = 0;

            var budget = new CancellationTokenSource();
            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budget.Token);
            if (queuedStartBudget != Timeout.InfiniteTimeSpan)
            {
                budget.CancelAfter(queuedStartBudget);
            }

            var registration = linked.Token.Register(() =>
            {
                // Only the first claim decides. If the UI callback claimed the slot
                // before us, the registration fires harmlessly and the callback's
                // own completion still delivers the real outcome.
                if (Interlocked.CompareExchange(ref claimed, 1, 0) == 0)
                {
                    completion.TrySetResult(cancellationToken.IsCancellationRequested
                        ? new UiPostResult<T>(UiPostState.CancelledNotStarted)
                        : new UiPostResult<T>(UiPostState.BusyNotStarted));
                }
            });

            var task = completion.Task;
            _ = task.ContinueWith(
                _ =>
                {
                    registration.Dispose();
                    linked.Dispose();
                    budget.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            try
            {
                post(() =>
                {
                    if (Interlocked.CompareExchange(ref claimed, 1, 0) != 0)
                    {
                        // The slot expired before this queued callback ran — the
                        // caller was already told the work did not start, so running
                        // it now would mutate after a reported failure.
                        return;
                    }

                    try
                    {
                        var value = work();
                        completion.TrySetResult(new UiPostResult<T>(UiPostState.Completed, value));
                    }
                    catch (Exception ex)
                    {
                        completion.TrySetResult(new UiPostResult<T>(UiPostState.Faulted, default, ex));
                    }
                });
            }
            catch (Exception ex)
            {
                // The post itself failed to queue (dispatcher gone, host shutting
                // down). Claim the slot so a queued callback — if any — stays a no-op.
                if (Interlocked.CompareExchange(ref claimed, 1, 0) == 0)
                {
                    completion.TrySetResult(new UiPostResult<T>(UiPostState.NotPosted, default, ex));
                }
            }

            return task;
        }
    }
}
