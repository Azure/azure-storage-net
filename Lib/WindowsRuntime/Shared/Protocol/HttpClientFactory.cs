﻿// -----------------------------------------------------------------------------------------
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

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    using Microsoft.Azure.Storage.Auth.Protocol;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;

    internal static class HttpClientFactory
    {
        private static Lazy<HttpClient> instance = new Lazy<HttpClient>(
                    () =>
                    {
                        HttpClient newClient = new HttpClient(StorageAuthenticationHttpHandler.Instance, false);

                        newClient.DefaultRequestHeaders.ExpectContinue = false;
                        newClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Constants.HeaderConstants.UserAgentProductName, Constants.HeaderConstants.UserAgentProductVersion));
                        newClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Constants.HeaderConstants.UserAgentComment));
                        newClient.DefaultRequestHeaders.TryAddWithoutValidation(Constants.HeaderConstants.StorageVersionHeader, OperationContext.StorageVersion ?? Constants.HeaderConstants.TargetStorageVersion);
                        newClient.Timeout = Timeout.InfiniteTimeSpan;

                        return newClient;
                    });

        public static HttpClient Instance
        {
            get
            {
                return instance.Value;
            }
        }
    }
}
