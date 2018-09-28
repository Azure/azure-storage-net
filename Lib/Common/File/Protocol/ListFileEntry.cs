//-----------------------------------------------------------------------
// <copyright file="ListFileEntry.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a file item returned in the XML response for a file listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListFileEntry : IListFileEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListFileEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="attributes">The file's attributes.</param>
        internal ListFileEntry(string name, CloudFileAttributes attributes)
        {
            this.Name = name;
            this.Attributes = attributes;
        }

        /// <summary>
        /// Stores the file item's attributes.
        /// </summary>
        internal CloudFileAttributes Attributes { get; private set; }

        /// <summary>
        /// Gets the name of the file item.
        /// </summary>
        /// <value>The name of the file item.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the file item's system properties.
        /// </summary>
        /// <value>The file item's properties.</value>
        public FileProperties Properties
        {
            get
            {
                return this.Attributes.Properties;
            }
        }

        /// <summary>
        /// Gets the user-defined metadata for the file item.
        /// </summary>
        /// <value>The file item's metadata, as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata
        {
            get
            {
                return this.Attributes.Metadata;
            }
        }

        /// <summary>
        /// Gets the file item's URI.
        /// </summary>
        /// <value>The absolute URI to the file item.</value>
        public Uri Uri
        {
            get
            {
                return this.Attributes.Uri;
            }
        }
    }
}
