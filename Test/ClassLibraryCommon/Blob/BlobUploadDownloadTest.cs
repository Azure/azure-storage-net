// -----------------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading;

    [TestClass]
    public class BlobUploadDownloadTest : BlobTestBase
    {
        private CloudBlobContainer testContainer;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testContainer = GetRandomContainerReference();
            this.testContainer.CreateIfNotExists();

            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testContainer.DeleteIfExists();
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Download a specific range of a page blob to a stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobDownloadToStreamRangeTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            using (MemoryStream wholeBlob = new MemoryStream(buffer))
            {
                blob.UploadFromStream(wholeBlob);

                byte[] testBuffer = new byte[1024];
                MemoryStream blobStream = new MemoryStream(testBuffer);
                StorageException storageEx = TestHelper.ExpectedException<StorageException>(
                    () => blob.DownloadRangeToStream(blobStream, 0, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));
                blob.DownloadRangeToStream(blobStream, 0, 1024);
                Assert.AreEqual(blobStream.Position, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(blobStream, wholeBlob, 0, 0, 1024);

                CloudPageBlob blob2 = this.testContainer.GetPageBlobReference("blob1");
                MemoryStream blobStream2 = new MemoryStream(testBuffer);
                storageEx = TestHelper.ExpectedException<StorageException>(
                    () => blob2.DownloadRangeToStream(blobStream, 1024, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));
                blob2.DownloadRangeToStream(blobStream2, 1024, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(blobStream2, wholeBlob, 0, 1024, 1024);

                AssertAreEqual(blob, blob2);
            }
        }

        [TestMethod]
        [Description("Download a specific range of an append blob to a stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobDownloadToStreamRangeTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            using (MemoryStream wholeBlob = new MemoryStream(buffer))
            {
                blob.CreateOrReplace();
                blob.AppendBlock(wholeBlob, null);

                byte[] testBuffer = new byte[1024];
                MemoryStream blobStream = new MemoryStream(testBuffer);
                StorageException storageEx = TestHelper.ExpectedException<StorageException>(
                    () => blob.DownloadRangeToStream(blobStream, 0, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));
                blob.DownloadRangeToStream(blobStream, 0, 1024);
                Assert.AreEqual(blobStream.Position, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(blobStream, wholeBlob, 0, 0, 1024);

                CloudAppendBlob blob2 = this.testContainer.GetAppendBlobReference("blob1");
                MemoryStream blobStream2 = new MemoryStream(testBuffer);
                storageEx = TestHelper.ExpectedException<StorageException>(
                    () => blob2.DownloadRangeToStream(blobStream, 1024, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));
                blob2.DownloadRangeToStream(blobStream2, 1024, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(blobStream2, wholeBlob, 0, 1024, 1024);

                AssertAreEqual(blob, blob2);
            }
        }

        [TestMethod]
        [Description("Upload a stream to a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobUploadFromStreamTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                blob.UploadFromStream(srcStream);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                blob.DownloadRangeToStream(dstStream, null, null);
                TestHelper.AssertStreamsAreEqual(srcStream, dstStream);
            }
        }

        [TestMethod]
        [Description("Upload from text to a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobUploadWithoutMD5ValidationAndStoreBlobContentTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            BlobRequestOptions options = new BlobRequestOptions();
            options.DisableContentMD5Validation = false;
            options.StoreBlobContentMD5 = false;
            OperationContext context = new OperationContext();
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                blob.UploadFromStream(srcStream, null, options, context);
                blob.FetchAttributes();
                string md5 = blob.Properties.ContentMD5;
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.SetProperties(null, options, context);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                TestHelper.ExpectedException(() => blob.DownloadRangeToStream(dstStream, null, null, null, options, context),
                    "Try to Download a stream with a corrupted md5 and DisableMD5Validation set to false",
                    HttpStatusCode.OK);

                options.DisableContentMD5Validation = true;
                blob.SetProperties(null, options, context);
                byte[] testBuffer2 = new byte[2048];
                MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                blob.DownloadRangeToStream(dstStream2, null, null, null, options, context);
            }
        }

        [TestMethod]
        [Description("Upload from text to an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobUploadWithMD5ValidationTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            BlobRequestOptions options = new BlobRequestOptions();
            options.DisableContentMD5Validation = false;
            options.StoreBlobContentMD5 = false;
            OperationContext context = new OperationContext();
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                blob.CreateOrReplace();
                blob.AppendBlock(srcStream, contentMD5, null, options, context);

                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.SetProperties(null, options, context);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                TestHelper.ExpectedException(() => blob.DownloadRangeToStream(dstStream, null, null, null, options, context),
                    "Try to Download a stream with a corrupted md5 and DisableMD5Validation set to false",
                    HttpStatusCode.OK);

                options.DisableContentMD5Validation = true;
                blob.SetProperties(null, options, context);
                byte[] testBuffer2 = new byte[2048];
                MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                blob.DownloadRangeToStream(dstStream2, null, null, null, options, context);
            }
        }

        [TestMethod]
        [Description("Check that the empty header is not used for signing.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobEmptyHeaderSigningTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            OperationContext context = new OperationContext();
            try
            {
                container.Create(null, context);
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                context.UserHeaders = new Dictionary<string, string>();
                context.UserHeaders.Add("x-ms-foo", String.Empty);
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    blob.UploadFromStream(srcStream, null, null, context);
                }
                byte[] testBuffer2 = new byte[2048];
                MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                blob.DownloadRangeToStream(dstStream2, null, null, null, null, context);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload from file to a block blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadDownloadFile()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            CloudBlockBlob nullBlob = this.testContainer.GetBlockBlobReference("null");
            this.DoUploadDownloadFile(blob, 0, false);
            this.DoUploadDownloadFile(blob, 4096, false);
            this.DoUploadDownloadFile(blob, 4097, false);

            TestHelper.ExpectedException<IOException>(
                () => blob.UploadFromFile("non_existent.file"),
                "UploadFromFile requires an existing file");

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.Create),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream file = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                file.Write(buffer, 0, buffer.Length);
            }
            TestHelper.ExpectedException<IOException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbage.file"));
            System.IO.File.Delete("garbage.file");

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.Append),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        [TestMethod]
        [Description("Upload from file to a page blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadDownloadFile()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            CloudPageBlob nullBlob = this.testContainer.GetPageBlobReference("null");
            this.DoUploadDownloadFile(blob, 0, false);
            this.DoUploadDownloadFile(blob, 4096, false);

            TestHelper.ExpectedException<ArgumentException>(
                () => this.DoUploadDownloadFile(blob, 4097, false),
                "Page blobs must be 512-byte aligned");

            TestHelper.ExpectedException<IOException>(
                () => blob.UploadFromFile("non_existent.file"),
                "UploadFromFile requires an existing file");

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.Create),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream file = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                file.WriteAsync(buffer, 0, buffer.Length);
            }
            TestHelper.ExpectedException<IOException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbage.file"));
            System.IO.File.Delete("garbage.file");

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.OpenOrCreate),
                "DownloadToFile should leave an empty file behind after failing, depending on file mode.");
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        [TestMethod]
        [Description("Upload from file to an append blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadDownloadFile()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            CloudAppendBlob nullBlob = this.testContainer.GetAppendBlobReference("null");
            this.DoUploadDownloadFile(blob, 0, false);
            this.DoUploadDownloadFile(blob, 4096, false);
            this.DoUploadDownloadFile(blob, 4097, false);

            TestHelper.ExpectedException<IOException>(
                () => blob.UploadFromFile("non_existent.file"),
                "UploadFromFile requires an existing file");

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.Create),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream file = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                file.WriteAsync(buffer, 0, buffer.Length);
            }
            TestHelper.ExpectedException<IOException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbage.file"));
            System.IO.File.Delete("garbage.file");

            TestHelper.ExpectedException<StorageException>(
                () => nullBlob.DownloadToFile("garbage.file", FileMode.Append),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        [TestMethod]
        [Description("Upload from file to a block blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadDownloadFileAPM()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            CloudBlockBlob nullBlob = this.testContainer.GetBlockBlobReference("null");
            this.DoUploadDownloadFile(blob, 0, true);
            this.DoUploadDownloadFile(blob, 4096, true);
            this.DoUploadDownloadFile(blob, 4097, true);

            TestHelper.ExpectedException<IOException>(
                () => blob.BeginUploadFromFile("non_existent.file", null, null),
                "UploadFromFile requires an existing file");

            IAsyncResult result;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                OperationContext context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.Create, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.CreateNew, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.Append, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should leave an empty file behind after failing, depending on file mode.");
                Assert.IsTrue(File.Exists("garbage.file"));
                File.Delete("garbage.file");
            }
        }

        [TestMethod]
        [Description("Upload from file to a page blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadDownloadFileAPM()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            CloudPageBlob nullBlob = this.testContainer.GetPageBlobReference("null");
            this.DoUploadDownloadFile(blob, 0, true);
            this.DoUploadDownloadFile(blob, 4096, true);

            TestHelper.ExpectedException<ArgumentException>(
                () => this.DoUploadDownloadFile(blob, 4097, true),
                "Page blobs must be 512-byte aligned");
 
            TestHelper.ExpectedException<IOException>(
                () => blob.BeginUploadFromFile("non_existent.file", null, null),
                "UploadFromFile requires an existing file");

            IAsyncResult result;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                OperationContext context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.Create, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.CreateNew, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.Append, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should leave an empty file behind after failing depending on file mode.");
                Assert.IsTrue(File.Exists("garbage.file"));
                File.Delete("garbage.file");
            }
        }

        [TestMethod]
        [Description("Upload from file to an append blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadDownloadFileAPM()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            CloudBlockBlob nullBlob = this.testContainer.GetBlockBlobReference("null");
            this.DoUploadDownloadFile(blob, 0, true);
            this.DoUploadDownloadFile(blob, 4096, true);
            this.DoUploadDownloadFile(blob, 4097, true);

            TestHelper.ExpectedException<IOException>(
                () => blob.BeginUploadFromFile("non_existent.file", null, null),
                "UploadFromFile requires an existing file");

            IAsyncResult result;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                OperationContext context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.Create, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.CreateNew, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullBlob.BeginDownloadToFile("garbage.file", FileMode.Append, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullBlob.EndDownloadToFile(result),
                    "DownloadToFile should leave an empty file behind after failing, depending on file mode.");
                Assert.IsTrue(File.Exists("garbage.file"));
                File.Delete("garbage.file");
            }
        }

#if TASK
        [TestMethod]
        [Description("Upload from file to a block blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadDownloadFileTask()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            CloudBlockBlob nullBlob = this.testContainer.GetBlockBlobReference("null");
            this.DoUploadDownloadFileTask(blob, 0);
            this.DoUploadDownloadFileTask(blob, 4096);
            this.DoUploadDownloadFileTask(blob, 4097);

            TestHelper.ExpectedException<IOException>(
                () => blob.UploadFromFileAsync("non_existent.file"),
                "UploadFromFile requires an existing file");

            AggregateException e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.Create).Wait(),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait(),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream systemFile = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                systemFile.WriteAsync(buffer, 0, buffer.Length);
            }
            try
            {
                nullBlob.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait();
                Assert.Fail("DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            }
            catch (System.IO.IOException)
            {
                // Success if test reaches here meaning the expected exception was thrown.
                Assert.IsTrue(System.IO.File.Exists("garbage.file"));
                System.IO.File.Delete("garbage.file");
            }

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.OpenOrCreate).Wait(),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        [TestMethod]
        [Description("Upload from file to a page blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadDownloadFileTask()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            CloudPageBlob nullBlob = this.testContainer.GetPageBlobReference("null");
            this.DoUploadDownloadFileTask(blob, 0);
            this.DoUploadDownloadFileTask(blob, 4096);
            
            TestHelper.ExpectedException<ArgumentException>(
                () => this.DoUploadDownloadFileTask(blob, 4097),
                "Page blobs must be 512-byte aligned");

            TestHelper.ExpectedException<IOException>(
                () => blob.UploadFromFileAsync("non_existent.file"),
                "UploadFromFile requires an existing file");

            AggregateException e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.Create).Wait(),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait(),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream systemFile = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                systemFile.WriteAsync(buffer, 0, buffer.Length);
            }
            try
            {
                nullBlob.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait();
                Assert.Fail("DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            }
            catch (System.IO.IOException)
            {
                // Success if test reaches here meaning the expected exception was thrown.
                Assert.IsTrue(System.IO.File.Exists("garbage.file"));
                System.IO.File.Delete("garbage.file");
            }

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.Append).Wait(),
                "DownloadToFile should leave an empty file behind after failing, depending on file mode.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        [TestMethod]
        [Description("Upload from file to an append blob with file cleanup for failure cases")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadDownloadFileTask()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            CloudAppendBlob nullBlob = this.testContainer.GetAppendBlobReference("null");
            this.DoUploadDownloadFileTask(blob, 0);
            this.DoUploadDownloadFileTask(blob, 4096);
            this.DoUploadDownloadFileTask(blob, 4097);

            TestHelper.ExpectedException<IOException>(
                () => blob.UploadFromFileAsync("non_existent.file"),
                "UploadFromFile requires an existing file");

            AggregateException e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.Create).Wait(),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait(),
                "DownloadToFile should not leave an empty file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream systemFile = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                systemFile.WriteAsync(buffer, 0, buffer.Length);
            }
            try
            {
                nullBlob.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait();
                Assert.Fail("DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            }
            catch (System.IO.IOException)
            {
                // Success if test reaches here meaning the expected exception was thrown.
                Assert.IsTrue(System.IO.File.Exists("garbage.file"));
                System.IO.File.Delete("garbage.file");
            }

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullBlob.DownloadToFileAsync("garbage.file", FileMode.OpenOrCreate).Wait(),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        private void DoUploadDownloadFileTask(ICloudBlob blob, int fileSize)
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();

            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    file.Write(buffer, 0, buffer.Length);
                }

                OperationContext context = new OperationContext();
                blob.UploadFromFileAsync(inputFileName).Wait();

                blob.UploadFromFileAsync(inputFileName, null, null, context).Wait();
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                TestHelper.ExpectedException<IOException>(
                    () => blob.DownloadToFileAsync(outputFileName, FileMode.CreateNew),
                    "CreateNew on an existing file should fail");

                context = new OperationContext();
                blob.DownloadToFileAsync(outputFileName, FileMode.Create, null, null, context).Wait();
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (
                    FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                               outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqual(inputFileStream, outputFileStream);
                }

                blob.DownloadToFileAsync(outputFileName, FileMode.Append).Wait();

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
            catch (AggregateException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }
            finally
            {
                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }
#endif

        private void DoUploadDownloadFile(ICloudBlob blob, int fileSize, bool isAsync)
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();

            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    file.Write(buffer, 0, buffer.Length);
                }

                if (isAsync)
                {
                    IAsyncResult result;
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        OperationContext context = new OperationContext();
                        result = blob.BeginUploadFromFile(inputFileName,
                                ar => waitHandle.Set(),
                                null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromFile(result);

                        result = blob.BeginUploadFromFile(inputFileName, null, null, context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromFile(result);
                        Assert.IsNotNull(context.LastResult.ServiceRequestID);

                        TestHelper.ExpectedException<IOException>(
                            () => blob.BeginDownloadToFile(outputFileName, FileMode.CreateNew, null, null),
                            "CreateNew on an existing file should fail");

                        context = new OperationContext();
                        result = blob.BeginDownloadToFile(outputFileName, FileMode.Create, null, null, context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndDownloadToFile(result);
                        Assert.IsNotNull(context.LastResult.ServiceRequestID);

                        using (FileStream inputFile = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                            outputFile = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                        {
                            TestHelper.AssertStreamsAreEqual(inputFile, outputFile);
                        }

                        result = blob.BeginDownloadToFile(outputFileName, FileMode.Append,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndDownloadToFile(result);

                        using (FileStream inputFile = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                            outputFile = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                        {
                            Assert.AreEqual(2 * fileSize, outputFile.Length);

                            for (int i = 0; i < fileSize; i++)
                            {
                                Assert.AreEqual(inputFile.ReadByte(), outputFile.ReadByte());
                            }

                            inputFile.Seek(0, SeekOrigin.Begin);
                            for (int i = 0; i < fileSize; i++)
                            {
                                Assert.AreEqual(inputFile.ReadByte(), outputFile.ReadByte());
                            }
                        }
                    }
                }
                else
                {
                    OperationContext context = new OperationContext();

                    blob.UploadFromFile(inputFileName);
                    blob.UploadFromFile(inputFileName, null, null, context);
                    Assert.IsNotNull(context.LastResult.ServiceRequestID);

                    TestHelper.ExpectedException<IOException>(
                        () => blob.DownloadToFile(outputFileName, FileMode.CreateNew),
                        "CreateNew on an existing file should fail");

                    context = new OperationContext();
                    blob.DownloadToFile(outputFileName, FileMode.Create, null, null, context);
                    Assert.IsNotNull(context.LastResult.ServiceRequestID);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                        outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                    {
                        TestHelper.AssertStreamsAreEqual(inputFileStream, outputFileStream);
                    }

                    blob.DownloadToFile(outputFileName, FileMode.Append);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
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
            }
            finally
            {
                File.Delete(inputFileName);
                File.Delete(outputFileName);
            }
        }

        [TestMethod]
        [Description("Upload a block blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadFromByteArray()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 512, 0, 511, false);
        }

        [TestMethod]
        [Description("Upload a block blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadFromByteArrayAPM()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 512, 0, 511, true);
        }

        [TestMethod]
        [Description("Upload a page blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromByteArray()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, false);

            TestHelper.ExpectedException<ArgumentException>(
                () => this.DoUploadFromByteArrayTest(blob, 512, 0, 511, false),
                "Page blobs must be 512-byte aligned");
        }

        [TestMethod]
        [Description("Upload a page blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromByteArrayAPM()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, true);

            TestHelper.ExpectedException<ArgumentException>(
                () => this.DoUploadFromByteArrayTest(blob, 512, 0, 511, true),
                "Page blobs must be 512-byte aligned");
        }

        [TestMethod]
        [Description("Upload an append blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromByteArray()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 512, 0, 511, false);
        }

        [TestMethod]
        [Description("Upload an append blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromByteArrayAPM()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 512, 0, 511, true);
        }

        [TestMethod]
        [Description("Upload an append blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadBlockFromByteArray()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(blob, 512, 0, 511, false);
        }

        [TestMethod]
        [Description("Upload an append blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadBlockFromByteArrayAPM()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 4 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 0, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 1 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 4 * 512, 2 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(blob, 512, 0, 511, true);
        }

#if TASK
        [TestMethod]
        [Description("Upload a block blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadFromByteArrayTask()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 0, 4 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 0, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 1 * 512, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 2 * 512, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 512, 0, 511);
        }

        [TestMethod]
        [Description("Upload a page blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromByteArrayTask()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 0, 4 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 0, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 1 * 512, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 2 * 512, 2 * 512);
            
            TestHelper.ExpectedException<ArgumentException>(
                () => this.DoUploadFromByteArrayTestTask(blob, 512, 0, 511),
                "Page blobs must be 512-byte aligned");
        }

        [TestMethod]
        [Description("Upload an append blob using a byte array")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromByteArrayTask()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 0, 4 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 0, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 1 * 512, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 4 * 512, 2 * 512, 2 * 512);
            this.DoUploadFromByteArrayTestTask(blob, 512, 0, 511);
        }

        private void DoUploadFromByteArrayTestTask(ICloudBlob blob, int bufferSize, int bufferOffset, int count)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);
            byte[] downloadedBuffer = new byte[bufferSize];
            int downloadLength;

            try
            {
                blob.UploadFromByteArrayAsync(buffer, bufferOffset, count).Wait();
                downloadLength = blob.DownloadToByteArrayAsync(downloadedBuffer, 0).Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }

            Assert.AreEqual(count, downloadLength);

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(buffer[i + bufferOffset], downloadedBuffer[i]);
            }
        }
#endif

        private void DoUploadFromByteArrayTest(ICloudBlob blob, int bufferSize, int bufferOffset, int count, bool isAsync)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);
            byte[] downloadedBuffer = new byte[bufferSize];
            int downloadLength;

            if (isAsync)
            {
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginUploadFromByteArray(buffer, bufferOffset, count,
                                                                    ar => waitHandle.Set(),
                                                                    null);
                    waitHandle.WaitOne();
                    blob.EndUploadFromByteArray(result);

                    result = blob.BeginDownloadToByteArray(downloadedBuffer, 0,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    downloadLength = blob.EndDownloadToByteArray(result);
                }
            }
            else
            {
                blob.UploadFromByteArray(buffer, bufferOffset, count);
                downloadLength = blob.DownloadToByteArray(downloadedBuffer, 0);
            }

            Assert.AreEqual(count, downloadLength);

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(buffer[i + bufferOffset], downloadedBuffer[i]);
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadToByteArray()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 0);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 0);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 0);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadToByteArrayAPM()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 1);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 1);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 1);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadToByteArrayAPMOverload()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 2);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 2);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadToByteArrayTask()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 0, false);
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 1 * 512, false);
            this.DoDownloadToByteArrayTestTask(blob, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadToByteArrayOverloadTask()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 0, true);
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 1 * 512, true);
            this.DoDownloadToByteArrayTestTask(blob, 2 * 512, 4 * 512, 1 * 512, true);
        }
#endif

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToByteArray()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 0);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 0);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 0);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToByteArrayAPM()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 1);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 1);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 1);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToByteArrayAPMOverload()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 2);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 2);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToByteArrayTask()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 0, false);
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 1 * 512, false);
            this.DoDownloadToByteArrayTestTask(blob, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToByteArrayOverloadTask()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 0, true);
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 1 * 512, true);
            this.DoDownloadToByteArrayTestTask(blob, 2 * 512, 4 * 512, 1 * 512, true);
        }
