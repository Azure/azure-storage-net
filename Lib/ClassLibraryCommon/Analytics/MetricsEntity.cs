// -----------------------------------------------------------------------------------------
// <copyright file="MetricsEntity.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an entity in a storage analytics metrics table.
    /// </summary>
    public class MetricsEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsEntity"/> class.
        /// </summary>
        public MetricsEntity()
        {
        }

        /// <summary>
        /// Gets the metrics entity's timestamp in UTC, representing the start time for that log entry.
        /// </summary>
        /// <value>A string containing the timestamp in UTC.</value>
        public DateTimeOffset Time
        {
            get
            {
                return DateTimeOffset.ParseExact(this.PartitionKey, "yyyyMMdd'T'HHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }
        }

        /// <summary>
        /// Gets the AccessType property for the metrics entity, indicating the type of access logged.
        /// </summary>
        /// <value>A string containing the access type for the metrics entity.</value>
        public string AccessType 
        { 
            get
            {
                CommonUtility.AssertNotNullOrEmpty("RowKey", this.RowKey);
                string result = this.RowKey.Split(';').ElementAtOrDefault(0);
                CommonUtility.AssertNotNullOrEmpty("AccessType", result);
                return result;
            }
        }

        /// <summary>
        /// Gets the TransactionType property for the metrics entity, indicating the type of transaction logged.
        /// </summary>
        /// <value>A string containing the transaction type for the metrics entity.</value>
        public string TransactionType 
        { 
            get
            {
                CommonUtility.AssertNotNullOrEmpty("RowKey", this.RowKey);
                string result = this.RowKey.Split(';').ElementAtOrDefault(1);
                CommonUtility.AssertNotNullOrEmpty("TransactionType", result);
                return result;
            }
        } 

        /// <summary>
        /// Gets or sets the TotalIngress property for the metrics entity, indicating the quantity of ingress data, in bytes.
        /// </summary>
        /// <value>A long containing the quantity of ingress data, in bytes, for the metrics entity.</value>
        public long TotalIngress { get; set; }

        /// <summary>
        /// Gets or sets the TotalEgress property for the metrics entity, indicating the quantity of egress data, in bytes.
        /// </summary>
        /// <value>A long containing the quantity of egress data, in bytes, for the metrics entity.</value>
        public long TotalEgress { get; set; }

        /// <summary>
        /// Gets or sets the TotalRequests property for the metrics entity, indicating the total number of requests.
        /// </summary>
        /// <value>A long containing the number of total requests for the metrics entity.</value>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the TotalBillableRequests property for the metrics entity, indicating the total number of billable requests.
        /// </summary>
        /// <value>A long containing the total number of billable requests for the metrics entity.</value>
        public long TotalBillableRequests { get; set; }

        /// <summary>
        /// Gets or sets the Availability property for the metrics entity, indicating the percentage of availability.
        /// </summary>
        /// <value>A double containing the percentage of availability for the metrics entity.</value>
        public double Availability { get; set; }

        /// <summary>
        /// Gets or sets the AverageE2ELatency property for the metrics entity, indicating the average end-to-end latency of successful requests.
        /// </summary>
        /// <value>A double containing the average end-to-end latency of successful requests for the metrics entity.</value>
        public double AverageE2ELatency { get; set; }

        /// <summary>
        /// Gets or sets the AverageServerLatency property for the metrics entity, indicating the average latency for the service to process 
        /// a successful request.
        /// </summary>
        /// <value>A double containing the average latency for the service to process a successful request for the metrics entity.</value>
        public double AverageServerLatency { get; set; }

        /// <summary>
        /// Gets or sets the PercentSuccess property for the metrics entity, indicating the percentage of successful requests.
        /// </summary>
        /// <value>A double containing the percentage of successful requests for the metrics entity.</value>
        public double PercentSuccess { get; set; }

        /// <summary>
        /// Gets or sets the PercentThrottlingError property for the metrics entity, indicating the percentage of requests that failed with a throttling error.
        /// </summary>
        /// <value>A double containing the percentage of requests that failed with a throttling error for the metrics entity.</value>
        public double PercentThrottlingError { get; set; }

        /// <summary>
        /// Gets or sets the PercentTimeoutError property for the metrics entity, indicating the percentage of requests that failed with a timeout error.
        /// </summary>
        /// <value>A double containing the percentage of requests that failed with a timeout error for the metrics entity.</value>
        public double PercentTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the PercentServerOtherError property for the metrics entity, indicating the percentage of requests that failed with a ServerOtherError.
        /// </summary>
        /// <value>A double containing the percentage of requests that failed with a ServerOtherError for the metrics entity.</value>
        public double PercentServerOtherError { get; set; }

        /// <summary>
        /// Gets or sets the PercentClientOtherError property for the metrics entity, indicating the percentage of requests that failed with a ClientOtherError.
        /// </summary>
        /// <value>A double containing the percentage of requests that failed with a ClientOtherError for the metrics entity.</value>
        public double PercentClientOtherError { get; set; }

        /// <summary>
        /// Gets or sets the PercentAuthorizationError property for the metrics entity, indicating the percentage of requests that failed with an AuthorizationError.
        /// </summary>
        /// <value>A double containing the percentage of requests that failed with an AuthorizationError for the metrics entity.</value>
        public double PercentAuthorizationError { get; set; }

        /// <summary>
        /// Gets or sets the PercentNetworkError property for the metrics entity, indicating the percentage of requests that failed with a NetworkError.
        /// </summary>
        /// <value>A double containing the percentage of requests that failed with a NetworkError for the metrics entity.</value>
        public double PercentNetworkError { get; set; }

        /// <summary>
        /// Gets or sets the Success property for the metrics entity, indicating the number of successful requests.
        /// </summary>
        /// <value>A long containing the number of successful requests for the metrics entity.</value>
        public long Success { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousSuccess property for the metrics entity, indicating the number of successful anonymous requests.
        /// </summary>
        /// <value>A long containing the number of successful anonymous requests for the metrics entity.</value>
        public long AnonymousSuccess { get; set; }

        /// <summary>
        /// Gets or sets the SASSuccess property for the metrics entity, indicating the number of successful SAS requests.
        /// </summary>
        /// <value>A long containing the number of successful SAS requests for the metrics entity.</value>
        public long SASSuccess { get; set; }

        /// <summary>
        /// Gets or sets the ThrottlingError property for the metrics entity, indicating the number of authenticated requests that returned a ThrottlingError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned a ThrottlingError for the metrics entity.</value>
        public long ThrottlingError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousThrottlingError property for the metrics entity, indicating the number of anonymous requests that returned a ThrottlingError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned a ThrottlingError for the metrics entity.</value>
        public long AnonymousThrottlingError { get; set; }

        /// <summary>
        /// Gets or sets the SASThrottlingError property for the metrics entity, indicating the number of SAS requests that returned a ThrottlingError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned a ThrottlingError for the metrics entity.</value>
        public long SASThrottlingError { get; set; }

        /// <summary>
        /// Gets or sets the ClientTimeoutError property for the metrics entity, indicating the number of authenticated requests that returned a ClientTimeoutError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned a ClientTimeoutError for the metrics entity.</value>
        public long ClientTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousClientTimeoutError property for the metrics entity, indicating the number of anonymous requests that returned a ClientTimeoutError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned a ClientTimeoutError for the metrics entity.</value>
        public long AnonymousClientTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the SASClientTimeoutError property for the metrics entity, indicating the number of SAS requests that returned a ClientTimeoutError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned a ClientTimeoutError for the metrics entity.</value>
        public long SASClientTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the ServerTimeoutError property for the metrics entity, indicating the number of authenticated requests that returned a ServerTimeoutError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned a ServerTimeoutError for the metrics entity.</value>
        public long ServerTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousServerTimeoutError property for the metrics entity, indicating the number of anonymous requests that returned a ServerTimeoutError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned a ServerTimeoutError for the metrics entity.</value>
        public long AnonymousServerTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the SASServerTimeoutError property for the metrics entity, indicating the number of SAS requests that returned a ServerTimeoutError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned a ServerTimeoutError for the metrics entity.</value>
        public long SASServerTimeoutError { get; set; }

        /// <summary>
        /// Gets or sets the ClientOtherError property for the metrics entity, indicating the number of authenticated requests that returned a ClientOtherError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned a ClientOtherError for the metrics entity.</value>
        public long ClientOtherError { get; set; }

        /// <summary>
        /// Gets or sets the SASClientOtherError property for the metrics entity, indicating the number of SAS requests that returned a ClientOtherError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned a ClientOtherError for the metrics entity.</value>
        public long SASClientOtherError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousClientOtherError property for the metrics entity, indicating the number of anonymous requests that returned an ClientOtherError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned a ClientOtherError for the metrics entity.</value>
        public long AnonymousClientOtherError { get; set; }

        /// <summary>
        /// Gets or sets the ServerOtherError property for the metrics entity, indicating the number of authenticated requests that returned a ServerOtherError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned a ServerOtherError for the metrics entity.</value>
        public long ServerOtherError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousServerOtherError property for the metrics entity, indicating the number of anonymous requests that returned a ServerOtherError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned a ServerOtherError for the metrics entity.</value>
        public long AnonymousServerOtherError { get; set; }

        /// <summary>
        /// Gets or sets the SASServerOtherError property for the metrics entity, indicating the number of SAS requests that returned a ServerOtherError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned a ServerOtherError for the metrics entity.</value>
        public long SASServerOtherError { get; set; }

        /// <summary>
        /// Gets or sets the AuthorizationError property for the metrics entity, indicating the number of authenticated requests that returned an AuthorizationError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned an AuthorizationError for the metrics entity.</value>
        public long AuthorizationError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousAuthorizationError property for the metrics entity, indicating the number of anonymous requests that returned an AuthorizationError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned an AuthorizationError for the metrics entity.</value>
        public long AnonymousAuthorizationError { get; set; }

        /// <summary>
        /// Gets or sets the SASAuthorizationError property for the metrics entity, indicating the number of SAS requests that returned an AuthorizationError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned an AuthorizationError for the metrics entity.</value>
        public long SASAuthorizationError { get; set; }

        /// <summary>
        /// Gets or sets the NetworkError property for the metrics entity, indicating the number of authenticated requests that returned a NetworkError.
        /// </summary>
        /// <value>A long containing the number of authenticated requests that returned a NetworkError for the metrics entity.</value>
        public long NetworkError { get; set; }

        /// <summary>
        /// Gets or sets the AnonymousNetworkError property for the metrics entity, indicating the number of anonymous requests that returned a NetworkError.
        /// </summary>
        /// <value>A long containing the number of anonymous requests that returned a NetworkError for the metrics entity.</value>
        public long AnonymousNetworkError { get; set; }

        /// <summary>
        /// Gets or sets the SASNetworkError property for the metrics entity, indicating the number of SAS requests that returned a NetworkError.
        /// </summary>
        /// <value>A long containing the number of SAS requests that returned a NetworkError for the metrics entity.</value>
        public long SASNetworkError { get; set; }
    }
}
