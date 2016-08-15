// -----------------------------------------------------------------------------------------
// <copyright file="TableQueryGenericTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Table.Entities;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableQueryGenericTests : TableTestBase
    {
        #region Locals + Ctors
        public TableQueryGenericTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        static CloudTable currentTable = null;
        static CloudTableClient tableClient = null;
        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            currentTable.CreateIfNotExists();

            for (int i = 0; i < 15; i++)
            {
                TableBatchOperation batch = new TableBatchOperation();

                for (int j = 0; j < 100; j++)
                {
                    BaseEntity ent = GenerateRandomEntity("tables_batch_" + i.ToString());
                    ent.RowKey = string.Format("{0:0000}", j);
                    batch.Insert(ent);
                }

                currentTable.ExecuteBatch(batch);
            }
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            currentTable.DeleteIfExists();
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.TableBufferManager != null)
            {
                TestBase.TableBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }
        #endregion

        #region Unit Tests

        #region Query Segmented

        #region Sync

        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryBasicSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryBasicSync(payloadFormat);
            }
        }

        private void DoTableGenericQueryBasicSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment<BaseEntity> seg = currentTable.ExecuteQuerySegmented(query, null);

            foreach (BaseEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                ent.Validate();
            }
        }

        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryComplexWithoutPropertyResolverSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryComplexWithoutPropertyResolverSync(payloadFormat);
            }
        }

        private void DoTableGenericQueryComplexWithoutPropertyResolverSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            CloudTable currentTestTable = tableClient.GetTableReference("tbl" + Guid.NewGuid().ToString("N"));
            try
            {
                currentTestTable.CreateIfNotExists();

                ComplexEntity ent = new ComplexEntity("tables_batch_1", Guid.NewGuid().ToString());
                currentTestTable.Execute(TableOperation.Insert(ent));

                TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

                TableQuerySegment<ComplexEntity> seg = currentTestTable.ExecuteQuerySegmented(query, null);

                foreach (ComplexEntity retrievedEnt in seg)
                {
                    Assert.AreEqual(retrievedEnt.PartitionKey, "tables_batch_1");
                    ComplexEntity.AssertEquality(ent, retrievedEnt);
                }
            }
            finally
            {
                currentTestTable.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithContinuationSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryWithContinuationSync(payloadFormat);
            }
        }

        private void DoTableGenericQueryWithContinuationSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<BaseEntity> seg = currentTable.ExecuteQuerySegmented(query, null, null, opContext);

            int count = 0;
            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            seg = currentTable.ExecuteQuerySegmented(query, seg.ContinuationToken, null, opContext);

            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }

        [TestMethod]
        [Description("A test to validate empty header values")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableEmptyHeaderSigningTest()
        {
            CloudTableClient client = GenerateCloudTableClient();
            CloudTable currentTable = client.GetTableReference(GenerateRandomTableName());
            OperationContext context = new OperationContext();
            try
            {
                context.UserHeaders = new Dictionary<string, string>();
                context.UserHeaders.Add("x-ms-foo", String.Empty);
                currentTable.Create(null, context);
                DynamicTableEntity ent = new DynamicTableEntity("pk", "rk");
                currentTable.Execute(TableOperation.Insert(ent), null, context);
            }
            finally
            {
                currentTable.DeleteIfExists(null, context);
            }
        }

        #endregion

        #region APM

        [TestMethod]
        [Description("A test to validate basic table query APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryBasicAPM()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment<BaseEntity> seg = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteQuerySegmented(query, null, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                seg = currentTable.EndExecuteQuerySegmented<BaseEntity>(asyncRes);
            }

            foreach (BaseEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                ent.Validate();
            }
        }

        [TestMethod]
        [Description("A test to validate basic table continuation APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithContinuationAPM()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<BaseEntity> seg = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteQuerySegmented(query, null, null, opContext, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                seg = currentTable.EndExecuteQuerySegmented<BaseEntity>(asyncRes);
            }

            int count = 0;
            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteQuerySegmented(query, seg.ContinuationToken, null, opContext, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                seg = currentTable.EndExecuteQuerySegmented<BaseEntity>(asyncRes);
            }

            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }
        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryBasicTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment<BaseEntity> seg = currentTable.ExecuteQuerySegmentedAsync(query, null).Result;

            foreach (BaseEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                ent.Validate();
            }
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithContinuationTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<BaseEntity> seg = currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext).Result;

            int count = 0;
            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            seg = currentTable.ExecuteQuerySegmentedAsync(query, seg.ContinuationToken, null, opContext).Result;

            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }

        [TestMethod]
        [Description("A test to validate basic table continuation with null target location")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithContinuationTokenNullTarget()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            OperationContext opContext = new OperationContext();
            TableQuerySegment<BaseEntity> seg = currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext).Result;
            
            int count = 0;
            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            // Second segment to validate integrity of continuation query execution and results with a null target location
            Assert.IsNotNull(seg.ContinuationToken);
            seg.ContinuationToken.TargetLocation = null;
            seg = currentTable.ExecuteQuerySegmentedAsync(query, seg.ContinuationToken, null, opContext).Result;

            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }


        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            TableContinuationToken token = null;

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenCancellationTokenTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            TableContinuationToken token = null;
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            EntityResolver<BaseEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                BaseEntity entity = new BaseEntity(partitionKey, rowKey);
                entity.ETag = etag;
                entity.foo = properties["foo"].StringValue;
                entity.A = properties["A"].StringValue;
                entity.B = properties["B"].StringValue;
                entity.C = properties["C"].StringValue;
                entity.D = properties["D"].StringValue;
                entity.E = properties["E"].Int32Value.Value;
                return entity;
            };
            TableContinuationToken token = null;

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenRequestOptionsOperationContextTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenCancellationTokenTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            EntityResolver<BaseEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                BaseEntity entity = new BaseEntity(partitionKey, rowKey);
                entity.ETag = etag;
                entity.foo = properties["foo"].StringValue;
                entity.A = properties["A"].StringValue;
                entity.B = properties["B"].StringValue;
                entity.C = properties["C"].StringValue;
                entity.D = properties["D"].StringValue;
                entity.E = properties["E"].Int32Value.Value;
                return entity;
            };
            TableContinuationToken token = null;
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenRequestOptionsOperationContextCancellationTokenTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenRequestOptionsOperationContextTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            EntityResolver<BaseEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                BaseEntity entity = new BaseEntity(partitionKey, rowKey);
                entity.ETag = etag;
                entity.foo = properties["foo"].StringValue;
                entity.A = properties["A"].StringValue;
                entity.B = properties["B"].StringValue;
                entity.C = properties["C"].StringValue;
                entity.D = properties["D"].StringValue;
                entity.E = properties["E"].Int32Value.Value;
                return entity;
            };
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenRequestOptionsOperationContextCancellationTokenTask()
        {
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            EntityResolver<BaseEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                BaseEntity entity = new BaseEntity(partitionKey, rowKey);
                entity.ETag = etag;
                entity.foo = properties["foo"].StringValue;
                entity.A = properties["A"].StringValue;
                entity.B = properties["B"].StringValue;
                entity.C = properties["C"].StringValue;
                entity.D = properties["D"].StringValue;
                entity.E = properties["E"].Int32Value.Value;
                return entity;
            };
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<BaseEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (BaseEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    entity.Validate();
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }
#endif

        #endregion

        #endregion

        [TestMethod]
        [Description("A test to validate basic table filtering")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithFilter()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryWithFilter(payloadFormat);
            }
        }

        private void DoTableGenericQueryWithFilter(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(string.Format("(PartitionKey eq '{0}') and (RowKey ge '{1}')", "tables_batch_1", "0050"));

            OperationContext opContext = new OperationContext();
            int count = 0;

            foreach (BaseEntity ent in currentTable.ExecuteQuery(query))
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.RowKey, string.Format("{0:0000}", count + 50));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(count, 50);
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryEnumerateTwice()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryEnumerateTwice(payloadFormat);
            }
        }

        private void DoTableGenericQueryEnumerateTwice(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();

            OperationContext opContext = new OperationContext();
            IEnumerable<BaseEntity> enumerable = currentTable.ExecuteQuery(query);

            List<BaseEntity> firstIteration = new List<BaseEntity>();
            List<BaseEntity> secondIteration = new List<BaseEntity>();

            foreach (BaseEntity ent in enumerable)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                firstIteration.Add(ent);
            }

            foreach (BaseEntity ent in enumerable)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                secondIteration.Add(ent);
            }

            Assert.AreEqual(firstIteration.Count, secondIteration.Count);

            for (int m = 0; m < firstIteration.Count; m++)
            {
                Assert.AreEqual(firstIteration[m].PartitionKey, secondIteration[m].PartitionKey);
                Assert.AreEqual(firstIteration[m].RowKey, secondIteration[m].RowKey);
                Assert.AreEqual(firstIteration[m].Timestamp, secondIteration[m].Timestamp);
                Assert.AreEqual(firstIteration[m].ETag, secondIteration[m].ETag);
                firstIteration[m].Validate();
            }
        }

        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjection()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryProjection(payloadFormat, false);
                DoTableGenericQueryProjection(payloadFormat, true);
            }
        }

        private void DoTableGenericQueryProjection(TablePayloadFormat format, bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "C" });

            foreach (BaseEntity ent in currentTable.ExecuteQuery(query))
            {
                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 0);

                if (tableClient.DefaultRequestOptions.ProjectSystemProperties.HasValue)
                {
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.PartitionKey == default(string), "Missing expected " + TableConstants.PartitionKey);
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.RowKey == default(string), "Missing expected " + TableConstants.RowKey);
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.Timestamp == default(DateTimeOffset), "Missing expected " + TableConstants.Timestamp);
                }
            }
        }

        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjectionSpecifyingSystemProperties()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryProjectionSpecifyingSystemProperties(payloadFormat, false);
                DoTableGenericQueryProjectionSpecifyingSystemProperties(payloadFormat, true);
            }
        }

        private void DoTableGenericQueryProjectionSpecifyingSystemProperties(TablePayloadFormat format, bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "C", TableConstants.PartitionKey, TableConstants.Timestamp });

            foreach (BaseEntity ent in currentTable.ExecuteQuery(query))
            {
                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 0);
                Assert.AreNotEqual(default(string), ent.PartitionKey);
                Assert.AreNotEqual(default(DateTimeOffset), ent.Timestamp);

                if (tableClient.DefaultRequestOptions.ProjectSystemProperties.HasValue)
                {
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.RowKey == default(string), "Missing expected " + TableConstants.RowKey);
                }
            }
        }

        [TestMethod]
        [Description("Projection test with nullable values.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjectionWithNull()
        {
            CloudTable table = tableClient.GetTableReference(GenerateRandomTableName());
            try
            {
                // Create a new table so we don't pollute the main query table
                table.CreateIfNotExists();

                // Insert an entity which is missing B, a string, and E, an int
                DynamicTableEntity entity = new DynamicTableEntity()
                {
                    PartitionKey = Guid.NewGuid().ToString(),
                    RowKey = DateTime.Now.Ticks.ToString()
                };
                entity.Properties.Add("A", new EntityProperty("a"));
                table.Execute(TableOperation.Insert(entity));

                DoTableGenericQueryProjectionWithSpecialCases(table, TablePayloadFormat.Json, false);
                DoTableGenericQueryProjectionWithSpecialCases(table, TablePayloadFormat.Json, true);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Projection test with incorrect types.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjectionWithIncorrectTypes()
        {
            CloudTable table = tableClient.GetTableReference(GenerateRandomTableName());
            try
            {
                // Create a new table so we don't pollute the main query table
                table.CreateIfNotExists();

                // Insert an entity with B as an int and E as a bool
                // BaseEntity defines B as a string and E as an int
                DynamicTableEntity entity = new DynamicTableEntity()
                {
                    PartitionKey = Guid.NewGuid().ToString(),
                    RowKey = DateTime.Now.Ticks.ToString()
                };
                entity.Properties.Add("A", new EntityProperty("a"));
                entity.Properties.Add("B", new EntityProperty(1234));
                entity.Properties.Add("E", new EntityProperty(true));
                table.Execute(TableOperation.Insert(entity));

                DoTableGenericQueryProjectionWithSpecialCases(table, TablePayloadFormat.Json, false);
                DoTableGenericQueryProjectionWithSpecialCases(table, TablePayloadFormat.Json, true);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        public void DoTableGenericQueryProjectionWithSpecialCases(CloudTable table, TablePayloadFormat format, bool disableCompiledSerializers)
        {
            table.ServiceClient.DefaultRequestOptions.PayloadFormat = format;
            BaseEntity.DisableCompiledSerializers = disableCompiledSerializers;

            // Query on A, B, and E
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "B", "E" });
            IEnumerable<BaseEntity> entities = table.ExecuteQuery(query, null);

            // Verify A has a set value and B and E have type defaults
            Assert.AreEqual(1, entities.Count());
            BaseEntity entity = entities.First();
            Assert.AreEqual("a", entity.A);
            Assert.IsNull(entity.B);
            Assert.AreEqual(0, entity.E);      
        }

        [TestMethod]
        [Description("Basic with resolver")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericWithResolver()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericWithResolver(payloadFormat);
            }
        }

        private void DoTableGenericWithResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<TableEntity> query = new TableQuery<TableEntity>().Select(new List<string>() { "A", "C", "E" });
            query.TakeCount = 1000;

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) => BaseEntity.BaseEntityPropertyResolver(pk, rk, propName, propValue)
            };

            foreach (string ent in currentTable.ExecuteQuery(query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue + prop["E"].Int32Value, options, null))
            {
                Assert.AreEqual(ent, "ac" + 1234);
            }

            foreach (BaseEntity ent in currentTable.ExecuteQuery(query,
                (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, E = prop["E"].Int32Value.Value, ETag = etag }, options, null))
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);
                Assert.IsNotNull(ent.ETag);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 1234);
            }

            Assert.AreEqual(1000, query.TakeCount);
        }

        [TestMethod]
        [Description("Basic with resolver")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericWithResolverAPM()
        {
            TableQuery<TableEntity> query = new TableQuery<TableEntity>().Select(new List<string>() { "A", "C" });

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                TableContinuationToken token = null;
                List<string> list = new List<string>();
                do
                {
                    IAsyncResult result = currentTable.BeginExecuteQuerySegmented(query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue, token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    TableQuerySegment<string> segment = currentTable.EndExecuteQuerySegmented<TableEntity, string>(result);
                    list.AddRange(segment.Results);
                    token = segment.ContinuationToken;
                } while (token != null);

                foreach (string ent in list)
                {
                    Assert.AreEqual(ent, "ac");
                }

                List<BaseEntity> list1 = new List<BaseEntity>();
                do
                {
                    IAsyncResult result = currentTable.BeginExecuteQuerySegmented(query, (pk, rk, ts, prop, etag) => new BaseEntity()
                    {
                        PartitionKey = pk,
                        RowKey = rk,
                        Timestamp = ts,
                        A = prop["A"].StringValue,
                        C = prop["C"].StringValue,
                        ETag = etag
                    }, token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    TableQuerySegment<BaseEntity> segment = currentTable.EndExecuteQuerySegmented<TableEntity, BaseEntity>(result);
                    list1.AddRange(segment.Results);
                    token = segment.ContinuationToken;
                } while (token != null);

                foreach (BaseEntity ent in list1)
                {
                    Assert.IsNotNull(ent.PartitionKey);
                    Assert.IsNotNull(ent.RowKey);
                    Assert.IsNotNull(ent.Timestamp);
                    Assert.IsNotNull(ent.ETag);

                    Assert.AreEqual(ent.A, "a");
                    Assert.IsNull(ent.B);
                    Assert.AreEqual(ent.C, "c");
                    Assert.IsNull(ent.D);
                }
            }
        }

#if TASK
        [TestMethod]
        [Description("Basic with resolver")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericWithResolverTask()
        {
            TableQuery<TableEntity> query = new TableQuery<TableEntity>().Select(new List<string>() { "A", "C" });

            TableContinuationToken token = null;
            List<string> list = new List<string>();
            do
            {
                TableQuerySegment<string> segment =
                    currentTable.ExecuteQuerySegmentedAsync(
                        query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue, token).Result;
                list.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (string ent in list)
            {
                Assert.AreEqual(ent, "ac");
            }

            List<BaseEntity> list1 = new List<BaseEntity>();
            do
            {
                TableQuerySegment<BaseEntity> segment =
                    currentTable.ExecuteQuerySegmentedAsync(
                        query,
                        (pk, rk, ts, prop, etag) =>
                        new BaseEntity()
                            {
                                PartitionKey = pk,
                                RowKey = rk,
                                Timestamp = ts,
                                A = prop["A"].StringValue,
                                C = prop["C"].StringValue,
                                ETag = etag
                            },
                        token).Result;

                list1.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (BaseEntity ent in list1)
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);
                Assert.IsNotNull(ent.ETag);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
            }

        }
#endif

        [TestMethod]
        [Description("Basic resolver test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryResolverWithDynamic()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryResolverWithDynamic(payloadFormat);
            }
        }

        private void DoTableQueryResolverWithDynamic(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) => BaseEntity.BaseEntityPropertyResolver(pk, rk, propName, propValue)
            };

            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C", "E" });
            foreach (string ent in currentTable.ExecuteQuery(query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue + prop["E"].Int32Value, options, null))
            {
                Assert.AreEqual(ent, "ac" + 1234);
            }

            foreach (BaseEntity ent in currentTable.ExecuteQuery(query,
                            (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, E = prop["E"].Int32Value.Value, ETag = etag }, options, null))
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);
                Assert.IsNotNull(ent.ETag);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 1234);
            }
        }

        [TestMethod]
        [Description("TableQuerySegmented resolver test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQuerySegmentedResolver()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQuerySegmentedResolver(payloadFormat);
            }
        }

        private void DoTableQuerySegmentedResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "C", "E" });
            TableContinuationToken token = null;
            List<string> list = new List<string>();
            do
            {
                TableQuerySegment<string> segment = currentTable.ExecuteQuerySegmented<BaseEntity, string>(query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue + prop["E"].Int32Value, token);
                list.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (string ent in list)
            {
                Assert.AreEqual(ent, "ac" + 1234);
            }

            List<BaseEntity> list1 = new List<BaseEntity>();
            do
            {
                TableQuerySegment<BaseEntity> segment = currentTable.ExecuteQuerySegmented<BaseEntity, BaseEntity>(query, (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, E = prop["E"].Int32Value.Value, ETag = etag }, token);
                list1.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (BaseEntity ent in list1)
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);
                Assert.IsNotNull(ent.ETag);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 1234);
            }
        }

        [TestMethod]
        [Description("TableQuerySegmented resolver test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQuerySegmentedResolverWithDynamic()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQuerySegmentedResolverWithDynamic(payloadFormat);
            }
        }

        private void DoTableQuerySegmentedResolverWithDynamic(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) => BaseEntity.BaseEntityPropertyResolver(pk, rk, propName, propValue)
            };

            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C", "E" });
            TableContinuationToken token = null;
            List<string> list = new List<string>();
            do
            {
                TableQuerySegment<string> segment = currentTable.ExecuteQuerySegmented<string>(query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue + prop["E"].Int32Value, token, options, null);
                list.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (string ent in list)
            {
                Assert.AreEqual(ent, "ac" + 1234);
            }

            List<BaseEntity> list1 = new List<BaseEntity>();
            do
            {
                TableQuerySegment<BaseEntity> segment = currentTable.ExecuteQuerySegmented<BaseEntity>(query, (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, E = prop["E"].Int32Value.Value, ETag = etag }, token, options, null);
                list1.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (BaseEntity ent in list1)
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);
                Assert.IsNotNull(ent.ETag);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 1234);
            }
        }

        [TestMethod]
        [Description("Basic resolver test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryResolverWithDynamicAPM()
        {
            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C" });
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                TableContinuationToken token = null;
                List<string> list = new List<string>();
                do
                {
                    IAsyncResult result = currentTable.BeginExecuteQuerySegmented(query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue, token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    TableQuerySegment<string> segment = currentTable.EndExecuteQuerySegmented<string>(result);
                    list.AddRange(segment.Results);
                    token = segment.ContinuationToken;
                } while (token != null);

                foreach (string ent in list)
                {
                    Assert.AreEqual(ent, "ac");
                }

                List<BaseEntity> list1 = new List<BaseEntity>();
                do
                {
                    IAsyncResult result = currentTable.BeginExecuteQuerySegmented(query, (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, ETag = etag }, token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    TableQuerySegment<BaseEntity> segment = currentTable.EndExecuteQuerySegmented<BaseEntity>(result);
                    list1.AddRange(segment.Results);
                    token = segment.ContinuationToken;
                } while (token != null);

                foreach (BaseEntity ent in list1)
                {
                    Assert.IsNotNull(ent.PartitionKey);
                    Assert.IsNotNull(ent.RowKey);
                    Assert.IsNotNull(ent.Timestamp);
                    Assert.IsNotNull(ent.ETag);

                    Assert.AreEqual(ent.A, "a");
                    Assert.IsNull(ent.B);
                    Assert.AreEqual(ent.C, "c");
                    Assert.IsNull(ent.D);
                }
            }
        }

#if TASK
        [TestMethod]
        [Description("Basic resolver test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryResolverWithDynamicTask()
        {
            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C" });
            TableContinuationToken token = null;
            List<string> list = new List<string>();
            do
            {
                TableQuerySegment<string> segment =
                    currentTable.ExecuteQuerySegmentedAsync(
                        query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue, token).Result;
                list.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (string ent in list)
            {
                Assert.AreEqual(ent, "ac");
            }

            List<BaseEntity> list1 = new List<BaseEntity>();
            do
            {
                TableQuerySegment<BaseEntity> segment =
                    currentTable.ExecuteQuerySegmentedAsync(
                        query,
                        (pk, rk, ts, prop, etag) => new BaseEntity()
                            {
                                PartitionKey = pk,
                                RowKey = rk,
                                Timestamp = ts,
                                A = prop["A"].StringValue,
                                C = prop["C"].StringValue,
                                ETag = etag
                            },
                        token).Result;

                list1.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            foreach (BaseEntity ent in list1)
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);
                Assert.IsNotNull(ent.ETag);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
            }
        }
#endif

        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryOnSupportedTypes()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryOnSupportedTypes(payloadFormat);
            }
        }

        private void DoTableGenericQueryOnSupportedTypes(TablePayloadFormat format)
        {
            CloudTableClient client = GenerateCloudTableClient();

            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            table.Create();
            client.DefaultRequestOptions.PayloadFormat = format;

            try
            {
                // Setup
                TableBatchOperation batch = new TableBatchOperation();
                string pk = Guid.NewGuid().ToString();
                ComplexEntity middleRef = null;
                for (int m = 0; m < 100; m++)
                {
                    ComplexEntity complexEntity = new ComplexEntity(pk, string.Format("{0:0000}", m));
                    complexEntity.String = string.Format("{0:0000}", m);
                    complexEntity.Binary = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.BinaryPrimitive = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.Bool = m % 2 == 0 ? true : false;
                    complexEntity.BoolPrimitive = m % 2 == 0 ? true : false;
                    complexEntity.Double = m + ((double)m / 100);
                    complexEntity.DoublePrimitive = m + ((double)m / 100);
                    complexEntity.Int32 = m;
                    complexEntity.IntegerPrimitive = m;
                    complexEntity.Int64 = (long)int.MaxValue + m;
                    complexEntity.LongPrimitive = (long)int.MaxValue + m;
                    complexEntity.Guid = Guid.NewGuid();

                    batch.Insert(complexEntity);

                    if (m == 50)
                    {
                        middleRef = complexEntity;
                    }

                    // Add delay to make times unique
                    Thread.Sleep(100);
                }

                table.ExecuteBatch(batch);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", QueryComparisons.GreaterThanOrEqual, "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", QueryComparisons.Equal, middleRef.Guid), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", QueryComparisons.GreaterThanOrEqual,
                                middleRef.LongPrimitive), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.LongPrimitive), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Double), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.DoublePrimitive), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Int32), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.IntegerPrimitive), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", QueryComparisons.GreaterThanOrEqual,
                                middleRef.DateTimeOffset), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", QueryComparisons.Equal, middleRef.Bool), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", QueryComparisons.Equal, middleRef.BoolPrimitive),
                        50);

                // 8. Filter on Binary 
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.Equal, middleRef.Binary), 1);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive", QueryComparisons.Equal,
                                middleRef.BinaryPrimitive), 1);

                // 9. Filter on Binary GTE
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Binary), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.BinaryPrimitive), 50);

                // 10. Complex Filter on Binary GTE
                ExecuteQueryAndAssertResults(table, TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                middleRef.PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Binary)), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.BinaryPrimitive), 50);


            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithSpecificOnSupportedTypes()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryWithSpecificOnSupportedTypes(payloadFormat);
            }
        }

        private void DoTableGenericQueryWithSpecificOnSupportedTypes(TablePayloadFormat format)
        {
            CloudTableClient client = GenerateCloudTableClient();

            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            table.Create();
            client.DefaultRequestOptions.PayloadFormat = format;

            try
            {
                // Setup
                TableBatchOperation batch = new TableBatchOperation();
                string pk = Guid.NewGuid().ToString();
                ComplexEntity middleRef = null;
                for (int m = 0; m < 100; m++)
                {
                    ComplexEntity complexEntity = new ComplexEntity(pk, string.Format("{0:0000}", m));
                    complexEntity.String = string.Format("{0:0000}", m);
                    complexEntity.Binary = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.BinaryPrimitive = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.Bool = m % 2 == 0 ? true : false;
                    complexEntity.BoolPrimitive = m % 2 == 0 ? true : false;
                    complexEntity.Double = m + ((double)m / 100);
                    complexEntity.DoublePrimitive = m + ((double)m / 100);
                    complexEntity.Int32 = m;
                    complexEntity.IntegerPrimitive = m;
                    complexEntity.Int64 = (long)int.MaxValue + m;
                    complexEntity.LongPrimitive = (long)int.MaxValue + m;
                    complexEntity.Guid = Guid.NewGuid();

                    batch.Insert(complexEntity);

                    if (m == 50)
                    {
                        middleRef = complexEntity;
                    }

                    // Add delay to make times unique
                    Thread.Sleep(100);
                }

                table.ExecuteBatch(batch);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", QueryComparisons.GreaterThanOrEqual, "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", QueryComparisons.Equal, middleRef.Guid), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", QueryComparisons.GreaterThanOrEqual,
                                middleRef.LongPrimitive), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.LongPrimitive), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Double), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.DoublePrimitive), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Int32), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.IntegerPrimitive), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", QueryComparisons.GreaterThanOrEqual,
                                middleRef.DateTimeOffset), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", QueryComparisons.Equal, middleRef.Bool), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", QueryComparisons.Equal, middleRef.BoolPrimitive),
                        50);

                // 8. Filter on Binary 
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.Equal, middleRef.Binary), 1);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive", QueryComparisons.Equal,
                                middleRef.BinaryPrimitive), 1);

                // 9. Filter on Binary GTE
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Binary), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.BinaryPrimitive), 50);

                // 10. Complex Filter on Binary GTE
                ExecuteQueryAndAssertResults(table, TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                middleRef.PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Binary)), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.BinaryPrimitive), 50);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("A test to validate basic take Count with and without continuations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryGenericWithTakeCount()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryGenericWithTakeCount(payloadFormat);
            }
        }

        private void DoTableQueryGenericWithTakeCount(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // No continuation
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Take(100);

            OperationContext opContext = new OperationContext();
            IEnumerable<BaseEntity> enumerable = currentTable.ExecuteQuery(query, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 1);


            // With continuations
            query.TakeCount = 1200;
            opContext = new OperationContext();
            enumerable = currentTable.ExecuteQuery(query, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 2);

            foreach (BaseEntity entity in enumerable)
            {
                entity.Validate();
            }
        }

        [TestMethod]
        [Description("A test to validate basic take Count with a resolver, with and without continuations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryGenericWithTakeCountAndResolver()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryGenericWithTakeCountAndResolver(payloadFormat);
            }
        }

        private void DoTableQueryGenericWithTakeCountAndResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // No continuation
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Take(100);

            OperationContext opContext = new OperationContext();
            IEnumerable<string> enumerable = currentTable.ExecuteQuery(query, (pk, rk, ts, prop, etag) => pk + rk, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 1);

            // With continuations
            query.TakeCount = 1200;
            opContext = new OperationContext();
            enumerable = currentTable.ExecuteQuery(query, (pk, rk, ts, prop, etag) => pk + rk, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 2);
        }

        [TestMethod]
        [Description("A test to validate EntityActivator can activate internal types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithInternalType()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryWithInternalType(payloadFormat);
            }
        }

        private void DoTableGenericQueryWithInternalType(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<InternalEntity> query = new TableQuery<InternalEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment<InternalEntity> seg = currentTable.ExecuteQuerySegmented(query, null);

            foreach (InternalEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                ent.Validate();
            }
        }

        [TestMethod]
        [Description("A test to ensure that a generic query must have a type with a default constructor ")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryOnTypeWithNoCtor()
        {
            TestHelper.ExpectedException<NotSupportedException>(() => new TableQuery<NoCtorEntity>(), "TableQuery should not be able to be instantiated with a generic type that has no default constructor");
        }
        #endregion

        #region Negative Tests

        [TestMethod]
        [Description("A test with invalid take count")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithInvalidTakeCount()
        {
            try
            {
                TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Take(0);
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.Message, "Take count must be positive and greater than 0.");
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            try
            {
                TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Take(-1);
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.Message, "Take count must be positive and greater than 0.");
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        [Description("A test to invalid query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithInvalidQuery()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableGenericQueryWithInvalidQuery(payloadFormat);
            }
        }

        private void DoTableGenericQueryWithInvalidQuery(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Where(string.Format("(PartitionKey ) and (RowKey ge '{1}')", "tables_batch_1", "000050"));

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteQuerySegmented(query, null, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidInput" }, "One of the request inputs is not valid.");
            }
        }
        #endregion

        [TestMethod]
        [Description("A test to check retrieve functionality Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithPropertyResolverCache()
        {
            DoTableQueryWithPropertyResolverCache(true);
            DoTableQueryWithPropertyResolverCache(false);
        }

        private void DoTableQueryWithPropertyResolverCache(bool disableCache)
        {
            CloudTable table = tableClient.GetTableReference(GenerateRandomTableName());
            try
            {
                table.CreateIfNotExists();

                tableClient.DefaultRequestOptions.PayloadFormat = TablePayloadFormat.JsonNoMetadata;
                TableEntity.DisablePropertyResolverCache = disableCache;

                string pk = Guid.NewGuid().ToString();

                // Add insert
                ComplexEntity sendEnt = new ComplexEntity();
                sendEnt.PartitionKey = pk;
                sendEnt.RowKey = Guid.NewGuid().ToString();

                // insert entity
                table.Execute(TableOperation.Insert(sendEnt));

                // Success
                TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sendEnt.PartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sendEnt.RowKey)));
                IEnumerable<ComplexEntity> result = table.ExecuteQuery<ComplexEntity>(query);

                foreach (ComplexEntity retrievedEntity in result)
                {
                    // Validate entity
                    Assert.AreEqual(sendEnt.String, retrievedEntity.String);

                    Assert.AreEqual(sendEnt.Int64, retrievedEntity.Int64);
                    Assert.AreEqual(sendEnt.Int64N, retrievedEntity.Int64N);

                    Assert.AreEqual(sendEnt.LongPrimitive, retrievedEntity.LongPrimitive);
                    Assert.AreEqual(sendEnt.LongPrimitiveN, retrievedEntity.LongPrimitiveN);

                    Assert.AreEqual(sendEnt.Int32, retrievedEntity.Int32);
                    Assert.AreEqual(sendEnt.Int32N, retrievedEntity.Int32N);
                    Assert.AreEqual(sendEnt.IntegerPrimitive, retrievedEntity.IntegerPrimitive);
                    Assert.AreEqual(sendEnt.IntegerPrimitiveN, retrievedEntity.IntegerPrimitiveN);

                    Assert.AreEqual(sendEnt.Guid, retrievedEntity.Guid);
                    Assert.AreEqual(sendEnt.GuidN, retrievedEntity.GuidN);

                    Assert.AreEqual(sendEnt.Double, retrievedEntity.Double);
                    Assert.AreEqual(sendEnt.DoubleN, retrievedEntity.DoubleN);
                    Assert.AreEqual(sendEnt.DoublePrimitive, retrievedEntity.DoublePrimitive);
                    Assert.AreEqual(sendEnt.DoublePrimitiveN, retrievedEntity.DoublePrimitiveN);

                    Assert.AreEqual(sendEnt.BinaryPrimitive.GetValue(0), retrievedEntity.BinaryPrimitive.GetValue(0));
                    Assert.AreEqual(sendEnt.BinaryPrimitive.GetValue(1), retrievedEntity.BinaryPrimitive.GetValue(1));
                    Assert.AreEqual(sendEnt.BinaryPrimitive.GetValue(2), retrievedEntity.BinaryPrimitive.GetValue(2));
                    Assert.AreEqual(sendEnt.BinaryPrimitive.GetValue(3), retrievedEntity.BinaryPrimitive.GetValue(3));

                    Assert.AreEqual(sendEnt.Binary.GetValue(0), retrievedEntity.Binary.GetValue(0));
                    Assert.AreEqual(sendEnt.Binary.GetValue(1), retrievedEntity.Binary.GetValue(1));
                    Assert.AreEqual(sendEnt.Binary.GetValue(2), retrievedEntity.Binary.GetValue(2));
                    Assert.AreEqual(sendEnt.Binary.GetValue(3), retrievedEntity.Binary.GetValue(3));


                    Assert.AreEqual(sendEnt.BoolPrimitive, retrievedEntity.BoolPrimitive);
                    Assert.AreEqual(sendEnt.BoolPrimitiveN, retrievedEntity.BoolPrimitiveN);
                    Assert.AreEqual(sendEnt.Bool, retrievedEntity.Bool);
                    Assert.AreEqual(sendEnt.BoolN, retrievedEntity.BoolN);

                    Assert.AreEqual(sendEnt.DateTimeOffset, retrievedEntity.DateTimeOffset);
                    Assert.AreEqual(sendEnt.DateTimeOffsetN, retrievedEntity.DateTimeOffsetN);
                    Assert.AreEqual(sendEnt.DateTime, retrievedEntity.DateTime);
                    Assert.AreEqual(sendEnt.DateTimeN, retrievedEntity.DateTimeN);
                }
            }
            finally
            {
                table.DeleteIfExists();
                tableClient.DefaultRequestOptions.PayloadFormat = TablePayloadFormat.Json;
            }
        }
        #region Helpers

        private static void ExecuteQueryAndAssertResults(CloudTable table, string filter, int expectedResults)
        {
            Assert.AreEqual(expectedResults, table.ExecuteQuery(new TableQuery<ComplexEntity>().Where(filter)).Count());
        }

        private static BaseEntity GenerateRandomEntity(string pk)
        {
            BaseEntity ent = new BaseEntity();
            ent.Populate();
            ent.PartitionKey = pk;
            ent.RowKey = Guid.NewGuid().ToString();
            return ent;
        }
        #endregion
    }
}
