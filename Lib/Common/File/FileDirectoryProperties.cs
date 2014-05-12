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

namespace Microsoft.WindowsAzure.Storage.File
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
    }
}
