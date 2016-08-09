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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

#if !NETCORE
using Windows.Storage;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileUploadDownloadTest : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public FileUploadDownloadTest()
        {
            TestInitialize();
        }
        public void Dispose()
        {
            TestCleanup();
        }
#endif
        private CloudFileShare testShare;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testShare = GetRandomShareReference();
            this.testShare.CreateIfNotExistsAsync().Wait();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testShare.DeleteIfExistsAsync().Wait();
        }

        [TestMethod]
        //[Description("Download a specific range of the file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileDownloadToStreamRangeTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
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

                    CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
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
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        //[Description("Upload a stream to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileUploadFromStreamTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(srcStream);
                    byte[] testBuffer = new byte[2048];
                    MemoryStream dstStream = new MemoryStream(testBuffer);
                    await file.DownloadRangeToStreamAsync(dstStream, null, null);
                    TestHelper.AssertStreamsAreEqual(srcStream, dstStream);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        //[Description("Upload from text to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileUploadWithoutMD5ValidationAndStoreFileContentTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                FileRequestOptions options = new FileRequestOptions();
                options.DisableContentMD5Validation = false;
                options.StoreFileContentMD5 = false;
                OperationContext context = new OperationContext();
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(srcStream, null, options, context);
                    await file.FetchAttributesAsync();
                    string md5 = file.Properties.ContentMD5;
                    file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    await file.SetPropertiesAsync(null, options, context);
                    byte[] testBuffer = new byte[2048];
                    MemoryStream dstStream = new MemoryStream(testBuffer);
                    await TestHelper.ExpectedExceptionAsync(async () => await file.DownloadRangeToStreamAsync(dstStream, null, null, null, options, context),
                        context,
                        "Try to Download a stream with a corrupted md5 and DisableMD5Validation set to false",
                        HttpStatusCode.OK);

                    options.DisableContentMD5Validation = true;
                    await file.SetPropertiesAsync(null, options, context);
                    byte[] testBuffer2 = new byte[2048];
                    MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                    await file.DownloadRangeToStreamAsync(dstStream2, null, null, null, options, context);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        /// [Description("Upload from file to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadDownloadFileAsync()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            await this.DoUploadDownloadFileAsync(file, 0);
            await this.DoUploadDownloadFileAsync(file, 4096);
            await this.DoUploadDownloadFileAsync(file, 4097);
        }

        private async Task DoUploadDownloadFileAsync(CloudFile file, int fileSize)
        {
#if NETCORE
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();
            if (System.IO.File.Exists(outputFileName))
            {
                System.IO.File.Delete(outputFileName);
            }
            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (FileStream localFile = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    await localFile.WriteAsync(buffer, 0, buffer.Length);
                }

                await file.UploadFromFileAsync(inputFileName);

                OperationContext context = new OperationContext();
                await file.UploadFromFileAsync(inputFileName, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                context = new OperationContext();
                await file.DownloadToFileAsync(outputFileName, FileMode.CreateNew, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (FileStream inputFile = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                    outputFile = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqual(inputFile, outputFile);
                }
            }
            finally
            {
                System.IO.File.Delete(inputFileName);
                System.IO.File.Delete(outputFileName);
            }
#else
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile inputFile = await tempFolder.CreateFileAsync("input.file", CreationCollisionOption.GenerateUniqueName);
            StorageFile outputFile = await tempFolder.CreateFileAsync("output.file", CreationCollisionOption.GenerateUniqueName);
            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (Stream localFile = await inputFile.OpenStreamForWriteAsync())
                {
                    await localFile.WriteAsync(buffer, 0, buffer.Length);
                }

                await file.UploadFromFileAsync(inputFile);

                OperationContext context = new OperationContext();
                await file.UploadFromFileAsync(inputFile, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                context = new OperationContext();
                await file.DownloadToFileAsync(outputFile, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (Stream inputFileStream = await inputFile.OpenStreamForReadAsync(),
                    outputFileStream = await outputFile.OpenStreamForReadAsync())
                {
                    TestHelper.AssertStreamsAreEqual(inputFileStream, outputFileStream);
                }
            }
            finally
            {
                inputFile.DeleteAsync().AsTask().Wait();
                outputFile.DeleteAsync().AsTask().Wait();
            }
#endif
        }

        [TestMethod]
        /// [Description("Upload a file using a byte array")]
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
            await this.DoUploadFromByteArrayTestAsync(file, 512, 0, 511);
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
        /// [Description("Single put file and get file")]
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
        /// [Description("Single put file and get file")]
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
        /// [Description("Single put file and get file")]
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
        /// [Description("Single put file and get file")]
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

#if NETCORE
        [TestMethod]
        [Description("Upload from file to a file with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadDownloadFileAsyncWithFailures()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            CloudFile nullFile = this.testShare.GetRootDirectoryReference().GetFileReference("null");
            await this.DoUploadDownloadFileAsync(file, 0);
            await this.DoUploadDownloadFileAsync(file, 4096);

            await TestHelper.ExpectedExceptionAsync<IOException>(
                async ()  => await file.UploadFromFileAsync("non_existentCloudFileUploadDownloadFileAsyncWithFailures.file"),
                "UploadFromFile requires an existing file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullFile.DownloadToFileAsync("garbageCloudFileUploadDownloadFileAsyncWithFailures.file", FileMode.Create),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudFileUploadDownloadFileAsyncWithFailures.file"));

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullFile.DownloadToFileAsync("garbageCloudFileUploadDownloadFileAsyncWithFailures.file", FileMode.CreateNew),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudFileUploadDownloadFileAsyncWithFailures.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream systemFile = new FileStream("garbageCloudFileUploadDownloadFileAsyncWithFailures.file", FileMode.Create, FileAccess.Write))
            {
                systemFile.Write(buffer, 0, buffer.Length);
            }
            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await nullFile.DownloadToFileAsync("garbageCloudFileUploadDownloadFileAsyncWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an empty file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudFileUploadDownloadFileAsyncWithFailures.file"));
            System.IO.File.Delete("garbageCloudFileUploadDownloadFileAsyncWithFailures.file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                 async () => await nullFile.DownloadToFileAsync("garbageCloudFileUploadDownloadFileAsyncWithFailures.file", FileMode.Append),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudFileUploadDownloadFileAsyncWithFailures.file"));
            System.IO.File.Delete("garbageCloudFileUploadDownloadFileAsyncWithFailures.file");
        }
#endif

        #region Negative tests
        [TestMethod]
        // [Description("Single put file and get file")]
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