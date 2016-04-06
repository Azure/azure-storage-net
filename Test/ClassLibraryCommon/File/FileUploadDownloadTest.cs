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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;

    [TestClass]
    public class FileUploadDownloadTest : FileTestBase
    {

        private CloudFileShare testShare;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testShare = GetRandomShareReference();
            this.testShare.CreateIfNotExists();

            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testShare.DeleteIfExists();
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Download a specific range of the file to a stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDownloadToStreamRangeTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            using (MemoryStream wholeFile = new MemoryStream(buffer))
            {
                file.UploadFromStream(wholeFile);

                byte[] testBuffer = new byte[1024];
                MemoryStream fileStream = new MemoryStream(testBuffer);
                StorageException storageEx = TestHelper.ExpectedException<StorageException>(
                    () => file.DownloadRangeToStream(fileStream, 0, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));
                file.DownloadRangeToStream(fileStream, 0, 1024);
                Assert.AreEqual(fileStream.Position, 1024);
                TestHelper.AssertStreamsAreEqualAtIndex(fileStream, wholeFile, 0, 0, 1024);

                CloudFile file2 = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
                MemoryStream fileStream2 = new MemoryStream(testBuffer);
                storageEx = TestHelper.ExpectedException<StorageException>(
                    () => file2.DownloadRangeToStream(fileStream, 1024, 0),
                    "Requesting 0 bytes when downloading range should not work");
                Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));
                file2.DownloadRangeToStream(fileStream2, 1024, 1024);
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
        public void FileUploadFromStreamTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                file.UploadFromStream(srcStream);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                file.DownloadRangeToStream(dstStream, null, null);
                TestHelper.AssertStreamsAreEqual(srcStream, dstStream);
            }
        }

        [TestMethod]
        [Description("Upload from text to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileUploadWithoutMD5ValidationAndStoreFileContentTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);

            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            FileRequestOptions options = new FileRequestOptions();
            options.DisableContentMD5Validation = false;
            options.StoreFileContentMD5 = false;
            OperationContext context = new OperationContext();
            using (MemoryStream srcStream = new MemoryStream(buffer))
            {
                file.UploadFromStream(srcStream, null, options, context);
                file.FetchAttributes();
                string md5 = file.Properties.ContentMD5;
                file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                file.SetProperties(null, options, context);
                byte[] testBuffer = new byte[2048];
                MemoryStream dstStream = new MemoryStream(testBuffer);
                TestHelper.ExpectedException(() => file.DownloadRangeToStream(dstStream, null, null, null, options, context),
                    "Try to Download a stream with a corrupted md5 and DisableMD5Validation set to false",
                    HttpStatusCode.OK);

                options.DisableContentMD5Validation = true;
                file.SetProperties(null, options, context);
                byte[] testBuffer2 = new byte[2048];
                MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                file.DownloadRangeToStream(dstStream2, null, null, null, options, context);
            }
        }

        [Ignore]
        [TestMethod]
        [Description("Upload from text to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileEmptyHeaderSigningTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            OperationContext context = new OperationContext();
            try
            {
                share.Create(null, context);
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                context.UserHeaders = new Dictionary<string, string>();
                context.UserHeaders.Add("x-ms-foo", String.Empty);
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    file.UploadFromStream(srcStream, null, null, context);
                }

                byte[] testBuffer2 = new byte[2048];
                MemoryStream dstStream2 = new MemoryStream(testBuffer2);
                file.DownloadRangeToStream(dstStream2, null, null, null, null, context);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload from file to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadDownloadFile()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            CloudFile nullFile = this.testShare.GetRootDirectoryReference().GetFileReference("null");
            this.DoUploadDownloadFile(file, 0, false);
            this.DoUploadDownloadFile(file, 4096, false);

            TestHelper.ExpectedException<IOException>(
                () => file.UploadFromFile("non_existent.file"),
                "UploadFromFile requires an existing file");

            TestHelper.ExpectedException<StorageException>(
                () => nullFile.DownloadToFile("garbage.file", FileMode.Create),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            TestHelper.ExpectedException<StorageException>(
                () => nullFile.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream systemFile = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                systemFile.WriteAsync(buffer, 0, buffer.Length);
            }
            TestHelper.ExpectedException<IOException>(
                () => nullFile.DownloadToFile("garbage.file", FileMode.CreateNew),
                "DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            Assert.IsTrue(System.IO.File.Exists("garbage.file"));
            System.IO.File.Delete("garbage.file");

            TestHelper.ExpectedException<StorageException>(
                 () => nullFile.DownloadToFile("garbage.file", FileMode.Append),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        [TestMethod]
        [Description("Upload from file to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadDownloadFileAPM()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            CloudFile nullFile = this.testShare.GetRootDirectoryReference().GetFileReference("null");
            this.DoUploadDownloadFile(file, 0, true);
            this.DoUploadDownloadFile(file, 4096, true);

            TestHelper.ExpectedException<IOException>(
                () => file.BeginUploadFromFile("non_existent.file", null, null),
                "UploadFromFile requires an existing file");

            IAsyncResult result;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                OperationContext context = new OperationContext();
                result = nullFile.BeginDownloadToFile("garbage.file", FileMode.Create, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullFile.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullFile.BeginDownloadToFile("garbage.file", FileMode.CreateNew, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullFile.EndDownloadToFile(result),
                    "DownloadToFile should not leave an empty file behind after failing.");
                Assert.IsFalse(File.Exists("garbage.file"));

                context = new OperationContext();
                result = nullFile.BeginDownloadToFile("garbage.file", FileMode.Append, null, null, context,
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException<StorageException>(
                    () => nullFile.EndDownloadToFile(result),
                    "DownloadToFile should leave an empty file behind after failing depending on file mode.");
                Assert.IsTrue(File.Exists("garbage.file"));
                File.Delete("garbage.file");
            }
        }

#if TASK
        [TestMethod]
        [Description("Upload from file to a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadDownloadFileTask()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            CloudFile nullFile = this.testShare.GetRootDirectoryReference().GetFileReference("null");
            this.DoUploadDownloadFileTask(file, 0);
            this.DoUploadDownloadFileTask(file, 4096);

            TestHelper.ExpectedException<IOException>(
                () => file.UploadFromFileAsync("non_existent.file"),
                "UploadFromFile requires an existing file");

            AggregateException e = TestHelper.ExpectedException<AggregateException>(
                () => nullFile.DownloadToFileAsync("garbage.file", FileMode.Create).Wait(),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullFile.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait(),
                "DownloadToFile should leave an unchanged file behind after failing.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsFalse(File.Exists("garbage.file"));

            byte[] buffer = GetRandomBuffer(100);
            using (FileStream systemFile = new FileStream("garbage.file", FileMode.Create, FileAccess.Write))
            {
                systemFile.WriteAsync(buffer, 0, buffer.Length);
            }
            try
            {
                nullFile.DownloadToFileAsync("garbage.file", FileMode.CreateNew).Wait();
                Assert.Fail("DownloadToFileAsync should leave an unchanged file behind after failing, depending on the mode.");
            }
            catch (System.IO.IOException)
            {
                // Success if test reaches here meaning the expected exception was thrown.
                Assert.IsTrue(System.IO.File.Exists("garbage.file"));
                System.IO.File.Delete("garbage.file");
            }

            e = TestHelper.ExpectedException<AggregateException>(
                () => nullFile.DownloadToFileAsync("garbage.file", FileMode.Append).Wait(),
                "DownloadToFile should leave an empty file behind after failing depending on file mode.");
            Assert.IsTrue(e.InnerException is StorageException);
            Assert.IsTrue(File.Exists("garbage.file"));
            File.Delete("garbage.file");
        }

        private void DoUploadDownloadFileTask(CloudFile file, int fileSize)
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();

            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (FileStream fileStream = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(buffer, 0, buffer.Length);
                }

                file.UploadFromFileAsync(inputFileName).Wait();

                OperationContext context = new OperationContext();
                file.UploadFromFileAsync(inputFileName, null, null, context).Wait();
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                TestHelper.ExpectedException<IOException>(
                    () => file.DownloadToFileAsync(outputFileName, FileMode.CreateNew),
                    "CreateNew on an existing file should fail");

                context = new OperationContext();
                file.DownloadToFileAsync(outputFileName, FileMode.Create, null, null, context).Wait();
                Assert.IsNotNull(context.LastResult.ServiceRequestID);

                using (
                    FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                               outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqual(inputFileStream, outputFileStream);
                }

                file.DownloadToFileAsync(outputFileName, FileMode.Append).Wait();

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

        private void DoUploadDownloadFile(CloudFile file, int fileSize, bool isAsync)
        {
            string inputFileName = Path.GetTempFileName();
            string outputFileName = Path.GetTempFileName();

            try
            {
                byte[] buffer = GetRandomBuffer(fileSize);
                using (FileStream fileStream = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(buffer, 0, buffer.Length);
                }

                if (isAsync)
                {
                    IAsyncResult result;
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        result = file.BeginUploadFromFile(inputFileName,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromFile(result);

                        OperationContext context = new OperationContext();
                        result = file.BeginUploadFromFile(inputFileName, null, null, context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromFile(result);
                        Assert.IsNotNull(context.LastResult.ServiceRequestID);

                        TestHelper.ExpectedException<IOException>(
                            () => file.BeginDownloadToFile(outputFileName, FileMode.CreateNew, null, null),
                            "CreateNew on an existing file should fail");

                        context = new OperationContext();
                        result = file.BeginDownloadToFile(outputFileName, FileMode.Create, null, null, context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadToFile(result);
                        Assert.IsNotNull(context.LastResult.ServiceRequestID);

                        using (FileStream inputFile = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                            outputFile = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                        {
                            TestHelper.AssertStreamsAreEqual(inputFile, outputFile);
                        }

                        result = file.BeginDownloadToFile(outputFileName, FileMode.Append,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadToFile(result);

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
                    file.UploadFromFile(inputFileName);

                    OperationContext context = new OperationContext();
                    file.UploadFromFile(inputFileName, null, null, context);
                    Assert.IsNotNull(context.LastResult.ServiceRequestID);

                    TestHelper.ExpectedException<IOException>(
                        () => file.DownloadToFile(outputFileName, FileMode.CreateNew),
                        "CreateNew on an existing file should fail");

                    context = new OperationContext();
                    file.DownloadToFile(outputFileName, FileMode.Create, null, null, context);
                    Assert.IsNotNull(context.LastResult.ServiceRequestID);

                    using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                        outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                    {
                        TestHelper.AssertStreamsAreEqual(inputFileStream, outputFileStream);
                    }

                    file.DownloadToFile(outputFileName, FileMode.Append);

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
        [Description("Upload a file using a byte array")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromByteArray()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoUploadFromByteArrayTest(file, 4 * 512, 0, 4 * 512, false);
            this.DoUploadFromByteArrayTest(file, 4 * 512, 0, 2 * 512, false);
            this.DoUploadFromByteArrayTest(file, 4 * 512, 1 * 512, 2 * 512, false);
            this.DoUploadFromByteArrayTest(file, 4 * 512, 2 * 512, 2 * 512, false);
        }

        [TestMethod]
        [Description("Upload a file using a byte array")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromByteArrayAPM()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoUploadFromByteArrayTest(file, 4 * 512, 0, 4 * 512, true);
            this.DoUploadFromByteArrayTest(file, 4 * 512, 0, 2 * 512, true);
            this.DoUploadFromByteArrayTest(file, 4 * 512, 1 * 512, 2 * 512, true);
            this.DoUploadFromByteArrayTest(file, 4 * 512, 2 * 512, 2 * 512, true);
        }

#if TASK
        [TestMethod]
        [Description("Upload a file using a byte array")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromByteArrayTask()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoUploadFromByteArrayTestTask(file, 4 * 512, 0, 4 * 512);
            this.DoUploadFromByteArrayTestTask(file, 4 * 512, 0, 2 * 512);
            this.DoUploadFromByteArrayTestTask(file, 4 * 512, 1 * 512, 2 * 512);
            this.DoUploadFromByteArrayTestTask(file, 4 * 512, 2 * 512, 2 * 512);
        }

        private void DoUploadFromByteArrayTestTask(CloudFile file, int bufferSize, int bufferOffset, int count)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);
            byte[] downloadedBuffer = new byte[bufferSize];
            int downloadLength;

            try
            {
                file.UploadFromByteArrayAsync(buffer, bufferOffset, count).Wait();
                downloadLength = file.DownloadToByteArrayAsync(downloadedBuffer, 0).Result;
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

        private void DoUploadFromByteArrayTest(CloudFile file, int bufferSize, int bufferOffset, int count, bool isAsync)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);
            byte[] downloadedBuffer = new byte[bufferSize];
            int downloadLength;

            if (isAsync)
            {
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = file.BeginUploadFromByteArray(buffer, bufferOffset, count,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndUploadFromByteArray(result);

                    result = file.BeginDownloadToByteArray(downloadedBuffer, 0,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    downloadLength = file.EndDownloadToByteArray(result);
                }
            }
            else
            {
                file.UploadFromByteArray(buffer, bufferOffset, count);
                downloadLength = file.DownloadToByteArray(downloadedBuffer, 0);
            }

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
        public void CloudFileDownloadToByteArray()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadToByteArrayTest(file, 1 * 512, 2 * 512, 0, 0);
            this.DoDownloadToByteArrayTest(file, 1 * 512, 2 * 512, 1 * 512, 0);
            this.DoDownloadToByteArrayTest(file, 2 * 512, 4 * 512, 1 * 512, 0);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToByteArrayAPM()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadToByteArrayTest(file, 1 * 512, 2 * 512, 0, 1);
            this.DoDownloadToByteArrayTest(file, 1 * 512, 2 * 512, 1 * 512, 1);
            this.DoDownloadToByteArrayTest(file, 2 * 512, 4 * 512, 1 * 512, 1);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToByteArrayAPMOverload()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadToByteArrayTest(file, 1 * 512, 2 * 512, 0, 2);
            this.DoDownloadToByteArrayTest(file, 1 * 512, 2 * 512, 1 * 512, 2);
            this.DoDownloadToByteArrayTest(file, 2 * 512, 4 * 512, 1 * 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToByteArrayTask()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadToByteArrayTestTask(file, 1 * 512, 2 * 512, 0, false);
            this.DoDownloadToByteArrayTestTask(file, 1 * 512, 2 * 512, 1 * 512, false);
            this.DoDownloadToByteArrayTestTask(file, 2 * 512, 4 * 512, 1 * 512, false);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToByteArrayOverloadTask()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadToByteArrayTestTask(file, 1 * 512, 2 * 512, 0, true);
            this.DoDownloadToByteArrayTestTask(file, 1 * 512, 2 * 512, 1 * 512, true);
            this.DoDownloadToByteArrayTestTask(file, 2 * 512, 4 * 512, 1 * 512, true);
        }
#endif

        /// <summary>
        /// Single put file and get file
        /// </summary>
        /// <param name="fileSize">The file size.</param>
        /// <param name="bufferOffset">The file offset.</param>
        /// <param name="option"> 0 - Sunc, 1 - APM and 2 - APM overload.</param>
        private void DoDownloadToByteArrayTest(CloudFile file, int fileSize, int bufferSize, int bufferOffset, int option)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(fileSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalFile = new MemoryStream(buffer))
            {
                if (option == 0)
                {
                    file.UploadFromStream(originalFile);
                    downloadLength = file.DownloadToByteArray(resultBuffer, bufferOffset);
                }
                else if (option == 1)
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);

                        result = file.BeginDownloadToByteArray(resultBuffer,
                            bufferOffset,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        downloadLength = file.EndDownloadToByteArray(result);
                    }
                }
                else
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);

                        OperationContext context = new OperationContext();
                        result = file.BeginDownloadToByteArray(resultBuffer,
                            bufferOffset, /* offset */
                            null, /* accessCondition */
                            null, /* options */
                            context, /* operationContext */
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        downloadLength = file.EndDownloadToByteArray(result);
                    }
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

#if TASK
        /// <summary>
        /// Single put file and get file
        /// </summary>
        /// <param name="fileSize">The file size.</param>
        /// <param name="bufferOffset">The file offset.</param>
        /// <param name="option">Run with overloaded parameters.</param>
        private void DoDownloadToByteArrayTestTask(CloudFile file, int fileSize, int bufferSize, int bufferOffset, bool overload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(fileSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalFile = new MemoryStream(buffer))
            {
                file.UploadFromStreamAsync(originalFile).Wait();

                if (overload)
                {
                    downloadLength = file.DownloadToByteArrayAsync(
                        resultBuffer,
                        bufferOffset,
                        null,
                        null,
                        new OperationContext())
                            .Result;
                }
                else
                {
                    downloadLength = file.DownloadToByteArrayAsync(resultBuffer, bufferOffset).Result;
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
#endif

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToByteArray()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, null, null, 0);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 0);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 0);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 0);

            // Edge cases
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 1023, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 0, 1, 0);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 512, 1, 0);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 512, 1023, 1, 0);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 512, 0, 512, 0);

        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToByteArrayAPM()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, null, null, 1);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 1);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 1);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 1);

            // Edge cases
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 1023, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 0, 1, 1);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 512, 1, 1);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 512, 1023, 1, 1);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 512, 0, 512, 1);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToByteArrayAPMOverload()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, null, null, 2);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, 2);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, 2);
            this.DoDownloadRangeToByteArray(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, 2);

            // Edge cases
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 1023, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 0, 1, 2);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 0, 512, 1, 2);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 512, 1023, 1, 2);
            this.DoDownloadRangeToByteArray(file, 1024, 1024, 512, 0, 512, 2);
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToByteArrayTask()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 1 * 512, null, null, false);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, false);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, false);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, false);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 1023, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 0, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 0, 0, 1, false);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 0, 512, 1, false);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 512, 1023, 1, false);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 512, 0, 512, false);
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToByteArrayOverloadTask()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 0, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 1 * 512, null, null, true);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 1 * 512, 1 * 512, null, true);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 1 * 512, 0, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 1 * 512, true);
            this.DoDownloadRangeToByteArrayTask(file, 2 * 512, 4 * 512, 2 * 512, 1 * 512, 2 * 512, true);

            // Edge cases
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 1023, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 0, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 0, 0, 1, true);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 0, 512, 1, true);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 512, 1023, 1, true);
            this.DoDownloadRangeToByteArrayTask(file, 1024, 1024, 512, 0, 512, true);
        }
