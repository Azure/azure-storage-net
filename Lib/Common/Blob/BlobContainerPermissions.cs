﻿//-----------------------------------------------------------------------
// <copyright file="BlobContainerPermissions.cs" company="Microsoft">
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
    /// <summary>
    /// Represents the permissions for a container.
    /// </summary>
    /// /// <remarks>
    /// ## Examples
    ///  [!code-csharp[BlobContainerPermissions_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/SASTests.cs#sample_CloudBlobContainer_GetSetPermissions "BlobContainerPermissions Sample")]
    /// </remarks>
    public sealed class BlobContainerPermissions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobContainerPermissions"/> class.
        /// </summary>
        public BlobContainerPermissions()
        {
            this.PublicAccess = BlobContainerPublicAccessType.Off;
            this.SharedAccessPolicies = new SharedAccessBlobPolicies();
        }

        /// <summary>
        /// Gets or sets the public access setting for the container.
        /// </summary>
        /// <value>A <see cref="BlobContainerPublicAccessType"/> enumeration value.</value>
        public BlobContainerPublicAccessType PublicAccess { get; set; }

        /// <summary>
        /// Gets the set of shared access policies for the container.
        /// </summary>
        /// <value>A <see cref="SharedAccessBlobPolicies"/> object.</value>
        public SharedAccessBlobPolicies SharedAccessPolicies { get; private set; }
    }
}
