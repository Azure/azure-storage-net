//-----------------------------------------------------------------------
// <copyright file="FileShareEntry.cs" company="Microsoft">
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
//    Contains code for the CloudStorageAccount class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a share item returned in the XML response for a share listing operation.
    /// </summary>
    /// 
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class FileShareEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareEntry"/> class.
        /// </summary>
        internal FileShareEntry()
        {
        }

        /// <summary>
        /// Gets the user-defined metadata for the share.
        /// </summary>
        /// <value>The share's metadata, as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata { get; internal set; }

        /// <summary>
        /// Gets the share's system properties.
        /// </summary>
        /// <value>The share's properties.</value>
        public FileShareProperties Properties { get; internal set; }

        /// <summary>
        /// Gets the name of the share.
        /// </summary>
        /// <value>The share's name.</value>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the share's URI.
        /// </summary>
        /// <value>The absolute URI to the share.</value>
        public Uri Uri { get; internal set; }
    }
}
