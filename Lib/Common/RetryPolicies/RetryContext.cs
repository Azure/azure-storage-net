// -----------------------------------------------------------------------------------------
// <copyright file="RetryContext.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.RetryPolicies
{
    using System.Globalization;

    /// <summary>
    /// Represents the context for one or more retries of a request made against the Microsoft Azure storage services,
    /// including the number of retries made for the request, the results of the last request, and the storage location and location mode for subsequent retries.
    /// </summary>
    public sealed class RetryContext
    {
        internal RetryContext(int currentRetryCount, RequestResult lastRequestResult, StorageLocation nextLocation, LocationMode locationMode)
        {
            this.CurrentRetryCount = currentRetryCount;
            this.LastRequestResult = lastRequestResult;
            this.NextLocation = nextLocation;
            this.LocationMode = locationMode;
        }

        /// <summary>
        /// Gets the target location for the next retry.
        /// </summary>
        /// <value>A <see cref="StorageLocation"/> enumeration value.</value>
        public StorageLocation NextLocation { get; private set; }

        /// <summary>
        /// Gets the location mode for subsequent retries.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode"/> enumeration value.</value>
        public LocationMode LocationMode { get; private set; }

        /// <summary>
        /// Gets the number of retries for the given operation.
        /// </summary>
        /// <value>An integer specifying the number of retries for the given operation.</value>
        public int CurrentRetryCount { get; private set; }

        /// <summary>
        /// Gets the results of the last request.
        /// </summary>
        /// <value>A <see cref="RequestResult"/> object.</value>
        public RequestResult LastRequestResult { get; private set; }

        /// <summary>
        /// Returns a string that represents the current <see cref="RetryContext"/> instance.
        /// </summary>
        /// <returns>A string that represents the current <see cref="RetryContext"/> instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", this.CurrentRetryCount, this.LocationMode);
        }
    }
}
