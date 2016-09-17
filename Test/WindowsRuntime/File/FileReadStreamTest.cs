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
using System.Threading.Tasks;

#if !NETCORE
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileReadStreamTest : FileTestBase
    {
        [TestMethod]
        /// [Description("Download a file using CloudFileStream")]
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
                    var readStream = await file.OpenReadAsync();
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
        /// [Description("Modify a file while downloading it using CloudFileStream")]
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
                using (var fileStream = await file.OpenReadAsync(null, null, opContext))
                {
                    Stream fileStreamForRead = fileStream;
                    await fileStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length);
                    await file.SetMetadataAsync();
                    await ExpectedExceptionAsync(
                        async () => await fileStreamForRead.ReadAsync(outBuffer, 0, outBuffer.Length),
                        opContext,
                        "File read stream should fail if file is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                opContext = new OperationContext();
                using (var fileStream = await file.OpenReadAsync(null, null, opContext))
                {
                    Stream fileStreamForRead = fileStream;
                    long length = fileStreamForRead.Length;
                    await file.SetMetadataAsync();
                    await ExpectedExceptionAsync(
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

        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operationDescription"></param>
        /// <param name="expectedStatusCode"></param>
        private static async Task ExpectedExceptionAsync(Func<Task> operation, OperationContext operationContext, string operationDescription, HttpStatusCode expectedStatusCode, string requestErrorCode = null)
        {
            try
            {
                await operation();
            }
#if NETCORE
            catch (Exception ex)
            {
                Assert.AreEqual((int)expectedStatusCode, ((StorageException)ex).RequestInformation.HttpStatusCode, "Http status code is unexpected.");
                if (!string.IsNullOrEmpty(requestErrorCode))
                {
                    Assert.IsNotNull(operationContext.LastResult.ExtendedErrorInformation);
                    Assert.AreEqual(requestErrorCode, operationContext.LastResult.ExtendedErrorInformation.ErrorCode);
                }
                return;
            }
#else
            catch (IOException ex)
            {
                Assert.AreEqual((int)expectedStatusCode, ((StorageException)ex.InnerException).RequestInformation.HttpStatusCode, "Http status code is unexpected.");
                if (!string.IsNullOrEmpty(requestErrorCode))
                {
                    Assert.IsNotNull(operationContext.LastResult.ExtendedErrorInformation);
                    Assert.AreEqual(requestErrorCode, operationContext.LastResult.ExtendedErrorInformation.ErrorCode);
                }
                return;
            }
#endif

            Assert.Fail("No exception received while expecting {0}: {1}", expectedStatusCode, operationDescription);
        }

#if WINDOWS_RT
        private static async Task<uint> FileReadStreamSeekAndCompareAsync(IRandomAccessStreamWithContentType fileStream, byte[] bufferToCompare, ulong offset, uint readSize, uint expectedReadCount)
#else
        private static async Task<uint> FileReadStreamSeekAndCompareAsync(Stream fileStream, byte[] bufferToCompare, ulong offset, uint readSize, uint expectedReadCount)
#endif
        {

            byte[] testBuffer = new byte[readSize];

#if NETCORE
            int actualReadSize = await fileStream.ReadAsync(testBuffer, 0, (int) readSize);
            Assert.AreEqual(expectedReadCount, actualReadSize);
#else
            IBuffer testBufferAsIBuffer = testBuffer.AsBuffer();
            await fileStream.ReadAsync(testBufferAsIBuffer, readSize, InputStreamOptions.None);
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
        private static async Task<int> FileReadStreamSeekTestAsync(IRandomAccessStreamWithContentType fileStream, long streamReadSize, byte[] bufferToCompare)
#else
        private static async Task<int> FileReadStreamSeekTestAsync(Stream fileStream, long streamReadSize, byte[] bufferToCompare)
#endif
        {
            int attempts = 1;
            ulong position = 0;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 512, 512);
            Assert.AreEqual(position, fileStream.Position);
            position = (ulong)(bufferToCompare.Length - 128);
            fileStream.Seek(position);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 128);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            position = 4096;
            fileStream.Seek(position);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            position += 4096;
            fileStream.Seek(position);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            Assert.AreEqual(position, fileStream.Position);
            position -= 4096;
            fileStream.Seek(position);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 128, 128);
            Assert.AreEqual(position, fileStream.Position);
            position = (ulong)(streamReadSize + 4096 - 512);
            fileStream.Seek(position);
#if NETCORE     
            //don't know why adding these two line will pass, but this this the same as the desktop test
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 512);
#endif
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 1024);
            Assert.AreEqual(position, fileStream.Position);
            position -= 1024;
            fileStream.Seek(position);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 2048, 2048);
            Assert.AreEqual(position, fileStream.Position);
            position = (ulong)(bufferToCompare.Length - 128);
            fileStream.Seek(position);
            Assert.AreEqual(position, fileStream.Position);
            position += await FileReadStreamSeekAndCompareAsync(fileStream, bufferToCompare, position, 1024, 128);
            Assert.AreEqual(position, fileStream.Position);
            return attempts;
        }

        [TestMethod]
        /// [Description("Seek and read in a CloudFileStream")]
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
                using (var fileStream = await file.OpenReadAsync(null, null, opContext))
                {
#if NETCORE
                    int attempts = await FileReadStreamSeekTestAsync(fileStream, file.StreamMinimumReadSizeInBytes, buffer);
#else
                    int attempts = await FileReadStreamSeekTestAsync(fileStream.AsRandomAccessStream(), file.StreamMinimumReadSizeInBytes, buffer);
#endif
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
