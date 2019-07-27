// -----------------------------------------------------------------------------------------
// <copyright file="MD5FlagsTest.cs" company="Microsoft">
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

using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

#if !NETCORE
using Windows.Storage.Streams;
#endif

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class MD5FlagsTest : BlobTestBase
    {
        [TestMethod]
        [Description("Test StoreBlobContentMD5 flag with UploadFromStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task StoreBlobContentMD5TestAsync()
        {
            BlobRequestOptions optionsWithNoMD5 = new BlobRequestOptions()
            {
                StoreBlobContentMD5 = false,
            };
            BlobRequestOptions optionsWithMD5 = new BlobRequestOptions()
            {
                StoreBlobContentMD5 = true,
            };

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob1 = container.GetBlockBlobReference("blob1");
                using (Stream stream = new NonSeekableMemoryStream())
                {
                    await blob1.UploadFromStreamAsync(stream, null, optionsWithMD5, null);
                }
                await blob1.FetchAttributesAsync();
                Assert.IsNotNull(blob1.Properties.ContentMD5);

                blob1 = container.GetBlockBlobReference("blob2");
                using (Stream stream = new NonSeekableMemoryStream())
                {
                    await blob1.UploadFromStreamAsync(stream, null, optionsWithNoMD5, null);
                }
                await blob1.FetchAttributesAsync();
                Assert.IsNull(blob1.Properties.ContentMD5);

                blob1 = container.GetBlockBlobReference("blob3");
                using (Stream stream = new NonSeekableMemoryStream())
                {
                    await blob1.UploadFromStreamAsync(stream);
                }
                await blob1.FetchAttributesAsync();
                Assert.IsNotNull(blob1.Properties.ContentMD5);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob4");
                blob2 = container.GetPageBlobReference("blob4");
                using (Stream stream = new MemoryStream())
                {
                    await blob2.UploadFromStreamAsync(stream, null, optionsWithMD5, null);
                }
                await blob2.FetchAttributesAsync();
                Assert.IsNotNull(blob2.Properties.ContentMD5);

                blob2 = container.GetPageBlobReference("blob5");
                using (Stream stream = new MemoryStream())
                {
                    await blob2.UploadFromStreamAsync(stream, null, optionsWithNoMD5, null);
                }
                await blob2.FetchAttributesAsync();
                Assert.IsNull(blob2.Properties.ContentMD5);

                blob2 = container.GetPageBlobReference("blob6");
                using (Stream stream = new MemoryStream())
                {
                    await blob2.UploadFromStreamAsync(stream);
                }
                await blob2.FetchAttributesAsync();
                Assert.IsNull(blob2.Properties.ContentMD5);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test DisableContentMD5Validation flag with DownloadToStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task DisableContentMD5ValidationTestAsync()
        {
            byte[] buffer = new byte[1024];
            Random random = new Random();
            random.NextBytes(buffer);

            BlobRequestOptions optionsWithNoMD5 = new BlobRequestOptions()
            {
                DisableContentMD5Validation = true,
                StoreBlobContentMD5 = true,
            };
            BlobRequestOptions optionsWithMD5 = new BlobRequestOptions()
            {
                DisableContentMD5Validation = false,
                StoreBlobContentMD5 = true,
            };

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                using (Stream stream = new NonSeekableMemoryStream(buffer))
                {
                    await blockBlob.UploadFromStreamAsync(stream, null, optionsWithMD5, null);
                }

                using (Stream stream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(stream, null, optionsWithMD5, null);
                    await blockBlob.DownloadToStreamAsync(stream, null, optionsWithNoMD5, null);

                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        int read;
                        do
                        {
                            read = await blobStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithNoMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        int read;
                        do
                        {
                            read = await blobStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    blockBlob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    await blockBlob.SetPropertiesAsync();

                    OperationContext opContext = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blockBlob.DownloadToStreamAsync(stream, null, optionsWithMD5, opContext),
                        opContext,
                        "Downloading a blob with invalid MD5 should fail",
                        HttpStatusCode.OK);
                    await blockBlob.DownloadToStreamAsync(stream, null, optionsWithNoMD5, null);

                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        TestHelper.ExpectedException<IOException>(
                            () =>
                            {
                                int read;
                                do
                                {
                                    read = blobStreamForRead.Read(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            },
                            "Downloading a blob with invalid MD5 should fail");
                    }

                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithNoMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        int read;
                        do
                        {
                            read = await blobStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }
                }

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                using (Stream stream = new MemoryStream(buffer))
                {
                    await pageBlob.UploadFromStreamAsync(stream, null, optionsWithMD5, null);
                }

                using (Stream stream = new MemoryStream())
                {
                    await pageBlob.DownloadToStreamAsync(stream, null, optionsWithMD5, null);
                    await pageBlob.DownloadToStreamAsync(stream, null, optionsWithNoMD5, null);

                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        int read;
                        do
                        {
                            read = await blobStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithNoMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        int read;
                        do
                        {
                            read = await blobStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    pageBlob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    await pageBlob.SetPropertiesAsync();

                    OperationContext opContext = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await pageBlob.DownloadToStreamAsync(stream, null, optionsWithMD5, opContext),
                        opContext,
                        "Downloading a blob with invalid MD5 should fail",
                        HttpStatusCode.OK);
                    await pageBlob.DownloadToStreamAsync(stream, null, optionsWithNoMD5, null);

                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithMD5, null))
                    {

                        Stream blobStreamForRead = blobStream;
                        TestHelper.ExpectedException<IOException>(
                            () =>
                            {
                                int read;
                                do
                                {
                                    read = blobStreamForRead.Read(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            },
                            "Downloading a blob with invalid MD5 should fail");
                    }

                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithNoMD5, null))
                    {
                        Stream blobStreamForRead = blobStream;
                        int read;
                        do
                        {
                            read = await blobStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalMD5 flag with PutBlock and WritePages")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task UseTransactionalMD5PutTestAsync()
        {
            BlobRequestOptions optionsWithNoMD5 = new BlobRequestOptions()
            {
                UseTransactionalMD5 = false,
            };
            BlobRequestOptions optionsWithMD5 = new BlobRequestOptions()
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
                if (HttpRequestParsers.GetContentLength(args.Request) >= buffer.Length)
                {
                    lastCheckMD5 = HttpRequestParsers.GetContentMD5(args.Request);
                    checkCount++;
                }
            };

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                List<string> blockIds = GetBlockIdList(3);
                checkCount = 0;
                using (Stream blockData = new MemoryStream(buffer))
                {
                    lastCheckMD5 = "invalid_md5";
                    await blockBlob.PutBlockAsync(blockIds[0], blockData, null, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await blockBlob.PutBlockAsync(blockIds[1], blockData, null, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await blockBlob.PutBlockAsync(blockIds[2], blockData, md5, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);
                }

                Assert.AreEqual(3, checkCount);

                checkCount = 0;
                CloudAppendBlob appendBlob = container.GetAppendBlobReference("blob2");
                await appendBlob.CreateOrReplaceAsync();
                checkCount = 0;
                using (Stream blockData = new MemoryStream(buffer))
                {
                    lastCheckMD5 = "invalid_md5";
                    await appendBlob.AppendBlockAsync(blockData, null, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await appendBlob.AppendBlockAsync(blockData, null, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await appendBlob.AppendBlockAsync(blockData, md5, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);
                }

                Assert.AreEqual(3, checkCount);

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob3");
                await pageBlob.CreateAsync(buffer.Length);
                checkCount = 0;
                using (Stream pageData = new MemoryStream(buffer))
                {
                    lastCheckMD5 = "invalid_md5";
                    await pageBlob.WritePagesAsync(pageData, 0, null, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    pageData.Seek(0, SeekOrigin.Begin);
                    await pageBlob.WritePagesAsync(pageData, 0, null, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    pageData.Seek(0, SeekOrigin.Begin);
                    await pageBlob.WritePagesAsync(pageData, 0, md5, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);
                }

                Assert.AreEqual(3, checkCount);

                lastCheckMD5 = null;
                blockBlob = container.GetBlockBlobReference("blob4");
                checkCount = 0;
                using (Stream blobStream = await blockBlob.OpenWriteAsync(null, optionsWithMD5, opContextWithMD5Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNotNull(lastCheckMD5);
                Assert.AreEqual(1, checkCount);

                lastCheckMD5 = "invalid_md5";
                blockBlob = container.GetBlockBlobReference("blob5");
                checkCount = 0;
                using (Stream blobStream = await blockBlob.OpenWriteAsync(null, optionsWithNoMD5, opContextWithMD5Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNull(lastCheckMD5);
                Assert.AreEqual(1, checkCount);

                lastCheckMD5 = null;
                pageBlob = container.GetPageBlobReference("blob6");
                checkCount = 0;
                using (Stream blobStream = await pageBlob.OpenWriteAsync(buffer.Length * 3, null, optionsWithMD5, opContextWithMD5Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNotNull(lastCheckMD5);
                Assert.AreEqual(1, checkCount);

                lastCheckMD5 = "invalid_md5";
                pageBlob = container.GetPageBlobReference("blob7");
                checkCount = 0;
                using (Stream blobStream = await pageBlob.OpenWriteAsync(buffer.Length * 3, null, optionsWithNoMD5, opContextWithMD5Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNull(lastCheckMD5);
                Assert.AreEqual(1, checkCount);
            }
            finally
            {
                await container.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalMD5 flag with DownloadRangeToStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task UseTransactionalMD5GetTestAsync()
        {
            BlobRequestOptions optionsWithNoMD5 = new BlobRequestOptions()
            {
                UseTransactionalMD5 = false,
            };
            BlobRequestOptions optionsWithMD5 = new BlobRequestOptions()
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
                if (long.Parse(HttpResponseParsers.GetContentLength(args.Response)) >= buffer.Length)
                {
                    lastCheckMD5 = HttpResponseParsers.GetContentMD5(args.Response);
                    checkCount++;
                }
            };

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                using (Stream blobStream = await blockBlob.OpenWriteAsync())
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }

                checkCount = 0;
                using (Stream stream = new MemoryStream())
                {
                    lastCheckMD5 = null;
                    await blockBlob.DownloadToStreamAsync(stream, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNotNull(lastCheckMD5);

                    lastCheckMD5 = null;
                    await blockBlob.DownloadToStreamAsync(stream, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.IsNotNull(lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    await blockBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    await blockBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    await blockBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    StorageException storageEx = await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => blockBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithMD5, opContextWithMD5Check),
                        "Downloading more than 4MB with transactional MD5 should not be supported");
                    Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));

                    lastCheckMD5 = null;
                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithMD5, opContextWithMD5Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNotNull(lastCheckMD5);
                    }

                    lastCheckMD5 = "invalid_md5";
                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithNoMD5, opContextWithMD5Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNull(lastCheckMD5);
                    }
                }

                Assert.AreEqual(9, checkCount);

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob3");
                using (Stream blobStream = await pageBlob.OpenWriteAsync(buffer.Length * 2))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }

                checkCount = 0;
                using (Stream stream = new MemoryStream())
                {
                    lastCheckMD5 = "invalid_md5";
                    await pageBlob.DownloadToStreamAsync(stream, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    StorageException storageEx = await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => pageBlob.DownloadToStreamAsync(stream, null, optionsWithMD5, opContextWithMD5Check),
                        "Page blob will not have MD5 set by default; with UseTransactional, download should fail");

                    lastCheckMD5 = "invalid_md5";
                    await pageBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    await pageBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithMD5, opContextWithMD5Check);
                    Assert.AreEqual(md5, lastCheckMD5);

                    lastCheckMD5 = "invalid_md5";
                    await pageBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithNoMD5, opContextWithMD5Check);
                    Assert.IsNull(lastCheckMD5);

                    storageEx = await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => pageBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithMD5, opContextWithMD5Check),
                        "Downloading more than 4MB with transactional MD5 should not be supported");
                    Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));

                    lastCheckMD5 = null;
                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithMD5, opContextWithMD5Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNotNull(lastCheckMD5);
                    }

                    lastCheckMD5 = "invalid_md5";
                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithNoMD5, opContextWithMD5Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNull(lastCheckMD5);
                    }
                }

                Assert.AreEqual(9, checkCount);
            }
            finally
            {
                await container.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalCRC64 flag with PutBlock and WritePages")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task UseTransactionalCRC64PutTestAsync()
        {
            BlobRequestOptions optionsWithNoCRC64 = new BlobRequestOptions()
            {
                ChecksumOptions = new ChecksumOptions { UseTransactionalCRC64 = false }
            };
            BlobRequestOptions optionsWithCRC64 = new BlobRequestOptions()
            {
                ChecksumOptions = new ChecksumOptions { UseTransactionalCRC64 = true }
            };

            byte[] buffer = GetRandomBuffer(1024);
            Crc64Wrapper hasher = new Crc64Wrapper();
            hasher.UpdateHash(buffer, 0, buffer.Length);
            string crc64 = hasher.ComputeHash();

            string lastCheckCRC64 = null;
            int checkCount = 0;
            OperationContext opContextWithCRC64Check = new OperationContext();
            opContextWithCRC64Check.SendingRequest += (_, args) =>
            {
                if (HttpRequestParsers.GetContentLength(args.Request) >= buffer.Length)
                {
                    lastCheckCRC64 = HttpRequestParsers.GetContentCRC64(args.Request);
                    checkCount++;
                }
            };

            OperationContext opContextWithCRC64CheckAndInjectedFailure = new OperationContext();
            opContextWithCRC64CheckAndInjectedFailure.SendingRequest += (_, args) =>
            {
                args.Response.Headers.Remove(Constants.HeaderConstants.ContentCrc64Header);
                args.Request.Headers.TryAddWithoutValidation(Constants.HeaderConstants.ContentCrc64Header, "dummy");
                if (HttpRequestParsers.GetContentLength(args.Request) >= buffer.Length)
                {
                    lastCheckCRC64 = HttpRequestParsers.GetContentCRC64(args.Request);
                    checkCount++;
                }
            };

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                List<string> blockIds = GetBlockIdList(3);
                checkCount = 0;
                using (Stream blockData = new MemoryStream(buffer))
                {
                    lastCheckCRC64 = "invalid_CRC64";
                    await blockBlob.PutBlockAsync(blockIds[0], blockData, null, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    lastCheckCRC64 = "invalid_CRC64";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await blockBlob.PutBlockAsync(blockIds[1], blockData, null, null, optionsWithCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);

                    blockData.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => blockBlob.PutBlockAsync(blockIds[1], blockData, null, null, optionsWithCRC64, opContextWithCRC64CheckAndInjectedFailure),
                        "Calculated CRC64 does not match existing property"
                        );

                    lastCheckCRC64 = "invalid_CRC64";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await blockBlob.PutBlockAsync(blockIds[2], blockData, new Checksum(crc64: crc64), null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);
                }

                Assert.AreEqual(3, checkCount);

                checkCount = 0;
                CloudAppendBlob appendBlob = container.GetAppendBlobReference("blob2");
                await appendBlob.CreateOrReplaceAsync();
                checkCount = 0;
                using (Stream blockData = new MemoryStream(buffer))
                {
                    lastCheckCRC64 = "invalid_CRC64";
                    await appendBlob.AppendBlockAsync(blockData, null, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    lastCheckCRC64 = "invalid_CRC64";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await appendBlob.AppendBlockAsync(blockData, null, null, optionsWithCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);

                    blockData.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => appendBlob.AppendBlockAsync(blockData, null, null, optionsWithCRC64, opContextWithCRC64CheckAndInjectedFailure),
                        "Calculated CRC64 does not match existing property"
                        );

                    lastCheckCRC64 = "invalid_CRC64";
                    blockData.Seek(0, SeekOrigin.Begin);
                    await appendBlob.AppendBlockAsync(blockData, new Checksum(crc64: crc64), null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);
                }

                Assert.AreEqual(3, checkCount);

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob3");
                await pageBlob.CreateAsync(buffer.Length);
                checkCount = 0;
                using (Stream pageData = new MemoryStream(buffer))
                {
                    lastCheckCRC64 = "invalid_CRC64";
                    await pageBlob.WritePagesAsync(pageData, 0, null, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    lastCheckCRC64 = "invalid_CRC64";
                    pageData.Seek(0, SeekOrigin.Begin);
                    await pageBlob.WritePagesAsync(pageData, 0, null, null, optionsWithCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);

                    pageData.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => pageBlob.WritePagesAsync(pageData, 0, null, null, optionsWithCRC64, opContextWithCRC64CheckAndInjectedFailure),
                        "Calculated CRC64 does not match existing property"
                        );

                    lastCheckCRC64 = "invalid_CRC64";
                    pageData.Seek(0, SeekOrigin.Begin);
                    await pageBlob.WritePagesAsync(pageData, 0, new Checksum(crc64: crc64), null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);
                }

                Assert.AreEqual(3, checkCount);

                lastCheckCRC64 = null;
                blockBlob = container.GetBlockBlobReference("blob4");
                checkCount = 0;
                using (Stream blobStream = await blockBlob.OpenWriteAsync(null, optionsWithCRC64, opContextWithCRC64Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNotNull(lastCheckCRC64);
                Assert.AreEqual(1, checkCount);

                lastCheckCRC64 = "invalid_CRC64";
                blockBlob = container.GetBlockBlobReference("blob5");
                checkCount = 0;
                using (Stream blobStream = await blockBlob.OpenWriteAsync(null, optionsWithNoCRC64, opContextWithCRC64Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNull(lastCheckCRC64);
                Assert.AreEqual(1, checkCount);

                lastCheckCRC64 = null;
                pageBlob = container.GetPageBlobReference("blob6");
                checkCount = 0;
                using (Stream blobStream = await pageBlob.OpenWriteAsync(buffer.Length * 3, null, optionsWithCRC64, opContextWithCRC64Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNotNull(lastCheckCRC64);
                Assert.AreEqual(1, checkCount);

                lastCheckCRC64 = "invalid_CRC64";
                pageBlob = container.GetPageBlobReference("blob7");
                checkCount = 0;
                using (Stream blobStream = await pageBlob.OpenWriteAsync(buffer.Length * 3, null, optionsWithNoCRC64, opContextWithCRC64Check))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }
                Assert.IsNull(lastCheckCRC64);
                Assert.AreEqual(1, checkCount);
            }
            finally
            {
                await container.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Test UseTransactionalCRC64 flag with DownloadRangeToStream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task UseTransactionalCRC64GetTestAsync()
        {
            BlobRequestOptions optionsWithNoCRC64 = new BlobRequestOptions()
            {
                ChecksumOptions = new ChecksumOptions { UseTransactionalCRC64 = false }
            };
            BlobRequestOptions optionsWithCRC64 = new BlobRequestOptions()
            {
                ChecksumOptions = new ChecksumOptions { UseTransactionalCRC64 = true }
            };

            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);
            Crc64Wrapper hasher = new Crc64Wrapper();
            hasher.UpdateHash(buffer, 0, buffer.Length);
            string crc64 = hasher.ComputeHash();

            string lastCheckCRC64 = null;
            OperationContext opContextWithCRC64Check = new OperationContext();
            opContextWithCRC64Check.ResponseReceived += (_, args) =>
            {
                if (long.Parse(HttpResponseParsers.GetContentLength(args.Response)) >= buffer.Length)
                {
                    lastCheckCRC64 = HttpResponseParsers.GetContentCRC64(args.Response);
                }
            };

            OperationContext opContextWithCRC64CheckAndInjectedFailure = new OperationContext();
            opContextWithCRC64CheckAndInjectedFailure.ResponseReceived += (_, args) =>
            {
                args.Response.Headers.Remove(Constants.HeaderConstants.ContentCrc64Header);
                args.Response.Headers.TryAddWithoutValidation(Constants.HeaderConstants.ContentCrc64Header, "dummy");
                if (long.Parse(HttpResponseParsers.GetContentLength(args.Response)) >= buffer.Length)
                {
                    lastCheckCRC64 = HttpResponseParsers.GetContentCRC64(args.Response);
                }
            };

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                using (Stream blobStream = await blockBlob.OpenWriteAsync())
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }

                using (Stream stream = new MemoryStream())
                {
                    //lastCheckCRC64 = null;
                    //blockBlob.DownloadToStream(stream, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    //Assert.IsNotNull(lastCheckCRC64);

                    //lastCheckCRC64 = null;
                    //blockBlob.DownloadToStream(stream, null, optionsWithCRC64, opContextWithCRC64Check);
                    //Assert.IsNotNull(lastCheckCRC64);

                    lastCheckCRC64 = "invalid_CRC64";
                    await blockBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    lastCheckCRC64 = "invalid_CRC64";
                    await blockBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);

                    await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => blockBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithCRC64, opContextWithCRC64CheckAndInjectedFailure),
                        "Calculated CRC64 does not match existing property"
                        );

                    lastCheckCRC64 = "invalid_CRC64";
                    await blockBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    StorageException storageEx = await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => blockBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithCRC64, opContextWithCRC64Check),
                        "Downloading more than 4MB with transactional CRC64 should not be supported");
                    Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));

                    lastCheckCRC64 = null;
                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithCRC64, opContextWithCRC64Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNotNull(lastCheckCRC64);
                    }

                    lastCheckCRC64 = "invalid_CRC64";
                    using (Stream blobStream = await blockBlob.OpenReadAsync(null, optionsWithNoCRC64, opContextWithCRC64Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNull(lastCheckCRC64);
                    }
                }

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob3");
                using (Stream blobStream = await pageBlob.OpenWriteAsync(buffer.Length * 2))
                {
                    blobStream.Write(buffer, 0, buffer.Length);
                    blobStream.Write(buffer, 0, buffer.Length);
                }

                using (Stream stream = new MemoryStream())
                {
                    lastCheckCRC64 = "invalid_CRC64";
                    await pageBlob.DownloadToStreamAsync(stream, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    StorageException storageEx = await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => pageBlob.DownloadToStreamAsync(stream, null, optionsWithCRC64, opContextWithCRC64Check),
                        "Page blob will not have CRC64 set by default; with UseTransactional, download should fail");

                    lastCheckCRC64 = "invalid_CRC64";
                    await pageBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    lastCheckCRC64 = "invalid_CRC64";
                    await pageBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithCRC64, opContextWithCRC64Check);
                    Assert.AreEqual(crc64, lastCheckCRC64);

                    await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => pageBlob.DownloadRangeToStreamAsync(stream, buffer.Length, buffer.Length, null, optionsWithCRC64, opContextWithCRC64CheckAndInjectedFailure),
                        "Calculated CRC64 does not match existing property"
                        );

                    lastCheckCRC64 = "invalid_CRC64";
                    await pageBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithNoCRC64, opContextWithCRC64Check);
                    Assert.IsNull(lastCheckCRC64);

                    storageEx = await TestHelper.ExpectedExceptionAsync<StorageException>(
                        () => pageBlob.DownloadRangeToStreamAsync(stream, 1024, 4 * 1024 * 1024 + 1, null, optionsWithCRC64, opContextWithCRC64Check),
                        "Downloading more than 4MB with transactional CRC64 should not be supported");
                    Assert.IsInstanceOfType(storageEx.InnerException, typeof(ArgumentOutOfRangeException));

                    lastCheckCRC64 = null;
                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithCRC64, opContextWithCRC64Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNotNull(lastCheckCRC64);
                    }

                    lastCheckCRC64 = "invalid_CRC64";
                    using (Stream blobStream = await pageBlob.OpenReadAsync(null, optionsWithNoCRC64, opContextWithCRC64Check))
                    {
                        blobStream.CopyTo(stream);
                        Assert.IsNull(lastCheckCRC64);
                    }
                }
            }
            finally
            {
                await container.DeleteIfExistsAsync();
            }
        }
    }
}
