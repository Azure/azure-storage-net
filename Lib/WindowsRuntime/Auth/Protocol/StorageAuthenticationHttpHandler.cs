// -----------------------------------------------------------------------------------------
// <copyright file="StorageAuthenticationHttpHandler.cs" company="Microsoft">
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
    using System.Net;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class StorageAuthenticationHttpHandler : HttpClientHandler
    {
        private StorageAuthenticationHttpHandler()
        {
#if NETCORE
            this.AutomaticDecompression = DecompressionMethods.None;
#endif
        }

        private static Lazy<StorageAuthenticationHttpHandler> instance =
            new Lazy<StorageAuthenticationHttpHandler>(() => new StorageAuthenticationHttpHandler());

        public static StorageAuthenticationHttpHandler Instance
        {
            get
            {
                return instance.Value;
            }
        }

        private Task<HttpResponseMessage> GetNoOpAuthenticationTask(StorageRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            StorageRequestMessage storageRequest = request as StorageRequestMessage;

            Func<StorageRequestMessage, CancellationToken, Task<HttpResponseMessage>> taskFactory = this.SelectAuthenticationTaskFactory(storageRequest);

            Task<HttpResponseMessage> task = taskFactory(storageRequest, cancellationToken);

            return task;
        }

        private Func<StorageRequestMessage, CancellationToken, Task<HttpResponseMessage>> SelectAuthenticationTaskFactory(StorageRequestMessage request)
        {
            Func<StorageRequestMessage, CancellationToken, Task<HttpResponseMessage>> authenticationHandler;

            if (request.Credentials.IsSharedKey)
            {
                authenticationHandler = this.GetSharedKeyAuthenticationTask;
            }
            else
            {
                authenticationHandler = this.GetNoOpAuthenticationTask;
            }

            return authenticationHandler;
        }
    }
}