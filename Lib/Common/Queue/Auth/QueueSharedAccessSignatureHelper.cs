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

namespace Microsoft.Azure.Storage.Core.Auth
{
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Contains helper methods for implementing shared access signatures.
    /// </summary>
    internal static class QueueSharedAccessSignatureHelper
    {
        /// <summary>
        /// Get the complete query builder for creating the Shared Access Signature query.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="signature">The signature to use.</param>
        /// <param name="accountKeyName">The name of the key used to create the signature, or <c>null</c> if the key is implicit.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <returns>The finished query builder.</returns>
        internal static UriQueryBuilder GetSignature(
            SharedAccessQueuePolicy policy,
            string accessPolicyIdentifier,
            string signature,
            string accountKeyName,
            string sasVersion,
            SharedAccessProtocol? protocols,
            IPAddressOrRange ipAddressOrRange)
        {
            CommonUtility.AssertNotNull("signature", signature);

            UriQueryBuilder builder = new UriQueryBuilder();

            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, SharedAccessSignatureHelper.GetProtocolString(protocols));
            SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, ipAddressOrRange == null ? null : ipAddressOrRange.ToString());

            if (policy != null)
            {
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, SharedAccessSignatureHelper.GetDateTimeOrNull(policy.SharedAccessStartTime));
                SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, SharedAccessSignatureHelper.GetDateTimeOrNull(policy.SharedAccessExpiryTime));

                string permissions = SharedAccessQueuePolicy.PermissionsToString(policy.Permissions);
                if (!string.IsNullOrEmpty(permissions))
                {
                    SharedAccessSignatureHelper.AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
                }
            }

            return builder;
        }

        /// <summary>
        /// Get the signature hash embedded inside the Shared Access Signature.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceName">The canonical resource string, unescaped.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        internal static string GetHash(
            SharedAccessQueuePolicy policy,
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
                permissions = SharedAccessQueuePolicy.PermissionsToString(policy.Permissions);
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
            ////                     signedversion
            ////
            //// HMAC-SHA256(UTF8.Encode(StringToSign))
            ////

            string stringToSign = string.Format(
                                     CultureInfo.InvariantCulture,
                                     "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}",
                                     permissions,
                                     SharedAccessSignatureHelper.GetDateTimeOrEmpty(startTime),
                                     SharedAccessSignatureHelper.GetDateTimeOrEmpty(expiryTime),
                                     resourceName,
                                     accessPolicyIdentifier,
                                     ipAddressOrRange == null ? string.Empty : ipAddressOrRange.ToString(),
                                     SharedAccessSignatureHelper.GetProtocolString(protocols),
                                     sasVersion);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }
    }
}
