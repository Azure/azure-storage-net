// -----------------------------------------------------------------------------------------
// <copyright file="CloudPageBlobTest.cs" company="Microsoft">
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

using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#if NETCORE
using System.Security.Cryptography;
#else
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
#endif

using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class CloudPageBlobTest : BlobTestBase
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
        [Description("Create a zero-length page blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCreateAndDeleteAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(0);
                Assert.IsTrue(await blob.ExistsAsync());
                await blob.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Resize a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobResizeAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                await blob.CreateAsync(1024);
                Assert.AreEqual(1024, blob.Properties.Length);
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1024, blob2.Properties.Length);
                blob2.Properties.ContentType = "text/plain";
                await blob2.SetPropertiesAsync();
                await blob.ResizeAsync(2048);
                Assert.AreEqual(2048, blob.Properties.Length);
                await blob.FetchAttributesAsync();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(2048, blob2.Properties.Length);

                // Resize to 0 length
                await blob.ResizeAsync(0);
                Assert.AreEqual(0, blob.Properties.Length);
                await blob.FetchAttributesAsync();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Properties.Length);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Use sequence number conditions on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSequenceNumberAsync()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");

                await blob.CreateAsync(buffer.Length);
                Assert.IsNull(blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Update, 5);
                Assert.AreEqual(5, blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Max, 7);
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Max, 3);
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Increment, null);
                Assert.AreEqual(8, blob.Properties.PageBlobSequenceNumber);

                StorageException e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Update, null),
                    "SetSequenceNumber with Update should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Update, -1),
                    "Negative sequence numbers are not supported");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentOutOfRangeException));

                e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Max, null),
                    "SetSequenceNumber with Max should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Increment, 1),
                    "SetSequenceNumber with Increment should require null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    OperationContext context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDeleteIfExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                Assert.IsFalse(await blob.DeleteIfExistsAsync());
                await blob.CreateAsync(0);
                Assert.IsTrue(await blob.DeleteIfExistsAsync());
                Assert.IsFalse(await blob.DeleteIfExistsAsync());
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Check a blob's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                Assert.IsFalse(await blob2.ExistsAsync());

                await blob.CreateAsync(2048);

                Assert.IsTrue(await blob2.ExistsAsync());
                Assert.AreEqual(2048, blob2.Properties.Length);

                await blob.DeleteAsync();

                Assert.IsFalse(await blob2.ExistsAsync());
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobFetchAttributesAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                Assert.AreEqual(1024, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentDisposition);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob.Properties.BlobType);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1024, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
#if WINDOWS_RT && !WINDOWS_PHONE
                Assert.IsNull(blob2.Properties.CacheControl); 