#endif
        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadToByteArray()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 0);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 0);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 0);
        }

        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadToByteArrayAPM()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 1);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 1);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 1);
        }

        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadToByteArrayAPMOverload()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 2);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 2);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadToByteArrayTask()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 0, false);
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 1 * 512, false);
            this.DoDownloadToByteArrayTestTask(blob, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadToByteArrayOverloadTask()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 0, true);
            this.DoDownloadToByteArrayTestTask(blob, 1 * 512, 2 * 512, 1 * 512, true);
            this.DoDownloadToByteArrayTestTask(blob, 2 * 512, 4 * 512, 1 * 512, true);
        }
#endif

        /// <summary>
        /// Single put blob and get blob
        /// </summary>
        /// <param name="blobSize">The blob size.</param>
        /// <param name="bufferOffset">The blob offset.</param>
        /// <param name="option"> 0 - Sunc, 1 - APM and 2 - APM overload.</param>
        private void DoDownloadToByteArrayTest(ICloudBlob blob, int blobSize, int bufferSize, int bufferOffset, int option)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(blobSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalBlob = new MemoryStream(buffer))
            {
                blob.UploadFromStream(originalBlob);
            }

            if (option == 0)
            {
                downloadLength = blob.DownloadToByteArray(resultBuffer, bufferOffset);
            }
            else if (option == 1)
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginDownloadToByteArray(resultBuffer,
                        bufferOffset,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    downloadLength = blob.EndDownloadToByteArray(result);
                }
            }
            else
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    OperationContext context = new OperationContext();
                    ICancellableAsyncResult result = blob.BeginDownloadToByteArray(resultBuffer,
                        bufferOffset, /* offset */
                        null, /* accessCondition */
                        null, /* options */
                        context, /* operationContext */
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    downloadLength = blob.EndDownloadToByteArray(result);
                }
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

