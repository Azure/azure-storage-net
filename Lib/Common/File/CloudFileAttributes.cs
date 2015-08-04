//-----------------------------------------------------------------------
// <copyright file="CloudFileAttributes.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    internal sealed class CloudFileAttributes
    {
        internal CloudFileAttributes()
        {
            this.Properties = new FileProperties();
            this.Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the file's system properties.
        /// </summary>
        /// <value>The file's properties.</value>
        public FileProperties Properties { get; internal set; }

        /// <summary>
        /// Gets the user-defined metadata for the file.
        /// </summary>
        /// <value>The file's metadata, as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata { get; internal set; }

        /// <summary>
        /// Gets the file's URI.
        /// </summary>
        /// <value>The absolute URI to the file.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the list of URIs for all locations.
        /// </summary>
        /// <value>The list of URIs for all locations.</value>
        public StorageUri StorageUri { get; internal set; }

        /// <summary>
        /// Gets the state of the most recent or pending copy operation.
        /// </summary>
        /// <value>A <see cref="CopyState"/> object containing the copy state, or <c>null</c> if no copy file state exists for this file.</value>
        public CopyState CopyState { get; internal set; }
    }
}