#endif
                Assert.IsNull(blob2.Properties.ContentDisposition);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob2.Properties.BlobType);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
                Assert.IsNull(blob3.Properties.ContentMD5);
                byte[] target = new byte[4];
                BlobRequestOptions options2 = new BlobRequestOptions();
                options2.UseTransactionalMD5 = true;
                blob3.Properties.ContentMD5 = "MDAwMDAwMDA=";
                await blob3.DownloadRangeToByteArrayAsync(target, 0, 0, 4, null, options2, null);
                Assert.IsNull(blob3.Properties.ContentMD5);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSetPropertiesAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                string eTag = blob.Properties.ETag;
                DateTimeOffset lastModified = blob.Properties.LastModified.Value;

                await Task.Delay(1000);

                blob.Properties.CacheControl = "no-transform";
                blob.Properties.ContentDisposition = "attachment";
                blob.Properties.ContentEncoding = "gzip";
                blob.Properties.ContentLanguage = "tr,en";
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.Properties.ContentType = "text/html";
                await blob.SetPropertiesAsync();
                Assert.IsTrue(blob.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, blob.Properties.ETag);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("attachment", blob2.Properties.ContentDisposition);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
                using (MemoryStream stream = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };

                    await blob3.DownloadToStreamAsync(stream, null, options, null);
                }
                AssertAreEqual(blob2.Properties, blob3.Properties);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null);
                CloudPageBlob blob4 = (CloudPageBlob)results.Results.First();
                AssertAreEqual(blob2.Properties, blob4.Properties);

                CloudPageBlob blob5 = container.GetPageBlobReference("blob1");
                Assert.IsNull(blob5.Properties.ContentMD5);
                byte[] target = new byte[4];
                await blob5.DownloadRangeToByteArrayAsync(target, 0, 0, 4);
                Assert.AreEqual("MDAwMDAwMDA=", blob5.Properties.ContentMD5);

                CloudPageBlob blob6 = container.GetPageBlobReference("blob1");
                Assert.IsNull(blob6.Properties.ContentMD5);
                target = new byte[4];
                BlobRequestOptions options2 = new BlobRequestOptions();
                options2.UseTransactionalMD5 = true;
                await blob6.DownloadRangeToByteArrayAsync(target, 0, 0, 4, null, options2, null);
                Assert.AreEqual("MDAwMDAwMDA=", blob6.Properties.ContentMD5);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try retrieving properties of a block blob using a page blob reference")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobFetchAttributesInvalidTypeAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);

                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob1");
                OperationContext operationContext = new OperationContext();

                Assert.ThrowsException<AggregateException>(
                    () => blob2.FetchAttributesAsync(null, null, operationContext).Wait(),
                    "Fetching attributes of a page blob using a block blob reference should fail");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(InvalidOperationException));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that creating a page blob can also set its metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCreateWithMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Metadata["key1"] = "value1";
                blob.Properties.CacheControl = "no-transform";
                blob.Properties.ContentDisposition = "attachment";
                blob.Properties.ContentEncoding = "gzip";
                blob.Properties.ContentLanguage = "tr,en";
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.Properties.ContentType = "text/html";
                await blob.CreateAsync(1024);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("attachment", blob2.Properties.ContentDisposition);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that a page blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSetMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Metadata.Count);

                OperationContext operationContext = new OperationContext();
                blob.Metadata["key1"] = null;

                Assert.ThrowsException<AggregateException>(
                    () => blob.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "";
                Assert.ThrowsException<AggregateException>(
                    () => blob.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = " ";
                Assert.ThrowsException<AggregateException>(
                    () => blob.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-whitespace only value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "value1";
                await blob.SetMetadataAsync();

                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.Metadata, null, null, null, null);
                CloudPageBlob blob3 = (CloudPageBlob)results.Results.First();
                Assert.AreEqual(1, blob3.Metadata.Count);
                Assert.AreEqual("value1", blob3.Metadata["key1"]);

                blob.Metadata.Clear();
                await blob.SetMetadataAsync();

                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload/clear pages in a page blob and then verify page ranges")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobGetPageRangesAsync()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await blob.WritePagesAsync(memoryStream, 512, null);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await blob.WritePagesAsync(memoryStream, 3 * 1024, null);
                }

                await blob.ClearPagesAsync(1024, 1024);
                await blob.ClearPagesAsync(0, 512);

                IEnumerable<PageRange> pageRanges = await blob.GetPageRangesAsync();
                List<string> expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                pageRanges = await blob.GetPageRangesAsync(1024, 1024, null, null, null);
                Assert.AreEqual(0, pageRanges.Count());

                pageRanges = await blob.GetPageRangesAsync(512, 3 * 1024, null, null, null);
                expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.GetPageRangesAsync(1024, null, null, null, opContext),
                    opContext,
                    "Get Page Ranges with an offset but no count should fail",
                    HttpStatusCode.Unused);
                Assert.IsInstanceOfType(opContext.LastResult.Exception.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload pages to a page blob and then verify the contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobWritePagesAsync()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
#if NETCORE
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));
#else
            CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
            hasher.Append(buffer.AsBuffer());
            string contentMD5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset());
