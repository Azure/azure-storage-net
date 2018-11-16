// -----------------------------------------------------------------------------------------
// <copyright file="HttpClientFactory.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using Microsoft.WindowsAzure.Storage.Auth.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;

    internal static class HttpClientFactory
    {
        private static Lazy<HttpClient> instance = new Lazy<HttpClient>(
                    () =>
                    {
                        return BuildHttpClient(StorageAuthenticationHttpHandler.Instance);
                    });

        public static HttpClient Instance
        {
            get
            {
                return instance.Value;
            }
        }

        internal static HttpClient HttpClientFromDelegatingHandler(DelegatingHandler delegatingHandler)
        {
            if (delegatingHandler == null)
            {
                return null;
            }

            var currentHandler = delegatingHandler;

            while (currentHandler.InnerHandler != null)
            {
                var innerHandler = currentHandler.InnerHandler;

                if (!(innerHandler is DelegatingHandler))
                {
                    throw new ArgumentException(SR.DelegatingHandlerNonNullInnerHandler);
                }
                currentHandler = (DelegatingHandler)innerHandler;
            }

            currentHandler.InnerHandler = new StorageAuthenticationHttpHandler();
            return BuildHttpClient(delegatingHandler);
        }

        private static HttpClient BuildHttpClient(HttpMessageHandler httpMessageHandler)
        {
            HttpClient httpClient = new HttpClient(httpMessageHandler, false);

            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Constants.HeaderConstants.UserAgentProductName, Constants.HeaderConstants.UserAgentProductVersion));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Constants.HeaderConstants.UserAgentComment));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(Constants.HeaderConstants.StorageVersionHeader, Constants.HeaderConstants.TargetStorageVersion);
            httpClient.Timeout = Timeout.InfiniteTimeSpan;

            return httpClient;
        }
    }
}
