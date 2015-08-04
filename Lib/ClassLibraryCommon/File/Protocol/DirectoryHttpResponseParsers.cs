//-----------------------------------------------------------------------
// <copyright file="DirectoryHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Provides methods for parsing responses to operations on directories in the File service.
    /// </summary>
    public static partial class DirectoryHttpResponseParsers
    {
        /// <summary>
        /// Gets the request ID from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A unique value associated with the request.</returns>
        public static string GetRequestId(HttpWebResponse response)
        {
            return Response.GetRequestId(response);
        }

        /// <summary>
        /// Gets the directory's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The directory's attributes.</returns>
        public static FileDirectoryProperties GetProperties(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            FileDirectoryProperties directoryProperties = new FileDirectoryProperties();
            directoryProperties.ETag = HttpResponseParsers.GetETag(response);

#if WINDOWS_PHONE 
            directoryProperties.LastModified = HttpResponseParsers.GetLastModified(response);
#else
            directoryProperties.LastModified = response.LastModified.ToUniversalTime();
#endif

            return directoryProperties;
        }

        /// <summary>
        /// Gets the user-defined metadata.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <returns>A <see cref="System.Collections.IDictionary"/> of the metadata.</returns>
        public static IDictionary<string, string> GetMetadata(HttpWebResponse response)
        {
            return HttpResponseParsers.GetMetadata(response);
        }
    }
}
