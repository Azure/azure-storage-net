// -----------------------------------------------------------------------------------------
// <copyright file="FileTestUtils.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    internal class FileTestUtils
    {
        #region Request Validation

        public static void DateHeader(HttpWebRequest request, bool required)
        {
            bool standardHeader = request.Headers[HttpRequestHeader.Date] != null;
            bool msHeader = request.Headers["x-ms-date"] != null;

            Assert.IsFalse(standardHeader && msHeader);
            Assert.IsFalse(required && !(standardHeader ^ msHeader));

            if (request.Headers[HttpRequestHeader.Date] != null)
            {
                try
                {
                    DateTime parsed = DateTime.Parse(request.Headers[HttpRequestHeader.Date]).ToUniversalTime();
                }
                catch (Exception)
                {
                    Assert.Fail();
                }
            }
            else if (request.Headers[HttpRequestHeader.Date] != null)
            {
                try
                {
                    DateTime parsed = DateTime.Parse(request.Headers["x-ms-date"]).ToUniversalTime();
                }
                catch (Exception)
                {
                    Assert.Fail();
                }
            }
        }

        public static void VersionHeader(HttpWebRequest request, bool required)
        {
            Assert.IsFalse(required && (request.Headers["x-ms-version"] == null));
            if (request.Headers["x-ms-version"] != null)
            {
                Assert.AreEqual(Constants.HeaderConstants.TargetStorageVersion, request.Headers["x-ms-version"]);
            }
        }

        public static void ContentLengthHeader(HttpWebRequest request, long expectedValue)
        {
            Assert.AreEqual(expectedValue, request.ContentLength);
        }

        public static void ContentTypeHeader(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.ContentType == null));
            if (request.ContentType != null)
            {
                Assert.AreEqual(expectedValue, request.ContentType);
            }
        }

        public static void ContentDispositionHeader(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers[Constants.HeaderConstants.FileContentDispositionRequestHeader] == null));
            if (request.Headers[Constants.HeaderConstants.FileContentDispositionRequestHeader] != null)
            {
                Assert.AreEqual(expectedValue, request.Headers[Constants.HeaderConstants.FileContentDispositionRequestHeader]);
            }
        }

        public static void ContentEncodingHeader(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers[HttpRequestHeader.ContentEncoding] == null));
            if (request.Headers[HttpRequestHeader.ContentEncoding] != null)
            {
                Assert.AreEqual(expectedValue, request.Headers[HttpRequestHeader.ContentEncoding]);
            }
        }

        public static void ContentLanguageHeader(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers[HttpRequestHeader.ContentLanguage] == null));
            if (request.Headers[HttpRequestHeader.ContentLanguage] != null)
            {
                Assert.AreEqual(expectedValue, request.Headers[HttpRequestHeader.ContentLanguage]);
            }
        }

        public static void ContentMd5Header(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers[HttpRequestHeader.ContentMd5] == null));
            if (request.Headers[HttpRequestHeader.ContentMd5] != null)
            {
                Assert.AreEqual(expectedValue, request.Headers[HttpRequestHeader.ContentMd5]);
            }
        }

        public static void CacheControlHeader(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers[HttpRequestHeader.CacheControl] == null));
            if (request.Headers[HttpRequestHeader.CacheControl] != null)
            {
                Assert.AreEqual(expectedValue, request.Headers[HttpRequestHeader.CacheControl]);
            }
        }

        public static void FileSizeHeader(HttpWebRequest request, long? expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers["x-ms-content-length"] == null));
            if (request.Headers["x-ms-content-length"] != null)
            {
                long? parsed = long.Parse(request.Headers["x-ms-content-length"]);
                Assert.IsNotNull(parsed);
                Assert.AreEqual(expectedValue, parsed);
            }
        }

        /// <summary>
        /// Tests for a range header in an HTTP request, where no end range is expected.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="expectedStart">The expected beginning of the range, or null if no range is expected.</param>
        public static void RangeHeader(HttpWebRequest request, long? expectedStart)
        {
            RangeHeader(request, expectedStart, null);
        }

        public static void FileTypeHeader(HttpWebRequest request, string expectedValue)
        {
            Assert.IsFalse((expectedValue != null) && (request.Headers["x-ms-type"] == null));
            if (request.Headers["x-ms-type"] != null)
            {
                Assert.AreEqual(expectedValue, request.Headers["x-ms-type"]);
            }
        }
        /// <summary>
        /// Tests for a range header in an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="expectedStart">The expected beginning of the range, or null if no range is expected.</param>
        /// <param name="expectedEnd">The expected end of the range, or null if no end is expected.</param>
        public static void RangeHeader(HttpWebRequest request, long? expectedStart, long? expectedEnd)
        {
            // The range in "x-ms-range" is used if it exists, or else "Range" is used.
            string requestRange = request.Headers["x-ms-range"] ?? request.Headers[HttpRequestHeader.Range];

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

        public static void SetRequest(HttpWebRequest request, FileContext context, byte[] content)
        {
            Assert.IsNotNull(request);
            if (context.Async)
            {
                FileTestUtils.SetRequestAsync(request, context, content);
            }
            else
            {
                FileTestUtils.SetRequestSync(request, context, content);
            }
        }

        static AutoResetEvent setRequestAsyncSem = new AutoResetEvent(false);
        static Stream setRequestAsyncStream;
        static void SetRequestAsync(HttpWebRequest request, FileContext context, byte[] content)
        {
            request.BeginGetRequestStream(new AsyncCallback(FileTestUtils.ReadCallback), request);
            setRequestAsyncSem.WaitOne();       
        }

        static void ReadCallback(IAsyncResult result)
        {
            HttpWebRequest request = (HttpWebRequest)result.AsyncState;
            setRequestAsyncStream = request.EndGetRequestStream(result);
            setRequestAsyncSem.Set();
        }

        static void SetRequestSync(HttpWebRequest request, FileContext context, byte[] content)
        {
            Stream stream = request.GetRequestStream();
            Assert.IsNotNull(stream);
            stream.Write(content, 0, content.Length);
            stream.Close();
        }

        #endregion

        #region Response Validation

        /// <summary>
        /// Tests for a content range header in an HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="expectedStart">The expected beginning of the range.</param>
        /// <param name="expectedEnd">The expected end of the range.</param>
        /// <param name="expectedTotal">The expected total number of bytes in the range.</param>
        public static void ContentRangeHeader(HttpWebResponse response, long expectedStart, long expectedEnd, long expectedTotal)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.ContentRange]);
            string expectedRange = string.Format("bytes {0}-{1}/{2}", expectedStart, expectedEnd, expectedTotal);
            Assert.AreEqual(expectedRange, response.Headers[HttpResponseHeader.ContentRange]);
        }

        public static void AuthorizationHeader(HttpWebRequest request, bool required, string account)
        {
            Assert.IsFalse(required && (request.Headers[HttpRequestHeader.Authorization] == null));
            if (request.Headers[HttpRequestHeader.Authorization] != null)
            {
                string authorization = request.Headers[HttpRequestHeader.Authorization];
                string pattern = String.Format("^(SharedKey|SharedKeyLite) {0}:[0-9a-zA-Z\\+/=]{{20,}}$", account);
                Regex authorizationRegex = new Regex(pattern);
                Assert.IsTrue(authorizationRegex.IsMatch(authorization));
            }
        }

        public static void ETagHeader(HttpWebResponse response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.ETag]);
            Regex eTagRegex = new Regex(@"^""0x[A-F0-9]{15,}""$");
            Assert.IsTrue(eTagRegex.IsMatch(response.Headers[HttpResponseHeader.ETag]));
        }

        public static void LastModifiedHeader(HttpWebResponse response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.LastModified]);
            try
            {
                DateTime parsed = DateTime.Parse(response.Headers[HttpResponseHeader.LastModified]).ToUniversalTime();
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        public static void ContentMd5Header(HttpWebResponse response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.ContentMd5]);
        }

        public static void RequestIdHeader(HttpWebResponse response)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers["x-ms-request-id"]);
        }

        public static void ContentLengthHeader(HttpWebResponse response, long expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.AreEqual(expectedValue, response.ContentLength);
        }

        public static void ContentTypeHeader(HttpWebResponse response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.ContentType);
            Assert.AreEqual(expectedValue, response.ContentType);
        }

        public static void ContentRangeHeader(HttpWebResponse response, FileRange expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.ContentRange]);
            Assert.AreEqual(expectedValue.ToString(), response.Headers[HttpResponseHeader.ContentRange]);
        }

        public static void ContentEncodingHeader(HttpWebResponse response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.ContentEncoding);
            Assert.AreEqual(expectedValue, response.ContentEncoding);
        }

        public static void ContentLanguageHeader(HttpWebResponse response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.ContentLanguage]);
            Assert.AreEqual(expectedValue, response.Headers[HttpResponseHeader.ContentLanguage]);
        }

        public static void CacheControlHeader(HttpWebResponse response, string expectedValue)
        {
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers[HttpResponseHeader.CacheControl]);
            Assert.AreEqual(expectedValue, response.Headers[HttpResponseHeader.CacheControl]);
        }

        public static void Contents(HttpWebResponse response, byte[] expectedContent)
        {
            Assert.IsNotNull(response);
            Assert.IsTrue(response.ContentLength >= 0);
            byte[] buf = new byte[response.ContentLength];
            Stream stream = response.GetResponseStream();
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

        public static HttpWebResponse GetResponse(HttpWebRequest request, FileContext context)
        {
            Assert.IsNotNull(request);
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)ex.Response;
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

