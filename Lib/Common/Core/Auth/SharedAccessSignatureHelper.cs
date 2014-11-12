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
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Contains helper methods for implementing shared access signatures.
    /// </summary>
    internal static class SharedAccessSignatureHelper
    {
        /// <summary>
        /// Get the complete query builder for creating the Shared Access Signature query.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="headers">The optional header values to set for a blob returned with this SAS. Not valid for the 2012-02-12 version.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceType">Either "b" for blobs or "c" for containers.</param>
        /// <param name="signature">The signature to use.</param>
        /// <param name="accountKeyName">The name of the key used to create the signature, or <c>null</c> if the key is implicit.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <returns>The finished query builder.</returns>
        internal static UriQueryBuilder GetSignature(
            SharedAccessBlobPolicy policy,
            SharedAccessBlobHeaders headers,
            string accessPolicyIdentifier,
            string resourceType,
            string signature,
            string accountKeyName,
            string sasVersion)
        {
            CommonUtility.AssertNotNullOrEmpty("resourceType", resourceType);

            UriQueryBuilder builder = new UriQueryBuilder();

            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedResource, resourceType);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);

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
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="signature">The signature to use.</param>
        /// <param name="accountKeyName">The name of the key used to create the signature, or <c>null</c> if the key is implicit.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <returns>The finished query builder.</returns>
        internal static UriQueryBuilder GetSignature(
            SharedAccessQueuePolicy policy,
            string accessPolicyIdentifier,
            string signature,
            string accountKeyName,
            string sasVersion)
        {
            CommonUtility.AssertNotNull("signature", signature);

            UriQueryBuilder builder = new UriQueryBuilder();

            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, sasVersion);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, accessPolicyIdentifier);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, accountKeyName);
            AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);

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
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
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
            string sasVersion)
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
        /// <param name="mandatorySignedResource">A boolean that represents whether SignedResource is part of Sas or not. True for blobs, False for Queues and Tables.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower", Justification = "ToLower(CultureInfo) is not present in RT and ToLowerInvariant() also violates FxCop")]
        internal static StorageCredentials ParseQuery(IDictionary<string, string> queryParameters, bool mandatorySignedResource)
        {
            string signature = null;
            string signedStart = null;
            string signedExpiry = null;
            string signedResource = null;
            string sigendPermissions = null;
            string signedIdentifier = null;
            string signedVersion = null;
            string signedKey = null;
            string cacheControl = null;
            string contentType = null;
            string contentEncoding = null;
            string contentLanguage = null;
            string contentDisposition = null;
            string tableName = null;
            string startPk = null;
            string startRk = null;
            string endPk = null;
            string endRk = null;

            bool sasParameterFound = false;

            foreach (KeyValuePair<string, string> parameter in queryParameters)
            {
                switch (parameter.Key.ToLower())
                {
                    case Constants.QueryConstants.SignedStart:
                        signedStart = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SignedExpiry:
                        signedExpiry = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SignedPermissions:
                        sigendPermissions = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SignedResource:
                        signedResource = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SignedIdentifier:
                        signedIdentifier = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SignedKey:
                        signedKey = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.Signature:
                        signature = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SignedVersion:
                        signedVersion = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.CacheControl:
                        cacheControl = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.ContentType:
                        contentType = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.ContentEncoding:
                        contentEncoding = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.ContentLanguage:
                        contentLanguage = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.ContentDisposition:
                        contentDisposition = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.SasTableName:
                        tableName = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.StartPartitionKey:
                        startPk = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.StartRowKey:
                        startRk = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.EndPartitionKey:
                        endPk = parameter.Value;
                        sasParameterFound = true;
                        break;

                    case Constants.QueryConstants.EndRowKey:
                        endRk = parameter.Value;
                        sasParameterFound = true;
                        break;

                    default:
                        break;
                }
            }

            if (sasParameterFound)
            {
                if (signature == null || (mandatorySignedResource && signedResource == null))
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.MissingMandatoryParametersForSAS);
                    throw new ArgumentException(errorMessage);
                }

                UriQueryBuilder builder = new UriQueryBuilder();
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedStart, signedStart);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedExpiry, signedExpiry);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedPermissions, sigendPermissions);
                if (signedResource != null)
                {
                    builder.Add(Constants.QueryConstants.SignedResource, signedResource);
                }

                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedIdentifier, signedIdentifier);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedVersion, signedVersion);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SignedKey, signedKey);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.Signature, signature);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.CacheControl, cacheControl);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentType, contentType);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentEncoding, contentEncoding);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentLanguage, contentLanguage);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.ContentDisposition, contentDisposition);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.SasTableName, tableName);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.StartPartitionKey, startPk);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.StartRowKey, startRk);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.EndPartitionKey, endPk);
                AddEscapedIfNotNull(builder, Constants.QueryConstants.EndRowKey, endRk);
               
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
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Microsoft.WindowsAzure.Storage.Core.Util.CryptoUtility.ComputeHmac256(System.Byte[],System.String)", Justification = "Reviewed")]
        internal static string GetHash(
            SharedAccessQueuePolicy policy,
            string accessPolicyIdentifier,
            string resourceName,
            string sasVersion,
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
            ////                     signedversion
            ////
            //// HMAC-SHA256(UTF8.Encode(StringToSign))

            string stringToSign = string.Format(
                                     CultureInfo.InvariantCulture,
                                     "{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
                                     permissions,
                                     GetDateTimeOrEmpty(startTime),
                                     GetDateTimeOrEmpty(expiryTime),
                                     resourceName,
                                     accessPolicyIdentifier,
                                     sasVersion);

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
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Microsoft.WindowsAzure.Storage.Core.Util.CryptoUtility.ComputeHmac256(System.Byte[],System.String)", Justification = "Reviewed")]
        internal static string GetHash(
            SharedAccessTablePolicy policy,
            string accessPolicyIdentifier,
            string startPartitionKey,
            string startRowKey,
            string endPartitionKey,
            string endRowKey,
            string resourceName,
            string sasVersion,
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
            ////                     signedversion + "\n" +
            ////                     startpk + "\n" +
            ////                     startrk + "\n" +
            ////                     endpk + "\n" +
            ////                     endrk
            ////
            //// HMAC-SHA256(UTF8.Encode(StringToSign))

            string stringToSign = string.Format(
                                     CultureInfo.InvariantCulture,
                                     "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}",
                                     permissions,
                                     GetDateTimeOrEmpty(startTime),
                                     GetDateTimeOrEmpty(expiryTime),
                                     resourceName,
                                     accessPolicyIdentifier,
                                     sasVersion,
                                     startPartitionKey,
                                     startRowKey,
                                     endPartitionKey,
                                     endRowKey);

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }

        /// <summary>
        /// Get the signature hash embedded inside the Shared Access Signature.
        /// </summary>
        /// <param name="policy">The shared access policy to hash.</param>
        /// <param name="headers">The optional header values to set for a blob returned with this SAS. Not valid for the 2012-02-12 version.</param>
        /// <param name="accessPolicyIdentifier">An optional identifier for the policy.</param>
        /// <param name="resourceName">The canonical resource string, unescaped.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <param name="keyValue">The key value retrieved as an atomic operation used for signing.</param>
        /// <returns>The signed hash.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Microsoft.WindowsAzure.Storage.Core.Util.CryptoUtility.ComputeHmac256(System.Byte[],System.String)", Justification = "Reviewed")]
        internal static string GetHash(
            SharedAccessBlobPolicy policy,
            SharedAccessBlobHeaders headers,
            string accessPolicyIdentifier,
            string resourceName,
            string sasVersion,
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
            ////                     signedversion + "\n" +
            ////                     cachecontrol + "\n" +
            ////                     contentdisposition + "\n" +
            ////                     contentencoding + "\n" +
            ////                     contentlanguage + "\n" +
            ////                     contenttype 
            ////
            //// HMAC-SHA256(UTF8.Encode(StringToSign))
            ////
            //// Note that the final five headers are invalid for the 2012-02-12 version.

            string stringToSign = string.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
                                    permissions,
                                    GetDateTimeOrEmpty(startTime),
                                    GetDateTimeOrEmpty(expiryTime),
                                    resourceName,
                                    accessPolicyIdentifier,
                                    sasVersion);

            if (string.Equals(sasVersion, Constants.VersionConstants.February2012))
            {
                if (headers != null)
                {
                    string errorString = string.Format(CultureInfo.CurrentCulture, SR.InvalidHeaders);
                    throw new ArgumentException(errorString);
                }
            }
            else
            {
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

                stringToSign = stringToSign + string.Format(
                                                CultureInfo.InvariantCulture,
                                                "\n{0}\n{1}\n{2}\n{3}\n{4}",
                                                cacheControl,
                                                contentDisposition,
                                                contentEncoding,
                                                contentLanguage,
                                                contentType);
            }

            return CryptoUtility.ComputeHmac256(keyValue, stringToSign);
        }

        /// <summary>
        /// Check to see if the SAS Version string provided is valid.
        /// </summary>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <returns>The valid SAS version string or the default (storage version) if given was null/invalid.</returns>
        internal static string ValidateSASVersionString(string sasVersion)
        {
            if (sasVersion == null)
            {
                return Constants.HeaderConstants.TargetStorageVersion;
            }
            else if (string.Equals(sasVersion, Constants.VersionConstants.August2013))
            {
                return Constants.VersionConstants.August2013;
            }
            else if (string.Equals(sasVersion, Constants.VersionConstants.February2012))
            {
                return Constants.VersionConstants.February2012;
            }
            else
            {
                string errorString = string.Format(CultureInfo.CurrentCulture, SR.InvalidSASVersion);
                throw new ArgumentException(errorString);
            }
        }
    }
}
