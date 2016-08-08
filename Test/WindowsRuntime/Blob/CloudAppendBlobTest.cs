// -----------------------------------------------------------------------------------------
// <copyright file="CloudAppendBlobTest.cs" company="Microsoft">
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
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

#if NETCORE
    using System.Security.Cryptography;
    using System.Text;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using System.Text;
#endif

    [TestClass]
    public class CloudAppendBlobTest : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        public CloudAppendBlobTest()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

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
        [Description("Create a zero-length append blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobCreateAndDeleteAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();
                Assert.IsTrue(await blob.ExistsAsync());
                await blob.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobDeleteIfExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                Assert.IsFalse(await blob.DeleteIfExistsAsync());
                await blob.CreateOrReplaceAsync();
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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");

                Assert.IsFalse(await blob2.ExistsAsync());

                await blob.CreateOrReplaceAsync();

                Assert.IsTrue(await blob2.ExistsAsync());
                Assert.AreEqual(0, blob2.Properties.Length);

                await blob.DeleteAsync();

                Assert.IsFalse(await blob2.ExistsAsync());
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobFetchAttributesAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();
                Assert.AreEqual(0, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.AppendBlob, blob.Properties.BlobType);

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.AppendBlob, blob2.Properties.BlobType);
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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobSetPropertiesAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();
                string eTag = blob.Properties.ETag;
                DateTimeOffset lastModified = blob.Properties.LastModified.Value;

                await Task.Delay(1000);

                blob.Properties.CacheControl = "no-transform";
                blob.Properties.ContentEncoding = "gzip";
                blob.Properties.ContentLanguage = "tr,en";
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.Properties.ContentType = "text/html";
                await blob.SetPropertiesAsync();
                Assert.IsTrue(blob.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, blob.Properties.ETag);

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudAppendBlob blob3 = container.GetAppendBlobReference("blob1");
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
                CloudAppendBlob blob4 = (CloudAppendBlob)results.Results.First();
                AssertAreEqual(blob2.Properties, blob4.Properties);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try retrieving properties of a block blob using an append blob reference")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobFetchAttributesInvalidTypeAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob1");
                OperationContext operationContext = new OperationContext();

                Assert.ThrowsException<AggregateException>(
                    () => blob2.FetchAttributesAsync(null, null, operationContext).Wait(),
                    "Fetching attributes of an append blob using a block blob reference should fail");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(InvalidOperationException));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that creating an append blob can also set its metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobCreateWithMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.Metadata["key1"] = "value1";
                await blob.CreateOrReplaceAsync();

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that an append blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobSetMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
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
                CloudAppendBlob blob3 = (CloudAppendBlob)results.Results.First();
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
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobAppendBlockAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await this.CloudAppendBlobUploadFromStreamAsync(container, 6 * 512, null, null, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 6 * 512, null, null, 1024);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        private async Task CloudAppendBlobUploadFromStreamAsync(CloudBlobContainer container, int size, AccessCondition accessCondition, OperationContext operationContext, int startOffset)
        {
            byte[] buffer = GetRandomBuffer(size);

            CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
            await blob.CreateOrReplaceAsync();

            using (MemoryStream originalBlobStream = new MemoryStream())
            {
                originalBlobStream.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    await blob.AppendBlockAsync(sourceStream, null, accessCondition, null, operationContext);
                }

                using (MemoryStream downloadedBlobStream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(downloadedBlobStream);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalBlobStream,
                        downloadedBlobStream,
                        0,
                        0,
                        (int)originalBlobStream.Length);
                }
            }
        }

        [TestMethod]
        [Description("Upload/Download text")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobDownloadTextAsync()
        {
            await this.DoTextUploadDownloadAsync("test");
            await this.DoTextUploadDownloadAsync("char中文test");
        }

        private async Task DoTextUploadDownloadAsync(string text)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateIfNotExistsAsync();
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");

                // Default encoding
                await blob.UploadTextAsync(text);
                Assert.AreEqual(text, await blob.DownloadTextAsync());
                Assert.AreNotEqual(text, await blob.DownloadTextAsync(Encoding.Unicode, null, null, null));
				
                // Custom Encoding
				await blob.UploadTextAsync(text, Encoding.Unicode, null, null, null);
                Assert.AreEqual(text, await blob.DownloadTextAsync(Encoding.Unicode, null, null, null));
                Assert.AreNotEqual(text, await blob.DownloadTextAsync());

                // Number of service calls
                OperationContext context = new OperationContext();
                await blob.UploadTextAsync(text, null, null, null, context);

                // 3 because of Create and Appendblock.
                Assert.AreEqual(2, context.RequestResults.Count);
                await blob.DownloadTextAsync(Encoding.Unicode, null, null, context);
                Assert.AreEqual(3, context.RequestResults.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create snapshots of an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobSnapshotAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();
                await blob.AppendBlockAsync(originalData, null);
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudAppendBlob snapshot1 = await blob.CreateSnapshotAsync();
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudAppendBlob snapshot2 = await blob.CreateSnapshotAsync();
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                await snapshot1.FetchAttributesAsync();
                await snapshot2.FetchAttributesAsync();
                await blob.FetchAttributesAsync();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudAppendBlob snapshot1Clone = new CloudAppendBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                await snapshot1Clone.FetchAttributesAsync();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties, false);

                CloudAppendBlob snapshotCopy = container.GetAppendBlobReference("blob2");
                await snapshotCopy.StartCopyAsync(TestHelper.Defiddler(snapshot1.Uri));
                await WaitForCopyAsync(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                using (Stream snapshotStream = (await snapshot1.OpenReadAsync()))
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                await blob.CreateOrReplaceAsync();

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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobSnapshotMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                await blob.SetMetadataAsync();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudAppendBlob snapshot = await blob.CreateSnapshotAsync(snapshotMetadata, null, null, null);

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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();
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
        [Description("Validate UploadFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobValidateUploadFromStreamAsync()
        {
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");

                using (MemoryStream memStream = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(memStream);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(memStream);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(memStream, null /* accessCondition */, null /* options */, null /* operationContext */);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobValidateAppendFromStreamAsync()
        {
            // Every time append a buffer that is bigger than a single block.
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                using (MemoryStream memStream = new MemoryStream(buffer))
                {
                    await blob.AppendFromStreamAsync(memStream);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    await blob.AppendFromStreamAsync(memStream);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(12 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    await blob.AppendFromStreamAsync(memStream, null /* accessCondition */, null /* options */, null /* operationContext */);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(18 * 1024 * 1024, blob.Properties.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobValidateAppendFromStreamWithLengthAsync()
        {
            // Every time append a buffer that is bigger than a single block.
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                using (MemoryStream memStream = new MemoryStream(buffer))
                {
                    await blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(5 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    await blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(10 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    await blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024, null /* accessCondition */, null /* options */, null /* operationContext */);
                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(15 * 1024 * 1024, blob.Properties.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify the append offset returned by the service.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobVerifyAppendOffsetAsync()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                long offset = await blob.AppendBlockAsync(new MemoryStream(buffer));
                Assert.AreEqual(0, offset);

                offset = await blob.AppendBlockAsync(new MemoryStream(buffer));
                Assert.AreEqual(2 * 1024 * 1024, offset);

                offset = await blob.AppendBlockAsync(new MemoryStream(buffer));
                Assert.AreEqual(4 * 1024 * 1024, offset);

                offset = await blob.AppendBlockAsync(new MemoryStream(buffer));
                Assert.AreEqual(6 * 1024 * 1024, offset);
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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobAppendOffsetConditionalAccessAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            OperationContext opContext = new OperationContext();

            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);

            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    await blob.AppendBlockAsync(originalBlob, null, AccessCondition.GenerateIfAppendPositionEqualCondition(0), null, null);

                    // Seek and upload the 1MB again
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    await blob.AppendBlockAsync(originalBlob, null, AccessCondition.GenerateIfAppendPositionEqualCondition(1 * 1024 * 1024), null, null);

                    // Seek and upload the 1MB again. This time it should throw since the append offset does not match
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.AppendBlockAsync(originalBlob, null, AccessCondition.GenerateIfAppendPositionEqualCondition(1 * 1024 * 1024), null, opContext),
                    opContext,
                    "IfAppendPositionEqual conditional should throw",
                    HttpStatusCode.PreconditionFailed,
                    "AppendPositionConditionNotMet");
                }
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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobMaxSizeConditionalAccessAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            OperationContext opContext = new OperationContext();
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);

            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                await blob.CreateOrReplaceAsync();

                AccessCondition condition = AccessCondition.GenerateIfMaxSizeLessThanOrEqualCondition(2 * 1024 * 1024);
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    await blob.AppendBlockAsync(originalBlob, null, condition, null, null);

                    // Seek and upload the 1MB again
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    await blob.AppendBlockAsync(originalBlob, null, condition, null, null);

                    // Seek and try to upload the 1MB again. This time it should fail with a Pre-condition failed error.
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.AppendBlockAsync(originalBlob, null, condition, null, opContext),
                    opContext,
                    "IfMaxSizeLessThanOrEqual conditional should throw",
                    HttpStatusCode.PreconditionFailed,
                    "MaxBlobSizeConditionNotMet");

                    originalBlob.Seek(0, SeekOrigin.Begin);
                    await blob.AppendBlockAsync(originalBlob, null, AccessCondition.GenerateIfMaxSizeLessThanOrEqualCondition(3 * 1024 * 1024), null, null);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test append blob methods on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobMethodsOnBlockBlobAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                List<string> blobs = await CreateBlobsAsync(container, 1, BlobType.BlockBlob);
                CloudAppendBlob blob = container.GetAppendBlobReference(blobs.First());

                OperationContext operationContext = new OperationContext();
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.AppendBlockAsync(stream, null, null, null, operationContext),
                        operationContext,
                        "Append operations should fail on block blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test block blob methods on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobMethodsOnAppendBlobAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                List<string> blocks = GetBlockIdList(1);
                List<string> blobs = await CreateBlobsAsync(container, 1, BlobType.AppendBlob);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobs.First());

                OperationContext operationContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.PutBlockListAsync(blocks, null, null, operationContext),
                        operationContext,
                        "Block operations should fail on append blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test page blob methods on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobMethodsOnAppendBlobAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                List<string> blobs = await CreateBlobsAsync(container, 1, BlobType.AppendBlob);
                CloudPageBlob blob = container.GetPageBlobReference(blobs.First());

                OperationContext operationContext = new OperationContext();

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await TestHelper.ExpectedExceptionAsync(
                            async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                            operationContext,
                            "Page operations should fail on append blobs",
                            HttpStatusCode.Conflict,
                            "InvalidBlobType");
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobUploadFromStreamAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, null, null, true, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, null, null, true, 1024);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, null, null, false, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, null, null, false, 1024);
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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobUploadFromStreamLengthAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                // Upload 2MB of 5MB stream
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, 1024);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, false, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, false, 1024);

                // Exclude last byte
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 5 * 1024 * 1024 - 1, null, true, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 4 * 1024 * 1024 - 1, null, true, 1024);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 5 * 1024 * 1024 - 1, null, false, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 4 * 1024 * 1024 - 1, null, false, 1024);

                // Upload exact amount
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 5 * 1024 * 1024, null, true, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 4 * 1024 * 1024, null, true, 1024);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 5 * 1024 * 1024, null, false, 0);
                await this.CloudAppendBlobUploadFromStreamAsync(container, 5 * 1024 * 1024, 4 * 1024 * 1024, null, false, 1024);
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
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobUploadFromStreamInvalidLengthAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudAppendBlobUploadFromStreamAsync(container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, true, 0),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudAppendBlobUploadFromStreamAsync(container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, true, 1024),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudAppendBlobUploadFromStreamAsync(container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, false, 0),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudAppendBlobUploadFromStreamAsync(container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, false, 1024),
                        "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        private async Task CloudAppendBlobUploadFromStreamAsync(CloudBlobContainer container, int size, long? copyLength, AccessCondition accessCondition, bool seekableSourceStream, int startOffset)
        {
            byte[] buffer = GetRandomBuffer(size);

            CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
            blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;

            using (MemoryStream originalBlobStream = new MemoryStream())
            {
                originalBlobStream.Write(buffer, startOffset, buffer.Length - startOffset);

                Stream sourceStream;
                if (seekableSourceStream)
                {
                    MemoryStream stream = new MemoryStream(buffer);
                    stream.Seek(startOffset, SeekOrigin.Begin);
                    sourceStream = stream;
                }
                else
                {
                    NonSeekableMemoryStream stream = new NonSeekableMemoryStream(buffer);
                    stream.ForceSeek(startOffset, SeekOrigin.Begin);
                    sourceStream = stream;
                }

                using (sourceStream)
                {
                    if (copyLength.HasValue)
                    {
                        await blob.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, null, null);
                    }
                    else
                    {
                        await blob.UploadFromStreamAsync(sourceStream, accessCondition, null, null);
                    }

                }

                using (MemoryStream downloadedBlobStream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(downloadedBlobStream);

                    Assert.AreEqual(copyLength ?? originalBlobStream.Length, downloadedBlobStream.Length);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalBlobStream,
                        downloadedBlobStream,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalBlobStream.Length);
                }
            }
        }
    }
}
