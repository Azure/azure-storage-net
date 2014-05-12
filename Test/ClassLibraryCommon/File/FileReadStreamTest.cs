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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileReadStreamTest : FileTestBase
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
        [Description("Download a file using CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileReadStreamBasicTest()
        {
            byte[] buffer = GetRandomBuffer(5 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    file.UploadFromStream(wholeFile);
                }

                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    using (Stream fileStream = file.OpenRead())
                    {
                        TestHelper.AssertStreamsAreEqual(wholeFile, fileStream);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Modify a file while downloading it using CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileReadLockToETagTest()
        {
            byte[] outBuffer = new byte[1 * 1024 * 1024];
            byte[] buffer = GetRandomBuffer(2 * outBuffer.Length);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamMinimumReadSizeInBytes = outBuffer.Length;
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    file.UploadFromStream(wholeFile);
                }

                using (Stream fileStream = file.OpenRead())
                {
                    fileStream.Read(outBuffer, 0, outBuffer.Length);
                    file.SetMetadata();
                    TestHelper.ExpectedException(
                        () => fileStream.Read(outBuffer, 0, outBuffer.Length),
                        "File read stream should fail if file is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                using (Stream fileStream = file.OpenRead())
                {
                    long length = fileStream.Length;
                    file.SetMetadata();
                    TestHelper.ExpectedException(
                        () => fileStream.Read(outBuffer, 0, outBuffer.Length),
                        "File read stream should fail if file is modified during read",
                        HttpStatusCode.PreconditionFailed);
                }

                /*
                AccessCondition accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1)));
                file.SetMetadata();
                TestHelper.ExpectedException(
                    () => file.OpenRead(accessCondition),
                    "File read stream should fail if file is modified during read",
                    HttpStatusCode.PreconditionFailed);
                 */
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Modify a file while downloading it using CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileReadLockToETagTestAPM()
        {
            byte[] outBuffer = new byte[1 * 1024 * 1024];
            byte[] buffer = GetRandomBuffer(2 * outBuffer.Length);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamMinimumReadSizeInBytes = outBuffer.Length;
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    file.UploadFromStream(wholeFile);
                }

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = file.BeginOpenRead(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    using (Stream fileStream = file.EndOpenRead(result))
                    {
                        fileStream.Read(outBuffer, 0, outBuffer.Length);
                        file.SetMetadata();
                        TestHelper.ExpectedException(
                            () => fileStream.Read(outBuffer, 0, outBuffer.Length),
                            "File read stream should fail if file is modified during read",
                            HttpStatusCode.PreconditionFailed);
                    }

                    result = file.BeginOpenRead(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    using (Stream fileStream = file.EndOpenRead(result))
                    {
                        long length = fileStream.Length;
                        file.SetMetadata();
                        TestHelper.ExpectedException(
                            () => fileStream.Read(outBuffer, 0, outBuffer.Length),
                            "File read stream should fail if file is modified during read",
                            HttpStatusCode.PreconditionFailed);
                    }

                    /*
                    AccessCondition accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1)));
                    file.SetMetadata();
                    result = file.BeginOpenRead(
                        accessCondition,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => file.EndOpenRead(result),
                        "File read stream should fail if file is modified during read",
                        HttpStatusCode.PreconditionFailed);
                     */
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        private static int FileReadStreamSeekAndCompare(Stream fileStream, byte[] bufferToCompare, long offset, int readSize, int expectedReadCount, bool isAsync)
        {
            byte[] testBuffer = new byte[readSize];

            if (isAsync)
            {
                using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                {
                    IAsyncResult result = fileStream.BeginRead(testBuffer, 0, readSize, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    int readCount = fileStream.EndRead(result);
                    Assert.AreEqual(expectedReadCount, readCount);
                }
            }
            else
            {
                int readCount = fileStream.Read(testBuffer, 0, readSize);
                Assert.AreEqual(expectedReadCount, readCount);
            }

            for (int i = 0; i < expectedReadCount; i++)
            {
                Assert.AreEqual(bufferToCompare[i + offset], testBuffer[i]);
            }

            return expectedReadCount;
        }

        private static int FileReadStreamSeekTest(Stream fileStream, long streamReadSize, byte[] bufferToCompare, bool isAsync)
        {
            int attempts = 1;
            long position = 0;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 1024, isAsync);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 512, 512, isAsync);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-128, SeekOrigin.End);
            position = bufferToCompare.Length - 128;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 128, isAsync);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(4096, SeekOrigin.Begin);
            position = 4096;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 1024, isAsync);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(4096, SeekOrigin.Current);
            position += 4096;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 1024, isAsync);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-4096, SeekOrigin.Current);
            position -= 4096;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 128, 128, isAsync);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(streamReadSize + 4096 - 512, SeekOrigin.Begin);
            position = streamReadSize + 4096 - 512;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 512, isAsync);
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 1024, isAsync);
            attempts++;
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-1024, SeekOrigin.Current);
            position -= 1024;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 2048, 2048, isAsync);
            Assert.AreEqual(position, fileStream.Position);
            fileStream.Seek(-128, SeekOrigin.End);
            position = bufferToCompare.Length - 128;
            Assert.AreEqual(position, fileStream.Position);
            position += FileReadStreamSeekAndCompare(fileStream, bufferToCompare, position, 1024, 128, isAsync);
            Assert.AreEqual(position, fileStream.Position);
            return attempts;
        }

        [TestMethod]
        [Description("Seek and read in a CloudFileStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileReadStreamSeekTest()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamMinimumReadSizeInBytes = 2 * 1024 * 1024;
                using (MemoryStream wholeFile = new MemoryStream(buffer))
                {
                    file.UploadFromStream(wholeFile);
                }

                OperationContext opContext = new OperationContext();
                using (Stream fileStream = file.OpenRead(null, null, opContext))
                {
                    int attempts = FileReadStreamSeekTest(fileStream, file.StreamMinimumReadSizeInBytes, buffer, false);
                    TestHelper.AssertNAttempts(opContext, attempts);
                }

                opContext = new OperationContext();
                using (Stream fileStream = file.OpenRead(null, null, opContext))
                {
                    int attempts = FileReadStreamSeekTest(fileStream, file.StreamMinimumReadSizeInBytes, buffer, true);
                    TestHelper.AssertNAttempts(opContext, attempts);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
    }
}
