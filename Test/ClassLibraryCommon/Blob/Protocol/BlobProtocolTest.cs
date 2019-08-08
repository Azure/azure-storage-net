// -----------------------------------------------------------------------------------------
// <copyright file="BlobProtocolTest.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    [TestClass]
    public class BlobProtocolTest : TestBase
    {
        private static Random random = new Random();

        private static BlobClientTests cloudOwnerSync = new BlobClientTests(true, false, 30);
        private static BlobClientTests cloudAnonSync = new BlobClientTests(false, false, 30);
        private static BlobClientTests cloudOwnerAsync = new BlobClientTests(true, false, 30);
        private static BlobClientTests cloudAnonAsync = new BlobClientTests(false, false, 30);

        private static BlobClientTests cloudSetup = new BlobClientTests(true, false, 30);

        [ClassInitialize]
        public static void InitialInitialize(TestContext testContext)
        {
            try
            {
                cloudSetup.Initialize().Wait();
            }
            catch
            {
                FinalCleanup();
            }
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            cloudSetup.Cleanup().Wait();

            // sleep for 40s so that if the test is re-run, we can recreate the container
            Thread.Sleep(35000);
        }

        #region PutPageBlob
        [TestMethod]
        [Description("owner, sync : Make a valid Put Index Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutPageBlobCloudOwnerSync()
        {
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.PageBlob };
            await cloudOwnerSync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(), properties, BlobType.PageBlob, new byte[0], HttpStatusCode.Created);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Put Index Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutPageBlobCloudAnonSync()
        {
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.PageBlob };
            await cloudAnonSync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(),
                properties, BlobType.PageBlob, new byte[0], HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, isAsync : Make a valid Put Index Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutPageBlobCloudOwnerAsync()
        {
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.PageBlob };
            await cloudOwnerAsync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(), properties, BlobType.PageBlob, new byte[0], HttpStatusCode.Created);
        }

        [TestMethod]
        [Description("anonymous, isAsync : Make an invalid Put Index Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutPageBlobCloudAnonAsync()
        {
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.PageBlob };
            await cloudAnonAsync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(),
                properties, BlobType.PageBlob, new byte[0], HttpStatusCode.NotFound);
        }
        #endregion

        #region PutBlockBlob
        [TestMethod]
        [Description("owner, sync : Make a valid Put Stream Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutBlockBlobCloudOwnerSync()
        {
            byte[] content = new byte[6000];
            random.NextBytes(content);
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.BlockBlob };
            await cloudOwnerSync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(), properties, BlobType.BlockBlob, content, null);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Put Stream Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutBlockBlobCloudAnonSync()
        {
            byte[] content = new byte[6000];
            random.NextBytes(content);
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.BlockBlob };
            await cloudAnonSync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(),
                properties, BlobType.BlockBlob, content, HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, isAsync : Make a valid Put Stream Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutBlockBlobCloudOwnerAsync()
        {
            byte[] content = new byte[6000];
            random.NextBytes(content);
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.BlockBlob };
            await cloudOwnerAsync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(), properties, BlobType.BlockBlob, content, null);
        }

        [TestMethod]
        [Description("anonymous, isAsync : Make an invalid Put Stream Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutBlockBlobCloudAnonAsync()
        {
            byte[] content = new byte[6000];
            random.NextBytes(content);
            BlobProperties properties = new BlobProperties() { BlobType = BlobType.BlockBlob };
            await cloudAnonAsync.PutBlobScenarioTest(cloudSetup.ContainerName, Guid.NewGuid().ToString(),
                properties, BlobType.BlockBlob, content, HttpStatusCode.NotFound);
        }
        #endregion

        #region Blob
        [TestMethod]
        [Description("owner, sync : Make a valid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobCloudOwnerSync()
        {
            await cloudOwnerSync.GetBlobScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, null);
        }

        [TestMethod]
        [Description("owner, isAsync : Make a valid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobCloudOwnerAsync()
        {
            await cloudOwnerAsync.GetBlobScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, null);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobCloudAnonSync()
        {
            await cloudAnonSync.GetBlobScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("anonymous, isAsync : Make an invalid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobCloudAnonAsync()
        {
            await cloudAnonAsync.GetBlobScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, sync : Make a public valid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetPublicBlobCloudOwnerSync()
        {
            await cloudOwnerSync.GetBlobScenarioTest(cloudSetup.PublicContainerName, cloudSetup.PublicBlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, null);
        }

        [TestMethod]
        [Description("owner, isAsync : Make a public valid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetPublicBlobCloudOwnerAsync()
        {
            await cloudOwnerAsync.GetBlobScenarioTest(cloudSetup.PublicContainerName, cloudSetup.PublicBlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, null);
        }

        [TestMethod]
        [Description("anonymous, sync : Make a public valid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetPublicBlobCloudAnonSync()
        {
            await cloudAnonSync.GetBlobScenarioTest(cloudSetup.PublicContainerName, cloudSetup.PublicBlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, null);
        }

        [TestMethod]
        [Description("anonymous, isAsync : Make a public valid Get Blob request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetPublicBlobCloudAnonAsync()
        {
            await cloudAnonAsync.GetBlobScenarioTest(cloudSetup.PublicContainerName, cloudSetup.PublicBlobName, cloudSetup.Properties,
                cloudSetup.LeaseId, cloudSetup.Content, null);
        }

        [TestMethod]
        [Description("owner, sync, range : Make valid Get Blob range requests and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobRangeCloudOwnerSync()
        {
            int all = cloudSetup.Content.Length;
            int quarter = cloudSetup.Content.Length / 4;
            int half = cloudSetup.Content.Length / 2;

            // Full content, as complete range. (0-end)
            await cloudOwnerSync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, 0, all, null);

            // Partial content, as complete range. (quarter-quarterPlusHalf)
            await cloudOwnerSync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, quarter, half, null);

            // Full content, as open range. (0-)
            await cloudOwnerSync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, 0, null, null);

            // Partial content, as open range. (half-)
            await cloudOwnerSync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, half, null, null);
        }

        [TestMethod]
        [Description("owner, sync, range : Make a Get Blob range request with an invalid range")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobRangeCloudOwnerSyncInvalidRange()
        {
            int all = cloudSetup.Content.Length;

            // Invalid range starting after the end of the blob (endPlusOne-)
            await cloudOwnerSync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, all, null, HttpStatusCode.RequestedRangeNotSatisfiable);
        }

        [TestMethod]
        [Description("owner, isAsync, range : Make valid Get Blob range requests and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetBlobRangeCloudOwnerAsync()
        {
            int all = cloudSetup.Content.Length;
            int quarter = cloudSetup.Content.Length / 4;
            int half = cloudSetup.Content.Length / 2;

            // Full content, as complete range. (0-end)
            await cloudOwnerAsync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, 0, all, null);

            // Partial content, as complete range. (quarter-quarterPlusHalf)
            await cloudOwnerAsync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, quarter, half, null);

            // Full content, as open range. (0-)
            await cloudOwnerAsync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, 0, null, null);

            // Partial content, as open range. (half-)
            await cloudOwnerAsync.GetBlobRangeScenarioTest(cloudSetup.ContainerName, cloudSetup.BlobName, cloudSetup.LeaseId, cloudSetup.Content, half, null, null);
        }
        #endregion

        #region ListBlobs
        [TestMethod]
        [Description("anonymous, sync : Make a valid List Blobs request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task BlobProtocolListBlobsCloudAnonSync()
        {
            BlobListingContext listingContext = new BlobListingContext("p", null, null, BlobListingDetails.All);
            await cloudAnonSync.ListBlobsScenarioTest(cloudSetup.PublicContainerName, listingContext, null, cloudSetup.PublicBlobName);

            await cloudSetup.CreateBlob(cloudSetup.PublicContainerName, "newblob1", true);
            await cloudSetup.CreateBlob(cloudSetup.PublicContainerName, "newblob2", true);

            try
            {
                await cloudAnonSync.ListBlobsScenarioTest(cloudSetup.PublicContainerName, listingContext, null, cloudSetup.PublicBlobName);

                // snapshots cannot be listed along with delimiter
                listingContext = new BlobListingContext("n", 10, "/", BlobListingDetails.Metadata);
                await cloudAnonSync.ListBlobsScenarioTest(cloudSetup.PublicContainerName, listingContext, null, "newblob1", "newblob2");
            }
            finally
            {
                await cloudSetup.DeleteBlob(cloudSetup.PublicContainerName, "newblob1");
                await cloudSetup.DeleteBlob(cloudSetup.PublicContainerName, "newblob2");
            }
        }

        [TestMethod]
        [Description("owner, sync : Make a valid List Blobs request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task BlobProtocolListBlobsCloudOwnerSync()
        {
            BlobListingContext listingContext = new BlobListingContext("def", null, null, BlobListingDetails.All);
            await cloudOwnerSync.ListBlobsScenarioTest(cloudSetup.ContainerName, listingContext, null, cloudSetup.BlobName);

            await cloudSetup.CreateBlob(cloudSetup.ContainerName, "newblob1", false);
            await cloudSetup.CreateBlob(cloudSetup.ContainerName, "newblob2", false);

            try
            {
                await cloudOwnerSync.ListBlobsScenarioTest(cloudSetup.ContainerName, listingContext, null, cloudSetup.BlobName);
                listingContext = new BlobListingContext("n", 10, "/", BlobListingDetails.Metadata);
                await cloudOwnerSync.ListBlobsScenarioTest(cloudSetup.ContainerName, listingContext, null, "newblob1", "newblob2");
            }
            finally
            {
                await cloudSetup.DeleteBlob(cloudSetup.ContainerName, "newblob1");
                await cloudSetup.DeleteBlob(cloudSetup.ContainerName, "newblob2");
            }
        }

        [TestMethod]
        [Description("Ensure that the parameters passed into ListingContexts are validated correctly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobProtocolListingContextValidation()
        {
            BlobListingContext listingContext1 = new BlobListingContext("correct", 1, null, BlobListingDetails.All);
            try
            {
                BlobListingContext listingContext2 = new BlobListingContext("below min", 0, null, BlobListingDetails.All);
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException e)
            {
                Assert.IsTrue(e.ToString().Contains("System.ArgumentOutOfRangeException: The argument 'maxResults' is smaller than minimum of '1'"));
            }
            BlobListingContext listingContext3 = new BlobListingContext("not limited", 6000, null, BlobListingDetails.All);
        }

        #endregion

        #region ListContainers
        [TestMethod]
        [Description("cloud: Make a valid List Containers request and get the response")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolListContainersCloud()
        {
            ListingContext listingContext = new ListingContext("default", null);
            await cloudOwnerAsync.ListContainersScenarioTest(listingContext, null, cloudSetup.ContainerName);

            await cloudSetup.CreateContainer("newcontainer1", true);
            await cloudSetup.CreateContainer("newcontainer2", true);

            try
            {
                await cloudOwnerAsync.ListContainersScenarioTest(listingContext, null, cloudSetup.ContainerName);
                listingContext = new ListingContext("newcontainer", 10);
                await cloudOwnerAsync.ListContainersScenarioTest(listingContext, null, "newcontainer1", "newcontainer2");
            }
            finally
            {
                await cloudSetup.DeleteContainer("newcontainer1");
                await cloudSetup.DeleteContainer("newcontainer2");
            }
        }

        [TestMethod]
        [Description("Get a container with empty header excluded/included from signature and verify request failed/succeeded")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolGetContainerWithEmptyHeader()
        {
            ListingContext listingContext = new ListingContext("default", null);
            await cloudOwnerAsync.CreateContainer("emptyheadercontainer", true);

            HttpRequestMessage request = BlobTests.ListContainersRequest(cloudOwnerAsync.BlobContext, listingContext);
            Assert.IsTrue(request != null, "Failed to create HttpRequestMessage");
            if (cloudOwnerAsync.BlobContext.Credentials != null)
            {
                request.Headers.Add("x-ms-blob-application-metadata", "");
            }
            using (HttpResponseMessage response = await BlobTestUtils.GetResponse(request, cloudOwnerAsync.BlobContext))
            {
                BlobTests.ListContainersResponse(response, cloudOwnerAsync.BlobContext, HttpStatusCode.OK/*HttpStatusCode.Forbidden*/);
            }

            request = BlobTests.ListContainersRequest(cloudOwnerAsync.BlobContext, listingContext);
            Assert.IsTrue(request != null, "Failed to create HttpRequestMessage");
            if (cloudOwnerAsync.BlobContext.Credentials != null)
            {
                request.Headers.Add("x-ms-blob-application-metadata", "");
            }
            using (HttpResponseMessage response = await BlobTestUtils.GetResponse(request, cloudOwnerAsync.BlobContext))
            {
                BlobTests.ListContainersResponse(response, cloudOwnerAsync.BlobContext, HttpStatusCode.OK);
            }
        }
        #endregion

        #region PutBlock, DownloadBlockList, and PutBlockList
        [TestMethod]
        [Description("owner, isAsync : PutBlock, DownloadBlockList, and PutBlockList scenarios")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutGetBlockListCloudOwnerAsync()
        {
            string blockId1 = Convert.ToBase64String(new byte[] { 99, 100, 101 });
            string blockId2 = Convert.ToBase64String(new byte[] { 102, 103, 104 });

            // use a unique name since temp blocks from previous runs can exist
            string blobName = "blob1"  + DateTime.UtcNow.Ticks;
            BlobProperties blobProperties = new BlobProperties();
            List<PutBlockListItem> blocks = new List<PutBlockListItem>();
            PutBlockListItem block1 = new PutBlockListItem(blockId1, BlockSearchMode.Uncommitted);
            blocks.Add(block1);
            PutBlockListItem block2 = new PutBlockListItem(blockId2, BlockSearchMode.Uncommitted);
            blocks.Add(block2);
            try
            {
                await cloudOwnerAsync.PutBlockScenarioTest(cloudSetup.ContainerName, blobName, blockId1, cloudSetup.LeaseId, cloudSetup.Content, null);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.All, cloudSetup.LeaseId, null, blockId1);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Uncommitted, cloudSetup.LeaseId, null, blockId1);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Committed, cloudSetup.LeaseId, null);

                await cloudOwnerAsync.PutBlockScenarioTest(cloudSetup.ContainerName, blobName, blockId2, cloudSetup.LeaseId, cloudSetup.Content, null);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.All, cloudSetup.LeaseId, null, blockId1, blockId2);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Uncommitted, cloudSetup.LeaseId, null, blockId1, blockId2);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Committed, cloudSetup.LeaseId, null);

                await cloudOwnerAsync.PutBlockListScenarioTest(cloudSetup.ContainerName, blobName, blocks, blobProperties, cloudSetup.LeaseId, null);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.All, cloudSetup.LeaseId, null, blockId1, blockId2);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Uncommitted, cloudSetup.LeaseId, null);
                await cloudOwnerAsync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Committed, cloudSetup.LeaseId, null, blockId1, blockId2);
            }
            finally
            {
                await cloudOwnerAsync.DeleteBlob(cloudSetup.ContainerName, blobName);
            }
        }

        [TestMethod]
        [Description("owner, sync : PutBlock, DownloadBlockList, and PutBlockList scenarios")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobProtocolPutGetBlockListCloudOwnerSync()
        {
            string blockId1 = Convert.ToBase64String(new byte[] { 99, 100, 101 });
            string blockId2 = Convert.ToBase64String(new byte[] { 102, 103, 104 });

            // use a unique name since temp blocks from previous runs can exist
            string blobName = "blob2" + DateTime.UtcNow.Ticks;
            BlobProperties blobProperties = new BlobProperties();
            List<PutBlockListItem> blocks = new List<PutBlockListItem>();
            PutBlockListItem block1 = new PutBlockListItem(blockId1, BlockSearchMode.Uncommitted);
            blocks.Add(block1);
            PutBlockListItem block2 = new PutBlockListItem(blockId2, BlockSearchMode.Uncommitted);
            blocks.Add(block2);
            try
            {
                await cloudOwnerSync.PutBlockScenarioTest(cloudSetup.ContainerName, blobName, blockId1, cloudSetup.LeaseId, cloudSetup.Content, null);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.All, cloudSetup.LeaseId, null, blockId1);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Uncommitted, cloudSetup.LeaseId, null, blockId1);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Committed, cloudSetup.LeaseId, null);

                await cloudOwnerSync.PutBlockScenarioTest(cloudSetup.ContainerName, blobName, blockId2, cloudSetup.LeaseId, cloudSetup.Content, null);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.All, cloudSetup.LeaseId, null, blockId1, blockId2);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Uncommitted, cloudSetup.LeaseId, null, blockId1, blockId2);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Committed, cloudSetup.LeaseId, null);

                await cloudOwnerSync.PutBlockListScenarioTest(cloudSetup.ContainerName, blobName, blocks, blobProperties, cloudSetup.LeaseId, null);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.All, cloudSetup.LeaseId, null, blockId1, blockId2);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Uncommitted, cloudSetup.LeaseId, null);
                await cloudOwnerSync.GetBlockListScenarioTest(cloudSetup.ContainerName, blobName, BlockListingFilter.Committed, cloudSetup.LeaseId, null, blockId1, blockId2);
            }
            finally
            {
                await cloudOwnerSync.DeleteBlob(cloudSetup.ContainerName, blobName);
            }
        }
        #endregion
    }
}
