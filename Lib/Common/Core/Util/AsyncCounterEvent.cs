//-----------------------------------------------------------------------
// <copyright file="AsyncCounterEvent.cs" company="Microsoft">
//    Copyright 2017 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class AsyncCounterEvent
    {
        private TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        private int counter = 0;

        public AsyncCounterEvent()
        {
            taskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Increments the counter by one and thus sets the state of the event to non-signaled, causing threads to block.
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref counter);
            this.Reset();
        }

        /// <summary>
        /// Decrements the counter by one. If the counter reaches zero, sets the state of the event to signaled, allowing one or more waiting threads to proceed.
        /// </summary>
        public void Decrement()
        {
            if (Interlocked.Decrement(ref counter) == 0)
            {
                taskCompletionSource.TrySetResult(true);
            }
        }

        /// <summary>
        /// Asynchronously waits until the CounterEvent is set.
        /// </summary>
        public Task WaitAsync()
        {
            return this.taskCompletionSource.Task;
        }

        private void Reset()
        {
            while (true)
            {
                var tcs = taskCompletionSource;
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref taskCompletionSource, new TaskCompletionSource<bool>(), tcs) == tcs)
                    return;
            }
        }
    }
}
