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

namespace Microsoft.Azure.Storage.Blob
{
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.Azure.Storage.Shared.Protocol;
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
        /// Indicates the maximum size of a blob, in bytes, that may be uploaded as a single blob,
        /// ranging from between 1 and 256 MB inclusive. The Default is 128 MB.
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

            ChecksumOptions = new ChecksumOptions
            {
#if (WINDOWS_PHONE && WINDOWS_DESKTOP)
                DisableContentMD5Validation = true,
                StoreContentMD5 = false,
                UseTransactionalMD5 = false,
                DisableContentCRC64Validation = true,
                StoreContentCRC64 = false,
                UseTransactionalCRC64 = false,
#else
                DisableContentMD5Validation = false,
                //// StoreContentMD5 = (blobType == BlobType.BlockBlob), // must be computed in ApplyDefaults
                UseTransactionalMD5 = false,

                DisableContentCRC64Validation = false,
                //// StoreContentCRC64 = (blobType == BlobType.BlockBlob), // must be computed in ApplyDefaults
                UseTransactionalCRC64 = false,
#endif
            }

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
                this.ChecksumOptions.CopyFrom(other.ChecksumOptions);
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
            modifiedOptions.ChecksumOptions.DisableContentMD5Validation = BaseDefaultRequestOptions.DisableContentMD5Validation;
            modifiedOptions.ChecksumOptions.StoreContentMD5 = BaseDefaultRequestOptions.ChecksumOptions.StoreBlobContentMD5;
            modifiedOptions.ChecksumOptions.UseTransactionalMD5 = BaseDefaultRequestOptions.UseTransactionalMD5;

            modifiedOptions.ChecksumOptions.DisableContentCRC64Validation = BaseDefaultRequestOptions.DisableContentCRC64Validation;
            modifiedOptions.ChecksumOptions.StoreContentCRC64 = BaseDefaultRequestOptions.ChecksumOptions.StoreBlobContentCRC64;
            modifiedOptions.ChecksumOptions.UseTransactionalCRC64 = BaseDefaultRequestOptions.UseTransactionalCRC64;
#else
            modifiedOptions.ChecksumOptions.DisableContentMD5Validation = 
                modifiedOptions.ChecksumOptions.DisableContentMD5Validation 
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.DisableContentMD5Validation 
                ?? BaseDefaultRequestOptions.ChecksumOptions.DisableContentMD5Validation;

            modifiedOptions.ChecksumOptions.StoreContentMD5 = 
                modifiedOptions.ChecksumOptions.StoreContentMD5 
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.StoreContentMD5 
                ?? (blobType == BlobType.BlockBlob); // must be computed

            modifiedOptions.ChecksumOptions.UseTransactionalMD5 = 
                modifiedOptions.ChecksumOptions.UseTransactionalMD5 
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.UseTransactionalMD5 
                ?? BaseDefaultRequestOptions.ChecksumOptions.UseTransactionalMD5;

            modifiedOptions.ChecksumOptions.DisableContentCRC64Validation =
                modifiedOptions.ChecksumOptions.DisableContentCRC64Validation
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.DisableContentCRC64Validation
                ?? BaseDefaultRequestOptions.ChecksumOptions.DisableContentCRC64Validation;

            modifiedOptions.ChecksumOptions.StoreContentCRC64 =
                modifiedOptions.ChecksumOptions.StoreContentCRC64
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.StoreContentCRC64
                ?? (blobType == BlobType.BlockBlob); // must be computed

