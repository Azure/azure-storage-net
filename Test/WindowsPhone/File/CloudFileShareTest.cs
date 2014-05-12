// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileShareTest.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class CloudFileShareTest : FileTestBase
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
        [Description("Validate share references")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareReference()
        {
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare share = client.GetShareReference("share");
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
            CloudFileDirectory directory = rootDirectory.GetDirectoryReference("directory4");
            CloudFile file = directory.GetFileReference("file2");

            Assert.AreEqual(share, file.Share);
            Assert.AreEqual(share, rootDirectory.Share);
            Assert.AreEqual(share, directory.Share);
            Assert.AreEqual(share, directory.Parent.Share);
            Assert.AreEqual(share, file.Parent.Share);
        }

        [TestMethod]
        [Description("Create and delete a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                OperationContext operationContext = new OperationContext();
                Assert.ThrowsException<AggregateException>(
                    () => share.CreateAsync(null, operationContext).Wait(),
                    "Creating already exists share should fail");
                Assert.AreEqual((int)HttpStatusCode.Conflict, operationContext.LastResult.HttpStatusCode);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to create a share after it is created")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateIfNotExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                Assert.IsTrue(await share.CreateIfNotExistsAsync());
                Assert.IsFalse(await share.CreateIfNotExistsAsync());
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareDeleteIfExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            Assert.IsFalse(await share.DeleteIfExistsAsync());
            await share.CreateAsync();
            Assert.IsTrue(await share.DeleteIfExistsAsync());
            Assert.IsFalse(await share.DeleteIfExistsAsync());
        }

        [TestMethod]
        [Description("Check a share's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);

            try
            {
                Assert.IsFalse(await share2.ExistsAsync());
                
                await share.CreateAsync();
                
                Assert.IsTrue(await share2.ExistsAsync());
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }

            Assert.IsFalse(await share2.ExistsAsync());
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateWithMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("key1", "value1");
                await share.CreateAsync();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                await share2.FetchAttributesAsync();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                Assert.IsTrue(share2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareSetMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                await share2.FetchAttributesAsync();
                Assert.AreEqual(0, share2.Metadata.Count);

                share.Metadata.Add("key1", "value1");
                await share.SetMetadataAsync();

                await share2.FetchAttributesAsync();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                ShareResultSegment results = await share.ServiceClient.ListSharesSegmentedAsync(share.Name, ShareListingDetails.Metadata, null, null, null, null);
                CloudFileShare share3 = results.Results.First();
                Assert.AreEqual(1, share3.Metadata.Count);
                Assert.AreEqual("value1", share3.Metadata["key1"]);

                share.Metadata.Clear();
                await share.SetMetadataAsync();

                await share2.FetchAttributesAsync();
                Assert.AreEqual(0, share2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareRegionalSetMetadataAsync()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sk-SK");

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("sequence", "value");
                share.Metadata.Add("schema", "value");
                await share.CreateAsync();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareListFilesAndDirectoriesAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                List<string> fileNames = await CreateFilesAsync(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                IEnumerable<IListFileItem> results = await ListFilesAndDirectoriesAsync(rootDirectory, null, null, null);
                Assert.AreEqual(fileNames.Count, results.Count());
                foreach (IListFileItem fileItem in results)
                {
                    Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                    Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareListFilesAndDirectoriesSegmentedAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                List<string> fileNames = await CreateFilesAsync(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = await rootDirectory.ListFilesAndDirectoriesSegmentedAsync(1, token, null, null);
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    Assert.AreEqual(1, count);
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        /*
        [TestMethod]
        [Description("Test conditional access on a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                await share.FetchAttributesAsync();

                string currentETag = share.Properties.ETag;
                DateTimeOffset currentModifiedTime = share.Properties.LastModified.Value;

                // ETag conditional tests
                share.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await share.SetMetadataAsync();

                await share.FetchAttributesAsync();
                string newETag = share.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                // LastModifiedTime tests
                currentModifiedTime = share.Properties.LastModified.Value;

                share.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                share.Metadata["DateConditionalName"] = "DateConditionalValue2";
                currentETag = share.Properties.ETag;

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                await share.FetchAttributesAsync();
                newETag = share.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
        */
    }
}
