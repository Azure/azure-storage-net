// -----------------------------------------------------------------------------------------
// <copyright file="FileReadStreamTest.cs" company="Microsoft">
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
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileReadStreamTest : FileTestBase
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
        [Description("Download a file using CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileReadStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(5 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(wholeFile);
                }

                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    Stream readStream = await file.OpenReadAsync();
                    using (Stream fileStream = readStream)
                    {
                        TestHelper.AssertStreamsAreEqual(wholeFile, fileStream);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Modify a file while downloading it using CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileReadLockToETagTestAsync()
        {
            byte[] outBuffer = new byte[1 * 1024 * 1024];
            byte[] buffer = GetRandomBuffer(2 * outBuffer.Length);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamMinimumReadSizeInBytes = outBuffer.Length;
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(wholeFile);
                }

                OperationContext opContext = new OperationContext();
                using (Stream fileStreamForRead = await file.OpenReadAsync(null, null, opContext))
                {
                    await fileStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length);
                    await file.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await fileStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "File read stream should fail if file is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                using (Stream fileStream = await file.OpenReadAsync(null, null, opContext))
                {
                    Stream fileStreamForRead = fileStream;
                    long length = fileStreamForRead.Length;
                    await file.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await fileStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "File read stream should fail if file is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                /*
                opContext = new OperationContext();
                AccessCondition accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1)));
                await file.SetMetadataAsync();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenReadAsync(accessCondition, null, opContext),
                    opContext,
                    "File read stream should fail if file is modified during read",
                    HttpStatusCode.PreconditionFailed);
                 */
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        private static async Task<int> FileReadStreamSeekAndCompareAsync(Stream fileStream, byte[] bufferToCompare, long offset, int readSize, int expectedReadCount)
        {
            byte[] testBuffer = new byte[readSize];

            int readCount = await fileStream.ReadAsync(testBuffer, 0, readSize);
            Assert.AreEqual(expectedReadCount, readCount);

            for (int i = 0; i < expectedReadCount; i++)
            {
                Assert.AreEqual(bufferToCompare[i + offset], testBuffer[i]);
            }

            return expectedReadCount;
        }

        private static async Task<int> FileReadStreamSeekTestAsync(Stream fileStream, long streamReadSize, byte[] bufferToCompare)
        {
            int attempts = 1;
            long position = 0;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 512, 512);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-128, SeekOrigin.End);
            position = bufferToCompare.Length - 128;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 128);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(4096, SeekOrigin.Begin);
            position = 4096;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(4096, SeekOrigin.Current);
            position += 4096;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-4096, SeekOrigin.Current);
            position -= 4096;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 128, 128);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(streamReadSize + 4096 - 512, SeekOrigin.Begin);
            position = streamReadSize + 4096 - 512;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 512);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-1024, SeekOrigin.Current);
            position -= 1024;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 2048, 2048);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-128, SeekOrigin.End);
            position = bufferToCompare.Length - 128;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 128);
            Assert.AreEqual(position, fileStream.Position);
            return attempts;
        }

        [TestMethod]
        [Description("Seek and read in a CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileReadStreamSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamMinimumReadSizeInBytes = 2 * 1024 * 1024;
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(wholeFile);
                }

                OperationContext opContext = new OperationContext();
                using (Stream fileStream = await file.OpenReadAsync(null, null, opContext))
                {
                    int attempts = await FileReadStreamSeekTestAsync(fileStream, file.StreamMinimumReadSizeInBytes, buffer);
                    TestHelper.AssertNAttempts(opContext, attempts);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
