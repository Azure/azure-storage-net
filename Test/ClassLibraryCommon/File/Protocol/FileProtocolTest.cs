// -----------------------------------------------------------------------------------------
// <copyright file="FileProtocolTest.cs" company="Microsoft">
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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.File.Protocol
{
    [TestClass]
    public class FileProtocolTest : TestBase
    {
        private static Random random = new Random();

        private static FileClientTests cloudOwnerSync = new FileClientTests(true, false, 30);
        private static FileClientTests cloudAnonSync = new FileClientTests(false, false, 30);
        private static FileClientTests cloudOwnerAsync = new FileClientTests(true, false, 30);
        private static FileClientTests cloudAnonAsync = new FileClientTests(false, false, 30);

        private static FileClientTests cloudSetup = new FileClientTests(true, false, 30);

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

            // sleep for 40s so that if the test is re-run, we can recreate the share
            Thread.Sleep(35000);
        }

        #region PutFile
        [TestMethod]
        [Description("owner, sync : Make a valid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolPutFileCloudOwnerSync()
        {
            FileProperties properties = new FileProperties();
            await cloudOwnerSync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(), properties, new byte[0], HttpStatusCode.Created);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolPutFileCloudAnonSync()
        {
            FileProperties properties = new FileProperties();
            await cloudAnonSync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(),
                properties, new byte[0], HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, async : Make a valid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolPutFileCloudOwnerAsync()
        {
            FileProperties properties = new FileProperties();
            await cloudOwnerAsync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(), properties, new byte[0], HttpStatusCode.Created);
        }

        [TestMethod]
        [Description("anonymous, async : Make an invalid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolPutFileCloudAnonAsync()
        {
            FileProperties properties = new FileProperties();
            await cloudAnonAsync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(),
                properties, new byte[0], HttpStatusCode.NotFound);
        }
        #endregion

        #region File
        [TestMethod]
        [Description("owner, sync : Make a valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetFileCloudOwnerSync()
        {
            await cloudOwnerSync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties, null);
        }

        [TestMethod]
        [Description("owner, async : Make a valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetFileCloudOwnerAsync()
        {
            await cloudOwnerAsync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties,
                 null);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolGetFileCloudAnonSync()
        {
            await cloudAnonSync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties,
                 HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("anonymous, async : Make an invalid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolGetFileCloudAnonAsync()
        {
            await cloudAnonAsync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties,
                 HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, sync : Make a public valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetPublicFileCloudOwnerSync()
        {
            await cloudOwnerSync.GetFileScenarioTest(cloudSetup.PublicShareName, cloudSetup.PublicFileName, cloudSetup.Properties,
                 null);
        }

        [TestMethod]
        [Description("owner, async : Make a public valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetPublicFileCloudOwnerAsync()
        {
            await cloudOwnerAsync.GetFileScenarioTest(cloudSetup.PublicShareName, cloudSetup.PublicFileName, cloudSetup.Properties,
                 null);
        }

        [TestMethod]
        [Description("owner, sync, range : Make valid Get File range requests and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetFileRangeCloudOwnerSync()
        {
            int all = cloudSetup.Content.Length;
            int quarter = cloudSetup.Content.Length / 4;
            int half = cloudSetup.Content.Length / 2;

            await cloudOwnerSync.WriteRange(cloudSetup.FileName, cloudSetup.ShareName, cloudSetup.Content, HttpStatusCode.Created);
            // Full content, as complete range. (0-end)
            await cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, all, null);

            // Partial content, as complete range. (quarter-quarterPlusHalf)
            await cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, quarter, half, null);

            // Full content, as open range. (0-)
            await cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, null, null);

            // Partial content, as open range. (half-)
            await cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, half, null, null);
        }

        [TestMethod]
        [Description("owner, sync, range : Make a Get File range request with an invalid range")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetFileRangeCloudOwnerSyncInvalidRange()
        {
            int all = cloudSetup.Content.Length;

            await cloudOwnerSync.WriteRange(cloudSetup.FileName, cloudSetup.ShareName, cloudSetup.Content, HttpStatusCode.Created);
            // Invalid range starting after the end of the file (endPlusOne-)
            await cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, all, null, HttpStatusCode.RequestedRangeNotSatisfiable);
        }

        [TestMethod]
        [Description("owner, async, range : Make valid Get File range requests and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolGetFileRangeCloudOwnerAsync()
        {
            int all = cloudSetup.Content.Length;
            int quarter = cloudSetup.Content.Length / 4;
            int half = cloudSetup.Content.Length / 2;

            await cloudOwnerAsync.WriteRange(cloudSetup.FileName, cloudSetup.ShareName, cloudSetup.Content, HttpStatusCode.Created);

            // Full content, as complete range. (0-end)
            await cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, all, null);

            // Partial content, as complete range. (quarter-quarterPlusHalf)
            await cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, quarter, half, null);

            // Full content, as open range. (0-)
            await cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, null, null);

            // Partial content, as open range. (half-)
            await cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, half, null, null);
        }
        #endregion

        #region ListShares
        [TestMethod]
        [Description("cloud: Make a valid List Shares request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public async Task FileProtocolListSharesCloud()
        {
            ListingContext listingContext = new ListingContext("default", null);
            await cloudOwnerAsync.ListSharesScenarioTest(listingContext, null, cloudSetup.ShareName);

            string prefix = Guid.NewGuid().ToString();
            await cloudSetup.CreateShare(prefix + "newshare1");
            await cloudSetup.CreateShare(prefix + "newshare2");

            try
            {
                await cloudOwnerAsync.ListSharesScenarioTest(listingContext, null, cloudSetup.ShareName);
                listingContext = new ListingContext(prefix, 10);
                await cloudOwnerAsync.ListSharesScenarioTest(listingContext, null, prefix + "newshare1", prefix + "newshare2");
            }
            finally
            {
                await cloudSetup.DeleteShare(prefix + "newshare1");
                await cloudSetup.DeleteShare(prefix + "newshare2");
            }
        }

        [TestMethod]
        [Description("Get a share with empty header excluded/included from signature and verify request succeeded")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileProtocolGetShareWithEmptyHeader()
        {
            ListingContext listingContext = new ListingContext("default", null);
            await cloudOwnerAsync.CreateShare("emptyheadershare1");

            HttpRequestMessage request = FileTests.ListSharesRequest(cloudOwnerAsync.FileContext, listingContext);
            Assert.IsTrue(request != null, "Failed to create HttpRequestMessage");
            if (cloudOwnerAsync.FileContext.Credentials != null)
            {
                request.Headers.Add("x-ms-file-application-metadata", "");
            }
            using (HttpResponseMessage response = await FileTestUtils.GetResponse(request, cloudOwnerAsync.FileContext))
            {
                FileTests.ListSharesResponse(response, cloudOwnerAsync.FileContext, null);
            }

            request = FileTests.ListSharesRequest(cloudOwnerAsync.FileContext, listingContext);
            Assert.IsTrue(request != null, "Failed to create HttpRequestMessage");
            if (cloudOwnerAsync.FileContext.Credentials != null)
            {
                request.Headers.Add("x-ms-file-application-metadata", "");
            }
            using (HttpResponseMessage response = await FileTestUtils.GetResponse(request, cloudOwnerAsync.FileContext))
            {
                FileTests.ListSharesResponse(response, cloudOwnerAsync.FileContext, HttpStatusCode.OK);
            }
        }
        #endregion
    }
}
