//-----------------------------------------------------------------------
// <copyright file="ListFileDirectoryEntry.cs" company="Microsoft">
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

    /// <summary>
    /// Represents a directory item that is returned in the XML response for a file listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListFileDirectoryEntry : IListFileEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListFileDirectoryEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the directory.</param>
        /// <param name="uri">The Uri of the directory.</param>
        /// <param name="properties">The directory's properties.</param>
        internal ListFileDirectoryEntry(string name, Uri uri, FileDirectoryProperties properties)
        {
            this.Name = name;
            this.Uri = uri;
            this.Properties = properties;
        }

        /// <summary>
        /// Gets the name of the directory item.
        /// </summary>
        /// <value>The name of the directory item.</value>
        public string Name
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the directory address.
        /// </summary>
        /// <value>The directory URL.</value>
        public Uri Uri
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the directory item's properties.
        /// </summary>
        /// <value>The directory item's properties.</value>
        public FileDirectoryProperties Properties
        {
            get;
            internal set;
        }
    }
}
