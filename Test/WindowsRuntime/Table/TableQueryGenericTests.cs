﻿// -----------------------------------------------------------------------------------------
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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if FACADE_NETCORE
using System.Threading;
#endif
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableQueryGenericTests : TableTestBase

#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public TableQueryGenericTests()
        {
            MyClassInitialize(null);
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyClassCleanup();
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
            currentTable.CreateIfNotExistsAsync().Wait();

            for (int i = 0; i < 15; i++)
            {
                TableBatchOperation batch = new TableBatchOperation();

                for (int j = 0; j < 100; j++)
                {
                    BaseEntity ent = GenerateRandomEntity("tables_batch_" + i.ToString());
                    ent.RowKey = string.Format("{0:0000}", j);
                    batch.Insert(ent);
                }
#if !FACADE_NETCORE
                currentTable.ExecuteBatchAsync(batch).Wait();
#else
                currentTable.ExecuteBatchAsync(batch, null, null, CancellationToken.None).Wait();
#endif
            }
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            currentTable.DeleteIfExistsAsync().Wait();
        }

        //
        // Use TestInitialize to run code before running each test 
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
#if !FACADE_NETCORE
        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableGenericQueryBasicAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableGenericQueryBasicAsync(payloadFormat);
            }
        }
#endif

        private async Task DoTableGenericQueryBasicAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
#if !FACADE_NETCORE
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));
            TableQuerySegment<BaseEntity> seg = await currentTable.ExecuteQuerySegmentedAsync(query, fullResolver, null);
#else
            TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", "eq", "tables_batch_1"));
            TableQuerySegment<BaseEntity> seg = await currentTable.ExecuteQuerySegmentedAsync<BaseEntity>(query, null, null);
#endif

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
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableGenericQueryWithContinuationAsync(payloadFormat);
            }
        }

        private async Task DoTableGenericQueryWithContinuationAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            OperationContext opContext = new OperationContext();
#if !FACADE_NETCORE
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>();
            TableQuerySegment<BaseEntity> seg = await currentTable.ExecuteQuerySegmentedAsync(query, fullResolver, null, null, opContext);
#else
            TableQuery query = new TableQuery();
            TableQuerySegment<BaseEntity> seg = await currentTable.ExecuteQuerySegmentedAsync<BaseEntity>(query, resolver, null, null, opContext);
#endif

            int count = 0;
            foreach (BaseEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                ent.Validate();
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
#if !FACADE_NETCORE
            seg = await currentTable.ExecuteQuerySegmentedAsync(query, fullResolver, seg.ContinuationToken, null, opContext);
#else
            seg = await currentTable.ExecuteQuerySegmentedAsync<BaseEntity>(query, resolver, seg.ContinuationToken, null, opContext);
#endif

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
            OperationContext opContext = new OperationContext();
            int count = 0;

#if !FACADE_NETCORE
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Where(string.Format("(PartitionKey eq '{0}') and (RowKey ge '{1}')", "tables_batch_1", "0050"));

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query))
#else

            TableQuery query = new TableQuery().Where(string.Format("(PartitionKey eq '{0}') and (RowKey ge '{1}')", "tables_batch_1", "0050"));
            foreach (BaseEntity ent in ExecuteQueryBaseEntity<BaseEntity>(currentTable, query))
#endif
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.RowKey, string.Format("{0:0000}", count + 50));
                ent.Validate();
                count++;
            }

            Assert.AreEqual(count, 50);
        }

#if !FACADE_NETCORE
        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjection()
        {
            DoTableGenericQueryProjection(false);
            DoTableGenericQueryProjection(true);
        }
#endif

        private void DoTableGenericQueryProjection(bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;
#if !FACADE_NETCORE
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "C" });

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query))
#else
            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C" });

            foreach (BaseEntity ent in ExecuteQueryBaseEntity<BaseEntity>(currentTable, query))
#endif
            {
                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);

                if (tableClient.DefaultRequestOptions.ProjectSystemProperties.HasValue)
                {
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.PartitionKey == default(string), "Missing expected " + TableConstants.PartitionKey);
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.RowKey == default(string), "Missing expected " + TableConstants.RowKey);
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.Timestamp == default(DateTimeOffset), "Missing expected " + TableConstants.Timestamp);
                }
            }
        }
#if !FACADE_NETCORE
        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryProjectionSpecifyingSystemProperties()
        {
            DoTableGenericQueryProjectionSpecifyingSystemProperties(false);
            DoTableGenericQueryProjectionSpecifyingSystemProperties(true);
        }
