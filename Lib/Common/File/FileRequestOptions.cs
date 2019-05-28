//-----------------------------------------------------------------------
// <copyright file="FileRequestOptions.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.File
{
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    
    /// <summary>
    /// Represents a set of timeout and retry policy options that may be specified for a request against the File service.
    /// </summary>
    public sealed class FileRequestOptions : IRequestOptions
    {
        /// <summary>
        /// Stores the parallelism factor.
        /// </summary>
        private int? parallelOperationThreadCount;

        /// <summary>
        /// Stores the maximum execution time.
        /// </summary>
        private TimeSpan? maximumExecutionTime;

        /// <summary>
        /// Defines the absolute default option values, should neither the user nor client specify anything.
        /// </summary>
        internal static FileRequestOptions BaseDefaultRequestOptions = new FileRequestOptions()
        {
            RetryPolicy = new NoRetry(),
            LocationMode = RetryPolicies.LocationMode.PrimaryOnly,

#if !(WINDOWS_RT || NETCORE)
            RequireEncryption = null,
#endif

            ServerTimeout = null,
            MaximumExecutionTime = null,
            ParallelOperationThreadCount = 1,

            ChecksumOptions = new ChecksumOptions
            {
#if WINDOWS_PHONE && WINDOWS_DESKTOP
                DisableContentMD5Validation = true,
                StoreContentMD5 = false,
                UseTransactionalMD5 = false,
                DisableContentCRC64Validation = true,
                StoreContentCRC64 = false,
                UseTransactionalCRC64 = false,
#else
                DisableContentMD5Validation = false,
                StoreContentMD5 = false,
                UseTransactionalMD5 = false,

                DisableContentCRC64Validation = false,
                StoreContentCRC64 = false,
                UseTransactionalCRC64 = false,
#endif
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="FileRequestOptions"/> class.
        /// </summary>
        public FileRequestOptions()
        {
        }

        /// <summary>
        /// Clones an instance of FileRequestOptions so that we can apply defaults.
        /// </summary>
        /// <param name="other">FileRequestOptions instance to be cloned.</param>
        internal FileRequestOptions(FileRequestOptions other)
            : this()
        {
            if (other != null)
            {
                this.RetryPolicy = other.RetryPolicy;

#if !(WINDOWS_RT || NETCORE)
                this.RequireEncryption = other.RequireEncryption;
#endif
                this.LocationMode = other.LocationMode;
                this.ServerTimeout = other.ServerTimeout;
                this.MaximumExecutionTime = other.MaximumExecutionTime;
                this.OperationExpiryTime = other.OperationExpiryTime;
                this.ChecksumOptions.CopyFrom(other.ChecksumOptions);
                this.ParallelOperationThreadCount = other.ParallelOperationThreadCount;
            }
        }

        internal static FileRequestOptions ApplyDefaults(FileRequestOptions options, CloudFileClient serviceClient, bool applyExpiry = true)
        {
            FileRequestOptions modifiedOptions = new FileRequestOptions(options);

            modifiedOptions.RetryPolicy = 
                modifiedOptions.RetryPolicy 
                ?? serviceClient.DefaultRequestOptions.RetryPolicy 
                ?? BaseDefaultRequestOptions.RetryPolicy;

            modifiedOptions.LocationMode = 
                modifiedOptions.LocationMode 
                ?? serviceClient.DefaultRequestOptions.LocationMode 
                ?? BaseDefaultRequestOptions.LocationMode;

#if !(WINDOWS_RT || NETCORE)
            modifiedOptions.RequireEncryption = 
                modifiedOptions.RequireEncryption 
                ?? serviceClient.DefaultRequestOptions.RequireEncryption 
                ?? BaseDefaultRequestOptions.RequireEncryption;
#endif

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

            if (applyExpiry && !modifiedOptions.OperationExpiryTime.HasValue && modifiedOptions.MaximumExecutionTime.HasValue)
            {
                modifiedOptions.OperationExpiryTime = DateTime.Now + modifiedOptions.MaximumExecutionTime.Value;
            }

#if WINDOWS_PHONE && WINDOWS_DESKTOP
            modifiedOptions.ChecksumOptions.DisableContentMD5Validation =  BaseDefaultRequestOptions.ChecksumOptions.DisableContentMD5Validation;
            modifiedOptions.ChecksumOptions.StoreContentMD5 = BaseDefaultRequestOptions.ChecksumOptions.StoreFileContentMD5;
            modifiedOptions.ChecksumOptions.UseTransactionalMD5 = BaseDefaultRequestOptions.ChecksumOptions.UseTransactionalMD5;

            modifiedOptions.ChecksumOptions.DisableContentCRC64Validation =  BaseDefaultRequestOptions.ChecksumOptions.DisableContentCRC64Validation;
            modifiedOptions.ChecksumOptions.StoreContentCRC64 = BaseDefaultRequestOptions.ChecksumOptions.StoreFileContentCRC64;
            modifiedOptions.ChecksumOptions.UseTransactionalCRC64 = BaseDefaultRequestOptions.ChecksumOptions.UseTransactionalCRC64;
#else
            modifiedOptions.ChecksumOptions.DisableContentMD5Validation = 
                modifiedOptions.ChecksumOptions.DisableContentMD5Validation 
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.DisableContentMD5Validation 
                ?? BaseDefaultRequestOptions.ChecksumOptions.DisableContentMD5Validation;

            modifiedOptions.ChecksumOptions.StoreContentMD5 = 
                modifiedOptions.ChecksumOptions.StoreContentMD5 
                ?? serviceClient.DefaultRequestOptions.ChecksumOptions.StoreContentMD5 
                ?? BaseDefaultRequestOptions.ChecksumOptions.StoreContentMD5;

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
                ?? BaseDefaultRequestOptions.ChecksumOptions.StoreContentCRC64;

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

        /// <summary>
        /// Gets or sets the absolute expiry time across all potential retries for the request. 
        /// </summary>
        internal DateTime? OperationExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets the retry policy.
        /// </summary>
        /// <value>The retry policy.</value>
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the location mode of the request.
        /// </summary>
        /// <value>The location mode of the request.</value>
        public LocationMode? LocationMode
        {
            get
            {
                return Microsoft.Azure.Storage.RetryPolicies.LocationMode.PrimaryOnly;
            }

            set
            {
                if (value != Microsoft.Azure.Storage.RetryPolicies.LocationMode.PrimaryOnly)
                {
                    throw new NotSupportedException(SR.PrimaryOnlyCommand);
                }
            }
        }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Gets or sets a value to indicate whether data written and read by the client library should be encrypted.
        /// </summary>
        /// <value>Use <c>true</c> to specify that data should be encrypted/decrypted for all transactions; otherwise, <c>false</c>.</value>
        public bool? RequireEncryption 
        {
            get
            {
                return false;
            }

            set
            {
                if (value.HasValue && value.Value)
                {
                    throw new NotSupportedException(SR.EncryptionNotSupportedForFiles);
                }
            }
        }
#endif

        /// <summary>
        /// Gets or sets the server timeout interval for the request.
        /// </summary>
        /// <value>The server timeout interval for the request.</value>
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
        /// Gets or sets the number of ranges that may be simultaneously uploaded when uploading a file.
        /// </summary>
        /// <value>The number of parallel operations that may proceed.</value>
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
        /// Gets or sets a value to calculate and send/validate content MD5 for transactions.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and send/validate content MD5 for transactions; otherwise, <c>false</c>.</value>       
#if WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#endif
        public bool? UseTransactionalMD5
        {
            get => this.ChecksumOptions.UseTransactionalMD5;
            set => this.ChecksumOptions.UseTransactionalMD5 = value;
        }

        /// <summary>
        /// Gets or sets a value to indicate that an MD5 hash will be calculated and stored when uploading a file.
        /// </summary>
        /// <value>Use true to calculate and store an MD5 hash when uploading a file; otherwise, false.</value>
#if WINDOWS_PHONE && WINDOWS_DESKTOP
        /// <remarks>This property is not supported for Windows Phone.</remarks>
#endif
        public bool? StoreFileContentMD5
        {
            get => this.ChecksumOptions.StoreContentMD5;
            set => this.ChecksumOptions.StoreContentMD5 = value;
        }

        /// <summary>
        /// Gets or sets a value to indicate that MD5 validation will be disabled when downloading files.
        /// </summary>
        /// <value>Use true to disable MD5 validation; false to enable MD5 validation.</value>
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
