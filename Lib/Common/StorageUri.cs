//-----------------------------------------------------------------------
// <copyright file="StorageUri.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

#if WINDOWS_RT
    using Windows.Foundation.Metadata;
#endif

    /// <summary>
    /// Contains the URIs for both the primary and secondary locations of a Windows Azure Storage resource.
    /// </summary>
    public sealed class StorageUri
#if !WINDOWS_RT
        : IEquatable<StorageUri>
#endif
    {
        private Uri primaryUri;
        private Uri secondaryUri;

        /// <summary>
        /// The endpoint for the primary location for the storage account.
        /// </summary>
        /// <value>The <see cref="System.Uri"/> for the primary endpoint.</value>
        public Uri PrimaryUri
        {
            get
            {
                return this.primaryUri;
            }

            private set
            {
                StorageUri.AssertAbsoluteUri(value);
                this.primaryUri = value;
            }
        }

        /// <summary>
        /// The endpoint for the secondary location for the storage account.
        /// </summary>
        /// <value>The <see cref="System.Uri"/> for the secondary endpoint.</value>
        public Uri SecondaryUri
        {
            get
            {
                return this.secondaryUri;
            }

            private set
            {
                StorageUri.AssertAbsoluteUri(value);
                this.secondaryUri = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageUri"/> class using the primary endpoint for the storage account.
        /// </summary>
        /// <param name="primaryUri">The <see cref="System.Uri"/> for the primary endpoint.</param>
        public StorageUri(Uri primaryUri)
            : this(primaryUri, null /* secondaryUri */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageUri"/> class using the primary and secondary endpoints for the storage account.
        /// </summary>
        /// <param name="primaryUri">The <see cref="System.Uri"/> for the primary endpoint.</param>
        /// <param name="secondaryUri">The <see cref="System.Uri"/> for the secondary endpoint.</param>
        public StorageUri(Uri primaryUri, Uri secondaryUri)
        {
            if ((primaryUri != null) && (secondaryUri != null))
            {
                bool primaryUriPathStyle = CommonUtility.UsePathStyleAddressing(primaryUri);
                bool secondaryUriPathStyle = CommonUtility.UsePathStyleAddressing(secondaryUri);

                if (!primaryUriPathStyle && !secondaryUriPathStyle)
                {
                    if (primaryUri.PathAndQuery != secondaryUri.PathAndQuery)
                    {
                        throw new ArgumentException(SR.StorageUriMustMatch, "secondaryUri");
                    }
                }
                else
                {
                    IEnumerable<string> primaryUriSegments = primaryUri.Segments.Skip(primaryUriPathStyle ? 2 : 0);
                    IEnumerable<string> secondaryUriSegments = secondaryUri.Segments.Skip(secondaryUriPathStyle ? 2 : 0);

                    if (!primaryUriSegments.SequenceEqual(secondaryUriSegments) || (primaryUri.Query != secondaryUri.Query))
                    {
                        throw new ArgumentException(SR.StorageUriMustMatch, "secondaryUri");
                    }
                }
            }

            this.PrimaryUri = primaryUri;
            this.SecondaryUri = secondaryUri;
        }

        /// <summary>
        /// Returns the URI for the storage account endpoint at the specified location.
        /// </summary>
        /// <param name="location">A <see cref="StorageLocation"/> enumeration value.</param>
        /// <returns>The <see cref="System.Uri"/> for the endpoint at the the specified location.</returns>
        public Uri GetUri(StorageLocation location)
        {
            switch (location)
            {
                case StorageLocation.Primary:
                    return this.PrimaryUri;

                case StorageLocation.Secondary:
                    return this.SecondaryUri;

                default:
                    CommonUtility.ArgumentOutOfRange("location", location);
                    return null;
            }
        }

        internal bool ValidateLocationMode(LocationMode mode)
        {
            switch (mode)
            {
                case LocationMode.PrimaryOnly:
                    return this.PrimaryUri != null;

                case LocationMode.SecondaryOnly:
                    return this.SecondaryUri != null;

                default:
                    return (this.PrimaryUri != null) && (this.SecondaryUri != null);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Primary = '{0}'; Secondary = '{1}'",
                this.PrimaryUri,
                this.SecondaryUri);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hash1 = this.PrimaryUri != null ? this.PrimaryUri.GetHashCode() : 0;
            int hash2 = this.SecondaryUri != null ? this.SecondaryUri.GetHashCode() : 0;
            return hash1 ^ hash2;
        }

#if WINDOWS_RT
        [DefaultOverload]
#endif
        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as StorageUri);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(StorageUri other)
        {
            return (other != null) &&
                StorageUri.AreUrisEqual(this.PrimaryUri, other.PrimaryUri) &&
                StorageUri.AreUrisEqual(this.SecondaryUri, other.SecondaryUri);
        }

        private static bool AreUrisEqual(Uri uri1, Uri uri2)
        {
            return uri1 == null ? uri2 == null : uri1.Equals(uri2);
        }

        private static void AssertAbsoluteUri(Uri uri)
        {
            if ((uri != null) && !uri.IsAbsoluteUri)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, SR.RelativeAddressNotPermitted, uri.ToString());
                throw new ArgumentException(errorMessage, "uri");
            }
        }
    }
}