#endif

        /// <summary>
        /// Single put file and get file
        /// </summary>
        /// <param name="fileSize">The file size.</param>
        /// <param name="bufferSize">The output buffer size.</param>
        /// <param name="bufferOffset">The output buffer offset.</param>
        /// <param name="fileOffset">The file offset.</param>
        /// <param name="length">Length of the data range to download.</param>
        /// <param name="option">0 - Sync, 1 - APM and 2 - APM overload.</param>
        private void DoDownloadRangeToByteArray(CloudFile file, int fileSize, int bufferSize, int bufferOffset, long? fileOffset, long? length, int option)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(fileSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalFile = new MemoryStream(buffer))
            {
                if (option == 0)
                {
                    file.UploadFromStream(originalFile);
                    downloadLength = file.DownloadRangeToByteArray(resultBuffer, bufferOffset, fileOffset, length);
                }
                else if (option == 1)
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);

                        result = file.BeginDownloadRangeToByteArray(resultBuffer,
                            bufferOffset,
                            fileOffset,
                            length,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        downloadLength = file.EndDownloadRangeToByteArray(result);
                    }
                }
                else
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);

                        OperationContext context = new OperationContext();
                        result = file.BeginDownloadRangeToByteArray(resultBuffer,
                            bufferOffset,
                            fileOffset,
                            length,
                            null,
                            null,
                            context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        downloadLength = file.EndDownloadRangeToByteArray(result);
                    }
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

