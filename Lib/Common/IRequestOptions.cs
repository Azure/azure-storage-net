// -----------------------------------------------------------------------------------------
// <copyright file="IRequestOptions.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;

    /// <summary>
    /// An interface required for request option types.
    /// </summary>
    /// <remarks>The <see cref="Microsoft.WindowsAzure.Storage.Queue.QueueRequestOptions"/>, <see cref="Microsoft.WindowsAzure.Storage.Blob.BlobRequestOptions"/>, and <see cref="Microsoft.WindowsAzure.Storage.Table.TableRequestOptions"/> classes implement the <see cref="IRequestOptions"/> interface.</remarks>
    public interface IRequestOptions
    {
        /// <summary>
        /// Gets or sets the retry policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="IRetryPolicy"/>.</value>
        IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the location mode of the request.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode"/> enumeration value.</value>
        LocationMode? LocationMode { get; set; }

        /// <summary>
        /// Gets or sets the default server timeout for the request.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval.</value>
        TimeSpan? ServerTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time across all potential retries.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the maximum execution time across all potential retries.</value>
        TimeSpan? MaximumExecutionTime { get; set; }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Gets or sets a value to indicate whether data written and read by the client library should be encrypted.
        /// </summary>
        /// <value>Use <c>true</c> to specify that data should be encrypted/decrypted for all transactions; otherwise, <c>false</c>.</value>
        bool? RequireEncryption { get; set; }
#endif
    }
}
