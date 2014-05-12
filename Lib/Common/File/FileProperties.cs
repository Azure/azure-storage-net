//-----------------------------------------------------------------------
// <copyright file="FileProperties.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;

    /// <summary>
    /// Represents the system properties for a file.
    /// </summary>
    public sealed class FileProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileProperties"/> class.
        /// </summary>
        public FileProperties()
        {
            this.Length = -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileProperties"/> class based on an existing instance.
        /// </summary>
        /// <param name="other">The set of file properties to clone.</param>
        public FileProperties(FileProperties other)
        {
            CommonUtility.AssertNotNull("other", other);

            this.ContentType = other.ContentType;
            this.ContentDisposition = other.ContentDisposition;
            this.ContentEncoding = other.ContentEncoding;
            this.ContentLanguage = other.ContentLanguage;
            this.CacheControl = other.CacheControl;
            this.ContentMD5 = other.ContentMD5;
            this.Length = other.Length;
            this.ETag = other.ETag;
            this.LastModified = other.LastModified;
        }

        /// <summary>
        /// Gets or sets the cache-control value stored for the file.
        /// </summary>
        /// <value>The file's cache-control value.</value>
        public string CacheControl { get; set; }

        /// <summary>
        /// Gets or sets the content-disposition value stored for the file.
        /// </summary>
        /// <value>The file's content-disposition value.</value>
        /// <remarks>
        /// If this property has not been set for the file, it returns null.
        /// </remarks>
        public string ContentDisposition { get; set; }

        /// <summary>
        /// Gets or sets the content-encoding value stored for the file.
        /// </summary>
        /// <value>The file's content-encoding value.</value>
        /// <remarks>
        /// If this property has not been set for the file, it returns <c>null</c>.
        /// </remarks>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Gets or sets the content-language value stored for the file.
        /// </summary>
        /// <value>The file's content-language value.</value>
        /// <remarks>
        /// If this property has not been set for the file, it returns <c>null</c>.
        /// </remarks>
        public string ContentLanguage { get; set; }

        /// <summary>
        /// Gets the size of the file, in bytes.
        /// </summary>
        /// <value>The file's size in bytes.</value>
        public long Length { get; internal set; }

        /// <summary>
        /// Gets or sets the content-MD5 value stored for the file.
        /// </summary>
        /// <value>The file's content-MD5 hash.</value>
        public string ContentMD5 { get; set; }

        /// <summary>
        /// Gets or sets the content-type value stored for the file.
        /// </summary>
        /// <value>The file's content-type value.</value>
        /// <remarks>
        /// If this property has not been set for the file, it returns <c>null</c>.
        /// </remarks>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the file's ETag value.
        /// </summary>
        /// <value>The file's ETag value.</value>
        public string ETag { get; internal set; }

        /// <summary>
        /// Gets the the last-modified time for the file, expressed as a UTC value.
        /// </summary>
        /// <value>The file's last-modified time, in UTC format.</value>
        public DateTimeOffset? LastModified { get; internal set; }
    }
}
