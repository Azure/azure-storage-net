﻿// -----------------------------------------------------------------------------------------
// <copyright file="QueueHttpResponseParsers.Common.cs" company="Microsoft">
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.Azure.Storage.Queue.Protocol
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

#if WINDOWS_RT
    internal
#else
    public
#endif
        static partial class QueueHttpResponseParsers
    {
        /// <summary>
        /// Reads service properties from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service properties.</param>
        /// <returns>The service properties stored in the stream.</returns>
        public static Task<ServiceProperties> ReadServicePropertiesAsync(Stream inputStream, CancellationToken token)
        {
            return HttpResponseParsers.ReadServicePropertiesAsync(inputStream, token);
        }

        /// <summary>
        /// Reads service stats from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service stats.</param>
        /// <returns>The service stats stored in the stream.</returns>
        public static Task<ServiceStats> ReadServiceStatsAsync(Stream inputStream, CancellationToken token)
        {
            return HttpResponseParsers.ReadServiceStatsAsync(inputStream, token);
        }

        /// <summary>
        /// Reads the share access policies from a stream in XML.
        /// </summary>
        /// <param name="inputStream">The stream of XML policies.</param>
        /// <param name="permissions">The permissions object to which the policies are to be written.</param>
        public static Task ReadSharedAccessIdentifiersAsync(Stream inputStream, QueuePermissions permissions, CancellationToken token)
        {
            CommonUtility.AssertNotNull("permissions", permissions);

            return Response.ReadSharedAccessIdentifiersAsync(permissions.SharedAccessPolicies, new QueueAccessPolicyResponse(inputStream), token);
        }
    }
}
