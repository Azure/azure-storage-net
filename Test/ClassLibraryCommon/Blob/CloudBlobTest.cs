// -----------------------------------------------------------------------------------------
// <copyright file="CloudBlobTest.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class CloudBlobTest : BlobTestBase
    {
        const string BlobName = "blob1";

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
        [Description("Create snapshots of a blob sync")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshot()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                // Upload some data to the blob.
                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplace();
                appendBlob.AppendBlock(originalData, null);

                CloudBlob blob = container.GetBlobReference(BlobName);
                blob.FetchAttributes();
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudBlob snapshot1 = blob.Snapshot();
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudBlob snapshot2 = blob.Snapshot();
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                snapshot1.FetchAttributes();
                snapshot2.FetchAttributes();
                blob.FetchAttributes();
                AssertAreEqual(snapshot1.Properties, blob.Properties, false);

                CloudBlob snapshot1Clone = new CloudBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                snapshot1Clone.FetchAttributes();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties, false);

                // The query parser should not be case sensitive in detecting a snapshot in the query string
                CloudBlob snapshotParseVerifier = new CloudBlob(new Uri(blob.Uri + "?sNapshOt=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshotParseVerifier.SnapshotTime, "Snapshot parse verifier did not successfully detect the snapshot time");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshotParseVerifier.SnapshotTime.Value);

                CloudBlob snapshotCopy = container.GetBlobReference("blob2");
                snapshotCopy.StartCopy(TestHelper.Defiddler(snapshot1.Uri));
                WaitForCopy(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                using (Stream snapshotStream = snapshot1.OpenRead())
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                //overwriting the blob and creating the 5th snapshot
                appendBlob.CreateOrReplace();
                blob.FetchAttributes();

                using (Stream snapshotStream = snapshot1.OpenRead())
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                List<IListBlobItem> blobs = container.ListBlobs(null, true, BlobListingDetails.All, null, null).ToList();
                Assert.AreEqual(4, blobs.Count);
                AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                AssertAreEqual(blob, (CloudBlob)blobs[2]);
                AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create snapshots of a blob APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshotAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplace();
                appendBlob.AppendBlock(originalData, null);

                CloudBlob blob = container.GetBlobReference(BlobName);
                blob.FetchAttributes();
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginSnapshot(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudBlob snapshot1 = blob.EndSnapshot(result);
                    Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                    Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                    Assert.IsTrue(snapshot1.IsSnapshot);
                    Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                    Assert.AreEqual(blob.Uri, snapshot1.Uri);
                    Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                    Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                    Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                    result = blob.BeginSnapshot(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudBlob snapshot2 = blob.EndSnapshot(result);
                    Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                    snapshot1.FetchAttributes();
                    snapshot2.FetchAttributes();
                    blob.FetchAttributes();
                    AssertAreEqual(snapshot1.Properties, blob.Properties);

                    CloudBlob snapshotCopy = container.GetBlobReference("blob2");
                    result = snapshotCopy.BeginStartCopy(snapshot1.Uri, null, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    snapshotCopy.EndStartCopy(result);
                    WaitForCopy(snapshotCopy);
                    Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                    result = snapshot1.BeginOpenRead(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    using (Stream snapshotStream = snapshot1.EndOpenRead(result))
                    {
                        snapshotStream.Seek(0, SeekOrigin.End);
                        TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                    }

                    result = appendBlob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    appendBlob.EndCreateOrReplace(result);
                    result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndFetchAttributes(result);

                    result = snapshot1.BeginOpenRead(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    using (Stream snapshotStream = snapshot1.EndOpenRead(result))
                    {
                        snapshotStream.Seek(0, SeekOrigin.End);
                        TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                    }

                    List<IListBlobItem> blobs = container.ListBlobs(null, true, BlobListingDetails.All, null, null).ToList();
                    Assert.AreEqual(4, blobs.Count);
                    AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                    AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                    AssertAreEqual(blob, (CloudBlob)blobs[2]);
                    AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create snapshots of a blob Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshotTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplaceAsync().Wait();
                appendBlob.AppendBlockAsync(originalData, null).Wait();

                CloudBlob blob = container.GetBlobReference(BlobName);
                blob.FetchAttributesAsync().Wait();
                CloudBlob snapshot1 = blob.SnapshotAsync().Result;
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudBlob snapshot2 = blob.SnapshotAsync().Result;
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                snapshot1.FetchAttributesAsync().Wait();
                snapshot2.FetchAttributesAsync().Wait();
                blob.FetchAttributesAsync().Wait();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudBlob snapshot1Clone = new CloudBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                snapshot1Clone.FetchAttributesAsync().Wait();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties);

                CloudBlob snapshotCopy = container.GetBlobReference("blob2");
                snapshotCopy.StartCopyAsync(snapshot1.Uri, null, null, null, null).Wait();
                WaitForCopy(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                using (Stream snapshotStream = snapshot1.OpenReadAsync().Result)
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                //overwriting will create another snapshot
                appendBlob.CreateOrReplaceAsync().Wait();
                blob.FetchAttributesAsync().Wait();

                using (Stream snapshotStream = snapshot1.OpenReadAsync().Result)
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                List<IListBlobItem> blobs =
                    container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, null, null, null)
                             .Result
                             .Results
                             .ToList();
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
#endif

        [TestMethod]
        [Description("Create a snapshot with explicit metadata Sync")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshotMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplace();

                CloudBlob blob = container.GetBlobReference(BlobName);
                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadata();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudBlob snapshot = blob.Snapshot(snapshotMetadata);

                // Test the client view against the expected metadata
                // None of the original metadata should be present
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                // Test the server view against the expected metadata
                snapshot.FetchAttributes();
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a snapshot with explicit metadata APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshotMetadataAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplace();

                CloudBlob blob = container.GetBlobReference(BlobName);
                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadata();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginSnapshot(snapshotMetadata, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudBlob snapshot = blob.EndSnapshot(result);

                    // Test the client view against the expected metadata
                    // None of the original metadata should be present
                    Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                    Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                    Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                    // Test the server view against the expected metadata
                    snapshot.FetchAttributes();
                    Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                    Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                    Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a snapshot with explicit metadata Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshotMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplaceAsync(null, null, new OperationContext()).Wait();

                CloudBlob blob = container.GetBlobReference(BlobName);
                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadataAsync().Wait();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudBlob snapshot = blob.SnapshotAsync(snapshotMetadata, null, null, null).Result;

                // Test the client view against the expected metadata
                // None of the original metadata should be present
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                // Test the server view against the expected metadata
                snapshot.FetchAttributesAsync().Wait();
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Ensure different regions can parse blob headers.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobLocaleParsing()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            var currCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                const string blobname = "tempfile";
                container.Create();
                var blockBlob = container.GetBlockBlobReference(blobname);
                OperationContext context = new OperationContext();

                blockBlob.UploadText("placeholder", null, null, null, context);

                foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                {
                    Thread.CurrentThread.CurrentCulture = culture;
                    Assert.IsTrue(blockBlob.Exists()); // parses the header to ensure can be parsed across cultures
                }                                      // failed assertion means we never tested the parser
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currCulture;
                container.DeleteIfExists();
            }
        }

        #region Soft-Delete

        #region SYNC

        [TestMethod]
        [Description("Soft Delete and Undelete a blob with no snapshot")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSoftDeleteNoSnapshot()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                //Enables a delete retention policy on the blob with 1 day of default retention days
                container.ServiceClient.EnableSoftDelete();
                Thread.Sleep(15000);
                container.Create();

                // Upload some data to the blob.
                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplace();
                appendBlob.AppendBlock(originalData, null);
                
                
                CloudBlob blob = container.GetBlobReference(BlobName);

                Assert.IsTrue(blob.Exists());
                Assert.IsFalse(blob.IsDeleted);

                blob.Delete();
                Assert.IsFalse(blob.Exists());

                int blobCount = 0;
                foreach(IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.All))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsTrue(blobItem.IsDeleted);
                    Assert.IsNotNull(blobItem.Properties.DeletedTime);
                    Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 1);

                blob.Undelete();

                blob.FetchAttributes();
                Assert.IsFalse(blob.IsDeleted);
                Assert.IsNull(blob.Properties.DeletedTime);
                Assert.IsNull(blob.Properties.RemainingDaysBeforePermanentDelete);

                blobCount = 0;
                foreach (IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.All))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsFalse(blobItem.IsDeleted);
                    Assert.IsNull(blobItem.Properties.DeletedTime);
                    Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 1);

            }
            finally
            {
                container.ServiceClient.DisableSoftDelete();
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Soft Delete and Undelete a blob with snapshots")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSoftDeleteSnapshot()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                //Enables a delete retention policy on the blob with 1 day of default retention days
                container.ServiceClient.EnableSoftDelete();
                container.Create();

                // Upload some data to the blob.
                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.UploadFromStream(originalData);

                CloudBlob blob = container.GetBlobReference(BlobName);

                //create snapshot via api
                CloudBlob snapshot = blob.Snapshot();
                //create snapshot via write protection
                appendBlob.UploadFromStream(originalData);

                //we should have 2 snapshots 1 regular and 1 deleted: there is no way to get only the deleted snapshots but the below listing will get both snapshot types
                int blobCount = 0;
                int deletedSnapshotCount = 0;
                int snapShotCount = 0;
                foreach (IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.Snapshots | BlobListingDetails.Deleted))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    if (blobItem.IsSnapshot)
                        snapShotCount++;
                    if (blobItem.IsDeleted)
                    {
                        Assert.IsNotNull(blobItem.Properties.DeletedTime);
                        Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                        deletedSnapshotCount++;
                    }
                    blobCount++;
                }
                Assert.AreEqual(blobCount, 3);
                Assert.AreEqual(deletedSnapshotCount, 1);
                Assert.AreEqual(snapShotCount, 2);

                blobCount = 0;
                foreach (IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.Snapshots))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsFalse(blobItem.IsDeleted);
                    Assert.IsNull(blobItem.Properties.DeletedTime);
                    Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 2);

                //Delete Blob and snapshots
                blob.Delete(DeleteSnapshotsOption.IncludeSnapshots);
                Assert.IsFalse(blob.Exists());
                Assert.IsFalse(snapshot.Exists());

                blobCount = 0;
                foreach (IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.All))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsTrue(blobItem.IsDeleted);
                    Assert.IsNotNull(blobItem.Properties.DeletedTime);
                    Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 3);

                blob.Undelete();

                blob.FetchAttributes();
                Assert.IsFalse(blob.IsDeleted);
                Assert.IsNull(blob.Properties.DeletedTime);
                Assert.IsNull(blob.Properties.RemainingDaysBeforePermanentDelete);

                blobCount = 0;
                foreach (IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.All))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsFalse(blobItem.IsDeleted);
                    Assert.IsNull(blobItem.Properties.DeletedTime);
                    Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 3);

            }
            finally
            {
                container.ServiceClient.DisableSoftDelete();
                container.DeleteIfExists();
            }
        }
        #endregion

        #region APM        
        [TestMethod]
        [Description("Soft Delete and Undelete a blob with no snapshot APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSoftDeleteNoSnapshotAPM()
        {         
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                //Enables a delete retention policy on the blob with 1 day of default retention days
                container.ServiceClient.EnableSoftDelete();
                container.Create();

                // Upload some data to the blob.
                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplace();
                appendBlob.AppendBlock(originalData, null);
                
                
                CloudBlob blob = container.GetBlobReference(BlobName);
                Assert.IsFalse(blob.IsDeleted);

                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginDelete(DeleteSnapshotsOption.None, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndDelete(result);
                }

                Assert.IsFalse(blob.Exists());

                int blobCount = 0;
                foreach(IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.All))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsTrue(blobItem.IsDeleted);
                    Assert.IsNotNull(blobItem.Properties.DeletedTime);
                    Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 1);

                using(AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginUndelete(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndUndelete(result);
                }

                blob.FetchAttributes();
                Assert.IsFalse(blob.IsDeleted);
                Assert.IsNull(blob.Properties.DeletedTime);
                Assert.IsNull(blob.Properties.RemainingDaysBeforePermanentDelete);

                blobCount = 0;
                foreach (IListBlobItem item in container.ListBlobs(null, true, BlobListingDetails.All))
                {
                    CloudAppendBlob blobItem = (CloudAppendBlob)item;
                    Assert.AreEqual(blobItem.Name, BlobName);
                    Assert.IsFalse(blobItem.IsDeleted);
                    Assert.IsNull(blobItem.Properties.DeletedTime);
                    Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                    blobCount++;
                }

                Assert.AreEqual(blobCount, 1);

            }
            finally
            {
                container.ServiceClient.DisableSoftDelete();
                container.DeleteIfExists();
            }
        }

        #endregion

        #region TASK
        [TestMethod]
        [Description("Soft Delete and Undelete a blob with no snapshot TASK")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSoftDeleteNoSnapshotTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                //Enables a delete retention policy on the blob with 1 day of default retention days
                container.ServiceClient.EnableSoftDelete();
                container.CreateAsync().Wait();

                // Upload some data to the blob.
                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.CreateOrReplaceAsync().Wait();
                appendBlob.AppendBlockAsync(originalData, null).Wait();


                CloudBlob blob = container.GetBlobReference(BlobName);
                Assert.IsTrue(blob.ExistsAsync().Result);
                Assert.IsFalse(blob.IsDeleted);

                blob.DeleteAsync().Wait();
                Assert.IsFalse(blob.ExistsAsync().Result);

                int blobCount = 0;
                BlobContinuationToken ct = null;
                do
                {
                    foreach (IListBlobItem item in container.ListBlobsSegmentedAsync
                        (null, true, BlobListingDetails.All, null, ct, null, null)
                        .Result
                        .Results
                        .ToList())
                    {
                        CloudAppendBlob blobItem = (CloudAppendBlob)item;
                        Assert.AreEqual(blobItem.Name, BlobName);
                        Assert.IsTrue(blobItem.IsDeleted);
                        Assert.IsNotNull(blobItem.Properties.DeletedTime);
                        Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                        blobCount++;
                    }
                } while (ct != null);

                Assert.AreEqual(blobCount, 1);

                blob.UndeleteAsync().Wait();

                blob.FetchAttributes();
                Assert.IsFalse(blob.IsDeleted);
                Assert.IsNull(blob.Properties.DeletedTime);
                Assert.IsNull(blob.Properties.RemainingDaysBeforePermanentDelete);

                blobCount = 0;
                ct = null;
                do
                {
                    foreach (IListBlobItem item in container.ListBlobsSegmentedAsync
                        (null, true, BlobListingDetails.All, null, ct, null, null)
                        .Result
                        .Results
                        .ToList())
                    {
                        CloudAppendBlob blobItem = (CloudAppendBlob)item;
                        Assert.AreEqual(blobItem.Name, BlobName);
                        Assert.IsFalse(blobItem.IsDeleted);
                        Assert.IsNull(blobItem.Properties.DeletedTime);
                        Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                        blobCount++;
                    }
                } while (ct != null);
                Assert.AreEqual(blobCount, 1);

            }
            finally
            {
                container.ServiceClient.DisableSoftDelete();
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Soft Delete and Undelete a blob with snapshots TASK")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSoftDeleteSnapshotTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                //Enables a delete retention policy on the blob with 1 day of default retention days
                container.ServiceClient.EnableSoftDelete();
                container.Create();

                // Upload some data to the blob.
                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(BlobName);
                appendBlob.UploadFromStream(originalData);

                CloudBlob blob = container.GetBlobReference(BlobName);

                //create snapshot via api
                CloudBlob snapshot = blob.Snapshot();
                //create snapshot via write protection
                appendBlob.UploadFromStream(originalData);

                //we should have 2 snapshots 1 regular and 1 deleted: there is no way to get only the deleted snapshots but the below listing will get both snapshot types
                int blobCount = 0;
                int deletedSnapshotCount = 0;
                int snapShotCount = 0;
                BlobContinuationToken ct = null;
                do
                {
                    foreach (IListBlobItem item in container.ListBlobsSegmentedAsync
                        (null, true, BlobListingDetails.Snapshots | BlobListingDetails.Deleted, null, ct, null, null)
                        .Result
                        .Results
                        .ToList())
                    {
                        CloudAppendBlob blobItem = (CloudAppendBlob)item;
                        Assert.AreEqual(blobItem.Name, BlobName);
                        if (blobItem.IsSnapshot)
                            snapShotCount++;
                        if (blobItem.IsDeleted)
                        {
                            Assert.IsNotNull(blobItem.Properties.DeletedTime);
                            Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                            deletedSnapshotCount++;
                        }
                        blobCount++;
                    }
                } while (ct != null);

                Assert.AreEqual(blobCount, 3);
                Assert.AreEqual(deletedSnapshotCount, 1);
                Assert.AreEqual(snapShotCount, 2);

                blobCount = 0;
                ct = null;
                do
                {
                    foreach (IListBlobItem item in container.ListBlobsSegmentedAsync
                        (null, true, BlobListingDetails.Snapshots, null, ct, null, null)
                        .Result
                        .Results
                        .ToList())
                    {
                        CloudAppendBlob blobItem = (CloudAppendBlob)item;
                        Assert.AreEqual(blobItem.Name, BlobName);
                        Assert.IsFalse(blobItem.IsDeleted);
                        Assert.IsNull(blobItem.Properties.DeletedTime);
                        Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                        blobCount++;
                    }
                } while (ct != null);

                Assert.AreEqual(blobCount, 2);

                //Delete Blob and snapshots
                blob.Delete(DeleteSnapshotsOption.IncludeSnapshots);
                Assert.IsFalse(blob.Exists());
                Assert.IsFalse(snapshot.Exists());

                blobCount = 0;
                ct = null;
                do
                {
                    foreach (IListBlobItem item in container.ListBlobsSegmentedAsync
                        (null, true, BlobListingDetails.All, null, ct, null, null)
                        .Result
                        .Results
                        .ToList())
                    {
                        CloudAppendBlob blobItem = (CloudAppendBlob)item;
                        Assert.AreEqual(blobItem.Name, BlobName);
                        Assert.IsTrue(blobItem.IsDeleted);
                        Assert.IsNotNull(blobItem.Properties.DeletedTime);
                        Assert.AreEqual(blobItem.Properties.RemainingDaysBeforePermanentDelete, 0);
                        blobCount++;
                    }
                }while(ct != null);

                Assert.AreEqual(blobCount, 3);

                blob.Undelete();

                blob.FetchAttributes();
                Assert.IsFalse(blob.IsDeleted);
                Assert.IsNull(blob.Properties.DeletedTime);
                Assert.IsNull(blob.Properties.RemainingDaysBeforePermanentDelete);

                blobCount = 0;
                ct = null;
                do
                {
                    foreach (IListBlobItem item in container.ListBlobsSegmentedAsync
                        (null, true, BlobListingDetails.All, null, ct, null, null)
                        .Result
                        .Results
                        .ToList())
                    {
                        CloudAppendBlob blobItem = (CloudAppendBlob)item;
                        Assert.AreEqual(blobItem.Name, BlobName);
                        Assert.IsFalse(blobItem.IsDeleted);
                        Assert.IsNull(blobItem.Properties.DeletedTime);
                        Assert.IsNull(blobItem.Properties.RemainingDaysBeforePermanentDelete);
                        blobCount++;
                    }
                }while(ct != null);

                Assert.AreEqual(blobCount, 3);

            }
            finally
            {
                container.ServiceClient.DisableSoftDelete();
                container.DeleteIfExists();
            }
        }
        #endregion
        #endregion
    }
}