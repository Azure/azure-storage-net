﻿// -----------------------------------------------------------------------------------------
// <copyright file="BlobUploadDownloadTest.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#if !NETCORE
using Windows.Storage;
#endif

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class BlobUploadDownloadTest : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public BlobUploadDownloadTest()
        {
            TestInitialize();
        }
        public void Dispose()
        {
            TestCleanup();
        }
#endif

        private CloudBlobContainer testContainer;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testContainer = GetRandomContainerReference();
            this.testContainer.CreateIfNotExistsAsync().Wait();

            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
#if NETCORE
            this.testContainer.DeleteIfExistsAsync().Wait();
#else
            this.testContainer.DeleteIfExistsAsync().Wait();
#endif
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

#if NETCORE
        [TestMethod]
        [Description("Upload a stream to a block blob, with progress, sample code")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobUploadFromStreamTestAsyncWithProgress_Sample()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");

            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                #region sample_StorageProgress_NetCore
                CancellationToken cancellationToken = new CancellationToken();
                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                    progress => Console.WriteLine("Progress: {0} bytes transferred", progress.BytesTransferred)
                    );

                await blob.UploadFromStreamAsync(
                    srcStream,
                    default(AccessCondition),
                    default(BlobRequestOptions),
                    default(OperationContext),
                    progressHandler,
                    cancellationToken
                    );
                #endregion
            }
        }

        [TestMethod]
        [Description("Upload a stream to a block blob, with progress")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobUploadFromStreamTestAsyncWithProgress()
        {
            await DoBlobUploadFromStreamTestAsyncWithProgress(
               () => this.testContainer.GetBlockBlobReference("blob1"),
               (blob, stream, progressHandler, cancellationToken) =>
                   blob.UploadFromStreamAsync(stream, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), progressHandler, cancellationToken)
                   );
        }

        [TestMethod]
        [Description("Upload a stream to a page blob, with progress")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobUploadFromStreamTestAsyncWithProgress()
        {
            await DoBlobUploadFromStreamTestAsyncWithProgress(
               () => this.testContainer.GetPageBlobReference("blob1"),
               (blob, stream, progressHandler, cancellationToken) =>
                   blob.UploadFromStreamAsync(stream, null, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), progressHandler, cancellationToken),
               5000
                   );
        }

        [TestMethod]
        [Description("Upload a stream to an append blob, with progress")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobUploadFromStreamTestAsyncWithProgress()
        {
            await DoBlobUploadFromStreamTestAsyncWithProgress(
               () => this.testContainer.GetAppendBlobReference("blob1"),
               (blob, stream, progressHandler, cancellationToken) =>
                   blob.UploadFromStreamAsync(stream, null, null, null, progressHandler, cancellationToken)
                   );
        }

        private static async Task DoBlobUploadFromStreamTestAsyncWithProgress<T>(
            Func<T> blobFactory,
            Func<T, Stream, IProgress<StorageProgress>, CancellationToken, Task> uploadTask,
            int delay = 0
            )
            where T : ICloudBlob
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            T blob = blobFactory();
            List<StorageProgress> progressList = new List<StorageProgress>();

            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                CancellationToken cancellationToken = new CancellationToken();
                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(progress => progressList.Add(progress));

                await uploadTask(blob, srcStream, progressHandler, cancellationToken);

                await Task.Delay(delay);

                Assert.IsTrue(progressList.Count > 2, "Too few progress received");

                StorageProgress lastProgress = progressList.Last();

                Assert.AreEqual(srcStream.Length, srcStream.Position, "Final position has unexpected value");
                Assert.AreEqual(srcStream.Length, lastProgress.BytesTransferred, "Final progress has unexpected value");
            }
        }

        [TestMethod]
        [Description("Download a block blob to a stream, with progress")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobDownloadToStreamTestAsyncWithProgress()
        {
            await DoBlobDownloadToStreamTestAsyncWithProgress(
                () => this.testContainer.GetBlockBlobReference("blob1"),
                (blob, targetStream, progressHandler, cancellationToken) =>
                    blob.DownloadToStreamAsync(targetStream, null, null, null, progressHandler, cancellationToken)
                    );
        }

        [TestMethod]
        [Description("Download a page blob to a stream, with progress")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobDownloadToStreamTestAsyncWithProgress()
        {
            await DoBlobDownloadToStreamTestAsyncWithProgress(
                () => this.testContainer.GetPageBlobReference("blob1"),
                (blob, targetStream, progressHandler, cancellationToken) =>
                    blob.DownloadToStreamAsync(targetStream, null, null, null, progressHandler, cancellationToken)
                    );
        }

        [TestMethod]
        [Description("Download an append blob to a stream, with progress")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobDownloadToStreamTestAsyncWithProgress()
        {
            await DoBlobDownloadToStreamTestAsyncWithProgress(
                () => this.testContainer.GetAppendBlobReference("blob1"),
                (blob, targetStream, progressHandler, cancellationToken) =>
                    blob.DownloadToStreamAsync(targetStream, null, null, null, progressHandler, cancellationToken)
                    );
        }

        private static async Task DoBlobDownloadToStreamTestAsyncWithProgress<T>(
            Func<T> blobFactory,
            Func<T, Stream, IProgress<StorageProgress>, CancellationToken, Task> downloadTask
            )
            where T : ICloudBlob
        {
            byte[] uploadBuffer = GetRandomBuffer(20 * 1024 * 1024);

            T uploadBlob = blobFactory();

            using (MemoryStream srcStream = new MemoryStream(uploadBuffer))
            {
                await uploadBlob.UploadFromStreamAsync(srcStream);
            }

            T downloadBlob = blobFactory();
            List<StorageProgress> progressList = new List<StorageProgress>();

            using (MemoryStream targetStream = new MemoryStream())
            {
                CancellationToken cancellationToken = new CancellationToken();
                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(progress => progressList.Add(progress));

                await downloadTask(downloadBlob, targetStream, progressHandler, cancellationToken);

                Assert.IsTrue(progressList.Count > 2, "Too few progress received");

                StorageProgress lastProgress = progressList.Last();

                Assert.AreEqual(targetStream.Length, lastProgress.BytesTransferred, "Final progress has unexpected value");
            }
        }

        [TestMethod]
        [Description("Test blob upload using parallel multi-filestream upload strategy.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobTestParallelUploadFromFileStreamWithProgress()
        {
            CloudBlobContainer container = this.testContainer;
            string inputFileName = "i_" + Path.GetRandomFileName();
            string outputFileName = "o_" + Path.GetRandomFileName();
            int bufferSize_25 = 25 * 1024 * 1024;
            int writeSize_5 = 5 * 1024 * 1024;
            int writeSize_6_plus_1 = 6 * 1024 * 1024 + 1;
            int delay = 5000;

            BlobRequestOptions options = new BlobRequestOptions()
            {
                StoreBlobContentMD5 = false,
                ParallelOperationThreadCount = 1
            };

            byte[] buffer = GetRandomBuffer(bufferSize_25);

            using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(buffer, 0, buffer.Length);
            }

            CloudBlockBlob blob1 = container.GetBlockBlobReference("blob1");
            CloudBlockBlob blob2 = container.GetBlockBlobReference("blob2");
            CloudBlockBlob blob3 = container.GetBlockBlobReference("blob3");
            CloudBlockBlob blob4 = container.GetBlockBlobReference("blob4");

            List<StorageProgress> uploadProgressList = new List<StorageProgress>();
            IProgress<StorageProgress> uploadProgressHandler = new Progress<StorageProgress>(progress => uploadProgressList.Add(progress));

            List<StorageProgress> downloadProgressList = new List<StorageProgress>();
            IProgress<StorageProgress> downloadProgressHandler = new Progress<StorageProgress>(progress => downloadProgressList.Add(progress));

            StorageProgress lastUploadProgress;
            StorageProgress lastDownloadProgress;

            blob1.StreamWriteSizeInBytes = writeSize_5;
            await blob1.UploadFromFileAsync(inputFileName, default(AccessCondition), options, default(OperationContext), uploadProgressHandler, CancellationToken.None);
            await blob1.DownloadToFileAsync(outputFileName, FileMode.Create, default(AccessCondition), options, default(OperationContext), downloadProgressHandler, CancellationToken.None);

            await Task.Delay(delay);

            lastUploadProgress = uploadProgressList.Last();
            lastDownloadProgress = downloadProgressList.Last();

            Assert.AreEqual(lastUploadProgress.BytesTransferred, lastDownloadProgress.BytesTransferred);
            Assert.AreEqual(buffer.Length, lastUploadProgress.BytesTransferred);

            uploadProgressList.Clear();
            downloadProgressList.Clear();

            using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                 outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
            {
                TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
            }

            blob2.StreamWriteSizeInBytes = writeSize_5;
            options.ParallelOperationThreadCount = 4;
            await blob2.UploadFromFileAsync(inputFileName, default(AccessCondition), options, default(OperationContext), uploadProgressHandler, CancellationToken.None);
            await blob2.DownloadToFileAsync(outputFileName, FileMode.Create, default(AccessCondition), options, default(OperationContext), downloadProgressHandler, CancellationToken.None);

            await Task.Delay(delay);

            lastUploadProgress = uploadProgressList.Last();
            lastDownloadProgress = downloadProgressList.Last();

            Assert.AreEqual(lastUploadProgress.BytesTransferred, lastDownloadProgress.BytesTransferred);
            Assert.AreEqual(buffer.Length, lastUploadProgress.BytesTransferred);

            uploadProgressList.Clear();
            downloadProgressList.Clear();

            using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                 outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
            {
                TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
            }

            blob3.StreamWriteSizeInBytes = writeSize_6_plus_1;
            options.ParallelOperationThreadCount = 1;
            await blob3.UploadFromFileAsync(inputFileName, default(AccessCondition), options, default(OperationContext), uploadProgressHandler, CancellationToken.None);
            await blob3.DownloadToFileAsync(outputFileName, FileMode.Create, default(AccessCondition), options, default(OperationContext), downloadProgressHandler, CancellationToken.None);

            await Task.Delay(delay);

            lastUploadProgress = uploadProgressList.Last();
            lastDownloadProgress = downloadProgressList.Last();

            Assert.AreEqual(lastUploadProgress.BytesTransferred, lastDownloadProgress.BytesTransferred);
            Assert.AreEqual(buffer.Length, lastUploadProgress.BytesTransferred);

            uploadProgressList.Clear();
            downloadProgressList.Clear();

            using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                 outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
            {
                TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
            }

            blob4.StreamWriteSizeInBytes = writeSize_6_plus_1;
            options.ParallelOperationThreadCount = 3;
            await blob4.UploadFromFileAsync(inputFileName, default(AccessCondition), options, default(OperationContext), uploadProgressHandler, CancellationToken.None);
            await blob4.DownloadToFileAsync(outputFileName, FileMode.Create, default(AccessCondition), options, default(OperationContext), downloadProgressHandler, CancellationToken.None);

            await Task.Delay(delay);

            lastUploadProgress = uploadProgressList.Last();
            lastDownloadProgress = downloadProgressList.Last();

            Assert.AreEqual(lastUploadProgress.BytesTransferred, lastDownloadProgress.BytesTransferred);
            Assert.AreEqual(buffer.Length, lastUploadProgress.BytesTransferred);

            uploadProgressList.Clear();
            downloadProgressList.Clear();

            using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
            {
                TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
            }
        }
#endif

        [TestMethod]
        [Description("Download a specific range of the blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobDownloadToStreamRangeTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            using (MemoryStream wholeBlob = new MemoryStream(buffer))
            {
                await blob.UploadFromStreamAsync(wholeBlob);

                byte[] testBuffer = new byte[1024];
                MemoryStream blobStream = new MemoryStream(testBuffer);

                Exception ex = await TestHelper.ExpectedExceptionAsync<Exception>(
                    async () => await blob.DownloadRangeToStreamAsync(blobStream, 0, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                await blob.DownloadRangeToStreamAsync(blobStream, 0, 1024);

                Assert.AreEqual(blobStream.Position, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(blobStream, wholeBlob, 0, 0, 1024);

                CloudPageBlob blob2 = this.testContainer.GetPageBlobReference("blob1");
                MemoryStream blobStream2 = new MemoryStream(testBuffer);

                ex = await TestHelper.ExpectedExceptionAsync<Exception>(
                    async () => await blob2.DownloadRangeToStreamAsync(blobStream, 1024, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                await blob2.DownloadRangeToStreamAsync(blobStream2, 1024, 1024);

                TestHelper.AssertStreamsAreEqualAtIndex(blobStream2, wholeBlob, 0, 1024, 1024);

                AssertAreEqual(blob, blob2);
            }
        }

        [TestMethod]
        [Description("Upload a stream to a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobUploadFromStreamTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                await blob.UploadFromStreamAsync(srcStream);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                await blob.DownloadRangeToStreamAsync(dstStream, null, null);
                TestHelper.AssertStreamsAreEqual(srcStream, dstStream);
            }
        }

        [TestMethod]
        [Description("Upload from text to a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobUploadWithoutMD5ValidationAndStoreBlobContentTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            BlobRequestOptions options = new BlobRequestOptions();
            options.DisableContentMD5Validation = false;
            options.StoreBlobContentMD5 = false;
            OperationContext context = new OperationContext();
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                await blob.UploadFromStreamAsync(srcStream, null, options, context);
                await blob.FetchAttributesAsync();
                string md5 = blob.Properties.ContentMD5;
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                await blob.SetPropertiesAsync(null, options, context);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                await TestHelper.ExpectedExceptionAsync(async () => await blob.DownloadRangeToStreamAsync(dstStream, null, null, null, options, context),
                    context,
                    "Try to Download a stream with a corrupted md5 and DisableMD5Validation set to false",
                    HttpStatusCode.OK);

                options.DisableContentMD5Validation = true;
                await blob.SetPropertiesAsync(null, options, context);
                byte[] testBuffer2 = new byte[2048];
                MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                await blob.DownloadRangeToStreamAsync(dstStream2, null, null, null, options, context);
            }
        }
#if !FACADE_NETCORE
        [TestMethod]
        [Description("Upload from file to a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobUploadDownloadFileAsync()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoUploadDownloadFileAsync(blob, 0);
            await this.DoUploadDownloadFileAsync(blob, 4096);
            await this.DoUploadDownloadFileAsync(blob, 4097);
           
            CloudBlockBlob blob2 = this.testContainer.GetBlockBlobReference("blob2");
            await this.DoLargeBlockUploadDownloadFileAsync(blob2, 10 * 1024 * 1024, 4 * 1024 * 1024 + 1);
            await this.DoLargeBlockUploadDownloadFileAsync(blob2, 10 * 1024 * 1024, 5 * 1024 * 1024);
            await this.DoLargeBlockUploadDownloadFileAsync(blob2, 10 * 1024 * 1024, 10 * 1024 * 1024);
        }

        [TestMethod]
        [Description("Upload from file to a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadDownloadFileAsync()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoUploadDownloadFileAsync(blob, 0);
            await this.DoUploadDownloadFileAsync(blob, 4096);

            await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                async () => await this.DoUploadDownloadFileAsync(blob, 4097),
                "Page blobs must be 512-byte aligned");
        }

        [TestMethod]
        [Description("Upload from file to a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobUploadDownloadFileAsync()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            await this.DoUploadDownloadFileAsync(blob, 0);
            await this.DoUploadDownloadFileAsync(blob, 4096);
            await this.DoUploadDownloadFileAsync(blob, 4097);
        }

        private async Task DoUploadDownloadFileAsync(ICloudBlob blob, int fileSize)
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
                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    await file.WriteAsync(buffer, 0, buffer.Length);
                }

                await blob.UploadFromFileAsync(inputFileName);

                OperationContext context = new OperationContext();
                await blob.UploadFromFileAsync(inputFileName, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                context = new OperationContext();
                await blob.DownloadToFileAsync(outputFileName, FileMode.CreateNew, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (FileStream inputFile = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                    outputFile = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFile, outputFile);
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
                using (Stream file = await inputFile.OpenStreamForWriteAsync())
                {
                    await file.WriteAsync(buffer, 0, buffer.Length);
                }

                OperationContext context = new OperationContext();

                await blob.UploadFromFileAsync(inputFile);
                await blob.UploadFromFileAsync(inputFile, null, null, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                context = new OperationContext();
                await blob.DownloadToFileAsync(outputFile, null, null, context);
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
#endif //!FACADE_NETCORE

#if NETCORE && !FACADE_NETCORE
        [TestMethod]
        [Description("Test upload using multi-filestream upload strategy.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobTestParallelUploadFromFileStream()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            string inputFileName = "i_" + Path.GetRandomFileName();
            string outputFileName = "o_" + Path.GetRandomFileName();

            try
            {                
                await container.CreateAsync();

                BlobRequestOptions options = new BlobRequestOptions()
                {
                    UseTransactionalMD5 = false,
                    StoreBlobContentMD5 = false,
                    ParallelOperationThreadCount = 1
                };

                byte[] buffer = GetRandomBuffer(10 * 1024 * 1024);
 
                using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buffer, 0, buffer.Length);
                }

                CloudBlockBlob blob1 = container.GetBlockBlobReference("blob1");
                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob2");
                CloudBlockBlob blob3 = container.GetBlockBlobReference("blob3");
                CloudBlockBlob blob4 = container.GetBlockBlobReference("blob4");

                blob1.StreamWriteSizeInBytes = 5 * 1024 * 1024;
                await blob1.UploadFromFileAsync(inputFileName, null, options, null);
                OperationContext context = new OperationContext();
                await blob1.DownloadToFileAsync(outputFileName, FileMode.CreateNew, null, options, context);
               
                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }
                
                blob2.StreamWriteSizeInBytes = 5 * 1024 * 1024;
                options.ParallelOperationThreadCount = 4;
                await blob2.UploadFromFileAsync(inputFileName, null, options, null);
                await blob2.DownloadToFileAsync(outputFileName, FileMode.Create, null, options, null);

                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }

                blob3.StreamWriteSizeInBytes = 6 * 1024 * 1024 + 1;
                options.ParallelOperationThreadCount = 1;
                await blob3.UploadFromFileAsync(inputFileName, null, options, null);
                await blob3.DownloadToFileAsync(outputFileName, FileMode.Create, null, options, null);

                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }

                blob4.StreamWriteSizeInBytes = 6 * 1024 * 1024 + 1;
                options.ParallelOperationThreadCount = 3;
                await blob4.UploadFromFileAsync(inputFileName, null, options, null);
                await blob4.DownloadToFileAsync(outputFileName, FileMode.Create, null, options, null);

                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }    
            }
            finally
            {
                System.IO.File.Delete(inputFileName);
                System.IO.File.Delete(outputFileName);
            }
        }
#endif
#if !FACADE_NETCORE
        private async Task DoLargeBlockUploadDownloadFileAsync(ICloudBlob blob, int fileSize, int blockSize)
        {
#if NETCORE
            string inputFileName = Path.GetRandomFileName();
            string outputFileName = Path.GetRandomFileName();

            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                BlobRequestOptions options = new BlobRequestOptions()
                {
                    StoreBlobContentMD5 = false,
                    ParallelOperationThreadCount = 2
                };
            
                blob.StreamWriteSizeInBytes = blockSize;

                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    await file.WriteAsync(buffer, 0, buffer.Length);
                }

                OperationContext context = new OperationContext();
                await blob.UploadFromFileAsync(inputFileName, null, options, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                context = new OperationContext();
                await blob.DownloadToFileAsync(outputFileName, FileMode.Create, null, options, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (FileStream inputFile = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                    outputFile = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFile, outputFile);
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
                using (Stream file = await inputFile.OpenStreamForWriteAsync())
                {
                    await file.WriteAsync(buffer, 0, buffer.Length);
                }
                
                BlobRequestOptions options = new BlobRequestOptions()
                {
                    StoreBlobContentMD5 = false,
                    ParallelOperationThreadCount = 2
                };

                blob.StreamWriteSizeInBytes = blockSize;
                OperationContext context = new OperationContext();

                await blob.UploadFromFileAsync(inputFile);
                await blob.UploadFromFileAsync(inputFile, null, options, context);
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                context = new OperationContext();
                await blob.DownloadToFileAsync(outputFile, null, options, context);
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
#endif
        [TestMethod]
        [Description("Upload a blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobUploadFromByteArrayAsync()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 0, 4 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 0, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 1 * 512, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 2 * 512, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 512, 0, 511);
        }

        [TestMethod]
        [Description("Upload a blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromByteArrayAsync()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 0, 4 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 0, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 1 * 512, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 2 * 512, 2 * 512);

            await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                async () => await this.DoUploadFromByteArrayTestAsync(blob, 512, 0, 511),
                "Page blobs must be 512-byte aligned");
        }

        [TestMethod]
        [Description("Upload a blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobUploadFromByteArrayAsync()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 0, 4 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 0, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 1 * 512, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 4 * 512, 2 * 512, 2 * 512);
            await this.DoUploadFromByteArrayTestAsync(blob, 512, 0, 511);
        }

        private async Task DoUploadFromByteArrayTestAsync(ICloudBlob blob, int bufferSize, int bufferOffset, int count)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);
            byte[] downloadedBuffer = new byte[bufferSize];
            int downloadLength;

            await blob.UploadFromByteArrayAsync(buffer, bufferOffset, count);

            downloadLength = await blob.DownloadToByteArrayAsync(downloadedBuffer, 0);

            Assert.AreEqual(count, downloadLength);

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(buffer[i + bufferOffset], downloadedBuffer[i]);
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobDownloadToByteArrayAsync()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 0, false);
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 1 * 512, false);
            await this.DoDownloadToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobDownloadToByteArrayAsyncOverload()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 0, true);
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 1 * 512, true);
            await this.DoDownloadToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, true);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDownloadToByteArrayAsync()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 0, false);
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 1 * 512, false);
            await this.DoDownloadToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDownloadToByteArrayAsyncOverload()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 0, true);
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 1 * 512, true);
            await this.DoDownloadToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, true);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobDownloadToByteArrayAsync()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 0, false);
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 1 * 512, false);
            await this.DoDownloadToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobDownloadToByteArrayAsyncOverload()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 0, true);
            await this.DoDownloadToByteArrayAsyncTest(blob, 1 * 512, 2 * 512, 1 * 512, true);
            await this.DoDownloadToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, true);
        }

        private async Task DoDownloadToByteArrayAsyncTest(ICloudBlob blob, int blobSize, int bufferSize, int bufferOffset, bool isOverload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(blobSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalBlob = new MemoryStream(buffer))
            {
                await blob.UploadFromStreamAsync(originalBlob);
            }

            if (!isOverload)
            {
                downloadLength = await blob.DownloadToByteArrayAsync(resultBuffer, bufferOffset);
            }
            else
            {
                OperationContext context = new OperationContext();
                downloadLength = await blob.DownloadToByteArrayAsync(resultBuffer, bufferOffset, null, null, context);
            }

            int downloadSize = Math.Min(blobSize, bufferSize - bufferOffset);
            Assert.AreEqual(downloadSize, downloadLength);

            for (int i = 0; i < blob.Properties.Length; i++)
            {
                Assert.AreEqual(buffer[i], resultBuffer[bufferOffset + i]);
            }

            for (int j = 0; j < bufferOffset; j++)
            {
                Assert.AreEqual(0, resultBuffer2[j]);
            }

            if (bufferOffset + blobSize < bufferSize)
            {
                for (int k = bufferOffset + blobSize; k < bufferSize; k++)
                {
                    Assert.AreEqual(0, resultBuffer2[k]);
                }
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobDownloadRangeToByteArrayAsync()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 1023, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 0, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 512, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobDownloadRangeToByteArrayAsyncOverload()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 1023, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 0, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 512, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 0, 512, true);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDownloadRangeToByteArrayAsync()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 1023, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 0, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 512, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDownloadRangeToByteArrayAsyncOverload()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 1023, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 0, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 512, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 0, 512, true);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobDownloadRangeToByteArrayAsync()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 1023, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 0, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 512, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 1023, 1, false);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobDownloadRangeToByteArrayAsyncOverload()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 1023, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 0, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 0, 512, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 1023, 1, true);
            await this.DoDownloadRangeToByteArrayAsyncTest(blob, 1024, 1024, 512, 0, 512, true);
        }

        /// <summary>
        /// Single put blob and get blob.
        /// </summary>
        /// <param name="blobSize">The blob size.</param>
        /// <param name="bufferSize">The output buffer size.</param>
        /// <param name="bufferOffset">The output buffer offset.</param>
        /// <param name="blobOffset">The blob offset.</param>
        /// <param name="length">Length of the data range to download.</param>
        /// <param name="isOverload">True when the overloaded method for DownloadRangeToByteArrayAsync is called. False when the basic method is called.</param>
        private async Task DoDownloadRangeToByteArrayAsyncTest(ICloudBlob blob, int blobSize, int bufferSize, int bufferOffset, long? blobOffset, long? length, bool isOverload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(blobSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalBlob = new MemoryStream(buffer))
            {
                await blob.UploadFromStreamAsync(originalBlob);
            }

            if (!isOverload)
            {
                downloadLength = await blob.DownloadRangeToByteArrayAsync(resultBuffer, bufferOffset, blobOffset, length);
            }
            else
            {
                OperationContext context = new OperationContext();
                downloadLength = await blob.DownloadRangeToByteArrayAsync(resultBuffer, bufferOffset, blobOffset, length, null, null, context);
            }

            int downloadSize = Math.Min(blobSize - (int)(blobOffset.HasValue ? blobOffset.Value : 0), bufferSize - bufferOffset);
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
                Assert.AreEqual(buffer[(blobOffset.HasValue ? blobOffset.Value : 0) + j], resultBuffer[bufferOffset + j]);
            }

            for (int k = bufferOffset + downloadLength; k < bufferSize; k++)
            {
                Assert.AreEqual(0, resultBuffer[k]);
            }
        }

