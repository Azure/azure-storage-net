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
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Net;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File.Protocol
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
            cloudSetup.Initialize();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            cloudSetup.Cleanup();

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
        public void FileProtocolPutFileCloudOwnerSync()
        {
            FileProperties properties = new FileProperties();
            cloudOwnerSync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(), properties, new byte[0], HttpStatusCode.Created);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolPutFileCloudAnonSync()
        {
            FileProperties properties = new FileProperties();
            cloudAnonSync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(),
                properties, new byte[0], HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, async : Make a valid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolPutFileCloudOwnerAsync()
        {
            FileProperties properties = new FileProperties();
            cloudOwnerAsync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(), properties, new byte[0], HttpStatusCode.Created);
        }

        [TestMethod]
        [Description("anonymous, async : Make an invalid Put Index File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolPutFileCloudAnonAsync()
        {
            FileProperties properties = new FileProperties();
            cloudAnonAsync.PutFileScenarioTest(cloudSetup.ShareName, Guid.NewGuid().ToString(),
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
        public void FileProtocolGetFileCloudOwnerSync()
        {
            cloudOwnerSync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties, null);
        }

        [TestMethod]
        [Description("owner, async : Make a valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetFileCloudOwnerAsync()
        {
            cloudOwnerAsync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties,
                 null);
        }

        [TestMethod]
        [Description("anonymous, sync : Make an invalid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetFileCloudAnonSync()
        {
            cloudAnonSync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties,
                 HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("anonymous, async : Make an invalid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetFileCloudAnonAsync()
        {
            cloudAnonAsync.GetFileScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Properties,
                 HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("owner, sync : Make a public valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetPublicFileCloudOwnerSync()
        {
            cloudOwnerSync.GetFileScenarioTest(cloudSetup.PublicShareName, cloudSetup.PublicFileName, cloudSetup.Properties,
                 null);
        }

        [TestMethod]
        [Description("owner, async : Make a public valid Get File request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetPublicFileCloudOwnerAsync()
        {
            cloudOwnerAsync.GetFileScenarioTest(cloudSetup.PublicShareName, cloudSetup.PublicFileName, cloudSetup.Properties,
                 null);
        }

        [TestMethod]
        [Description("owner, sync, range : Make valid Get File range requests and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetFileRangeCloudOwnerSync()
        {
            int all = cloudSetup.Content.Length;
            int quarter = cloudSetup.Content.Length / 4;
            int half = cloudSetup.Content.Length / 2;

            cloudOwnerSync.WriteRange(cloudSetup.FileName, cloudSetup.ShareName, cloudSetup.Content, HttpStatusCode.Created);
            // Full content, as complete range. (0-end)
            cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, all, null);

            // Partial content, as complete range. (quarter-quarterPlusHalf)
            cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, quarter, half, null);

            // Full content, as open range. (0-)
            cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, null, null);

            // Partial content, as open range. (half-)
            cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, half, null, null);
        }

        [TestMethod]
        [Description("owner, sync, range : Make a Get File range request with an invalid range")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetFileRangeCloudOwnerSyncInvalidRange()
        {
            int all = cloudSetup.Content.Length;

            cloudOwnerSync.WriteRange(cloudSetup.FileName, cloudSetup.ShareName, cloudSetup.Content, HttpStatusCode.Created);
            // Invalid range starting after the end of the file (endPlusOne-)
            cloudOwnerSync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, all, null, HttpStatusCode.RequestedRangeNotSatisfiable);
        }

        [TestMethod]
        [Description("owner, async, range : Make valid Get File range requests and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetFileRangeCloudOwnerAsync()
        {
            int all = cloudSetup.Content.Length;
            int quarter = cloudSetup.Content.Length / 4;
            int half = cloudSetup.Content.Length / 2;

            cloudOwnerAsync.WriteRange(cloudSetup.FileName, cloudSetup.ShareName, cloudSetup.Content, HttpStatusCode.Created);

            // Full content, as complete range. (0-end)
            cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, all, null);

            // Partial content, as complete range. (quarter-quarterPlusHalf)
            cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, quarter, half, null);

            // Full content, as open range. (0-)
            cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, 0, null, null);

            // Partial content, as open range. (half-)
            cloudOwnerAsync.GetFileRangeScenarioTest(cloudSetup.ShareName, cloudSetup.FileName, cloudSetup.Content, half, null, null);
        }
        #endregion

        #region ListShares
        [TestMethod]
        [Description("cloud: Make a valid List Shares request and get the response")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolListSharesCloud()
        {
            ListingContext listingContext = new ListingContext("default", null);
            cloudOwnerAsync.ListSharesScenarioTest(listingContext, null, cloudSetup.ShareName);

            cloudSetup.CreateShare("newshare1");
            cloudSetup.CreateShare("newshare2");

            try
            {
                cloudOwnerAsync.ListSharesScenarioTest(listingContext, null, cloudSetup.ShareName);
                listingContext = new ListingContext("n", 10);
                cloudOwnerAsync.ListSharesScenarioTest(listingContext, null, "newshare1", "newshare2");
            }
            finally
            {
                cloudSetup.DeleteShare("newshare1");
                cloudSetup.DeleteShare("newshare2");
            }
        }

        [Ignore]
        [TestMethod]
        [Description("Get a share with empty header excluded/included from signature and verify request succeeded")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileProtocolGetShareWithEmptyHeader()
        {
            ListingContext listingContext = new ListingContext("default", null);
            cloudOwnerAsync.CreateShare("emptyheadershare1");

            HttpWebRequest request = FileTests.ListSharesRequest(cloudOwnerAsync.FileContext, listingContext);
            Assert.IsTrue(request != null, "Failed to create HttpWebRequest");
            if (cloudOwnerAsync.FileContext.Credentials != null)
            {
                FileTests.SignRequest(request, cloudOwnerAsync.FileContext);
                request.Headers.Add("x-ms-file-application-metadata", "");
            }
            using (HttpWebResponse response = FileTestUtils.GetResponse(request, cloudOwnerAsync.FileContext))
            {
                FileTests.ListSharesResponse(response, cloudOwnerAsync.FileContext, null);
            }

            request = FileTests.ListSharesRequest(cloudOwnerAsync.FileContext, listingContext);
            Assert.IsTrue(request != null, "Failed to create HttpWebRequest");
            if (cloudOwnerAsync.FileContext.Credentials != null)
            {
                request.Headers.Add("x-ms-file-application-metadata", "");
                FileTests.SignRequest(request, cloudOwnerAsync.FileContext);
            }
            using (HttpWebResponse response = FileTestUtils.GetResponse(request, cloudOwnerAsync.FileContext))
            {
                FileTests.ListSharesResponse(response, cloudOwnerAsync.FileContext, HttpStatusCode.OK);
            }
        }
        #endregion
    }
}
