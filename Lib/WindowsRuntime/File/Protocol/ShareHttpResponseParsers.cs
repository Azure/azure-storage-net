// -----------------------------------------------------------------------------------------
// <copyright file="ShareHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Net.Http;

    /// <summary>
    /// Provides a set of methods for parsing share responses from the File service.
    /// </summary>
    internal static partial class ShareHttpResponseParsers
    {
        /// <summary>
        /// Gets the share's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The share's attributes.</returns>
        public static FileShareProperties GetProperties(HttpResponseMessage response)
        {
            // Set the share properties
            FileShareProperties shareProperties = new FileShareProperties();
            shareProperties.ETag = (response.Headers.ETag == null) ? null :
                response.Headers.ETag.ToString();

            if (response.Content != null)
            {
                shareProperties.LastModified = response.Content.Headers.LastModified;
            }
            else
            {
                shareProperties.LastModified = null;
            }

            return shareProperties;
        }

        /// <summary>
        /// Gets the user-defined metadata.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <returns>A <see cref="IDictionary"/> of the metadata.</returns>
        public static IDictionary<string, string> GetMetadata(HttpResponseMessage response)
        {
            return HttpResponseParsers.GetMetadata(response);
        }
    }
}
