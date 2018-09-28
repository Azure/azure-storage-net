// -----------------------------------------------------------------------------------------
// <copyright file="SharedAccessAccountPermissions.cs" company="Microsoft">
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
    /// Specifies the set of possible permissions for a shared access account policy.
    /// </summary>
    [Flags]
    public enum SharedAccessAccountPermissions
    {
        /// <summary>
        /// No shared access granted.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Permission to read resources and list queues and tables granted.
        /// </summary>
        Read = 0x1,

        /// <summary>
        /// Permission to add messages, table entities, blobs, and files granted.
        /// </summary>
        Add = 0x2,

        /// <summary>
        /// Permission to create containers, blobs, shares, directories, and files granted.
        /// </summary>
        Create = 0x4,

        /// <summary>
        /// Permissions to update messages and table entities granted.
        /// </summary>
        Update = 0x8,

        /// <summary>
        /// Permission to get and delete messages granted.
        /// </summary>
        ProcessMessages = 0x10,

        /// <summary>
        /// Permission to write resources granted.
        /// </summary>
        Write = 0x20,

        /// <summary>
        /// Permission to delete resources granted.
        /// </summary>
        Delete = 0x40,

        /// <summary>
        /// Permission to list blob containers, blobs, shares, directories, and files granted.
        /// </summary>
        List = 0x80
    }
}
