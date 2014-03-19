//-----------------------------------------------------------------------
// <copyright file="SharedAccessBlobHeaders.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;

    /// <summary>
    /// Represents the optional headers that can be returned with blobs accessed using SAS.
    /// </summary>
    public sealed class SharedAccessBlobHeaders
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessBlobHeaders"/> class.
        /// </summary>
        public SharedAccessBlobHeaders()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessBlobHeaders"/> class based on an existing instance.
        /// </summary>
        /// <param name="sharedAccessBlobHeaders">The set of <see cref="SharedAccessBlobHeaders"/> to clone.</param>
        public SharedAccessBlobHeaders(SharedAccessBlobHeaders sharedAccessBlobHeaders)
        {
            CommonUtility.AssertNotNull("sharedAccessBlobHeaders", sharedAccessBlobHeaders);

            this.ContentType = sharedAccessBlobHeaders.ContentType;
            this.ContentDisposition = sharedAccessBlobHeaders.ContentDisposition;
            this.ContentEncoding = sharedAccessBlobHeaders.ContentEncoding;
            this.ContentLanguage = sharedAccessBlobHeaders.ContentLanguage;
            this.CacheControl = sharedAccessBlobHeaders.CacheControl;
        }

        /// <summary>
        /// Gets or sets the cache-control header returned with the blob.
        /// </summary>
        /// <value>A string containing the cache-control value.</value>
        public string CacheControl { get; set; }

        /// <summary>
        /// Gets or sets the content-disposition header returned with the blob.
        /// </summary>
        /// <value>A string containing the content-disposition value.</value>
        public string ContentDisposition { get; set; }

        /// <summary>
        /// Gets or sets the content-encoding header returned with the blob.
        /// </summary>
        /// <value>A string containing the content-encoding value.</value>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Gets or sets the content-language header returned with the blob.
        /// </summary>
        /// <value>A string containing the content-language value.</value>
        public string ContentLanguage { get; set; }

        /// <summary>
        /// Gets or sets the content-type header returned with the blob.
        /// </summary>
        /// <value>A string containing the content-type value.</value>
        public string ContentType { get; set; }
    }
}