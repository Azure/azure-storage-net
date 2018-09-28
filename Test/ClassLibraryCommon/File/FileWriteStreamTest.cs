// -----------------------------------------------------------------------------------------
// <copyright file="FileWriteStreamTest.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileWriteStreamTest : FileTestBase
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
        [Description("Create files using file stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamOpenAndClose()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                TestHelper.ExpectedException(
                    () => file.OpenWrite(null),
                    "Opening a file stream with no size should fail on a file that does not exist",
                    HttpStatusCode.NotFound);
                using (Stream fileStream = file.OpenWrite(1024))
                {
                }
                using (Stream fileStream = file.OpenWrite(null))
                {
                }

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file");
                file2.FetchAttributes();
                Assert.AreEqual(1024, file2.Properties.Length);
            }
            finally
            {
                share.Delete();
            }
        }

        /*
        [TestMethod]
        [Description("Create a file using file stream by specifying an access condition")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamOpenWithAccessCondition()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFile existingFile = share.GetRootDirectoryReference().GetFileReference("file");
                existingFile.Create(1024);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                TestHelper.ExpectedException(
                    () => file.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                Stream fileStream = file.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                fileStream = file.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = file.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = file.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                fileStream = existingFile.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingFile.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                fileStream = existingFile.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingFile.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                TestHelper.ExpectedException(
                    () => existingFile.OpenWrite(1024, accessCondition),
                    "FileWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = existingFile.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                TestHelper.ExpectedException(
                    () => existingFile.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = existingFile.OpenWrite(1024, accessCondition);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                TestHelper.ExpectedException(
                    () => existingFile.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                share.Delete();
            }
        }
        
        [TestMethod]
        [Description("Create a file using file stream by specifying an access condition")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamOpenAPMWithAccessCondition()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFile existingFile = share.GetRootDirectoryReference().GetFileReference("file");
                existingFile.Create(1024);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                    AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                    IAsyncResult result = file.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => file.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    file = share.GetRootDirectoryReference().GetFileReference("file3");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                    result = file.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Stream fileStream = file.EndOpenWrite(result);
                    fileStream.Dispose();

                    file = share.GetRootDirectoryReference().GetFileReference("file4");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = file.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = file.EndOpenWrite(result);
                    fileStream.Dispose();

                    file = share.GetRootDirectoryReference().GetFileReference("file5");
                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                    result = file.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = file.EndOpenWrite(result);
                    fileStream.Dispose();

                    file = share.GetRootDirectoryReference().GetFileReference("file6");
                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                    result = file.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = file.EndOpenWrite(result);
                    fileStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = existingFile.EndOpenWrite(result);
                    fileStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingFile.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = existingFile.EndOpenWrite(result);
                    fileStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingFile.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingFile.EndOpenWrite(result),
                        "FileWriteStream.Dispose with a non-met condition should fail",
                        HttpStatusCode.Conflict);

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = existingFile.EndOpenWrite(result);
                    fileStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingFile.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    fileStream = existingFile.EndOpenWrite(result);
                    fileStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingFile.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingFile.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);
                }
            }
            finally
            {
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a file using file stream by specifying an access condition")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamOpenWithAccessConditionTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();

            try
            {
                CloudFile existingFile = share.GetRootDirectoryReference().GetFileReference("file");
                existingFile.CreateAsync(1024).Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                TestHelper.ExpectedExceptionTask(
                    file.OpenWriteAsync(1024, accessCondition, null, null),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                Stream fileStream = file.OpenWriteAsync(1024, accessCondition, null, null).Result;
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                fileStream = file.OpenWriteAsync(1024, accessCondition, null, null).Result;
                fileStream.Dispose();
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
#endif
        */

        [TestMethod]
        [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamBasicTest()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);

            MD5 hasher = MD5.Create();
            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };

                    using (Stream fileStream = file.OpenWrite(buffer.Length * 3, null, options))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            fileStream.Write(buffer, 0, buffer.Length);
                            wholeFile.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }
                    }

                    wholeFile.Seek(0, SeekOrigin.Begin);
                    string md5 = Convert.ToBase64String(hasher.ComputeHash(wholeFile));
                    file.FetchAttributes();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStream(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }

                    TestHelper.ExpectedException<ArgumentException>(
                        () => file.OpenWrite(null, null, options),
                        "OpenWrite with StoreFileContentMD5 on an existing file should fail");

                    using (Stream fileStream = file.OpenWrite(null))
                    {
                        fileStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeFile.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            fileStream.Write(buffer, 0, buffer.Length);
                            wholeFile.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }

                        wholeFile.Seek(0, SeekOrigin.End);
                    }

                    file.FetchAttributes();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        options.DisableContentMD5Validation = true;
                        file.DownloadToStream(downloadedFile, null, options);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamOneByteTest()
        {
            byte buffer = 127;

            MD5 hasher = MD5.Create();
            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 16 * 1024;

                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };

                    using (Stream fileStream = file.OpenWrite(1 * 1024 * 1024, null, options))
                    {
                        for (int i = 0; i < 1 * 1024 * 1024; i++)
                        {
                            fileStream.WriteByte(buffer);
                            wholeFile.WriteByte(buffer);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }
                    }

                    wholeFile.Seek(0, SeekOrigin.Begin);
                    string md5 = Convert.ToBase64String(hasher.ComputeHash(wholeFile));
                    file.FetchAttributes();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStream(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }


        [TestMethod]
        [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamBasicTestAPM()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            MD5 hasher = MD5.Create();
            CloudFileClient fileClient = GenerateCloudFileClient();
            fileClient.DefaultRequestOptions.ParallelOperationThreadCount = 4;
            string name = GetRandomShareName();
            CloudFileShare share = fileClient.GetShareReference(name);

            try
            {
                ServicePointManager.DefaultConnectionLimit = fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value;
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = buffer.Length;

                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };

                    IAsyncResult result;
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                         result = file.BeginOpenWrite(fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value * 2 * buffer.Length, null, options, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        using (CloudFileStream fileStream = file.EndOpenWrite(result))
                        {

                            IAsyncResult[] results = new IAsyncResult[fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value * 2];
                            for (int i = 0; i < results.Length; i++)
                            {
                                results[i] = fileStream.BeginWrite(buffer, 0, buffer.Length, null, null);
                                wholeFile.Write(buffer, 0, buffer.Length);
                               // fileStream.EndWrite(results[i]);
                                Assert.AreEqual(wholeFile.Position, fileStream.Position);
                            }

                            for (int i = 0; i < fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i++)
                            {
                                Assert.IsTrue(results[i].IsCompleted);
                            }

                            for (int i = fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i < results.Length; i++)
                            {
                                Assert.IsFalse(results[i].IsCompleted);
                            }

                            for (int i = 0; i < results.Length; i++)
                            {
                                fileStream.EndWrite(results[i]);
                            }

                            result = fileStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            fileStream.EndCommit(result);
                        }
                    }

                    wholeFile.Seek(0, SeekOrigin.Begin);
                    string md5 = Convert.ToBase64String(hasher.ComputeHash(wholeFile));
                    file.FetchAttributes();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStream(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }

                    fileClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        result = file.BeginOpenWrite(null, null, options, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException<ArgumentException>(
                            () => file.EndOpenWrite(result),
                            "BeginOpenWrite with StoreFileContentMD5 on an existing file should fail");
                         result = file.BeginOpenWrite(null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        using (Stream fileStream = file.EndOpenWrite(result))
                        {
                            fileStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                            wholeFile.Seek(buffer.Length / 2, SeekOrigin.Begin);

                            IAsyncResult[] results = new IAsyncResult[fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value * 2];
                            for (int i = 0; i < results.Length; i++)
                            {
                                results[i] = fileStream.BeginWrite(buffer, 0, buffer.Length, null, null);
                                wholeFile.Write(buffer, 0, buffer.Length);
                                Assert.AreEqual(wholeFile.Position, fileStream.Position);
                            }

                            for (int i = 0; i < fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i++)
                            {
                                Assert.IsTrue(results[i].IsCompleted);
                            }

                            for (int i = fileClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i < results.Length; i++)
                            {
                                Assert.IsFalse(results[i].IsCompleted);
                            }

                            for (int i = 0; i < results.Length; i++)
                            {
                                fileStream.EndWrite(results[i]);
                            }

                            wholeFile.Seek(0, SeekOrigin.End);
                        }

                        file.FetchAttributes();
                        Assert.AreEqual(md5, file.Properties.ContentMD5);

                        using (MemoryStream downloadedFile = new MemoryStream())
                        {
                            options.DisableContentMD5Validation = true;
                            file.DownloadToStream(downloadedFile, null, options);
                            TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                        }
                    }

                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Seek in a file write stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamRandomSeekTest()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    using (Stream fileStream = file.OpenWrite(buffer.Length))
                    {
                        fileStream.Write(buffer, 0, buffer.Length);
                        wholeFile.Write(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length);
                            TestHelper.SeekRandomly(fileStream, offset);
                            fileStream.Write(buffer, 0, buffer.Length - offset);
                            wholeFile.Seek(offset, SeekOrigin.Begin);
                            wholeFile.Write(buffer, 0, buffer.Length - offset);
                        }
                    }

                    file.FetchAttributes();
                    Assert.IsNull(file.Properties.ContentMD5);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStream(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of file stream's flush functionality")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamFlushTest()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions() { StoreFileContentMD5 = true };
                    OperationContext opContext = new OperationContext();
                    using (CloudFileStream fileStream = file.OpenWrite(4 * 512, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            fileStream.Write(buffer, 0, buffer.Length);
                            wholeFile.Write(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        fileStream.Flush();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        fileStream.Flush();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        fileStream.Write(buffer, 0, buffer.Length);
                        wholeFile.Write(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        fileStream.Commit();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStream(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of file stream's flush functionality")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteStreamFlushTestAPM()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions() { StoreFileContentMD5 = true };
                    OperationContext opContext = new OperationContext();
                    using (CloudFileStream fileStream = file.OpenWrite(4 * buffer.Length, null, options, opContext))
                    {
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            IAsyncResult result;
                            for (int i = 0; i < 3; i++)
                            {
                                result = fileStream.BeginWrite(
                                    buffer,
                                    0,
                                    buffer.Length,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                fileStream.EndWrite(result);
                                wholeFile.Write(buffer, 0, buffer.Length);
                            }

                            Assert.AreEqual(2, opContext.RequestResults.Count);

                            ICancellableAsyncResult cancellableResult = fileStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            Assert.IsFalse(cancellableResult.IsCompleted);
                            cancellableResult.Cancel();
                            waitHandle.WaitOne();
                            fileStream.EndFlush(cancellableResult);

                            result = fileStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            fileStream.EndFlush(result);
                            //In the new Async Flush we will not throw in case of multiple flushes
                            fileStream.BeginFlush(ar => waitHandle.Set(), null);
                            waitHandle.WaitOne();
                            result = fileStream.BeginFlush(null, null);
                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = fileStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            fileStream.EndFlush(result);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = fileStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            fileStream.EndWrite(result);
                            wholeFile.Write(buffer, 0, buffer.Length);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = fileStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            fileStream.EndCommit(result);

                            Assert.AreEqual(5, opContext.RequestResults.Count);
                        }
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStream(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        /// [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamRandomSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    using (var writeStream = await file.OpenWriteAsync(buffer.Length))
                    {
                        Stream fileStream = writeStream;
                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length);
                            TestHelper.SeekRandomly(fileStream, offset);
                            await fileStream.WriteAsync(buffer, 0, buffer.Length - offset);
                            wholeFile.Seek(offset, SeekOrigin.Begin);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length - offset);
                        }
                    }

                    wholeFile.Seek(0, SeekOrigin.End);
                    await file.FetchAttributesAsync();
                    Assert.IsNull(file.Properties.ContentMD5);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
        
       
        [TestMethod]
        /// [Description("Create files using file stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamOpenAndCloseAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenWriteAsync(null, null, null, opContext),
                    opContext,
                    "Opening a file stream with no size should fail on a file that does not exist",
                    HttpStatusCode.NotFound);
                using (var writeStream = await file.OpenWriteAsync(1024))
                {
                }
                using (var writeStream = await file.OpenWriteAsync(null))
                {
                }

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1024, file2.Properties.Length);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        /*
        [TestMethod]
        /// [Description("Create a file using file stream by specifying an access condition")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamOpenWithAccessConditionAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudFile existingFile = share.GetRootDirectoryReference().GetFileReference("file");
                await existingFile.CreateAsync(1024);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                IOutputStream fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "FileWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
        */

        [TestMethod]
        /// [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);

            MD5 hasher = MD5.Create();

            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };
                    using (var writeStream = await file.OpenWriteAsync(buffer.Length * 3, null, options, null))
                    {
                        Stream fileStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await fileStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);

                        }

                        await fileStream.FlushAsync();
                    }

                    string md5 = Convert.ToBase64String(hasher.ComputeHash(wholeFile.ToArray()));

                    await file.FetchAttributesAsync();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }

                    await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await file.OpenWriteAsync(null, null, options, null),
                        "OpenWrite with StoreFileContentMD5 on an existing file should fail");

                    using (var writeStream = await file.OpenWriteAsync(null))
                    {
                        Stream fileStream = writeStream;
                        fileStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeFile.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            fileStream.Write(buffer, 0, buffer.Length);
                            wholeFile.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }

                        await fileStream.FlushAsync();
                    }

                    await file.FetchAttributesAsync();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        options.DisableContentMD5Validation = true;
                        await file.DownloadToStreamAsync(downloadedFile, null, options, null);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }
                }
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        /// [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions() { StoreFileContentMD5 = true };
                    OperationContext opContext = new OperationContext();
                    using (var fileStream = await file.OpenWriteAsync(4 * 512, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await fileStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                        }
                        // todo: Make some other better logic for this test to be reliable.
                        System.Threading.Thread.Sleep(500);
                        Task.Delay(500).GetAwaiter().GetResult();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await fileStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeFile.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.CommitAsync();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }


    }
}
