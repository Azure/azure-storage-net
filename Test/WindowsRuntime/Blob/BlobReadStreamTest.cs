// -----------------------------------------------------------------------------------------
// <copyright file="BlobReadStreamTest.cs" company="Microsoft">
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
using System.Threading.Tasks;

#if !NETCORE
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
#endif

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class BlobReadStreamTest : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {
#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public BlobReadStreamTest()
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
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Create a service client with URI and credentials")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobReadStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(5 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(wholeBlob);
                }

                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    using (var blobStream = await blob.OpenReadAsync())
                    {
                        await TestHelper.AssertStreamsAreEqualAsync(wholeBlob, blobStream);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Download a blob using CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobReadStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(5 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(wholeBlob);
                }

                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    using (Stream blobStream = (await blob.OpenReadAsync()))
                    {
                        TestHelper.AssertStreamsAreEqual(wholeBlob, blobStream);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Download a blob using CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobReadStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.AppendBlockAsync(wholeBlob);
                }

                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    using (Stream blobStream = (await blob.OpenReadAsync()))
                    {
                        TestHelper.AssertStreamsAreEqual(wholeBlob, blobStream);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Modify a blob while downloading it using CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobReadLockToETagTestAsync()
        {
            byte[] outBuffer = new byte[1 * 1024 * 1024];
            byte[] buffer = GetRandomBuffer(2 * outBuffer.Length);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamMinimumReadSizeInBytes = outBuffer.Length;
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(wholeBlob);
                }

                OperationContext opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
                    Stream blobStreamForRead = blobStream;
                    await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length);
                    await blob.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "Blob read stream should fail if blob is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
                    Stream blobStreamForRead = blobStream;
                    long length = blobStreamForRead.Length;
                    await blob.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "Blob read stream should fail if blob is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                AccessCondition accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1)));
                await blob.SetMetadataAsync();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenReadAsync(accessCondition, null, opContext),
                    opContext,
                    "Blob read stream should fail if blob is modified during read",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Modify a blob while downloading it using CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobReadLockToETagTestAsync()
        {
            byte[] outBuffer = new byte[1 * 1024 * 1024];
            byte[] buffer = GetRandomBuffer(2 * outBuffer.Length);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamMinimumReadSizeInBytes = outBuffer.Length;
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(wholeBlob);
                }

                OperationContext opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
                    Stream blobStreamForRead = blobStream;
                    await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length);
                    await blob.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "Blob read stream should fail if blob is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
                    Stream blobStreamForRead = blobStream;
                    long length = blobStreamForRead.Length;
                    await blob.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "Blob read stream should fail if blob is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                AccessCondition accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1)));
                await blob.SetMetadataAsync();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenReadAsync(accessCondition, null, opContext),
                    opContext,
                    "Blob read stream should fail if blob is modified during read",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Modify a blob while downloading it using CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobReadLockToETagTestAsync()
        {
            byte[] outBuffer = new byte[1 * 1024 * 1024];
            byte[] buffer = GetRandomBuffer(2 * outBuffer.Length);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                blob.StreamMinimumReadSizeInBytes = outBuffer.Length;
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.AppendBlockAsync(wholeBlob);
                }

                OperationContext opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
                    Stream blobStreamForRead = blobStream;
                    await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length);
                    await blob.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "Blob read stream should fail if blob is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
                    Stream blobStreamForRead = blobStream;
                    long length = blobStreamForRead.Length;
                    await blob.SetMetadataAsync();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blobStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "Blob read stream should fail if blob is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                AccessCondition accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1)));
                await blob.SetMetadataAsync();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenReadAsync(accessCondition, null, opContext),
                    opContext,
                    "Blob read stream should fail if blob is modified during read",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

#if WINDOWS_RT
        private static async Task<uint> BlobReadStreamSeekAndCompareAsync(IRandomAccessStreamWithContentType blobStream, byte[] bufferToCompare, ulong offset, uint readSize, uint expectedReadCount)
#else
        private static async Task<uint> BlobReadStreamSeekAndCompareAsync(Stream blobStream, byte[] bufferToCompare, ulong offset, uint readSize, uint expectedReadCount)
