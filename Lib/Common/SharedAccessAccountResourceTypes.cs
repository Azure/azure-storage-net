// -----------------------------------------------------------------------------------------
// <copyright file="SharedAccessAccountResourceTypes.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using System;

    /// <summary>
    /// Specifies the set of possible signed resource types for a shared access account policy.
    /// </summary>
    [Flags]
    public enum SharedAccessAccountResourceTypes
    {
        /// <summary>
        /// No shared access granted.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Permission to access service level APIs granted.
        /// </summary>
        Service = 0x1,

        /// <summary>
        /// Permission to access container level APIs (Blob Containers, Tables, Queues, File Shares) granted.
        /// </summary>
        Container = 0x2,

        /// <summary>
        /// Permission to access object level APIs (Blobs, Table Entities, Queue Messages, Files) granted
        /// </summary>
        Object = 0x4
    }
}
