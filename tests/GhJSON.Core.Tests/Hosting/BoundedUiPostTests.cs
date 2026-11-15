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
using System.Threading;
using System.Threading.Tasks;
using GhJSON.Core.Hosting;
using Xunit;

namespace GhJSON.Core.Tests.Hosting
{
    /// <summary>
    /// Host-independent coverage for <see cref="BoundedUiPost"/>: the post delegate
    /// is a hand-driven queue so every ownership transition is deterministic.
    /// </summary>
    public sealed class BoundedUiPostTests
    {
        /// <summary>A controllable stand-in for a host UI queue.</summary>
        private sealed class FakeUiQueue
        {
            private readonly Queue<Action> pending = new Queue<Action>();

            public bool IsUiThread { get; set; }

            public int PostedCount => this.pending.Count;

            public void Post(Action callback)
            {
                this.pending.Enqueue(callback);
            }

            /// <summary>Runs the next queued callback (a "UI pass").</summary>
            public bool RunNext()
            {
                if (this.pending.Count == 0)
                {
                    return false;
                }

                this.pending.Dequeue()();
                return true;
            }
        }

        [Fact]
        public async Task PostedWork_RunsAndReturnsValue()
        {
            var queue = new FakeUiQueue();
            var task = BoundedUiPost.PostAsync(queue.Post, () => queue.IsUiThread, () => 42, TimeSpan.FromSeconds(5), CancellationToken.None);
            Assert.True(queue.RunNext());
            var result = await task;
            Assert.Equal(UiPostState.Completed, result.State);
            Assert.True(result.Started);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public async Task FaultedWork_ReportsErrorAndCountsAsStarted()
        {
            var queue = new FakeUiQueue();
            var task = BoundedUiPost.PostAsync<int>(queue.Post, () => queue.IsUiThread, () => throw new InvalidOperationException("boom"), TimeSpan.FromSeconds(5), CancellationToken.None);
            Assert.True(queue.RunNext());
            var result = await task;
            Assert.Equal(UiPostState.Faulted, result.State);
            Assert.True(result.Started);
            Assert.IsType<InvalidOperationException>(result.Error);
        }

        [Fact]
        public async Task ExpiredQueuedStart_CallbackNeverRuns()
        {
            var queue = new FakeUiQueue();
            var ran = false;
            var task = BoundedUiPost.PostAsync(
                queue.Post,
                () => queue.IsUiThread,
                () =>
                {
                    ran = true;
                    return true;
                },
                TimeSpan.FromMilliseconds(20),
                CancellationToken.None);

            var result = await task;
            Assert.Equal(UiPostState.BusyNotStarted, result.State);
            Assert.False(result.Started);

            // A late UI pass must not run work that was already reported as not started.
            Assert.True(queue.RunNext());
            Assert.False(ran);
        }

        [Fact]
        public async Task CancelledBeforeStart_ReportsCancelledNotStarted()
        {
            var queue = new FakeUiQueue();
            using var cts = new CancellationTokenSource();
            var ran = false;
            var task = BoundedUiPost.PostAsync(
                queue.Post,
                () => queue.IsUiThread,
                () =>
                {
                    ran = true;
                    return true;
                },
                TimeSpan.FromMinutes(1),
                cts.Token);

            // Give the registration a moment to arm, then cancel before the UI pass.
            await Task.Delay(20);
            cts.Cancel();
            var result = await task;
            Assert.Equal(UiPostState.CancelledNotStarted, result.State);
            Assert.False(result.Started);
            Assert.True(queue.RunNext());
            Assert.False(ran);
        }

        [Fact]
        public async Task StartedWork_OutlivesCallerCancellation()
        {
            var queue = new FakeUiQueue();
            using var cts = new CancellationTokenSource();
            var release = new ManualResetEventSlim(false);
            var ran = false;
            var task = BoundedUiPost.PostAsync(
                queue.Post,
                () => queue.IsUiThread,
                () =>
                {
                    ran = true;
                    release.Wait(TimeSpan.FromSeconds(5));
                    return 7;
                },
                TimeSpan.FromSeconds(5),
                cts.Token);

            // The UI pass claims the slot and blocks inside the work.
            var uiPass = Task.Run(() => queue.RunNext());
            Assert.True(SpinWait.SpinUntil(() => ran, TimeSpan.FromSeconds(5)));

            // Caller cancellation cannot retract started work; the waiter still
            // observes the real outcome.
            cts.Cancel();
            Assert.False(task.IsCompleted);
            release.Set();
            var result = await task;
            Assert.Equal(UiPostState.Completed, result.State);
            Assert.True(ran);
            Assert.Equal(7, result.Value);
        }

        [Fact]
        public async Task ReentrantUiThread_RunsInline()
        {
            var queue = new FakeUiQueue { IsUiThread = true };
            var result = await BoundedUiPost.PostAsync(queue.Post, () => queue.IsUiThread, () => "inline", TimeSpan.FromMilliseconds(1), CancellationToken.None);
            Assert.Equal(UiPostState.Completed, result.State);
            Assert.Equal("inline", result.Value);
            Assert.Equal(0, queue.PostedCount);
        }

        [Fact]
        public async Task PostFailure_ReportsNotPosted()
        {
            Action<Action> failingPost = _ => throw new InvalidOperationException("dispatcher gone");
            var ran = false;
            var result = await BoundedUiPost.PostAsync(
                failingPost,
                () => false,
                () =>
                {
                    ran = true;
                    return true;
                },
                TimeSpan.FromSeconds(5),
                CancellationToken.None);
            Assert.Equal(UiPostState.NotPosted, result.State);
            Assert.False(result.Started);
            Assert.False(ran);
            Assert.IsType<InvalidOperationException>(result.Error);
        }

        [Fact]
        public async Task AlreadyCancelled_NeverPosts()
        {
            var queue = new FakeUiQueue();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var result = await BoundedUiPost.PostAsync(queue.Post, () => queue.IsUiThread, () => true, TimeSpan.FromSeconds(5), cts.Token);
            Assert.Equal(UiPostState.CancelledNotStarted, result.State);
            Assert.Equal(0, queue.PostedCount);
        }
    }
}
