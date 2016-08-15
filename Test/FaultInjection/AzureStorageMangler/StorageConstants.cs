// -----------------------------------------------------------------------------------------
// <copyright file="StorageConstants.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Test.Network
{
    /// <summary>
    /// Holds well-known information about Windows Azure Storage.
    /// </summary>
    public static class StorageConstants
    {
        /// <summary>
        /// The root host name for the Blob service.
        /// </summary>
        public const string BlobBaseDnsName = "blob.core.windows.net";

        /// <summary>
        /// The root host name for the Queue service.
        /// </summary>
        public const string QueueBaseDnsName = "queue.core.windows.net";

        /// <summary>
        /// The root host name for the Table service.
        /// </summary>
        public const string TableBaseDnsName = "table.core.windows.net";

        /// <summary>
        /// The root file storage DNS name.
        /// </summary>
        public const string FileBaseDnsName = "file.core.windows.net";
    }
}
