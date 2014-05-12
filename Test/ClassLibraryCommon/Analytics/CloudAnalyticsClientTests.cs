// -----------------------------------------------------------------------------------------
// <copyright file="CloudAnalyticsClientTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    [TestClass]
    public class CloudAnalyticsClientTests : AnalyticsTestBase
    {
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
        [Description("Get log directory references")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientGetLogDirectories()
        {
            CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, tableClient.Credentials);

            CloudBlobDirectory blobLogs = analyticsClient.GetLogDirectory(StorageService.Blob);
            CloudBlobDirectory queueLogs = analyticsClient.GetLogDirectory(StorageService.Queue);
            CloudBlobDirectory tableLogs = analyticsClient.GetLogDirectory(StorageService.Table);

            Assert.AreEqual("$logs/blob/", blobLogs.Container.Name + "/" + blobLogs.Prefix);
            Assert.AreEqual("$logs/queue/", queueLogs.Container.Name + "/" + queueLogs.Prefix);
            Assert.AreEqual("$logs/table/", tableLogs.Container.Name + "/" + tableLogs.Prefix);
        }

        [TestMethod]
        [Description("Get metrics table references")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientGetMetricsTables()
        {
            CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, tableClient.Credentials);

            CloudTable capacityTable = analyticsClient.GetCapacityTable();
            
            CloudTable blobHourPrimaryTable = analyticsClient.GetHourMetricsTable(StorageService.Blob);
            CloudTable blobMinutePrimaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.Blob);
            CloudTable blobHourSecondaryTable = analyticsClient.GetHourMetricsTable(StorageService.Blob, StorageLocation.Secondary);
            CloudTable blobMinuteSecondaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.Blob, StorageLocation.Secondary);
            
            CloudTable queueHourPrimaryTable = analyticsClient.GetHourMetricsTable(StorageService.Queue);
            CloudTable queueMinutePrimaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.Queue);
            CloudTable queueHourSecondaryTable = analyticsClient.GetHourMetricsTable(StorageService.Queue, StorageLocation.Secondary);
            CloudTable queueMinuteSecondaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.Queue, StorageLocation.Secondary);
            
            CloudTable tableHourPrimaryTable = analyticsClient.GetHourMetricsTable(StorageService.Table);
            CloudTable tableMinutePrimaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.Table);
            CloudTable tableHourSecondaryTable = analyticsClient.GetHourMetricsTable(StorageService.Table, StorageLocation.Secondary);
            CloudTable tableMinuteSecondaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.Table, StorageLocation.Secondary);

            Assert.AreEqual("$MetricsCapacityBlob", capacityTable.Name);

            Assert.AreEqual("$MetricsHourPrimaryTransactionsBlob", blobHourPrimaryTable.Name);
            Assert.AreEqual("$MetricsMinutePrimaryTransactionsBlob", blobMinutePrimaryTable.Name);
            Assert.AreEqual("$MetricsHourSecondaryTransactionsBlob", blobHourSecondaryTable.Name);
            Assert.AreEqual("$MetricsMinuteSecondaryTransactionsBlob", blobMinuteSecondaryTable.Name);

            Assert.AreEqual("$MetricsHourPrimaryTransactionsQueue", queueHourPrimaryTable.Name);
            Assert.AreEqual("$MetricsMinutePrimaryTransactionsQueue", queueMinutePrimaryTable.Name);
            Assert.AreEqual("$MetricsHourSecondaryTransactionsQueue", queueHourSecondaryTable.Name);
            Assert.AreEqual("$MetricsMinuteSecondaryTransactionsQueue", queueMinuteSecondaryTable.Name);

            Assert.AreEqual("$MetricsHourPrimaryTransactionsTable", tableHourPrimaryTable.Name);
            Assert.AreEqual("$MetricsMinutePrimaryTransactionsTable", tableMinutePrimaryTable.Name);
            Assert.AreEqual("$MetricsHourSecondaryTransactionsTable", tableHourSecondaryTable.Name);
            Assert.AreEqual("$MetricsMinuteSecondaryTransactionsTable", tableMinuteSecondaryTable.Name);
        }