#if NETCORE && !FACADE_NETCORE
        [TestMethod]
        [Description("Upload from file to a block blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobUploadDownloadFileWithFailures()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            CloudBlockBlob nullBlob = this.testContainer.GetBlockBlobReference("null");
            await this.DoUploadDownloadFileAsync(blob, 0);
            await this.DoUploadDownloadFileAsync(blob, 4096);
            await this.DoUploadDownloadFileAsync(blob, 4097);

            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await blob.UploadFromFileAsync("non_existentCloudBlockBlobUploadDownloadFileWithFailures.file"),
                "UploadFromFileAsync requires an existing file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudBlockBlobUploadDownloadFileWithFailures.file", FileMode.Create),
                "DownloadToFileAsync should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudBlockBlobUploadDownloadFileWithFailures.file"));

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudBlockBlobUploadDownloadFileWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudBlockBlobUploadDownloadFileWithFailures.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream file = new FileStream("garbageCloudBlockBlobUploadDownloadFileWithFailures.file", FileMode.Create, FileAccess.Write))
            {
                await file.WriteAsync(buffer, 0, buffer.Length);
            }
            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudBlockBlobUploadDownloadFileWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an empty file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudBlockBlobUploadDownloadFileWithFailures.file"));
            System.IO.File.Delete("garbageCloudBlockBlobUploadDownloadFileWithFailures.file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudBlockBlobUploadDownloadFileWithFailures.file", FileMode.Append),
                "DownloadToFileAsync should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudBlockBlobUploadDownloadFileWithFailures.file"));
            System.IO.File.Delete("garbageCloudBlockBlobUploadDownloadFileWithFailures.file");
        }

        [TestMethod]
        [Description("Upload from file to a page blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadDownloadFileWithFailures()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            CloudPageBlob nullBlob = this.testContainer.GetPageBlobReference("null");
            await this.DoUploadDownloadFileAsync(blob, 0);
            await this.DoUploadDownloadFileAsync(blob, 4096);

            await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                async () => await this.DoUploadDownloadFileAsync(blob, 4097),
                "Page blobs must be 512-byte aligned");

            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await blob.UploadFromFileAsync("non_existentCloudPageBlobUploadDownloadFileWithFailures.file"),
                "UploadFromFileAsync requires an existing file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudPageBlobUploadDownloadFileWithFailures.file", FileMode.Create),
                "DownloadToFileAsync should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudPageBlobUploadDownloadFileWithFailures.file"));

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudPageBlobUploadDownloadFileWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudPageBlobUploadDownloadFileWithFailures.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream file = new FileStream("garbageCloudPageBlobUploadDownloadFileWithFailures.file", FileMode.Create, FileAccess.Write))
            {
                await file.WriteAsync(buffer, 0, buffer.Length);
            }
            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudPageBlobUploadDownloadFileWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an empty file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudPageBlobUploadDownloadFileWithFailures.file"));
            System.IO.File.Delete("garbageCloudPageBlobUploadDownloadFileWithFailures.file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudPageBlobUploadDownloadFileWithFailures.file", FileMode.OpenOrCreate),
                "DownloadToFileAsync should leave an empty file behind after failing, depending on file mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudPageBlobUploadDownloadFileWithFailures.file"));
            System.IO.File.Delete("garbageCloudPageBlobUploadDownloadFileWithFailures.file");
        }

        [TestMethod]
        [Description("Upload from file to an append blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobUploadDownloadFileWithFailures()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            CloudAppendBlob nullBlob = this.testContainer.GetAppendBlobReference("null");
            await this.DoUploadDownloadFileAsync(blob, 0);
            await this.DoUploadDownloadFileAsync(blob, 4096);
            await this.DoUploadDownloadFileAsync(blob, 4097);

            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await blob.UploadFromFileAsync("non_existentCloudAppendBlobUploadDownloadFileWithFailures.file"),
                "UploadFromFileAsync requires an existing file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudAppendBlobUploadDownloadFileWithFailures.file", FileMode.Create),
                "DownloadToFileAsync should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudAppendBlobUploadDownloadFileWithFailures.file"));

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudAppendBlobUploadDownloadFileWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should not leave an empty file behind after failing.");
            Assert.IsFalse(System.IO.File.Exists("garbageCloudAppendBlobUploadDownloadFileWithFailures.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream file = new FileStream("garbageCloudAppendBlobUploadDownloadFileWithFailures.file", FileMode.Create, FileAccess.Write))
            {
                await file.WriteAsync(buffer, 0, buffer.Length);
            }
            await TestHelper.ExpectedExceptionAsync<IOException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudAppendBlobUploadDownloadFileWithFailures.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an empty file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudAppendBlobUploadDownloadFileWithFailures.file"));
            System.IO.File.Delete("garbageCloudAppendBlobUploadDownloadFileWithFailures.file");

            await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await nullBlob.DownloadToFileAsync("garbageCloudAppendBlobUploadDownloadFileWithFailures.file", FileMode.Append),
                "DownloadToFileAsync should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(System.IO.File.Exists("garbageCloudAppendBlobUploadDownloadFileWithFailures.file"));
            System.IO.File.Delete("garbageCloudAppendBlobUploadDownloadFileWithFailures.file");
        }
#endif

#region Negative tests
        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobDownloadRangeToByteArrayNegativeTestsAsync()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayNegativeTestsAsync(blob);
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDownloadRangeToByteArrayNegativeTestsAsync()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            await this.DoDownloadRangeToByteArrayNegativeTestsAsync(blob);
        }

        private async Task DoDownloadRangeToByteArrayNegativeTestsAsync(ICloudBlob blob)
        {
            int blobLength = 1024;
            int resultBufSize = 1024;
            byte[] buffer = GetRandomBuffer(blobLength);
            byte[] resultBuffer = new byte[resultBufSize];

            using (MemoryStream stream = new MemoryStream(buffer))
            {

                await blob.UploadFromStreamAsync(stream);

                OperationContext context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(async () => await blob.DownloadRangeToByteArrayAsync(resultBuffer, 0, 1024, 1, null, null, context), context, "Try invalid length", HttpStatusCode.RequestedRangeNotSatisfiable);
                StorageException ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await blob.DownloadToByteArrayAsync(resultBuffer, 1024), "Provide invalid offset");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await blob.DownloadRangeToByteArrayAsync(resultBuffer, 1023, 0, 2), "Should fail when offset + length required is greater than size of the buffer");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await blob.DownloadRangeToByteArrayAsync(resultBuffer, 0, 0, -10), "Fail when a negative length is specified");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(async () => await blob.DownloadRangeToByteArrayAsync(resultBuffer, -10, 0, 20), "Fail if a negative offset is provided");
                ex = await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await blob.DownloadRangeToByteArrayAsync(resultBuffer, 0, -10, 20), "Fail if a negative blob offset is provided");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
            }
        }
        #endregion
    }
}