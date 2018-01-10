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
    using Auth;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

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

        [TestMethod]
        [Description("Upload from file to a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobUploadBasicFunctionality()
        {
            string accountName = this.testContainer.ServiceClient.Credentials.AccountName;
            string accountKey = this.testContainer.ServiceClient.Credentials.ExportBase64EncodedKey();
            string containerName = this.testContainer.Name + "copy";
            string blobName = "myBlob";
            string inputFileName = Path.GetTempFileName();
            File.WriteAllText(inputFileName, @"Sample file text here.");

            try
            {
                #region sample_UploadBlob_EndToEnd
                // This is one common way of creating a CloudStorageAccount object. You can get 
                // your Storage Account Name and Key from the Azure Portal.
                StorageCredentials credentials = new StorageCredentials(accountName, accountKey);
                CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, useHttps: true);

                // Another common way to create a CloudStorageAccount object is to use a connection string:
                // CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // This call creates a local CloudBlobContainer object, but does not make a network call
                // to the Azure Storage Service. The container on the service that this object represents may
                // or may not exist at this point. If it does exist, the properties will not yet have been
                // popluated on this object.
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);

                // This makes an actual service call to the Azure Storage service. Unless this call fails,
                // the container will have been created.
                blobContainer.Create();

                // This also does not make a service call, it only creates a local object.
                CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blobName);

                // This transfers data in the file to the blob on the service.
                blob.UploadFromFile(inputFileName);
                #endregion

                Assert.AreEqual(File.ReadAllText(inputFileName), blob.DownloadText());
            }
            finally
            {
                StorageCredentials credentials = new StorageCredentials(accountName, accountKey);
                CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
                blobContainer.DeleteIfExists();

                if (File.Exists(inputFileName))
                {
                    File.Delete(inputFileName);
                }
            }
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
        [Description("Test blob upload using parallel multi-filestream upload strategy.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobTestParallelUploadFromFileStream()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            string inputFileName = "i_" + Path.GetRandomFileName();
            string outputFileName = "o_" + Path.GetRandomFileName();

            try
            {                
                container.Create();

                BlobRequestOptions options = new BlobRequestOptions()
                {
                    StoreBlobContentMD5 = false,
                    ParallelOperationThreadCount = 1
                };

                byte[] buffer = GetRandomBuffer(25 * 1024 * 1024);
 
                using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buffer, 0, buffer.Length);
                }

                CloudBlockBlob blob1 = container.GetBlockBlobReference("blob1");

                blob1.StreamWriteSizeInBytes = 5 * 1024 * 1024;
                blob1.UploadFromFile(inputFileName, null, options, null);
                blob1.DownloadToFile(outputFileName, FileMode.Create, null, options, null);
               
                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }

                CloudBlockBlob blob = container.GetBlockBlobReference("unittestblob"); // This one is used in the unit test samples, hence the name "blob", not "blob2".
                blob.StreamWriteSizeInBytes = 5 * 1024 * 1024;

                #region sample_BlobRequestOptions_ParallelOperationThreadCount

                BlobRequestOptions parallelThreadCountOptions = new BlobRequestOptions();

                // Allow up to four simultaneous I/O operations.
                parallelThreadCountOptions.ParallelOperationThreadCount = 4;
                blob.UploadFromFile(inputFileName, accessCondition: null, options: parallelThreadCountOptions, operationContext: null);

                #endregion

                options.ParallelOperationThreadCount = 4;
                blob.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }

                CloudBlockBlob blob3 = container.GetBlockBlobReference("blob3");
                blob3.StreamWriteSizeInBytes = 6 * 1024 * 1024 + 1;
                options.ParallelOperationThreadCount = 1;
                blob3.UploadFromFile(inputFileName, null, options, null);
                blob3.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                     outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }

                CloudBlockBlob blob4 = container.GetBlockBlobReference("blob4");
                blob4.StreamWriteSizeInBytes = 6 * 1024 * 1024 + 1;
                options.ParallelOperationThreadCount = 3;
                blob4.UploadFromFile(inputFileName, null, options, null);
                blob4.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read), 
                    outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                }
            }
            finally
            {
                File.Delete(inputFileName);
                File.Delete(outputFileName);
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Test blob upload using multi-filestream upload strategy - APM.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobTestParallelUploadFromFileStreamAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            string inputFileName = "i_" + Path.GetRandomFileName();
            string outputFileName = "o_" + Path.GetRandomFileName();

            try
            {
                container.Create();

                BlobRequestOptions options = new BlobRequestOptions()
                {
                    UseTransactionalMD5 = false,
                    StoreBlobContentMD5 = false,
                    ParallelOperationThreadCount = 1
                };

                byte[] buffer = GetRandomBuffer(20 * 1024 * 1024);

                using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buffer, 0, buffer.Length);
                }

                CloudBlockBlob blob1 = container.GetBlockBlobReference("blob1");
                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob2");
                CloudBlockBlob blob3 = container.GetBlockBlobReference("blob3");
                CloudBlockBlob blob4 = container.GetBlockBlobReference("blob4");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    blob1.StreamWriteSizeInBytes = 5 * 1024 * 1024;
                    IAsyncResult result = blob1.BeginUploadFromFile(inputFileName, null, options, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob1.EndUploadFromFile(result);
                    blob1.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                         outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                    {
                        TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                    }

                    blob2.StreamWriteSizeInBytes = 5 * 1024 * 1024;
                    options.ParallelOperationThreadCount = 3;
                    result = blob2.BeginUploadFromFile(inputFileName, null, options, null, 
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndUploadFromFile(result);
                    blob2.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                         outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                    {
                        TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                    }

                    blob3.StreamWriteSizeInBytes = 6 * 1024 * 1024 + 1;
                    options.ParallelOperationThreadCount = 1;
                    result = blob3.BeginUploadFromFile(inputFileName, null, options, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob3.EndUploadFromFile(result);
                    blob3.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                         outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                    {
                        TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                    }

                    blob4.StreamWriteSizeInBytes = 6 * 1024 * 1024 + 1;
                    options.ParallelOperationThreadCount = 5;
                    result = blob4.BeginUploadFromFile(inputFileName, null, options, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob4.EndUploadFromFile(result);
                    blob4.DownloadToFile(outputFileName, FileMode.Create, null, options, null);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                         outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                    {
                        TestHelper.AssertStreamsAreEqualFast(inputFileStream, outputFileStream);
                    }
                }
            }
            finally
            {
                File.Delete(inputFileName);
                File.Delete(outputFileName);
                container.Delete();
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
            AssertSecondaryEndpoint();

            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference("blob1");
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 0, 0);
            this.DoDownloadToByteArrayTest(blob, 1 * 512, 2 * 512, 1 * 512, 0);
            this.DoDownloadToByteArrayTest(blob, 2 * 512, 4 * 512, 1 * 512, 0);

            byte[] bytes = new byte[] { 1, 2, 3, 4 };
            byte[] destinationArray = new byte[4];
            blob.UploadFromByteArray(bytes, 0, bytes.Length);

            // Wait until the data has been replicated to the secondary tenant.
            try
            {
                TestHelper.SpinUpTo30SecondsIgnoringFailures(() => blob.FetchAttributes(null, new BlobRequestOptions() { LocationMode = RetryPolicies.LocationMode.SecondaryOnly }, null));
            }
            catch (Exception)
            {
                Assert.Inconclusive("Data took more than 30 seconds to replicate to the secondary; aborting test.");
            }

            #region sample_RequestOptions_RetryPolicy

            // Create a Linear Retry Policy.
            // This retry policy will instruct the Storage Client to retry the request in a linear fashion.
            // This particular retry policy will retry the request every 20 seconds, up to a maximum of 4 retries.
            BlobRequestOptions optionsWithRetryPolicy = new BlobRequestOptions() { RetryPolicy = new RetryPolicies.LinearRetry(TimeSpan.FromSeconds(20), 4) };

            int byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: optionsWithRetryPolicy);

            // This retry policy will never retry.
            optionsWithRetryPolicy = new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() };
            byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: optionsWithRetryPolicy);

            #endregion


            #region sample_RequestOptions_LocationMode
            // The PrimaryOnly LocationMode directs the request and all potential retries to go to the primary endpoint.
            BlobRequestOptions locationModeRequestOptions = new BlobRequestOptions() { LocationMode = RetryPolicies.LocationMode.PrimaryOnly };
            byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: locationModeRequestOptions);

            // The PrimaryThenSecondary LocationMode directs the first request to go to the primary location.
            // If this request fails with a retryable error, the retry will next hit the secondary location.
            // Retries will switch back and forth between primary and secondary until the request succeeds, 
            // or retry attempts have been exhausted.
            locationModeRequestOptions = new BlobRequestOptions() { LocationMode = RetryPolicies.LocationMode.PrimaryThenSecondary };
            byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: locationModeRequestOptions);

            // The SecondaryOnly LocationMode directs the request and all potential retries to go to the secondary endpoint.
            locationModeRequestOptions = new BlobRequestOptions() { LocationMode = RetryPolicies.LocationMode.SecondaryOnly };
            byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: locationModeRequestOptions);

            // The SecondaryThenPrimary LocationMode directs the first request to go to the secondary location.
            // If this request fails with a retryable error, the retry will next hit the primary location.
            // Retries will switch back and forth between secondary and primary until the request succeeds, or retry attempts
            // have been exhausted.
            locationModeRequestOptions = new BlobRequestOptions() { LocationMode = RetryPolicies.LocationMode.SecondaryThenPrimary };
            byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: locationModeRequestOptions);
            #endregion

            #region sample_RequestOptions_ServerTimeout_MaximumExecutionTime

            BlobRequestOptions timeoutRequestOptions = new BlobRequestOptions()
            {
                // Each REST operation will timeout after 5 seconds.
                ServerTimeout = TimeSpan.FromSeconds(5),

                // Allot 30 seconds for this API call, including retries
                MaximumExecutionTime = TimeSpan.FromSeconds(30)
            };

            byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: timeoutRequestOptions);

            #endregion

            bool exceptionThrown = false;
            try
            {
                #region sample_RequestOptions_RequireEncryption
                // Instruct the client library to fail if data read from the service is not encrypted.
                BlobRequestOptions requireEncryptionRequestOptions = new BlobRequestOptions() { RequireEncryption = true };

                byteCount = blob.DownloadToByteArray(destinationArray, index: 0, accessCondition: null, options: requireEncryptionRequestOptions);
                #endregion
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown);
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

#if !(WINDOWS_RT || NETCORE)
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
                   blob.UploadFromStreamAsync(stream, null, null, null, progressHandler, cancellationToken)
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
            byte[] uploadBuffer = GetRandomBuffer(2 * 1024 * 1024);

            T uploadBlob = blobFactory();
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
            CloudBlobContainer container = GetRandomContainerReference();
            string inputFileName = "i_" + Path.GetRandomFileName();
            string outputFileName = "o_" + Path.GetRandomFileName();
            int bufferSize_25 = 25 * 1024 * 1024;
            int writeSize_5 = 5 * 1024 * 1024;
            int writeSize_6_plus_1 = 6 * 1024 * 1024 + 1;
            int delay = 5000;

            try
            {
                container.Create();

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
            finally
            {
                File.Delete(inputFileName);
                File.Delete(outputFileName);
                container.Delete();
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
            blob.ServiceClient.DefaultRequestOptions.RetryPolicy = null;

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