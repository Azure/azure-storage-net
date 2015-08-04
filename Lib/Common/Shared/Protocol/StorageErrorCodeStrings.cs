// -----------------------------------------------------------------------------------------
// <copyright file="StorageErrorCodeStrings.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides error code strings that are common to all storage services.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        static class StorageErrorCodeStrings
    {
        /// <summary>
        /// The specified HTTP verb is not supported.
        /// </summary>
        public static readonly string UnsupportedHttpVerb = "UnsupportedHttpVerb";

        /// <summary>
        /// The Content-Length header is required for this request.
        /// </summary>
        public static readonly string MissingContentLengthHeader = "MissingContentLengthHeader";

        /// <summary>
        /// A required header was missing.
        /// </summary>
        public static readonly string MissingRequiredHeader = "MissingRequiredHeader";

        /// <summary>
        /// A required XML node was missing.
        /// </summary>
        public static readonly string MissingRequiredXmlNode = "MissingRequiredXmlNode";

        /// <summary>
        /// One or more header values are not supported.
        /// </summary>
        public static readonly string UnsupportedHeader = "UnsupportedHeader";

        /// <summary>
        /// One or more XML nodes are not supported.
        /// </summary>
        public static readonly string UnsupportedXmlNode = "UnsupportedXmlNode";

        /// <summary>
        /// One or more header values are invalid.
        /// </summary>
        public static readonly string InvalidHeaderValue = "InvalidHeaderValue";

        /// <summary>
        /// One or more XML node values are invalid.
        /// </summary>
        public static readonly string InvalidXmlNodeValue = "InvalidXmlNodeValue";

        /// <summary>
        /// A required query parameter is missing.
        /// </summary>
        public static readonly string MissingRequiredQueryParameter = "MissingRequiredQueryParameter";

        /// <summary>
        /// One or more query parameters is not supported.
        /// </summary>
        public static readonly string UnsupportedQueryParameter = "UnsupportedQueryParameter";

        /// <summary>
        /// One or more query parameters are invalid.
        /// </summary>
        public static readonly string InvalidQueryParameterValue = "InvalidQueryParameterValue";

        /// <summary>
        /// One or more query parameters are out of range.
        /// </summary>
        public static readonly string OutOfRangeQueryParameterValue = "OutOfRangeQueryParameterValue";

        /// <summary>
        /// The URI is invalid.
        /// </summary>
        public static readonly string InvalidUri = "InvalidUri";

        /// <summary>
        /// The HTTP verb is invalid.
        /// </summary>
        public static readonly string InvalidHttpVerb = "InvalidHttpVerb";

        /// <summary>
        /// The metadata key is empty.
        /// </summary>
        public static readonly string EmptyMetadataKey = "EmptyMetadataKey";

        /// <summary>
        /// The request body is too large.
        /// </summary>
        public static readonly string RequestBodyTooLarge = "RequestBodyTooLarge";

        /// <summary>
        /// The specified XML document is invalid.
        /// </summary>
        public static readonly string InvalidXmlDocument = "InvalidXmlDocument";

        /// <summary>
        /// An internal error occurred.
        /// </summary>
        public static readonly string InternalError = "InternalError";

        /// <summary>
        /// Authentication failed.
        /// </summary>
        public static readonly string AuthenticationFailed = "AuthenticationFailed";

        /// <summary>
        /// The specified MD5 hash does not match the server value.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1709:IdentifiersShouldBeCasedCorrectly",
            MessageId = "Md",
            Justification = "The casing matches the storage constant the identifier represents.")]
        public static readonly string Md5Mismatch = "Md5Mismatch";

        /// <summary>
        /// The specified MD5 hash is invalid.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1709:IdentifiersShouldBeCasedCorrectly",
            MessageId = "Md",
            Justification = "The casing matches the storage constant the identifier represents.")]
        public static readonly string InvalidMd5 = "InvalidMd5";

        /// <summary>
        /// The input is out of range.
        /// </summary>
        public static readonly string OutOfRangeInput = "OutOfRangeInput";

        /// <summary>
        /// The input is invalid.
        /// </summary>
        public static readonly string InvalidInput = "InvalidInput";

        /// <summary>
        /// The operation timed out.
        /// </summary>
        public static readonly string OperationTimedOut = "OperationTimedOut";

        /// <summary>
        /// The specified resource was not found.
        /// </summary>
        public static readonly string ResourceNotFound = "ResourceNotFound";

        /// <summary>
        /// The specified metadata is invalid.
        /// </summary>
        public static readonly string InvalidMetadata = "InvalidMetadata";

        /// <summary>
        /// The specified metadata is too large.
        /// </summary>
        public static readonly string MetadataTooLarge = "MetadataTooLarge";

        /// <summary>
        /// The specified condition was not met.
        /// </summary>
        public static readonly string ConditionNotMet = "ConditionNotMet";

        /// <summary>
        /// The specified range is invalid.
        /// </summary>
        public static readonly string InvalidRange = "InvalidRange";

        /// <summary>
        /// The specified container was not found.
        /// </summary>
        public static readonly string ContainerNotFound = "ContainerNotFound";

        /// <summary>
        /// The specified container already exists.
        /// </summary>
        public static readonly string ContainerAlreadyExists = "ContainerAlreadyExists";

        /// <summary>
        /// The specified container is disabled.
        /// </summary>
        public static readonly string ContainerDisabled = "ContainerDisabled";

        /// <summary>
        /// The specified container is being deleted.
        /// </summary>
        public static readonly string ContainerBeingDeleted = "ContainerBeingDeleted";

        /// <summary>
        /// The server is busy.
        /// </summary>
        public static readonly string ServerBusy = "ServerBusy";

        /// <summary>
        /// The url in the request could not be parsed.
        /// </summary>
        public static readonly string RequestUrlFailedToParse = "RequestUrlFailedToParse";

        /// <summary>
        /// The authentication information was not provided in the correct format. Verify the value of Authorization header.
        /// </summary>
        public static readonly string InvalidAuthenticationInfo = "InvalidAuthenticationInfo";

        /// <summary>
        /// The specified resource name contains invalid characters.
        /// </summary>
        public static readonly string InvalidResourceName = "InvalidResourceName";

        /// <summary>
        /// Condition headers are not supported.
        /// </summary>
        public static readonly string ConditionHeadersNotSupported = "ConditionHeadersNotSupported";

        /// <summary>
        /// Multiple condition headers are not supported.
        /// </summary>
        public static readonly string MultipleConditionHeadersNotSupported = "MultipleConditionHeadersNotSupported";

        /// <summary>
        /// Read-access geo-redundant replication is not enabled for the account, write operations to the secondary location are not allowed, 
        /// or the account being accessed does not have sufficient permissions to execute this operation.
        /// </summary>
        public static readonly string InsufficientAccountPermissions = "InsufficientAccountPermissions";

        /// <summary>
        /// The specified account is disabled.
        /// </summary>
        public static readonly string AccountIsDisabled = "AccountIsDisabled";

        /// <summary>
        /// The specified account already exists.
        /// </summary>
        public static readonly string AccountAlreadyExists = "AccountAlreadyExists";

        /// <summary>
        /// The specified account is in the process of being created.
        /// </summary>
        public static readonly string AccountBeingCreated = "AccountBeingCreated";

        /// <summary>
        /// The specified resource already exists.
        /// </summary>
        public static readonly string ResourceAlreadyExists = "ResourceAlreadyExists";

        /// <summary>
        /// The specified resource type does not match the type of the existing resource.
        /// </summary>
        public static readonly string ResourceTypeMismatch = "ResourceTypeMismatch";
    }
}