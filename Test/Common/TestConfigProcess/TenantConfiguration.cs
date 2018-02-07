﻿// -----------------------------------------------------------------------------------------
// <copyright file="TenantConfiguration.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage
{
    public class TenantConfiguration
    {
        public string TenantName { get; internal set; }
        public string AccountName { get; internal set; }
        public string AccountKey { get; internal set; }
        public string BlobServiceEndpoint { get; internal set; }
        public string FileServiceEndpoint { get; internal set; }
        public string QueueServiceEndpoint { get; internal set; }
        public string TableServiceEndpoint { get; internal set; }
        public string BlobSecurePortOverride { get; internal set; }
        public string FileSecurePortOverride { get; internal set; }
        public string TableSecurePortOverride { get; internal set; }
        public string QueueSecurePortOverride { get; internal set; }
        public string BlobServiceSecondaryEndpoint { get; internal set; }
        public string FileServiceSecondaryEndpoint { get; internal set; }
        public string QueueServiceSecondaryEndpoint { get; internal set; }
        public string TableServiceSecondaryEndpoint { get; internal set; }
        public TenantType TenantType { get; internal set; }
    }
}