#endif

        private void DoTableGenericQueryProjectionSpecifyingSystemProperties(bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;
#if !FACADE_NETCORE
            TableQuery<BaseEntity> query = new TableQuery<BaseEntity>().Select(new List<string>() { "A", "C", TableConstants.PartitionKey, TableConstants.Timestamp });

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query))
#else
            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C", TableConstants.PartitionKey, TableConstants.Timestamp });

            foreach (BaseEntity ent in ExecuteQueryBaseEntity<BaseEntity>(currentTable, query))
#endif
            {
                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
                Assert.AreNotEqual(default(string), ent.PartitionKey);
                Assert.AreNotEqual(default(DateTimeOffset), ent.Timestamp);

                if (tableClient.DefaultRequestOptions.ProjectSystemProperties.HasValue)
                {
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.RowKey == default(string), "Missing expected " + TableConstants.RowKey);
                }
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
#if !FACADE_NETCORE
            TableQuery<TableEntity> query = new TableQuery<TableEntity>().Select(new List<string>() { "A", "C" });

            foreach (string ent in ExecuteQuery(currentTable, query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue))
            {
                Assert.AreEqual(ent, "ac");
            }

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query, acResolver))
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
#else
            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C" });

            foreach (string ent in ExecuteQuery(currentTable, query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue))
            {
                Assert.AreEqual(ent, "ac");
            }

            foreach (BaseEntity ent in ExecuteQuery(currentTable, query, entityResolver))
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
#endif
        }

        static EntityResolver<BaseEntity> acResolver =
                (pk, rk, ts, prop, etag) =>
                    new BaseEntity(pk, rk)
                    {
                        ETag = etag,
                        Timestamp = ts,
                        A = prop["A"].StringValue,
                        C = prop["C"].StringValue
                    };

        static EntityResolver<BaseEntity> fullResolver = (partitionKey, rowKey, timestamp, properties, etag) =>
        {
            BaseEntity entity = new BaseEntity(partitionKey, rowKey);
            entity.ETag = etag;
            entity.foo = properties["foo"].StringValue;
            entity.A = properties["A"].StringValue;
            entity.B = properties["B"].StringValue;
            entity.C = properties["C"].StringValue;
            entity.D = properties["D"].StringValue;
            entity.E = properties["E"].Int32Value.Value;

            entity.Binary = properties["Binary"].BinaryValue;

            entity.Double = properties["Double"].DoubleValue.Value;
            entity.DoubleEpsilon = properties["DoubleEpsilon"].DoubleValue.Value;
            entity.DoubleNan = properties["DoubleNan"].DoubleValue.Value;
            entity.DoublePositiveInfinity = properties["DoublePositiveInfinity"].DoubleValue.Value;
            entity.DoubleNegativeInfinity = properties["DoubleNegativeInfinity"].DoubleValue.Value;
            entity.DoubleNullWithValue = properties["DoubleNullWithValue"].DoubleValue.Value;
            entity.DoubleNullWithNan = properties["DoubleNullWithNan"].DoubleValue.Value;
            entity.DoubleNullWithPositiveInfinity = properties["DoubleNullWithPositiveInfinity"].DoubleValue.Value;
            entity.DoubleNullWithNegativeInfinity = properties["DoubleNullWithNegativeInfinity"].DoubleValue.Value;

            entity.String = properties["String"].StringValue;

            entity.Int32 = properties["Int32"].Int32Value.Value;
            entity.Int32NullWithValue = properties["Int32NullWithValue"].Int32Value.Value;

            entity.Int64 = properties["Int64"].Int64Value.Value;
            entity.Int64NullWithValue = properties["Int64NullWithValue"].Int64Value.Value;

            entity.DateTime = properties["DateTime"].DateTime.Value;
            entity.DateTimeNullWithValue = properties["DateTimeNullWithValue"].DateTime.Value;
            entity.DateTimeOffset = properties["DateTimeOffset"].DateTime.Value;

            entity.DateTimeOffsetNullWithValue = properties["DateTimeOffsetNullWithValue"].DateTimeOffsetValue.Value;

            entity.Guid = properties["Guid"].GuidValue.Value;
            entity.GuidNullWithValue = properties["GuidNullWithValue"].GuidValue.Value;

            entity.BooleanTrue = properties["BooleanTrue"].BooleanValue.Value;
            entity.BooleanFalse = properties["BooleanFalse"].BooleanValue.Value;
            entity.BooleanNullWithTrue = properties["BooleanNullWithTrue"].BooleanValue.Value;
            entity.BooleanNullWithFalse = properties["BooleanNullWithFalse"].BooleanValue.Value;

            return entity;
        };

        [TestMethod]
        [Description("Basic resolver test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryResolverWithDynamic()
        {
            TableQuery query = new TableQuery().Select(new List<string>() { "A", "C" });
            foreach (string ent in ExecuteQueryWithResolver(currentTable, query, (pk, rk, ts, prop, etag) => prop["A"].StringValue + prop["C"].StringValue))
            {
                Assert.AreEqual(ent, "ac");
            }
            foreach (BaseEntity ent in ExecuteQueryWithResolver(currentTable, query, acResolver))
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);

                Assert.AreEqual(ent.A, "a");
                Assert.IsNull(ent.B);
                Assert.AreEqual(ent.C, "c");
                Assert.IsNull(ent.D);
            }
        }

