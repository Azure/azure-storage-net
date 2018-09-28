// -----------------------------------------------------------------------------------------
// <copyright file="LinearRetry.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;

    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a specified fixed time interval between retries.
    /// </summary>
    public sealed class LinearRetry : IExtendedRetryPolicy
    {
        private const int DefaultClientRetryCount = 3;
        private static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(30);

        private TimeSpan deltaBackoff;
        private int maximumAttempts;
        private DateTimeOffset? lastPrimaryAttempt = null;
        private DateTimeOffset? lastSecondaryAttempt = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearRetry"/> class.
        /// </summary>
        public LinearRetry()
            : this(DefaultClientBackoff, DefaultClientRetryCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearRetry"/> class using the specified delta and maximum number of retries.
        /// </summary>
        /// <param name="deltaBackoff">A <see cref="TimeSpan"/> specifying the back-off interval between retries.</param>
        /// <param name="maxAttempts">An integer specifying the maximum number of retry attempts.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Backoff", Justification = "Reviewed: Backoff is allowed.")]
        public LinearRetry(TimeSpan deltaBackoff, int maxAttempts)
        {
            this.deltaBackoff = deltaBackoff;
            this.maximumAttempts = maxAttempts;
        }

        /// <summary>
        /// Determines whether the operation should be retried and the interval until the next retry.
        /// </summary>
        /// <param name="currentRetryCount">An integer specifying the number of retries for the given operation. A value of zero signifies this is the first error encountered.</param>
        /// <param name="statusCode">An integer containing the status code for the last operation.</param>
        /// <param name="lastException">An <see cref="Exception"/> object that represents the last exception encountered.</param>
        /// <param name="retryInterval">A <see cref="TimeSpan"/> indicating the interval to wait until the next retry.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the operation should be retried; otherwise, <c>false</c>.</returns>
        public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("lastException", lastException);

            retryInterval = TimeSpan.Zero;

            // If this method is called after a successful response, it means
            // we failed during the response body download. So, we should not
            // check for success codes here.
            if ((statusCode >= 300 && statusCode < 500 && statusCode != 408)
                   || statusCode == 501 // Not Implemented
                     || statusCode == 505 // Version Not Supported
                 || lastException.Message == SR.BlobTypeMismatch)
            {
                return false;
            }

            retryInterval = this.deltaBackoff;
            return currentRetryCount < this.maximumAttempts;
        }

        /// <summary>
        /// Determines whether the operation should be retried and the interval until the next retry.
        /// </summary>
        /// <param name="retryContext">A <see cref="RetryContext"/> object that indicates the number of retries, the results of the last request, and whether the next retry should happen in the primary or secondary location, and specifies the location mode.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="RetryInfo"/> object that indicates the location mode, and whether the next retry should happen in the primary or secondary location. If <c>null</c>, the operation will not be retried.</returns>
        public RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("retryContext", retryContext);

            // Retry interval of a request to a location must take the time spent sending requests
            // to other locations into account. For example, assume a request was sent to the primary
            // location first, then to the secondary, and then to the primary again. If it
            // was supposed to wait 10 seconds between requests to the primary and the request to
            // the secondary took 3 seconds in total, retry interval should only be 7 seconds, because
            // in total, the requests will be 10 seconds apart from the primary locations' point of view.
            // For this calculation, current instance of the retry policy stores timestamp of the last
            // request to a specific location.
            if (retryContext.LastRequestResult.TargetLocation == StorageLocation.Primary)
            {
                this.lastPrimaryAttempt = retryContext.LastRequestResult.EndTime;
            }
            else
            {
                this.lastSecondaryAttempt = retryContext.LastRequestResult.EndTime;
            }

            // If a request sent to the secondary location fails with 404 (Not Found), it is possible
            // that the resource replication is not finished yet. So, in case of 404 only in the secondary
            // location, the failure should still be retryable.
            bool secondaryNotFound = (retryContext.LastRequestResult.TargetLocation == StorageLocation.Secondary) && (retryContext.LastRequestResult.HttpStatusCode == (int)HttpStatusCode.NotFound);

            TimeSpan retryInterval;
            if (this.ShouldRetry(retryContext.CurrentRetryCount, secondaryNotFound ? 500 : retryContext.LastRequestResult.HttpStatusCode, retryContext.LastRequestResult.Exception, out retryInterval, operationContext))
            {
                RetryInfo retryInfo = new RetryInfo(retryContext);

                // Moreover, in case of 404 when trying the secondary location, instead of retrying on the
                // secondary, further requests should be sent only to the primary location, as it most
                // probably has a higher chance of succeeding there.
                if (secondaryNotFound && (retryContext.LocationMode != LocationMode.SecondaryOnly))
                {
                    retryInfo.UpdatedLocationMode = LocationMode.PrimaryOnly;
                    retryInfo.TargetLocation = StorageLocation.Primary;
                }

                // Now is the time to calculate the exact retry interval. ShouldRetry call above already
                // returned back how long two requests to the same location should be apart from each other.
                // However, for the reasons explained above, the time spent between the last attempt to
                // the target location and current time must be subtracted from the total retry interval
                // that ShouldRetry returned.
                DateTimeOffset? lastAttemptTime = retryInfo.TargetLocation == StorageLocation.Primary ? this.lastPrimaryAttempt : this.lastSecondaryAttempt;
                if (lastAttemptTime.HasValue)
                {
                    TimeSpan sinceLastAttempt = CommonUtility.MaxTimeSpan(DateTimeOffset.Now - lastAttemptTime.Value, TimeSpan.Zero);
                    retryInfo.RetryInterval = retryInterval - sinceLastAttempt;
                }
                else
                {
                    retryInfo.RetryInterval = TimeSpan.Zero;
                }

                return retryInfo;
            }

            return null;
        }

        /// <summary>
        /// Generates a new retry policy for the current request attempt.
        /// </summary>
        /// <returns>An <see cref="IRetryPolicy"/> object that represents the retry policy for the current request attempt.</returns>        
        public IRetryPolicy CreateInstance()
        {
            return new LinearRetry(this.deltaBackoff, this.maximumAttempts);
        }
    }
}
