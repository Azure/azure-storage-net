// -----------------------------------------------------------------------------------------
// <copyright file="BlobDownloadTests.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
#if !WINDOWS_PHONE && !WINDOWS_RT && !WINDOWS_PHONE_RT
#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class BlobDownloadToFileParallelTests : BlobTestBase
    {
        [TestMethod]
        [Description("Test downloading a large blob to a file")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task LargeDownloadToFileTest()
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("largeblob1");

                long bufferSize = 2 * Constants.KB;
                long offSet = 0;
                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    while (offSet < 500 * Constants.MB)
                    {
                        byte[] buffer = GetRandomBuffer(bufferSize);
                        await file.WriteAsync(buffer, 0, buffer.Length);
                        offSet += bufferSize;
                    }
                }

                BlobRequestOptions options = new BlobRequestOptions();
                options.ParallelOperationThreadCount = 16;
                await blob.UploadFromFileAsync(inputFileName, null, options, null);

                #region sample_DownloadToFileParallel

                // When calling the DownloadToFileParallelAsync API,
                // the parallelIOCount variable represents how many ranges can be downloaded concurrently. If the
                // parallel I/O count reaches this threshold, no more further requests are made until one range completes.
                // The rangeSizeInBytes represents the size of each individual range that is being dowloaded in parallel.
                // Passing a cancellation token is advised since for certain network errors, this code will continue to retry indefintitely.
                int parallelIOCount = 16;
                long rangeSizeInBytes = 16*Constants.MB;
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                await blob.DownloadToFileParallelAsync(outputFileName, FileMode.Create, parallelIOCount, rangeSizeInBytes, cancellationTokenSource.Token);
                #endregion

                await blob.DeleteAsync();

                Assert.IsTrue(FilesAreEqual(inputFileName, outputFileName));
            }
            finally
            {
                container.DeleteIfExists();
                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        [TestMethod]
        [Description("Test downloading a range of a large blob to a file")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task LargeDownloadRangeToFileTest()
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("largeblob1");

                long bufferSize = 2 * Constants.KB;
                long offSet = 0;
                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    while (offSet < 500 * Constants.MB)
                    {
                        byte[] buffer = GetRandomBuffer(bufferSize);
                        await file.WriteAsync(buffer, 0, buffer.Length);
                        offSet += bufferSize;
                    }
                }

                BlobRequestOptions options = new BlobRequestOptions();
                options.ParallelOperationThreadCount = 16;
                await blob.UploadFromFileAsync(inputFileName, null, options, null);

                long blobOffset = 3 * Constants.KB;
                await blob.DownloadToFileParallelAsync(
                    outputFileName,
                    FileMode.Create,
                    16,
                    16 * Constants.MB,
                    blobOffset,
                    200 * Constants.MB,
                    null,
                    null,
                    null,
                    CancellationToken.None);
                await blob.DeleteAsync();

                Assert.IsTrue(FilesAreEqual(inputFileName, outputFileName, blobOffset));
            }
            finally
            {
                container.DeleteIfExists();
                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        [TestMethod]
        [Description("Test that cancelling a large download actually cancels the request.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task LargeDownloadVerifyCancellationTest()
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("largeblob1");

                long bufferSize = 2 * Constants.KB;
                long offSet = 0;
                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    while (offSet < 200 * Constants.MB)
                    {
                        byte[] buffer = GetRandomBuffer(bufferSize);
                        await file.WriteAsync(buffer, 0, buffer.Length);
                        offSet += bufferSize;
                    }
                }

                BlobRequestOptions options = new BlobRequestOptions();
                options.ParallelOperationThreadCount = 16;
                await blob.UploadFromFileAsync(inputFileName, null, options, null);

                CancellationTokenSource cts = new CancellationTokenSource();

                try
                {
                    Task downloadTask = blob.DownloadToFileParallelAsync(outputFileName, FileMode.Create, 16, 16 * Constants.MB, cts.Token);
                    await Task.Delay(1000);
                    cts.Cancel();
                    await downloadTask;

                    Assert.Fail("Expected a cancellation exception to be thrown");
                }
                catch (OperationCanceledException)
                {
                }

                blob.Delete();
            }
            finally
            {
                container.DeleteIfExists();

                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        [TestMethod]
        [Description("Test that restrictions on the range size.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task LargeDownloadVerifyRangeSizeRestrictions()
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("largeblob1");
                await blob.UploadTextAsync("Tent");
                try
                {
                    await blob.DownloadToFileParallelAsync(outputFileName, FileMode.Create, 16, 16 * Constants.MB + 3, CancellationToken.None);
                    Assert.Fail("Expected a failure");
                }
                catch (ArgumentException) {}

                try
                {
                    await blob.DownloadToFileParallelAsync(outputFileName, FileMode.Create, 16, 2 * Constants.MB, CancellationToken.None);
                    Assert.Fail("Expected a failure");
                }
                catch (ArgumentOutOfRangeException) {}

                try
                {
                    BlobRequestOptions options = new BlobRequestOptions();
                    options.UseTransactionalMD5 = true;
                    await blob.DownloadToFileParallelAsync(outputFileName, FileMode.Create, 16, 16 * Constants.MB, 0, null, null, options, null, CancellationToken.None);
                    Assert.Fail("Expected a failure");
                }
                catch (ArgumentException) {}

                blob.Delete();
            }
            finally
            {
                container.DeleteIfExists();

                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        [TestMethod]
        [Description("Test downloading an empty blob to a file")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task ParallelDownloadToEmptyFileTest()
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempPath() + "output.tmp";
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("largeblob1");


                FileStream fs = File.Create(inputFileName);
                fs.Close();
                
                BlobRequestOptions options = new BlobRequestOptions();
                options.ParallelOperationThreadCount = 16;
                await blob.UploadFromFileAsync(inputFileName, null, options, null);

                Assert.IsFalse(File.Exists(outputFileName));
                await blob.DownloadToFileParallelAsync(
                    outputFileName,
                    FileMode.Create,
                    16,
                    null);
                Assert.IsTrue(File.Exists(outputFileName));

                await blob.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExists();
                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        private static bool FilesAreEqual(string file1Name, string file2Name, long? file1Offset = null)
        {
            int BYTES_TO_READ = sizeof(Int64);
            FileInfo file1 = new FileInfo(file1Name);
            FileInfo file2 = new FileInfo(file2Name);

            if (!file1Offset.HasValue && file1.Length != file2.Length)
            {
                return false;
            }

            int count = (int)Math.Ceiling((double)file2.Length / BYTES_TO_READ);

            using (FileStream fs1 = file1.OpenRead())
            using (FileStream fs2 = file2.OpenRead())
            {
                if (file1Offset.HasValue)
                {
                    fs1.Seek(file1Offset.Value, SeekOrigin.Begin);
                }

                byte[] buff1 = new byte[sizeof(Int64)];
                byte[] buff2 = new byte[sizeof(Int64)];

                for (int i = 0; i < count; i++)
                {
                    fs1.Read(buff1, 0, BYTES_TO_READ);
                    fs2.Read(buff2, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(buff1, 0) != BitConverter.ToInt64(buff2, 0))
                        return false;
                }
            }

            return true;
        }
    }
#endif
}