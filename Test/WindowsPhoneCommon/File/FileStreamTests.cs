// -----------------------------------------------------------------------------------------
// <copyright file="FileStreamTests.cs" company="Microsoft">
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
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileStreamTests : FileTestBase
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
        public async Task FileSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream srcStream = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(srcStream, null, null, null);
                    using (Stream fileStream = await file.OpenReadAsync())
                    {
                        Stream fileStreamForRead = fileStream;
                        fileStreamForRead.Seek(2048, 0);
                        byte[] buff = new byte[100];
                        int numRead = await fileStreamForRead.ReadAsync(buff, 0, 100);
                        Assert.AreEqual(numRead, 0);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("OpenWrite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileOpenWriteTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (CloudFileStream fileStream = await file.OpenWriteAsync(2048))
                {
                    Stream fileStreamForWrite = fileStream;
                    await fileStreamForWrite.WriteAsync(buffer, 0, 2048);
                    await fileStreamForWrite.FlushAsync();

                    byte[] testBuffer = new byte[2048];
                    MemoryStream dstStream = new MemoryStream(testBuffer);
                    await file.DownloadRangeToStreamAsync(dstStream, null, null);
                    
                    MemoryStream memStream = new MemoryStream(buffer);
                    TestHelper.AssertStreamsAreEqual(memStream, dstStream);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("OpenRead")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileOpenReadTestAsync()
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

                    Stream dstStream = await file.OpenReadAsync();
                    using (Stream dstStreamForRead = dstStream)
                    {
                        TestHelper.AssertStreamsAreEqual(srcStream, dstStreamForRead);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("OpenReadWrite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileOpenReadWriteTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                
                using (CloudFileStream fileStream = await file.OpenWriteAsync(2048))
                {
                    Stream fileStreamForWrite = fileStream;
                    await fileStreamForWrite.WriteAsync(buffer, 0, 2048);
                    await fileStreamForWrite.FlushAsync();
                }

                using (Stream dstStream = await file.OpenReadAsync())
                {
                    Stream dstStreamForRead = dstStream;
                    MemoryStream memoryStream = new MemoryStream(buffer);
                    TestHelper.AssertStreamsAreEqual(memoryStream, dstStreamForRead);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("OpenWriteSeekRead")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileOpenWriteSeekReadTestAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");

                MemoryStream memoryStream = new MemoryStream(buffer);
                using (CloudFileStream fileStream = await file.OpenWriteAsync(2048))
                {
                    Stream fileStreamForWrite = fileStream;
                    await fileStreamForWrite.WriteAsync(buffer, 0, 2048);

                    Assert.AreEqual(fileStreamForWrite.Position, 2048);

                    fileStreamForWrite.Seek(1024, 0);
                    memoryStream.Seek(1024, 0);
                    Assert.AreEqual(fileStreamForWrite.Position, 1024);

                    byte[] testBuffer = GetRandomBuffer(1024);

                    await memoryStream.WriteAsync(testBuffer, 0, 1024);
                    await fileStreamForWrite.WriteAsync(testBuffer, 0, 1024);
                    Assert.AreEqual(fileStreamForWrite.Position, memoryStream.Position);

                    await fileStreamForWrite.FlushAsync();
                }

                using (Stream dstStream = await file.OpenReadAsync())
                {
                    Stream dstStreamForRead = dstStream;
                    TestHelper.AssertStreamsAreEqual(memoryStream, dstStreamForRead);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Read when opened in OpenWrite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileReadWhenOpenWriteAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024);
            bool thrown = false;
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                MemoryStream memoryStream = new MemoryStream(buffer);
                using (Stream fileStream = await file.OpenWriteAsync(2048))
                {
                    Stream fileStreamForWrite = fileStream;
                    await fileStreamForWrite.WriteAsync(buffer, 0, 2048);
                    byte[] testBuffer = new byte[2048];
                    try
                    {
                        await fileStreamForWrite.ReadAsync(testBuffer, 0, 2048);
                    }
                    catch (NotSupportedException)
                    {
                        thrown = true;
                    }

                    Assert.IsTrue(thrown);
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Write when opened in OpenRead")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteWhenOpenReadAsync()
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
                    bool thrown = false;
                    byte[] testBuffer = new byte[2048];
                    using (Stream fileStream = await file.OpenReadAsync())
                    {
                        Stream fileStreamForRead = fileStream;
                        try
                        {
                            await fileStreamForRead.WriteAsync(testBuffer, 0, 2048);
                        }
                        catch (NotSupportedException)
                        {
                            thrown = true;
                        }

                        Assert.IsTrue(thrown);
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