#if TASK
        /// <summary>
        /// Single put blob and get blob
        /// </summary>
        /// <param name="blobSize">The blob size.</param>
        /// <param name="bufferOffset">The blob offset.</param>
        /// <param name="option">Run with overloaded parameters.</param>
        private void DoDownloadToByteArrayTestTask(ICloudBlob blob, int blobSize, int bufferSize, int bufferOffset, bool overload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(blobSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalBlob = new MemoryStream(buffer))
            {
                blob.UploadFromStreamAsync(originalBlob).Wait();
            }

            if (overload)
            {
                downloadLength = blob.DownloadToByteArrayAsync(
                    resultBuffer,
                    bufferOffset,
                    null,
                    null,
                    new OperationContext())
                        .Result;
            }
            else
            {
                downloadLength = blob.DownloadToByteArrayAsync(resultBuffer, bufferOffset).Result;
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
#endif

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToByteArray()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 0);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 0);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToByteArrayAPM()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 1);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 1);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToByteArrayAPMOverload()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 2);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToByteArrayTask()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 1023, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 0, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 512, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToByteArrayOverloadTask()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 1023, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 0, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 512, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 0, 512, true);
        }
#endif

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadRangeToByteArray()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 0);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 0);

        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadRangeToByteArrayAPM()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 1);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 1);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadRangeToByteArrayAPMOverload()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 2);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadRangeToByteArrayTask()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 1023, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 0, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 512, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadRangeToByteArrayOverloadTask()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 1023, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 0, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 512, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 0, 512, true);
        }
