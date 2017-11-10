//-----------------------------------------------------------------------
// <copyright file="CommonUtility.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.File;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Xml;

#if WINDOWS_DESKTOP
    using System.Net;
    using Microsoft.Azure.Storage.Shared.Protocol;
#endif

    internal static class FileCommonUtility
    {
        /// <summary>
        /// Create an ExecutionState object that can be used for pre-request operations
        /// such as buffering user's data.
        /// </summary>
        /// <param name="options">Request options</param>
        /// <returns>Temporary ExecutionState object</returns>
        internal static ExecutionState<NullType> CreateTemporaryExecutionState(FileRequestOptions options)
        {
            RESTCommand<NullType> cmdWithTimeout = new RESTCommand<NullType>(new StorageCredentials(), null /* Uri */);
            if (options != null)
            {
                options.ApplyToStorageCommand(cmdWithTimeout);
            }

            return new ExecutionState<NullType>(cmdWithTimeout, options != null ? options.RetryPolicy : null, new OperationContext());
        }
    }
}