#endif
        {
            byte[] testBuffer = new byte[readSize];
#if NETCORE
            int actualReadSize = await blobStream.ReadAsync(testBuffer, 0, (int) readSize);
            Assert.AreEqual(expectedReadCount, actualReadSize);
#else
            IBuffer testBufferAsIBuffer = testBuffer.AsBuffer();
            await blobStream.ReadAsync(testBufferAsIBuffer, readSize, InputStreamOptions.None);
            Assert.AreEqual(expectedReadCount, testBufferAsIBuffer.Length);
#endif
            long bufferOffset = (long)offset;
            for (int i = 0; i < expectedReadCount; i++, bufferOffset++)
            {
                Assert.AreEqual(bufferToCompare[bufferOffset], testBuffer[i]);
            }

            return expectedReadCount;
        }

#if WINDOWS_RT
        private static async Task<int> BlobReadStreamSeekTestAsync(IRandomAccessStreamWithContentType blobStream, long streamReadSize, byte[] bufferToCompare)
#else
        private static async Task<int> BlobReadStreamSeekTestAsync(Stream blobStream, long streamReadSize, byte[] bufferToCompare)
#endif
        {
            int attempts = 1;
            ulong position = 0;
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 512, 512);
            Assert.AreEqual(position, blobStream.Position);
            position = (ulong)(bufferToCompare.Length - 128);
            blobStream.Seek(position);
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 128);
            attempts++;
            Assert.AreEqual(position, blobStream.Position);
            position = 4096;
            blobStream.Seek(position);
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, blobStream.Position);
            position += 4096;
            blobStream.Seek(position);
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 1024);
            Assert.AreEqual(position, blobStream.Position);
            position -= 4096;
            blobStream.Seek(position);
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 128, 128);
            Assert.AreEqual(position, blobStream.Position);
            position = (ulong)(streamReadSize + 4096 - 512);
            blobStream.Seek(position);
#if NETCORE
            //don't know why adding these two line will pass, but this this the same as the desktop test
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 512);
#endif
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 1024);
            Assert.AreEqual(position, blobStream.Position);
            position -= 1024;
            blobStream.Seek(position);
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 2048, 2048);
            Assert.AreEqual(position, blobStream.Position);
            position = (ulong)(bufferToCompare.Length - 128);
            blobStream.Seek(position);
            Assert.AreEqual(position, blobStream.Position);
            position += await BlobReadStreamSeekAndCompareAsync(blobStream, bufferToCompare, position, 1024, 128);
            Assert.AreEqual(position, blobStream.Position);
            return attempts;
        }

        [TestMethod]
        [Description("Seek and read in a CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobReadStreamSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamMinimumReadSizeInBytes = 2 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(wholeBlob);
                }

                OperationContext opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
#if WINDOWS_RT
                    int attempts = await BlobReadStreamSeekTestAsync(blobStream.AsRandomAccessStream(), blob.StreamMinimumReadSizeInBytes, buffer);
#else 
                    int attempts = await BlobReadStreamSeekTestAsync(blobStream, blob.StreamMinimumReadSizeInBytes, buffer);
#endif
                    TestHelper.AssertNAttempts(opContext, attempts);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Seek and read in a CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobReadStreamSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamMinimumReadSizeInBytes = 2 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(wholeBlob);
                }

                OperationContext opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
#if WINDOWS_RT
                    int attempts = await BlobReadStreamSeekTestAsync(blobStream.AsRandomAccessStream(), blob.StreamMinimumReadSizeInBytes, buffer);
#else 
                    int attempts = await BlobReadStreamSeekTestAsync(blobStream, blob.StreamMinimumReadSizeInBytes, buffer);
#endif
                    TestHelper.AssertNAttempts(opContext, attempts);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Seek and read in a CloudBlobStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobReadStreamSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                blob.StreamMinimumReadSizeInBytes = 2 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    await blob.AppendBlockAsync(wholeBlob);
                }

                OperationContext opContext = new OperationContext();
                using (var blobStream = await blob.OpenReadAsync(null, null, opContext))
                {
#if WINDOWS_RT
                    int attempts = await BlobReadStreamSeekTestAsync(blobStream.AsRandomAccessStream(), blob.StreamMinimumReadSizeInBytes, buffer);
#else 
                    int attempts = await BlobReadStreamSeekTestAsync(blobStream, blob.StreamMinimumReadSizeInBytes, buffer);
#endif
                    TestHelper.AssertNAttempts(opContext, attempts);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