#endif

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(4 * 1024 * 1024);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(memoryStream, 0, null),
                        "Zero-length WritePages should fail");

                    memoryStream.SetLength(4 * 1024 * 1024 + 1);
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(memoryStream, 0, null),
                        ">4MB WritePages should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        OperationContext opContext = new OperationContext();
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await blob.WritePagesAsync(memoryStream, 512, null, null, null, opContext),
                            opContext,
                            "Writing out-of-range pages should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidPageRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await blob.WritePagesAsync(memoryStream, 0, contentMD5);
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await blob.WritePagesAsync(memoryStream, 0, contentMD5, null, null, opContext),
                            opContext,
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await blob.WritePagesAsync(memoryStream, 0, null);
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await blob.WritePagesAsync(memoryStream, 1024, null);
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream blobData = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(blobData);
                        Assert.AreEqual(resultingData.Length, blobData.Length);
                        Assert.IsTrue(blobData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload pages to a page blob from a Url and then verify the contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobWritePagesAsync_FromUrl()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);

            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync().ConfigureAwait(false);

                BlobContainerPermissions permissions = await container.GetPermissionsAsync().ConfigureAwait(false);
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                await container.SetPermissionsAsync(permissions).ConfigureAwait(false);

                CloudBlockBlob source = container.GetBlockBlobReference("source");
                await source.UploadFromByteArrayAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                Task.Delay(1000).Wait();

                CloudPageBlob dest = container.GetPageBlobReference("blob1");
                await dest.CreateAsync(buffer.Length).ConfigureAwait(false);

                await dest.WritePagesAsync(source.Uri, 0, buffer.Length, 0, contentMD5, default(AccessCondition), default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None).ConfigureAwait(false);

                using (MemoryStream resultingData = new MemoryStream())
                {
                    await dest.DownloadToStreamAsync(resultingData).ConfigureAwait(false);
                    Assert.AreEqual(resultingData.Length, buffer.Length);
                    Assert.IsTrue(resultingData.ToArray().SequenceEqual(buffer.ToArray()));
                }
            }
            finally
            {
                await container.DeleteIfExistsAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamWithAccessConditionAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("\"*\"");
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, accessCondition, operationContext, 0, true);

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, accessCondition, operationContext, 0, true),
                    operationContext,
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, accessCondition, operationContext, 0, true);

                blob = container.GetPageBlobReference("blob3");
                await blob.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, accessCondition, operationContext, 0, true),
                    operationContext,
                    "Uploading a blob on top of an non-existing blob should fail when the ETag doesn't match",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, accessCondition, operationContext, 0, true);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, null, null, 0, true);
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, null, null, null, 1024, true);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamLengthAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                // Upload half
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, 3 * 512, null, null, 0, true);
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, 3 * 512, null, null, 1024, true);

                // Upload full stream
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, 6 * 512, null, null, 0, true);
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, 4 * 512, null, null, 1024, true);

                // Exclude last page
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, 5 * 512, null, null, 0, true);
                await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 6 * 512, 3 * 512, null, null, 1024, true);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamLengthInvalidAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 3 * 512, 4 * 512, null, null, 0, false),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudPageBlobUploadFromStreamAsyncInternal(container, 3 * 512, 2 * 512, null, null, 1024, false),
                        "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        private async Task CloudPageBlobUploadFromStreamAsyncInternal(CloudBlobContainer container, int size, long? copyLength, AccessCondition accessCondition, OperationContext operationContext, int startOffset, bool testMd5)
        {
            byte[] buffer = GetRandomBuffer(size);

            string md5 = string.Empty;
            if (testMd5)
            {
#if NETCORE
                MD5 hasher = MD5.Create();
                md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
#else
                CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
                hasher.Append(buffer.AsBuffer(startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
                md5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset()); 
#endif
            }

            CloudPageBlob blob = container.GetPageBlobReference("blob1");
            blob.StreamWriteSizeInBytes = 512;

            using (MemoryStream originalBlobStream = new MemoryStream())
            {
                originalBlobStream.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        StoreBlobContentMD5 = true,
                    };
                    if (copyLength.HasValue)
                    {
                        await blob.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, options, operationContext);
                    }
                    else
                    {
                        await blob.UploadFromStreamAsync(sourceStream, accessCondition, options, operationContext); 
                    }
                }

                if (testMd5)
                {
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(md5, blob.Properties.ContentMD5); 
                }

                using (MemoryOutputStream downloadedBlobStream = new MemoryOutputStream())
                {
                    await blob.DownloadToStreamAsync(downloadedBlobStream);
                    Assert.AreEqual(copyLength ?? originalBlobStream.Length, downloadedBlobStream.UnderlyingStream.Length);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalBlobStream,
                        downloadedBlobStream.UnderlyingStream,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalBlobStream.Length);
                }
            }
        }

        [TestMethod]
        [Description("Create snapshots of a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSnapshotAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.UploadFromStreamAsync(originalData);

                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudPageBlob snapshot1 = await blob.CreateSnapshotAsync();
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudPageBlob snapshot2 = await blob.CreateSnapshotAsync();
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                await snapshot1.FetchAttributesAsync();
                await snapshot2.FetchAttributesAsync();
                await blob.FetchAttributesAsync();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudPageBlob snapshot1Clone = new CloudPageBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                await snapshot1Clone.FetchAttributesAsync();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties);

                CloudPageBlob snapshotCopy = container.GetPageBlobReference("blob2");
                await snapshotCopy.StartCopyAsync(TestHelper.Defiddler(snapshot1.Uri));
                await WaitForCopyAsync(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                await TestHelper.ExpectedExceptionAsync<InvalidOperationException>(
                    async () => await snapshot1.OpenWriteAsync(1024),
                    "Trying to write to a blob snapshot should fail");

                using (Stream snapshotStream = (await snapshot1.OpenReadAsync()))
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                await blob.CreateAsync(1024);

                using (Stream snapshotStream = (await snapshot1.OpenReadAsync()))
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, null, null, null);
                List<IListBlobItem> blobs = resultSegment.Results.ToList();
                Assert.AreEqual(4, blobs.Count);
                AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                AssertAreEqual(blob, (CloudBlob)blobs[2]);
                AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a snapshot with explicit metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSnapshotMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                await blob.SetMetadataAsync();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudPageBlob snapshot = await blob.CreateSnapshotAsync(snapshotMetadata, null, null, null);

                // Test the client view against the expected metadata
                // None of the original metadata should be present
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                // Test the server view against the expected metadata
                await snapshot.FetchAttributesAsync();
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test conditional access on a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                await blob.FetchAttributesAsync();

                string currentETag = blob.Properties.ETag;
                DateTimeOffset currentModifiedTime = blob.Properties.LastModified.Value;

                // ETag conditional tests
                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await blob.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(currentETag), null, null);

                await blob.FetchAttributesAsync();
                string newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue2";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(newETag), null, operationContext),
                    operationContext,
                    "If none match on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                string invalidETag = "\"0x10101010\"";
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(invalidETag), null, operationContext),
                    operationContext,
                    "Invalid ETag on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                currentETag = blob.Properties.ETag;
                await blob.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(invalidETag), null, null);

                await blob.FetchAttributesAsync();
                newETag = blob.Properties.ETag;

                // LastModifiedTime tests
                currentModifiedTime = blob.Properties.LastModified.Value;

                blob.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                currentModifiedTime = blob.Properties.LastModified.Value;

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                blob.Metadata["DateConditionalName"] = "DateConditionalValue2";

                currentETag = blob.Properties.ETag;
                await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null, null);

                await blob.FetchAttributesAsync();
                newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test page blob methods on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobMethodsOnBlockBlobAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                List<string> blobs = await CreateBlobsAsync(container, 1, BlobType.BlockBlob);
                CloudPageBlob blob = container.GetPageBlobReference(blobs.First());

                OperationContext operationContext = new OperationContext();
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                        operationContext,
                        "Page operations should fail on block blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
                }

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.ClearPagesAsync(0, 512, null, null, operationContext),
                    operationContext,
                    "Page operations should fail on block blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.GetPageRangesAsync(null /* offset */, null /* length */, null, null, operationContext),
                    operationContext,
                    "Page operations should fail on block blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test 512-byte page alignment")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobAlignmentAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                OperationContext operationContext = new OperationContext();

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.CreateAsync(511, null, null, operationContext),
                    operationContext,
                    "Page operations that are not 512-byte aligned should fail",
                    HttpStatusCode.BadRequest);

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.CreateAsync(513, null, null, operationContext),
                    operationContext,
                    "Page operations that are not 512-byte aligned should fail",
                    HttpStatusCode.BadRequest);

                await blob.CreateAsync(512);

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(511);
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                        "Page operations that are not 512-byte aligned should fail");
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(513);
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                        "Page operations that are not 512-byte aligned should fail");
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await blob.WritePagesAsync(stream, 0, null);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload and download null/empty data")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadDownloadNoDataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob");
                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await blob.UploadFromStreamAsync(null),
                    "Uploading from a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await blob.UploadFromStreamAsync(stream);
                }

                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await blob.DownloadToStreamAsync(null),
                    "Downloading to a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(stream);
                    Assert.AreEqual(0, stream.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List blobs with an incremental copied blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task ListBlobsWithIncrementalCopiedBlobTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                CloudPageBlob source = container.GetPageBlobReference("source");
                await source.CreateAsync(1024);

                CloudPageBlob sourceSnapshot = await source.CreateSnapshotAsync(null, null, null, null);

                CloudPageBlob copy = container.GetPageBlobReference("copy");

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
#if !FACADE_NETCORE
                string copyId = await copy.StartIncrementalCopyAsync(accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri));
