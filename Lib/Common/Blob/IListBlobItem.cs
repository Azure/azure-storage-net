//-----------------------------------------------------------------------
// <copyright file="IListBlobItem.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using System;

    /// <summary>
    /// Represents an item that may be returned by a blob listing operation.
    /// </summary>
    public interface IListBlobItem
    {
        /// <summary>
        /// Gets the URI to the blob item, at the primary location.
        /// </summary>
        /// <value>The <see cref="System.Uri"/> for the blob item.</value>
        Uri Uri { get; }

        /// <summary>
        /// Gets the blob item's URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the blob item's URIs for both the primary and secondary locations.</value>
        StorageUri StorageUri { get; }

        /// <summary>
        /// Gets the blob item's parent virtual directory.
        /// </summary>
        /// <value>A <see cref="CloudBlobDirectory"/> object.</value>
        CloudBlobDirectory Parent { get; }

        /// <summary>
        /// Gets the blob item's container.
        /// </summary>
        /// <value>A <see cref="CloudBlobContainer"/> object.</value>
        CloudBlobContainer Container { get; }
    }
}
