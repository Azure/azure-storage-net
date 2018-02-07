﻿// -----------------------------------------------------------------------------------------
// <copyright file="IContinuationToken.cs" company="Microsoft">
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.Azure.Storage
{
    /// <summary>
    /// An interface required for continuation token types.
    /// </summary>
    /// <remarks>The <see cref="Microsoft.Azure.Storage.Blob.BlobContinuationToken"/>, 
    /// and <see cref="Microsoft.Azure.Storage.Queue.QueueContinuationToken"/> classes implement the <see cref="IContinuationToken"/> interface.</remarks>
    public interface IContinuationToken
    {
        /// <summary>
        /// Gets the location that the token applies to.
        /// </summary>
        /// <value>A <see cref="StorageLocation"/> enumeration value.</value>
        StorageLocation? TargetLocation { get; set; }
    }
}
