// -----------------------------------------------------------------------------------------
// <copyright file="CopyBlobTest.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class CopyBlobTest : BlobTestBase
    {
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("CopyFromBlob with Unicode source blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CopyBlobUsingUnicodeBlobNameAsync()
        {
            string _unicodeBlobName = "繁体字14a6c";
            string _nonUnicodeBlobName = "sample_file";

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                CloudBlockBlob blobUnicodeSource = container.GetBlockBlobReference(_unicodeBlobName);
                string data = "Test content";
                await UploadTextAsync(blobUnicodeSource, data, Encoding.UTF8);
                CloudBlockBlob blobAsciiSource = container.GetBlockBlobReference(_nonUnicodeBlobName);
                await UploadTextAsync(blobAsciiSource, data, Encoding.UTF8);

                //Copy blobs over
                CloudBlockBlob blobAsciiDest = container.GetBlockBlobReference(_nonUnicodeBlobName + "_copy");
                string copyId = await blobAsciiDest.StartCopyAsync(TestHelper.Defiddler(blobAsciiSource));
                await WaitForCopyAsync(blobAsciiDest);

                CloudBlockBlob blobUnicodeDest = container.GetBlockBlobReference(_unicodeBlobName + "_copy");
                copyId = await blobUnicodeDest.StartCopyAsync(TestHelper.Defiddler(blobUnicodeSource));
                await WaitForCopyAsync(blobUnicodeDest);

                Assert.AreEqual(CopyStatus.Success, blobUnicodeDest.CopyState.Status);
                Assert.AreEqual(blobUnicodeSource.Uri.AbsolutePath, blobUnicodeDest.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, blobUnicodeDest.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, blobUnicodeDest.CopyState.BytesCopied);
                Assert.AreEqual(copyId, blobUnicodeDest.CopyState.CopyId);
                Assert.IsTrue(blobUnicodeDest.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
        
        private static async Task CloudBlockBlobCopyImplAsync(Func<CloudBlockBlob, CloudBlockBlob, Task<string>> copyFunc)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob source = container.GetBlockBlobReference("source");

                string data = "String data";
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudBlockBlob copy = container.GetBlockBlobReference("copy");
                string copyId = await copyFunc(source, copy);
                Assert.AreEqual(BlobType.BlockBlob, copy.BlobType);
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                Assert.AreEqual(copyId, copy.CopyState.CopyId);
                Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await copy.AbortCopyAsync(copyId, null, null, opContext),
                    opContext,
                    "Aborting a copy operation after completion should fail",
                    HttpStatusCode.Conflict,
                    "NoPendingCopyOperation");

                await source.FetchAttributesAsync();
                Assert.IsNotNull(copy.Properties.ETag);
                Assert.AreNotEqual(source.Properties.ETag, copy.Properties.ETag);
                Assert.IsTrue(copy.Properties.LastModified > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                string copyData = await DownloadTextAsync(copy, Encoding.UTF8);
                Assert.AreEqual(data, copyData, "Data inside copy of blob not similar");

                await copy.FetchAttributesAsync();
                BlobProperties prop1 = copy.Properties;
                BlobProperties prop2 = source.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value", copy.Metadata["Test"], false, "Copied metadata not same");

                await copy.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
        
        [TestMethod]
        [Description("Copy a blob and then verify its contents, properties, and metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobCopyTestAsync()
        {
            foreach (var rehydratePriority in new[] { default(RehydratePriority?), RehydratePriority.Standard, RehydratePriority.High })
            {
                await CloudBlockBlobCopyImplAsync(async (source, copy) => await copy.StartCopyAsync(TestHelper.Defiddler(source), rehydratePriority, null, null, null, null, CancellationToken.None));
            }
        }

        [TestMethod]
        [Description("Copy a blob using x-ms-requires-sync, and then verify its contents, properties, and metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobCopyWithRequiresSyncTestAsync()
        {
            //await CloudBlockBlobCopyImpl(async (source, copy) => await copy.StartCopyAsync(TestHelper.Defiddler(source), true /* syncCopy */, default(AccessCondition), default(AccessCondition), default(BlobRequestOptions), default(OperationContext), default(CancellationToken)));
        }

        private static async Task CloudBlockBlobCopyFromCloudFileImpl(Func<CloudFile, CloudBlockBlob, string> copyFunc)
        {
            CloudFileClient fileClient = GenerateCloudFileClient();

            string name = GetRandomContainerName();
            CloudFileShare share = fileClient.GetShareReference(name);

            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                try
                {
                    await container.CreateAsync();

                    await share.CreateAsync();

                    CloudFile source = share.GetRootDirectoryReference().GetFileReference("source");
                    byte[] data = GetRandomBuffer(1024);

                    await source.UploadFromByteArrayAsync(data, 0, data.Length);

                    source.Metadata["Test"] = "value";
                    source.SetMetadataAsync().Wait();

                    string sasToken = source.GetSharedAccessSignature(new SharedAccessFilePolicy
                    {
                        Permissions = SharedAccessFilePermissions.Read,
                        SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24)
                    });

                    Uri fileSasUri = new Uri(source.StorageUri.PrimaryUri.ToString() + sasToken);

                    source = new CloudFile(fileSasUri);

                    CloudBlockBlob copy = container.GetBlockBlobReference("copy");
                    string copyId = copyFunc(source, copy);
                    Assert.AreEqual(BlobType.BlockBlob, copy.BlobType);

                    try
                    {
                        await WaitForCopyAsync(copy);

                        Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                        Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                        Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                        Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                        Assert.AreEqual(copyId, copy.CopyState.CopyId);
                        Assert.IsFalse(copy.Properties.IsIncrementalCopy);
                        Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));
                    }
                    catch (NullReferenceException)
                    {
                        // potential null ref in the WaitForCopyTask and CopyState check implementation
                    }

                    OperationContext opContext = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await copy.AbortCopyAsync(copyId, null, null, opContext),
                        opContext,
                        "Aborting a copy operation after completion should fail",
                        HttpStatusCode.Conflict,
                        "NoPendingCopyOperation");

                    source.FetchAttributesAsync().Wait();
                    Assert.IsNotNull(copy.Properties.ETag);
                    Assert.AreNotEqual(source.Properties.ETag, copy.Properties.ETag);
                    Assert.IsTrue(copy.Properties.LastModified > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                    byte[] copyData = new byte[source.Properties.Length];

                    await copy.DownloadToByteArrayAsync(copyData, 0);

                    Assert.IsTrue(data.SequenceEqual(copyData), "Data inside copy of blob not similar");

                    copy.FetchAttributesAsync().Wait();
                    BlobProperties prop1 = copy.Properties;
                    FileProperties prop2 = source.Properties;

                    Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                    Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                    Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                    Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                    Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                    Assert.AreEqual("value", copy.Metadata["Test"], false, "Copied metadata not same");

                    copy.DeleteIfExistsAsync().Wait();
                }
                finally
                {
                    share.DeleteIfExistsAsync().Wait();
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        //[TestMethod]
        //[Description("Copy a blob and then verify its contents, properties, and metadata")]
        //[TestCategory(ComponentCategory.Blob)]
        //[TestCategory(TestTypeCategory.UnitTest)]
        //[TestCategory(SmokeTestCategory.NonSmoke)]
        //[TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        //public async Task CloudBlockBlobCopyFromCloudFileTestTask()
        //{
        //    await CloudBlockBlobCopyFromCloudFileImpl((source, copy) => copy.StartCopyAsync(TestHelper.Defiddler(source)).Result);
        //}

        [TestMethod]
        [Description("Copy a blob and override metadata during copy")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobCopyTestWithMetadataOverrideAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob source = container.GetBlockBlobReference("source");

                string data = "String data";
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudBlockBlob copy = container.GetBlockBlobReference("copy");
                copy.Metadata["Test2"] = "value2";
                string copyId = await copy.StartCopyAsync(TestHelper.Defiddler(source));
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                Assert.AreEqual(copyId, copy.CopyState.CopyId);
                Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                string copyData = await DownloadTextAsync(copy, Encoding.UTF8);
                Assert.AreEqual(data, copyData, "Data inside copy of blob not similar");

                await copy.FetchAttributesAsync();
                await source.FetchAttributesAsync();
                BlobProperties prop1 = copy.Properties;
                BlobProperties prop2 = source.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value2", copy.Metadata["Test2"], false, "Copied metadata not same");
                Assert.IsFalse(copy.Metadata.ContainsKey("Test"), "Source Metadata should not appear in destination blob");

                await copy.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Copy a blob and override metadata during copy")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobCopyFromSnapshotTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob source = container.GetBlockBlobReference("source");

                string data = "String data";
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudBlockBlob snapshot = await source.CreateSnapshotAsync();

                //Modify source
                string newData = "Hello";
                source.Metadata["Test"] = "newvalue";
                await source.SetMetadataAsync();
                source.Properties.ContentMD5 = null;
                await UploadTextAsync(source, newData, Encoding.UTF8);

                Assert.AreEqual(newData, await DownloadTextAsync(source, Encoding.UTF8), "Source is modified correctly");
                Assert.AreEqual(data, await DownloadTextAsync(snapshot, Encoding.UTF8), "Modifying source blob should not modify snapshot");

                await source.FetchAttributesAsync();
                await snapshot.FetchAttributesAsync();
                Assert.AreNotEqual(source.Metadata["Test"], snapshot.Metadata["Test"], "Source and snapshot metadata should be independent");

                CloudBlockBlob copy = container.GetBlockBlobReference("copy");
                await copy.StartCopyAsync(TestHelper.Defiddler(snapshot));
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(data, await DownloadTextAsync(copy, Encoding.UTF8), "Data inside copy of blob not similar");

                await copy.FetchAttributesAsync();
                BlobProperties prop1 = copy.Properties;
                BlobProperties prop2 = snapshot.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value", copy.Metadata["Test"], false, "Copied metadata not same");

                await copy.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Copy a blob and then verify its contents, properties, and metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCopyTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob source = container.GetPageBlobReference("source");

                string data = new string('a', 512);
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudPageBlob copy = container.GetPageBlobReference("copy");
                string copyId = await copy.StartCopyAsync(TestHelper.Defiddler(source));
                Assert.AreEqual(BlobType.PageBlob, copy.BlobType);
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                Assert.AreEqual(copyId, copy.CopyState.CopyId);
                Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await copy.AbortCopyAsync(copyId, null, null, opContext),
                    opContext,
                    "Aborting a copy operation after completion should fail",
                    HttpStatusCode.Conflict,
                    "NoPendingCopyOperation");

                await source.FetchAttributesAsync();
                Assert.IsNotNull(copy.Properties.ETag);
                Assert.AreNotEqual(source.Properties.ETag, copy.Properties.ETag);
                Assert.IsTrue(copy.Properties.LastModified > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                string copyData = await DownloadTextAsync(copy, Encoding.UTF8);
                Assert.AreEqual(data, copyData, "Data inside copy of blob not similar");

                await copy.FetchAttributesAsync();
                BlobProperties prop1 = copy.Properties;
                BlobProperties prop2 = source.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value", copy.Metadata["Test"], false, "Copied metadata not same");

                await copy.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Copy a blob and override metadata during copy")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCopyTestWithMetadataOverrideAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob source = container.GetPageBlobReference("source");

                string data = new string('a', 512);
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudPageBlob copy = container.GetPageBlobReference("copy");
                copy.Metadata["Test2"] = "value2";
                string copyId = await copy.StartCopyAsync(TestHelper.Defiddler(source));
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                Assert.AreEqual(copyId, copy.CopyState.CopyId);
                Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                string copyData = await DownloadTextAsync(copy, Encoding.UTF8);
                Assert.AreEqual(data, copyData, "Data inside copy of blob not similar");

                await copy.FetchAttributesAsync();
                await source.FetchAttributesAsync();
                BlobProperties prop1 = copy.Properties;
                BlobProperties prop2 = source.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value2", copy.Metadata["Test2"], false, "Copied metadata not same");
                Assert.IsFalse(copy.Metadata.ContainsKey("Test"), "Source Metadata should not appear in destination blob");

                await copy.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Copy a blob and override metadata during copy")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCopyFromSnapshotTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob source = container.GetPageBlobReference("source");

                string data = new string('a', 512);
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudPageBlob snapshot = await source.CreateSnapshotAsync();

                //Modify source
                string newData = new string('b', 512);
                source.Metadata["Test"] = "newvalue";
                await source.SetMetadataAsync();
                source.Properties.ContentMD5 = null;
                await UploadTextAsync(source, newData, Encoding.UTF8);

                Assert.AreEqual(newData, await DownloadTextAsync(source, Encoding.UTF8), "Source is modified correctly");
                Assert.AreEqual(data, await DownloadTextAsync(snapshot, Encoding.UTF8), "Modifying source blob should not modify snapshot");

                await source.FetchAttributesAsync();
                await snapshot.FetchAttributesAsync();
                Assert.AreNotEqual(source.Metadata["Test"], snapshot.Metadata["Test"], "Source and snapshot metadata should be independent");

                CloudPageBlob copy = container.GetPageBlobReference("copy");
                await copy.StartCopyAsync(TestHelper.Defiddler(snapshot));
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(data, await DownloadTextAsync(copy, Encoding.UTF8), "Data inside copy of blob not similar");

                await copy.FetchAttributesAsync();
                BlobProperties prop1 = copy.Properties;
                BlobProperties prop2 = snapshot.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value", copy.Metadata["Test"], false, "Copied metadata not same");

                await copy.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Perform an incremental of copy a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobIncrementalCopyTestAsync()
        {
            for (int i = 0; i < 4; i++)
            {
                await IncrementalCopyAsyncImpl(i);
            }
        }

        private async Task IncrementalCopyAsyncImpl(int overload)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                CloudPageBlob source = container.GetPageBlobReference("source");
                await source.CreateAsync(1024);

                string data = new string('a', 512);
                await UploadTextAsync(source, data, Encoding.UTF8);
                CloudPageBlob sourceSnapshot = await source.CreateSnapshotAsync(null, null, null, null);

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                };

                string sasToken = sourceSnapshot.GetSharedAccessSignature(policy);

                StorageCredentials blobSAS = new StorageCredentials(sasToken);
                Uri sourceSnapshotUri = blobSAS.TransformUri(TestHelper.Defiddler(sourceSnapshot).SnapshotQualifiedUri);

                StorageCredentials accountSAS = new StorageCredentials(sasToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, source.ServiceClient.StorageUri, null, null, null);

                CloudPageBlob snapshotWithSas = await accountWithSAS.CreateCloudBlobClient().GetBlobReferenceFromServerAsync(sourceSnapshot.SnapshotQualifiedUri) as CloudPageBlob;

                CloudPageBlob copy = container.GetPageBlobReference("copy");
                string copyId = null;
                if (overload == 0)
                {
#if !FACADE_NETCORE
                    copyId = await copy.StartIncrementalCopyAsync(accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri));
#else
                    Uri snapShotQualifiedUri = accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri);
                    copyId = await copy.StartIncrementalCopyAsync(new CloudPageBlob(new StorageUri(snapShotQualifiedUri), null, null));
#endif
                }
                else if (overload == 1)
                {
#if !FACADE_NETCORE
                    CloudPageBlob blob = new CloudPageBlob(accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri));
#else
                    Uri snapShotQualifiedUri = accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri);
                    CloudPageBlob blob = new CloudPageBlob(new StorageUri(snapShotQualifiedUri), null, null);
#endif
                    copyId = await copy.StartIncrementalCopyAsync(blob);
                }
                else if (overload == 2)
                {
#if !FACADE_NETCORE
                    CloudPageBlob blob = new CloudPageBlob(accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri));
#else
                    Uri snapShotQualifiedUri = accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri);
                    CloudPageBlob blob = new CloudPageBlob(new StorageUri(snapShotQualifiedUri), null, null);
#endif
                    copyId = await copy.StartIncrementalCopyAsync(blob, null, null, null, CancellationToken.None);
                }
                else
                {
#if !FACADE_NETCORE
                    copyId = await copy.StartIncrementalCopyAsync(accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri), null, null, null, CancellationToken.None);
#else
                    Uri snapShotQualifiedUri = accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri);
                    CloudPageBlob blob = new CloudPageBlob(new StorageUri(snapShotQualifiedUri), null, null);
                    copyId = await copy.StartIncrementalCopyAsync(blob, null, null, null, CancellationToken.None);
#endif


                }

                await WaitForCopyAsync(copy);

                Assert.AreEqual(BlobType.PageBlob, copy.BlobType);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                Assert.AreEqual(copyId, copy.CopyState.CopyId);
                Assert.IsTrue(copy.Properties.IsIncrementalCopy);
                Assert.IsTrue(copy.CopyState.DestinationSnapshotTime.HasValue);
                Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