#else
                Uri snapShotQualifiedUri = accountSAS.TransformUri(TestHelper.Defiddler(snapshotWithSas).SnapshotQualifiedUri);
                string copyId = await copy.StartIncrementalCopyAsync(new CloudPageBlob(new StorageUri(snapShotQualifiedUri), null, null));
#endif
                await WaitForCopyAsync(copy);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, null, null, null);

                List<IListBlobItem> listResults = results.Results.ToList();
                Assert.AreEqual(listResults.Count(), 4);

                bool incrementalCopyFound = false;
                foreach (IListBlobItem blobItem in listResults)
                {
                    CloudPageBlob blob = blobItem as CloudPageBlob;

                    if (blob.Name == "copy" && blob.IsSnapshot)
                    {
                        // Check that the incremental copied blob is found exactly once
                        Assert.IsFalse(incrementalCopyFound);
                        Assert.IsTrue(blob.Properties.IsIncrementalCopy);
                        Assert.IsTrue(blob.CopyState.DestinationSnapshotTime.HasValue);
                        incrementalCopyFound = true;
                    }
                    else if (blob.Name == "copy")
                    {
                        Assert.IsTrue(blob.CopyState.DestinationSnapshotTime.HasValue);
                    }
                }

                Assert.IsTrue(incrementalCopyFound);

            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set premium blob tier when creating a page blob and fetch attributes")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public async Task CloudPageBlobSetPremiumBlobTierOnCreateAsync()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(0, PremiumPageBlobTier.P30, null, null, null, CancellationToken.None);
                Assert.AreEqual(PremiumPageBlobTier.P30, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.BlobTierInferred.Value);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(PremiumPageBlobTier.P30, blob2.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob2.Properties.BlobTierInferred.Value);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null);
                CloudPageBlob blob3 = (CloudPageBlob)results.Results.ToList().First();
                Assert.AreEqual(PremiumPageBlobTier.P30, blob3.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob3.Properties.BlobTierInferred.HasValue);

                byte[] data = GetRandomBuffer(512);

                CloudPageBlob blob4 = container.GetPageBlobReference("blob4");
                await blob4.UploadFromByteArrayAsync(data, 0, data.Length, PremiumPageBlobTier.P10, null, null, null, CancellationToken.None);
                Assert.AreEqual(PremiumPageBlobTier.P10, blob4.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob4.Properties.BlobTierInferred.Value);

