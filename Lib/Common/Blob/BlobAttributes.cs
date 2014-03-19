//-----------------------------------------------------------------------
// <copyright file="BlobAttributes.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    internal sealed class BlobAttributes
    {
        internal BlobAttributes()
        {
            this.Properties = new BlobProperties();
            this.Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the blob's system properties.
        /// </summary>
        /// <value>A <see cref="BlobProperties"/> object.</value>
        public BlobProperties Properties { get; internal set; }

        /// <summary>
        /// Gets the user-defined metadata for the blob.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the blob's metadata as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata { get; internal set; }

        /// <summary>
        /// Gets the blob's URI for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the blob at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the blob's URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>A <see cref="StorageUri"/> object.</value>
        public StorageUri StorageUri { get; internal set; }

        /// <summary>
        /// Gets the date and time that the blob snapshot was taken, if this blob is a snapshot.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> containing the blob's snapshot time if the blob is a snapshot; otherwise, <c>null</c>.</value>
        /// <remarks>
        /// If the blob is not a snapshot, the value of this property is <c>null</c>.
        /// </remarks>
        public DateTimeOffset? SnapshotTime { get; internal set; }

        /// <summary>
        /// Gets the state of the most recent or pending copy operation.
        /// </summary>
        /// <value>A <see cref="CopyState"/> object containing the copy state, or <c>null</c> if no copy blob state exists for this blob.</value>
        public CopyState CopyState { get; internal set; }

        /// <summary>
        /// Verifies that the blob is not a snapshot.
        /// </summary>
        internal void AssertNoSnapshot()
        {
            if (this.SnapshotTime.HasValue)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotModifySnapshot);
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
