﻿//-----------------------------------------------------------------------
// <copyright file="ShareResultSegment.cs" company="Microsoft">
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
    using System.Collections.Generic;

    /// <summary>
    /// Represents a segment of <see cref="CloudFileShare"/> results and contains continuation and pagination information.
    /// </summary>
    public class ShareResultSegment
    {
        public ShareResultSegment(IEnumerable<CloudFileShare> shares, FileContinuationToken continuationToken)
        {
            this.Results = shares;
            this.ContinuationToken = continuationToken;
        }

        /// <summary>
        /// Gets an enumerable collection of <see cref="CloudFileShare"/> results.
        /// </summary>
        /// <value>An enumerable collection of results.</value>
        public IEnumerable<CloudFileShare> Results { get; private set; }

        /// <summary>
        /// Gets the <see cref="FileContinuationToken"/> object used to retrieve the next segment of <see cref="CloudFileShare"/> results.
        /// </summary>
        /// <value>The continuation token.</value>
        public FileContinuationToken ContinuationToken { get; private set; }
    }
}
