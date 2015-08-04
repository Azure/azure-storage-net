//-----------------------------------------------------------------------
// <copyright file="FileShareProperties.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;

    /// <summary>
    /// Represents the system properties for a share.
    /// </summary>
    public sealed class FileShareProperties
    {
        private int? quota;

        /// <summary>
        /// Gets the ETag value for the share.
        /// </summary>
        /// <value>The share's quoted ETag value.</value>
        public string ETag { get; internal set; }

        /// <summary>
        /// Gets the share's last-modified time.
        /// </summary>
        /// <value>The share's last-modified time.</value>
        public DateTimeOffset? LastModified { get; internal set; }

        /// <summary>
        /// Gets or sets the maximum size for the share, in gigabytes.
        /// </summary>
        public int? Quota
        {
            get
            {
                return this.quota;
            }

            set
            {
                if (value.HasValue)
                {
                    CommonUtility.AssertInBounds("Quota", value.Value, 1);
                }
                
                this.quota = value;
            }
        }
    }
}
