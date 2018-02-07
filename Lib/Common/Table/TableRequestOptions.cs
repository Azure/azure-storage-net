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

namespace Microsoft.Azure.Storage.Table
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.Azure.Storage.Shared.Protocol;
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

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// If this is set, encryption has been explicitly cleared.
        /// Thus, do not copy the value when applying defaults.
        /// </summary>
        private bool encryptionCleared = false;
#endif

        /// <summary>
        /// Defines the absolute default option values, should neither the user nor client specify anything.
        /// </summary>
        internal static TableRequestOptions BaseDefaultRequestOptions = new TableRequestOptions()
        {
            RetryPolicy = new NoRetry(),
            LocationMode = RetryPolicies.LocationMode.PrimaryOnly,
            ServerTimeout = null,
            MaximumExecutionTime = null,
            PayloadFormat = TablePayloadFormat.Json,
            PropertyResolver = null,
            ProjectSystemProperties = true,

#if !(WINDOWS_RT || NETCORE)
            EncryptionPolicy = null,
            RequireEncryption = null,
            EncryptionResolver = null
#endif
        };

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
                this.PropertyResolver = other.PropertyResolver;
                this.ProjectSystemProperties = other.ProjectSystemProperties;
#if !(WINDOWS_RT || NETCORE)
                this.EncryptionPolicy = other.EncryptionPolicy;
                this.RequireEncryption = other.RequireEncryption;
                this.EncryptionResolver = other.EncryptionResolver;
#endif
            }
        }

        internal static TableRequestOptions ApplyDefaults(TableRequestOptions requestOptions, CloudTableClient serviceClient)
        {
            TableRequestOptions modifiedOptions = new TableRequestOptions(requestOptions);
            
            modifiedOptions.RetryPolicy = 
                modifiedOptions.RetryPolicy 
                ?? serviceClient.DefaultRequestOptions.RetryPolicy 
                ?? BaseDefaultRequestOptions.RetryPolicy;

            modifiedOptions.LocationMode = 
                modifiedOptions.LocationMode 
                ?? serviceClient.DefaultRequestOptions.LocationMode 
                ?? BaseDefaultRequestOptions.LocationMode;

            modifiedOptions.ServerTimeout = 
                modifiedOptions.ServerTimeout 
                ?? serviceClient.DefaultRequestOptions.ServerTimeout 
                ?? BaseDefaultRequestOptions.ServerTimeout;

            modifiedOptions.MaximumExecutionTime = 
                modifiedOptions.MaximumExecutionTime 
                ?? serviceClient.DefaultRequestOptions.MaximumExecutionTime 
                ?? BaseDefaultRequestOptions.MaximumExecutionTime;

            modifiedOptions.PayloadFormat = 
                modifiedOptions.PayloadFormat 
                ?? serviceClient.DefaultRequestOptions.PayloadFormat 
                ?? BaseDefaultRequestOptions.PayloadFormat;

            if (!modifiedOptions.OperationExpiryTime.HasValue && modifiedOptions.MaximumExecutionTime.HasValue)
            {
                modifiedOptions.OperationExpiryTime = DateTime.Now + modifiedOptions.MaximumExecutionTime.Value;
            }

            modifiedOptions.PropertyResolver = 
                modifiedOptions.PropertyResolver 
                ?? serviceClient.DefaultRequestOptions.PropertyResolver 
                ?? BaseDefaultRequestOptions.PropertyResolver;

            modifiedOptions.ProjectSystemProperties = 
                modifiedOptions.ProjectSystemProperties 
                ?? serviceClient.DefaultRequestOptions.ProjectSystemProperties 
                ?? BaseDefaultRequestOptions.ProjectSystemProperties;

#if !(WINDOWS_RT || NETCORE)
            if (requestOptions != null && requestOptions.encryptionCleared)
            {
                modifiedOptions.encryptionCleared = true;
            }
            else
            {
                modifiedOptions.EncryptionPolicy =
                    modifiedOptions.EncryptionPolicy
                    ?? serviceClient.DefaultRequestOptions.EncryptionPolicy
                    ?? BaseDefaultRequestOptions.EncryptionPolicy;

                modifiedOptions.RequireEncryption =
                    modifiedOptions.RequireEncryption
                    ?? serviceClient.DefaultRequestOptions.RequireEncryption
                    ?? BaseDefaultRequestOptions.RequireEncryption;

                modifiedOptions.EncryptionResolver =
                    modifiedOptions.EncryptionResolver
                    ?? serviceClient.DefaultRequestOptions.EncryptionResolver
                    ?? BaseDefaultRequestOptions.EncryptionResolver;
            }
#endif

            return modifiedOptions;
        }

#if !(WINDOWS_RT || NETCORE)
        internal void ClearEncryption()
        {
            this.RequireEncryption = false;
            this.EncryptionPolicy = null;
            this.EncryptionResolver = null;
            this.encryptionCleared = true;
        }
#endif

        internal static TableRequestOptions ApplyDefaultsAndClearEncryption(TableRequestOptions requestOptions, CloudTableClient serviceClient)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, serviceClient);
#if !(WINDOWS_RT || NETCORE)
            modifiedOptions.ClearEncryption();
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

#if !(WINDOWS_RT || NETCORE)
        internal void AssertNoEncryptionPolicyOrStrictMode()
        {
            if (this.EncryptionPolicy != null)
            {
                throw new InvalidOperationException(SR.EncryptionNotSupportedForOperation);
            }

            this.AssertPolicyIfRequired();
        }

        internal void AssertPolicyIfRequired()
        {
            if (this.RequireEncryption.HasValue && this.RequireEncryption.Value && this.EncryptionPolicy == null)
            {
                throw new InvalidOperationException(SR.EncryptionPolicyMissingInStrictMode);
            }
        }
#endif

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
        /// Gets or sets the option to include system properties such as Partition Key and Row Key in queries.
        /// </summary>
        public bool? ProjectSystemProperties { get; set; }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Gets or sets the encryption policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="EncryptionPolicy"/>.</value>
        public TableEncryptionPolicy EncryptionPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate whether data written and read by the client library should be encrypted.
        /// </summary>
        /// <value>Use <c>true</c> to specify that data should be encrypted/decrypted for all transactions; otherwise, <c>false</c>.</value>
        public bool? RequireEncryption { get; set; }
#endif

        /// <summary>
        /// Gets or sets the location mode of the request.
        /// </summary>
        /// <value>A <see cref="Microsoft.Azure.Storage.RetryPolicies.LocationMode"/> enumeration value indicating the location mode of the request.</value>
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
                    this.payloadFormat = value.Value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the delegate that is used to get the <see cref="EdmType"/> for an entity property given the partition key, row key, and the property name. 
        /// </summary>
        public Func<string, string, string, string, EdmType> PropertyResolver { get; set; }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Gets or sets the delegate to get the value indicating whether or not a property should be encrypted, given the partition key, row key, 
        /// and property name. 
        /// </summary>
        public Func<string, string, string, bool> EncryptionResolver { get; set; }
#endif
    }
}
