// -----------------------------------------------------------------------------------------
// <copyright file="CancellationUnitTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Test.Network;
using Microsoft.WindowsAzure.Test.Network.Behaviors;
using System;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableManglerTests : TableTestBase
    {
        #region Locals + Ctors
        public TableManglerTests()
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
            CloudTableClient tableClient = GenerateCloudTableClient();
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

        #region Table Operation

        [TestMethod]
        [Description("TableIngressEgressTableOperation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableIngressEgressTableOperation()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            DynamicTableEntity insertEntity = new DynamicTableEntity("insert test", "foo");

            for (int m = 0; m < 20; m++)
            {
                insertEntity.Properties.Add("prop" + m.ToString(), new EntityProperty(new byte[50 * 1024]));
            }

            // APM
            TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(currentTable.Uri.ToString()), () =>
            {
                OperationContext opContext = new OperationContext();
                currentTable.EndExecute(currentTable.BeginExecute(TableOperation.InsertOrMerge(insertEntity), new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext, null, null));
                return opContext.LastResult;
            });

            // Sync
            TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(currentTable.Uri.ToString()), () =>
            {
                OperationContext opContext = new OperationContext();
                currentTable.Execute(TableOperation.InsertOrMerge(insertEntity), new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                return opContext.LastResult;
            });
        }

        #endregion

        #region Batch Operation

        [TestMethod]
        [Description("TableIngressEgressBatch")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableIngressEgressBatch()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableBatchOperation batch = new TableBatchOperation();

            for (int m = 0; m < 100; m++)
            {
                // Insert Entity
                DynamicTableEntity insertEntity = new DynamicTableEntity("insert test", m.ToString());
                insertEntity.Properties.Add("prop" + m.ToString(), new EntityProperty(new byte[30 * 1024]));
                batch.InsertOrMerge(insertEntity);
            }

            // APM
            TestHelper.ValidateIngressEgress(Selectors.IfUrlContains("$batch"), () =>
            {
                OperationContext opContext = new OperationContext();
                currentTable.EndExecuteBatch(currentTable.BeginExecuteBatch(batch, new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext, null, null));
                return opContext.LastResult;
            });

            // SYNC
            TestHelper.ValidateIngressEgress(Selectors.IfUrlContains("$batch"), () =>
            {
                OperationContext opContext = new OperationContext();
                currentTable.ExecuteBatch(batch, new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                return opContext.LastResult;
            });
        }

        #endregion

        #region TableQuery

        [Ignore]
        [TestMethod]
        [Description("TableIngressEgressQuery")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableIngressEgressQuery()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableBatchOperation batch = new TableBatchOperation();

            for (int m = 0; m < 100; m++)
            {
                // Insert Entity
                DynamicTableEntity insertEntity = new DynamicTableEntity("insert test", m.ToString());
                insertEntity.Properties.Add("prop" + m.ToString(), new EntityProperty(new byte[30 * 1024]));
                batch.Insert(insertEntity, true);
            }

            currentTable.ExecuteBatch(batch);
            TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "insert test"));

            // APM
            TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(currentTable.Uri.ToString()), () =>
            {
                OperationContext opContext = new OperationContext();
                currentTable.EndExecuteQuerySegmented(currentTable.BeginExecuteQuerySegmented(query, null, new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext, null, null));
                return opContext.LastResult;
            });

            // SYNC
            TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(currentTable.Uri.ToString()), () =>
            {
                OperationContext opContext = new OperationContext();
                currentTable.ExecuteQuerySegmented(query, null, new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                return opContext.LastResult;
            });
        }

        #endregion

        #region ACLs

        [TestMethod]
        [Description("TableIngressEgressACLs")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableIngressEgressACLs()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            CloudTable tbl = tableClient.GetTableReference(GenerateRandomTableName());
            tbl.CreateIfNotExists();

            try
            {
                TablePermissions perms = new TablePermissions();

                // Add a policy, check setting and getting.
                perms.SharedAccessPolicies.Add(Guid.NewGuid().ToString(), new SharedAccessTablePolicy
                {
                    Permissions = SharedAccessTablePermissions.Query,
                    SharedAccessStartTime = DateTimeOffset.Now - TimeSpan.FromHours(1),
                    SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(1)
                });

                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(tbl.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    tbl.EndSetPermissions(tbl.BeginSetPermissions(perms, new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext, null, null));
                    return opContext.LastResult;
                });

                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(tbl.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    tbl.EndGetPermissions(tbl.BeginGetPermissions(new TableRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext, null, null));
                    return opContext.LastResult;
                });
            }
            finally
            {
                tbl.DeleteIfExists();
            }
        }
        #endregion
    }
}