            modifiedOptions.ChecksumOptions.UseTransactionalCRC64 =
                modifiedOptions.ChecksumOptions.UseTransactionalCRC64
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.UseTransactionalCRC64
                ?? BaseDefaultRequestOptions.ChecksumOptions.UseTransactionalCRC64;
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
        /// <remarks> Retry policies instruct the Storage Client to retry failed requests.
        /// By default, only some failures are retried. For example, connection failures and 
        /// throttling failures can be retried. Resource not found (404) or authentication 
        /// failures are not retried, because these are not likely to succeed on retry.
        /// If not set, the Storage Client uses an exponential backoff retry policy, where the wait time gets
        /// exponentially longer between requests, up to a total of around 30 seconds.
        /// The default retry policy is recommended for most scenarios.
        /// 
        ///## Examples
        ///  [!code-csharp[Retry_Policy_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_RetryPolicy "Retry Policy Sample")]         
        ///</remarks>
        public IRetryPolicy RetryPolicy { get; set; }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Gets or sets the encryption policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="EncryptionPolicy"/>.</value>
        /// <remarks>
        /// ## Examples
        /// [!code-csharp[Encryption_Policy_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobClientEncryptionTests.cs#sample_RequestOptions_EncryptionPolicy "Encryption Policy Sample")]        
        /// </remarks>
        public BlobEncryptionPolicy EncryptionPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate whether data written and read by the client library should be encrypted.
        /// </summary>
        /// <value>Use <c>true</c> to specify that data should be encrypted/decrypted for all transactions; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// RequireEncryption here refers to Client-Side Encryption.
        /// If this value is set to <c>true</c>, all calls will fail if the data
        /// is not encrypted/decrypted with an encryption policy. If this value 
        /// is false (the default), any data being downloaded that is not encrypted
        /// will be returned as-is.
        /// 
        ///  ## Examples
        ///  [!code-csharp[Require_Encryption_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_RequireEncryption "Require Encryption Sample")]        
        /// </remarks>
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
        /// This option is used only by the <see cref="CloudAppendBlob"/> object in the <b>UploadFrom*</b> methods, 
        /// the <b>AppendFrom*</b> methods, and the <b>BlobWriteStream</b> class. By default, it is set to <c>false</c>. 
        /// Set this option to <c>true</c> only for single-writer scenarios. 
        /// Setting this option to <c>true</c> in a multi-writer scenario may lead to corrupted blob data.
        /// 
        /// When calling "UploadFrom*" on an append blob, the Storage Client breaks the input data
        /// up into a number of data blocks, and uploads each data block with an "append block" operation.
        /// Normally, an "IfAppendPositionEqual" access condition is added to the append block operation, so that the
        /// upload operation will fail if some other process somewhere has appended data in the middle of this data stream.
        /// However, this can result in a false failure in a very specific case. If an append operation fails with a timeout,
        /// there is a chance that the operation succeeded on the service, but the "success" response did not make it back to the client.
        /// In this case, the client will retry, and then get an "append position not met" failure.
        /// 
        /// Setting this value to <c>true</c> results in the upload operation continuing when it sees an "append position not met"
        /// failure on retry - accounting for the above possibility. However, this loses protection in the multi-writer
        /// scenario - if multiple threads are uploading to the blob at once, and this value is set to <c>true</c>, some data
        /// may be lost, because the client thinks the data was uploaded, when in fact it was the other process' data.
        /// 
        ///## Examples
        ///[!code-csharp[Absorb_Conditional_Errors_On_Retry_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/CloudAppendBlobTest.cs#sample_BlobRequestOptions_AbsorbConditionalErrorsOnRetry "Absorb Conditional Errors On Retry Sample")]  
        /// </remarks>
        public bool? AbsorbConditionalErrorsOnRetry { get; set; }

        /// <summary>
        /// Gets or sets the location mode of the request.
        /// </summary>
        /// <value>A <see cref="Microsoft.Azure.Storage.RetryPolicies.LocationMode"/> enumeration value indicating the location mode of the request.</value>
        /// <remarks>The LocationMode specifies in which locations the Storage Client 
        /// will attempt to make the request. This is only valid for RA-GRS accounts - accounts 
        /// where data can be read from either the primary or the secondary endpoint.
        ///  ## Examples
        ///  [!code-csharp[LocationMode_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_LocationMode "LocationMode Sample")]        
        ///</remarks>
        public LocationMode? LocationMode { get; set; }

