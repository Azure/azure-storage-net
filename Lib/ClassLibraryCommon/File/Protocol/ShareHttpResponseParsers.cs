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

namespace Microsoft.Azure.Storage.File.Protocol
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Provides a set of methods for parsing share responses from the File service.
    /// </summary>
    public static partial class ShareHttpResponseParsers
    {
        /// <summary>
        /// Gets the share's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The share's attributes.</returns>
        public static FileShareProperties GetProperties(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);

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

            string quota = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ShareQuota);
            if (!string.IsNullOrEmpty(quota))
            {
                shareProperties.Quota = int.Parse(quota, CultureInfo.InvariantCulture);
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

        /// <summary>
        /// Reads the share access policies from a stream in XML.
        /// </summary>
        /// <param name="inputStream">The stream of XML policies.</param>
        /// <param name="permissions">The permissions object to which the policies are to be written.</param>
        public static Task ReadSharedAccessIdentifiersAsync(Stream inputStream, FileSharePermissions permissions, CancellationToken token)
        {
            CommonUtility.AssertNotNull("permissions", permissions);

            return Response.ReadSharedAccessIdentifiersAsync(permissions.SharedAccessPolicies, new FileAccessPolicyResponse(inputStream), token);
        }

        /// <summary>
        /// Gets the snapshot timestamp from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The snapshot timestamp.</returns>
        public static string GetSnapshotTime(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);
            return response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.SnapshotHeader);
        }

        /// <summary>
        /// Reads share stats from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the share stats.</param>
        /// <returns>The share stats stored in the stream.</returns>
        public static Task<ShareStats> ReadShareStatsAsync(Stream inputStream, CancellationToken token)
        {
            return Task.Run(
                () =>
                {
                    using (XmlReader reader = XmlReader.Create(inputStream))
                    {
                        XDocument shareStatsDocument = XDocument.Load(reader);

                        return ShareStats.FromServiceXml(shareStatsDocument);
                    }
                },
                token
                );
        }
    }
}
