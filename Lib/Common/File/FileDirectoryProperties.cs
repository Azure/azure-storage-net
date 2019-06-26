//-----------------------------------------------------------------------
// <copyright file="FileDirectoryProperties.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.File
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents the system properties for a directory.
    /// </summary>
    public sealed class FileDirectoryProperties
    {
        /// <summary>
        /// Gets the ETag value for the directory.
        /// </summary>
        /// <value>The directory's quoted ETag value.</value>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1702:CompoundWordsShouldBeCasedCorrectly",
            MessageId = "ETag",
            Justification = "ETag is the correct capitalization.")]
        public string ETag { get; internal set; }

        /// <summary>
        /// Gets the directory's last-modified time.
        /// </summary>
        /// <value>The directory's last-modified time.</value>
        public DateTimeOffset? LastModified { get; internal set; }

        /// <summary>
        /// Gets the directory's server-side encryption state.
        /// </summary>
        /// <value>A bool representing the directory's server-side encryption state.</value>
        public bool IsServerEncrypted { get; internal set; }

        /// <summary>
        /// The directory's File Permission Key.
        /// </summary>
        internal string filePermissionKey;

        /// <summary>
        /// The file permission key to set on the next Directory Create or Set Properties call.
        /// </summary>
        internal string filePermissionKeyToSet;

        /// <summary>
        /// Gets or sets the directory's File Permission Key.
        /// </summary>
        public string FilePermissionKey
        {
            get
            {
                return filePermissionKeyToSet ?? filePermissionKey;
            }
            set
            {
                filePermissionKeyToSet = value;
            }
        }

        /// <summary>
        /// The file system attributes for this directory.
        /// </summary>
        internal CloudFileNtfsAttributes? ntfsAttributes;

        /// <summary>
        /// The file system attributes to set on the next Directory Create or Set Properties call.
        /// </summary>
        internal CloudFileNtfsAttributes? ntfsAttributesToSet;

        /// <summary>
        /// Gets or sets the file system attributes for this directory
        /// </summary>
        public CloudFileNtfsAttributes? NtfsAttributes
        {
            get
            {
                return ntfsAttributesToSet ?? ntfsAttributes;
            }
            set
            {
                ntfsAttributesToSet = value;
            }
        }

        /// <summary>
        /// The <see cref="DateTimeOffset"/> when the File or Directory was created.  Read only.
        /// </summary>
        internal DateTimeOffset? creationTime;

        /// <summary>
        /// The directory creation time to set on the next Directory Create or Set Properties call.
        /// </summary>
        internal DateTimeOffset? creationTimeToSet;

        /// <summary>
        /// Gets or sets the <see cref="DateTimeOffset"/> when the File or Directory was created.
        /// </summary>
        public DateTimeOffset? CreationTime
        {
            get
            {
                return creationTimeToSet ?? creationTime;
            }
            set
            {
                creationTimeToSet = value;
            }
        }

        /// <summary>
        /// The <see cref="DateTime"/> when the Directory was last modified.
        /// </summary>
        internal DateTimeOffset? lastWriteTime;

        /// <summary>
        /// The directory last write time to set on the next Directory Create or Set Properties call.
        /// </summary>
        internal DateTimeOffset? lastWriteTimeToSet;

        /// <summary>
        /// Gets or sets the <see cref="DateTimeOffset"/> when the Directory was last modified.
        /// </summary>
        public DateTimeOffset? LastWriteTime
        {
            get
            {
                return lastWriteTimeToSet ?? lastWriteTime;
            }
            set
            {
                lastWriteTimeToSet = value;
            }
        }

        /// <summary>
        /// The <see cref="DateTimeOffset"/> when the File was last changed.  Ready only.
        /// </summary>
        public DateTimeOffset? ChangeTime { get; internal set; }

        /// <summary>
        /// The Id of this directory.  Ready only.
        /// </summary>
        public string DirectoryId { get; internal set; }

        /// <summary>
        /// The Id of this directory's parent.  Read only.
        /// </summary>
        public string ParentId { get; internal set; }
    }
}
