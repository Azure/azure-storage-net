//-----------------------------------------------------------------------
// <copyright file="DeleteShareSnapshotsOption.cs" company="Microsoft">
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
    /// <summary>
    /// The set of options describing delete operation.
    /// </summary>
    internal enum DeleteShareSnapshotsOption
    {
        /// <summary>
        /// Delete the share only. If the share has snapshots, this option will result in an error from the service.
        /// </summary>
        None,

        /// <summary>
        /// Delete the share and its snapshots.
        /// </summary>
        IncludeSnapshots
    }
}