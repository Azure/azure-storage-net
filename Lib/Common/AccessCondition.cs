// -----------------------------------------------------------------------------------------
// <copyright file="AccessCondition.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a set of access conditions to be used for operations against the storage services. 
    /// </summary>
    public sealed class AccessCondition
    {
        /// <summary>
        /// Time for IfModifiedSince.
        /// </summary>
        private DateTimeOffset? ifModifiedSinceDateTime;

        /// <summary>
        /// Time for IfUnmodifiedSince.
        /// </summary>
        private DateTimeOffset? ifNotModifiedSinceDateTime;

        /// <summary>
        /// Gets or sets an ETag value for a condition specifying that the given ETag must match the ETag of the specified resource.
        /// </summary>
        /// <value>A string containing an ETag value, or <c>"*"</c> to match any ETag. If <c>null</c>, no condition exists.</value>
        public string IfMatchETag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an ETag value for a condition specifying that the given ETag must not match the ETag of the specified resource.
        /// </summary>
        /// <value>A string containing an ETag value, or <c>"*"</c> to match any ETag. If <c>null</c>, no condition exists.</value>
        public string IfNoneMatchETag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a <see cref="DateTimeOffset"/> value for a condition specifying a time since which a resource has been modified.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> value specified in UTC, or <c>null</c> if no condition exists.</value>
        public DateTimeOffset? IfModifiedSinceTime
        {
            get
            {
                return this.ifModifiedSinceDateTime;
            }

            set
            {
                this.ifModifiedSinceDateTime = value.HasValue ? value.Value.ToUniversalTime() : value;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="DateTimeOffset"/> value for a condition specifying a time since which a resource has not been modified.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> value specified in UTC, or <c>null</c> if no condition exists.</value>
        public DateTimeOffset? IfNotModifiedSinceTime
        {
            get
            {
                return this.ifNotModifiedSinceDateTime;
            }

            set
            {
                this.ifNotModifiedSinceDateTime = value.HasValue ? value.Value.ToUniversalTime() : value;
            }
        }

        /// <summary>
        /// Gets or sets a value for a condition that specifies the maximum size allowed for an append blob when a new block is committed. The append
        /// will succeed only if the size of the blob after the append operation is less than or equal to the specified size.
        /// </summary>
        /// <value>The maximum size in bytes, or <c>null</c> if no value is set.</value>
        /// <remarks>This condition only applies to append blobs.</remarks>
        public long? IfMaxSizeLessThanOrEqual
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value for a condition specifying the byte offset to check for when committing a block to an append blob.
        /// The append will succeed only if the end position is equal to this number. 
        /// </summary>
        /// <value>An append position number, or <c>null</c> if no value is set.</value>
        /// <remarks>This condition only applies to append blobs.</remarks>
        public long? IfAppendPositionEqual
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value for a condition specifying that the current sequence number must be less than or equal to the specified value.
        /// </summary>
        /// <value>A sequence number, or <c>null</c> if no condition exists.</value>
        /// <remarks>This condition only applies to page blobs.</remarks>
        public long? IfSequenceNumberLessThanOrEqual
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value for a condition specifying that the current sequence number must be less than the specified value.
        /// </summary>
        /// <value>A sequence number, or <c>null</c> if no condition exists.</value>
        /// <remarks>This condition only applies to page blobs.</remarks>
        public long? IfSequenceNumberLessThan
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value for a condition specifying that the current sequence number must be equal to the specified value.
        /// </summary>
        /// <value>A sequence number, or <c>null</c> if no condition exists.</value>
        /// <remarks>This condition only applies to page blobs.</remarks>
        public long? IfSequenceNumberEqual
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a lease ID that must match the lease on a resource.
        /// </summary>
        /// <value>A string containing a lease ID, or <c>null</c> if no condition exists.</value>
        public string LeaseId
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether the access condition is one of the four conditional headers.
        /// </summary>
        /// <value><c>true</c> if the access condition is a conditional header; otherwise, <c>false</c>.</value>
        internal bool IsConditional
        {
            get
            {
                return !string.IsNullOrEmpty(this.IfMatchETag) ||
                    !string.IsNullOrEmpty(this.IfNoneMatchETag) ||
                    this.IfModifiedSinceTime.HasValue ||
                    this.IfNotModifiedSinceTime.HasValue;
            }
        }

        /// <summary>
        /// Constructs an empty access condition.
        /// </summary>
        /// <returns>An empty <see cref="AccessCondition"/> object.</returns>
        public static AccessCondition GenerateEmptyCondition()
        {
            return new AccessCondition();
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource does not exist.
        /// </summary>
        /// <returns>An <see cref="AccessCondition"/> object that represents a condition where a resource does not exist.</returns>
        /// <remarks>Setting this access condition modifies the request to include the HTTP <i>If-None-Match</i> conditional header.</remarks>
        public static AccessCondition GenerateIfNotExistsCondition()
        {
            return new AccessCondition { IfNoneMatchETag = "*" };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource exists.
        /// </summary>
        /// <returns>An <see cref="AccessCondition"/> object that represents a condition where a resource exists.</returns>
        /// <remarks>Setting this access condition modifies the request to include the HTTP <i>If-Match</i> conditional header.</remarks>
        public static AccessCondition GenerateIfExistsCondition()
        {
            return new AccessCondition { IfMatchETag = "*" };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource's ETag value
        /// matches the specified ETag value.
        /// </summary>
        /// <param name="etag">The ETag value to check against the resource's ETag.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Match condition.</returns>
        public static AccessCondition GenerateIfMatchCondition(string etag)
        {
            return new AccessCondition { IfMatchETag = etag };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource has been
        /// modified since the specified time.
        /// </summary>
        /// <param name="modifiedTime">A <see cref="DateTimeOffset"/> value specifying the time since which the resource must have been modified.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Modified-Since condition.</returns>
        public static AccessCondition GenerateIfModifiedSinceCondition(DateTimeOffset modifiedTime)
        {
            return new AccessCondition { IfModifiedSinceTime = modifiedTime };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource's ETag value
        /// does not match the specified ETag value.
        /// </summary>
        /// <param name="etag">The ETag value to check against the resource's ETag, or <c>"*"</c> to require that the resource does not exist.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-None-Match condition.</returns>
        /// <remarks>
        /// If <c>"*"</c> is specified for the <paramref name="etag"/> parameter, then this condition requires that the resource does not exist.
        /// </remarks>        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "etag", Justification = "Reviewed: etag can be used for identifier names.")]
        public static AccessCondition GenerateIfNoneMatchCondition(string etag)
        {
            return new AccessCondition { IfNoneMatchETag = etag };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource has not been
        /// modified since the specified time.
        /// </summary>
        /// <param name="modifiedTime">A <see cref="DateTimeOffset"/> value specifying the time since which the resource must not have been modified.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Unmodified-Since condition.</returns>
        public static AccessCondition GenerateIfNotModifiedSinceCondition(DateTimeOffset modifiedTime)
        {
            return new AccessCondition { IfNotModifiedSinceTime = modifiedTime };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the size of the append blob after committing the block is less
        /// than or equal to the specified value.
        /// </summary>
        /// <param name="maxSize">An integer specifying the maximum allowed size of the blob, in bytes, when committing a new block.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the maximum allowed size.</returns>
        public static AccessCondition GenerateIfMaxSizeLessThanOrEqualCondition(long maxSize)
        {
            return new AccessCondition { IfMaxSizeLessThanOrEqual = maxSize };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the end position of the append blob is equal to the specified value.
        /// </summary>
        /// <param name="appendPosition">An integer specifying the offset to compare to the current end position of the blob.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the offset to compare.</returns>
        public static AccessCondition GenerateIfAppendPositionEqualCondition(long appendPosition)
        {
            return new AccessCondition { IfAppendPositionEqual = appendPosition };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if resource's current sequence
        /// number is less than or equal to the specified value.
        /// </summary>
        /// <param name="sequenceNumber">The value to compare to the current sequence number.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Sequence-Number-LE condition.</returns>
        public static AccessCondition GenerateIfSequenceNumberLessThanOrEqualCondition(long sequenceNumber)
        {
            return new AccessCondition { IfSequenceNumberLessThanOrEqual = sequenceNumber };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if resource's current sequence
        /// number is less than the specified value.
        /// </summary>
        /// <param name="sequenceNumber">The value to compare to the current sequence number.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Sequence-Number-LT condition.</returns>
        public static AccessCondition GenerateIfSequenceNumberLessThanCondition(long sequenceNumber)
        {
            return new AccessCondition { IfSequenceNumberLessThan = sequenceNumber };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if resource's current sequence
        /// number is equal to the specified value.
        /// </summary>
        /// <param name="sequenceNumber">The value to compare to the current sequence number.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Sequence-Number-EQ condition.</returns>
        public static AccessCondition GenerateIfSequenceNumberEqualCondition(long sequenceNumber)
        {
            return new AccessCondition { IfSequenceNumberEqual = sequenceNumber };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the lease ID on the
        /// resource matches the specified lease ID.
        /// </summary>
        /// <param name="leaseId">The lease ID to compare to the lease ID of the resource.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the lease condition.</returns>
        public static AccessCondition GenerateLeaseCondition(string leaseId)
        {
            return new AccessCondition { LeaseId = leaseId };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource's ETag value
        /// matches the specified ETag value and the lease ID on the resource matches the lease ID specified in
        /// the given access condition.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="etag">The ETag value that must be matched.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Match and the lease conditions.</returns>
        internal static AccessCondition CloneConditionWithETag(AccessCondition accessCondition, string etag)
        {
            return new AccessCondition
            {
                IfMatchETag = etag,
                LeaseId = accessCondition != null ? accessCondition.LeaseId : null,
            };
        }
    }
}
