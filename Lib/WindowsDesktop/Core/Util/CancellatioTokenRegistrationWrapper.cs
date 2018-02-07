//-----------------------------------------------------------------------
// <copyright file="CancellableAsyncResultTaskWrapper.cs" company="Microsoft">
//    Copyright 2016 Microsoft Corporation
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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    /// <summary>
    /// This class wraps a CancellationTokenRegistration inorder to enable GC to unregister the registered callback
    /// if it has been registered
    /// </summary>
    internal class CancellationTokenRegistrationWrapper : IDisposable
    {
        private CancellationTokenRegistration? wrappedRegistration;
        private bool disposed = false;

        public CancellationTokenRegistrationWrapper(CancellationTokenRegistration? cancellationTokenRegisteration)
        {
            wrappedRegistration = cancellationTokenRegisteration;
        }

        /// <summary>
        /// Cleans up references.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (wrappedRegistration.HasValue)
            {
                wrappedRegistration.Value.Dispose();
            }
          
            disposed = true;
        }
    }
}