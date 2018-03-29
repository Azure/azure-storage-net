﻿// -----------------------------------------------------------------------------------------
// <copyright file="TableBatchOperationTest.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using Microsoft.WindowsAzure.Test.Network;
using Microsoft.WindowsAzure.Test.Network.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableBatchOperationTest : TableTestBase
    {
        #region Locals + Ctors
        public TableBatchOperationTest()
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

        CloudTable currentTable = null;
        CloudTableClient tableClient = null;
        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            currentTable.CreateIfNotExists();
            
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
            currentTable.DeleteIfExists();
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Insert

        #region Sync
        [TestMethod]
        [Description("A test to check the DynamicTableEntity constructor")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableDynamicTableEntityConstructor()
        {
            string pk = Guid.NewGuid().ToString();
            string rk = Guid.NewGuid().ToString();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();
            properties.Add("foo", new EntityProperty("bar"));
            properties.Add("foo1", new EntityProperty("bar1"));

            DynamicTableEntity ent = new DynamicTableEntity(pk, rk, "*", properties);
            currentTable.Execute(TableOperation.Insert(ent));
        }

        [TestMethod]
        [Description("A test to check the DynamicTableEntity setter")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableDynamicTableEntitySetter()
        {
            string pk = Guid.NewGuid().ToString();
            string rk = Guid.NewGuid().ToString();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();
            properties.Add("foo", new EntityProperty("bar"));
            properties.Add("foo1", new EntityProperty("bar1"));

            DynamicTableEntity ent = new DynamicTableEntity();
            ent.PartitionKey = pk;
            ent.RowKey = rk;
            ent.Properties = properties;
            ent.ETag = "*";
            ent.Timestamp = DateTimeOffset.MinValue;
            currentTable.Execute(TableOperation.Insert(ent));
        }

        [TestMethod]
        [Description("A test to check EntityProperty")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableEntityPropertyGenerator()
        {
            string pk = Guid.NewGuid().ToString();
            string rk = Guid.NewGuid().ToString();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();
            EntityProperty boolEntity = EntityProperty.GeneratePropertyForBool(true);
            properties.Add("boolEntity", boolEntity);
            EntityProperty timeEntity = EntityProperty.GeneratePropertyForDateTimeOffset(DateTimeOffset.UtcNow);
            properties.Add("timeEntity", timeEntity);
            EntityProperty doubleEntity = EntityProperty.GeneratePropertyForDouble(0.1);
            properties.Add("doubleEntity", doubleEntity);
            EntityProperty guidEntity = EntityProperty.GeneratePropertyForGuid(Guid.NewGuid());
            properties.Add("guidEntity", guidEntity);
            EntityProperty intEntity = EntityProperty.GeneratePropertyForInt(1);
            properties.Add("intEntity", intEntity);
            EntityProperty longEntity = EntityProperty.GeneratePropertyForLong(1);
            properties.Add("longEntity", longEntity);
            EntityProperty stringEntity = EntityProperty.GeneratePropertyForString("string");
            properties.Add("stringEntity", stringEntity);

            DynamicTableEntity ent = new DynamicTableEntity(pk, rk, "*", properties);
            currentTable.Execute(TableOperation.Insert(ent));
        }

        [TestMethod]
        [Description("A test to check EntityProperty setter")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableEntityPropertySetter()
        {
            string pk = Guid.NewGuid().ToString();
            string rk = Guid.NewGuid().ToString();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();
            EntityProperty boolEntity = EntityProperty.GeneratePropertyForBool(null);
            boolEntity.BooleanValue = true;
            properties.Add("boolEntity", boolEntity);

            EntityProperty timeEntity = EntityProperty.GeneratePropertyForDateTimeOffset(null);
            timeEntity.DateTimeOffsetValue = DateTimeOffset.UtcNow;
            properties.Add("timeEntity", timeEntity);

            EntityProperty doubleEntity = EntityProperty.GeneratePropertyForDouble(null);
            doubleEntity.DoubleValue = 0.1;
            properties.Add("doubleEntity", doubleEntity);

            EntityProperty guidEntity = EntityProperty.GeneratePropertyForGuid(null);
            guidEntity.GuidValue = Guid.NewGuid();
            properties.Add("guidEntity", guidEntity);

            EntityProperty intEntity = EntityProperty.GeneratePropertyForInt(null);
            intEntity.Int32Value = 1;
            properties.Add("intEntity", intEntity);

            EntityProperty longEntity = EntityProperty.GeneratePropertyForLong(null);
            longEntity.Int64Value = 1;
            properties.Add("longEntity", longEntity);

            EntityProperty stringEntity = EntityProperty.GeneratePropertyForString(null);
            stringEntity.StringValue = "string";
            properties.Add("stringEntity", stringEntity);

            DynamicTableEntity ent = new DynamicTableEntity(pk, rk, "*", properties);
            currentTable.Execute(TableOperation.Insert(ent));
        }

        [TestMethod]
        [Description("A test to check batch insert functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchInsertSync(payloadFormat);
            }
        }

        private void DoTableBatchInsertSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            for (int m = 0; m < 3; m++)
            {
                AddInsertToBatch(pk, batch);
            }

            // Add insert
            DynamicTableEntity ent = GenerateRandomEntity(pk);

            currentTable.Execute(TableOperation.Insert(ent));

            // Add delete
            batch.Delete(ent);

            IList<TableResult> results = currentTable.ExecuteBatch(batch);

            Assert.AreEqual(results.Count, 4);

            IEnumerator<TableResult> enumerator = results.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            // delete
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
        }

        [TestMethod]
        [Description("A test to check batch basic functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchBasicOperationsCheck()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchBasicOperationsCheck(payloadFormat);
            }
        }

        private void DoTableBatchBasicOperationsCheck(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            string pk = Guid.NewGuid().ToString();
            TableBatchOperation batch = new TableBatchOperation();

            // Add insert
            DynamicTableEntity ent1 = GenerateRandomEntity(pk);
            TableOperation operation1 = TableOperation.Insert(ent1);
            batch.Add(operation1);

            DynamicTableEntity ent2 = GenerateRandomEntity(pk);
            TableOperation operation2 = TableOperation.Insert(ent2);
            batch.Add(operation2);

            TableOperation[] operationsArray = new TableOperation[2];
            batch.CopyTo(operationsArray, 0);

            Assert.AreEqual(operation1.Entity.RowKey, operationsArray[0].Entity.RowKey);
            Assert.AreEqual(operation1.OperationType, operationsArray[0].OperationType);
            Assert.AreEqual(operation2.Entity.RowKey, operationsArray[1].Entity.RowKey);
            Assert.AreEqual(operation2.OperationType, operationsArray[1].OperationType);

            Assert.AreEqual(0, batch.IndexOf(operation1));
            Assert.AreEqual(1, batch.IndexOf(operation2));

            IEnumerator<TableOperation> enumerator = batch.GetEnumerator();
            int totalCount = 0;
            while (enumerator.MoveNext())
                totalCount++;
            Assert.AreEqual(2, totalCount);

            Assert.IsTrue(batch.Remove(operation2));
            Assert.IsFalse(batch.IsReadOnly);
            TestHelper.ExpectedException<NotSupportedException>(() => batch[0] = operation1,
                    "Setter is not supported for TableBatchOperation");
        }

        [TestMethod]
        [Description("A test to check batch insert functionality when entity already exists")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertFailSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchInsertFailSync(payloadFormat);
            }
        }

        private void DoTableBatchInsertFailSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            currentTable.Execute(TableOperation.Insert(ent));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Insert(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.Conflict, new string[] { "EntityAlreadyExists" }, "The specified entity already exists.", "The specified entity already exists.");
            }
        }

        #endregion

        #region APM
        [TestMethod]
        [Description("A test to check batch insert functionality APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertAPM()
        {
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            for (int m = 0; m < 3; m++)
            {
                AddInsertToBatch(pk, batch);
            }

            // Add insert
            DynamicTableEntity ent = GenerateRandomEntity(pk);

            currentTable.Execute(TableOperation.Insert(ent));

            // Add delete
            batch.Delete(ent);

            IList<TableResult> results = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 4);

            IEnumerator<TableResult> enumerator = results.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            // delete
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
        }

        [TestMethod]
        [Description("A test to check batch insert functionality when entity already exists APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertFailAPM()
        {
            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            currentTable.Execute(TableOperation.Insert(ent));


            TableBatchOperation batch = new TableBatchOperation();
            batch.Insert(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                IList<TableResult> results = null;
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    results = currentTable.EndExecuteBatch(asyncRes);
                }
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.Conflict, new string[] { "EntityAlreadyExists" }, "The specified entity already exists");
            }
        }
        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("A test to check batch insert functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertTask()
        {
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            for (int m = 0; m < 3; m++)
            {
                AddInsertToBatch(pk, batch);
            }

            // Add insert
            DynamicTableEntity ent = GenerateRandomEntity(pk);

            currentTable.ExecuteAsync(TableOperation.Insert(ent)).Wait();

            // Add delete
            batch.Delete(ent);

            IList<TableResult> results = currentTable.ExecuteBatchAsync(batch).Result;

            Assert.AreEqual(results.Count, 4);

            IEnumerator<TableResult> enumerator = results.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            // delete
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
        }

        [TestMethod]
        [Description("A test to check batch insert functionality when entity already exists")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertFailTask()
        {
            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            currentTable.ExecuteAsync(TableOperation.Insert(ent)).Wait();

            TableBatchOperation batch = new TableBatchOperation();
            batch.Insert(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatchAsync(batch, null, opContext).Wait();
                Assert.Fail();
            }
            catch (AggregateException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.Conflict, new string[] { "EntityAlreadyExists" }, "The specified entity already exists");
            }
        }
#endif

        #endregion

        #endregion

        #region Insert Or Merge

        #region Sync
        [TestMethod]
        [Description("TableBatch Insert Or Merge")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrMergeSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchInsertOrMergeSync(payloadFormat);
            }
        }

        private void DoTableBatchInsertOrMergeSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Or Merge with no pre-existing entity
            DynamicTableEntity insertOrMergeEntity = new DynamicTableEntity("insertOrMerge entity", "foo" + format.ToString());
            insertOrMergeEntity.Properties.Add("prop1", new EntityProperty("value1"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrMerge(insertOrMergeEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity mergeEntity = new DynamicTableEntity(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey);
            mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrMerge(mergeEntity);
            currentTable.ExecuteBatch(batch2);

            // Retrieve Entity & Verify Contents
            result = currentTable.Execute(TableOperation.Retrieve(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey));
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(2, retrievedEntity.Properties.Count);

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties["prop1"], retrievedEntity.Properties["prop1"]);
            Assert.AreEqual(mergeEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        #endregion

        #region APM
        [TestMethod]
        [Description("TableBatch Insert Or Merge APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrMergeAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Insert Or Merge with no pre-existing entity
            DynamicTableEntity insertOrMergeEntity = new DynamicTableEntity("insertOrMerge entity", "foo");
            insertOrMergeEntity.Properties.Add("prop1", new EntityProperty("value1"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrMerge(insertOrMergeEntity);

            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                currentTable.EndExecuteBatch(asyncRes);
            }

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity mergeEntity = new DynamicTableEntity(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey);
            mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrMerge(mergeEntity);
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch2, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                currentTable.EndExecuteBatch(asyncRes);
            }

            // Retrieve Entity & Verify Contents
            result = currentTable.Execute(TableOperation.Retrieve(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey));
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(2, retrievedEntity.Properties.Count);

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties["prop1"], retrievedEntity.Properties["prop1"]);
            Assert.AreEqual(mergeEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        #endregion
        #endregion

        #region Insert Or Replace

        #region Sync
        [TestMethod]
        [Description("TableOperation Insert Or Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrReplaceSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchInsertOrReplaceSync(payloadFormat);
            }
        }

        private void DoTableBatchInsertOrReplaceSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Or Replace with no pre-existing entity
            DynamicTableEntity insertOrReplaceEntity = new DynamicTableEntity("insertOrReplace entity", "foo" + format.ToString());
            insertOrReplaceEntity.Properties.Add("prop1", new EntityProperty("value1"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrReplace(insertOrReplaceEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrReplaceEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity replaceEntity = new DynamicTableEntity(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey);
            replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrReplace(replaceEntity);
            currentTable.ExecuteBatch(batch2);

            // Retrieve Entity & Verify Contents
            result = currentTable.Execute(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey));
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(1, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        #endregion

        #region APM
        [TestMethod]
        [Description("TableOperation Insert Or Replace APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrReplaceAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Insert Or Replace with no pre-existing entity
            DynamicTableEntity insertOrReplaceEntity = new DynamicTableEntity("insertOrReplace entity", "foo");
            insertOrReplaceEntity.Properties.Add("prop1", new EntityProperty("value1"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrReplace(insertOrReplaceEntity);

            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                currentTable.EndExecuteBatch(asyncRes);
            }

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrReplaceEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity replaceEntity = new DynamicTableEntity(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey);
            replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrReplace(replaceEntity);
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch2, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                currentTable.EndExecuteBatch(asyncRes);
            }

            // Retrieve Entity & Verify Contents
            result = currentTable.Execute(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey));
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(1, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }
        #endregion
        #endregion

        #region Delete

        #region Sync
        [TestMethod]
        [Description("A test to check batch delete functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchDeleteSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchDeleteSync(payloadFormat);
            }
        }

        private void DoTableBatchDeleteSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            string pk = Guid.NewGuid().ToString();

            // Add insert
            DynamicTableEntity ent = GenerateRandomEntity(pk);
            currentTable.Execute(TableOperation.Insert(ent));

            TableBatchOperation batch = new TableBatchOperation();

            // Add delete
            batch.Delete(ent);

            // success
            IList<TableResult> results = currentTable.ExecuteBatch(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NoContent);

            // fail - not found
            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }

        [TestMethod]
        [Description("A test to check batch delete failure")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchDeleteFailSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchDeleteFailSync(payloadFormat);
            }
        }

        private void DoTableBatchDeleteFailSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            currentTable.Execute(TableOperation.Insert(ent));

            // update entity
            TableResult res = currentTable.Execute(TableOperation.Retrieve(ent.PartitionKey, ent.RowKey));
            DynamicTableEntity retrievedEnt = res.Result as DynamicTableEntity;
            retrievedEnt.Properties.Add("prop", new EntityProperty("var"));
            currentTable.Execute(TableOperation.Replace(retrievedEnt));

            // Attempt to delete with stale etag
            TableBatchOperation batch = new TableBatchOperation();
            batch.Delete(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }
        }
        #endregion

        #region APM
        [TestMethod]
        [Description("A test to check batch delete functionalityAPM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchDeleteAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string pk = Guid.NewGuid().ToString();

            // Add insert
            DynamicTableEntity ent = GenerateRandomEntity(pk);
            currentTable.Execute(TableOperation.Insert(ent));

            TableBatchOperation batch = new TableBatchOperation();

            // Add delete
            batch.Delete(ent);

            // success
            IList<TableResult> results = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NoContent);

            // fail - not found
            OperationContext opContext = new OperationContext();
            try
            {
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    currentTable.EndExecuteBatch(asyncRes);
                }
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }

        [TestMethod]
        [Description("A test to check batch delete failure APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchDeleteFailAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            currentTable.Execute(TableOperation.Insert(ent));

            // update entity
            TableResult result = currentTable.Execute(TableOperation.Retrieve(ent.PartitionKey, ent.RowKey));
            DynamicTableEntity retrievedEnt = result.Result as DynamicTableEntity;
            retrievedEnt.Properties.Add("prop", new EntityProperty("var"));
            currentTable.Execute(TableOperation.Replace(retrievedEnt));

            // Attempt to delete with stale etag
            TableBatchOperation batch = new TableBatchOperation();
            batch.Delete(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    currentTable.EndExecuteBatch(asyncRes);
                }
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                     1,
                     (int)HttpStatusCode.PreconditionFailed,
                     new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                     new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }
        }
        #endregion

        #endregion

        #region Merge

        #region Sync
        [TestMethod]
        [Description("TableBatch Merge Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchMergeSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchMergeSync(payloadFormat);
            }
        }

        private void DoTableBatchMergeSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo" + format.ToString());
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Merge(mergeEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey));

            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(2, retrievedEntity.Properties.Count);
            Assert.AreEqual(baseEntity.Properties["prop1"], retrievedEntity.Properties["prop1"]);
            Assert.AreEqual(mergeEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        [TestMethod]
        [Description("TableBatch Merge Fail Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchMergeFailSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchMergeFailSync(payloadFormat);
            }
        }

        private void DoTableBatchMergeFailSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            string staleEtag = baseEntity.ETag;

            // update entity to rev etag
            baseEntity.Properties["prop1"].StringValue = "updated value";
            currentTable.Execute(TableOperation.Replace(baseEntity));

            OperationContext opContext = new OperationContext();

            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = staleEtag };
                mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Merge(mergeEntity);
                currentTable.ExecuteBatch(batch, null, opContext);

                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            // Delete Entity
            currentTable.Execute(TableOperation.Delete(baseEntity));

            opContext = new OperationContext();

            // try merging with deleted entity
            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
                mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Merge(mergeEntity);
                currentTable.ExecuteBatch(batch, null, opContext);

                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }

        #endregion

        #region APM
        [TestMethod]
        [Description("TableBatch Merge APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchMergeAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Merge(mergeEntity);
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                currentTable.EndExecuteBatch(asyncRes);
            }

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey));

            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(2, retrievedEntity.Properties.Count);
            Assert.AreEqual(baseEntity.Properties["prop1"], retrievedEntity.Properties["prop1"]);
            Assert.AreEqual(mergeEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        [TestMethod]
        [Description("TableBatch Merge Fail APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchMergeFailAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            string staleEtag = baseEntity.ETag;

            // update entity to rev etag
            baseEntity.Properties["prop1"].StringValue = "updated value";
            currentTable.Execute(TableOperation.Replace(baseEntity));

            OperationContext opContext = new OperationContext();

            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = staleEtag };
                mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Merge(mergeEntity);
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    currentTable.EndExecuteBatch(asyncRes);
                }

                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            // Delete Entity
            currentTable.Execute(TableOperation.Delete(baseEntity));

            opContext = new OperationContext();

            // try merging with deleted entity
            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
                mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Merge(mergeEntity);
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    currentTable.EndExecuteBatch(asyncRes);
                }

                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }
        #endregion

        #endregion

        #region Replace

        #region Sync
        [TestMethod]
        [Description("TableBatch ReplaceSync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchReplaceSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchReplaceSync(payloadFormat);
            }
        }

        private void DoTableBatchReplaceSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo" + format.ToString());
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            // ReplaceEntity
            DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Replace(replaceEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replaceEntity.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        [TestMethod]
        [Description("TableBatch Replace Fail Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchReplaceFailSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchReplaceFailSync(payloadFormat);
            }
        }

        private void DoTableBatchReplaceFailSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            string staleEtag = baseEntity.ETag;

            // update entity to rev etag
            baseEntity.Properties["prop1"].StringValue = "updated value";
            currentTable.Execute(TableOperation.Replace(baseEntity));

            OperationContext opContext = new OperationContext();

            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = staleEtag };
                replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Replace(replaceEntity);
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            // Delete Entity
            currentTable.Execute(TableOperation.Delete(baseEntity));

            opContext = new OperationContext();

            // try replacing with deleted entity
            try
            {
                DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
                replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Replace(replaceEntity);
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }

        #endregion

        #region APM
        [TestMethod]
        [Description("TableBatch Replace APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchReplaceAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            // ReplaceEntity
            DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Replace(replaceEntity);
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                currentTable.EndExecuteBatch(asyncRes);
            }

            // Retrieve Entity & Verify Contents
            TableResult result = currentTable.Execute(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replaceEntity.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        [TestMethod]
        [Description("TableBatch Replace Fail APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchReplaceFailAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity));

            string staleEtag = baseEntity.ETag;

            // update entity to rev etag
            baseEntity.Properties["prop1"].StringValue = "updated value";
            currentTable.Execute(TableOperation.Replace(baseEntity));

            OperationContext opContext = new OperationContext();

            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = staleEtag };
                replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Replace(replaceEntity);
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    currentTable.EndExecuteBatch(asyncRes);
                }

                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            // Delete Entity
            currentTable.Execute(TableOperation.Delete(baseEntity));

            opContext = new OperationContext();

            // try replacing with deleted entity
            try
            {
                DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
                replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Replace(replaceEntity);
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = null;
                    currentTable.BeginExecuteBatch(batch, null, opContext, (res) =>
                    {
                        asyncRes = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    currentTable.EndExecuteBatch(asyncRes);
                }

                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }
        #endregion

        #endregion

        #region Batch With All Supported Operations

        #region Sync
        [TestMethod]
        [Description("A test to check batch with all supported operations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchAllSupportedOperationsSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchAllSupportedOperationsSync(payloadFormat);
            }
        }

        private void DoTableBatchAllSupportedOperationsSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            // insert
            batch.Insert(GenerateRandomEntity(pk), true);

            // delete
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.Delete(entity);
            }

            // replace
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.Replace(entity);
            }

            // insert or replace
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.InsertOrReplace(entity);
            }

            // merge
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.Merge(entity);
            }

            // insert or merge
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.InsertOrMerge(entity);
            }

            IList<TableResult> results = currentTable.ExecuteBatch(batch);

            Assert.AreEqual(results.Count, 6);

            IEnumerator<TableResult> enumerator = results.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
        }

        [TestMethod]
        [Description("A test to check batch with all supported operations, with all edm types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchAllEdmtypesSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchAllEdmTypesSync(payloadFormat);
            }
        }

        private void DoTableBatchAllEdmTypesSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            List<DynamicTableEntity> entities = new List<DynamicTableEntity>();

            // insert
            DynamicTableEntity entity = GenerateRandomEntityWithAllEdmtypes(pk);
            entities.Add(entity);
            batch.Insert(entity, true);

            // delete
            entity = GenerateRandomEntityWithAllEdmtypes(pk);
            entities.Add(entity);
            currentTable.Execute(TableOperation.Insert(entity));
            batch.Delete(entity);

            // replace
            entity = GenerateRandomEntityWithAllEdmtypes(pk);
            entities.Add(entity);
            currentTable.Execute(TableOperation.Insert(entity));
            batch.Replace(entity);

            // insert or replace
            entity = GenerateRandomEntityWithAllEdmtypes(pk);
            entities.Add(entity);
            currentTable.Execute(TableOperation.Insert(entity));
            batch.InsertOrReplace(entity);

            // merge
            entity = GenerateRandomEntityWithAllEdmtypes(pk);
            entities.Add(entity);
            currentTable.Execute(TableOperation.Insert(entity));
            batch.Merge(entity);

            // insert or merge
            entity = GenerateRandomEntityWithAllEdmtypes(pk);
            entities.Add(entity);
            currentTable.Execute(TableOperation.Insert(entity));
            batch.InsertOrMerge(entity);

            IList<TableResult> results = currentTable.ExecuteBatch(batch);

            Assert.AreEqual(results.Count, 6);

            IEnumerator<TableResult> enumerator = results.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            AssertDTEEquals(entities[0], (DynamicTableEntity)enumerator.Current.Result, false, true);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);

            TableQuery query = new TableQuery();
            query.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", "eq", pk);

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (partitionKey, rowKey, propName, propValue) => ComplexEntity.ComplexEntityPropertyResolver(partitionKey, rowKey, propName, propValue)
            };

            List<DynamicTableEntity> queriedEntities = currentTable.ExecuteQuery(query, options).ToList();
            Assert.AreEqual(5, queriedEntities.Count);
            foreach (DynamicTableEntity dte in queriedEntities)
            {
                AssertDTEEquals(dte, entities.First(ent => ent.RowKey == dte.RowKey), false, true);
            }
        }

        #endregion

        #region APM
        [TestMethod]
        [Description("A test to check batch with all supported operations APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchAllSupportedOperationsAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            // insert
            batch.Insert(GenerateRandomEntity(pk), true);

            // delete
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.Delete(entity);
            }

            // replace
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.Replace(entity);
            }

            // insert or replace
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.InsertOrReplace(entity);
            }

            // merge
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.Merge(entity);
            }

            // insert or merge
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                currentTable.Execute(TableOperation.Insert(entity));
                batch.InsertOrMerge(entity);
            }

            IList<TableResult> results = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 6);

            IEnumerator<TableResult> enumerator = results.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.Created);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
            enumerator.MoveNext();
            Assert.AreEqual(enumerator.Current.HttpStatusCode, (int)HttpStatusCode.NoContent);
        }
        #endregion
        #endregion

        #region Retrieve

        #region Sync
        [TestMethod]
        [Description("A test to check batch retrieve functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchRetrieveSync(payloadFormat);
            }
        }

        private void DoTableBatchRetrieveSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            string pk = Guid.NewGuid().ToString();

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(pk);

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (partitionKey, rowKey, propName, propValue) => ComplexEntity.ComplexEntityPropertyResolver(partitionKey, rowKey, propName, propValue)
            };

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve(sendEnt.PartitionKey, sendEnt.RowKey);

            // not found
            IList<TableResult> results = currentTable.ExecuteBatch(batch, options, null);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt));

            // Success
            results = currentTable.ExecuteBatch(batch, options, null);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);
            DynamicTableEntity retrievedEntity = results.First().Result as DynamicTableEntity;

            // Validate entity
            Assert.AreEqual(sendEnt["String"], retrievedEntity["String"]);
            Assert.AreEqual(sendEnt["Int64"], retrievedEntity["Int64"]);
            Assert.AreEqual(sendEnt["Int64N"], retrievedEntity["Int64N"]);
            Assert.AreEqual(sendEnt["LongPrimitive"], retrievedEntity["LongPrimitive"]);
            Assert.AreEqual(sendEnt["LongPrimitiveN"], retrievedEntity["LongPrimitiveN"]);
            Assert.AreEqual(sendEnt["Int32"], retrievedEntity["Int32"]);
            Assert.AreEqual(sendEnt["Int32N"], retrievedEntity["Int32N"]);
            Assert.AreEqual(sendEnt["IntegerPrimitive"], retrievedEntity["IntegerPrimitive"]);
            Assert.AreEqual(sendEnt["IntegerPrimitiveN"], retrievedEntity["IntegerPrimitiveN"]);
            Assert.AreEqual(sendEnt["Guid"], retrievedEntity["Guid"]);
            Assert.AreEqual(sendEnt["GuidN"], retrievedEntity["GuidN"]);
            Assert.AreEqual(sendEnt["Double"], retrievedEntity["Double"]);
            Assert.AreEqual(sendEnt["DoubleN"], retrievedEntity["DoubleN"]);
            Assert.AreEqual(sendEnt["DoublePrimitive"], retrievedEntity["DoublePrimitive"]);
            Assert.AreEqual(sendEnt["DoublePrimitiveN"], retrievedEntity["DoublePrimitiveN"]);
            Assert.AreEqual(sendEnt["BinaryPrimitive"], retrievedEntity["BinaryPrimitive"]);
            Assert.AreEqual(sendEnt["Binary"], retrievedEntity["Binary"]);
            Assert.AreEqual(sendEnt["BoolPrimitive"], retrievedEntity["BoolPrimitive"]);
            Assert.AreEqual(sendEnt["BoolPrimitiveN"], retrievedEntity["BoolPrimitiveN"]);
            Assert.AreEqual(sendEnt["Bool"], retrievedEntity["Bool"]);
            Assert.AreEqual(sendEnt["BoolN"], retrievedEntity["BoolN"]);
            Assert.AreEqual(sendEnt["DateTimeOffsetN"], retrievedEntity["DateTimeOffsetN"]);
            Assert.AreEqual(sendEnt["DateTimeOffset"], retrievedEntity["DateTimeOffset"]);
            Assert.AreEqual(sendEnt["DateTime"], retrievedEntity["DateTime"]);
            Assert.AreEqual(sendEnt["DateTimeN"], retrievedEntity["DateTimeN"]);
        }

        [TestMethod]
        [Description("A test to check batch retrieve with resolver")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveWithResolverSync()
        {
            DoTableBatchRetrieveWithResolverSync(TablePayloadFormat.Json);
            DoTableBatchRetrieveWithResolverSync(TablePayloadFormat.JsonNoMetadata);
            DoTableBatchRetrieveWithResolverSync(TablePayloadFormat.JsonFullMetadata);
        }

        private void DoTableBatchRetrieveWithResolverSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(Guid.NewGuid().ToString());

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);
            sendEnt.Properties.Add("foo", new EntityProperty("bar"));

            EntityResolver<string> resolver = (pk, rk, ts, props, etag) => pk + rk + props["foo"].StringValue + props.Count;

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve(sendEnt.PartitionKey, sendEnt.RowKey, resolver);

            // not found
            IList<TableResult> results = currentTable.ExecuteBatch(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt));

            // Success
            results = currentTable.ExecuteBatch(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);
            // Since there are properties in ComplexEntity set to null, we do not receive those from the server. Hence we need to check for non null values.
            Assert.AreEqual((string)results.First().Result, sendEnt.PartitionKey + sendEnt.RowKey + sendEnt["foo"].StringValue + ComplexEntity.NumberOfNonNullProperties);
        }

        [TestMethod]
        [Description("A test to check batch retrieve with ITableEntity")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveWithITableEntitySync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchRetrieveWithITableEntitySync(payloadFormat);
            }
        }

        private void DoTableBatchRetrieveWithITableEntitySync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(Guid.NewGuid().ToString());

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);
            sendEnt.Properties.Add("foo", new EntityProperty("bar"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve<DynamicTableEntity>(sendEnt.PartitionKey, sendEnt.RowKey);

            // not found
            IList<TableResult> results = currentTable.ExecuteBatch(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt));

            // Success
            results = currentTable.ExecuteBatch(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);

            DynamicTableEntity retrievedEntity = results.First().Result as DynamicTableEntity;

            // Validate entity
            Assert.AreEqual(sendEnt["foo"], retrievedEntity["foo"]);
        }
        #endregion

        #region APM
        [TestMethod]
        [Description("A test to check batch retrieve functionality APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string pk = Guid.NewGuid().ToString();

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(pk);

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve(sendEnt.PartitionKey, sendEnt.RowKey);

            // not found
            IList<TableResult> results = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt));

            // Success
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);
            DynamicTableEntity retrievedEntity = results.First().Result as DynamicTableEntity;

            // Validate entity
            Assert.AreEqual(sendEnt["String"], retrievedEntity["String"]);
            Assert.AreEqual(sendEnt["Int64"], retrievedEntity["Int64"]);
            Assert.AreEqual(sendEnt["LongPrimitive"], retrievedEntity["LongPrimitive"]);
            Assert.AreEqual(sendEnt["Int32"], retrievedEntity["Int32"]);
            Assert.AreEqual(sendEnt["IntegerPrimitive"], retrievedEntity["IntegerPrimitive"]);
            Assert.AreEqual(sendEnt["Guid"], retrievedEntity["Guid"]);
            Assert.AreEqual(sendEnt["Double"], retrievedEntity["Double"]);
            Assert.AreEqual(sendEnt["DoublePrimitive"], retrievedEntity["DoublePrimitive"]);
            Assert.AreEqual(sendEnt["BinaryPrimitive"], retrievedEntity["BinaryPrimitive"]);
            Assert.AreEqual(sendEnt["Binary"], retrievedEntity["Binary"]);
            Assert.AreEqual(sendEnt["BoolPrimitive"], retrievedEntity["BoolPrimitive"]);
            Assert.AreEqual(sendEnt["Bool"], retrievedEntity["Bool"]);
            Assert.AreEqual(sendEnt["DateTimeOffsetN"], retrievedEntity["DateTimeOffsetN"]);
            Assert.AreEqual(sendEnt["DateTimeOffset"], retrievedEntity["DateTimeOffset"]);
            Assert.AreEqual(sendEnt["DateTime"], retrievedEntity["DateTime"]);
            Assert.AreEqual(sendEnt["DateTimeN"], retrievedEntity["DateTimeN"]);
        }

        [TestMethod]
        [Description("A test to check batch retrieve with resolver APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveWithResolverAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(Guid.NewGuid().ToString());

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);
            sendEnt.Properties.Add("foo", new EntityProperty("bar"));

            EntityResolver<string> resolver = (pk, rk, ts, props, etag) => pk + rk + props["foo"].StringValue + props.Count;

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve(sendEnt.PartitionKey, sendEnt.RowKey, resolver);

            // not found
            IList<TableResult> results = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt));

            // Success
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);
            // Since there are properties in ComplexEntity set to null, we do not receive those from the server. Hence we need to check for non null values.
            Assert.AreEqual((string)results.First().Result, sendEnt.PartitionKey + sendEnt.RowKey + sendEnt["foo"].StringValue + ComplexEntity.NumberOfNonNullProperties);
        }

        [TestMethod]
        [Description("A test to check batch retrieve with ITableEntity APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveWithITableEntityAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(Guid.NewGuid().ToString());

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);
            sendEnt.Properties.Add("foo", new EntityProperty("bar"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve<DynamicTableEntity>(sendEnt.PartitionKey, sendEnt.RowKey);

            // not found
            IList<TableResult> results = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt));

            // Success
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteBatch(batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                results = currentTable.EndExecuteBatch(asyncRes);
            }
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);

            DynamicTableEntity retrievedEntity = results.First().Result as DynamicTableEntity;
            // Validate entity
            Assert.AreEqual(sendEnt["foo"], retrievedEntity["foo"]);
        }
        #endregion
        #endregion

        #region Empty Keys Test

        [TestMethod]
        [Description("TableBatchOperations with Empty keys")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchOperationsWithEmptyKeys()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchOperationsWithEmptyKeys(payloadFormat);
            }
        }

        private void DoTableBatchOperationsWithEmptyKeys(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = "", RowKey = "" };
            ent.Properties.Add("foo2", new EntityProperty("bar2"));
            ent.Properties.Add("foo", new EntityProperty("bar"));
            TableBatchOperation batch = new TableBatchOperation();
            batch.Insert(ent);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity
            TableBatchOperation retrieveBatch = new TableBatchOperation();
            retrieveBatch.Retrieve(ent.PartitionKey, ent.RowKey);
            TableResult result = currentTable.ExecuteBatch(retrieveBatch).First();

            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);
            Assert.AreEqual(ent.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(ent.Properties["foo"].StringValue, retrievedEntity.Properties["foo"].StringValue);
            Assert.AreEqual(ent.Properties["foo"], retrievedEntity.Properties["foo"]);
            Assert.AreEqual(ent.Properties["foo2"].StringValue, retrievedEntity.Properties["foo2"].StringValue);
            Assert.AreEqual(ent.Properties["foo2"], retrievedEntity.Properties["foo2"]);

            // InsertOrMerge
            DynamicTableEntity insertOrMergeEntity = new DynamicTableEntity(ent.PartitionKey, ent.RowKey);
            insertOrMergeEntity.Properties.Add("foo3", new EntityProperty("value"));
            batch = new TableBatchOperation();
            batch.InsertOrMerge(insertOrMergeEntity);
            currentTable.ExecuteBatch(batch);

            result = currentTable.ExecuteBatch(retrieveBatch).First();
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties["foo3"], retrievedEntity.Properties["foo3"]);

            // InsertOrReplace
            DynamicTableEntity insertOrReplaceEntity = new DynamicTableEntity(ent.PartitionKey, ent.RowKey);
            insertOrReplaceEntity.Properties.Add("prop2", new EntityProperty("otherValue"));
            batch = new TableBatchOperation();
            batch.InsertOrReplace(insertOrReplaceEntity);
            currentTable.ExecuteBatch(batch);

            result = currentTable.ExecuteBatch(retrieveBatch).First();
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(1, retrievedEntity.Properties.Count);
            Assert.AreEqual(insertOrReplaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);

            // Merge
            DynamicTableEntity mergeEntity = new DynamicTableEntity(retrievedEntity.PartitionKey, retrievedEntity.RowKey) { ETag = retrievedEntity.ETag };
            mergeEntity.Properties.Add("mergeProp", new EntityProperty("merged"));
            batch = new TableBatchOperation();
            batch.Merge(mergeEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity & Verify Contents
            result = currentTable.ExecuteBatch(retrieveBatch).First();
            retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(mergeEntity.Properties["mergeProp"], retrievedEntity.Properties["mergeProp"]);

            // Replace
            DynamicTableEntity replaceEntity = new DynamicTableEntity(ent.PartitionKey, ent.RowKey) { ETag = retrievedEntity.ETag };
            replaceEntity.Properties.Add("replaceProp", new EntityProperty("replace"));
            batch = new TableBatchOperation();
            batch.Replace(replaceEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity & Verify Contents
            result = currentTable.ExecuteBatch(retrieveBatch).First();
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replaceEntity.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["replaceProp"], retrievedEntity.Properties["replaceProp"]);

            // Delete Entity
            batch = new TableBatchOperation();
            batch.Delete(retrievedEntity);
            currentTable.ExecuteBatch(batch);

            // Retrieve Entity
            result = currentTable.ExecuteBatch(retrieveBatch).First();
            Assert.IsNull(result.Result);
        }

        #endregion

        #region Bulk insert

        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 1")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsert1()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertAndDeleteBatchWithNEntities(1, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 10")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsert10()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertAndDeleteBatchWithNEntities(10, payloadFormat);
            }
        }


        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 99")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsert99()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertAndDeleteBatchWithNEntities(99, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 100")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsert100()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertAndDeleteBatchWithNEntities(100, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 100")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertPOCO100()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                POCOInsertAndDeleteBatchWithNEntities(100, payloadFormat);
            }
        }

        private void InsertAndDeleteBatchWithNEntities(int n, TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();
            for (int m = 0; m < n; m++)
            {
                batch.Insert(GenerateRandomEntity(pk), true);
            }

            IList<TableResult> results = currentTable.ExecuteBatch(batch);

            TableBatchOperation delBatch = new TableBatchOperation();

            foreach (TableResult res in results)
            {
                delBatch.Delete((ITableEntity)res.Result);
                Assert.IsTrue((res.HttpStatusCode == (int)HttpStatusCode.NoContent) || (res.HttpStatusCode == (int)HttpStatusCode.Created));
            }

            IList<TableResult> delResults = currentTable.ExecuteBatch(delBatch);
            foreach (TableResult res in delResults)
            {
                Assert.AreEqual((int)HttpStatusCode.NoContent, res.HttpStatusCode);
            }
        }

        private void POCOInsertAndDeleteBatchWithNEntities(int n, TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();
            for (int m = 0; m < n; m++)
            {
                var entity = new BaseEntity(pk, Guid.NewGuid().ToString());
                entity.Populate();
                batch.Insert(entity, true);
            }

            IList<TableResult> results = currentTable.ExecuteBatch(batch);

            TableBatchOperation delBatch = new TableBatchOperation();

            foreach (TableResult res in results)
            {
                delBatch.Delete((ITableEntity)res.Result);
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.Created);
            }

            IList<TableResult> delResults = currentTable.ExecuteBatch(delBatch);
            foreach (TableResult res in delResults)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);
            }
        }
        #endregion

        #region Bulk Upsert

        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 1")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrMerge1()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertOrMergeBatchWithNEntities(1, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 10")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrMerge10()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertOrMergeBatchWithNEntities(10, payloadFormat);
            }
        }


        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 99")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrMerge99()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertOrMergeBatchWithNEntities(99, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 100")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrMerge100()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                InsertOrMergeBatchWithNEntities(100, payloadFormat);
            }
        }

        private void InsertOrMergeBatchWithNEntities(int n, TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            string pk = Guid.NewGuid().ToString();

            TableBatchOperation insertBatch = new TableBatchOperation();
            TableBatchOperation mergeBatch = new TableBatchOperation();
            TableBatchOperation delBatch = new TableBatchOperation();

            for (int m = 0; m < n; m++)
            {
                insertBatch.InsertOrMerge(GenerateRandomEntity(pk));
            }

            IList<TableResult> results = currentTable.ExecuteBatch(insertBatch);
            foreach (TableResult res in results)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);

                // update entity and add to merge batch
                DynamicTableEntity ent = res.Result as DynamicTableEntity;
                ent.Properties.Add("foo2", new EntityProperty("bar2"));
                mergeBatch.InsertOrMerge(ent);

            }

            // execute insertOrMerge batch, this time entities exist
            IList<TableResult> mergeResults = currentTable.ExecuteBatch(mergeBatch);

            foreach (TableResult res in mergeResults)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);

                // Add to delete batch
                delBatch.Delete((ITableEntity)res.Result);
            }

            IList<TableResult> delResults = currentTable.ExecuteBatch(delBatch);
            foreach (TableResult res in delResults)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);
            }
        }
        #endregion

        #region Boundary Conditions

        [TestMethod]
        [Description("Ensure that adding null to the batch will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchAddNullShouldThrow()
        {
            TableBatchOperation batch = new TableBatchOperation();
            try
            {
                batch.Add(null);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            try
            {
                batch.Insert(0, null);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        [Description("Ensure that adding multiple queries to the batch will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchAddMultiQueryShouldThrow()
        {
            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve("foo", "bar");
            try
            {
                batch.Retrieve("foo", "bar2");
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        [Description("Ensure that a batch that contains multiple operations on the same entity fails")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchWithMultipleOperationsOnSameEntityShouldFail()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchWithMultipleOperationsOnSameEntityShouldFail(payloadFormat);
            }
        }

        private void DoTableBatchWithMultipleOperationsOnSameEntityShouldFail(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            // Add entity 0
            ITableEntity first = GenerateRandomEntity(pk);
            batch.Insert(first);

            // Add entities 1 - 98
            for (int m = 1; m < 99; m++)
            {
                batch.Insert(GenerateRandomEntity(pk));
            }

            // Insert Duplicate of entity 0
            batch.Insert(first);

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidDuplicateRow" }, new string[] { "99:The batch request contains multiple changes with same row key. An entity can appear only once in a batch request." }, false);
            }
        }

        [TestMethod]
        [Description("Ensure that a batch with entity over 1 MB will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchEntityOver1MBShouldThrow()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchEntityOver1MBShouldThrow(payloadFormat);
            }
        }

        private void DoTableBatchEntityOver1MBShouldThrow(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            DynamicTableEntity ent = GenerateRandomEntity(pk);
            ent.Properties.Add("binary", EntityProperty.GeneratePropertyForByteArray(new byte[1024 * 1024]));
            batch.Insert(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }

            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "EntityTooLarge" }, "The entity is larger than the maximum allowed size (1MB).");
            }
        }

        [TestMethod]
        [Description("Ensure that a batch over 4 MB will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchOver4MBShouldThrow()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchOver4MBShouldThrow(payloadFormat);
            }
        }

        private void DoTableBatchOver4MBShouldThrow(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            for (int m = 0; m < 65; m++)
            {
                DynamicTableEntity ent = GenerateRandomEntity(pk);

                // Maximum Entity size is 64KB
                ent.Properties.Add("binary", EntityProperty.GeneratePropertyForByteArray(new byte[64 * 1024]));
                batch.Insert(ent);
            }

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }

            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.RequestEntityTooLarge, new string[] { "RequestBodyTooLarge" }, "The request body is too large and exceeds the maximum permissible limit.");
            }
        }

        [TestMethod]
        [Description("Ensure that a query and one more operation will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchAddQueryAndOneMoreOperationShouldThrow()
        {
            TableBatchOperation batch = new TableBatchOperation();
            TableOperation operation = TableOperation.Retrieve("foo", "bar");

            try
            {
                batch.Add(operation);
                Assert.IsTrue(batch.Contains(operation));
                batch.Add(TableOperation.Insert(GenerateRandomEntity("foo")));
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            batch.Clear();
            Assert.IsFalse(batch.Contains(operation));

            try
            {
                batch.Add(TableOperation.Insert(GenerateRandomEntity("foo")));
                batch.Add(TableOperation.Retrieve("foo", "bar"));
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            batch.Clear();

            try
            {
                batch.Add(TableOperation.Retrieve("foo", "bar"));
                batch.Insert(0, TableOperation.Insert(GenerateRandomEntity("foo")));

                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            try
            {
                batch.Insert(0, TableOperation.Insert(GenerateRandomEntity("foo")));
                batch.Insert(0, TableOperation.Retrieve("foo", "bar"));

                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }

        }

        [TestMethod]
        [Description("Ensure that empty batch will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchEmptyBatchShouldThrow()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchEmptyBatchShouldThrow(payloadFormat);
            }
        }

        private void DoTableBatchEmptyBatchShouldThrow(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            TestHelper.ExpectedException<InvalidOperationException>(
                () => currentTable.ExecuteBatch(batch),
                "Empty batch operation should fail");
        }

        [TestMethod]
        [Description("Ensure that a given batch only allows entities with the same partitionkey")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchLockToPartitionKey()
        {
            TableBatchOperation batch = new TableBatchOperation();
            batch.Add(TableOperation.Insert(GenerateRandomEntity("foo")));

            try
            {
                batch.Add(TableOperation.Insert(GenerateRandomEntity("foo2")));
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // no op
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            // should reset pk lock
            batch.RemoveAt(0);
            batch.Add(TableOperation.Insert(GenerateRandomEntity("foo2")));

            try
            {
                batch.Add(TableOperation.Insert(GenerateRandomEntity("foo2")));
            }
            catch (ArgumentException)
            {
                Assert.Fail();
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        [Description("Ensure that a batch with an entity property over 255 chars will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchWithPropertyOver255CharsShouldThrow()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchWithPropertyOver255CharsShouldThrow(payloadFormat);
            }
        }

        private void DoTableBatchWithPropertyOver255CharsShouldThrow(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            string propName = new string('a', 256);

            DynamicTableEntity ent = new DynamicTableEntity("foo", "bar");
            ent.Properties.Add(propName, new EntityProperty("propbar"));
            batch.Insert(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteBatch(batch, null, opContext);
                Assert.Fail();
            }

            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "PropertyNameTooLong" }, "The property name exceeds the maximum allowed length (255).");
            }
        }

        [TestMethod]
        [Description("Test a Table batch operation that retried has the correct number of results returned to the user")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchOperationWithRetryHasCorrectNumberOfResults()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableBatchOperationWithRetryHasCorrectNumberOfResults(payloadFormat);
            }
        }

        private void DoTableBatchOperationWithRetryHasCorrectNumberOfResults(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableBatchOperation batch = new TableBatchOperation();
            for (int i = 0; i < 100; i++)
            {
                DynamicTableEntity insertEntity = new DynamicTableEntity("retry test", format.ToString() + i.ToString());
                insertEntity.Properties.Add("prop", new EntityProperty(new byte[20 * 1024]));
                batch.Insert(insertEntity);
            }

            IList<TableResult> results = null;
            TestHelper.ExecuteMethodWithRetry(
                3,
                new[] {
                    // Insert upstream network delay to prevent upload to server @ 1000ms / kb
                    PerformanceBehaviors.InsertUpstreamNetworkDelay(10000,
                                                                    AzureStorageSelectors.TableTraffic().IfHostNameContains(tableClient.Credentials.AccountName),
                                                                    new BehaviorOptions(2)),
                    // After 500 ms return throttle message
                    DelayedActionBehaviors.ExecuteAfter(Actions.ThrottleTableRequest,
                                                            100,
                                                            AzureStorageSelectors.TableTraffic().IfHostNameContains(tableClient.Credentials.AccountName),
                                                            new BehaviorOptions(2))                    
               },
               (options, opContext) => results = currentTable.ExecuteBatch(batch, (TableRequestOptions)options, opContext));

            Assert.AreEqual(batch.Count, results.Count);
        }

        [TestMethod]
        [Description("A test to peform batch insert with batch size of 101. Should fail client-side.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchTooManyOperations()
        {
            TableBatchOperation batch = new TableBatchOperation();
            try
            {
                for (int m = 0; m < 101; m++)
                {
                    batch.Insert(GenerateRandomEntity("testpk"), true);
                }

                currentTable.ExecuteBatch(batch);
                
                Assert.Fail("Batch commands with more than 101 operations should fail.");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(e.Message, SR.BatchExceededMaximumNumberOfOperations);
            }
        }

        #endregion

        #region Secondary

        [TestMethod]
        [Description("A test to check batch retrieve functionality on secondary")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchOnSecondary()
        {
            AssertSecondaryEndpoint();

            CloudTable table = GenerateCloudTableClient().GetTableReference(GenerateRandomTableName());

            TableRequestOptions options = new TableRequestOptions()
            {
                LocationMode = LocationMode.SecondaryOnly,
                RetryPolicy = new NoRetry(),
            };

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve("PartitionKey", "RowKey");

            OperationContext context = new OperationContext();
            table.ExecuteBatch(batch, options, context);
            Assert.AreEqual(StorageLocation.Secondary, context.LastResult.TargetLocation);

            batch = new TableBatchOperation();
            batch.Insert(new DynamicTableEntity("PartitionKey", "RowKey"));

            StorageException e = TestHelper.ExpectedException<StorageException>(
                () => table.ExecuteBatch(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.InsertOrMerge(new DynamicTableEntity("PartitionKey", "RowKey"));

            e = TestHelper.ExpectedException<StorageException>(
                () => table.ExecuteBatch(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.InsertOrReplace(new DynamicTableEntity("PartitionKey", "RowKey"));

            e = TestHelper.ExpectedException<StorageException>(
                () => table.ExecuteBatch(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.Merge(new DynamicTableEntity("PartitionKey", "RowKey") { ETag = "*" });

            e = TestHelper.ExpectedException<StorageException>(
                () => table.ExecuteBatch(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.Replace(new DynamicTableEntity("PartitionKey", "RowKey") { ETag = "*" });

            e = TestHelper.ExpectedException<StorageException>(
                () => table.ExecuteBatch(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.Delete(new DynamicTableEntity("PartitionKey", "RowKey") { ETag = "*" });

            e = TestHelper.ExpectedException<StorageException>(
                () => table.ExecuteBatch(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);
        }
        #endregion

        #region Helpers

        private static void AddInsertToBatch(string pk, TableBatchOperation batch)
        {
            batch.Insert(GenerateRandomEntity(pk), true);
        }

        private static DynamicTableEntity GenerateRandomEntity(string pk)
        {
            DynamicTableEntity ent = new DynamicTableEntity();
            ent.Properties.Add("foo", new EntityProperty("bar"));

            ent.PartitionKey = pk;
            ent.RowKey = Guid.NewGuid().ToString();
            return ent;
        }

        private static DynamicTableEntity GenerateRandomEntityWithAllEdmtypes(string pk)
        {
            DynamicTableEntity ent = new DynamicTableEntity();
            ent.PartitionKey = pk;
            ent.RowKey = Guid.NewGuid().ToString();
            ent.Properties = new ComplexEntity().WriteEntity(null);
            ent.Properties.Add("foo", new EntityProperty("bar"));
            return ent;
        }

        private static void AssertDTEEquals(DynamicTableEntity left, DynamicTableEntity right, bool includeTimestamp, bool ignoreNullColumns)
        {
            Assert.AreEqual(left.PartitionKey, right.PartitionKey);
            Assert.AreEqual(left.RowKey, right.RowKey);
            if (includeTimestamp)
            {
                Assert.AreEqual(left.Timestamp, right.Timestamp);
            }
            IDictionary<string, EntityProperty> leftProperties = left.Properties;
            IDictionary<string, EntityProperty> rightProperties = right.Properties;

            if (ignoreNullColumns)
            {
                leftProperties = leftProperties.Where(kvp => !kvp.Key.Contains("Null")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                rightProperties = rightProperties.Where(kvp => !kvp.Key.Contains("Null")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            Assert.AreEqual(leftProperties.Count, rightProperties.Count);
            
            foreach (string key in leftProperties.Keys)
            {
                try
                {
                    Assert.IsTrue(rightProperties.ContainsKey(key));
                    Assert.AreEqual(left[key], right[key]);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        #endregion
    }
}
