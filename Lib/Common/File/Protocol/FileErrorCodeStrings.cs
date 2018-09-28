//-----------------------------------------------------------------------
// <copyright file="FileErrorCodeStrings.cs" company="Microsoft">
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
// <summary>
//    Contains code for the CloudStorageAccount class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    /// <summary>
    /// Provides error code strings that are specific to the File service.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        static class FileErrorCodeStrings
    {
        /// <summary>
        /// The specified share was not found.
        /// </summary>
        public static readonly string ShareNotFound = "ShareNotFound";

        /// <summary>
        /// The specified share already exists.
        /// </summary>
        public static readonly string ShareAlreadyExists = "ShareAlreadyExists";

        /// <summary>
        /// The specified share is disabled.
        /// </summary>
        public static readonly string ShareDisabled = "ShareDisabled";

        /// <summary>
        /// The specified share is being deleted.
        /// </summary>
        public static readonly string ShareBeingDeleted = "ShareBeingDeleted";

        /// <summary>
        /// The specified resource is marked for deletion by an SMB client.
        /// </summary>
        public static readonly string DeletePending = "DeletePending";

        /// <summary>
        /// The specified parent was not found.
        /// </summary>
        public static readonly string ParentNotFound = "ParentNotFound";

        /// <summary>
        /// The specified resource name contains invalid characters.
        /// </summary>
        public static readonly string InvalidResourceName = "InvalidResourceName";

        /// <summary>
        /// The specified resource already exists.
        /// </summary>
        public static readonly string ResourceAlreadyExists = "ResourceAlreadyExists";

        /// <summary>
        /// The specified resource type does not match the type of the existing resource.
        /// </summary>
        public static readonly string ResourceTypeMismatch = "ResourceTypeMismatch";

        /// <summary>
        /// The specified resource may be in use by an SMB client.
        /// </summary>
        public static readonly string SharingViolation = "SharingViolation";

        /// <summary>
        /// The file or directory could not be deleted because it is in use by an SMB client.
        /// </summary>
        public static readonly string CannotDeleteFileOrDirectory = "CannotDeleteFileOrDirectory";

        /// <summary>
        /// A portion of the specified file is locked by an SMB client.
        /// </summary>
        public static readonly string FileLockConflict = "FileLockConflict";

        /// <summary>
        /// The specified resource is read-only and cannot be modified at this time.
        /// </summary>
        public static readonly string ReadOnlyAttribute = "ReadOnlyAttribute";

        /// <summary>
        /// The specified resource state could not be flushed from an SMB client in the specified time.
        /// </summary>
        public static readonly string ClientCacheFlushDelay = "ClientCacheFlushDelay";

        /// <summary>
        /// File or directory path is too long.
        /// </summary>
        public static readonly string InvalidFileOrDirectoryPathName = "InvalidFileOrDirectoryPathName";

        /// <summary>
        /// Condition headers are not supported.
        /// </summary>
        public static readonly string ConditionHeadersNotSupported = "ConditionHeadersNotSupported";
    }
}
