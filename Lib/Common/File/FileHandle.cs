//-----------------------------------------------------------------------
// <copyright file="FileHandle.cs" company="Microsoft">
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
// <summary>
//    Contains code for the FileHandle class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Azure.Storage.File
{
    using System;
    using System.Net;

    /// <summary> 
    /// Represents a range in a file. 
    /// </summary> 
    public sealed class FileHandle
    {
        /// <summary> 
        /// XSMB service handle ID. 
        /// </summary> 
        public ulong? HandleId { get; internal set; }

        /// <summary> 
        /// File or directory name including full path starting from share root. 
        /// </summary> 
        public string Path { get; internal set; }

        /// <summary> 
        /// Client IP that opened the handle. 
        /// </summary> 
        public IPAddress ClientIp { get; internal set; }

        /// <summary> 
        /// Time the handle was opened. 
        /// </summary> 
        public DateTimeOffset OpenTime { get; internal set; }

        /// <summary> 
        /// Time the handle was opened. 
        /// </summary> 
        public DateTimeOffset LastReconnectTime { get; internal set; }

        /// <summary> 
        /// Unique file ID. 
        /// </summary> 
        public ulong FileId { get; internal set; }

        /// <summary> 
        /// Parent's unique file ID. 
        /// </summary> 
        public ulong ParentId { get; internal set; }

        /// <summary> 
        /// SMB session ID in context of whivh the file handle was opened. 
        /// </summary> 
        public ulong SessionId { get; internal set; }
    }
}