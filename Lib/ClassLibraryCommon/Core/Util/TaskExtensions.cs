//-----------------------------------------------------------------------
// <copyright file="TaskExtensions.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

#if TASK

    internal static class TaskExtensions
    {
        /// <summary>
        ///  Extension method to add cancellation logic to non-cancellable operations.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to enable cancellation on.</param>
        /// <param name="cancellationToken"> the cancellation token which will be used to cancel the combined task </param>
        /// <remarks>Please refer to this post for more information: https://blogs.msdn.microsoft.com/pfxteam/2012/10/05/how-do-i-cancel-non-cancelable-async-operations/ </remarks>
        internal static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                        taskCompletionSource => ((TaskCompletionSource<bool>)taskCompletionSource).TrySetResult(true), tcs))
                        if (task != await Task.WhenAny(task, tcs.Task))
                            throw new OperationCanceledException(cancellationToken);
            return await task;
        }
    }

#endif
}
