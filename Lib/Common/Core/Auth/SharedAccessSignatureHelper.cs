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
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Contains helper methods for implementing shared access signatures.
    /// </summary>
    internal static class SharedAccessSignatureHelper
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

            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedResource, resourceType);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, GetProtocolString(protocols));
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, ipAddressOrRange == null ? null : ipAddressOrRange.ToString());

            if (policy != null)
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, GetDateTimeOrNull(policy.SharedAccessStartTime));
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, GetDateTimeOrNull(policy.SharedAccessExpiryTime));

                string permissions = SharedAccessBlobPolicy.PermissionsToString(policy.Permissions);
                if (!string.IsNullOrEmpty(permissions))
                {
                    AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
                }
            }

            if (headers != null)
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.CacheControl, headers.CacheControl);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentType, headers.ContentType);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentEncoding, headers.ContentEncoding);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentLanguage, headers.ContentLanguage);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentDisposition, headers.ContentDisposition);
            }

            return builder;
        }

        /// <summary>
        /// Get the complete query builder for creating the Shared Access Signature query.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="headers">The optional header values to set for a file returned with this SAS.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceType">Either "f" for files or "s" for shares.</param>
        /// <param name="signature">The signature to use.</param>
        /// <param name="accountKeyName">The name of the key used to create the signature, or <c>null</c> if the key is implicit.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <returns>The finished query builder.</returns>
        internal static UriQueryBuilder GetSignature(
            SharedAccessFilePolicy policy,
            SharedAccessFileHeaders headers,
            string accessPolicyIdentifier,
            string resourceType,
            string signature,
            string accountKeyName,
            string sasVersion,
            SharedAccessProtocol? protocols,
            IPAddressOrRange ipAddressOrRange)
        {
            CommonUtility.AssertNotNullOrEmpty("resourceType", resourceType);

            UriQueryBuilder builder = new UriQueryBuilder();

            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedResource, resourceType);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, GetProtocolString(protocols));
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, ipAddressOrRange == null ? null : ipAddressOrRange.ToString());

            if (policy != null)
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, GetDateTimeOrNull(policy.SharedAccessStartTime));
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, GetDateTimeOrNull(policy.SharedAccessExpiryTime));

                string permissions = SharedAccessFilePolicy.PermissionsToString(policy.Permissions);
                if (!string.IsNullOrEmpty(permissions))
                {
                    AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
                }
            }

            if (headers != null)
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.CacheControl, headers.CacheControl);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentType, headers.ContentType);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentEncoding, headers.ContentEncoding);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentLanguage, headers.ContentLanguage);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentDisposition, headers.ContentDisposition);
            }

            return builder;
        }

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

            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, GetProtocolString(protocols));
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, ipAddressOrRange == null ? null : ipAddressOrRange.ToString());

            if (policy != null)
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, GetDateTimeOrNull(policy.SharedAccessStartTime));
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, GetDateTimeOrNull(policy.SharedAccessExpiryTime));

                string permissions = SharedAccessQueuePolicy.PermissionsToString(policy.Permissions);
                if (!string.IsNullOrEmpty(permissions))
                {
                    AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
                }
            }

            return builder;
        }

        /// <summary>
        /// Get the complete query builder for creating the Shared Access Signature query.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="tableName">The name of the table associated with this shared access signature.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="startPartitionKey">The start partition key, or <c>null</c>.</param>
        /// <param name="startRowKey">The start row key, or <c>null</c>.</param>
        /// <param name="endPartitionKey">The end partition key, or <c>null</c>.</param>
        /// <param name="endRowKey">The end row key, or <c>null</c>.</param>
        /// <param name="signature">The signature to use.</param>
        /// <param name="accountKeyName">The name of the key used to create the signature, or <c>null</c> if the key is implicit.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <returns>The finished query builder.</returns>
        internal static UriQueryBuilder GetSignature(
            SharedAccessTablePolicy policy,
            string tableName,
            string accessPolicyIdentifier,
            string startPartitionKey,
            string startRowKey,
            string endPartitionKey,
            string endRowKey,
            string signature,
            string accountKeyName,
            string sasVersion,
            SharedAccessProtocol? protocols,
            IPAddressOrRange ipAddressOrRange)
        {          
            CommonUtility.AssertNotNull("signature", signature);

            UriQueryBuilder builder = new UriQueryBuilder();
 
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SasTableName, tableName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.StartPartitionKey, startPartitionKey);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.StartRowKey, startRowKey);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.EndPartitionKey, endPartitionKey);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.EndRowKey, endRowKey);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, GetProtocolString(protocols));
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, ipAddressOrRange == null ? null : ipAddressOrRange.ToString());

            if (policy != null)
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, GetDateTimeOrNull(policy.SharedAccessStartTime));
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, GetDateTimeOrNull(policy.SharedAccessExpiryTime));

                string permissions = SharedAccessTablePolicy.PermissionsToString(policy.Permissions);
                if (!string.IsNullOrEmpty(permissions))
                {
                    AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
                }
            }

            return builder;
        }

        internal static UriQueryBuilder GetSignature(
            SharedAccessAccountPolicy policy,
            string signature,
            string accountKeyName,
            string sasVersion)
        {
            CommonUtility.AssertNotNull("signature", signature);
            CommonUtility.AssertNotNull("policy", policy);

            UriQueryBuilder builder = new UriQueryBuilder();
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedProtocols, policy.Protocols == null ? null : GetProtocolString(policy.Protocols.Value));
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIP, policy.IPAddressOrRange == null ? null : policy.IPAddressOrRange.ToString());
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, GetDateTimeOrNull(policy.SharedAccessStartTime));
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, GetDateTimeOrNull(policy.SharedAccessExpiryTime));

            string resourceTypes = SharedAccessAccountPolicy.ResourceTypesToString(policy.ResourceTypes);
            if (!string.IsNullOrEmpty(resourceTypes))
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedResourceTypes, resourceTypes);
            }

            string services = SharedAccessAccountPolicy.ServicesToString(policy.Services);
            if (!string.IsNullOrEmpty(services))
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedServices, services);
            }

            string permissions = SharedAccessAccountPolicy.PermissionsToString(policy.Permissions);
            if (!string.IsNullOrEmpty(permissions))
            {
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, permissions);
            }

            return builder;
        }

        /// <summary>
        /// Converts the specified value to either a string representation or <see cref="String.Empty"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A string representing the specified value.</returns>
        internal static string GetDateTimeOrEmpty(DateTimeOffset? value)
        {
            string result = GetDateTimeOrNull(value) ?? string.Empty;
            return result;
        }

        /// <summary>
        /// Converts the specified value to either a string representation or <c>null</c>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A string representing the specified value.</returns>
        internal static string GetDateTimeOrNull(DateTimeOffset? value)
        {
            string result = value != null ? value.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) : null;
            return result;
        }

        /// <summary>
        /// Converts the specified value to either a string representation or <c>null</c>.
        /// </summary>
        /// <param name="protocols">The protocols to convert</param>
        /// <returns>A string representing the specified value.</returns>
        internal static string GetProtocolString(SharedAccessProtocol? protocols)
        {
            if (!protocols.HasValue)
            {
                return null;
            }

            if ((protocols.Value != SharedAccessProtocol.HttpsOnly) && (protocols.Value != SharedAccessProtocol.HttpsOrHttp))
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.InvalidProtocolsInSAS, protocols.Value));
            }

            return protocols.Value == SharedAccessProtocol.HttpsOnly ? "https" : "https,http";
        }

        /// <summary>
        /// Escapes and adds the specified name/value pair to the query builder if it is not null.
        /// </summary>
        /// <param name="builder">The builder to add the value to.</param>
        /// <param name="name">The name of the pair.</param>
        /// <param name="value">The value to be escaped.</param>
        internal static void AddEscapedIfNotNull(UriQueryBuilder builder, string name, string value)
        {
            if (value != null)
            {
                builder.Add(name, value);
            }
        }

        /// <summary>
        /// Parses the query.
        /// </summary>
        /// <param name="queryParameters">The query parameters.</param>
        internal static StorageCredentials ParseQuery(IDictionary<string, string> queryParameters)
        {
            bool sasParameterFound = false;
            List<string> removeList = new List<string>();

            foreach (KeyValuePair<string, string> parameter in queryParameters)
            {
                switch (parameter.Key.ToLower())
                {
                    case Constants.QueryConstants.Signature:
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.ResourceType:
                    case Constants.QueryConstants.Component:
                    case Constants.QueryConstants.Snapshot:
                    case Constants.QueryConstants.ApiVersion:
                        removeList.Add(parameter.Key);
                        break;

                    default:
                        break;
                }
            }

            foreach (string removeParam in removeList)
            {
                queryParameters.Remove(removeParam);
            }

            if (sasParameterFound)
            {
                UriQueryBuilder builder = new UriQueryBuilder();
                foreach (KeyValuePair<string, string> parameter in queryParameters)
                {
                    AddEscapedIfNotNull(builder, parameter.Key.ToLower(), parameter.Value);
                }

                return new StorageCredentials(builder.ToString());
            }

            return null;
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
                                     GetDateTimeOrEmpty(startTime),
                                     GetDateTimeOrEmpty(expiryTime),
                                     resourceName,
                                     accessPolicyIdentifier,
                                     ipAddressOrRange == null ? string.Empty : ipAddressOrRange.ToString(),
                                     GetProtocolString(protocols),
                                     sasVersion);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }

        /// <summary>
        /// Get the signature hash embedded inside the Shared Access Signature.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="startPartitionKey">The start partition key, or <c>null</c>.</param>
        /// <param name="startRowKey">The start row key, or <c>null</c>.</param>
        /// <param name="endPartitionKey">The end partition key, or <c>null</c>.</param>
        /// <param name="endRowKey">The end row key, or <c>null</c>.</param>
        /// <param name="resourceName">The canonical resource string, unescaped.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        internal static string GetHash(
            SharedAccessTablePolicy policy,
            string accessPolicyIdentifier,
            string startPartitionKey,
            string startRowKey,
            string endPartitionKey,
            string endRowKey,
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
                permissions = SharedAccessTablePolicy.PermissionsToString(policy.Permissions);
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
            ////                     startpk + "\n" +
            ////                     startrk + "\n" +
            ////                     endpk + "\n" +
            ////                     endrk
            ////
            //// HMAC-SHA256(UTF8.Encode(StringToSign))
            //// 

            string stringToSign = string.Format(
                                     CultureInfo.InvariantCulture,
                                     "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}\n{11}",
                                     permissions,
                                     GetDateTimeOrEmpty(startTime),
                                     GetDateTimeOrEmpty(expiryTime),
                                     resourceName,
                                     accessPolicyIdentifier,
                                     ipAddressOrRange == null ? string.Empty : ipAddressOrRange.ToString(),
                                     GetProtocolString(protocols),
                                     sasVersion,
                                     startPartitionKey,
                                     startRowKey,
                                     endPartitionKey,
                                     endRowKey);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);
            
            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
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
                                    GetDateTimeOrEmpty(startTime),
                                    GetDateTimeOrEmpty(expiryTime),
                                    resourceName,
                                    accessPolicyIdentifier,
                                    ipAddressOrRange == null ? string.Empty : ipAddressOrRange.ToString(),
                                    GetProtocolString(protocols),
                                    sasVersion,
                                    cacheControl,
                                    contentDisposition,
                                    contentEncoding,
                                    contentLanguage,
                                    contentType);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }

        /// <summary>
        /// Get the signature hash embedded inside the Shared Access Signature.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="headers">The optional header values to set for a file returned with this SAS.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceName">The canonical resource string, unescaped.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format.</param>
        /// <param name="protocols">The HTTP/HTTPS protocols for Account SAS.</param>
        /// <param name="ipAddressOrRange">The IP range for IPSAS.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        internal static string GetHash(
            SharedAccessFilePolicy policy,
            SharedAccessFileHeaders headers,
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
                permissions = SharedAccessFilePolicy.PermissionsToString(policy.Permissions);
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
                                    GetDateTimeOrEmpty(startTime),
                                    GetDateTimeOrEmpty(expiryTime),
                                    resourceName,
                                    accessPolicyIdentifier,
                                    ipAddressOrRange == null ? string.Empty : ipAddressOrRange.ToString(),
                                    GetProtocolString(protocols),
                                    sasVersion,
                                    cacheControl,
                                    contentDisposition,
                                    contentEncoding,
                                    contentLanguage,
                                    contentType);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }

        internal static string GetHash(
            SharedAccessAccountPolicy policy,
            string accountName,
            string sasVersion,
            byte[] keyValue)
        {
            string stringToSign = string.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}",
                                    accountName,
                                    SharedAccessAccountPolicy.PermissionsToString(policy.Permissions),
                                    SharedAccessAccountPolicy.ServicesToString(policy.Services),
                                    SharedAccessAccountPolicy.ResourceTypesToString(policy.ResourceTypes),
                                    GetDateTimeOrEmpty(policy.SharedAccessStartTime),
                                    GetDateTimeOrEmpty(policy.SharedAccessExpiryTime),
                                    policy.IPAddressOrRange == null ? string.Empty : policy.IPAddressOrRange.ToString(),
                                    GetProtocolString(policy.Protocols),
                                    sasVersion,
                                    string.Empty);

            Logger.LogVerbose(null /* operationContext */, SR.TraceStringToSign, stringToSign);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }
    }
}
