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

namespace Microsoft.WindowsAzure.Storage.Auth.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    partial class StorageAuthenticationHttpHandler
    {
        private Task<HttpResponseMessage> GetTokenAuthenticationTask(StorageRequestMessage request, CancellationToken cancellationToken)
        {
            StorageRequestMessage storageRequest = request as StorageRequestMessage;
            StorageCredentials credentials = storageRequest.Credentials;

            if (!request.Headers.Contains(Constants.HeaderConstants.Date))
            {
                string dateString = HttpWebUtility.ConvertDateTimeToHttpString(DateTimeOffset.UtcNow);
                request.Headers.Add(Constants.HeaderConstants.Date, dateString);
            }

            if (!"https".Equals(storageRequest.RequestUri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(SR.OnlyHttpsIsSupportedForTokenCredential);
            }

            if (credentials.IsToken)
            {
                request.Headers.Add(
                    "Authorization",
                    string.Format(CultureInfo.InvariantCulture, "Bearer {0}", credentials.TokenCredential.Token));
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
