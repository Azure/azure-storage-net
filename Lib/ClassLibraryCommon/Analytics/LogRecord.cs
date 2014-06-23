// -----------------------------------------------------------------------------------------
// <copyright file="LogRecord.cs" company="Microsoft">
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
// ----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a Storage Analytics log entry.
    /// </summary>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    [Serializable]
#endif
    public class LogRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecord"/> class.
        /// </summary>
        internal LogRecord()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecord"/> class based on a <see cref="LogRecordStreamReader"/> object.
        /// <param name="reader">The <see cref="LogRecordStreamReader"/> object to use to populate the log record.</param>
        /// </summary>
        internal LogRecord(LogRecordStreamReader reader)
        {
            CommonUtility.AssertNotNull("reader", reader);

            this.VersionNumber = reader.ReadString();
            CommonUtility.AssertNotNullOrEmpty("VersionNumber", this.VersionNumber);

            if (string.Equals("1.0", this.VersionNumber, StringComparison.Ordinal))
            {
                this.PopulateVersion1Log(reader);
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.LogVersionUnsupported, this.VersionNumber));
            }
        }

        private void PopulateVersion1Log(LogRecordStreamReader reader)
        {
            this.RequestStartTime = reader.ReadDateTimeOffset("o");
            this.OperationType = reader.ReadString();
            this.RequestStatus = reader.ReadString();
            this.HttpStatusCode = reader.ReadString();
            this.EndToEndLatency = reader.ReadTimeSpanInMS();
            this.ServerLatency = reader.ReadTimeSpanInMS();
            this.AuthenticationType = reader.ReadString();
            this.RequesterAccountName = reader.ReadString();
            this.OwnerAccountName = reader.ReadString();
            this.ServiceType = reader.ReadString();
            this.RequestUrl = reader.ReadUri();
            this.RequestedObjectKey = reader.ReadQuotedString();
            this.RequestIdHeader = reader.ReadGuid();
            this.OperationCount = reader.ReadInt();
            this.RequesterIPAddress = reader.ReadString();
            this.RequestVersionHeader = reader.ReadString();
            this.RequestHeaderSize = reader.ReadLong();
            this.RequestPacketSize = reader.ReadLong();
            this.ResponseHeaderSize = reader.ReadLong();
            this.ResponsePacketSize = reader.ReadLong();
            this.RequestContentLength = reader.ReadLong();
            this.RequestMD5 = reader.ReadQuotedString();
            this.ServerMD5 = reader.ReadQuotedString();
            this.ETagIdentifier = reader.ReadQuotedString();
            this.LastModifiedTime = reader.ReadDateTimeOffset("dddd, dd-MMM-yy HH:mm:ss 'GMT'");
            this.ConditionsUsed = reader.ReadQuotedString();
            this.UserAgentHeader = reader.ReadQuotedString();
            this.ReferrerHeader = reader.ReadQuotedString();
            this.ClientRequestId = reader.ReadQuotedString();
            reader.EndCurrentRecord();
        }

        /// <summary>
        /// The version of Storage Analytics Logging used to record the entry.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the version number.</value>
        public string VersionNumber { get; internal set; }

        /// <summary>
        /// The time at which the request was received by the service, in UTC format.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> specifying the request start time.</value>
        public DateTimeOffset? RequestStartTime { get; internal set; }

        /// <summary>
        /// The type of REST operation performed. 
        /// </summary>
        /// <value>A <see cref="System.String"/> specifying the operation type.</value>
        public string OperationType { get; internal set; }

        /// <summary>
        /// The status of the requested operation.
        /// </summary>
        /// <value>A <see cref="System.String"/> indicating the request status.</value>
        public string RequestStatus { get; internal set; }

        /// <summary>
        /// The HTTP status code for the request. If the request is interrupted, this value may be set to Unknown.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the HTTP status code.</value>
        public string HttpStatusCode { get; internal set; }

        /// <summary>
        /// The total time in milliseconds to perform the requested operation, including the time required to read the
        /// incoming request and send the response to the requester.
        /// </summary>
        /// <value>A <see cref="System.TimeSpan"/> indicating the end-to-end latency for the operation.</value>
        public TimeSpan? EndToEndLatency { get; internal set; }

        /// <summary>
        /// The total time in milliseconds to perform the requested operation. This value does not include network 
        /// latency (the time required to read the incoming request and send the response to the requester).
        /// </summary>
        /// <value>A <see cref="System.TimeSpan"/> indicating the server latency for the operation.</value>
        public TimeSpan? ServerLatency { get; internal set; }

        /// <summary>
        /// Indicates whether the request was authenticated via Shared Key or a Shared Access Signature (SAS), or was anonymous.
        /// </summary>
        /// <value>A <see cref="System.String"/> indicating the authentication scheme.</value>
        public string AuthenticationType { get; internal set; }

        /// <summary>
        /// The name of the storage account from which the request originated, if the request is authenticated via Shared Key. 
        /// This field is <c>null</c> for anonymous requests and requests made via a shared access signature (SAS).
        /// </summary>
        /// <value>A <see cref="System.String"/> specifying the name of the storage account.</value>
        public string RequesterAccountName { get; internal set; }

        /// <summary>
        /// The account name of the service owner.
        /// </summary>
        /// <value>A <see cref="System.String"/> specifying the name of the storage account.</value>
        public string OwnerAccountName { get; internal set; }

        /// <summary>
        /// The storage service against which the request was made: blob, table, or queue.
        /// </summary>
        /// <value>A <see cref="System.String"/> indicating against which service the request was made.</value>
        public string ServiceType { get; internal set; }

        /// <summary>
        /// The complete URL of the request.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> object.</value>
        public Uri RequestUrl { get; internal set; }

        /// <summary>
        /// The key of the requested object, as an encoded string. This field will always use the account name, 
        /// even if a custom domain name has been configured.
        /// </summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string RequestedObjectKey { get; internal set; }

        /// <summary>
        /// The request ID assigned by the storage service. This is equivalent to the value of the x-ms-request-id header.
        /// </summary>
        /// <value>A <see cref="System.Guid"/> containing the request ID.</value>
        public Guid? RequestIdHeader { get; internal set; }

        /// <summary>
        /// The number of operations logged for a request, starting at index zero. Some requests require more than
        /// one operation, such as Copy Blob, though most perform just one operation.
        /// </summary>
        /// <value>An integer containing the operation count.</value>
        public int? OperationCount { get; internal set; }

        /// <summary>
        /// The IP address of the requester, including the port number.
        /// </summary>
        public string RequesterIPAddress { get; internal set; }

        /// <summary>
        /// The storage service version specified when the request was made. This is equivalent to the value of the x-ms-version header.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the request version header.</value>
        public string RequestVersionHeader { get; internal set; }

        /// <summary>
        /// The size of the request header, in bytes. If a request is unsuccessful, this value may be <c>null</c>.
        /// </summary>
        /// <value>A long containing the request header size.</value>
        public long? RequestHeaderSize { get; internal set; }

        /// <summary>
        /// The size of the request packets read by the storage service, in bytes. If a request is unsuccessful, this value may be <c>null</c>.
        /// </summary>
        /// <value>A long containing the request packet size.</value>
        public long? RequestPacketSize { get; internal set; }

        /// <summary>
        /// The size of the response header, in bytes. If a request is unsuccessful, this value may be <c>null</c>.
        /// </summary>
        /// <value>A long containing the size of the response header in bytes.</value>
        public long? ResponseHeaderSize { get; internal set; }

        /// <summary>
        /// The size of the response packets written by the storage service, in bytes. If a request is unsuccessful, this value may be <c>null</c>.
        /// </summary>
        /// <value>A long containing the packet size of the response header, in bytes.</value>
        public long? ResponsePacketSize { get; internal set; }

        /// <summary>
        /// The value of the Content-Length header for the request sent to the storage service. If the request was successful, 
        /// this value is equal to request-packet-size. If a request is unsuccessful, this value may not be equal to 
        /// request-packet-size, or it may be <c>null</c>.
        /// </summary>
        /// <value>A long containing the request content length, in bytes.</value>
        public long? RequestContentLength { get; internal set; }

        /// <summary>
        /// The value of either the Content-MD5 header or the x-ms-content-md5 header in the request as an encoded string.
        /// The MD5 hash value specified in this field represents the content in the request. This field can be <c>null</c>.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the request MD5 value.</value>
        public string RequestMD5 { get; internal set; }

        /// <summary>
        /// The value of the MD5 hash calculated by the storage service, as an encoded string.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the server MD5 hash.</value>
        public string ServerMD5 { get; internal set; }

        /// <summary>
        /// The ETag identifier for the returned object as an encoded string.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the ETag for the resource.</value>
        public string ETagIdentifier { get; internal set; }

        /// <summary>
        /// The Last Modified Time (LMT) for the returned object as an encoded string. This field is <c>null</c> for operations that return multiple objects.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> specifying the last modified time.</value>
        public DateTimeOffset? LastModifiedTime { get; internal set; }

        /// <summary>
        /// A semicolon-separated list, in the form of ConditionName=value, as an encoded string.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the conditions used for the request.</value>
        public string ConditionsUsed { get; internal set; }

        /// <summary>
        /// The User-Agent header value as an encoded string.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the value of the User-Agent header.</value>
        public string UserAgentHeader { get; internal set; }

        /// <summary>
        /// The Referrer header value as an encoded string.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the value of the Referrer header.</value>
        public string ReferrerHeader { get; internal set; }

        /// <summary>
        /// The value of the x-ms-client-request-id header, included in the request as an encoded string.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the client request ID.</value>
        public string ClientRequestId { get; internal set; }
    }
}
