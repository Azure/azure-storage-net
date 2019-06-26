// -----------------------------------------------------------------------------------------
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.Azure.Storage.File.Protocol
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    public static partial class DirectoryHttpResponseParsers
    {
        /// <summary>
        /// Gets the directory's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The directory's attributes.</returns>
        public static FileDirectoryProperties GetProperties(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);

            // Set the directory properties
            FileDirectoryProperties directoryProperties = new FileDirectoryProperties();
            directoryProperties.ETag = (response.Headers.ETag == null) ? null :
                response.Headers.ETag.ToString();
            string directoryEncryption = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ServerEncrypted);
            directoryProperties.IsServerEncrypted = string.Equals(directoryEncryption, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

            if (response.Content != null)
            {
                directoryProperties.LastModified = response.Content.Headers.LastModified;
            }
            else
            {
                directoryProperties.LastModified = null;
            }

            return directoryProperties;
        }

        /// <summary>
        /// Sets the SMB related file properties.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <param name="properties">The properties to modify.</param>
        public static void UpdateSmbProperties(HttpResponseMessage response, FileDirectoryProperties properties)
        {
            properties.filePermissionKey = HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FilePermissionKey);
            properties.ntfsAttributes = CloudFileNtfsAttributesHelper.ToAttributes(HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FileAttributes));
            properties.creationTime = DateTimeOffset.Parse(HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FileCreationTime));
            properties.lastWriteTime = DateTimeOffset.Parse(HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FileLastWriteTime));
            properties.ChangeTime = DateTimeOffset.Parse(HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FileChangeTime));
            properties.DirectoryId = HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FileId);
            properties.ParentId = HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.FileParentId);

            properties.filePermissionKeyToSet = null;
            properties.ntfsAttributesToSet = null;
            properties.creationTimeToSet = null;
            properties.lastWriteTimeToSet = null;
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
