//-----------------------------------------------------------------------
// <copyright file="TokenAuthenticationHandler.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Auth.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Represents a handler that signs HTTPS requests with a token.
    /// </summary>
    internal sealed class TokenAuthenticationHandler : IAuthenticationHandler
    {
        private readonly StorageCredentials credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        public TokenAuthenticationHandler(StorageCredentials credentials)
        {
            this.credentials = credentials;
        }

        /// <summary>
        /// Signs the specified HTTPS request with a token.
        /// </summary>
        /// <param name="request">The HTTPS request to sign.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        public void SignRequest(HttpWebRequest request, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("request", request);

            // only HTTPS is allowed for token credential, as the token would be at risk of being intercepted with HTTP.
            #if !WINDOWS_PHONE
            if (!"https".Equals(request.Address.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(SR.OnlyHttpsIsSupportedForTokenCredential);
            }
            #endif

            if (!request.Headers.AllKeys.Contains(Constants.HeaderConstants.Date, StringComparer.Ordinal))
            {
                string dateString = HttpWebUtility.ConvertDateTimeToHttpString(DateTime.UtcNow);
                request.Headers.Add(Constants.HeaderConstants.Date, dateString);
            }

            if (this.credentials.IsToken)
            {
                request.Headers.Add(
                    "Authorization",
                    string.Format(CultureInfo.InvariantCulture, "Bearer {0}", this.credentials.TokenCredential.Token));
            }
        }
    }
}