#if !FACADE_NETCORE
        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableGenericQueryOnSupportedTypesAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableGenericQueryOnSupportedTypesAsync(payloadFormat);
            }
        }
#endif
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

#if !FACADE_NETCORE
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

#else
                await table.ExecuteBatchAsync(batch, null, null, CancellationToken.None);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", "ge", "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", "eq", middleRef.Guid), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", "ge",
                                middleRef.LongPrimitive), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        "ge", middleRef.LongPrimitive), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", "ge",
                                middleRef.Double), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        "ge", middleRef.DoublePrimitive), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", "ge",
                                middleRef.Int32), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        "ge", middleRef.IntegerPrimitive), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", "ge",
                                middleRef.DateTimeOffset), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", "eq", middleRef.Bool), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", "eq", middleRef.BoolPrimitive),
                        50);
#endif

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
        public async Task TableGenericQueryWithInvalidQueryAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableGenericQueryWithInvalidQueryAsync(payloadFormat);
            }
        }

        private async Task DoTableGenericQueryWithInvalidQueryAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
#if !FACADE_NETCORE
            TableQuery<ComplexEntity> query = new TableQuery<ComplexEntity>().Where(string.Format("(PartitionKey ) and (RowKey ge '{1}')", "tables_batch_1", "000050"));
#else
            TableQuery query = new TableQuery().Where(string.Format("(PartitionKey ) and (RowKey ge '{1}')", "tables_batch_1", "000050"));
#endif

            OperationContext opContext = new OperationContext();
            try
            {
#if !FACADE_NETCORE
                await currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext);
#else
                await currentTable.ExecuteQuerySegmentedAsync<ComplexEntity>(query, (pk, rk, tse, prop, etag) => new ComplexEntity(pk, rk), null, null, opContext);
#endif
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidInput" }, "A binary operator with incompatible types was detected. Found operand types 'Edm.String' and 'Edm.Boolean' for operator kind 'And'.");
            }
        }

#endregion

#region Helpers

#if !FACADE_NETCORE
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
#else
        private static List<BaseEntity> ExecuteQueryBaseEntity<T>(CloudTable table, TableQuery query) where T : BaseEntity, new()
        {
            List<BaseEntity> retList = new List<BaseEntity>();

            TableQuerySegment<T> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<T>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync<T>(query, resolver, currSeg != null ? currSeg.ContinuationToken : null, null, null));

                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);
            }

            return retList;
        }

        private static List<ComplexEntity> ExecuteQueryComplexEntity<T>(CloudTable table, TableQuery query) where T : ComplexEntity, new()
        {
            List<ComplexEntity> retList = new List<ComplexEntity>();

            TableQuerySegment<T> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<T>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync<T>(query, (pk, rk, tse, prop, etag) => (T)new ComplexEntity(pk, rk), currSeg != null ? currSeg.ContinuationToken : null, null, null));

                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);
            }

            return retList;
        }
#endif


        private static List<TResult> ExecuteQueryWithResolver<TResult>(CloudTable table, TableQuery query, EntityResolver<TResult> resolver)
        {
            List<TResult> retList = new List<TResult>();

            TableQuerySegment<TResult> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<TResult>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync(query, resolver, currSeg != null ? currSeg.ContinuationToken : null));
                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);

            }

            return retList;
        }

#if !FACADE_NETCORE
        private static List<TResult> ExecuteQuery<T,TResult>(CloudTable table, TableQuery<T> query, EntityResolver<TResult> resolver) where T : ITableEntity, new()
#else
        private static List<TResult> ExecuteQuery<TResult>(CloudTable table, TableQuery query, EntityResolver<TResult> resolver)
#endif
        {
            List<TResult> retList = new List<TResult>();

            TableQuerySegment<TResult> currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment<TResult>> task = Task.Run(() => table.ExecuteQuerySegmentedAsync(query, resolver, currSeg != null ? currSeg.ContinuationToken : null));
                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);
            }

            return retList;
        }

        private static void ExecuteQueryAndAssertResults(CloudTable table, string filter, int expectedResults)
        {
#if !FACADE_NETCORE
            Assert.AreEqual(expectedResults, ExecuteQuery(table, new TableQuery<ComplexEntity>().Where(filter)).Count());
#else
            Assert.AreEqual(expectedResults, ExecuteQueryComplexEntity<ComplexEntity>(table, new TableQuery().Where(filter)).Count());
#endif
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
