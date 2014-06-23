// -----------------------------------------------------------------------------------------
// <copyright file="FileWriteStreamTest.cs" company="Microsoft">
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
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileWriteStreamTest : FileTestBase
    {
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
        [Description("Create files using file stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamOpenAndCloseAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenWriteAsync(null, null, null, opContext),
                    opContext,
                    "Opening a file stream with no size should fail on a file that does not exist",
                    HttpStatusCode.NotFound);
                using (Stream writeStream = await file.OpenWriteAsync(1024))
                {
                }
                using (Stream writeStream = await file.OpenWriteAsync(null))
                {
                }

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1024, file2.Properties.Length);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        /*
        [TestMethod]
        [Description("Create a file using file stream by specifying an access condition")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamOpenWithAccessConditionAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudFile existingFile = share.GetRootDirectoryReference().GetFileReference("file");
                await existingFile.CreateAsync(1024);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                Stream fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "FileWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
        */

        [TestMethod]
        [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);

            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeFile = new MemoryStream())
                {
                    using (Stream writeStream = await file.OpenWriteAsync(buffer.Length * 3))
                    {
                        Stream fileStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await fileStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }

                        await fileStream.FlushAsync();
                    }

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }

                    using (Stream writeStream = await file.OpenWriteAsync(null))
                    {
                        Stream fileStream = writeStream;
                        fileStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeFile.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            fileStream.Write(buffer, 0, buffer.Length);
                            wholeFile.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }

                        await fileStream.FlushAsync();
                    }

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamRandomSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    using (Stream fileStream = await file.OpenWriteAsync(buffer.Length))
                    {
                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length / 512) * 512;
                            TestHelper.SeekRandomly(fileStream, offset);
                            await fileStream.WriteAsync(buffer, 0, buffer.Length - offset);
                            wholeFile.Seek(offset, SeekOrigin.Begin);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length - offset);
                        }
                    }

                    await file.FetchAttributesAsync();
                    Assert.IsNull(file.Properties.ContentMD5);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test the effects of file stream's flush functionality")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudFileStream fileStream = await file.OpenWriteAsync(4 * 512, null, null, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await fileStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await fileStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeFile.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await Task.Factory.FromAsync(fileStream.BeginCommit, fileStream.EndCommit, null);

                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
