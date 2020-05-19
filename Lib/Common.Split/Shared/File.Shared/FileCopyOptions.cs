//-----------------------------------------------------------------------
// <copyright file="FileCopyOptions.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represent options in file copying.
    /// </summary>
    public struct FileCopyOptions
    {
        /// <summary>
        /// Get or set a boolen value that indicates whether to copy the File Permission from the source file to the destination file.
        /// </summary>
        public bool PreservePermissions { get; set; }

        /// <summary>
        /// Get or set a boolen value that indicates whether to copy the file system attributes from the source file to the destination file.
        /// </summary>
        public bool PreserveNtfsAttributes { get; set; }

        /// <summary>
        /// Get or set a boolen value that indicates whether to copy the creation time from the source file to the destination file.
        /// </summary>
        public bool PreserveCreationTime { get; set; }

        /// <summary>
        /// Get or set a boolen value that indicates whether to copy the last write time from the source file to the destination file.
        /// </summary>
        public bool PreserveLastWriteTime { get; set; }

        /// <summary>
        /// Get or set a boolean value that indicates whether the Archive attribute should be set.
        /// </summary>
        public bool? SetArchive { get; set; }

        /// <summary>
        /// Get or set a boolean value that indicates whether the ReadOnly attribute on a preexisting destination file should be respected.
        /// If true, the copy will succeed, otherwise, a previous file at the destination with the ReadOnly attribute set will cause the copy to fail.
        /// </summary>
        public bool IgnoreReadOnly { get; set; }
    }
}
