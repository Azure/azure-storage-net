//-----------------------------------------------------------------------
// <copyright file="StorageRequestMessage.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using System;
    using System.Net.Http;

    internal sealed class StorageRequestMessage : HttpRequestMessage
    {
        public ICanonicalizer Canonicalizer { get; private set; }

        public StorageCredentials Credentials { get; private set; }

        public string AccountName { get; private set; }

        public StorageRequestMessage(HttpMethod method, Uri requestUri, ICanonicalizer canonicalizer, StorageCredentials credentials, string accountName)
            : base(method, requestUri)
        {
            this.Canonicalizer = canonicalizer;
            this.Credentials = credentials;
            this.AccountName = accountName;
        }
    }
}