#if SYNC
        [TestMethod]
        [Description("List all logs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientListAllLogs()
        {
            CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, blobClient.Credentials);
            analyticsClient.LogContainer = CloudAnalyticsClientTests.GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(analyticsClient.LogContainer);

            try
            {
                container.CreateIfNotExists();
                List<string> logBlobNames = CreateLogs(container, StorageService.Blob, 13, DateTime.UtcNow.AddMonths(-13), "month"); // 13 months of logs, one per month

                IEnumerable<ICloudBlob> results = analyticsClient.ListLogs(StorageService.Blob);
                foreach (ICloudBlob result in results)
                {
                    result.Delete();
                    logBlobNames.Remove(result.Name);
                }

                Assert.AreEqual(0, logBlobNames.Count);
                Assert.AreEqual(0, analyticsClient.ListLogs(StorageService.Blob).Count());
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List logs with open ended time range")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientListLogsStartDate()
        {
            CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, blobClient.Credentials);
            analyticsClient.LogContainer = CloudAnalyticsClientTests.GetRandomContainerName();

            CloudBlobContainer container = blobClient.GetContainerReference(analyticsClient.LogContainer);

            try
            {
                DateTime time = DateTime.UtcNow;
                container.CreateIfNotExists();
                List<string> logBlobNames = CreateLogs(container, StorageService.Blob, 48, time.AddDays(-2), "hour"); // 2 days of logs, one per hour
                string expectedYesterdayPrefix = string.Concat("blob/", time.AddDays(-1).ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture));
                string expectedNowPrefix = string.Concat("blob/", DateTime.UtcNow.ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture));

                IEnumerable<ICloudBlob> results = analyticsClient.ListLogs(StorageService.Blob, time.AddDays(-1), null); // only want the last day's logs
                foreach (ICloudBlob result in results)
                { 
                    Assert.IsTrue(string.Compare(result.Parent.Prefix, expectedYesterdayPrefix) >= 0);
                    Assert.IsTrue(string.Compare(result.Parent.Prefix, expectedNowPrefix) <= 0);
                    Assert.IsTrue(logBlobNames.Remove(result.Name));
                    result.Delete();
                }

                Assert.AreEqual(24, logBlobNames.Count); // should have half of the logs remaining. 
                Assert.AreEqual(0, analyticsClient.ListLogs(StorageService.Blob, time.AddDays(-1), null).Count());
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List logs with well defined time range")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientListLogsStartEndDate()
        {
            CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, blobClient.Credentials);
            analyticsClient.LogContainer = CloudAnalyticsClientTests.GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(analyticsClient.LogContainer);

            try
            {
                DateTime time = DateTime.UtcNow;
                container.CreateIfNotExists();
                List<string> logBlobNames = CreateLogs(container, StorageService.Blob, 72, time.AddDays(-3), "hour"); // 3 days of logs, one per hour
                string expectedTwoDaysAgoPrefix = string.Concat("blob/", time.AddDays(-2).ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture));
                string expectedYesterdayPrefix = string.Concat("blob/", time.AddDays(-1).ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture));

                IEnumerable<ICloudBlob> results = analyticsClient.ListLogs(StorageService.Blob, time.AddDays(-2), time.AddDays(-1)); // only want the middle day's logs
                foreach (ICloudBlob result in results)
                {
                    Assert.IsTrue(string.Compare(result.Parent.Prefix, expectedTwoDaysAgoPrefix) >= 0);
                    Assert.IsTrue(string.Compare(result.Parent.Prefix, expectedYesterdayPrefix) <= 0);
                    Assert.IsTrue(logBlobNames.Remove(result.Name));
                    result.Delete();
                }

                Assert.AreEqual(48, logBlobNames.Count); // should have two thirds of the logs remaining. 
                Assert.AreEqual(0, analyticsClient.ListLogs(StorageService.Blob, time.AddDays(-2), time.AddDays(-1)).Count());
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
#endif
        
        // These tests will only pass on an account that has metrics enabled. 
        // [TestMethod]
        [Description("Check MetricsEntity population")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientPopulateMetricsQuery()
        {
            Blob.CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            Table.CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, tableClient.Credentials);

            MetricsEntity mEntity = analyticsClient.CreateHourMetricsQuery(StorageService.Blob, StorageLocation.Primary).Execute().First();
            DynamicTableEntity dtEntity = tableClient.GetTableReference("$MetricsHourPrimaryTransactionsBlob").CreateQuery<DynamicTableEntity>().Execute().First();

            IDictionary<string, EntityProperty> mEntityDictionary = mEntity.WriteEntity(null);
            IDictionary<string, EntityProperty> dtEntityDictionary = dtEntity.Properties;

            // Note that the other direction will fail because the PartitionKey/RowKey are not present in dtEntityDictionary
            foreach (KeyValuePair<string, EntityProperty> pair in mEntityDictionary)
            {
                EntityProperty propertyValue;
                bool propertyExists = dtEntityDictionary.TryGetValue(pair.Key, out propertyValue);
                Assert.IsTrue(propertyExists);
                Assert.AreEqual(propertyValue, pair.Value);
            }
        }

        // These tests will only pass on an account that has metrics enabled.
        //[TestMethod]
        [Description("Check CapacityEntity population")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientPopulateCapacityQuery()
        {
            Blob.CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            Table.CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, tableClient.Credentials);

            CapacityEntity cEntity = analyticsClient.CreateCapacityQuery().Execute().First();
            DynamicTableEntity dtEntity = tableClient.GetTableReference("$MetricsCapacityBlob").CreateQuery<DynamicTableEntity>().Execute().First();

            IDictionary<string, EntityProperty> cEntityDictionary = cEntity.WriteEntity(null);
            IDictionary<string, EntityProperty> dtEntityDictionary = dtEntity.Properties;

            // Note that the other direction will fail because the PartitionKey/RowKey are not present in dtEntityDictionary
            foreach (KeyValuePair<string, EntityProperty> pair in cEntityDictionary)
            {
                EntityProperty propertyValue;
                bool propertyExists = dtEntityDictionary.TryGetValue(pair.Key, out propertyValue);
                Assert.IsTrue(propertyExists);
                Assert.AreEqual(propertyValue, pair.Value);
            }
        }
    }
}