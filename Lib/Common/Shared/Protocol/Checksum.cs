// -----------------------------------------------------------------------------------------
// <copyright file="Checksum.cs" company="Microsoft">
//    Copyright 2018 Microsoft Corporation
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

using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Util;

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    public sealed class Checksum
    {
        public static Checksum None => new Checksum();

        public Checksum(string md5 = default(string), string crc64 = default(string))
        {
            this.MD5 = !string.IsNullOrEmpty(md5) ? md5 : default(string);
            this.CRC64 = !string.IsNullOrEmpty(crc64) ? crc64 : default(string);
        }

        public string MD5 { get; set; }
        public string CRC64 { get; set; }

        internal bool HasAny
            => !string.IsNullOrEmpty(this.MD5)
            || !string.IsNullOrEmpty(this.CRC64)
            ;

        public static implicit operator Checksum(string md5) => new Checksum(md5: md5);
    }

    internal struct ChecksumRequested
    {
        public static ChecksumRequested None => new ChecksumRequested();

        public ChecksumRequested(bool md5, bool crc64)
        {
            this.MD5 = md5;
            this.CRC64 = crc64;
        }

        public bool MD5 { get; }
        public bool CRC64 { get; }

        internal bool HasAny => this.MD5 || this.CRC64;

        internal void AssertInBounds(long? offset, long? count, int maxMd5 = int.MaxValue, int maxCrc64 = int.MaxValue)
        {
            if (offset.HasValue && this.HasAny)
            {
                CommonUtility.AssertNotNull("count", count);
                CommonUtility.AssertInBounds("count", count.Value, 1, this.MD5 ? maxMd5 : int.MaxValue);
                CommonUtility.AssertInBounds("count", count.Value, 1, this.CRC64 ? maxCrc64 : int.MaxValue);
            }
        }
    }

    public sealed class ChecksumOptions
    {
        #region MD5
        /// <summary>
        /// Gets or sets a value to indicate that MD5 validation will be disabled when downloading blobs.
        /// </summary>
        /// <value>Use <c>true</c> to disable MD5 validation; <c>false</c> to enable MD5 validation. Default is <c>false</c>.</value>
        /// <remarks>
        /// When downloading a blob, if the value already exists on the blob, the Storage service 
        /// will include the MD5 hash of the entire blob as a header. This option controls 
        /// whether or not the Storage Client will validate that MD5 hash on download.
        /// See <see cref="ChecksumOptions.StoreContentMD5"/> for more details.
        /// 
        ///## Examples
        ///[!code-csharp[Disable_Content_MD5_Validation_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/MD5FlagsTest.cs#sample_ChecksumOptions_DisableContentMD5Validation "Disable Content MD5 Validation Sample")]        
        /// </remarks>
        public bool? DisableContentMD5Validation { get; set; }

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
        ///  [!code-csharp[Store_Blob_Content_MD5_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/MD5FlagsTest.cs#sample_ChecksumOptions_StoreBlobContentMD5 "Store Blob Content MD5 Sample")] 
        /// </remarks>
        public bool? StoreContentMD5 { get; set; }

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
        ///  [!code-csharp[Use_Transactional_MD5_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/MD5FlagsTest.cs#sample_ChecksumOptions_UseTransactionalMD5 "Use Transactional MD5 Sample")] 
        /// </remarks>
        public bool? UseTransactionalMD5 { get; set; }
        #endregion

        #region CRC64 
        /// <summary>
        /// Gets or sets a value to indicate that CRC64 validation will be disabled when downloading blobs.
        /// </summary>
        /// <value>Use <c>true</c> to disable CRC64 validation; <c>false</c> to enable CRC64 validation. Default is <c>false</c>.</value>
        public bool? DisableContentCRC64Validation { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate that an CRC64 hash will be calculated and stored when uploading a blob.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and store an CRC64 hash when uploading a blob; otherwise, <c>false</c>. Defaults to <c>false</c>.</value>
        /// <remarks>This property is not supported for the <see cref="CloudAppendBlob"/> Append* APIs.
        /// The StoreBlobContentCRC64 request option instructs the Storage Client to calculate the CRC64 hash 
        /// of the blob content during an upload operation. This value is then stored on the 
        /// blob object as the Content-CRC64 header. This option applies only to upload operations. 
        /// This is useful for validating the integrity of the blob upon later download, and 
        /// compatible with the Content-CRC64 header as defined in the HTTP spec. If using 
        /// the Storage Client for later download, if the Content-CRC64 header is present, 
        /// the CRC64 hash of the content will be validated, unless "DisableContentCRC64Validation" is set.
        /// Note that this value is not validated on the Azure Storage service on either upload or download of data; 
        /// it is merely stored and returned.
        /// </remarks>
        internal bool? StoreContentCRC64
        {
            // hardcode to false
            get => false;
            set { } 
        }

        /// <summary>
        /// Gets or sets a value to calculate and send/validate content CRC64 for transactions.
        /// </summary>
        /// <value>Use <c>true</c> to calculate and send/validate content CRC64 for transactions; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        /// <remarks>
        /// The UseTransactionalCRC64 option instructs the Storage Client to calculate and validate 
        /// the CRC64 hash of individual Storage REST operations. For a given REST operation, 
        /// if this value is set, both the Storage Client and the Storage service will calculate
        /// the CRC64 hash of the transferred data, and will fail if the values do not match.
        /// This value is not persisted on the service or the client.
        /// This option applies to both upload and download operations.
        /// Note that HTTPS does a similar check during transit. If you are using HTTPS, 
        /// we recommend this feature be off.
        /// </remarks>
        public bool? UseTransactionalCRC64 { get; set; }
        #endregion

        // TODO: would we need a copy method if this were a struct?
        internal void CopyFrom(ChecksumOptions other)
        {
            this.DisableContentMD5Validation = other.DisableContentMD5Validation;
            this.StoreContentMD5 = other.StoreContentMD5;
            this.UseTransactionalMD5 = other.UseTransactionalMD5;

            this.DisableContentCRC64Validation = other.DisableContentCRC64Validation;
            this.StoreContentCRC64 = other.StoreContentCRC64;
            this.UseTransactionalCRC64 = other.UseTransactionalCRC64;
        }
    }
}