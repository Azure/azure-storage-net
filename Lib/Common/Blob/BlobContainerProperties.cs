﻿//-----------------------------------------------------------------------
// <copyright file="BlobContainerProperties.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
    using System;

    /// <summary>
    /// Represents the system properties for a container.
    /// </summary>
    public sealed class BlobContainerProperties
    {
        /// <summary>
        /// Gets the ETag value for the container.
        /// </summary>
        /// <value>A string containing the container's quoted ETag value.</value>
        public string ETag { get; internal set; }

        /// <summary>
        /// Gets the container's last-modified time.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> containing the container's last-modified time, in UTC format.</value>
        public DateTimeOffset? LastModified { get; internal set; }

        /// <summary>
        /// Gets the container's lease status.
        /// </summary>
        /// <value>A <see cref="LeaseStatus"/> object that indicates the container's lease status.</value>
        public LeaseStatus LeaseStatus { get; internal set; }

        /// <summary>
        /// Gets the container's lease state.
        /// </summary>
        /// <value>A <see cref="LeaseState"/> object that indicates the container's lease state.</value>
        public LeaseState LeaseState { get; internal set; }

        /// <summary>
        /// Gets the container's lease duration.
        /// </summary>
        /// <value>A <see cref="LeaseDuration"/> object that indicates the container's lease duration.</value>
        public LeaseDuration LeaseDuration { get; internal set; }

        /// <summary>
        ///  Gets the public access for the container.
        /// </summary>
        /// <remarks>This field should only be set using the container's Create() method or SetPermissions() method</remarks>
        /// <value>A <see cref="BlobContainerPublicAccessType"/> that specifies the level of public access that is allowed on the container.</value>
        public BlobContainerPublicAccessType? PublicAccess { get; internal set; }
    }
}
