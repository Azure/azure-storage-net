//-----------------------------------------------------------------------
// <copyright file="RehydrationStatus.cs" company="Microsoft">
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
    /// <summary>
    /// The rehydration status for a blob that is currently archived.
    /// </summary>
    /// <remarks>Only applicable to block blobs for this version.</remarks>
    public enum RehydrationStatus
    {
        /// <summary>
        /// The rehydration status is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The blob is being rehydrated to hot storage.
        /// </summary>
        PendingToHot,

        /// <summary>
        /// The blob is being rehydrated to cool storage.
        /// </summary>
        PendingToCool
    }
}