//-----------------------------------------------------------------------
// <copyright file="CounterEvent.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
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

namespace Microsoft.Azure.Storage.Core.Util
{
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class CounterEventAsync
    {
        private AsyncManualResetEvent internalEvent = new AsyncManualResetEvent(true);
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private int counter = 0;

        /// <summary>
        /// Increments the counter by one and thus sets the state of the event to non-signaled, causing threads to block.
        /// </summary>
        public void Increment()
        {
            semaphoreSlim.Wait();
            try
            {
                this.counter++;
                this.internalEvent.Reset();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Decrements the counter by one. If the counter reaches zero, sets the state of the event to signaled, allowing one or more waiting threads to proceed.
        /// </summary>
        public async Task DecrementAsync()
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (--this.counter == 0)
                {
                    await this.internalEvent.Set().ConfigureAwait(false);
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Blocks the current thread until the CounterEvent is set.
        /// </summary>
        public Task WaitAsync()
        {
            return this.internalEvent.WaitAsync();
        }
    }
}
