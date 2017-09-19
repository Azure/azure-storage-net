//-----------------------------------------------------------------------
// <copyright file="SharedAccessSignatureHelper.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Auth
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Contains helper methods for implementing shared access signatures.
    /// </summary>
    internal static class BlobSharedAccessSignatureHelper
    {
        /// <summary>
        /// Get the complete query builder for creating the Shared Access Signature query.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="headers">The optional header values to set for a blob returned with this SAS.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceType">Either "b" for blobs or "c" for containers.</param>
        /// <param name="signature">The signature to use.</param>
        /// <param name="accountKeyName">The name of the key used to create the signature, or <c>null</c> if the key is implicit.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <returns>The finished query builder.</returns>
        internal static UriQueryBuilder GetSignature(
            SharedAccessBlobPolicy policy,
            SharedAccessBlobHeaders headers,
            string accessPolicyIdentifier,
            string resourceType,
            string signature,
            string accountKeyName,
            string sasVersion,
            SharedAccessProtocol? protocols,
            IPAddressOrRange ipAddressOrRange
            )
        {
            CommonUtility.AssertNotNullOrEmpty("resourceType", resourceType);

            UriQueryBuilder builder = new UriQueryBuilder();

            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedResource, resourceType);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, SharedAccessSignatureHelper.GetProtocolString(protocols));
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, ipAddressOrRange == null ? null : ipAddressOrRange.ToString());

            if (policy != null)
            {
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, SharedAccessSignatureHelper.GetDateTimeOrNull(policy.SharedAccessStartTime));
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, SharedAccessSignatureHelper.GetDateTimeOrNull(policy.SharedAccessExpiryTime));

                string permissions = SharedAccessBlobPolicy.PermissionsToString(policy.Permissions);
                if (!string.IsNullOrEmpty(permissions))
                {
                    SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
                }
            }

            if (headers != null)
            {
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.CacheControl, headers.CacheControl);
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentType, headers.ContentType);
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentEncoding, headers.ContentEncoding);
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentLanguage, headers.ContentLanguage);
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentDisposition, headers.ContentDisposition);
            }

            return builder;
        }

         /// <summary>
        /// Get the signature hash embedded inside the Shared Access Signature.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="headers">The optional header values to set for a blob returned with this SAS.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceName">The canonical resource string, unescaped.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        internal static string GetHash(
            SharedAccessBlobPolicy policy,
            SharedAccessBlobHeaders headers,
            string accessPolicyIdentifier,
            string resourceName,
            string sasVersion,
            SharedAccessProtocol? protocols,
            IPAddressOrRange ipAddressOrRange,
            byte[] keyValue)
        {
            CommonUtility.AssertNotNullOrEmpty("resourceName", resourceName);
            CommonUtility.AssertNotNull("keyValue", keyValue);
            CommonUtility.AssertNotNullOrEmpty("sasVersion", sasVersion);

            string permissions = null;
            DateTimeOffset? startTime = null;
            DateTimeOffset? expiryTime = null;
            if (policy != null)
            {
                permissions = SharedAccessBlobPolicy.PermissionsToString(policy.Permissions);
                startTime = policy.SharedAccessStartTime;
                expiryTime = policy.SharedAccessExpiryTime;
            }

            //// StringToSign =      signedpermissions + "\n" +
            ////                     signedstart + "\n" +
            ////                     signedexpiry + "\n" +
            ////                     canonicalizedresource + "\n" +
            ////                     signedidentifier + "\n" +
            ////                     signedIP + "\n" + 
            ////                     signedProtocol + "\n" + 
            ////                     signedversion + "\n" +
            ////                     cachecontrol + "\n" +
            ////                     contentdisposition + "\n" +
            ////                     contentencoding + "\n" +
            ////                     contentlanguage + "\n" +
            ////                     contenttype 
            ////
            //// HMAC-SHA256(UTF8.Encode(StringToSign))
            ////

            string cacheControl = null;
            string contentDisposition = null;
            string contentEncoding = null;
            string contentLanguage = null;
            string contentType = null;
            if (headers != null)
            {
                cacheControl = headers.CacheControl;
                contentDisposition = headers.ContentDisposition;
                contentEncoding = headers.ContentEncoding;
                contentLanguage = headers.ContentLanguage;
                contentType = headers.ContentType;
            }

            string stringToSign = string.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}\n{11}\n{12}",
                                    permissions,
                                    SharedAccessSignatureHelper.GetDateTimeOrEmpty(startTime),
                                    SharedAccessSignatureHelper.GetDateTimeOrEmpty(expiryTime),
                                    resourceName,
                                    accessPolicyIdentifier,
                                    ipAddressOrRange == null ? string.Empty : ipAddressOrRange.ToString(),
                                    SharedAccessSignatureHelper.GetProtocolString(protocols),
                                    sasVersion,
                                    cacheControl,
                                    contentDisposition,
                                    contentEncoding,
                                    contentLanguage,
                                    contentType);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }
    }
}
