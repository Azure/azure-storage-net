// -----------------------------------------------------------------------------------------
// <copyright file="SecondaryTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    [TestClass]
    public class SecondaryTests : TestBase
    {
        [TestMethod]
        [Description("LocationMode should be limited to PrimaryOnly when StorageUri does not have a secondary Uri")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void LocationModeWithMissingUri()
        {
            AssertSecondaryEndpoint();

            CloudBlobClient client = GenerateCloudBlobClient();
            CloudBlobClient primaryOnlyClient = new CloudBlobClient(client.BaseUri, client.Credentials);
            CloudBlobContainer container = primaryOnlyClient.GetContainerReference("nonexistingcontainer");

            BlobRequestOptions options = new BlobRequestOptions()
            {
                LocationMode = LocationMode.SecondaryOnly,
                RetryPolicy = new NoRetry(),
            };

            StorageException e = TestHelper.ExpectedException<StorageException>(
                () => container.FetchAttributes(null, options, null),
                "Request should fail when an URI is not provided for the target location");
            Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));

            options.LocationMode = LocationMode.SecondaryThenPrimary;
            e = TestHelper.ExpectedException<StorageException>(
                () => container.FetchAttributes(null, options, null),
                "Request should fail when an URI is not provided for the target location");
            Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));

            options.LocationMode = LocationMode.PrimaryThenSecondary;
            e = TestHelper.ExpectedException<StorageException>(
                () => container.FetchAttributes(null, options, null),
                "Request should fail when an URI is not provided for the target location");
            Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
        }

        [TestMethod]
        [Description("Blob If*Exists request should not be sent to secondary location")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobIfExistsShouldNotHitSecondary()
        {
            AssertSecondaryEndpoint();

            BlobRequestOptions options = new BlobRequestOptions();

            CloudBlobContainer container = BlobTestBase.GetRandomContainerReference();
            TestPrimaryOnlyCommand((opt, ctx) => container.CreateIfNotExists(opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => container.EndCreateIfNotExists(container.BeginCreateIfNotExists(opt, ctx, null, null)), options);
            TestPrimaryOnlyCommand((opt, ctx) => container.DeleteIfExists(null, opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => container.EndDeleteIfExists(container.BeginDeleteIfExists(null, opt, ctx, null, null)), options);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
            TestPrimaryOnlyCommand((opt, ctx) => blockBlob.DeleteIfExists(DeleteSnapshotsOption.None, null, opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => blockBlob.EndDeleteIfExists(blockBlob.BeginDeleteIfExists(DeleteSnapshotsOption.None, null, opt, ctx, null, null)), options);

            CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
            TestPrimaryOnlyCommand((opt, ctx) => pageBlob.DeleteIfExists(DeleteSnapshotsOption.None, null, opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => pageBlob.EndDeleteIfExists(pageBlob.BeginDeleteIfExists(DeleteSnapshotsOption.None, null, opt, ctx, null, null)), options);
        }

        [TestMethod]
        [Description("Queue If*Exists request should not be sent to secondary location")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void QueueIfExistsShouldNotHitSecondary()
        {
            AssertSecondaryEndpoint();

            QueueRequestOptions options = new QueueRequestOptions();

            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(QueueTestBase.GenerateNewQueueName());
            TestPrimaryOnlyCommand((opt, ctx) => queue.CreateIfNotExists(opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => queue.EndCreateIfNotExists(queue.BeginCreateIfNotExists(opt, ctx, null, null)), options);
            TestPrimaryOnlyCommand((opt, ctx) => queue.DeleteIfExists(opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => queue.EndDeleteIfExists(queue.BeginDeleteIfExists(opt, ctx, null, null)), options);
        }

        [TestMethod]
        [Description("Table If*Exists request should not be sent to secondary location")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableIfExistsShouldNotHitSecondary()
        {
            AssertSecondaryEndpoint();

            TableRequestOptions options = new TableRequestOptions();

            CloudTableClient client = GenerateCloudTableClient();
            CloudTable table = client.GetTableReference(TableTestBase.GenerateRandomTableName());
            TestPrimaryOnlyCommand((opt, ctx) => table.CreateIfNotExists(opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => table.EndCreateIfNotExists(table.BeginCreateIfNotExists(opt, ctx, null, null)), options);
            TestPrimaryOnlyCommand((opt, ctx) => table.DeleteIfExists(opt, ctx), options);
            TestPrimaryOnlyCommand((opt, ctx) => table.EndDeleteIfExists(table.BeginDeleteIfExists(opt, ctx, null, null)), options);
        }

        private void TestPrimaryOnlyCommand<T>(Action<T, OperationContext> command, T options)
            where T: IRequestOptions
        {
            OperationContext context = new OperationContext();
            options.LocationMode = LocationMode.PrimaryOnly;
            command(options, context);
            Assert.AreEqual(StorageLocation.Primary, context.RequestResults[0].TargetLocation);

            context = new OperationContext();
            options.LocationMode = LocationMode.PrimaryThenSecondary;
            command(options, context);
            Assert.AreEqual(StorageLocation.Primary, context.RequestResults[0].TargetLocation);

            context = new OperationContext();
            options.LocationMode = LocationMode.SecondaryThenPrimary;
            command(options, context);
            Assert.AreEqual(StorageLocation.Primary, context.RequestResults[0].TargetLocation);

            context = new OperationContext();
            options.LocationMode = LocationMode.SecondaryOnly;
            StorageException e = TestHelper.ExpectedException<StorageException>(
                () => command(options, context),
                "SecondaryOnly should fail a primary only command");
            Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
        }

        [TestMethod]
        [Description("Blob requests should be sent to the correct location")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobMultiLocationRetries()
        {
            MultiLocationRetries(SecondaryTests.TestContainerFetchAttributes);
        }

        [TestMethod]
        [Description("Queue requests should be sent to the correct location")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void QueueMultiLocationRetries()
        {
            MultiLocationRetries(SecondaryTests.TestQueueFetchAttributes);
        }

        [TestMethod]
        [Description("Table requests should be sent to the correct location")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableMultiLocationRetries()
        {
            MultiLocationRetries(SecondaryTests.TestTableRetrieve);
        }

        private static void MultiLocationRetries(Action<LocationMode?, LocationMode, StorageLocation, IList<RetryContext>, IList<RetryInfo>> testMethod)
        {
            AssertSecondaryEndpoint();

            List<RetryInfo> retryInfoList = new List<RetryInfo>();
            List<RetryContext> retryContextList = new List<RetryContext>();

            retryContextList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryOnly));
            testMethod(LocationMode.PrimaryOnly, LocationMode.PrimaryOnly, StorageLocation.Primary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.PrimaryOnly, StorageLocation.Primary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.SecondaryOnly));
            testMethod(LocationMode.SecondaryOnly, LocationMode.PrimaryOnly, StorageLocation.Secondary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.SecondaryOnly, StorageLocation.Secondary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.PrimaryThenSecondary));
            testMethod(LocationMode.PrimaryThenSecondary, LocationMode.PrimaryOnly, StorageLocation.Primary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.PrimaryThenSecondary, StorageLocation.Primary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.SecondaryThenPrimary));
            testMethod(LocationMode.SecondaryThenPrimary, LocationMode.PrimaryOnly, StorageLocation.Secondary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.SecondaryThenPrimary, StorageLocation.Secondary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryInfoList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryOnly));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(6) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryOnly));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryOnly));
            AddUpdatedLocationModes(retryContextList, retryInfoList);
            testMethod(LocationMode.PrimaryOnly, LocationMode.PrimaryOnly, StorageLocation.Primary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.PrimaryOnly, StorageLocation.Primary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryInfoList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.SecondaryOnly));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(6) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.SecondaryOnly));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.SecondaryOnly));
            AddUpdatedLocationModes(retryContextList, retryInfoList);
            testMethod(LocationMode.SecondaryOnly, LocationMode.PrimaryOnly, StorageLocation.Secondary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.SecondaryOnly, StorageLocation.Secondary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryInfoList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.PrimaryThenSecondary));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(6) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryThenSecondary));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.PrimaryThenSecondary));
            AddUpdatedLocationModes(retryContextList, retryInfoList);
            testMethod(LocationMode.PrimaryThenSecondary, LocationMode.PrimaryOnly, StorageLocation.Primary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.PrimaryThenSecondary, StorageLocation.Primary, retryContextList, retryInfoList);

            retryContextList.Clear();
            retryInfoList.Clear();
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.SecondaryThenPrimary));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(6) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.SecondaryThenPrimary));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.SecondaryThenPrimary));
            AddUpdatedLocationModes(retryContextList, retryInfoList);
            testMethod(LocationMode.SecondaryThenPrimary, LocationMode.PrimaryOnly, StorageLocation.Secondary, retryContextList, retryInfoList);
            testMethod(null, LocationMode.SecondaryThenPrimary, StorageLocation.Secondary, retryContextList, retryInfoList);
        }

        private static void AddUpdatedLocationModes(IList<RetryContext> retryContextList, IList<RetryInfo> retryInfoList)
        {
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(4) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Secondary, LocationMode.SecondaryOnly));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryOnly));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.PrimaryThenSecondary));
            retryInfoList.Add(new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(1) });
            retryContextList.Add(new RetryContext(0, null, StorageLocation.Primary, LocationMode.SecondaryThenPrimary));
        }

        private static void TestContainerFetchAttributes(LocationMode? optionsLocationMode, LocationMode clientLocationMode, StorageLocation initialLocation, IList<RetryContext> retryContextList, IList<RetryInfo> retryInfoList)
        {
            CloudBlobContainer container = BlobTestBase.GetRandomContainerReference();
            using (MultiLocationTestHelper helper = new MultiLocationTestHelper(container.ServiceClient.StorageUri, initialLocation, retryContextList, retryInfoList))
            {
                container.ServiceClient.DefaultRequestOptions.LocationMode = clientLocationMode;
                BlobRequestOptions options = new BlobRequestOptions()
                {
                    LocationMode = optionsLocationMode,
                    RetryPolicy = helper.RetryPolicy,
                };

                TestHelper.ExpectedException(
                    () => container.FetchAttributes(null, options, helper.OperationContext),
                    "FetchAttributes on a non-existing container should fail",
                    HttpStatusCode.NotFound);
            }
        }

        private static void TestQueueFetchAttributes(LocationMode? optionsLocationMode, LocationMode clientLocationMode, StorageLocation initialLocation, IList<RetryContext> retryContextList, IList<RetryInfo> retryInfoList)
        {
            CloudQueue queue = GenerateCloudQueueClient().GetQueueReference(QueueTestBase.GenerateNewQueueName());
            using (MultiLocationTestHelper helper = new MultiLocationTestHelper(queue.ServiceClient.StorageUri, initialLocation, retryContextList, retryInfoList))
            {
                queue.ServiceClient.DefaultRequestOptions.LocationMode = clientLocationMode;
                QueueRequestOptions options = new QueueRequestOptions()
                {
                    LocationMode = optionsLocationMode,
                    RetryPolicy = helper.RetryPolicy,
                };

                TestHelper.ExpectedException(
                    () => queue.FetchAttributes(options, helper.OperationContext),
                    "FetchAttributes on a non-existing queue should fail",
                    HttpStatusCode.NotFound);
            }
        }

        private static void TestTableRetrieve(LocationMode? optionsLocationMode, LocationMode clientLocationMode, StorageLocation initialLocation, IList<RetryContext> retryContextList, IList<RetryInfo> retryInfoList)
        {
            CloudTable table = GenerateCloudTableClient().GetTableReference(TableTestBase.GenerateRandomTableName());
            using (MultiLocationTestHelper helper = new MultiLocationTestHelper(table.ServiceClient.StorageUri, initialLocation, retryContextList, retryInfoList))
            {
                table.ServiceClient.DefaultRequestOptions.LocationMode = clientLocationMode;
                TableRequestOptions options = new TableRequestOptions()
                {
                    LocationMode = optionsLocationMode,
                    RetryPolicy = helper.RetryPolicy,
                };

                TestHelper.ExpectedException(
                    () => table.GetPermissions(options, helper.OperationContext),
                    "GetPermissions on a non-existing table should fail",
                    HttpStatusCode.NotFound);
            }
        }

        private static void TestShareFetchAttributes(LocationMode? optionsLocationMode, LocationMode clientLocationMode, StorageLocation initialLocation, IList<RetryContext> retryContextList, IList<RetryInfo> retryInfoList)
        {
            CloudFileShare share = FileTestBase.GetRandomShareReference();
            using (MultiLocationTestHelper helper = new MultiLocationTestHelper(share.ServiceClient.StorageUri, initialLocation, retryContextList, retryInfoList))
            {
                share.ServiceClient.DefaultRequestOptions.LocationMode = clientLocationMode;
                FileRequestOptions options = new FileRequestOptions()
                {
                    LocationMode = optionsLocationMode,
                    RetryPolicy = helper.RetryPolicy,
                };

                TestHelper.ExpectedException(
                    () => share.FetchAttributes(null, options, helper.OperationContext),
                    "FetchAttributes on a non-existing share should fail",
                    HttpStatusCode.NotFound);
            }
        }

        private class MultiLocationTestHelper : IDisposable
        {
            private StorageUri storageUri;
            private StorageLocation initialLocation;
            private IList<RetryInfo> retryInfoList;
            private IList<RetryContext> retryContextList;
            private int requestCounter;
            private string error;

            public OperationContext OperationContext { get; private set; }

            public IExtendedRetryPolicy RetryPolicy { get; private set; }

            public MultiLocationTestHelper(StorageUri storageUri, StorageLocation initialLocation, IList<RetryContext> retryContextList, IList<RetryInfo> retryInfoList)
            {
                this.storageUri = storageUri;
                this.initialLocation = initialLocation;
                this.retryContextList = retryContextList;
                this.retryInfoList = retryInfoList;

                this.OperationContext = new OperationContext();
                this.OperationContext.SendingRequest += this.SendingRequest;

                this.RetryPolicy = new AlwaysRetry(this.retryContextList, this.retryInfoList);
            }

            private void SendingRequest(object sender, RequestEventArgs e)
            {
                if (this.error == null)
                {
                    StorageLocation location = (this.requestCounter == 0) ? this.initialLocation : this.retryInfoList[this.requestCounter - 1].TargetLocation;
                    Uri uri = this.storageUri.GetUri(location);
                    if (!e.Request.RequestUri.AbsoluteUri.StartsWith(uri.AbsoluteUri))
                    {
                        this.error = string.Format("Request {0} was sent to {1} while the host should have been {2}", this.requestCounter, e.Request.RequestUri, uri);
                    }
                }

                this.requestCounter++;
            }

            public void Dispose()
            {
                Assert.IsNull(this.error, this.error);
                Assert.AreEqual(this.initialLocation, this.OperationContext.RequestResults[0].TargetLocation);
                Assert.AreEqual(this.retryInfoList.Count + 1, this.OperationContext.RequestResults.Count);
                for (int i = 0; i < this.retryInfoList.Count; i++)
                {
                    Assert.AreEqual(this.retryInfoList[i].TargetLocation, this.OperationContext.RequestResults[i + 1].TargetLocation);

                    TimeSpan retryInterval = this.OperationContext.RequestResults[i + 1].StartTime - this.OperationContext.RequestResults[i].EndTime;
                    string error = string.Format("{0} <= {1}", this.retryInfoList[i].RetryInterval, retryInterval);
                    Assert.IsTrue(this.retryInfoList[i].RetryInterval <= retryInterval, error);
                }
            }
        }
    }
}