#if TASK
        /// <summary>
        /// Single put file and get file
        /// </summary>
        /// <param name="fileSize">The file size.</param>
        /// <param name="bufferSize">The output buffer size.</param>
        /// <param name="bufferOffset">The output buffer offset.</param>
        /// <param name="fileOffset">The file offset.</param>
        /// <param name="length">Length of the data range to download.</param>
        /// <param name="overload">Run with overloaded parameters.</param>
        private void DoDownloadRangeToByteArrayTask(CloudFile file, int fileSize, int bufferSize, int bufferOffset, long? fileOffset, long? length, bool overload)
        {
            int downloadLength;
            byte[] buffer = GetRandomBuffer(fileSize);
            byte[] resultBuffer = new byte[bufferSize];
            byte[] resultBuffer2 = new byte[bufferSize];

            using (MemoryStream originalFile = new MemoryStream(buffer))
            {
                if (overload)
                {
                    file.UploadFromStream(originalFile);
                    downloadLength = file.DownloadRangeToByteArrayAsync(
                        resultBuffer, bufferOffset, fileOffset, length, null, null, new OperationContext()).Result;
                }
                else
                {
                    file.UploadFromStream(originalFile);
                    downloadLength = file.DownloadRangeToByteArrayAsync(resultBuffer, bufferOffset, fileOffset, length).Result;
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
#endif

        #region Negative tests
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToByteArrayNegativeTests()
        {
            CloudFile file = this.testShare.GetRootDirectoryReference().GetFileReference("file1");
            this.DoDownloadRangeToByteArrayNegativeTests(file);
        }

        private void DoDownloadRangeToByteArrayNegativeTests(CloudFile file)
        {
            int fileLength = 1024;
            int resultBufSize = 1024;
            byte[] buffer = GetRandomBuffer(fileLength);
            byte[] resultBuffer = new byte[resultBufSize];

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                file.UploadFromStream(stream);

                TestHelper.ExpectedException(() => file.DownloadRangeToByteArray(resultBuffer, 0, 1024, 1), "Try invalid length", HttpStatusCode.RequestedRangeNotSatisfiable);
                StorageException ex = TestHelper.ExpectedException<StorageException>(() => file.DownloadToByteArray(resultBuffer, 1024), "Provide invalid offset");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = TestHelper.ExpectedException<StorageException>(() => file.DownloadRangeToByteArray(resultBuffer, 1023, 0, 2), "Should fail when offset + length required is greater than size of the buffer");
                Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                ex = TestHelper.ExpectedException<StorageException>(() => file.DownloadRangeToByteArray(resultBuffer, 0, 0, -10), "Fail when a negative length is specified");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(() => file.DownloadRangeToByteArray(resultBuffer, -10, 0, 20), "Fail if a negative offset is provided");
                ex = TestHelper.ExpectedException<StorageException>(() => file.DownloadRangeToByteArray(resultBuffer, 0, -10, 20), "Fail if a negative file offset is provided");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException));
            }
        }
        #endregion
    }
}