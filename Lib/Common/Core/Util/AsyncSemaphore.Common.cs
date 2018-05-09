//-----------------------------------------------------------------------
// <copyright file="AsyncSemaphore.Common.cs" company="Microsoft">
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Core.Util
{
    internal partial class AsyncSemaphore
    {
        private int count;

        public AsyncSemaphore(int initialCount)
        {
            CommonUtility.AssertInBounds("initialCount", initialCount, 0, int.MaxValue);
            this.count = initialCount;
        }
    }

    internal partial class AsyncSemaphoreAsync
    {
        private int count;
        public AsyncSemaphoreAsync(int initialCount)
        {
            CommonUtility.AssertInBounds("initialCount", initialCount, 0, int.MaxValue);
            this.count = initialCount;
        }
    }


    /// <summary>
    /// This class provides asynchronous semaphore functionality (based on Stephen Toub's blog https://blogs.msdn.microsoft.com/pfxteam/2012/02/12/building-async-coordination-primitives-part-5-asyncsemaphore/).
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed - Stephen Toub is a proper noun.")]
    internal partial class AsyncSemaphoreAsync
    {
        // Callers assume FIFO functionality here.
        // This is useful to restrict the number of simultaneous uploads and total amount of data the stream can buffer.
        // Callbacks are queued such that when a specific one returns, it's a signal that the stream can accept more data.
        // If we don't have FIFO ordering, this signal may occur at the wrong time.
        // Also, it ensures that append blob blocks are appended in the correct order.
        private readonly Queue<Func<bool, Task>> pendingWaits =
            new Queue<Func<bool, Task>>();

        public async Task<bool> WaitAsync(Func<bool, CancellationToken, Task> callback, CancellationToken token)
        {
            CommonUtility.AssertNotNull("callback", callback);
            bool queued = false;
            lock (this.pendingWaits)
            {
                if (this.count > 0)
                {
                    this.count--;
                }
                else
                {
                    this.pendingWaits.Enqueue((bool calledInline) => callback(calledInline, CancellationToken.None));
                    queued = true;
                }
            }

            if (!queued)
            {
                await callback(true, token).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        public async Task ReleaseAsync(CancellationToken token)
        {
            Func<bool, Task> next = null;
            lock (this.pendingWaits)
            {
                if (this.pendingWaits.Count > 0)
                {
                    next = this.pendingWaits.Dequeue();
                }
                else
                {
                    this.count++;
                }
            }

            if (next != null)
            {
                await next(false).ConfigureAwait(false);
            }
        }
    }
}
