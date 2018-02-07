﻿// -----------------------------------------------------------------------------------------
// <copyright file="TableQueryGenericTaskTests.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Table
{
    [TestClass]
    public class TableQueryGenericTaskTests : TableTestBase
    {
        #region Locals + Ctors
        public TableQueryGenericTaskTests()
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
        public static async Task MyClassInitialize(TestContext testContext)
        {
            tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            await currentTable.CreateIfNotExistsAsync();

            for (int i = 0; i < 15; i++)
            {
                TableBatchOperation batch = new TableBatchOperation();

                for (int j = 0; j < 100; j++)
                {
                    BaseEntity ent = GenerateRandomEntity("tables_batch_" + i.ToString());
                    ent.RowKey = string.Format("{0:0000}", j);
                    batch.Insert(ent);
                }

                await currentTable.ExecuteBatchAsync(batch);
            }
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static async Task MyClassCleanup()
        {
            await currentTable.DeleteIfExistsAsync();
        }
        //
        // Use TestInitialize to run code before running each test 
        //[TestInitialize()]
        //public void MyTestInitialize(){}
        //
        // Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        // public void MyTestCleanup(){}

        #endregion

        #region Unit Tests

        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableGenericQueryBasicAsync()
        {
            await DoTableGenericQueryBasicAsync(TablePayloadFormat.Json);
            await DoTableGenericQueryBasicAsync(TablePayloadFormat.JsonNoMetadata);
            await DoTableGenericQueryBasicAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoTableGenericQueryBasicAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment<BaseEntity> seg = await currentTable.ExecuteQuerySegmentedAsync(query, null);

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
        public async Task TableGenericQueryWithContinuationAsync()
        {
            await DoTableGenericQueryWithContinuationAsync(TablePayloadFormat.Json);
            await DoTableGenericQueryWithContinuationAsync(TablePayloadFormat.JsonNoMetadata);
            await DoTableGenericQueryWithContinuationAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoTableGenericQueryWithContinuationAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<BaseEntity> seg = await currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext);

            int count = 0;
            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            seg = await currentTable.ExecuteQuerySegmentedAsync(query, seg.ContinuationToken, null, opContext);

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
        [Description("A test to validate basic table filtering")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithFilter()
        {
            DoTableGenericQueryWithFilter(TablePayloadFormat.Json);
            DoTableGenericQueryWithFilter(TablePayloadFormat.JsonNoMetadata);
            DoTableGenericQueryWithFilter(TablePayloadFormat.JsonFullMetadata);
        }

        private void DoTableGenericQueryWithFilter(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(string.Format("(PartitionKey eq '{0}') and (RowKey ge '{1}')", "tables_batch_1", "0050"));

            OperationContext opContext = new OperationContext();
            int count = 0;

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query))
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.RowKey, string.Format("{0:0000}", count + 50));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(count, 50);
        }

        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjection()
        {
            DoTableGenericQueryProjection(TablePayloadFormat.Json);
            DoTableGenericQueryProjection(TablePayloadFormat.JsonNoMetadata);
            DoTableGenericQueryProjection(TablePayloadFormat.JsonFullMetadata);
        }

        private void DoTableGenericQueryProjection(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "C" });

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query))
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 0);
            }
        }

        [TestMethod]
        [Description("Basic with resolver")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericWithResolver()
        {
            DoTableGenericWithResolver(TablePayloadFormat.Json);
            DoTableGenericWithResolver(TablePayloadFormat.JsonNoMetadata);
            DoTableGenericWithResolver(TablePayloadFormat.JsonFullMetadata);
        }

        private void DoTableGenericWithResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery<TableEntity> query = new TableQuery<TableEntity>().Select(new List<string>() { "A", "C", "E" });

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) => BaseEntity.BaseEntityPropertyResolver(pk, rk, propName, propValue)
            };

            foreach (string ent in ExecuteQuery(currentTable, query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue + prop["E"].Int32Value, options))
            {
                Assert.AreEqual(ent, "ac" + 1234);
            }

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query,
                (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, E = prop["E"].Int32Value.Value, ETag = etag }, options))
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
        public void TableQueryResolverWithDynamic()
        {
            DoTableQueryResolverWithDynamic(TablePayloadFormat.Json);
            DoTableQueryResolverWithDynamic(TablePayloadFormat.JsonNoMetadata);
            DoTableQueryResolverWithDynamic(TablePayloadFormat.JsonFullMetadata);
        }

        private void DoTableQueryResolverWithDynamic(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableRequestOptions options = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) => BaseEntity.BaseEntityPropertyResolver(pk, rk, propName, propValue)
            };

            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C", "E" });
            foreach (string ent in ExecuteQueryWithResolver(currentTable, query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue + prop["E"].Int32Value, options))
            {
                Assert.AreEqual(ent, "ac" + 1234);
            }
            foreach (BaseEntity ent in ExecuteQueryWithResolver(currentTable, query,
                            (pk, rk, ts, prop, etag) => new BaseEntity() { PartitionKey = pk, RowKey = rk, Timestamp = ts, A = prop["A"].StringValue, C = prop["C"].StringValue, E = prop["E"].Int32Value.Value, ETag = etag }, options))
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreEqual(ent.E, 1234);
            }
        }

        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableGenericQueryOnSupportedTypesAsync()
        {
            await DoTableGenericQueryOnSupportedTypesAsync(TablePayloadFormat.Json);
            await DoTableGenericQueryOnSupportedTypesAsync(TablePayloadFormat.JsonNoMetadata);
            await DoTableGenericQueryOnSupportedTypesAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoTableGenericQueryOnSupportedTypesAsync(TablePayloadFormat format)
        {
            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.PayloadFormat = format;

            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            await table.CreateAsync();

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
                    await Task.Delay(100);
                }

                await table.ExecuteBatchAsync(batch);

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
                table.DeleteIfExistsAsync().Wait();
            }
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
            DoTableGenericQueryWithInvalidTakeCount(TablePayloadFormat.Json);
            DoTableGenericQueryWithInvalidTakeCount(TablePayloadFormat.JsonNoMetadata);
            DoTableGenericQueryWithInvalidTakeCount(TablePayloadFormat.JsonFullMetadata);
        }

        private void DoTableGenericQueryWithInvalidTakeCount(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
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
        public async Task TableGenericQueryWithInvalidQuery()
        {
            await DoTableGenericQueryWithInvalidQuery(TablePayloadFormat.Json);
            await DoTableGenericQueryWithInvalidQuery(TablePayloadFormat.JsonNoMetadata);
            await DoTableGenericQueryWithInvalidQuery(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoTableGenericQueryWithInvalidQuery(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Where(string.Format("(PartitionKey ) and (RowKey ge '{1}')", "tables_batch_1", "000050"));

            OperationContext opContext = new OperationContext();
            try
            {
                await currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidInput" }, "One of the request inputs is not valid.");
            }
        }

        #endregion

        #region Helpers

        private static List<T> ExecuteQuery<T>(CloudTable table, TableQuery<T> query) where T: ITableEntity, new()
        {
            List<T> retList = new List<T>();

            TableQuerySegment<T> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<T>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync(query, currSeg != null ? currSeg.ContinuationToken : null));
                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);
            }

            return retList;
        }

        private static List<TResult> ExecuteQueryWithResolver<TResult>(CloudTable table, TableQuery query, EntityResolver<TResult> resolver, TableRequestOptions options)
        {
            List<TResult> retList = new List<TResult>();

            TableQuerySegment<TResult> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<TResult>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync(query, resolver, currSeg != null ? currSeg.ContinuationToken : null, options, null));
                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);

            }

            return retList;
        }

        private static List<TResult> ExecuteQuery<T,TResult>(CloudTable table, TableQuery<T> query, EntityResolver<TResult> resolver, TableRequestOptions options) where T : ITableEntity, new()
        {
            List<TResult> retList = new List<TResult>();

            TableQuerySegment<TResult> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<TResult>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync(query, resolver, currSeg != null ? currSeg.ContinuationToken : null, options, null));
                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);
            }

            return retList;
        }

        private static void ExecuteQueryAndAssertResults(CloudTable table, string filter, int expectedResults)
        {
            Assert.AreEqual(expectedResults, ExecuteQuery(table, new TableQuery<ComplexEntity>().Where(filter)).Count());
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
