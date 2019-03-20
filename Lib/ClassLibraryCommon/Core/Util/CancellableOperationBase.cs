﻿//-----------------------------------------------------------------------
// <copyright file="CancellableOperationBase.cs" company="Microsoft">
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
    using System;

    /// <summary>
    /// Represents an operation that supports cancellation. Used by
    /// ICancellableAsyncResult implementations throughout the library.
    /// Also used by AsyncExtensions as a bridge between CancellationToken
    /// and the ICancellableAsyncResult returned by an APM method call.
    /// </summary>
    internal class CancellableOperationBase
    {
        private object cancellationLockerObject = new object();

        internal object CancellationLockerObject
        {
            get { return this.cancellationLockerObject; }
            set { this.cancellationLockerObject = value; }
        }

        private volatile bool cancelRequested = false;

        internal bool CancelRequested
        {
            get { return this.cancelRequested; }
            set { this.cancelRequested = value; }
        }

        internal Action CancelDelegate { get; set; }

        public void Cancel()
        {
            Action cancelDelegate = null;
            lock (this.cancellationLockerObject)
            {
                this.cancelRequested = true;
                if (this.CancelDelegate != null)
                {
                    cancelDelegate = this.CancelDelegate;
                    this.CancelDelegate = null;
                }
            }

            if (cancelDelegate != null)
            {
                cancelDelegate();
            }
        }
    }
}
