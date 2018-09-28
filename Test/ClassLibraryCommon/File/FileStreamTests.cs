// -----------------------------------------------------------------------------------------
// <copyright file="FileOutputStreamTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileOutputStreamTests : FileTestBase
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
        [Description("FileSeek")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileSeekTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    file.UploadFromStream(srcStream);
                    Stream fileStream = file.OpenRead();
                    fileStream.Seek(2048, 0);
                    byte[] buff = new byte[100];
                    int numRead = fileStream.Read(buff, 0, 100);
                    Assert.AreEqual(numRead, 0);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }       
        }

        [TestMethod]
        [Description("OpenWrite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileOpenWriteTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (CloudFileStream fileStream = file.OpenWrite(2048))
                {
                    fileStream.Write(buffer, 0, 2048);
                    fileStream.Flush();

                    byte[] testBuffer = new byte[2048];
                    MemoryStream dstStream = new MemoryStream(testBuffer);
                    file.DownloadRangeToStream(dstStream, null, null);

                    MemoryStream memStream = new MemoryStream(buffer);
                    TestHelper.AssertStreamsAreEqual(memStream, dstStream);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("OpenRead")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileOpenReadTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    file.UploadFromStream(srcStream);

                    Stream dstStream = file.OpenRead();
                    TestHelper.AssertStreamsAreEqual(srcStream, dstStream);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("OpenReadWrite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileOpenReadWriteTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");

                Stream fileStream = file.OpenWrite(2048);
                fileStream.Write(buffer, 0, 2048);
                fileStream.Close();

                MemoryStream memoryStream = new MemoryStream(buffer);
                Stream dstStream = file.OpenRead();
                TestHelper.AssertStreamsAreEqual(memoryStream, dstStream);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("OpenWriteSeekRead")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileOpenWriteSeekReadTest()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                MemoryStream memoryStream = new MemoryStream(buffer);
                Stream fileStream = file.OpenWrite(2048);
                fileStream.Write(buffer, 0, 2048);

                Assert.AreEqual(fileStream.Position, 2048);

                fileStream.Seek(1024, 0);
                memoryStream.Seek(1024, 0);
                Assert.AreEqual(fileStream.Position, 1024);

                byte[] testBuffer = GetRandomBuffer(1024);

                memoryStream.Write(testBuffer, 0, 1024);
                fileStream.Write(testBuffer, 0, 1024);
                Assert.AreEqual(fileStream.Position, memoryStream.Position);

                fileStream.Close();

                Stream dstStream = file.OpenRead();
                TestHelper.AssertStreamsAreEqual(memoryStream, dstStream);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Read when opened in OpenWrite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileReadWhenOpenWrite()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                MemoryStream memoryStream = new MemoryStream(buffer);
                Stream fileStream = file.OpenWrite(2048);
                fileStream.Write(buffer, 0, 2048);
                byte[] testBuffer = new byte[2048];
                TestHelper.ExpectedException<NotSupportedException>(() => fileStream.Read(testBuffer, 0, 2048),
                        "Try reading from a stream opened for Write");
                
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Write when opened in OpenRead")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileWriteWhenOpenRead()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    file.UploadFromStream(srcStream);
                    Stream fileStream = file.OpenRead();
                   
                    byte[] testBuffer = new byte[2048];
                    TestHelper.ExpectedException<NotSupportedException>(() => fileStream.Write(testBuffer, 0, 2048), 
                            "Try writing to a stream opened for read");
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
    }
}
