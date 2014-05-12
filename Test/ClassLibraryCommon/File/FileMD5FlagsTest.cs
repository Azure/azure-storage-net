// -----------------------------------------------------------------------------------------
// <copyright file="FileMD5FlagsTest.cs" company="Microsoft">
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
using System.Security.Cryptography;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileMD5FlagsTest : FileTestBase
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
        [Description("Test StoreFileContentMD5 flag with UploadFromStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileStoreContentMD5Test()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                StoreFileContentMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                StoreFileContentMD5 = true,
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file4");
                using (Stream stream = new MemoryStream())
                {
                    file.UploadFromStream(stream, null, optionsWithMD5);
                }
                file.FetchAttributes();
                Assert.IsNotNull(file.Properties.ContentMD5);

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                using (Stream stream = new MemoryStream())
                {
                    file.UploadFromStream(stream, null, optionsWithNoMD5);
                }
                file.FetchAttributes();
                Assert.IsNull(file.Properties.ContentMD5);

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                using (Stream stream = new MemoryStream())
                {
                    file.UploadFromStream(stream);
                }
                file.FetchAttributes();
                Assert.IsNull(file.Properties.ContentMD5);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test StoreFileContentMD5 flag with UploadFromStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileStoreContentMD5TestAPM()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                StoreFileContentMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                StoreFileContentMD5 = true,
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file4");
                    using (Stream stream = new MemoryStream())
                    {
                        result = file.BeginUploadFromStream(stream, null, optionsWithMD5, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);
                    }
                    file.FetchAttributes();
                    Assert.IsNotNull(file.Properties.ContentMD5);

                    file = share.GetRootDirectoryReference().GetFileReference("file5");
                    using (Stream stream = new MemoryStream())
                    {
                        result = file.BeginUploadFromStream(stream, null, optionsWithNoMD5, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);
                    }
                    file.FetchAttributes();
                    Assert.IsNull(file.Properties.ContentMD5);

                    file = share.GetRootDirectoryReference().GetFileReference("file6");
                    using (Stream stream = new MemoryStream())
                    {
                        result = file.BeginUploadFromStream(stream,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);
                    }
                    file.FetchAttributes();
                    Assert.IsNull(file.Properties.ContentMD5);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test DisableContentMD5Validation flag with DownloadToStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDisableContentMD5ValidationTest()
        {
            byte[] buffer = new byte[1024];
            Random random = new Random();
            random.NextBytes(buffer);

            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                DisableContentMD5Validation = true,
                StoreFileContentMD5 = true,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                DisableContentMD5Validation = false,
                StoreFileContentMD5 = true,
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                using (Stream stream = new MemoryStream(buffer))
                {
                    file.UploadFromStream(stream, null, optionsWithMD5);
                }

                using (Stream stream = new MemoryStream())
                {
                    file.DownloadToStream(stream, null, optionsWithMD5);
                    file.DownloadToStream(stream, null, optionsWithNoMD5);

                    using (Stream fileStream = file.OpenRead(null, optionsWithMD5))
                    {
                        int read;
                        do
                        {
                            read = fileStream.Read(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    using (Stream fileStream = file.OpenRead(null, optionsWithNoMD5))
                    {
                        int read;
                        do
                        {
                            read = fileStream.Read(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    file.SetProperties();

                    TestHelper.ExpectedException(
                        () => file.DownloadToStream(stream, null, optionsWithMD5),
                        "Downloading a file with invalid MD5 should fail",
                        HttpStatusCode.OK);
                    file.DownloadToStream(stream, null, optionsWithNoMD5);

                    using (Stream fileStream = file.OpenRead(null, optionsWithMD5))
                    {
                        TestHelper.ExpectedException<IOException>(
                            () =>
                            {
                                int read;
                                do
                                {
                                    read = fileStream.Read(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            },
                            "Downloading a file with invalid MD5 should fail");
                    }

                    using (Stream fileStream = file.OpenRead(null, optionsWithNoMD5))
                    {
                        int read;
                        do
                        {
                            read = fileStream.Read(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test DisableContentMD5Validation flag with DownloadToStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDisableContentMD5ValidationTestAPM()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                DisableContentMD5Validation = true,
                StoreFileContentMD5 = true,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                DisableContentMD5Validation = false,
                StoreFileContentMD5 = true,
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                    using (Stream stream = new MemoryStream())
                    {
                        file.UploadFromStream(stream, null, optionsWithMD5);
                    }

                    using (Stream stream = new MemoryStream())
                    {
                        result = file.BeginDownloadToStream(stream, null, optionsWithMD5, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadToStream(result);
                        result = file.BeginDownloadToStream(stream, null, optionsWithNoMD5, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadToStream(result);

                        file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                        file.SetProperties();

                        result = file.BeginDownloadToStream(stream, null, optionsWithMD5, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => file.EndDownloadToStream(result),
                            "Downloading a file with invalid MD5 should fail",
                            HttpStatusCode.OK);
                        result = file.BeginDownloadToStream(stream, null, optionsWithNoMD5, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadToStream(result);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalMD5 flag with WriteRange")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileUseTransactionalMD5PutTest()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = true,
            };

            byte[] buffer = GetRandomBuffer(1024);
            MD5 hasher = MD5.Create();
            string md5 = Convert.ToBase64String(hasher.ComputeHash(buffer));

            string lastCheckMD5 = null;
            int checkCount = 0;
            OperationContext opContextWithMD5Check = new OperationContext();
            opContextWithMD5Check.SendingRequest += (_, args) =>
            {
                if (args.Request.ContentLength >= buffer.Length)
                {
                    lastCheckMD5 = args.Request.Headers[HttpRequestHeader.ContentMd5];
                    checkCount++;
                }
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                file.Create(buffer.Length);
                checkCount = 0;
                using (Stream fileData = new MemoryStream(buffer))
                {
                    file.WriteRange(fileData, 0, null, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    fileData.Seek(0, SeekOrigin.Begin);
                    file.WriteRange(fileData, 0, null, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    fileData.Seek(0, SeekOrigin.Begin);
                    file.WriteRange(fileData, 0, md5, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);
                }
                Assert.AreEqual(3, checkCount);

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                checkCount = 0;
                using (Stream fileStream = file.OpenWrite(buffer.Length * 3, null, optionsWithMD5, opContextWithMD5Check))
                {
                    fileStream.Write(buffer, 0, buffer.Length);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNotNull(lastCheckMD5);
                Assert.AreEqual(1, checkCount);

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                checkCount = 0;
                using (Stream fileStream = file.OpenWrite(buffer.Length * 3, null, optionsWithNoMD5, opContextWithMD5Check))
                {
                    fileStream.Write(buffer, 0, buffer.Length);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNull(lastCheckMD5);
                Assert.AreEqual(1, checkCount);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalMD5 flag with WriteRange")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileUseTransactionalMD5PutTestAPM()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = true,
            };

            byte[] buffer = GetRandomBuffer(1024);
            MD5 hasher = MD5.Create();
            string md5 = Convert.ToBase64String(hasher.ComputeHash(buffer));

            string lastCheckMD5 = null;
            int checkCount = 0;
            OperationContext opContextWithMD5Check = new OperationContext();
            opContextWithMD5Check.SendingRequest += (_, args) =>
            {
                if (args.Request.ContentLength >= buffer.Length)
                {
                    lastCheckMD5 = args.Request.Headers[HttpRequestHeader.ContentMd5];
                    checkCount++;
                }
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                    file.Create(buffer.Length);
                    checkCount = 0;
                    using (Stream fileData = new MemoryStream(buffer))
                    {
                        result = file.BeginWriteRange(fileData, 0, null, null, optionsWithNoMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndWriteRange(result);
                        Assert.IsNull(lastCheckMD5);

                        fileData.Seek(0, SeekOrigin.Begin);
                        result = file.BeginWriteRange(fileData, 0, null, null, optionsWithMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndWriteRange(result);
                        Assert.AreEqual(md5, lastCheckMD5);

                        fileData.Seek(0, SeekOrigin.Begin);
                        result = file.BeginWriteRange(fileData, 0, md5, null, optionsWithNoMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndWriteRange(result);
                        Assert.AreEqual(md5, lastCheckMD5);
                    }
                    Assert.AreEqual(3, checkCount);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalMD5 flag with DownloadRangeToStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileUseTransactionalMD5GetTest()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = true,
            };

            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            MD5 hasher = MD5.Create();
            string md5 = Convert.ToBase64String(hasher.ComputeHash(buffer));

            string lastCheckMD5 = null;
            int checkCount = 0;
            OperationContext opContextWithMD5Check = new OperationContext();
            opContextWithMD5Check.ResponseReceived += (_, args) =>
            {
                if (args.Response.ContentLength >= buffer.Length)
                {
                    lastCheckMD5 = args.Response.Headers[HttpResponseHeader.ContentMd5];
                    checkCount++;
                }
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                using (Stream fileStream = file.OpenWrite(buffer.Length * 2))
                {
                    fileStream.Write(buffer, 0, buffer.Length);
                    fileStream.Write(buffer, 0, buffer.Length);
                }

                checkCount = 0;
                using (Stream stream = new MemoryStream())
                {
                    file.DownloadToStream(stream, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    StorageException storageEx = TestHelper.ExpectedException<StorageException>(
                        () => file.DownloadToStream(stream, null, optionsWithMD5, opContextWithMD5Check),
                        "File will not have MD5 set by default; with UseTransactional, download should fail");

                    file.DownloadRangeToStream(stream, buffer.Length, buffer.Length, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    file.DownloadRangeToStream(stream, buffer.Length, buffer.Length, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    file.DownloadRangeToStream(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    storageEx = TestHelper.ExpectedException<StorageException>(
                        () => file.DownloadRangeToStream(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithMD5, opContextWithMD5Check),
                        "Downloading more than 4MB with transactional MD5 should not be supported");
                    Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));

                    using (Stream fileStream = file.OpenRead(null, optionsWithMD5, opContextWithMD5Check))
                    {
                        fileStream.CopyTo(stream);
                        Assert.IsNotNull(lastCheckMD5);
                    }

                    using (Stream fileStream = file.OpenRead(null, optionsWithNoMD5, opContextWithMD5Check))
                    {
                        fileStream.CopyTo(stream);
                        Assert.IsNull(lastCheckMD5);
                    }
                }
                Assert.AreEqual(9, checkCount);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalMD5 flag with DownloadRangeToStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileUseTransactionalMD5GetTestAPM()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                UseTransactionalMD5 = true,
            };

            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            MD5 hasher = MD5.Create();
            string md5 = Convert.ToBase64String(hasher.ComputeHash(buffer));

            string lastCheckMD5 = null;
            int checkCount = 0;
            OperationContext opContextWithMD5Check = new OperationContext();
            opContextWithMD5Check.ResponseReceived += (_, args) =>
            {
                if (args.Response.ContentLength >= buffer.Length)
                {
                    lastCheckMD5 = args.Response.Headers[HttpResponseHeader.ContentMd5];
                    checkCount++;
                }
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                    using (Stream fileStream = file.OpenWrite(buffer.Length * 2))
                    {
                        fileStream.Write(buffer, 0, buffer.Length);
                        fileStream.Write(buffer, 0, buffer.Length);
                    }

                    checkCount = 0;
                    using (Stream stream = new MemoryStream())
                    {
                        result = file.BeginDownloadToStream(stream, null, optionsWithNoMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadRangeToStream(result);
                        Assert.IsNull(lastCheckMD5);

                        result = file.BeginDownloadToStream(stream, null, optionsWithMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        StorageException storageEx = TestHelper.ExpectedException<StorageException>(
                            () => file.EndDownloadRangeToStream(result),
                            "File will not have MD5 set by default; with UseTransactional, download should fail");

                        result = file.BeginDownloadRangeToStream(stream, buffer.Length, buffer.Length, null, optionsWithNoMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadRangeToStream(result);
                        Assert.IsNull(lastCheckMD5);

                        result = file.BeginDownloadRangeToStream(stream, buffer.Length, buffer.Length, null, optionsWithMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadRangeToStream(result);
                        Assert.AreEqual(md5, lastCheckMD5);

                        result = file.BeginDownloadRangeToStream(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithNoMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndDownloadRangeToStream(result);
                        Assert.IsNull(lastCheckMD5);

                        result = file.BeginDownloadRangeToStream(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        storageEx = TestHelper.ExpectedException<StorageException>(
                            () => file.EndDownloadRangeToStream(result),
                            "Downloading more than 4MB with transactional MD5 should not be supported");
                        Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));

                        result = file.BeginOpenRead(null, optionsWithMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        using (Stream fileStream = file.EndOpenRead(result))
                        {
                            fileStream.CopyTo(stream);
                            Assert.IsNotNull(lastCheckMD5);
                        }

                        result = file.BeginOpenRead(null, optionsWithNoMD5, opContextWithMD5Check,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        using (Stream fileStream = file.EndOpenRead(result))
                        {
                            fileStream.CopyTo(stream);
                            Assert.IsNull(lastCheckMD5);
                        }
                    }
                    Assert.AreEqual(9, checkCount);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
    }
}
