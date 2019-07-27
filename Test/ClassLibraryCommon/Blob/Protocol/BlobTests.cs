// -----------------------------------------------------------------------------------------
// <copyright file="BlobTests.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Auth.Protocol;
using Microsoft.Azure.Storage.Core.Auth;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    internal class BlobTests
    {
        public static HttpRequestMessage PutBlobRequest(BlobContext context, string containerName, string blobName,
            BlobProperties properties, BlobType blobType, byte[] content, long pageBlobSize, AccessCondition accessCondition)
        {
            bool valid = BlobTests.ContainerNameValidator(containerName) &&
                BlobTests.BlobNameValidator(blobName) &&
                BlobTests.PutPropertiesValidator(properties) &&
                BlobTestUtils.ContentValidator(content);

            bool fatal = !BlobTests.PutPropertiesValidator(properties);

            Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
            HttpRequestMessage request = null;
            OperationContext opContext = new OperationContext();
            try
            {
                request = BlobHttpRequestMessageFactory.Put(uri, context.Timeout, properties, blobType, pageBlobSize, null, accessCondition, null, opContext, 
                    SharedKeyCanonicalizer.Instance, context.Credentials, null);
                if (fatal)
                {
                    Assert.Fail();
                }
            }
            catch (InvalidOperationException)
            {
                if (valid)
                {
                    Assert.Fail();
                }
            }
            if (valid)
            {
                Assert.IsNotNull(request);
                Assert.IsNotNull(request.Method);
                Assert.AreEqual(HttpMethod.Put, request.Method);
                BlobTestUtils.VersionHeader(request, false);
                BlobTestUtils.ContentTypeHeader(request, null);
                BlobTestUtils.ContentDispositionHeader(request, properties.ContentDisposition);
                BlobTestUtils.ContentEncodingHeader(request, properties.ContentEncoding);
                BlobTestUtils.ContentLanguageHeader(request, null);
                BlobTestUtils.ContentMd5Header(request, null);
                BlobTestUtils.ContentCrc64Header(request, null);
                BlobTestUtils.CacheControlHeader(request, null);
                BlobTestUtils.BlobTypeHeader(request, properties.BlobType);
                BlobTestUtils.BlobSizeHeader(request, (properties.BlobType == BlobType.PageBlob) ? properties.Length : (long?)null);
            }
            return request;
        }

        public static void PutBlobResponse(HttpResponseMessage response, BlobContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.ETagHeader(response);
                BlobTestUtils.LastModifiedHeader(response);
                BlobTestUtils.ContentChecksumHeader(response);
                BlobTestUtils.RequestIdHeader(response);
                BlobTestUtils.ContentLengthHeader(response, 0);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
            response.Dispose();
        }

        public static HttpRequestMessage GetBlobRequest(BlobContext context, string containerName, string blobName, AccessCondition accessCondition)
        {
            bool valid = BlobTests.ContainerNameValidator(containerName) &&
                BlobTests.BlobNameValidator(blobName) &&
                BlobTests.LeaseIdValidator(accessCondition);

            Uri uri = BlobTests.ConstructGetUri(context.Address, containerName, blobName);
            HttpRequestMessage request = null;
            OperationContext opContext = new OperationContext();
            try
            {
                request = BlobHttpRequestMessageFactory.Get(uri, context.Timeout, null /* snapshot */, accessCondition, null, opContext, SharedKeyCanonicalizer.Instance, context.Credentials);
            }
            catch (InvalidOperationException)
            {
                if (valid)
                {
                    Assert.Fail();
                }
            }
            if (valid)
            {
                Assert.IsNotNull(request);
                Assert.IsNotNull(request.Method);
                Assert.AreEqual(HttpMethod.Get, request.Method);
                BlobTestUtils.RangeHeader(request, null);
                BlobTestUtils.LeaseIdHeader(request, accessCondition == null ? null : accessCondition.LeaseId);
            }
            return request;
        }

        public static void GetBlobResponse(HttpResponseMessage response, BlobContext context, BlobProperties properties, byte[] content, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                BlobTestUtils.LastModifiedHeader(response);
                BlobTestUtils.ContentLengthHeader(response, content.Length);
                BlobTestUtils.ETagHeader(response);
                BlobTestUtils.RequestIdHeader(response);
                BlobTestUtils.Contents(response, content);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
            response.Dispose();
        }

        /// <summary>
        /// Generates a get blob request over the specified range, and checks the request for consistency.
        /// </summary>
        /// <param name="context">The testing context.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <param name="offset">The offset to the range.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <param name="leaseId">The lease ID, or null if the blob is not leased.</param>
        /// <returns>A web request for getting a blob range.</returns>
        public static HttpRequestMessage GetBlobRangeRequest(BlobContext context, string containerName, string blobName, long offset, long? count, AccessCondition accessCondition)
        {
            bool valid = BlobTests.ContainerNameValidator(containerName) &&
                BlobTests.BlobNameValidator(blobName) &&
                BlobTests.LeaseIdValidator(accessCondition);

            Uri uri = BlobTests.ConstructGetUri(context.Address, containerName, blobName);
            HttpRequestMessage request = null;
            OperationContext opContext = new OperationContext();

            try
            {
                request = BlobHttpRequestMessageFactory.Get(uri, context.Timeout, null /* snapshot */, offset, count, ChecksumRequested.None, accessCondition, null, opContext, SharedKeyCanonicalizer.Instance, context.Credentials, null);
            }
            catch (InvalidOperationException)
            {
                if (valid)
                {
                    Assert.Fail();
                }
            }

            if (valid)
            {
                Assert.IsNotNull(request);
                Assert.IsNotNull(request.Method);
                Assert.AreEqual(HttpMethod.Get, request.Method);
                BlobTestUtils.RangeHeader(request, offset, count.HasValue ? (long?)(count.Value + offset - 1) : null);
                BlobTestUtils.LeaseIdHeader(request, accessCondition == null ? null : accessCondition.LeaseId);
            }

            return request;
        }

        /// <summary>
        /// Checks a get blob range response for consistency with the given parameters, and closes the response.
        /// </summary>
        /// <param name="response">The HTTP web response to check.</param>
        /// <param name="context">The testing context.</param>
        /// <param name="content">The expected content returned in the response.</param>
        /// <param name="expectedStartRange">The expected start range returned in the response header.</param>
        /// <param name="expectedEndRange">The expected end range returned in the response header.</param>
        /// <param name="expectedTotalBytes">The expected total number of bytes in the blob.</param>
        /// <param name="expectedError">The expected error code, or null if the operation is expected to succeed.</param>
        public static void CheckBlobRangeResponse(
            HttpResponseMessage response,
            BlobContext context,
            byte[] content,
            long expectedStartRange,
            long expectedEndRange,
            long expectedTotalBytes,
            HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.PartialContent, response.StatusCode);
                Assert.IsNotNull(content);
                BlobTestUtils.LastModifiedHeader(response);
                BlobTestUtils.ContentLengthHeader(response, content.Length);
                BlobTestUtils.ETagHeader(response);
                BlobTestUtils.RequestIdHeader(response);
                BlobTestUtils.Contents(response, content);
                BlobTestUtils.ContentRangeHeader(response, expectedStartRange, expectedEndRange, expectedTotalBytes);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }

            response.Dispose();
        }

        public static HttpRequestMessage PutBlockRequest(BlobContext context, string containerName, string blobName, string blockId, AccessCondition accessCondition)
        {
            Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
            HttpRequestMessage request = null;
            OperationContext opContext = new OperationContext();
            request = BlobHttpRequestMessageFactory.PutBlock(uri, context.Timeout, blockId, accessCondition, null, opContext, SharedKeyCanonicalizer.Instance, context.Credentials, null);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.ContentLanguageHeader(request, null);
            BlobTestUtils.ContentMd5Header(request, null);
            BlobTestUtils.ContentCrc64Header(request, null);
            return request;
        }

        public static void PutBlockResponse(HttpResponseMessage response, BlobContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.ContentChecksumHeader(response);
                BlobTestUtils.RequestIdHeader(response);
                BlobTestUtils.ContentLengthHeader(response, 0);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
            response.Dispose();
        }

        public static HttpRequestMessage PutBlockListRequest(BlobContext context, string containerName, string blobName, BlobProperties blobProperties, AccessCondition accessCondition)
        {
            Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
            HttpRequestMessage request = null;
            OperationContext opContext = new OperationContext();
            request = BlobHttpRequestMessageFactory.PutBlockList(uri, context.Timeout, blobProperties, accessCondition, null, opContext, SharedKeyCanonicalizer.Instance, context.Credentials, null);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.ContentLanguageHeader(request, null);
            BlobTestUtils.ContentMd5Header(request, null);
            BlobTestUtils.ContentCrc64Header(request, null);
            return request;
        }

        public static void PutBlockListResponse(HttpResponseMessage response, BlobContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.ContentChecksumHeader(response);
                BlobTestUtils.RequestIdHeader(response);
                BlobTestUtils.ContentLengthHeader(response, 0);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
            response.Dispose();
        }

        /// <summary>
        /// Generates and validates a request to acquire a lease.
        /// </summary>
        /// <param name="context">The blob context.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobName">The blob name.</param>
        /// <param name="leaseDuration">The lease duration.</param>
        /// <param name="proposedLeaseId">The proposed lease ID.</param>
        /// <returns>A web request for the operation.</returns>
        public static HttpRequestMessage AcquireLeaseRequest(BlobContext context, string containerName, string blobName, int leaseDuration, string proposedLeaseId, AccessCondition accessCondition)
        {
            HttpRequestMessage request;
            OperationContext opContext = new OperationContext();
            if (blobName != null)
            {
                // blob lease
                Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
                request = BlobHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Acquire,
                    proposedLeaseId,
                    leaseDuration,
                    null /* break period */,
                    null,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            }
            else
            {
                // container lease
                Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
                request = ContainerHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Acquire,
                    proposedLeaseId,
                    leaseDuration,
                    null /* break period */,
                    null,
                    null,
                    opContext, 
                    SharedKeyCanonicalizer.Instance, 
                    context.Credentials);
            }
            Assert.IsNotNull(request);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.LeaseIdHeader(request, null);
            BlobTestUtils.LeaseActionHeader(request, "acquire");
            BlobTestUtils.LeaseDurationHeader(request, leaseDuration.ToString());
            BlobTestUtils.ProposedLeaseIdHeader(request, proposedLeaseId);
            BlobTestUtils.BreakPeriodHeader(request, null);
            return request;
        }

        /// <summary>
        /// Validates a response to an acquire lease operation.
        /// </summary>
        /// <param name="response">The response to validate.</param>
        /// <param name="expectedLeaseId">The expected lease ID.</param>
        /// <param name="expectedError">The error status code to expect.</param>
        public static void AcquireLeaseResponse(HttpResponseMessage response, string expectedLeaseId, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, response.ReasonPhrase);

                if (expectedLeaseId != null)
                {
                    BlobTestUtils.LeaseIdHeader(response, expectedLeaseId);
                }
                else
                {
                    BlobTestUtils.LeaseIdHeader(response);
                }

                BlobTestUtils.LeaseTimeHeader(response, null, null);
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        /// <summary>
        /// Generates and validates a request to renew a lease.
        /// </summary>
        /// <param name="context">The blob context.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobName">The blob name.</param>
        /// <param name="leaseID">The lease ID.</param>
        /// <returns>A web request for the operation.</returns>
        public static HttpRequestMessage RenewLeaseRequest(BlobContext context, string containerName, string blobName, AccessCondition accessCondition)
        {
            HttpRequestMessage request;
            OperationContext opContext = new OperationContext();
            if (blobName != null)
            {
                // blob lease
                Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
                request = BlobHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Renew,
                    null /* proposed lease ID */,
                    null /* lease duration */,
                    null /* break period */,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance, 
                    context.Credentials);
            }
            else
            {
                // container lease
                Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
                request = ContainerHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Renew,
                    null /* proposed lease ID */,
                    null /* lease duration */,
                    null /* break period */,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance, 
                    context.Credentials);
            }
            Assert.IsNotNull(request);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.LeaseIdHeader(request, accessCondition == null ? null : accessCondition.LeaseId);
            BlobTestUtils.LeaseActionHeader(request, "renew");
            BlobTestUtils.LeaseDurationHeader(request, null);
            BlobTestUtils.ProposedLeaseIdHeader(request, null);
            BlobTestUtils.BreakPeriodHeader(request, null);
            return request;
        }

        /// <summary>
        /// Validates a response to a renew lease operation.
        /// </summary>
        /// <param name="response">The response to validate.</param>
        /// <param name="expectedLeaseId">The expected lease ID.</param>
        /// <param name="expectedError">The error status code to expect.</param>
        public static void RenewLeaseResponse(HttpResponseMessage response, string expectedLeaseId, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.ReasonPhrase);

                if (expectedLeaseId != null)
                {
                    BlobTestUtils.LeaseIdHeader(response, expectedLeaseId);
                }
                else
                {
                    BlobTestUtils.LeaseIdHeader(response);
                }

                BlobTestUtils.LeaseTimeHeader(response, null, null);
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        /// <summary>
        /// Generates and validates a request to change a lease.
        /// </summary>
        /// <param name="context">The blob context.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobName">The blob name.</param>
        /// <param name="leaseID">The lease ID.</param>
        /// <param name="proposedLeaseId">The proposed lease ID.</param>
        /// <returns>A web request for the operation.</returns>
        public static HttpRequestMessage ChangeLeaseRequest(BlobContext context, string containerName, string blobName, string proposedLeaseId, AccessCondition accessCondition)
        {
            HttpRequestMessage request;
            OperationContext opContext = new OperationContext();
            if (blobName != null)
            {
                // blob lease
                Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
                request = BlobHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Change,
                    proposedLeaseId,
                    null /* lease duration */,
                    null /* break period */,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance, 
                    context.Credentials);
            }
            else
            {
                // container lease
                Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
                request = ContainerHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Change,
                    proposedLeaseId,
                    null /* lease duration */,
                    null /* break period */,
                    null,
                    null,
                    opContext, 
                    SharedKeyCanonicalizer.Instance, 
                    context.Credentials);
            }
            Assert.IsNotNull(request);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.LeaseIdHeader(request, accessCondition == null ? null : accessCondition.LeaseId);
            BlobTestUtils.LeaseActionHeader(request, "change");
            BlobTestUtils.LeaseDurationHeader(request, null);
            BlobTestUtils.ProposedLeaseIdHeader(request, proposedLeaseId);
            BlobTestUtils.BreakPeriodHeader(request, null);
            return request;
        }

        /// <summary>
        /// Validates a response to a change lease operation.
        /// </summary>
        /// <param name="response">The response to validate.</param>
        /// <param name="expectedLeaseId">The expected lease ID.</param>
        /// <param name="expectedError">The error status code to expect.</param>
        public static void ChangeLeaseResponse(HttpResponseMessage response, string expectedLeaseId, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.ReasonPhrase);

                if (expectedLeaseId != null)
                {
                    BlobTestUtils.LeaseIdHeader(response, expectedLeaseId);
                }
                else
                {
                    BlobTestUtils.LeaseIdHeader(response);
                }

                BlobTestUtils.LeaseTimeHeader(response, null, null);
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        /// <summary>
        /// Generates and validates a request to release a lease.
        /// </summary>
        /// <param name="context">The blob context.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobName">The blob name.</param>
        /// <param name="leaseID">The lease ID.</param>
        /// <returns>A web request for the operation.</returns>
        public static HttpRequestMessage ReleaseLeaseRequest(BlobContext context, string containerName, string blobName, AccessCondition accessCondition)
        {
            HttpRequestMessage request;
            OperationContext opContext = new OperationContext();
            if (blobName != null)
            {
                // blob lease
                Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
                request = BlobHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Release,
                    null /* proposed lease ID */,
                    null /* lease duration */,
                    null /* break period */,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            }
            else
            {
                // container lease
                Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
                request = ContainerHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Release,
                    null /* proposed lease ID */,
                    null /* lease duration */,
                    null /* break period */,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            }
            Assert.IsNotNull(request);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.LeaseIdHeader(request, accessCondition == null ? null : accessCondition.LeaseId);
            BlobTestUtils.LeaseActionHeader(request, "release");
            BlobTestUtils.LeaseDurationHeader(request, null);
            BlobTestUtils.ProposedLeaseIdHeader(request, null);
            BlobTestUtils.BreakPeriodHeader(request, null);
            return request;
        }

        /// <summary>
        /// Validates a response to a release lease operation.
        /// </summary>
        /// <param name="response">The response to validate.</param>
        /// <param name="expectedError">The error status code to expect.</param>
        public static void ReleaseLeaseResponse(HttpResponseMessage response, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.LeaseIdHeader(response, null);
                BlobTestUtils.LeaseTimeHeader(response, null, null);
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        /// <summary>
        /// Generates and validates a request to break a lease.
        /// </summary>
        /// <param name="context">The blob context.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobName">The blob name.</param>
        /// <param name="breakPeriod">The break period.</param>
        /// <returns>A web request for the operation.</returns>
        public static HttpRequestMessage BreakLeaseRequest(BlobContext context, string containerName, string blobName, int? breakPeriod, AccessCondition accessCondition)
        {
            HttpRequestMessage request;
            OperationContext opContext = new OperationContext();
            if (blobName != null)
            {
                // blob lease
                Uri uri = BlobTests.ConstructPutUri(context.Address, containerName, blobName);
                request = BlobHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Break,
                    null /* proposed lease ID */,
                    null /* lease duration */,
                    breakPeriod,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            }
            else
            {
                // container lease
                Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
                request = ContainerHttpRequestMessageFactory.Lease(
                    uri,
                    context.Timeout,
                    LeaseAction.Break,
                    null /* proposed lease ID */,
                    null /* lease duration */,
                    breakPeriod,
                    accessCondition,
                    null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            }
            Assert.IsNotNull(request);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.VersionHeader(request, false);
            BlobTestUtils.LeaseIdHeader(request, null);
            BlobTestUtils.LeaseActionHeader(request, "break");
            BlobTestUtils.LeaseDurationHeader(request, null);
            BlobTestUtils.ProposedLeaseIdHeader(request, null);
            BlobTestUtils.BreakPeriodHeader(request, breakPeriod.HasValue ? breakPeriod.Value.ToString() : null);
            return request;
        }

        /// <summary>
        /// Validates a response to a break lease operation.
        /// </summary>
        /// <param name="response">The response to validate.</param>
        /// <param name="expectedLeaseTime">The expected remaining lease time.</param>
        /// <param name="errorMargin">The error margin on the expected remaining lease time.</param>
        /// <param name="expectedError">The error status code to expect.</param>
        public static void BreakLeaseResponse(HttpResponseMessage response, int expectedLeaseTime, int errorMargin, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.LeaseIdHeader(response, null);
                BlobTestUtils.LeaseTimeHeader(response, expectedLeaseTime, errorMargin);
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        public static HttpRequestMessage CreateContainerRequest(BlobContext context, string containerName)
        {
            Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
            OperationContext opContext = new OperationContext();
            HttpRequestMessage request = ContainerHttpRequestMessageFactory.Create(uri, context.Timeout, null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Put, request.Method);
            BlobTestUtils.RangeHeader(request, null);
            BlobTestUtils.LeaseIdHeader(request, null);
            return request;
        }

        public static HttpRequestMessage DeleteContainerRequest(BlobContext context, string containerName, AccessCondition accessCondition)
        {
            Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
            OperationContext opContext = new OperationContext();
            HttpRequestMessage request = ContainerHttpRequestMessageFactory.Delete(uri, context.Timeout, accessCondition, null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Delete, request.Method);
            BlobTestUtils.RangeHeader(request, null);
            BlobTestUtils.LeaseIdHeader(request, null);
            return request;
        }

        public static HttpRequestMessage DeleteBlobRequest(BlobContext context, string containerName, string blobName, AccessCondition accessCondition)
        {
            Uri uri = BlobClientTests.ConstructUri(context.Address, containerName, blobName);
            OperationContext opContext = new OperationContext();
            HttpRequestMessage request = BlobHttpRequestMessageFactory.Delete(uri, context.Timeout, null /* snapshot */, DeleteSnapshotsOption.None, accessCondition, null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Delete, request.Method);
            BlobTestUtils.RangeHeader(request, null);
            BlobTestUtils.LeaseIdHeader(request, null);
            return request;
        }

        public static HttpRequestMessage ListBlobsRequest(BlobContext context, string containerName, BlobListingContext listingContext)
        {
            Uri uri = BlobClientTests.ConstructUri(context.Address, containerName);
            OperationContext opContext = new OperationContext();
            HttpRequestMessage request = ContainerHttpRequestMessageFactory.ListBlobs(uri, context.Timeout, listingContext, null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            BlobTestUtils.RangeHeader(request, null);
            BlobTestUtils.LeaseIdHeader(request, null);
            return request;
        }

        public static void ListBlobsResponse(HttpResponseMessage response, BlobContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.ContentTypeHeader(response, "application/xml");
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        public static HttpRequestMessage ListContainersRequest(BlobContext context, ListingContext listingContext)
        {
            Uri uri = BlobClientTests.ConstructUri(context.Address);
            OperationContext opContext = new OperationContext();
            HttpRequestMessage request = ContainerHttpRequestMessageFactory.List(uri, context.Timeout, listingContext, ContainerListingDetails.Metadata, null,
                    opContext,
                    SharedKeyCanonicalizer.Instance,
                    context.Credentials);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            BlobTestUtils.RangeHeader(request, null);
            BlobTestUtils.LeaseIdHeader(request, null);
            return request;
        }

        public static void ListContainersResponse(HttpResponseMessage response, BlobContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.ContentTypeHeader(response, "application/xml");
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        public static HttpRequestMessage GetBlockListRequest(BlobContext context, string containerName, string blobName, BlockListingFilter typesOfBlocks, AccessCondition accessCondition)
        {
            Uri uri = BlobClientTests.ConstructUri(context.Address, containerName, blobName);
            OperationContext opContext = new OperationContext();
            HttpRequestMessage request = BlobHttpRequestMessageFactory.GetBlockList(uri, context.Timeout, null /* snapshot */, typesOfBlocks, accessCondition, null,
                    opContext,
                    SharedKeyCanonicalizer.Instance, 
                    context.Credentials);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            BlobTestUtils.RangeHeader(request, null);
            BlobTestUtils.LeaseIdHeader(request, null);
            return request;
        }

        public static void GetBlockListResponse(HttpResponseMessage response, BlobContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.ReasonPhrase);
                BlobTestUtils.ContentTypeHeader(response, "application/xml");
                BlobTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.ReasonPhrase);
            }
        }

        public static Uri ConstructPutUri(string address, string containerName, string blobName)
        {
            return BlobTests.GenericUri(address, containerName, blobName);
        }

        public static Uri ConstructGetUri(string address, string containerName, string blobName)
        {
            return BlobTests.GenericUri(address, containerName, blobName);
        }

        static Uri GenericUri(string address, string containerName, string blobName)
        {
            Assert.IsNotNull(address);
            Assert.IsNotNull(containerName);
            Assert.IsNotNull(blobName);

            Uri uri = null;
            try
            {
                uri = new Uri(String.Format("{0}/{1}/{2}", address, containerName, blobName));
            }
            catch (Exception)
            {
                Assert.Fail("Cannot create URI with given arguments.");
            }
            return uri;
        }

        public static bool ContainerNameValidator(string name)
        {
            Regex nameRegex = new Regex(@"^([a-z0-9]|((?<=[a-z0-9])-(?=[a-z0-9]))){3,63}$");
            return nameRegex.IsMatch(name);
        }

        public static bool BlobNameValidator(string name)
        {
            Regex nameRegex = new Regex(@"^([0-9a-zA-Z\$\-_\.\+!\*'(),]|(%[0-9a-fA-F]{2})){1,}$");
            return nameRegex.IsMatch(name) && name.Length <= 1024;
        }

        public static bool PutPropertiesValidator(BlobProperties properties)
        {
            if (properties.BlobType == BlobType.Unspecified)
            {
                return false;
            }

            if ((properties.BlobType == BlobType.PageBlob) &&
                (properties.Length % 512 != 0) &&
                (properties.Length > 0))
            {
                return false;
            }

            return true;
        }

        public async static void AcquireAndReleaseLeaseTest(BlobContext context, string containerName, string blobName)
        {
            CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(context.Account, context.Key), false);
            CloudBlobClient client = new CloudBlobClient(new Uri(context.Address), account.Credentials);
            CloudBlobContainer container = client.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            BlobTestBase.UploadText(blob, "Text sent to cloud", Encoding.UTF8);

            // acquire a release on the blob and check LeaseStatus to be "locked"
            OperationContext opContext = new OperationContext();
            HttpRequestMessage blobRequest = BlobHttpRequestMessageFactory.Lease(
                blob.Uri,
                context.Timeout,
                LeaseAction.Acquire,
                null /* proposed lease ID */,
                60 /* lease duration */,
                null /* break period */,
                null /* access condition */,
                null,
                opContext,
                SharedKeyCanonicalizer.Instance,
                context.Credentials);

            string leaseId = null;
            using (HttpResponseMessage response = await BlobTestUtils.GetResponse(blobRequest, context))
            {
                leaseId = HttpResponseParsers.GetHeader(response, "x-ms-lease-id");
                Assert.AreEqual<HttpStatusCode>(response.StatusCode, HttpStatusCode.Created);
            }

            blob.FetchAttributes();
            Assert.AreEqual<LeaseStatus>(blob.Properties.LeaseStatus, LeaseStatus.Locked);

            // release the release on the blob and check LeaseStatus to be "unlocked"
            opContext = new OperationContext();
            blobRequest = BlobHttpRequestMessageFactory.Lease(
                blob.Uri,
                context.Timeout,
                LeaseAction.Release,
                null /* proposed lease ID */,
                null /* lease duration */,
                null /* break period */,
                AccessCondition.GenerateLeaseCondition(leaseId),
                null,
                opContext,
                SharedKeyCanonicalizer.Instance,
                context.Credentials);
            using (HttpResponseMessage response = await BlobTestUtils.GetResponse(blobRequest, context))
            {
                Assert.AreEqual<HttpStatusCode>(response.StatusCode, HttpStatusCode.OK);
            }

            blob.FetchAttributes();
            Assert.AreEqual<LeaseStatus>(blob.Properties.LeaseStatus, LeaseStatus.Unlocked);

            blob.Delete();
        }

        public static bool LeaseIdValidator(AccessCondition accessCondition)
        {
            if (accessCondition != null && accessCondition.LeaseId != null)
            {
                Regex leaseIdRegex = new Regex(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$");
                if (!leaseIdRegex.IsMatch(accessCondition.LeaseId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
