// -----------------------------------------------------------------------------------------
// <copyright file="ContainerHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;


    public static partial class ContainerHttpResponseParsers
    {
        /// <summary>
        /// Reads account properties from a HttpWebResponse.
        /// </summary>
        /// <param name="response">The HttpWebResponse from which to read the account properties.</param>
        /// <returns>The account properties stored in the headers.</returns>
        public static AccountProperties ReadAccountProperties(HttpResponseMessage response)
        {
            return HttpResponseParsers.ReadAccountProperties(response);
        }

        /// <summary>
        /// Gets the container's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The container's attributes.</returns>
        public static BlobContainerProperties GetProperties(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);

            // Set the container properties
            BlobContainerProperties containerProperties = new BlobContainerProperties();
            containerProperties.ETag = (response.Headers.ETag == null) ? null :
                response.Headers.ETag.ToString();

            containerProperties.LastModified = response?.Content?.Headers?.LastModified;

            // Get lease properties
            containerProperties.LeaseStatus = BlobHttpResponseParsers.GetLeaseStatus(response);
            containerProperties.LeaseState = BlobHttpResponseParsers.GetLeaseState(response);
            containerProperties.LeaseDuration = BlobHttpResponseParsers.GetLeaseDuration(response);

            // Reading public access
            containerProperties.PublicAccess = GetAcl(response);

            // WORM policies
            string hasImmutability = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.HasImmutabilityPolicyHeader);
            containerProperties.HasImmutabilityPolicy = string.IsNullOrEmpty(hasImmutability) ? (bool?)null : bool.Parse(hasImmutability);

            string hasLegalHold = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.HasLegalHoldHeader);
            containerProperties.HasLegalHold = string.IsNullOrEmpty(hasLegalHold) ? (bool?)null : bool.Parse(hasLegalHold);

            string defaultEncryptionScope = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.DefaultEncryptionScopeHeader);
            if (!string.IsNullOrEmpty(defaultEncryptionScope))
            {
                containerProperties.EncryptionScopeOptions = new BlobContainerEncryptionScopeOptions();
                containerProperties.EncryptionScopeOptions.DefaultEncryptionScope = defaultEncryptionScope;

                string preventEncryptionScopeOverride = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.PreventEncryptionScopeOverrideHeader);
                containerProperties.EncryptionScopeOptions.PreventEncryptionScopeOverride = 
                    string.Equals(preventEncryptionScopeOverride, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);
            }

            return containerProperties;
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
        /// Gets the ACL for the container from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A value indicating the public access level for the container.</returns>
        public static BlobContainerPublicAccessType GetAcl(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);
            string acl = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobPublicAccess);
            return GetContainerAcl(acl);
        }
    }
}
