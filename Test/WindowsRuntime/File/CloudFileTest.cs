// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileTest.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

#if NETCORE
using System.Security.Cryptography;
#else
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class CloudFileTest : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public CloudFileTest()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Create a zero-length file and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCreateAndDeleteAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(0);
                Assert.IsTrue(await file.ExistsAsync());
                await file.DeleteAsync();
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Resize a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileResizeAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                await file.CreateAsync(1024);
                Assert.AreEqual(1024, file.Properties.Length);
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1024, file2.Properties.Length);
                file2.Properties.ContentType = "text/plain";
                await file2.SetPropertiesAsync();
                await file.ResizeAsync(2048);
                Assert.AreEqual(2048, file.Properties.Length);
                await file.FetchAttributesAsync();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                await file2.FetchAttributesAsync();
                Assert.AreEqual(2048, file2.Properties.Length);

                // Resize to 0 length
                await file.ResizeAsync(0);
                Assert.AreEqual(0, file.Properties.Length);
                await file.FetchAttributesAsync();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                await file2.FetchAttributesAsync();
                Assert.AreEqual(0, file2.Properties.Length);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDeleteIfExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                Assert.IsFalse(await file.DeleteIfExistsAsync());
                await file.CreateAsync(0);
                Assert.IsTrue(await file.DeleteIfExistsAsync());
                Assert.IsFalse(await file.DeleteIfExistsAsync());
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Check a file's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                Assert.IsFalse(await file2.ExistsAsync());

                await file.CreateAsync(2048);

                Assert.IsTrue(await file2.ExistsAsync());
                Assert.AreEqual(2048, file2.Properties.Length);

                await file.DeleteAsync();

                Assert.IsFalse(await file2.ExistsAsync());
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileFetchAttributesAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                Assert.AreEqual(1024, file.Properties.Length);
                Assert.IsNotNull(file.Properties.ETag);
                Assert.IsTrue(file.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(file.Properties.CacheControl);
                Assert.IsNull(file.Properties.ContentEncoding);
                Assert.IsNull(file.Properties.ContentLanguage);
                Assert.IsNull(file.Properties.ContentType);
                Assert.IsNull(file.Properties.ContentMD5);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1024, file2.Properties.Length);
                Assert.AreEqual(file.Properties.ETag, file2.Properties.ETag);
                Assert.AreEqual(file.Properties.LastModified, file2.Properties.LastModified);
#if WINDOWS_RT && !WINDOWS_PHONE
                Assert.IsNull(file2.Properties.CacheControl);
#endif
                Assert.IsNull(file2.Properties.ContentEncoding);
                Assert.IsNull(file2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", file2.Properties.ContentType);
                Assert.IsNull(file2.Properties.ContentMD5);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSetPropertiesAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                string eTag = file.Properties.ETag;
                DateTimeOffset lastModified = file.Properties.LastModified.Value;

                await Task.Delay(1000);

                file.Properties.CacheControl = "no-transform";
                file.Properties.ContentEncoding = "gzip";
                file.Properties.ContentLanguage = "tr,en";
                file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                file.Properties.ContentType = "text/html";
                await file.SetPropertiesAsync();
                Assert.IsTrue(file.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, file.Properties.ETag);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual("no-transform", file2.Properties.CacheControl);
                Assert.AreEqual("gzip", file2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", file2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", file2.Properties.ContentMD5);
                Assert.AreEqual("text/html", file2.Properties.ContentType);

                CloudFile file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream stream = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    await file3.DownloadToStreamAsync(stream, null, options, null);
                }
                AssertAreEqual(file2.Properties, file3.Properties);

                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                IEnumerable<IListFileItem> results = await ListFilesAndDirectoriesAsync(rootDirectory, null, null, null);
                CloudFile file4 = (CloudFile)results.First();
                Assert.AreEqual(file2.Properties.Length, file4.Properties.Length);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that creating a file can also set its metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCreateWithMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Metadata["key1"] = "value1";
                await file.CreateAsync(1024);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that a file's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSetMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(0, file2.Metadata.Count);

                OperationContext operationContext = new OperationContext();
                file.Metadata["key1"] = null;

                Assert.ThrowsException<AggregateException>(
                    () => file.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "";
                Assert.ThrowsException<AggregateException>(
                    () => file.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "value1";
                await file.SetMetadataAsync();

                await file2.FetchAttributesAsync();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);

                file.Metadata.Clear();
                await file.SetMetadataAsync();

                await file2.FetchAttributesAsync();
                Assert.AreEqual(0, file2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload/clear ranges in a file and then verify ranges")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileListRangesAsync()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await file.WriteRangeAsync(memoryStream, 512, null);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await file.WriteRangeAsync(memoryStream, 3 * 1024, null);
                }

                await file.ClearRangeAsync(1024, 1024);
                await file.ClearRangeAsync(0, 512);

                IEnumerable<FileRange> fileRanges = await file.ListRangesAsync();
                List<string> expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (FileRange fileRange in fileRanges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(fileRange.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                fileRanges = await file.ListRangesAsync(1024, 1024, null, null, null);
                Assert.AreEqual(0, fileRanges.Count());

                fileRanges = await file.ListRangesAsync(512, 3 * 1024, null, null, null);
                expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (FileRange fileRange in fileRanges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(fileRange.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.ListRangesAsync(1024, null, null, null, opContext),
                    opContext,
                    "List Ranges with an offset but no count should fail",
                    HttpStatusCode.Unused);
                Assert.IsInstanceOfType(opContext.LastResult.Exception.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload ranges to a file and then verify the contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileWriteRangeAsync()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
#if NETCORE
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));
#else
            CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
            hasher.Append(buffer.AsBuffer());
            string contentMD5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset());
#endif

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(4 * 1024 * 1024);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await file.WriteRangeAsync(memoryStream, 0, null),
                        "Zero-length WriteRange should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        OperationContext opContext = new OperationContext();
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await file.WriteRangeAsync(memoryStream, 512, null, null, null, opContext),
                            opContext,
                            "Writing out-of-range ranges should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await file.WriteRangeAsync(memoryStream, 0, contentMD5);
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await file.WriteRangeAsync(memoryStream, 0, contentMD5, null, null, opContext),
                            opContext,
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await file.WriteRangeAsync(memoryStream, 0, null);
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await file.WriteRangeAsync(memoryStream, 1024, null);
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream fileData = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(fileData);
                        Assert.AreEqual(resultingData.Length, fileData.Length);

                        Assert.IsTrue(fileData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        /*
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamWithAccessConditionAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("\"*\"");
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0),
                    operationContext,
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                await file.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0),
                    operationContext,
                    "Uploading a file on top of an non-existing file should fail when the ETag doesn't match",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
        */

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, null, null, 0);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, null, null, 1024);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamLengthAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                // Upload half
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, 3 * 512, null, null, 0);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, 3 * 512, null, null, 1024);

                // Upload full stream
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, 6 * 512, null, null, 0);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, 4 * 512, null, null, 1024);

                // Exclude last range
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, 5 * 512, null, null, 0);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, 3 * 512, null, null, 1024);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamLengthInvalidAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await this.CloudFileUploadFromStreamAsync(share, 3 * 512, 4 * 512, null, null, 0),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await this.CloudFileUploadFromStreamAsync(share, 3 * 512, 2 * 512, null, null, 1024),
                        "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        private async Task CloudFileUploadFromStreamAsync(CloudFileShare share, int size, long? copyLength, AccessCondition accessCondition, OperationContext operationContext, int startOffset)
        {
            byte[] buffer = GetRandomBuffer(size);
#if NETCORE
            MD5 hasher = MD5.Create();
            string md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
#else
            CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
            hasher.Append(buffer.AsBuffer(startOffset, copyLength.HasValue ? (int)copyLength.Value : buffer.Length - startOffset));
            string md5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset());