        /// <summary>
        /// Gets or sets the server timeout interval for a single HTTP request.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval for each HTTP request.</value>
        /// <remarks>
        /// The server timeout is the timeout sent to the Azure Storage service 
        /// for each REST request made. If the API called makes multiple REST calls 
        /// (UploadFromStream, for example, or if the request retries), this timeout 
        /// will be applied separately to each request. This value is not 
        /// tracked or validated on the client, it is only passed to the Storage service.
        /// 
        ///  ## Examples
        ///  [!code-csharp[Server_Timeout_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_ServerTimeout_MaximumExecutionTime "Server Timeout Sample")]         
        /// </remarks>
        public TimeSpan? ServerTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time across all potential retries for the request. 
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> representing the maximum execution time for retries for the request.</value>
        /// <remarks>
        /// The maximum execution time is the time allotted for a single API call.
        /// If the total amount of time spent in the API - across all REST requests, 
        /// retries, etc - exceeds this value, the client will timeout. This value 
        /// is only tracked on the client, it is not sent to the service.
        ///  ## Examples
        ///  [!code-csharp[Maximum_Execution_Time_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_ServerTimeout_MaximumExecutionTime "Maximum Execution Time Sample")]        
        /// </remarks>
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
        /// Gets or sets the number of blocks that may be simultaneously uploaded.
        /// </summary>
        /// <value>An integer value indicating the number of parallel blob upload operations that may proceed.</value>
        /// <remarks>
        /// When using the UploadFrom* methods on a blob, the blob will be broken up into blocks. Setting this 
        /// value limits the number of outstanding I/O "put block" requests that the library will have in-flight 
        /// at a given time. Default is 1 (no parallelism). Setting this value higher may result in 
        /// faster blob uploads, depending on the network between the client and the Azure Storage service.
        /// If blobs are small (less than 256 MB), keeping this value equal to 1 is advised.
        ///  ## Examples
        ///  [!code-csharp[Parallel_Operation_ThreadCount_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_BlobRequestOptions_ParallelOperationThreadCount "Parallel Operation ThreadCount Sample")]        
        ///</remarks>
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
        /// ranging from between 1 and 256 MB inclusive.</value>
        /// <remarks>This value will be ignored if the ParallelOperationThreadCount is set to a value greater than 1</remarks>
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
        /// <value>Use <c>true</c> to calculate and send/validate content MD5 for transactions; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        /// <remarks>
        /// The UseTransactionalMD5 option instructs the Storage Client to calculate and validate 
        /// the MD5 hash of individual Storage REST operations. For a given REST operation, 
        /// if this value is set, both the Storage Client and the Storage service will calculate
        /// the MD5 hash of the transferred data, and will fail if the values do not match.
        /// This value is not persisted on the service or the client.
        /// This option applies to both upload and download operations.
        /// Note that HTTPS does a similar check during transit. If you are using HTTPS, 
        /// we recommend this feature be off.
        ///  ## Examples
        ///  [!code-csharp[Use_Transactional_MD5_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/MD5FlagsTest.cs#sample_BlobRequestOptions_UseTransactionalMD5 "Use Transactional MD5 Sample")] 
        /// </remarks>
#if WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#endif
        public bool? UseTransactionalMD5
        {
            get => this.ChecksumOptions.UseTransactionalMD5;
            set => this.ChecksumOptions.UseTransactionalMD5 = value;
        }

        /// <summary>
        /// Gets or sets a value to indicate that an MD5 hash will be calculated and stored when uploading a blob.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and store an MD5 hash when uploading a blob; otherwise, <c>false</c>. Defaults to <c>false</c>.</value>
        /// <remarks>This property is not supported for the <see cref="CloudAppendBlob"/> Append* APIs.
        /// The StoreBlobContentMD5 request option instructs the Storage Client to calculate the MD5 hash 
        /// of the blob content during an upload operation. This value is then stored on the 
        /// blob object as the Content-MD5 header. This option applies only to upload operations. 
        /// This is useful for validating the integrity of the blob upon later download, and 
        /// compatible with the Content-MD5 header as defined in the HTTP spec. If using 
        /// the Storage Client for later download, if the Content-MD5 header is present, 
        /// the MD5 hash of the content will be validated, unless "DisableContentMD5Validation" is set.
        /// Note that this value is not validated on the Azure Storage service on either upload or download of data; 
        /// it is merely stored and returned.
        ///  ## Examples
        ///  [!code-csharp[Store_Blob_Content_MD5_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/MD5FlagsTest.cs#sample_BlobRequestOptions_StoreBlobContentMD5 "Store Blob Content MD5 Sample")] 
        /// </remarks>
#if WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#endif
        public bool? StoreBlobContentMD5
        {
            get => this.ChecksumOptions.StoreContentMD5;
            set => this.ChecksumOptions.StoreContentMD5 = value;
        }

        /// <summary>
        /// Gets or sets a value to indicate that MD5 validation will be disabled when downloading blobs.
        /// </summary>
        /// <value>Use <c>true</c> to disable MD5 validation; <c>false</c> to enable MD5 validation. Default is <c>false</c>.</value>
        /// <remarks>
        /// When downloading a blob, if the value already exists on the blob, the Storage service 
        /// will include the MD5 hash of the entire blob as a header. This option controls 
        /// whether or not the Storage Client will validate that MD5 hash on download.
        /// See <see cref="BlobRequestOptions.ChecksumOptions.StoreBlobContentMD5"/> for more details.
        /// 
        ///## Examples
        ///[!code-csharp[Disable_Content_MD5_Validation_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/MD5FlagsTest.cs#sample_BlobRequestOptions_DisableContentMD5Validation "Disable Content MD5 Validation Sample")]        
        /// </remarks>
#if WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#endif
        public bool? DisableContentMD5Validation
        {
            get => this.ChecksumOptions.DisableContentMD5Validation;
            set => this.ChecksumOptions.DisableContentMD5Validation = value;
        }

        public ChecksumOptions ChecksumOptions { get; set; } = new ChecksumOptions();
    }
}
