// -----------------------------------------------------------------------------------------
// <copyright file="BlobTestUtils.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    internal class BlobTestUtils
    {
        #region Request Validation

        public static void DateHeader(HttpRequestMessage request, bool required)
        {
            bool standardHeader = HttpRequestParsers.GetDate(request) != null;
            bool msHeader = HttpRequestParsers.GetHeader(request, "x-ms-date") != null;

            Assert.IsFalse(standardHeader && msHeader);
            Assert.IsFalse(required && !(standardHeader ^ msHeader));

            if (standardHeader)
            {
                try
                {
                    DateTime parsed = DateTime.Parse(HttpRequestParsers.GetDate(request)).ToUniversalTime();
                }
                catch (Exception)
                {
                    Assert.Fail();
                }
            }
            else if (msHeader)
            {
                try
                {
                    DateTime parsed = DateTime.Parse(HttpRequestParsers.GetHeader(request, "x-ms-date")).ToUniversalTime();
                }
                catch (Exception)
                {
                    Assert.Fail();
                }
            }
        }

        public static void VersionHeader(HttpRequestMessage request, bool required)
        {
            Assert.IsFalse(required && (HttpRequestParsers.GetHeader(request, "x-ms-version") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-version") != null)
            {
                Assert.AreEqual(Constants.HeaderConstants.TargetStorageVersion, HttpRequestParsers.GetHeader(request, "x-ms-version"));
            }
        }

        public static void ContentLengthHeader(HttpRequestMessage request, long expectedValue)
        {
            Assert.AreEqual(expectedValue, HttpRequestParsers.GetContentLength(request));
        }

        public static void ContentTypeHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetContentType(request) == null));
            if (HttpRequestParsers.GetContentType(request) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetContentType(request));
            }
        }

        public static void ContentDispositionHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, Constants.HeaderConstants.BlobContentDispositionRequestHeader) == null));
            if (HttpRequestParsers.GetHeader(request, Constants.HeaderConstants.BlobContentDispositionRequestHeader) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetHeader(request, Constants.HeaderConstants.BlobContentDispositionRequestHeader));
            }
        }

        public static void ContentEncodingHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetContentEncoding(request) == null));
            if (HttpRequestParsers.GetContentEncoding(request) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetContentEncoding(request));
            }
        }

        public static void ContentLanguageHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetContentLanguage(request) == null));
            if (HttpRequestParsers.GetContentLanguage(request) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetContentLanguage(request));
            }
        }

        public static void ContentMd5Header(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetContentMD5(request) == null));
            if (HttpRequestParsers.GetContentMD5(request) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetContentMD5(request));
            }
        }

        public static void ContentCrc64Header(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetContentCRC64(request) == null));
            if (HttpRequestParsers.GetContentCRC64(request) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetContentCRC64(request));
            }
        }

        public static void CacheControlHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetCacheControl(request) == null));
            if (HttpRequestParsers.GetCacheControl(request) != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetCacheControl(request));
            }
        }

        public static void BlobTypeHeader(HttpRequestMessage request, BlobType? expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-blob-type") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-blob-type") != null)
            {
                string blobTypeString = HttpRequestParsers.GetHeader(request, "x-ms-blob-type");
                Assert.IsNotNull(blobTypeString);
                BlobType? blobType = null;
                switch (blobTypeString)
                {
                    case "PageBlob":
                        blobType = BlobType.PageBlob;
                        break;
                    case "BlockBlob":
                        blobType = BlobType.BlockBlob;
                        break;
                }

                Assert.AreEqual(expectedValue, blobType);
            }
        }

        public static void BlobSizeHeader(HttpRequestMessage request, long? expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-blob-content-length") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-blob-content-length") != null)
            {
                long? parsed = long.Parse(HttpRequestParsers.GetHeader(request, "x-ms-blob-content-length"));
                Assert.IsNotNull(parsed);
                Assert.AreEqual(expectedValue, parsed);
            }
        }

        /// <summary>
        /// Tests for a range header in an HTTP request, where no end range is expected.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="expectedStart">The expected beginning of the range, or null if no range is expected.</param>
        public static void RangeHeader(HttpRequestMessage request, long? expectedStart)
        {
            RangeHeader(request, expectedStart, null);
        }

        /// <summary>
        /// Tests for a range header in an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="expectedStart">The expected beginning of the range, or null if no range is expected.</param>
        /// <param name="expectedEnd">The expected end of the range, or null if no end is expected.</param>
        public static void RangeHeader(HttpRequestMessage request, long? expectedStart, long? expectedEnd)
        {
            // The range in "x-ms-range" is used if it exists, or else "Range" is used.
            string requestRange = HttpRequestParsers.GetHeader(request, "x-ms-range") ?? HttpRequestParsers.GetContentRange(request);

            // We should find a range if and only if we expect one.
            Assert.AreEqual(expectedStart.HasValue, requestRange != null);

            // If we expect a range, the range we find should be identical.
            if (expectedStart.HasValue)
            {
                string rangeStart = expectedStart.Value.ToString();
                string rangeEnd = expectedEnd.HasValue ? expectedEnd.Value.ToString() : string.Empty;
                string expectedValue = string.Format("bytes={0}-{1}", rangeStart, rangeEnd);
                Assert.AreEqual(expectedValue.ToString(), requestRange);
            }
        }

        static AutoResetEvent setRequestAsyncSem = new AutoResetEvent(false);
        static Stream setRequestAsyncStream;


        //static void ReadCallback(IAsyncResult result)
        //{
        //    HttpResponseMessage response = ((System.Threading.Tasks.Task<HttpResponseMessage>)(result)).Result;
        //    setRequestAsyncStream = response.Content.ReadAsStreamAsync().Result;
        //    setRequestAsyncSem.Set();
        //}

        #endregion

        #region Response Validation

        /// <summary>
        /// Tests for a content range header in an HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="expectedStart">The expected beginning of the range.</param>
        /// <param name="expectedEnd">The expected end of the range.</param>
        /// <param name="expectedTotal">The expected total number of bytes in the range.</param>
        public static void ContentRangeHeader(HttpResponseMessage response, long expectedStart, long expectedEnd, long expectedTotal)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetContentRange(response));
            string expectedRange = string.Format("bytes {0}-{1}/{2}", expectedStart, expectedEnd, expectedTotal);
            Assert.AreEqual(expectedRange, HttpResponseParsers.GetContentRange(response));
        }

        /// <summary>
        /// Validates a lease ID header in a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void LeaseIdHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-lease-id") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-lease-id") != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetHeader(request, "x-ms-lease-id"));
            }
        }

        /// <summary>
        /// Validates a lease action header in a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void LeaseActionHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-lease-action") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-lease-action") != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetHeader(request, "x-ms-lease-action"));
            }
        }

        /// <summary>
        /// Validates a proposed lease ID header in a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void ProposedLeaseIdHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-proposed-lease-id") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-proposed-lease-id") != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetHeader(request, "x-ms-proposed-lease-id"));
            }
        }

        /// <summary>
        /// Validates a lease duration header in a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void LeaseDurationHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-lease-duration") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-lease-duration") != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetHeader(request, "x-ms-lease-duration"));
            }
        }

        /// <summary>
        /// Validates a break period header in a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void BreakPeriodHeader(HttpRequestMessage request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (HttpRequestParsers.GetHeader(request, "x-ms-lease-break-period") == null));
            if (HttpRequestParsers.GetHeader(request, "x-ms-lease-break-period") != null)
            {
                Assert.AreEqual(expectedValue, HttpRequestParsers.GetHeader(request, "x-ms-lease-break-period"));
            }
        }

        public static void AuthorizationHeader(HttpRequestMessage request, bool required, string account)
        {
            Assert.IsFalse(required && (HttpRequestParsers.GetAuthorization(request) == null));
            if (HttpRequestParsers.GetAuthorization(request) != null)
            {
                string authorization = HttpRequestParsers.GetAuthorization(request);
                string pattern = String.Format("^(SharedKey|SharedKeyLite) {0}:[0-9a-zA-Z\\+/=]{{20,}}$", account);
                Regex authorizationRegex = new Regex(pattern);
                Assert.IsTrue(authorizationRegex.IsMatch(authorization));
            }
        }

        public static void ETagHeader(HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetETag(response));
            Regex eTagRegex = new Regex(@"^""0x[A-F0-9]{15,}""$");
            Assert.IsTrue(eTagRegex.IsMatch(HttpResponseParsers.GetETag(response)));
        }

        public static void LastModifiedHeader(HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetLastModifiedTime(response));
            try
            {
                DateTime parsed = HttpResponseParsers.GetLastModifiedTime(response).ToUniversalTime().Date;
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        public static void ContentChecksumHeader(HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            Assert.IsFalse(HttpResponseParsers.GetContentMD5(response) == null && HttpResponseParsers.GetContentCRC64(response) == null);
        }

        public static void RequestIdHeader(HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetHeader(response, "x-ms-request-id"));
        }

        public static void ContentLengthHeader(HttpResponseMessage response, long expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.AreEqual(expectedValue.ToString(), HttpResponseParsers.GetContentLength(response));
        }

        public static void ContentTypeHeader(HttpResponseMessage response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetContentType(response));
            Assert.AreEqual(expectedValue, HttpResponseParsers.GetContentType(response));
        }

        public static void ContentRangeHeader(HttpResponseMessage response, PageRange expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetContentRange(response));
            Assert.AreEqual(expectedValue.ToString(), HttpResponseParsers.GetContentRange(response));
        }

        public static void ContentDispositionHeader(HttpResponseMessage response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetContentDisposition(response));
            Assert.AreEqual(expectedValue, HttpResponseParsers.GetContentDisposition(response));
        }
        
        public static void ContentEncodingHeader(HttpResponseMessage response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetContentEncoding(response));
            Assert.AreEqual(expectedValue, HttpResponseParsers.GetContentEncoding(response));
        }

        public static void ContentLanguageHeader(HttpResponseMessage response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetContentLanguage(response));
            Assert.AreEqual(expectedValue, HttpResponseParsers.GetContentLanguage(response));
        }

        public static void CacheControlHeader(HttpResponseMessage response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(HttpResponseParsers.GetCacheControl(response));
            Assert.AreEqual(expectedValue, HttpResponseParsers.GetCacheControl(response));
        }

        public static void BlobTypeHeader(HttpResponseMessage response, BlobType expectedValue)
        {
            Assert.IsNotNull(response);
            string header = HttpResponseParsers.GetHeader(response, "x-ms-blob-type");
            BlobType? parsed = null;
            switch (header)
            {
                case "BlockBlob":
                    parsed = BlobType.BlockBlob;
                    break;
                case "PageBlob":
                    parsed = BlobType.PageBlob;
                    break;
            }
            Assert.IsNotNull(parsed);
            Assert.AreEqual(expectedValue, parsed);
        }

        /// <summary>
        /// Validates a lease time header in a response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="expectedValue">The expected value.</param>
        /// <param name="errorMargin">The margin of error in the value.</param>
        public static void LeaseTimeHeader(HttpResponseMessage response, int? expectedValue, int? errorMargin)
        {
            int? leaseTime = BlobHttpResponseParsers.GetRemainingLeaseTime(response);
            Assert.IsFalse((expectedValue != null) && (leaseTime == null));
            if (leaseTime != null)
            {
                int error = Math.Abs(expectedValue.Value - leaseTime.Value);
                Assert.IsTrue(error < errorMargin, "Lease Time header is not within expected range.");
            }
        }

        /// <summary>
        /// Validates a lease ID header in a response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void LeaseIdHeader(HttpResponseMessage response, string expectedValue)
        {
            string leaseId = BlobHttpResponseParsers.GetLeaseId(response);
            Assert.IsFalse((expectedValue != null) && (leaseId == null));
            if (leaseId != null)
            {
                LeaseIdHeader(response);
                Assert.AreEqual(expectedValue, leaseId);
            }
        }

        /// <summary>
        /// Validates a lease ID header in a response.
        /// </summary>
        /// <param name="response">The response.</param>
        public static void LeaseIdHeader(HttpResponseMessage response)
        {
            string leaseId = BlobHttpResponseParsers.GetLeaseId(response);
            Assert.IsNotNull(leaseId);
            Assert.IsTrue(BlobTests.LeaseIdValidator(AccessCondition.GenerateLeaseCondition(leaseId)));
        }

        public static void Contents(HttpResponseMessage response, byte[] expectedContent)
        {
            Assert.IsNotNull(response);
            Assert.IsTrue(long.Parse(HttpResponseParsers.GetContentLength(response)) >= 0);
            byte[] buf = new byte[long.Parse(HttpResponseParsers.GetContentLength(response))];
            Stream stream = HttpResponseParsers.GetResponseStream(response);
            // Have to read one byte each time because of an issue of this stream.
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(stream.ReadByte());
            }
            stream.Close();
            Assert.IsTrue(buf.SequenceEqual(expectedContent));
        }

        #endregion

        #region Helpers

        public static async Task<HttpResponseMessage> GetResponse(HttpRequestMessage request, BlobContext context, CancellationTokenSource token = null)
        {
            Assert.IsNotNull(request);
            HttpResponseMessage response = null;
            try
            {
                HttpClient httpClient = HttpClientFactory.Instance;

                if (token != null)
                {
                    response = await httpClient.SendAsync(request, token.Token);
                }
                else
                {
                    response = await httpClient.SendAsync(request);
                }
            }
            catch (HttpRequestException)
            {
            }
            Assert.IsNotNull(response);
            return response;
        }

        public static bool ContentValidator(byte[] content)
        {
            return content != null;
        }

        #endregion
    }
}
