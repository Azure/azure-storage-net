//-----------------------------------------------------------------------
// <copyright file="StorageCommandBase.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Executor
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;

#if WINDOWS_RT || NETCORE
    using System.Net.Http;
#endif

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
    internal abstract class StorageCommandBase<T>
    {
        // Server Timeout to send
        public int? ServerTimeoutInSeconds = null;

        // Max client timeout, enforced over entire operation on client side
        internal DateTime? OperationExpiryTime = null;

        // State- different than async state, this is used for ops to communicate state between invocations, i.e. bytes downloaded etc
        internal object OperationState = null;

        // Used to keep track of Md5 / Length of a stream as it is being copied
        private volatile StreamDescriptor streamCopyState = null;

        internal StreamDescriptor StreamCopyState
        {
            get { return this.streamCopyState; }
            set { this.streamCopyState = value; }
        }

        private volatile RequestResult currentResult = null;

        internal RequestResult CurrentResult
        {
            get { return this.currentResult; }
            set { this.currentResult = value; }
        }

        private IList<RequestResult> requestResults = new List<RequestResult>();

        internal IList<RequestResult> RequestResults
        {
            get { return this.requestResults; }
        }

        // Delegate that will be executed in the event of an Exception after signing.
        public Action<StorageCommandBase<T>, Exception, OperationContext> RecoveryAction = null;

#if WINDOWS_RT || NETCORE
        public Func<Stream, HttpResponseMessage, string, StorageExtendedErrorInformation> ParseError = null;
#else
        // Delegate that will be executed in the event of a failure.
        public Func<Stream, HttpWebResponse, string, StorageExtendedErrorInformation> ParseError = null;
#endif
        // Delegate that will be executed in the event of a failure while using the WCF Data Services Client.
        public Func<Stream, IDictionary<string, string>, string, StorageExtendedErrorInformation> ParseDataServiceError = null;
    }
}
