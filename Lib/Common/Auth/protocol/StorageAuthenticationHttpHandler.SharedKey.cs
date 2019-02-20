// -----------------------------------------------------------------------------------------
// <copyright file="StorageAuthenticationHttpHandler.SharedKey.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Auth.Protocol
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Auth;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    internal partial class StorageAuthenticationHttpHandler
    {
        private Task<HttpResponseMessage> GetSharedKeyAuthenticationTask(StorageRequestMessage request, CancellationToken cancellationToken)
        { 
            AddDateHeader(request);

            AddSharedKeyAuth(request);

            return base.SendAsync(request, cancellationToken);
        }

        internal static void AddDateHeader(StorageRequestMessage request)
        {
            if (!request.Headers.Contains(Constants.HeaderConstants.Date))
            {
                string dateString = HttpWebUtility.ConvertDateTimeToHttpString(DateTimeOffset.UtcNow);
                request.Headers.Add(Constants.HeaderConstants.Date, dateString);
            }
        }

        internal static void AddSharedKeyAuth(StorageRequestMessage request)
        {
            string accountName = request.AccountName;
            StorageCredentials credentials = request.Credentials;
            ICanonicalizer canonicalizer = request.Canonicalizer;

            if (credentials.IsSharedKey)
            {
                StorageAccountKey accountKey = credentials.Key;
                if (!string.IsNullOrEmpty(accountKey.KeyName))
                {
                    request.Headers.Add(Constants.HeaderConstants.KeyNameHeader, accountKey.KeyName);
                }

                string message = canonicalizer.CanonicalizeHttpRequest(request, accountName);
                string signature = CryptoUtility.ComputeHmac256(accountKey.KeyValue, message);

                request.Headers.Authorization = new AuthenticationHeaderValue(
                    canonicalizer.AuthorizationScheme,
                    string.Format(CultureInfo.InvariantCulture, "{0}:{1}", credentials.AccountName, signature));
            }
        }
    }
}