#if !NETCORE
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile inputFile = await tempFolder.CreateFileAsync("input.file", CreationCollisionOption.GenerateUniqueName);
                using (Stream file = await inputFile.OpenStreamForWriteAsync())
                {
                    await file.WriteAsync(data, 0, data.Length);
                }

                CloudPageBlob blob5 = container.GetPageBlobReference("blob5");
                await blob5.UploadFromFileAsync(inputFile, PremiumPageBlobTier.P20, null, null, null, CancellationToken.None);
                Assert.AreEqual(PremiumPageBlobTier.P20, blob5.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob5.Properties.BlobTierInferred.Value);
#endif

                using (MemoryStream memStream = new MemoryStream(data))
                {
                    CloudPageBlob blob6 = container.GetPageBlobReference("blob6");
                    await blob6.UploadFromStreamAsync(memStream, PremiumPageBlobTier.P30, null, null, null, CancellationToken.None);
                    Assert.AreEqual(PremiumPageBlobTier.P30, blob6.Properties.PremiumPageBlobTier);
                    Assert.IsFalse(blob6.Properties.BlobTierInferred.Value);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set premium blob tier and fetch attributes")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public async Task CloudPageBlobSetPremiumBlobTierAsync()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(0);

                Assert.IsFalse(blob.Properties.BlobTierInferred.HasValue);
                await blob.FetchAttributesAsync();
                Assert.IsTrue(blob.Properties.BlobTierInferred.Value);
                Assert.IsFalse(blob.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob.Properties.RehydrationStatus.HasValue);

                await blob.SetPremiumBlobTierAsync(PremiumPageBlobTier.P30);
                Assert.AreEqual(PremiumPageBlobTier.P30, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.BlobTierInferred.Value);
                Assert.IsFalse(blob.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob.Properties.RehydrationStatus.HasValue);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(PremiumPageBlobTier.P30, blob2.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob2.Properties.BlobTierInferred.Value);
                Assert.IsFalse(blob2.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob2.Properties.RehydrationStatus.HasValue);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null);
                CloudPageBlob blob3 = (CloudPageBlob)results.Results.ToList().First();
                Assert.AreEqual(PremiumPageBlobTier.P30, blob3.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob3.Properties.BlobTierInferred.HasValue);
                Assert.IsFalse(blob3.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob3.Properties.RehydrationStatus.HasValue);

                CloudPageBlob blob4 = container.GetPageBlobReference("blob4");
                await blob4.CreateAsync(125 * Constants.GB);
                try
                {
                    await blob4.SetPremiumBlobTierAsync(PremiumPageBlobTier.P6);
                    Assert.Fail("Expected failure when setting blob tier size to be less than content length");
                }
                catch (StorageException e)
                {
                    Assert.AreEqual("Specified blob tier size limit cannot be less than content length.", e.Message);
                    Assert.IsFalse(blob4.Properties.BlobTierInferred.HasValue);
                }

                try
                {
                    await blob2.SetPremiumBlobTierAsync(PremiumPageBlobTier.P4);
                    Assert.Fail("Expected failure when attempted to set the tier to a lower value than previously");
                }
                catch (StorageException e)
                {
                    Assert.AreEqual("A higher blob tier has already been explicitly set.", e.Message);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set premium blob tier when copying from an existing blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public async Task CloudPageBlobSetPremiumBlobTierOnCopyAsync()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("source");
                await blob.CreateAsync(1024, PremiumPageBlobTier.P6, null, null, null, CancellationToken.None);

                // copy to larger disk
                CloudPageBlob copy = container.GetPageBlobReference("copy");
                await copy.StartCopyAsync(blob, PremiumPageBlobTier.P10, null, null, null, null, CancellationToken.None);
                await WaitForCopyAsync(copy);
                Assert.AreEqual(PremiumPageBlobTier.P6, blob.Properties.PremiumPageBlobTier);
                Assert.AreEqual(PremiumPageBlobTier.P10, copy.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.BlobTierInferred.Value);
                Assert.IsFalse(copy.Properties.BlobTierInferred.Value);

                CloudPageBlob copyRef = container.GetPageBlobReference("copy");
                await copyRef.FetchAttributesAsync();
                Assert.AreEqual(PremiumPageBlobTier.P10, copyRef.Properties.PremiumPageBlobTier);
                Assert.IsFalse(copyRef.Properties.BlobTierInferred.Value);

                // copy where source does not have a tier
                CloudPageBlob source2 = container.GetPageBlobReference("source2");
                await source2.CreateAsync(1024);
                CloudPageBlob copy2 = container.GetPageBlobReference("copy2");
                await copy2.StartCopyAsync(TestHelper.Defiddler(source2), PremiumPageBlobTier.P20, null, null, null, null, CancellationToken.None);
                await WaitForCopyAsync(copy2);
                Assert.AreEqual(BlobType.PageBlob, copy2.BlobType);
                Assert.IsNull(source2.Properties.PremiumPageBlobTier);
                Assert.AreEqual(PremiumPageBlobTier.P20, copy2.Properties.PremiumPageBlobTier);
                Assert.IsFalse(copy2.Properties.BlobTierInferred.Value);

                // attempt to copy to a disk too small
                CloudPageBlob source3 = container.GetPageBlobReference("source3");
                source3.CreateAsync(120 * Constants.GB).Wait();
                CloudPageBlob copy3 = container.GetPageBlobReference("copy3");
                try
                {
                    copy3.StartCopyAsync(TestHelper.Defiddler(source3), PremiumPageBlobTier.P6, null, null, null, null, CancellationToken.None).Wait();
                    Assert.Fail("Expect failure when attempting to copy to too small of a disk");
                }
                catch (AggregateException e)
                {
                    Assert.AreEqual("Specified blob tier size limit cannot be less than content length.", e.InnerException.Message);
                    Assert.IsFalse(copy3.Properties.BlobTierInferred.HasValue);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("GetAccountProperties via Page Blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobGetAccountProperties()
        {
            CloudBlobContainer blobContainerWithSAS = GenerateRandomWriteOnlyBlobContainer();
            try
            {
                await blobContainerWithSAS.CreateAsync();

                CloudPageBlob blob = blobContainerWithSAS.GetPageBlobReference("test");

                AccountProperties result = await blob.GetAccountPropertiesAsync();

                await blob.DeleteIfExistsAsync();

                Assert.IsNotNull(result);

                Assert.IsNotNull(result.SkuName);

                Assert.IsNotNull(result.AccountKind);
            }
            finally
            {
                blobContainerWithSAS.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
