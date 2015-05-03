﻿//-----------------------------------------------------------------------
// <copyright file="BlobRequestOptions.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
#if !(WINDOWS_RT || ASPNET_K || PORTABLE)
    using System.Security.Cryptography;
#endif
    
    /// <summary>
    /// Represents a set of timeout and retry policy options that may be specified for a request against the Blob service.
    /// </summary>
    public sealed class BlobRequestOptions : IRequestOptions
    {
        /// <summary>
        /// Stores the parallelism factor.
        /// </summary>
        private int? parallelOperationThreadCount;

        /// <summary>
        /// Default is 32 MB.
        /// </summary>
        private long? singleBlobUploadThresholdInBytes;

        /// <summary>
        /// Stores the maximum execution time.
        /// </summary>
        private TimeSpan? maximumExecutionTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobRequestOptions"/> class.
        /// </summary>
        public BlobRequestOptions()
        {
        }

        /// <summary>
        /// Clones an instance of BlobRequestOptions so that we can apply defaults.
        /// </summary>
        /// <param name="other">BlobRequestOptions instance to be cloned.</param>
        internal BlobRequestOptions(BlobRequestOptions other)
        {
            if (other != null)
            {
                this.RetryPolicy = other.RetryPolicy;
#if !(WINDOWS_RT || ASPNET_K || PORTABLE)
                this.EncryptionPolicy = other.EncryptionPolicy;
#endif
                this.LocationMode = other.LocationMode;
                this.ServerTimeout = other.ServerTimeout;
                this.MaximumExecutionTime = other.MaximumExecutionTime;
                this.OperationExpiryTime = other.OperationExpiryTime;
                this.UseTransactionalMD5 = other.UseTransactionalMD5;
                this.StoreBlobContentMD5 = other.StoreBlobContentMD5;
                this.DisableContentMD5Validation = other.DisableContentMD5Validation;
                this.ParallelOperationThreadCount = other.ParallelOperationThreadCount;
                this.SingleBlobUploadThresholdInBytes = other.SingleBlobUploadThresholdInBytes;
            }
        }

        internal static BlobRequestOptions ApplyDefaults(BlobRequestOptions options, BlobType blobType, CloudBlobClient serviceClient, bool applyExpiry = true)
        {
            BlobRequestOptions modifiedOptions = new BlobRequestOptions(options);

            modifiedOptions.RetryPolicy = modifiedOptions.RetryPolicy ?? serviceClient.DefaultRequestOptions.RetryPolicy;
#if !(WINDOWS_RT || ASPNET_K || PORTABLE)
            modifiedOptions.EncryptionPolicy = modifiedOptions.EncryptionPolicy ?? serviceClient.DefaultRequestOptions.EncryptionPolicy;
#endif

            modifiedOptions.LocationMode = (modifiedOptions.LocationMode 
                                            ?? serviceClient.DefaultRequestOptions.LocationMode) 
                                            ?? RetryPolicies.LocationMode.PrimaryOnly;
            modifiedOptions.ServerTimeout = modifiedOptions.ServerTimeout ?? serviceClient.DefaultRequestOptions.ServerTimeout;
            modifiedOptions.MaximumExecutionTime = modifiedOptions.MaximumExecutionTime ?? serviceClient.DefaultRequestOptions.MaximumExecutionTime;
            modifiedOptions.ParallelOperationThreadCount = (modifiedOptions.ParallelOperationThreadCount
                                                           ?? serviceClient.DefaultRequestOptions.ParallelOperationThreadCount)
                                                           ?? 1;
            modifiedOptions.SingleBlobUploadThresholdInBytes = (modifiedOptions.SingleBlobUploadThresholdInBytes
                                                               ?? serviceClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes)
                                                               ?? Constants.MaxSingleUploadBlobSize / 2;
            
            if (applyExpiry && !modifiedOptions.OperationExpiryTime.HasValue && modifiedOptions.MaximumExecutionTime.HasValue)
            {
                modifiedOptions.OperationExpiryTime = DateTime.Now + modifiedOptions.MaximumExecutionTime.Value;
            }

#if (WINDOWS_PHONE && WINDOWS_DESKTOP) || PORTABLE
            modifiedOptions.DisableContentMD5Validation = true;
            modifiedOptions.StoreBlobContentMD5 = false;
            modifiedOptions.UseTransactionalMD5 = false;
#else
            modifiedOptions.DisableContentMD5Validation = (modifiedOptions.DisableContentMD5Validation
                                                            ?? serviceClient.DefaultRequestOptions.DisableContentMD5Validation) 
                                                            ?? false;
            modifiedOptions.StoreBlobContentMD5 = (modifiedOptions.StoreBlobContentMD5 
                                                    ?? serviceClient.DefaultRequestOptions.StoreBlobContentMD5)
                                                    ?? blobType == BlobType.BlockBlob;
            modifiedOptions.UseTransactionalMD5 = (modifiedOptions.UseTransactionalMD5 
                                                    ?? serviceClient.DefaultRequestOptions.UseTransactionalMD5)
                                                    ?? false;
#endif

            return modifiedOptions;
        }

        internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
        {
            if (this.LocationMode.HasValue)
            {
                cmd.LocationMode = this.LocationMode.Value;
            }

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

#if !(WINDOWS_RT || ASPNET_K || PORTABLE)
        internal void AssertNoEncryptionPolicy()
        {
            if (this.EncryptionPolicy != null)
            {
                throw new InvalidOperationException(SR.EncryptionNotSupportedForOperation);
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

#if !(WINDOWS_RT || ASPNET_K || PORTABLE)
        /// <summary>
        /// Gets or sets the encryption policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="EncryptionPolicy"/>.</value>
        public BlobEncryptionPolicy EncryptionPolicy { get; set; }
#endif

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
        /// Gets or sets the maximum execution time across all potential retries for the request. 
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
        /// Gets or sets the number of blocks that may be simultaneously uploaded when uploading a blob that is greater than 
        /// the value specified by the <see cref="SingleBlobUploadThresholdInBytes"/> property in size.
        /// </summary>
        /// <value>An integer value indicating the number of parallel blob upload operations that may proceed.</value>
        public int? ParallelOperationThreadCount
        {
            get
            {
                return this.parallelOperationThreadCount;
            }

            set
            {
                if (value.HasValue)
                {
                    CommonUtility.AssertInBounds("ParallelOperationThreadCount", value.Value, 1, Constants.MaxParallelOperationThreadCount);
                }

                this.parallelOperationThreadCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum size of a blob in bytes that may be uploaded as a single blob. 
        /// </summary>
        /// <value>A long indicating the maximum size of a blob, in bytes, that may be uploaded as a single blob,
        /// ranging from between 1 and 64 MB inclusive.</value>
        public long? SingleBlobUploadThresholdInBytes
        {
            get
            {
                return this.singleBlobUploadThresholdInBytes;
            }

            set
            {
                if (value.HasValue)
                {
                    CommonUtility.AssertInBounds("SingleBlobUploadThresholdInBytes", value.Value, 1 * Constants.MB, Constants.MaxSingleUploadBlobSize);
                }

                this.singleBlobUploadThresholdInBytes = value;
            }
        }

        /// <summary>
        /// Gets or sets a value to calculate and send/validate content MD5 for transactions.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and send/validate content MD5 for transactions; otherwise, <c>false</c>.</value>       
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#elif PORTABLE
        /// <remarks>This property is not supported for Portable Class Library.</remarks>
#endif
        public bool? UseTransactionalMD5
        {
            get
            {
                return this.useTransactionalMD5;
            }

            set
            {
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
                if (value.HasValue && value.Value)
                {
                    throw new NotSupportedException(SR.WindowsPhoneDoesNotSupportMD5);
                }
#elif PORTABLE
                if (value.HasValue && value.Value)
                {
                    throw new NotSupportedException(SR.PortableDoesNotSupportMD5);
                }
#endif
                this.useTransactionalMD5 = value;
            }
        }

        private bool? useTransactionalMD5;

        /// <summary>
        /// Gets or sets a value to indicate that an MD5 hash will be calculated and stored when uploading a blob.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and store an MD5 hash when uploading a blob; otherwise, <c>false</c>.</value>
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#elif PORTABLE
        /// <remarks>This property is not supported for Portable Class Library.</remarks>
#endif
        public bool? StoreBlobContentMD5
        {
            get
            {
                return this.storeBlobContentMD5;
            }

            set
            {
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
                if (value.HasValue && value.Value)
                {
                    throw new NotSupportedException(SR.WindowsPhoneDoesNotSupportMD5);
                }
#elif PORTABLE
                if (value.HasValue && value.Value)
                {
                    throw new NotSupportedException(SR.PortableDoesNotSupportMD5);
                }
#endif
                this.storeBlobContentMD5 = value;
            }
        }

        private bool? storeBlobContentMD5;

        /// <summary>
        /// Gets or sets a value to indicate that MD5 validation will be disabled when downloading blobs.
        /// </summary>
        /// <value>Use <c>true</c> to disable MD5 validation; <c>false</c> to enable MD5 validation.</value>
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#elif PORTABLE
        /// <remarks>This property is not supported for Portable Class Library.</remarks>
#endif
        public bool? DisableContentMD5Validation
        {
            get
            {
                return this.disableContentMD5Validation;
            }

            set
            {
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
                if (value.HasValue && !value.Value)
                {
                    throw new NotSupportedException(SR.WindowsPhoneDoesNotSupportMD5);
                }
#elif PORTABLE
                if (value.HasValue && !value.Value)
                {
                    throw new NotSupportedException(SR.PortableDoesNotSupportMD5);
                }
#endif
                this.disableContentMD5Validation = value;
            }
        }

        private bool? disableContentMD5Validation;
    }
}
