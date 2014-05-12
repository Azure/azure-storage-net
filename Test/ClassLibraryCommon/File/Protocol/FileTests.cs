// -----------------------------------------------------------------------------------------
// <copyright file="FileTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Auth.Protocol;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    internal class FileTests
    {
        public static HttpWebRequest PutFileRequest(FileContext context, string shareName, string fileName,
            FileProperties properties, byte[] content, long fileSize, AccessCondition accessCondition)
        {
            bool valid = FileTests.ShareNameValidator(shareName) &&
                FileTests.FileNameValidator(fileName) &&
                FileTestUtils.ContentValidator(content);

            Uri uri = FileTests.ConstructPutUri(context.Address, shareName, fileName);
            HttpWebRequest request = null;
            OperationContext opContext = new OperationContext();
            try
            {
                request = FileHttpWebRequestFactory.Create(uri, context.Timeout, properties, fileSize, accessCondition, true, opContext);
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
                Assert.AreEqual("PUT", request.Method);
                FileTestUtils.VersionHeader(request, false);
                FileTestUtils.ContentTypeHeader(request, null);
                FileTestUtils.ContentDispositionHeader(request, properties.ContentDisposition);
                FileTestUtils.ContentEncodingHeader(request, properties.ContentEncoding);
                FileTestUtils.ContentLanguageHeader(request, null);
                FileTestUtils.CacheControlHeader(request, null);
                FileTestUtils.FileTypeHeader(request, "File");
                FileTestUtils.FileSizeHeader(request, properties.Length);
            }
            return request;
        }

        public static void PutFileResponse(HttpWebResponse response, FileContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, response.StatusDescription);
                FileTestUtils.ETagHeader(response);
                FileTestUtils.LastModifiedHeader(response);
                FileTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.StatusDescription);
            }
            response.Close();
        }

        public static HttpWebRequest GetFileRequest(FileContext context, string shareName, string fileName, AccessCondition accessCondition)
        {
            bool valid = FileTests.ShareNameValidator(shareName) &&
                FileTests.FileNameValidator(fileName);
            Uri uri = FileTests.ConstructGetUri(context.Address, shareName, fileName);
            HttpWebRequest request = null;
            OperationContext opContext = new OperationContext();
            try
            {
                request = FileHttpWebRequestFactory.Get(uri, context.Timeout, accessCondition, true, opContext);
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
                Assert.AreEqual("GET", request.Method);
                FileTestUtils.RangeHeader(request, null);
            }
            return request;
        }

        public static void GetFileResponse(HttpWebResponse response, FileContext context, FileProperties properties, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                FileTestUtils.LastModifiedHeader(response);
                FileTestUtils.ETagHeader(response);
                FileTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.StatusDescription);
            }
            response.Close();
        }

        public static HttpWebRequest WriteRangeRequest(FileContext context, string shareName, string fileName, FileRange range, int length, AccessCondition accessCondition)
        {
            bool valid = FileTests.ShareNameValidator(shareName) &&
                FileTests.FileNameValidator(fileName);
            Uri uri = FileTests.ConstructPutUri(context.Address, shareName, fileName);
            HttpWebRequest request = null;
            OperationContext opContext = new OperationContext();
            try
            {
                request = FileHttpWebRequestFactory.PutRange(uri, context.Timeout, range, FileRangeWrite.Update, accessCondition, true, opContext);
                request.ContentLength = length;
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
                Assert.AreEqual("PUT", request.Method);
                FileTestUtils.RangeHeader(request, range.StartOffset, range.EndOffset);
            }
            return request;
        }

        public static void WriteRangeResponse(HttpWebResponse response, FileContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                FileTestUtils.LastModifiedHeader(response);
                FileTestUtils.ETagHeader(response);
                FileTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.StatusDescription);
            }
            response.Close();
        }

        /// <summary>
        /// Generates a get file request over the specified range, and checks the request for consistency.
        /// </summary>
        /// <param name="context">The testing context.</param>
        /// <param name="shareName">The name of the share.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="offset">The offset to the range.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <param name="leaseId">The lease ID, or null if the file is not leased.</param>
        /// <returns>A web request for getting a file range.</returns>
        public static HttpWebRequest GetFileRangeRequest(FileContext context, string shareName, string fileName, long offset, long? count, AccessCondition accessCondition)
        {
            bool valid = FileTests.ShareNameValidator(shareName) &&
                FileTests.FileNameValidator(fileName);

            Uri uri = FileTests.ConstructGetUri(context.Address, shareName, fileName);
            HttpWebRequest request = null;
            OperationContext opContext = new OperationContext();

            try
            {
                request = FileHttpWebRequestFactory.Get(uri, context.Timeout, offset, count, false, accessCondition, true, opContext);
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
                Assert.AreEqual("GET", request.Method);
                FileTestUtils.RangeHeader(request, offset, count.HasValue ? (long?)(count.Value + offset - 1) : null);
            }

            return request;
        }

        /// <summary>
        /// Checks a get file range response for consistency with the given parameters, and closes the response.
        /// </summary>
        /// <param name="response">The HTTP web response to check.</param>
        /// <param name="context">The testing context.</param>
        /// <param name="content">The expected content returned in the response.</param>
        /// <param name="expectedStartRange">The expected start range returned in the response header.</param>
        /// <param name="expectedEndRange">The expected end range returned in the response header.</param>
        /// <param name="expectedTotalBytes">The expected total number of bytes in the file.</param>
        /// <param name="expectedError">The expected error code, or null if the operation is expected to succeed.</param>
        public static void CheckFileRangeResponse(
            HttpWebResponse response,
            FileContext context,
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
                FileTestUtils.LastModifiedHeader(response);
                FileTestUtils.ContentLengthHeader(response, content.Length);
                FileTestUtils.ETagHeader(response);
                FileTestUtils.RequestIdHeader(response);
                FileTestUtils.Contents(response, content);
                FileTestUtils.ContentRangeHeader(response, expectedStartRange, expectedEndRange, expectedTotalBytes);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.StatusDescription);
            }

            response.Close();
        }

        public static HttpWebRequest CreateShareRequest(FileContext context, string shareName)
        {
            Uri uri = FileClientTests.ConstructUri(context.Address, shareName);
            OperationContext opContext = new OperationContext();
            HttpWebRequest request = ShareHttpWebRequestFactory.Create(uri, context.Timeout, true, opContext);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual("PUT", request.Method);
            FileTestUtils.RangeHeader(request, null);
            FileTestUtils.VersionHeader(request, false);
            return request;
        }

        public static HttpWebRequest DeleteShareRequest(FileContext context, string shareName, AccessCondition accessCondition)
        {
            Uri uri = FileClientTests.ConstructUri(context.Address, shareName);
            OperationContext opContext = new OperationContext();
            HttpWebRequest request = ShareHttpWebRequestFactory.Delete(uri, context.Timeout, accessCondition, true, opContext);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual("DELETE", request.Method);
            FileTestUtils.RangeHeader(request, null);
            return request;
        }

        public static HttpWebRequest DeleteFileRequest(FileContext context, string shareName, string fileName, AccessCondition accessCondition)
        {
            Uri uri = FileClientTests.ConstructUri(context.Address, shareName, fileName);
            OperationContext opContext = new OperationContext();
            HttpWebRequest request = FileHttpWebRequestFactory.Delete(uri, context.Timeout, accessCondition, true, opContext);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual("DELETE", request.Method);
            FileTestUtils.RangeHeader(request, null);
            return request;
        }

        public static HttpWebRequest ListFilesAndDirectoriesRequest(FileContext context, string shareName, FileListingContext listingContext)
        {
            Uri uri = FileClientTests.ConstructUri(context.Address, shareName);
            OperationContext opContext = new OperationContext();
            HttpWebRequest request = DirectoryHttpWebRequestFactory.List(uri, context.Timeout, listingContext, true, opContext);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual("GET", request.Method);
            FileTestUtils.RangeHeader(request, null);
            return request;
        }

        public static void ListFilesAndDirectoriesResponse(HttpWebResponse response, FileContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.StatusDescription);
                FileTestUtils.ContentTypeHeader(response, "application/xml");
                FileTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.StatusDescription);
            }
        }

        public static HttpWebRequest ListSharesRequest(FileContext context, ListingContext listingContext)
        {
            Uri uri = FileClientTests.ConstructUri(context.Address);
            OperationContext opContext = new OperationContext();
            HttpWebRequest request = ShareHttpWebRequestFactory.List(uri, context.Timeout, listingContext, ShareListingDetails.Metadata, true, opContext);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Method);
            Assert.AreEqual("GET", request.Method);
            FileTestUtils.RangeHeader(request, null);
            return request;
        }

        public static void ListSharesResponse(HttpWebResponse response, FileContext context, HttpStatusCode? expectedError)
        {
            Assert.IsNotNull(response);
            if (expectedError == null)
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.StatusDescription);
                FileTestUtils.ContentTypeHeader(response, "application/xml");
                FileTestUtils.RequestIdHeader(response);
            }
            else
            {
                Assert.AreEqual(expectedError, response.StatusCode, response.StatusDescription);
            }
        }

        public static Uri ConstructPutUri(string address, string shareName, string fileName)
        {
            return FileTests.GenericUri(address, shareName, fileName);
        }

        public static Uri ConstructGetUri(string address, string shareName, string fileName)
        {
            return FileTests.GenericUri(address, shareName, fileName);
        }

        static Uri GenericUri(string address, string shareName, string fileName)
        {
            Assert.IsNotNull(address);
            Assert.IsNotNull(shareName);
            Assert.IsNotNull(fileName);

            Uri uri = null;
            try
            {
                uri = new Uri(String.Format("{0}/{1}/{2}", address, shareName, fileName));
            }
            catch (Exception)
            {
                Assert.Fail("Cannot create URI with given arguments.");
            }
            return uri;
        }

        public static void SignRequest(HttpWebRequest request, FileContext context)
        {
            Assert.IsNotNull(request);
            Assert.IsNotNull(context);

            OperationContext opContext = new OperationContext();
            SharedKeyAuthenticationHandler handler = new SharedKeyAuthenticationHandler(
                SharedKeyCanonicalizer.Instance,
                context.Credentials,
                context.Account);
            handler.SignRequest(request, opContext);

            FileTestUtils.AuthorizationHeader(request, true, context.Account);
            FileTestUtils.DateHeader(request, true);
        }

        public static bool ShareNameValidator(string name)
        {
            Regex nameRegex = new Regex(@"^([a-z0-9]|((?<=[a-z0-9])-(?=[a-z0-9]))){3,63}$");
            return nameRegex.IsMatch(name);
        }

        public static bool FileNameValidator(string name)
        {
            Regex nameRegex = new Regex(@"^([0-9a-zA-Z\$\-_\.\+!\*'(),]|(%[0-9a-fA-F]{2})){1,}$");
            return nameRegex.IsMatch(name) && name.Length <= 1024;
        }
    }
}
