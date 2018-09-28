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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
#if ALL_SERVICES
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
#endif
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
                    case Constants.QueryConstants.ShareSnapshot:
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