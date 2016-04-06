// -----------------------------------------------------------------------------------------
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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableBatchOperationTest : TableTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public TableBatchOperationTest()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif
        #region Locals + Ctors
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
        // public static async Task MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static async Task MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            currentTable.CreateIfNotExistsAsync().Wait();

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
            currentTable.DeleteIfExistsAsync().Wait();
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Insert

        [TestMethod]
        [Description("A test to check batch insert functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsertAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchInsertAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchInsertAsync(TablePayloadFormat format)
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

            await currentTable.ExecuteAsync(TableOperation.Insert(ent));

            // Add delete
            batch.Delete(ent);

            IList<TableResult> results = await currentTable.ExecuteBatchAsync(batch);

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
        public async Task TableBatchInsertFailAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchInsertFailAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchInsertFailAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            await currentTable.ExecuteAsync(TableOperation.Insert(ent));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Insert(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.Conflict, new string[] { "EntityAlreadyExists" }, "The specified entity already exists", "The specified entity already exists");
            }
        }
        #endregion

        #region Insert Or Merge

        [TestMethod]
        [Description("TableBatch Insert Or Merge")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsertOrMergeAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchInsertOrMergeAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchInsertOrMergeAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Or Merge with no pre-existing entity
            DynamicTableEntity insertOrMergeEntity = new DynamicTableEntity("insertOrMerge entity", "foo" + format.ToString());
            insertOrMergeEntity.Properties.Add("prop1", new EntityProperty("value1"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrMerge(insertOrMergeEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = await currentTable.ExecuteAsync(TableOperation.Retrieve(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity mergeEntity = new DynamicTableEntity(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey);
            mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrMerge(mergeEntity);
            await currentTable.ExecuteBatchAsync(batch2);

            // Retrieve Entity & Verify Contents
            result = await currentTable.ExecuteAsync(TableOperation.Retrieve(insertOrMergeEntity.PartitionKey, insertOrMergeEntity.RowKey));
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(2, retrievedEntity.Properties.Count);

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties["prop1"], retrievedEntity.Properties["prop1"]);
            Assert.AreEqual(mergeEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        #endregion

        #region Insert Or Replace

        [TestMethod]
        [Description("TableOperation Insert Or Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsertOrReplaceAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchInsertOrReplaceAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchInsertOrReplaceAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Or Replace with no pre-existing entity
            DynamicTableEntity insertOrReplaceEntity = new DynamicTableEntity("insertOrReplace entity", "foo");
            insertOrReplaceEntity.Properties.Add("prop1", new EntityProperty("value1"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrReplace(insertOrReplaceEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = await currentTable.ExecuteAsync(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrReplaceEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity replaceEntity = new DynamicTableEntity(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey);
            replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrReplace(replaceEntity);
            await currentTable.ExecuteBatchAsync(batch2);

            // Retrieve Entity & Verify Contents
            result = await currentTable.ExecuteAsync(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey));
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(1, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        #endregion

        #region Delete

        [TestMethod]
        [Description("A test to check batch delete functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchDeleteAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchDeleteAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchDeleteAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string pk = Guid.NewGuid().ToString();

            // Add insert
            DynamicTableEntity ent = GenerateRandomEntity(pk);
            await currentTable.ExecuteAsync(TableOperation.Insert(ent));

            TableBatchOperation batch = new TableBatchOperation();

            // Add delete
            batch.Delete(ent);

            // success
            IList<TableResult> results = await currentTable.ExecuteBatchAsync(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NoContent);

            // fail - not found
            OperationContext opContext = new OperationContext();
            try
            {
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
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
        public async Task TableBatchDeleteFailAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchDeleteFailAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchDeleteFailAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            ITableEntity ent = GenerateRandomEntity("foo");

            // add entity
            await currentTable.ExecuteAsync(TableOperation.Insert(ent));

            // update entity
            TableResult res = await currentTable.ExecuteAsync(TableOperation.Retrieve(ent.PartitionKey, ent.RowKey));
            DynamicTableEntity retrievedEnt = res.Result as DynamicTableEntity;
            retrievedEnt.Properties.Add("prop", new EntityProperty("var"));
            await currentTable.ExecuteAsync(TableOperation.Replace(retrievedEnt));

            // Attempt to delete with stale etag
            TableBatchOperation batch = new TableBatchOperation();
            batch.Delete(ent);

            OperationContext opContext = new OperationContext();
            try
            {
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }
        }
        #endregion

        #region Merge

        [TestMethod]
        [Description("TableBatch Merge")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchMergeAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchMergeAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchMergeAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo" + format.ToString());
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            await currentTable.ExecuteAsync(TableOperation.Insert(baseEntity));

            DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Merge(mergeEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = await currentTable.ExecuteAsync(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey));

            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(2, retrievedEntity.Properties.Count);
            Assert.AreEqual(baseEntity.Properties["prop1"], retrievedEntity.Properties["prop1"]);
            Assert.AreEqual(mergeEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        [TestMethod]
        [Description("TableBatch Merge Fail")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchMergeFailAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchMergeFailAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchMergeFailAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            await currentTable.ExecuteAsync(TableOperation.Insert(baseEntity));

            string staleEtag = baseEntity.ETag;

            // update entity to rev etag
            baseEntity.Properties["prop1"].StringValue = "updated value";
            await currentTable.ExecuteAsync(TableOperation.Replace(baseEntity));

            OperationContext opContext = new OperationContext();

            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = staleEtag };
                mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Merge(mergeEntity);
                await currentTable.ExecuteBatchAsync(batch, null, opContext);

                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext,
                    1,
                    (int)HttpStatusCode.PreconditionFailed,
                    new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                    new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            // Delete Entity
            await currentTable.ExecuteAsync(TableOperation.Delete(baseEntity));

            opContext = new OperationContext();

            // try merging with deleted entity
            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity mergeEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
                mergeEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Merge(mergeEntity);
                await currentTable.ExecuteBatchAsync(batch, null, opContext);

                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }
        #endregion

        #region Replace

        [TestMethod]
        [Description("TableBatch Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchReplaceAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchReplaceAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchReplaceAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo" + format.ToString());
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            await currentTable.ExecuteAsync(TableOperation.Insert(baseEntity));

            // ReplaceEntity
            DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Replace(replaceEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity & Verify Contents
            TableResult result = await currentTable.ExecuteAsync(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replaceEntity.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);
        }

        [TestMethod]
        [Description("TableBatch Replace Fail")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchReplaceFailAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchReplaceFailAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchReplaceFailAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("merge test", "foo");
            baseEntity.Properties.Add("prop1", new EntityProperty("value1"));
            await currentTable.ExecuteAsync(TableOperation.Insert(baseEntity));

            string staleEtag = baseEntity.ETag;

            // update entity to rev etag
            baseEntity.Properties["prop1"].StringValue = "updated value";
            await currentTable.ExecuteAsync(TableOperation.Replace(baseEntity));

            OperationContext opContext = new OperationContext();

            try
            {
                // Attempt a merge with stale etag
                DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = staleEtag };
                replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Replace(replaceEntity);
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext,
                     1,
                     (int)HttpStatusCode.PreconditionFailed,
                     new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                     new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            // Delete Entity
            await currentTable.ExecuteAsync(TableOperation.Delete(baseEntity));

            opContext = new OperationContext();

            // try replacing with deleted entity
            try
            {
                DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
                replaceEntity.Properties.Add("prop2", new EntityProperty("value2"));

                TableBatchOperation batch = new TableBatchOperation();
                batch.Replace(replaceEntity);
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }

        #endregion

        #region Batch With All Supported Operations

        [TestMethod]
        [Description("A test to check batch with all supported operations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchAllSupportedOperationsAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchAllSupportedOperationsAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchAllSupportedOperationsAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();

            // insert
            batch.Insert(GenerateRandomEntity(pk), true);

            // delete
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                await currentTable.ExecuteAsync(TableOperation.Insert(entity));
                batch.Delete(entity);
            }

            // replace
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                await currentTable.ExecuteAsync(TableOperation.Insert(entity));
                batch.Replace(entity);
            }

            // insert or replace
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                await currentTable.ExecuteAsync(TableOperation.Insert(entity));
                batch.InsertOrReplace(entity);
            }

            // merge
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                await currentTable.ExecuteAsync(TableOperation.Insert(entity));
                batch.Merge(entity);
            }

            // insert or merge
            {
                DynamicTableEntity entity = GenerateRandomEntity(pk);
                await currentTable.ExecuteAsync(TableOperation.Insert(entity));
                batch.InsertOrMerge(entity);
            }

            IList<TableResult> results = await currentTable.ExecuteBatchAsync(batch);

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

        #region Retrieve

        [TestMethod]
        [Description("A test to check batch retrieve functionality")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchRetrieveAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchRetrieveAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchRetrieveAsync(TablePayloadFormat format)
        {
            if (format == TablePayloadFormat.JsonNoMetadata)
            {
                // Would need additional resolver for no metadata json.
                return;
            }

            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string pk = Guid.NewGuid().ToString();

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(pk);

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve(sendEnt.PartitionKey, sendEnt.RowKey);

            // not found
            IList<TableResult> results = await currentTable.ExecuteBatchAsync(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            await currentTable.ExecuteAsync(TableOperation.Insert(sendEnt));

            // Success
            results = await currentTable.ExecuteBatchAsync(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);
            DynamicTableEntity retrievedEntity = results.First().Result as DynamicTableEntity;

            // Validate entity
            Assert.AreEqual(sendEnt.Properties["String"], retrievedEntity.Properties["String"]);
            Assert.AreEqual(sendEnt.Properties["Int64"], retrievedEntity.Properties["Int64"]);
            Assert.AreEqual(sendEnt.Properties["Int64N"], retrievedEntity.Properties["Int64N"]);
            Assert.AreEqual(sendEnt.Properties["LongPrimitive"], retrievedEntity.Properties["LongPrimitive"]);
            Assert.AreEqual(sendEnt.Properties["LongPrimitiveN"], retrievedEntity.Properties["LongPrimitiveN"]);
            Assert.AreEqual(sendEnt.Properties["Int32"], retrievedEntity.Properties["Int32"]);
            Assert.AreEqual(sendEnt.Properties["Int32N"], retrievedEntity.Properties["Int32N"]);
            Assert.AreEqual(sendEnt.Properties["IntegerPrimitive"], retrievedEntity.Properties["IntegerPrimitive"]);
            Assert.AreEqual(sendEnt.Properties["IntegerPrimitiveN"], retrievedEntity.Properties["IntegerPrimitiveN"]);
            Assert.AreEqual(sendEnt.Properties["Guid"], retrievedEntity.Properties["Guid"]);
            Assert.AreEqual(sendEnt.Properties["GuidN"], retrievedEntity.Properties["GuidN"]);
            Assert.AreEqual(sendEnt.Properties["Double"], retrievedEntity.Properties["Double"]);
            Assert.AreEqual(sendEnt.Properties["DoubleN"], retrievedEntity.Properties["DoubleN"]);
            Assert.AreEqual(sendEnt.Properties["DoublePrimitive"], retrievedEntity.Properties["DoublePrimitive"]);
            Assert.AreEqual(sendEnt.Properties["DoublePrimitiveN"], retrievedEntity.Properties["DoublePrimitiveN"]);
            Assert.AreEqual(sendEnt.Properties["BinaryPrimitive"], retrievedEntity.Properties["BinaryPrimitive"]);
            Assert.AreEqual(sendEnt.Properties["Binary"], retrievedEntity.Properties["Binary"]);
            Assert.AreEqual(sendEnt.Properties["BoolPrimitive"], retrievedEntity.Properties["BoolPrimitive"]);
            Assert.AreEqual(sendEnt.Properties["BoolPrimitiveN"], retrievedEntity.Properties["BoolPrimitiveN"]);
            Assert.AreEqual(sendEnt.Properties["Bool"], retrievedEntity.Properties["Bool"]);
            Assert.AreEqual(sendEnt.Properties["BoolN"], retrievedEntity.Properties["BoolN"]);
            Assert.AreEqual(sendEnt.Properties["DateTimeOffsetN"], retrievedEntity.Properties["DateTimeOffsetN"]);
            Assert.AreEqual(sendEnt.Properties["DateTimeOffset"], retrievedEntity.Properties["DateTimeOffset"]);
            Assert.AreEqual(sendEnt.Properties["DateTime"], retrievedEntity.Properties["DateTime"]);
            Assert.AreEqual(sendEnt.Properties["DateTimeN"], retrievedEntity.Properties["DateTimeN"]);
        }

        [TestMethod]
        [Description("A test to check batch retrieve with resolver")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchRetrieveWithResolverAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchRetrieveWithResolverAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchRetrieveWithResolverAsync(TablePayloadFormat format)
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
            IList<TableResult> results = await currentTable.ExecuteBatchAsync(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            await currentTable.ExecuteAsync(TableOperation.Insert(sendEnt));

            // Success
            results = await currentTable.ExecuteBatchAsync(batch);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);
            // Since there are properties in ComplexEntity set to null, we do not receive those from the server. Hence we need to check for non null values.
            Assert.AreEqual((string)results.First().Result, sendEnt.PartitionKey + sendEnt.RowKey + sendEnt.Properties["foo"].StringValue + ComplexEntity.NumberOfNonNullProperties);
        }
        #endregion

        #region Empty Keys Test

        [TestMethod]
        [Description("TableBatchOperations with Empty keys")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchOperationsWithEmptyKeysAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchOperationsWithEmptyKeysAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchOperationsWithEmptyKeysAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = "", RowKey = "" };
            ent.Properties.Add("foo2", new EntityProperty("bar2"));
            ent.Properties.Add("foo", new EntityProperty("bar"));
            TableBatchOperation batch = new TableBatchOperation();
            batch.Insert(ent);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity
            TableBatchOperation retrieveBatch = new TableBatchOperation();
            retrieveBatch.Retrieve(ent.PartitionKey, ent.RowKey);
            TableResult result = (await currentTable.ExecuteBatchAsync(retrieveBatch)).First();

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
            await currentTable.ExecuteBatchAsync(batch);

            result = (await currentTable.ExecuteBatchAsync(retrieveBatch)).First();
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.Properties["foo3"], retrievedEntity.Properties["foo3"]);

            // InsertOrReplace
            DynamicTableEntity insertOrReplaceEntity = new DynamicTableEntity(ent.PartitionKey, ent.RowKey);
            insertOrReplaceEntity.Properties.Add("prop2", new EntityProperty("otherValue"));
            batch = new TableBatchOperation();
            batch.InsertOrReplace(insertOrReplaceEntity);
            await currentTable.ExecuteBatchAsync(batch);

            result = (await currentTable.ExecuteBatchAsync(retrieveBatch)).First();
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(1, retrievedEntity.Properties.Count);
            Assert.AreEqual(insertOrReplaceEntity.Properties["prop2"], retrievedEntity.Properties["prop2"]);

            // Merge
            DynamicTableEntity mergeEntity = new DynamicTableEntity(retrievedEntity.PartitionKey, retrievedEntity.RowKey) { ETag = retrievedEntity.ETag };
            mergeEntity.Properties.Add("mergeProp", new EntityProperty("merged"));
            batch = new TableBatchOperation();
            batch.Merge(mergeEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity & Verify Contents
            result = (await currentTable.ExecuteBatchAsync(retrieveBatch)).First();
            retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(mergeEntity.Properties["mergeProp"], retrievedEntity.Properties["mergeProp"]);

            // Replace
            DynamicTableEntity replaceEntity = new DynamicTableEntity(ent.PartitionKey, ent.RowKey) { ETag = retrievedEntity.ETag };
            replaceEntity.Properties.Add("replaceProp", new EntityProperty("replace"));
            batch = new TableBatchOperation();
            batch.Replace(replaceEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity & Verify Contents
            result = (await currentTable.ExecuteBatchAsync(retrieveBatch)).First();
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replaceEntity.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["replaceProp"], retrievedEntity.Properties["replaceProp"]);

            // Delete Entity
            batch = new TableBatchOperation();
            batch.Delete(retrievedEntity);
            await currentTable.ExecuteBatchAsync(batch);

            // Retrieve Entity
            result = (await currentTable.ExecuteBatchAsync(retrieveBatch)).First();
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
        public async Task TableBatchInsert1()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertAndDeleteBatchWithNEntities(1, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 10")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsert10()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertAndDeleteBatchWithNEntities(10, payloadFormat);
            }
        }


        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 99")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsert99()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertAndDeleteBatchWithNEntities(99, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch insert and delete with batch size of 100")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsert100()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertAndDeleteBatchWithNEntities(100, payloadFormat);
            }
        }

        private async Task InsertAndDeleteBatchWithNEntities(int n, TablePayloadFormat payloadFormat)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = payloadFormat;
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();
            for (int m = 0; m < n; m++)
            {
                batch.Insert(GenerateRandomEntity(pk), true);
            }

            IList<TableResult> results = await currentTable.ExecuteBatchAsync(batch);

            TableBatchOperation delBatch = new TableBatchOperation();

            foreach (TableResult res in results)
            {
                delBatch.Delete((ITableEntity)res.Result);
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.Created);
            }

            IList<TableResult> delResults = await currentTable.ExecuteBatchAsync(delBatch);
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
        public async Task TableBatchInsertOrMerge1()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertOrMergeBatchWithNEntities(1, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 10")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsertOrMerge10()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertOrMergeBatchWithNEntities(10, payloadFormat);
            }
        }


        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 99")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsertOrMerge99()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertOrMergeBatchWithNEntities(99, payloadFormat);
            }
        }

        [TestMethod]
        [Description("A test to peform batch InsertOrMerge with batch size of 100")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchInsertOrMerge100()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await InsertOrMergeBatchWithNEntities(100, payloadFormat);
            }
        }

        private async Task InsertOrMergeBatchWithNEntities(int n, TablePayloadFormat payloadFormat)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = payloadFormat;
            string pk = Guid.NewGuid().ToString();

            TableBatchOperation insertBatch = new TableBatchOperation();
            TableBatchOperation mergeBatch = new TableBatchOperation();
            TableBatchOperation delBatch = new TableBatchOperation();

            for (int m = 0; m < n; m++)
            {
                insertBatch.InsertOrMerge(GenerateRandomEntity(pk));
            }

            IList<TableResult> results = await currentTable.ExecuteBatchAsync(insertBatch);
            foreach (TableResult res in results)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);

                // update entity and add to merge batch
                DynamicTableEntity ent = res.Result as DynamicTableEntity;
                ent.Properties.Add("foo2", new EntityProperty("bar2"));
                mergeBatch.InsertOrMerge(ent);

            }

            // execute insertOrMerge batch, this time entities exist
            IList<TableResult> mergeResults = await currentTable.ExecuteBatchAsync(mergeBatch);

            foreach (TableResult res in mergeResults)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);

                // Add to delete batch
                delBatch.Delete((ITableEntity)res.Result);
            }

            IList<TableResult> delResults = await currentTable.ExecuteBatchAsync(delBatch);
            foreach (TableResult res in delResults)
            {
                Assert.AreEqual(res.HttpStatusCode, (int)HttpStatusCode.NoContent);
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
        public async Task TableBatchOnSecondaryAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchOnSecondaryAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchOnSecondaryAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
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
            await table.ExecuteBatchAsync(batch, options, context);
            Assert.AreEqual(StorageLocation.Secondary, context.LastResult.TargetLocation);

            batch = new TableBatchOperation();
            batch.Insert(new DynamicTableEntity("PartitionKey", "RowKey"));

            Exception e = await TestHelper.ExpectedExceptionAsync<Exception>(
                async () => await table.ExecuteBatchAsync(batch, options, context),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.InsertOrMerge(new DynamicTableEntity("PartitionKey", "RowKey"));

            e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await table.ExecuteBatchAsync(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.InsertOrReplace(new DynamicTableEntity("PartitionKey", "RowKey"));

            e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await table.ExecuteBatchAsync(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.Merge(new DynamicTableEntity("PartitionKey", "RowKey") { ETag = "*" });

            e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await table.ExecuteBatchAsync(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.Replace(new DynamicTableEntity("PartitionKey", "RowKey") { ETag = "*" });

            e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await table.ExecuteBatchAsync(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);

            batch = new TableBatchOperation();
            batch.Delete(new DynamicTableEntity("PartitionKey", "RowKey") { ETag = "*" });

            e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                async () => await table.ExecuteBatchAsync(batch, options, null),
                "Batch operations other than retrieve should not be sent to secondary");
            Assert.AreEqual(SR.PrimaryOnlyCommand, e.Message);
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
        public async Task TableBatchWithMultipleOperationsOnSameEntityShouldFailAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchWithMultipleOperationsOnSameEntityShouldFailAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchWithMultipleOperationsOnSameEntityShouldFailAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableBatchOperation batch = new TableBatchOperation();
            string pk = Guid.NewGuid().ToString();
            ITableEntity first = GenerateRandomEntity(pk);
            batch.Insert(first);

            for (int m = 0; m < 98; m++)
            {
                batch.Insert(GenerateRandomEntity(pk));
            }

            // Insert Duplicate entity
            batch.Insert(first);

            OperationContext opContext = new OperationContext();
            try
            {
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidInput" }, new string[] { "99:One of the request inputs is not valid." }, false);
            }
        }

        [TestMethod]
        [Description("Ensure that a batch with entity over 1 MB will throw")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchEntityOver1MBShouldThrowAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchEntityOver1MBShouldThrowAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchEntityOver1MBShouldThrowAsync(TablePayloadFormat format)
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
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
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
        public async Task TableBatchOver4MBShouldThrowAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchOver4MBShouldThrowAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchOver4MBShouldThrowAsync(TablePayloadFormat format)
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
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
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
        public void TableBatchAddQueryAndOneMoreOperationShouldThrowSync()
        {
            TableBatchOperation batch = new TableBatchOperation();

            try
            {
                batch.Add(TableOperation.Retrieve("foo", "bar"));
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
        public async Task TableBatchEmptyBatchShouldThrowAsync()
        {
            TableBatchOperation batch = new TableBatchOperation();
            await TestHelper.ExpectedExceptionAsync<InvalidOperationException>(
                async () => await currentTable.ExecuteBatchAsync(batch),
                "Empty batch operation should fail");
        }

        [TestMethod]
        [Description("A test to peform batch insert with batch size of 101. Should fail client-side.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableBatchTooManyOperationsAsync()
        {
            TableBatchOperation batch = new TableBatchOperation();
            try
            {
                for (int m = 0; m < 101; m++)
                {
                    batch.Insert(GenerateRandomEntity("testpk"), true);
                }

                await currentTable.ExecuteBatchAsync(batch);

                Assert.Fail("Batch commands with more than 101 operations should fail.");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(e.Message, SR.BatchExceededMaximumNumberOfOperations);
            }
        }

        [TestMethod]
        [Description("Ensure that a given batch only allows entities with the same partitionkey")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchLockToPartitionKeySync()
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
        public async Task TableBatchWithPropertyOver255CharsShouldThrowAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableBatchWithPropertyOver255CharsShouldThrowAsync(payloadFormat);
            }
        }

        private async Task DoTableBatchWithPropertyOver255CharsShouldThrowAsync(TablePayloadFormat format)
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
                await currentTable.ExecuteBatchAsync(batch, null, opContext);
                Assert.Fail();
            }

            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "PropertyNameTooLong" }, "The property name exceeds the maximum allowed length (255).");
            }
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
        #endregion
    }
}