#endif

            CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
            file.StreamWriteSizeInBytes = 512;

            using (MemoryStream originalFileStream = new MemoryStream())
            {
                originalFileStream.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };
                    if (copyLength.HasValue)
                    {
                        await file.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, options, operationContext);
                    }
                    else
                    {
                        await file.UploadFromStreamAsync(sourceStream, accessCondition, options, operationContext);
                    }
                }

                await file.FetchAttributesAsync();
                Assert.AreEqual(md5, file.Properties.ContentMD5);

                using (MemoryStream downloadedFileStream = new MemoryStream())
                {
                    await file.DownloadToStreamAsync(downloadedFileStream);
                    Assert.AreEqual(copyLength ?? originalFileStream.Length, downloadedFileStream.Length);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalFileStream,
                        downloadedFileStream,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalFileStream.Length);
                }
            }
        }

        /*
        [TestMethod]
        [Description("Test conditional access on a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                await file.FetchAttributesAsync();

                string currentETag = file.Properties.ETag;
                DateTimeOffset currentModifiedTime = file.Properties.LastModified.Value;

                // ETag conditional tests
                file.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await file.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(currentETag), null, null);

                await file.FetchAttributesAsync();
                string newETag = file.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                file.Metadata["ETagConditionalName"] = "ETagConditionalValue2";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(newETag), null, operationContext),
                    operationContext,
                    "If none match on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                string invalidETag = "\"0x10101010\"";
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(invalidETag), null, operationContext),
                    operationContext,
                    "Invalid ETag on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                currentETag = file.Properties.ETag;
                await file.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(invalidETag), null, null);

                await file.FetchAttributesAsync();
                newETag = file.Properties.ETag;

                // LastModifiedTime tests
                currentModifiedTime = file.Properties.LastModified.Value;

                file.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                currentModifiedTime = file.Properties.LastModified.Value;

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                file.Metadata["DateConditionalName"] = "DateConditionalValue2";

                currentETag = file.Properties.ETag;
                await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null, null);

                await file.FetchAttributesAsync();
                newETag = file.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
        */

        [TestMethod]
        [Description("Test file sizes")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileAlignmentAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                OperationContext operationContext = new OperationContext();

                await file.CreateAsync(511);
                await file.CreateAsync(512);
                await file.CreateAsync(513);

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(511);
                    await file.WriteRangeAsync(stream, 0, null);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await file.WriteRangeAsync(stream, 0, null);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(513);
                    await file.WriteRangeAsync(stream, 0, null);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload and download null/empty data")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadDownloadNoDataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await file.UploadFromStreamAsync(null),
                    "Uploading from a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await file.UploadFromStreamAsync(stream);
                }

                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await file.DownloadToStreamAsync(null),
                    "Downloading to a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await file.DownloadToStreamAsync(stream);
                    Assert.AreEqual(0, stream.Length);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
