// -----------------------------------------------------------------------------------------
// <copyright file="FileUploadDownloadTest.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    [TestClass]
    public class FileUploadDownloadTest : FileTestBase
    {
        private CloudFileShare testShare;

        [TestInitialize]
        public async Task TestInitialize()
        {
            this.testShare = GetRandomShareReference();
            await this.testShare.CreateIfNotExistsAsync();
            
            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await this.testShare.DeleteIfExistsAsync();
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Download a specific range of the file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileDownloadToStreamRangeTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            using (MemoryStream wholeFile = new MemoryStream(buffer))
            {
                await file.UploadFromStreamAsync(wholeFile);

                byte[] testBuffer = new byte[1024];
                MemoryStream fileStream = new MemoryStream(testBuffer);
                Exception ex = await TestHelper.ExpectedExceptionAsync<Exception>(
                    async () => await file.DownloadRangeToStreamAsync(fileStream, 0, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                await file.DownloadRangeToStreamAsync(fileStream, 0, 1024);
                Assert.AreEqual(fileStream.Position, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(fileStream, wholeFile, 0, 0, 1024);

                CloudFile file2 = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
                MemoryStream fileStream2 = new MemoryStream(testBuffer);
                ex = await TestHelper.ExpectedExceptionAsync<Exception>(
                    async () => await file2.DownloadRangeToStreamAsync(fileStream, 1024, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                await file2.DownloadRangeToStreamAsync(fileStream2, 1024, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(fileStream2, wholeFile, 0, 1024, 1024);

                AssertAreEqual(file, file2);
            }
        }

        [TestMethod]
        [Description("Upload a stream to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileUploadFromStreamTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                await file.UploadFromStreamAsync(srcStream);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                await file.DownloadRangeToStreamAsync(dstStream, null, null);
                TestHelper.AssertStreamsAreEqual(srcStream, dstStream);
            }
        }

        [TestMethod]
        [Description("Upload from file to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadDownloadFileAsync()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoUploadDownloadFileAsync(file, 0);
            await this.DoUploadDownloadFileAsync(file, 4096);
        }

        private async Task DoUploadDownloadFileAsync(CloudFile file, int fileSize)
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();

            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (FileStream fileStream = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.WriteAsync(buffer, 0, buffer.Length);
                }

                await file.UploadFromFileAsync(inputFileName);

                OperationContext context = new OperationContext();
                await file.UploadFromFileAsync(inputFileName, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                await TestHelper.ExpectedExceptionAsync<IOException>(
                    async () => await file.DownloadToFileAsync(outputFileName, FileMode.CreateNew),
                    "CreateNew on an existing file should fail");

                context = new OperationContext();
                await file.DownloadToFileAsync(outputFileName, FileMode.Create, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (
                    FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                               outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    await TestHelper.AssertStreamsAreEqualAsync(inputFileStream, outputFileStream);
                }

                await file.DownloadToFileAsync(outputFileName, FileMode.Append);

                using (
                    FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                               outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    Assert.AreEqual(2 * fileSize, outputFileStream.Length);

                    for (int i = 0; i < fileSize; i++)
                    {
                        Assert.AreEqual(inputFileStream.ReadByte(), outputFileStream.ReadByte());
                    }

                    inputFileStream.Seek(0, SeekOrigin.Begin);
                    for (int i = 0; i < fileSize; i++)
                    {
                        Assert.AreEqual(inputFileStream.ReadByte(), outputFileStream.ReadByte());
                    }
                }
            }
            finally
            {
                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        [TestMethod]
        [Description("Upload a file using a byte array")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromByteArrayAsync()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoUploadFromByteArrayTestAsync(file, 4 * 512, 0, 4 * 512);
            await this.DoUploadFromByteArrayTestAsync(file, 4 * 512, 0, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(file, 4 * 512, 1 * 512, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(file, 4 * 512, 2 * 512, 2 * 512);
        }

        private async Task DoUploadFromByteArrayTestAsync(CloudFile file, int bufferSize, int bufferOffset, int count)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);
            byte[] downloadedBuffer = new byte[bufferSize];
            int downloadLength;

            await file.UploadFromByteArrayAsync(buffer, bufferOffset, count);
            downloadLength = await file.DownloadToByteArrayAsync(downloadedBuffer, 0);

            Assert.AreEqual(count, downloadLength);

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(buffer[i + bufferOffset], downloadedBuffer[i]);
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadToByteArrayAsync()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoDownloadToByteArrayAsyncTest(file, 1 * 512, 2 * 512, 0, false);
            await this.DoDownloadToByteArrayAsyncTest(file, 1 * 512, 2 * 512, 1 * 512, false);
            await this.DoDownloadToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadToByteArrayAsyncOverload()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoDownloadToByteArrayAsyncTest(file, 1 * 512, 2 * 512, 0, true);
            await this.DoDownloadToByteArrayAsyncTest(file, 1 * 512, 2 * 512, 1 * 512, true);
            await this.DoDownloadToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, true);
        }

        private async Task DoDownloadToByteArrayAsyncTest(CloudFile file, int fileSize, int bufferSize, int bufferOffset, bool isOverload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(fileSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalFile = new MemoryStream(buffer))
            {
                if (!isOverload)
                {
                    await file.UploadFromStreamAsync(originalFile);
                    downloadLength = await file.DownloadToByteArrayAsync(resultBuffer, bufferOffset);
                }
                else
                {
                    await file.UploadFromStreamAsync(originalFile);
                    OperationContext context = new OperationContext();
                    downloadLength = await file.DownloadToByteArrayAsync(resultBuffer, bufferOffset, null, null, context);
                }

                int downloadSize = Math.Min(fileSize, bufferSize - bufferOffset);
                Assert.AreEqual(downloadSize, downloadLength);

                for (int i = 0; i < file.Properties.Length; i++)
                {
                    Assert.AreEqual(buffer[i], resultBuffer[bufferOffset + i]);
                }

                for (int j = 0; j < bufferOffset; j++)
                {
                    Assert.AreEqual(0, resultBuffer2[j]);
                }

                if (bufferOffset + fileSize < bufferSize)
                {
                    for (int k = bufferOffset + fileSize; k < bufferSize; k++)
                    {
                        Assert.AreEqual(0, resultBuffer2[k]);
                    }
                }
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadRangeToByteArrayAsync()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 1023, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 0, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 0, 0, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 0, 512, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 512, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadRangeToByteArrayAsyncOverload()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 1023, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 0, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 0, 0, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 0, 512, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 512, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(file, 1024, 1024, 512, 0, 512, true);
        }

        /// <summary>
        /// Single put file and get file.
        /// </summary>
        /// <param name="fileSize">The file size.</param>
        /// <param name="bufferSize">The output buffer size.</param>
        /// <param name="bufferOffset">The output buffer offset.</param>
        /// <param name="fileOffset">The file offset.</param>
        /// <param name="length">Length of the data range to download.</param>
        /// <param name="isOverload">True when the overloaded method for DownloadRangeToByteArrayAsync is called. False when the basic method is called.</param>
        private async Task DoDownloadRangeToByteArrayAsyncTest(CloudFile file, int fileSize, int bufferSize, int bufferOffset, long? fileOffset, long? length, bool isOverload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(fileSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalFile = new MemoryStream(buffer))
            {
                if (!isOverload)
                {
                    await file.UploadFromStreamAsync(originalFile);
                    downloadLength = await file.DownloadRangeToByteArrayAsync(resultBuffer, bufferOffset, fileOffset, length);
                }
                else
                {
                    await file.UploadFromStreamAsync(originalFile);
                    OperationContext context = new OperationContext();
                    downloadLength = await file.DownloadRangeToByteArrayAsync(resultBuffer, bufferOffset, fileOffset, length, null, null, context);
                }

                int downloadSize = Math.Min(fileSize - (int)(fileOffset.HasValue ? fileOffset.Value : 0), bufferSize - bufferOffset);
                if (length.HasValue && (length.Value < downloadSize))
                {
                    downloadSize = (int)length.Value;
                }

                Assert.AreEqual(downloadSize, downloadLength);

                for (int i = 0; i < bufferOffset; i++)
                {
                    Assert.AreEqual(0, resultBuffer[i]);
                }

                for (int j = 0; j < downloadLength; j++)
                {
                    Assert.AreEqual(buffer[(fileOffset.HasValue ? fileOffset.Value : 0) + j], resultBuffer[bufferOffset + j]);
                }

                for (int k = bufferOffset + downloadLength; k < bufferSize; k++)
                {
                    Assert.AreEqual(0, resultBuffer[k]);
                }
            }
        }

        #region Negative tests
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadRangeToByteArrayNegativeTestsAsync()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoDownloadRangeToByteArrayNegativeTestsAsync(file);
        }

        private async Task DoDownloadRangeToByteArrayNegativeTestsAsync(CloudFile file)
        {
            int fileLength = 1024;
            int resultBufSize = 1024;
            byte[] buffer = GetRandomBuffer(fileLength);
            byte[] resultBuffer = new byte[resultBufSize];

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                await file.UploadFromStreamAsync(stream);

                OperationContext context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(async () => await file.DownloadRangeToByteArrayAsync(resultBuffer, 0, 1024, 1, null, null, context), context, "Try invalid length", HttpStatusCode.RequestedRangeNotSatisfiable);
                StorageException ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await file.DownloadToByteArrayAsync(resultBuffer, 1024), "Provide invalid offset");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await file.DownloadRangeToByteArrayAsync(resultBuffer, 1023, 0, 2), "Should fail when offset + length required is greater than size of the buffer");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await file.DownloadRangeToByteArrayAsync(resultBuffer, 0, 0, -10), "Fail when a negative length is specified");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(async () => await file.DownloadRangeToByteArrayAsync(resultBuffer, -10, 0, 20), "Fail if a negative offset is provided");
                ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await file.DownloadRangeToByteArrayAsync(resultBuffer, 0, -10, 20), "Fail if a negative file offset is provided");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
            }
        }
        #endregion
    }
}