//-----------------------------------------------------------------------
// <copyright file="IListFileItem.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using System;

    /// <summary>
    /// Represents an item that may be returned by a file listing operation.
    /// </summary>
    public interface IListFileItem
    {
        /// <summary>
        /// Gets the URI to the file item.
        /// </summary>
        /// <value>The file item's URI.</value>
        Uri Uri { get; }

        /// <summary>
        /// Gets the URI to the file item.
        /// </summary>
        /// <value>The file item's URI.</value>
        StorageUri StorageUri { get; }

        /// <summary>
        /// Gets the file item's parent directory.
        /// </summary>
        /// <value>The file item's parent directory.</value>
        CloudFileDirectory Parent { get; }

        /// <summary>
        /// Gets the file item's share.
        /// </summary>
        /// <value>The file item's share.</value>
        CloudFileShare Share { get; }
    }
}
