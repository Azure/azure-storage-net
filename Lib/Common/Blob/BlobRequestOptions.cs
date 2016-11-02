//-----------------------------------------------------------------------
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
#if !(WINDOWS_RT || NETCORE)
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
        /// Defines the absolute default option values, should neither the user nor client specify anything.
        /// </summary>
        internal static BlobRequestOptions BaseDefaultRequestOptions = new BlobRequestOptions()
        {
            RetryPolicy = new NoRetry(),
            AbsorbConditionalErrorsOnRetry = false,

#if !(WINDOWS_RT || NETCORE)
            EncryptionPolicy = null,
            RequireEncryption = null,
#endif
            LocationMode = RetryPolicies.LocationMode.PrimaryOnly,
            ServerTimeout = null,
            MaximumExecutionTime = null,
            ParallelOperationThreadCount = 1,
            SingleBlobUploadThresholdInBytes = Constants.MaxSingleUploadBlobSize / 2,
            
#if (WINDOWS_PHONE && WINDOWS_DESKTOP)  
            DisableContentMD5Validation = true,
            StoreBlobContentMD5 = false,
            UseTransactionalMD5 = false,
#else
            DisableContentMD5Validation = false,
            //// StoreBlobContentMD5 = (blobType == BlobType.BlockBlob), // must be computed in ApplyDefaults
            UseTransactionalMD5 = false,
#endif
        };

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
                this.AbsorbConditionalErrorsOnRetry = other.AbsorbConditionalErrorsOnRetry;
#if !(WINDOWS_RT || NETCORE)
                this.EncryptionPolicy = other.EncryptionPolicy;
                this.RequireEncryption = other.RequireEncryption;
                this.SkipEncryptionPolicyValidation = other.SkipEncryptionPolicyValidation;
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

            modifiedOptions.RetryPolicy = 
                modifiedOptions.RetryPolicy 
                ?? serviceClient.DefaultRequestOptions.RetryPolicy 
                ?? BaseDefaultRequestOptions.RetryPolicy;

            modifiedOptions.AbsorbConditionalErrorsOnRetry = 
                modifiedOptions.AbsorbConditionalErrorsOnRetry 
                ?? serviceClient.DefaultRequestOptions.AbsorbConditionalErrorsOnRetry 
                ?? BaseDefaultRequestOptions.AbsorbConditionalErrorsOnRetry;

#if !(WINDOWS_RT || NETCORE)
            modifiedOptions.EncryptionPolicy = 
                modifiedOptions.EncryptionPolicy 
                ?? serviceClient.DefaultRequestOptions.EncryptionPolicy 
                ?? BaseDefaultRequestOptions.EncryptionPolicy;

            modifiedOptions.RequireEncryption = 
                modifiedOptions.RequireEncryption 
                ?? serviceClient.DefaultRequestOptions.RequireEncryption 
                ?? BaseDefaultRequestOptions.RequireEncryption;
#endif

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

            modifiedOptions.ParallelOperationThreadCount = 
                modifiedOptions.ParallelOperationThreadCount 
                ?? serviceClient.DefaultRequestOptions.ParallelOperationThreadCount 
                ?? BaseDefaultRequestOptions.ParallelOperationThreadCount;

            modifiedOptions.SingleBlobUploadThresholdInBytes = 
                modifiedOptions.SingleBlobUploadThresholdInBytes 
                ?? serviceClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes 
                ?? BaseDefaultRequestOptions.SingleBlobUploadThresholdInBytes;
            
            if (applyExpiry && !modifiedOptions.OperationExpiryTime.HasValue && modifiedOptions.MaximumExecutionTime.HasValue)
            {
                modifiedOptions.OperationExpiryTime = DateTime.Now + modifiedOptions.MaximumExecutionTime.Value;
            }

#if (WINDOWS_PHONE && WINDOWS_DESKTOP)  
            modifiedOptions.DisableContentMD5Validation = BaseDefaultRequestOptions.DisableContentMD5Validation;
            modifiedOptions.StoreBlobContentMD5 = BaseDefaultRequestOptions.StoreBlobContentMD5;
            modifiedOptions.UseTransactionalMD5 = BaseDefaultRequestOptions.UseTransactionalMD5;
#else
            modifiedOptions.DisableContentMD5Validation = 
                modifiedOptions.DisableContentMD5Validation 
                ?? serviceClient.DefaultRequestOptions.DisableContentMD5Validation 
                ?? BaseDefaultRequestOptions.DisableContentMD5Validation;

            modifiedOptions.StoreBlobContentMD5 = 
                modifiedOptions.StoreBlobContentMD5 
                ?? serviceClient.DefaultRequestOptions.StoreBlobContentMD5 
                ?? (blobType == BlobType.BlockBlob); // must be computed

            modifiedOptions.UseTransactionalMD5 = 
                modifiedOptions.UseTransactionalMD5 
                ?? serviceClient.DefaultRequestOptions.UseTransactionalMD5 
                ?? BaseDefaultRequestOptions.UseTransactionalMD5;
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

#if !(WINDOWS_RT || NETCORE)
        internal void AssertNoEncryptionPolicyOrStrictMode()
        {
            // Throw if an encryption policy is set and encryption validation is on
            if (this.EncryptionPolicy != null && !this.SkipEncryptionPolicyValidation)
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

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Gets or sets the encryption policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="EncryptionPolicy"/>.</value>
        public BlobEncryptionPolicy EncryptionPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate whether data written and read by the client library should be encrypted.
        /// </summary>
        /// <value>Use <c>true</c> to specify that data should be encrypted/decrypted for all transactions; otherwise, <c>false</c>.</value>
        public bool? RequireEncryption { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate whether validating the presence of the encryption policy should be skipped.
        /// </summary>
        /// <value>Use <c>true</c> to skip validation; otherwise, <c>false</c>.</value>
        internal bool SkipEncryptionPolicyValidation { get; set; }
#endif

        /// <summary>
        /// Gets or sets a value that indicates whether a conditional failure should be absorbed on a retry attempt
        /// for the request. 
        /// </summary>
        /// <remarks>
        /// This option is used only by the <see cref="CloudAppendBlob"/> object in the <b>UploadFrom*</b> methods and
        /// the <b>BlobWriteStream</b> methods. By default, it is set to <c>false</c>. Set this option to <c>true</c> only for single writer scenarios.
        /// Setting this option to <c>true</c> in a multi-writer scenario may lead to corrupted blob data.
        /// </remarks>
        public bool? AbsorbConditionalErrorsOnRetry { get; set; }

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
#endif
                this.useTransactionalMD5 = value;
            }
        }

        private bool? useTransactionalMD5;

        /// <summary>
        /// Gets or sets a value to indicate that an MD5 hash will be calculated and stored when uploading a blob.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and store an MD5 hash when uploading a blob; otherwise, <c>false</c>.</value>
        /// <remarks>This property is not supported for the <see cref="CloudAppendBlob"/> Append* APIs.</remarks>
#if  WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
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
#endif
                this.disableContentMD5Validation = value;
            }
        }

        private bool? disableContentMD5Validation;
    }
}