#endif
        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadRangeToByteArray()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 0);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 0);

        }

        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadRangeToByteArrayAPM()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 1);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 1);
        }

        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadRangeToByteArrayAPMOverload()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, null, null, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 2);

            // Edge cases
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 1023, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 0, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 0, 512, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(blob, 1024, 1024, 512, 0, 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadRangeToByteArrayTask()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 1023, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 0, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 512, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put blob and get blob on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDownloadRangeToByteArrayOverloadTask()
        {
            CloudAppendBlob blob = this.testContainer.GetAppendBlobReference("blob1");
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(blob, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 1023, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 0, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 0, 512, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(blob, 1024, 1024, 512, 0, 512, true);
        }
#endif
        /// <summary>
        /// Single put blob and get blob
        /// </summary>
        /// <param name="blobSize">The blob size.</param>
        /// <param name="bufferSize">The output buffer size.</param>
        /// <param name="bufferOffset">The output buffer offset.</param>
        /// <param name="blobOffset">The blob offset.</param>
        /// <param name="length">Length of the data range to download.</param>
        /// <param name="option">0 - Sync, 1 - APM and 2 - APM overload.</param>
        private void DoDownloadRangeToByteArray(ICloudBlob blob, int blobSize, int bufferSize, int bufferOffset, long? blobOffset, long? length, int option)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(blobSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalBlob = new MemoryStream(buffer))
            {
                blob.UploadFromStream(originalBlob);
            }

            if (option == 0)
            {
                downloadLength = blob.DownloadRangeToByteArray(resultBuffer, bufferOffset, blobOffset, length);
            }
            else if (option == 1)
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginDownloadRangeToByteArray(resultBuffer,
                        bufferOffset,
                        blobOffset,
                        length,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    downloadLength = blob.EndDownloadRangeToByteArray(result);
                }
            }
            else
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    OperationContext context = new OperationContext();
                    ICancellableAsyncResult result = blob.BeginDownloadRangeToByteArray(resultBuffer,
                        bufferOffset,
                        blobOffset,
                        length,
                        null,
                        null,
                        context,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    downloadLength = blob.EndDownloadRangeToByteArray(result);
                }
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

#if TASK
        /// <summary>
        /// Single put blob and get blob
        /// </summary>
        /// <param name="blobSize">The blob size.</param>
        /// <param name="bufferSize">The output buffer size.</param>
        /// <param name="bufferOffset">The output buffer offset.</param>
        /// <param name="blobOffset">The blob offset.</param>
        /// <param name="length">Length of the data range to download.</param>
        /// <param name="overload">Run with overloaded parameters.</param>
        private void DoDownloadRangeToByteArrayTask(ICloudBlob blob, int blobSize, int bufferSize, int bufferOffset, long? blobOffset, long? length, bool overload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(blobSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalBlob = new MemoryStream(buffer))
            {
                blob.UploadFromStreamAsync(originalBlob).Wait();
            }

            if (overload)
            {
                downloadLength = blob.DownloadRangeToByteArrayAsync(
                    resultBuffer, bufferOffset, blobOffset, length, null, null, new OperationContext()).Result;
            }
            else
            {
                downloadLength = blob.DownloadRangeToByteArrayAsync(resultBuffer, bufferOffset, blobOffset, length).Result;
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
#endif

        #region Negative tests
        [TestMethod]
        [Description("Single put blob and get blob on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToByteArrayNegativeTests()
        {
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadRangeToByteArrayNegativeTests(blob);
        }

        [TestMethod]
        [Description("Single put blob and get blob on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadRangeToByteArrayNegativeTests()
        {
            CloudPageBlob blob = this.testContainer.GetPageBlobReference("blob1");
            this.DoDownloadRangeToByteArrayNegativeTests(blob);
        }

        private void DoDownloadRangeToByteArrayNegativeTests(ICloudBlob blob)
        {
            int blobLength = 1024;
            int resultBufSize = 1024;
            byte[] buffer = GetRandomBuffer(blobLength);
            byte[] resultBuffer = new byte[resultBufSize];

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                blob.UploadFromStream(stream);

                TestHelper.ExpectedException(() => blob.DownloadRangeToByteArray(resultBuffer, 0, 1024, 1), "Try invalid length", HttpStatusCode.RequestedRangeNotSatisfiable);
                StorageException ex = TestHelper.ExpectedException<StorageException>(() => blob.DownloadToByteArray(resultBuffer, 1024), "Provide invalid offset");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = TestHelper.ExpectedException<StorageException>(() => blob.DownloadRangeToByteArray(resultBuffer, 1023, 0, 2), "Should fail when offset + length required is greater than size of the buffer");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = TestHelper.ExpectedException<StorageException>(() => blob.DownloadRangeToByteArray(resultBuffer, 0, 0, -10), "Fail when a negative length is specified");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(() => blob.DownloadRangeToByteArray(resultBuffer, -10, 0, 20), "Fail if a negative offset is provided");
                ex = TestHelper.ExpectedException<StorageException>(() => blob.DownloadRangeToByteArray(resultBuffer, 0, -10, 20), "Fail if a negative blob offset is provided");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
            }
        }
        #endregion
    }
}