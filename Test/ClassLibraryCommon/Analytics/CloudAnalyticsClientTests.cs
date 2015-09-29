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
    using System.Collections;
    using System.Net;
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

            CloudTable fileHourPrimaryTable = analyticsClient.GetHourMetricsTable(StorageService.File);
            CloudTable fileMinutePrimaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.File);
            CloudTable fileHourSecondaryTable = analyticsClient.GetHourMetricsTable(StorageService.File, StorageLocation.Secondary);
            CloudTable fileMinuteSecondaryTable = analyticsClient.GetMinuteMetricsTable(StorageService.File, StorageLocation.Secondary);

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

            Assert.AreEqual("$MetricsHourPrimaryTransactionsFile", fileHourPrimaryTable.Name);
            Assert.AreEqual("$MetricsMinutePrimaryTransactionsFile", fileMinutePrimaryTable.Name);
            Assert.AreEqual("$MetricsHourSecondaryTransactionsFile", fileHourSecondaryTable.Name);
            Assert.AreEqual("$MetricsMinuteSecondaryTransactionsFile", fileMinuteSecondaryTable.Name);
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
        [TestMethod]
        [Description("Validate log parser.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAnalyticsClientParseLogs()
        {
            string logText = "1.0;2011-08-09T18:52:40.9241789Z;GetBlob;AnonymousSuccess;200;18;10;anonymous;;myaccount;blob;\"https://myaccount.blob.core.windows.net/thumb&amp;nails/lake.jpg?timeout=30000\";\"/myaccount/thumbnails/lake.jpg\";a84aa705-8a85-48c5-b064-b43bd22979c3;0;123.100.2.10;2009-09-19;252;0;265;100;0;;;\"0x8CE1B6EA95033D5\";Tuesday, 09-Aug-11 18:52:40 GMT;;;;\"8/9/2011 6:52:40 PM ba98eb12-700b-4d53-9230-33a3330571fc\"" + '\n' + "1.0;2011-08-09T18:02:40.6271789Z;PutBlob;Success;201;28;21;authenticated;myaccount;myaccount;blob;\"https://myaccount.blob.core.windows.net/thumbnails/lake.jpg?timeout=30000\";\"/myaccount/thumbnails/lake.jpg\";fb658ee6-6123-41f5-81e2-4bfdc178fea3;0;201.9.10.20;2009-09-19;438;100;223;0;100;;\"66CbMXKirxDeTr82SXBKbg==\";\"0x8CE1B67AD25AA05\";Tuesday, 09-Aug-11 18:02:40 GMT;;;;\"8/9/2011 6:02:40 PM ab970a57-4a49-45c4-baa9-20b687941e32\"" + '\n' + "2.0;2011-08-09T18:02:40.6271789Z;PutBlob;Success;201;28;21;authenticated;myaccount;myaccount;blob;\"https://myaccount.blob.core.windows.net/thumbnails/lake.jpg?timeout=30000\";\"/myaccount/thumbnails/lake.jpg\";fb658ee6-6123-41f5-81e2-4bfdc178fea3;0;201.9.10.20;2009-09-19;438;100;223;0;100;;\"66CbMXKirxDeTr82SXBKbg==\";\"0x8CE1B67AD25AA05\";Tuesday, 09-Aug-11 18:02:40 GMT;;;;\"8/9/2011 6:02:40 PM ab970a57-4a49-45c4-baa9-20b687941e32\"" + '\n';         
            Blob.CloudBlobClient blobClient = CloudAnalyticsClientTests.GenerateCloudBlobClient();
            Table.CloudTableClient tableClient = CloudAnalyticsClientTests.GenerateCloudTableClient();
            CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, tableClient.Credentials);
            IEnumerable<LogRecord> logRecordsEnumerable = analyticsClient.ListLogRecords(StorageService.Blob);
            CloudBlobContainer container = blobClient.GetContainerReference(CloudAnalyticsClientTests.GetRandomContainerName());
            container.CreateIfNotExists();
            CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
            blob.UploadText(logText);

            IEnumerable<LogRecord> enumerable = CloudAnalyticsClient.ParseLogBlob(blob);
            IEnumerator<LogRecord> enumerator = enumerable.GetEnumerator();

            enumerator.MoveNext();
            LogRecord actualItemOne = enumerator.Current;
            enumerator.MoveNext();
            LogRecord actualItemTwo = enumerator.Current;

            try 
            {
                enumerator.MoveNext();
                LogRecord actualItemThree = enumerator.Current;
                Assert.Fail();
            }
            catch(ArgumentException e)
            {
                Assert.AreEqual(e.Message, "A storage log version of 2.0 is unsupported.");
            }

            LogRecord expectedItemOne = new LogRecord();
            expectedItemOne.VersionNumber = "1.0";
            expectedItemOne.RequestStartTime = DateTimeOffset.ParseExact("2011-08-09T18:52:40.9241789Z", "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            expectedItemOne.OperationType = "GetBlob";
            expectedItemOne.RequestStatus = "AnonymousSuccess";
            expectedItemOne.HttpStatusCode = "200";
            expectedItemOne.EndToEndLatency = new TimeSpan(0, 0, 0, 0, 18);
            expectedItemOne.ServerLatency = new TimeSpan(0, 0, 0, 0, 10);
            expectedItemOne.AuthenticationType = "anonymous";
            expectedItemOne.RequesterAccountName = null;
            expectedItemOne.OwnerAccountName = "myaccount";
            expectedItemOne.ServiceType = "blob";
            expectedItemOne.RequestUrl = new Uri("https://myaccount.blob.core.windows.net/thumb&nails/lake.jpg?timeout=30000");
            expectedItemOne.RequestedObjectKey = "/myaccount/thumbnails/lake.jpg";
            expectedItemOne.RequestIdHeader = new Guid("a84aa705-8a85-48c5-b064-b43bd22979c3");
            expectedItemOne.OperationCount = 0;
            expectedItemOne.RequesterIPAddress = "123.100.2.10";
            expectedItemOne.RequestVersionHeader = "2009-09-19";
            expectedItemOne.RequestHeaderSize = 252;
            expectedItemOne.RequestPacketSize = 0;
            expectedItemOne.ResponseHeaderSize = 265;
            expectedItemOne.ResponsePacketSize = 100;
            expectedItemOne.RequestContentLength = 0;
            expectedItemOne.RequestMD5 = null;
            expectedItemOne.ServerMD5 = null;
            expectedItemOne.ETagIdentifier = "0x8CE1B6EA95033D5";
            expectedItemOne.LastModifiedTime = DateTimeOffset.ParseExact("Tuesday, 09-Aug-11 18:52:40 GMT", "dddd, dd-MMM-yy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            expectedItemOne.ConditionsUsed = null;
            expectedItemOne.UserAgentHeader = null;
            expectedItemOne.ReferrerHeader = null;
            expectedItemOne.ClientRequestId = "8/9/2011 6:52:40 PM ba98eb12-700b-4d53-9230-33a3330571fc";

            LogRecord expectedItemTwo = new LogRecord();
            expectedItemTwo.VersionNumber = "1.0";
            expectedItemTwo.RequestStartTime = DateTimeOffset.ParseExact("2011-08-09T18:02:40.6271789Z", "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            expectedItemTwo.OperationType = "PutBlob";
            expectedItemTwo.RequestStatus = "Success";
            expectedItemTwo.HttpStatusCode = "201";
            expectedItemTwo.EndToEndLatency = new TimeSpan(0, 0, 0, 0, 28);
            expectedItemTwo.ServerLatency = new TimeSpan(0, 0, 0, 0, 21);
            expectedItemTwo.AuthenticationType = "authenticated";
            expectedItemTwo.RequesterAccountName = "myaccount";
            expectedItemTwo.OwnerAccountName = "myaccount";
            expectedItemTwo.ServiceType = "blob";
            expectedItemTwo.RequestUrl = new Uri("https://myaccount.blob.core.windows.net/thumbnails/lake.jpg?timeout=30000");
            expectedItemTwo.RequestedObjectKey = "/myaccount/thumbnails/lake.jpg";
            expectedItemTwo.RequestIdHeader = new Guid("fb658ee6-6123-41f5-81e2-4bfdc178fea3");
            expectedItemTwo.OperationCount = 0;
            expectedItemTwo.RequesterIPAddress = "201.9.10.20";
            expectedItemTwo.RequestVersionHeader = "2009-09-19";
            expectedItemTwo.RequestHeaderSize = 438;
            expectedItemTwo.RequestPacketSize = 100;
            expectedItemTwo.ResponseHeaderSize = 223;
            expectedItemTwo.ResponsePacketSize = 0;
            expectedItemTwo.RequestContentLength = 100;
            expectedItemTwo.RequestMD5 = null;
            expectedItemTwo.ServerMD5 = "66CbMXKirxDeTr82SXBKbg==";
            expectedItemTwo.ETagIdentifier = "0x8CE1B67AD25AA05";
            expectedItemTwo.LastModifiedTime = DateTimeOffset.ParseExact("Tuesday, 09-Aug-11 18:02:40 GMT", "dddd, dd-MMM-yy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            expectedItemTwo.ConditionsUsed = null;
            expectedItemTwo.UserAgentHeader = null;
            expectedItemTwo.ReferrerHeader = null;
            expectedItemTwo.ClientRequestId = "8/9/2011 6:02:40 PM ab970a57-4a49-45c4-baa9-20b687941e32";
        
            CloudAnalyticsClientTests.AssertLogItemsEqual(expectedItemOne, actualItemOne);
            CloudAnalyticsClientTests.AssertLogItemsEqual(expectedItemTwo, actualItemTwo);
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

            MetricsEntity metricsEntity = analyticsClient.CreateHourMetricsQuery(StorageService.Blob, StorageLocation.Primary).Execute().First();
            DynamicTableEntity dynamicEntity = tableClient.GetTableReference("$MetricsHourPrimaryTransactionsBlob").CreateQuery<DynamicTableEntity>().Execute().First();

            IDictionary<string, EntityProperty> metricsEntityDictionary = metricsEntity.WriteEntity(null);
            IDictionary<string, EntityProperty> dynamicEntityDictionary = dynamicEntity.Properties;

            // Note that the other direction will fail because the PartitionKey/RowKey are not present in dtEntityDictionary
            foreach (KeyValuePair<string, EntityProperty> pair in metricsEntityDictionary)
            {
                EntityProperty propertyValue;
                bool propertyExists = dynamicEntityDictionary.TryGetValue(pair.Key, out propertyValue);
                Assert.IsTrue(propertyExists);
                Assert.AreEqual(propertyValue, pair.Value);
            }
        }

        // These tests will only pass on an account that has metrics enabled.
        // [TestMethod]
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

            CapacityEntity capacityEntity = analyticsClient.CreateCapacityQuery().Execute().First();
            DynamicTableEntity dynamicEntity = tableClient.GetTableReference("$MetricsCapacityBlob").CreateQuery<DynamicTableEntity>().Execute().First();

            IDictionary<string, EntityProperty> capacityEntityDictionary = capacityEntity.WriteEntity(null);
            IDictionary<string, EntityProperty> dynamicEntityDictionary = dynamicEntity.Properties;

            // Note that the other direction will fail because the PartitionKey/RowKey are not present in dtEntityDictionary
            foreach (KeyValuePair<string, EntityProperty> pair in capacityEntityDictionary)
            {
                EntityProperty propertyValue;
                bool propertyExists = dynamicEntityDictionary.TryGetValue(pair.Key, out propertyValue);
                Assert.IsTrue(propertyExists);
                Assert.AreEqual(propertyValue, pair.Value);
            }
        }
    }   
}