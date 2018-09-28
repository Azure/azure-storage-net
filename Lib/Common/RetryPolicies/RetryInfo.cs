// -----------------------------------------------------------------------------------------
// <copyright file="RetryInfo.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Globalization;

    /// <summary>
    /// Specifies parameters for the next retry of a request to be made against the Microsoft Azure storage services,
    /// including the target location and location mode for the next retry and the interval until the next retry.
    /// </summary>
    public sealed class RetryInfo
    {
        private TimeSpan interval = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryInfo"/> class.
        /// </summary>
        public RetryInfo()
        {
            this.TargetLocation = StorageLocation.Primary;
            this.UpdatedLocationMode = LocationMode.PrimaryOnly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryInfo"/> class.
        /// </summary>
        /// <param name="retryContext">The <see cref="RetryContext"/> object that was passed in to the retry policy.</param>
        public RetryInfo(RetryContext retryContext)
        {
            CommonUtility.AssertNotNull("retryContext", retryContext);
            this.TargetLocation = retryContext.NextLocation;
            this.UpdatedLocationMode = retryContext.LocationMode;
        }

        /// <summary>
        /// Gets or sets the target location for the next retry.
        /// </summary>
        /// <value>A <see cref="StorageLocation"/> enumeration value.</value>
        public StorageLocation TargetLocation { get; set; }

        /// <summary>
        /// Gets or sets the location mode for subsequent retries.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode"/> enumeration value.</value>
        public LocationMode UpdatedLocationMode { get; set; }

        /// <summary>
        /// Gets the interval until the next retry.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> object specifying the interval until the next retry.</value>
        public TimeSpan RetryInterval
        {
            get
            {
                return this.interval;
            }

            set
            {
                this.interval = CommonUtility.MaxTimeSpan(value, TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Returns a string that represents the current <see cref="RetryInfo"/> instance.
        /// </summary>
        /// <returns>A string that represents the current <see cref="RetryInfo"/> instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", this.TargetLocation, this.RetryInterval);
        }
    }
}
