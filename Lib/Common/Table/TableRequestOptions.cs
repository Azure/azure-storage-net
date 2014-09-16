// -----------------------------------------------------------------------------------------
// <copyright file="TableRequestOptions.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;

    /// <summary>
    /// Represents a set of timeout and retry policy options that may be specified for a request against the Table service.
    /// </summary>
    public sealed class TableRequestOptions : IRequestOptions
    {
        private TablePayloadFormat? payloadFormat = null;

        /// <summary>
        /// Stores the maximum execution time.
        /// </summary>
        private TimeSpan? maximumExecutionTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableRequestOptions"/> class.
        /// </summary>
        public TableRequestOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableRequestOptions"/> class with the specified <see cref="TableRequestOptions"/>.
        /// </summary>
        /// <param name="other">The <see cref="TableRequestOptions"/> object used to initialize a new instance of the <see cref="TableRequestOptions"/> class.</param>
        public TableRequestOptions(TableRequestOptions other)
        {
            if (other != null)
            {
                this.ServerTimeout = other.ServerTimeout;
                this.RetryPolicy = other.RetryPolicy;
                this.LocationMode = other.LocationMode;
                this.MaximumExecutionTime = other.MaximumExecutionTime;
                this.OperationExpiryTime = other.OperationExpiryTime;
                this.PayloadFormat = other.PayloadFormat;
#if !WINDOWS_RT
                this.PropertyResolver = other.PropertyResolver;
#endif
            }
        }

        internal static TableRequestOptions ApplyDefaults(TableRequestOptions requestOptions, CloudTableClient serviceClient)
        {
            TableRequestOptions modifiedOptions = new TableRequestOptions(requestOptions);
            
            modifiedOptions.RetryPolicy = modifiedOptions.RetryPolicy ?? serviceClient.DefaultRequestOptions.RetryPolicy;
            modifiedOptions.LocationMode = (modifiedOptions.LocationMode 
                                            ?? serviceClient.DefaultRequestOptions.LocationMode)
                                            ?? RetryPolicies.LocationMode.PrimaryOnly;
            modifiedOptions.ServerTimeout = modifiedOptions.ServerTimeout ?? serviceClient.DefaultRequestOptions.ServerTimeout;
            modifiedOptions.MaximumExecutionTime = modifiedOptions.MaximumExecutionTime ?? serviceClient.DefaultRequestOptions.MaximumExecutionTime;
#if !WINDOWS_RT
            modifiedOptions.PayloadFormat = (modifiedOptions.PayloadFormat 
                                                ?? serviceClient.DefaultRequestOptions.PayloadFormat)
                                                ?? TablePayloadFormat.Json;
#else
            modifiedOptions.PayloadFormat = (modifiedOptions.PayloadFormat 
                                                ?? serviceClient.DefaultRequestOptions.PayloadFormat)
                                                ?? TablePayloadFormat.AtomPub;
#endif

            if (!modifiedOptions.OperationExpiryTime.HasValue && modifiedOptions.MaximumExecutionTime.HasValue)
            {
                modifiedOptions.OperationExpiryTime = DateTime.Now + modifiedOptions.MaximumExecutionTime.Value;
            }

#if !WINDOWS_RT
            modifiedOptions.PropertyResolver = modifiedOptions.PropertyResolver ?? serviceClient.DefaultRequestOptions.PropertyResolver;
#endif

            return modifiedOptions;
        }

        internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
        {
            if (this.LocationMode.HasValue)
            {
                cmd.LocationMode = this.LocationMode.Value;
            }

            this.ApplyToStorageCommandCommon(cmd);
        }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        [Obsolete("Support for accessing Windows Azure Tables via WCF Data Services is now obsolete. It's recommended that you use the Microsoft.WindowsAzure.Storage.Table namespace for working with tables.")]
        internal void ApplyToStorageCommand<T, INTERMEDIATE_TYPE>(TableCommand<T, INTERMEDIATE_TYPE> cmd)
        {
            if (this.LocationMode.HasValue &&
                (this.LocationMode.Value != RetryPolicies.LocationMode.PrimaryOnly))
            {
                throw new InvalidOperationException(SR.PrimaryOnlyCommand);
            }

            this.ApplyToStorageCommandCommon(cmd);
        }
#endif

        private void ApplyToStorageCommandCommon<T>(StorageCommandBase<T> cmd)
        {
            if (this.ServerTimeout.HasValue)
            {
                cmd.ServerTimeoutInSeconds = (int)this.ServerTimeout.Value.TotalSeconds;
            }

            if (this.OperationExpiryTime.HasValue)
            {
                cmd.OperationExpiryTime = this.OperationExpiryTime;
            }
            else if (this.MaximumExecutionTime.HasValue)
            {
                cmd.OperationExpiryTime = DateTime.Now + this.MaximumExecutionTime.Value;
            }
        }

        /// <summary>
        ///  Gets or sets the absolute expiry time across all potential retries for the request. 
        /// </summary>
        internal DateTime? OperationExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets the retry policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="IRetryPolicy"/>.</value>
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the location mode of the request.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode"/> enumeration value indicating the location mode of the request.</value>
        public LocationMode? LocationMode { get; set; }

        /// <summary>
        /// Gets or sets the server timeout interval for the request.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval for the request.</value>
        public TimeSpan? ServerTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time for all potential retries for the request.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> representing the maximum execution time for retries for the request.</value>
        public TimeSpan? MaximumExecutionTime
        {
            get
            {
                return this.maximumExecutionTime;
            }

            set
            {
                if (value.HasValue)
                {
                    CommonUtility.AssertInBounds("MaximumExecutionTime", value.Value, TimeSpan.Zero, Constants.MaxMaximumExecutionTime);
                }

                this.maximumExecutionTime = value;
            }
        } 

        /// <summary>
        /// Gets or sets the <see cref="TablePayloadFormat"/> that will be used for the request.
        /// </summary>
        /// <value>A <see cref="TablePayloadFormat"/> enumeration value.</value>
        public TablePayloadFormat? PayloadFormat
        {
            get
            {
                return this.payloadFormat;
            }

            set
            {
                if (value.HasValue)
                {
#if WINDOWS_RT
                if (value.Value == TablePayloadFormat.Json || value.Value == TablePayloadFormat.JsonNoMetadata || value.Value == TablePayloadFormat.JsonFullMetadata)
                {
                    throw new ArgumentException(SR.JsonNotSupportedOnRT, "value");
                }
#endif
                    this.payloadFormat = value.Value;
                }
            }
        }

        // For now, this is ok since we don't have RT support. Has to change to internal once we do. In that case, we can add overloads to Table operation
        // methods to take in EdmTypeResolver.
#if !WINDOWS_RT
        private Func<string, string, string, string, EdmType> propertyResolver = null;

        /// <summary>
        /// Gets or sets the delegate that is used to get the <see cref="EdmType"/> for an entity property given the partition key, row key, and the property name. 
        /// </summary>
        public Func<string, string, string, string, EdmType> PropertyResolver
        {
            get { return this.propertyResolver; }
            set { this.propertyResolver = value; }
        }
#endif
    }
}
