// -----------------------------------------------------------------------------------------
// <copyright file="TableSasUnitTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableSasUnitTests : TableTestBase
    {
        #region Locals + Ctors
        public TableSasUnitTests()
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
        //
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

        #region Constructor Tests

        [TestMethod]
        [Description("Test TableSas via various constructors")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSASConstructors()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));
            try
            {
                table.Create();

                table.Execute(TableOperation.Insert(new BaseEntity("PK", "RK")));

                // Prepare SAS authentication with full permissions
                string sasToken = table.GetSharedAccessSignature(
                    new SharedAccessTablePolicy
                    {
                        Permissions = SharedAccessTablePermissions.Add | SharedAccessTablePermissions.Delete | SharedAccessTablePermissions.Query,
                        SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(30)
                    },
                    null /* accessPolicyIdentifier */,
                    null /* startPk */,
                    null /* startRk */,
                    null /* endPk */,
                    null /* endRk */);

                CloudStorageAccount sasAccount;
                StorageCredentials sasCreds;
                CloudTableClient sasClient;
                CloudTable sasTable;
                Uri baseUri = new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint);

                // SAS via connection string parse
                sasAccount = CloudStorageAccount.Parse(string.Format("TableEndpoint={0};SharedAccessSignature={1}", baseUri.AbsoluteUri, sasToken));
                sasClient = sasAccount.CreateCloudTableClient();
                sasTable = sasClient.GetTableReference(table.Name);

                Assert.AreEqual(1, sasTable.ExecuteQuery(new TableQuery<BaseEntity>()).Count());

                // SAS via account constructor
                sasCreds = new StorageCredentials(sasToken);
                sasAccount = new CloudStorageAccount(sasCreds, null, null, baseUri, null);
                sasClient = sasAccount.CreateCloudTableClient();
                sasTable = sasClient.GetTableReference(table.Name);
                Assert.AreEqual(1, sasTable.ExecuteQuery(new TableQuery<BaseEntity>()).Count());

                // SAS via client constructor URI + Creds
                sasCreds = new StorageCredentials(sasToken);
                sasClient = new CloudTableClient(baseUri, sasCreds);
                sasTable = sasClient.GetTableReference(table.Name);
                Assert.AreEqual(1, sasTable.ExecuteQuery(new TableQuery<BaseEntity>()).Count());

                // SAS via CloudTable constructor Uri + Client
                sasCreds = new StorageCredentials(sasToken);
                sasTable = new CloudTable(table.Uri, tableClient.Credentials);
                sasClient = sasTable.ServiceClient;
                Assert.AreEqual(1, sasTable.ExecuteQuery(new TableQuery<BaseEntity>()).Count());
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        #endregion

        #region Permissions

        [TestMethod]
        [Description("Tests setting and getting table permissions")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSetGetPermissions()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                table.Execute(TableOperation.Insert(new BaseEntity("PK", "RK")));

                TablePermissions expectedPermissions = new TablePermissions();
                TablePermissions testPermissions = table.GetPermissions();

                AssertPermissionsEqual(expectedPermissions, testPermissions);

                // Add a policy, check setting and getting.
                expectedPermissions.SharedAccessPolicies.Add(Guid.NewGuid().ToString(), new SharedAccessTablePolicy
                {
                    Permissions = SharedAccessTablePermissions.Query,
                    SharedAccessStartTime = DateTimeOffset.Now - TimeSpan.FromHours(1),
                    SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(1)
                });

                table.SetPermissions(expectedPermissions);
                Thread.Sleep(30 * 1000);
                testPermissions = table.GetPermissions();
                AssertPermissionsEqual(expectedPermissions, testPermissions);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }


        [TestMethod]
        [Description("Tests Null Access Policy")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSASNullAccessPolicy()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                table.Execute(TableOperation.Insert(new BaseEntity("PK", "RK")));

                TablePermissions expectedPermissions = new TablePermissions();

                // Add a policy
                expectedPermissions.SharedAccessPolicies.Add(Guid.NewGuid().ToString(), new SharedAccessTablePolicy
                {
                    Permissions = SharedAccessTablePermissions.Query | SharedAccessTablePermissions.Add,
                    SharedAccessStartTime = DateTimeOffset.Now - TimeSpan.FromHours(1),
                    SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(1)
                });

                table.SetPermissions(expectedPermissions);
                Thread.Sleep(30 * 1000);

                // Generate the sasToken the user should use
                string sasToken = table.GetSharedAccessSignature(null, expectedPermissions.SharedAccessPolicies.First().Key, "AAAA", null, "AAAA", null);

                CloudTable sasTable = new CloudTable(table.Uri, new StorageCredentials(sasToken));

                sasTable.Execute(TableOperation.Insert(new DynamicTableEntity("AAAA", "foo")));

                TableResult result = sasTable.Execute(TableOperation.Retrieve("AAAA", "foo"));

                Assert.IsNotNull(result.Result);

                // revoke table permissions
                table.SetPermissions(new TablePermissions());
                Thread.Sleep(30 * 1000);

                OperationContext opContext = new OperationContext();
                try
                {
                    sasTable.Execute(TableOperation.Insert(new DynamicTableEntity("AAAA", "foo2")), null, opContext);
                    Assert.Fail();
                }
                catch (Exception)
                {
                    Assert.AreEqual(opContext.LastResult.HttpStatusCode, (int)HttpStatusCode.Forbidden);
                }

                opContext = new OperationContext();
                try
                {
                    result = sasTable.Execute(TableOperation.Retrieve("AAAA", "foo"), null, opContext);
                    Assert.Fail();
                }
                catch (Exception)
                {
                    Assert.AreEqual(opContext.LastResult.HttpStatusCode, (int)HttpStatusCode.Forbidden);
                }
            }
            finally
            {
                table.DeleteIfExists();
            }
        }
        #endregion

        #region SAS Operations

        [TestMethod]
        [Description("Tests table SAS with query permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasQueryTestSync()
        {
            TestTableSas(SharedAccessTablePermissions.Query);
        }

        [TestMethod]
        [Description("Tests table SAS with delete permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasDeleteTestSync()
        {
            TestTableSas(SharedAccessTablePermissions.Delete);
        }

        [TestMethod]
        [Description("Tests table SAS with process and update permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasUpdateTestSync()
        {
            TestTableSas(SharedAccessTablePermissions.Update);
        }

        [TestMethod]
        [Description("Tests table SAS with add permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasAddTestSync()
        {
            TestTableSas(SharedAccessTablePermissions.Add);
        }

        [TestMethod]
        [Description("Tests table SAS with full permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasFullTestSync()
        {
            TestTableSas(SharedAccessTablePermissions.Query | SharedAccessTablePermissions.Delete | SharedAccessTablePermissions.Update | SharedAccessTablePermissions.Add);
        }

        /// <summary>
        /// Tests table access permissions with SAS, using a stored policy and using permissions on the URI.
        /// Various table range constraints are tested.
        /// </summary>
        /// <param name="accessPermissions">The permissions to test.</param>
        internal void TestTableSas(SharedAccessTablePermissions accessPermissions)
        {
            string startPk = "M";
            string startRk = "F";
            string endPk = "S";
            string endRk = "T";

            // No ranges specified
            TestTableSasWithRange(accessPermissions, null, null, null, null);

            // All ranges specified
            TestTableSasWithRange(accessPermissions, startPk, startRk, endPk, endRk);

            // StartPk & StartRK specified
            TestTableSasWithRange(accessPermissions, startPk, startRk, null, null);

            // StartPk specified
            TestTableSasWithRange(accessPermissions, startPk, null, null, null);

            // EndPk & EndRK specified
            TestTableSasWithRange(accessPermissions, null, null, endPk, endRk);

            // EndPk specified
            TestTableSasWithRange(accessPermissions, null, null, endPk, null);

            // StartPk and EndPk specified
            TestTableSasWithRange(accessPermissions, startPk, null, endPk, null);

            // StartRk and StartRK and EndPk specified
            TestTableSasWithRange(accessPermissions, startPk, startRk, endPk, null);

            // StartRk and EndPK and EndPk specified
            TestTableSasWithRange(accessPermissions, startPk, null, endPk, endRk);
        }

        /// <summary>
        /// Tests table access permissions with SAS, using a stored policy and using permissions on the URI.
        /// </summary>
        /// <param name="accessPermissions">The permissions to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        internal void TestTableSasWithRange(
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                // Set up a policy
                string identifier = Guid.NewGuid().ToString();
                TablePermissions permissions = new TablePermissions();
                permissions.SharedAccessPolicies.Add(identifier, new SharedAccessTablePolicy
                {
                    Permissions = accessPermissions,
                    SharedAccessExpiryTime = DateTimeOffset.Now.AddDays(1)
                });

                table.SetPermissions(permissions);
                Thread.Sleep(30 * 1000);

                // Prepare SAS authentication using access identifier
                string sasString = table.GetSharedAccessSignature(new SharedAccessTablePolicy(), identifier, startPk, startRk, endPk, endRk);
                CloudTableClient identifierSasClient = new CloudTableClient(tableClient.BaseUri, new StorageCredentials(sasString));

                // Prepare SAS authentication using explicit policy
                sasString = table.GetSharedAccessSignature(
                                        new SharedAccessTablePolicy
                                        {
                                            Permissions = accessPermissions,
                                            SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(30)
                                        },
                                        null,
                                        startPk,
                                        startRk,
                                        endPk,
                                        endRk);

                CloudTableClient explicitSasClient = new CloudTableClient(tableClient.BaseUri, new StorageCredentials(sasString));

                // Point query
                TestPointQuery(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestPointQuery(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Add row
                TestAdd(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestAdd(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Update row (merge)
                TestUpdateMerge(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestUpdateMerge(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Update row (replace)
                TestUpdateReplace(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestUpdateReplace(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Delete row
                TestDelete(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestDelete(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Upsert row (merge)
                TestUpsertMerge(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestUpsertMerge(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Upsert row (replace)
                TestUpsertReplace(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                TestUpsertReplace(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }


        /// <summary>
        /// Test point queries entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestPointQuery(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            bool expectSuccess = ((accessPermissions & SharedAccessTablePermissions.Query) != 0);

            Action<BaseEntity, OperationContext> queryDelegate = (tableEntity, ctx) =>
            {
                TableResult retrieveResult = testClient.GetTableReference(tableName).Execute(TableOperation.Retrieve<BaseEntity>(tableEntity.PartitionKey, tableEntity.RowKey), null, ctx);

                if (expectSuccess)
                {
                    Assert.IsNotNull(retrieveResult.Result);
                }
                else
                {
                    Assert.AreEqual(ctx.LastResult.HttpStatusCode, (int)HttpStatusCode.OK);
                }
            };

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                queryDelegate,
                "point query",
                expectSuccess,
                expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotFound, 
                false,
                expectSuccess);
        }


        /// <summary>
        /// Test update (merge) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestUpdateMerge(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> updateDelegate = (tableEntity, ctx) =>
            {
                // Merge entity
                tableEntity.A = "10";
                tableEntity.ETag = "*";

                testClient.GetTableReference(tableName).Execute(TableOperation.Merge(tableEntity), null, ctx);
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Update) != 0;

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                updateDelegate,
                "update merge",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test update (replace) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestUpdateReplace(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> updateDelegate = (tableEntity, ctx) =>
            {
                // replace entity
                tableEntity.A = "20";
                tableEntity.ETag = "*";

                testClient.GetTableReference(tableName).Execute(TableOperation.Replace(tableEntity), null, ctx);
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Update) != 0;

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                updateDelegate,
                "update replace",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test adding entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestAdd(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> addDelegate = (tableEntity, ctx) =>
            {
                // insert entity
                tableEntity.A = "10";

                testClient.GetTableReference(tableName).Execute(TableOperation.Insert(tableEntity), null, ctx);
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Add) != 0;

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                addDelegate,
                "add",
                expectSuccess,
                expectSuccess ? HttpStatusCode.Created : HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test deleting entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestDelete(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> deleteDelegate = (tableEntity, ctx) =>
            {
                // delete entity
                tableEntity.A = "10";
                tableEntity.ETag = "*";

                testClient.GetTableReference(tableName).Execute(TableOperation.Delete(tableEntity), null, ctx);
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Delete) != 0;

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                deleteDelegate,
                "delete",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test upsert (insert or merge) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestUpsertMerge(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> upsertDelegate = (tableEntity, ctx) =>
            {
                // insert or merge entity
                tableEntity.A = "10";

                testClient.GetTableReference(tableName).Execute(TableOperation.InsertOrMerge(tableEntity), null, ctx);
            };

            SharedAccessTablePermissions upsertPermissions = (SharedAccessTablePermissions.Update | SharedAccessTablePermissions.Add);
            bool expectSuccess = (accessPermissions & upsertPermissions) == upsertPermissions;

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                upsertDelegate,
                "upsert merge",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test upsert (insert or replace) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private void TestUpsertReplace(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> upsertDelegate = (tableEntity, ctx) =>
            {
                // insert or replace entity
                tableEntity.A = "10";

                testClient.GetTableReference(tableName).Execute(TableOperation.InsertOrReplace(tableEntity), null, ctx);
            };

            SharedAccessTablePermissions upsertPermissions = (SharedAccessTablePermissions.Update | SharedAccessTablePermissions.Add);
            bool expectSuccess = (accessPermissions & upsertPermissions) == upsertPermissions;

            // Perform test
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                upsertDelegate,
                "upsert replace",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test a table operation on entities inside and outside the given range.
        /// </summary>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        /// <param name="runOperationDelegate">A delegate with the table operation to test.</param>
        /// <param name="opName">The name of the operation being tested.</param>
        /// <param name="expectSuccess">Whether the operation should succeed on entities within the range.</param>
        private void TestOperationWithRange(
            string tableName,
            string startPk,
            string startRk,
            string endPk,
            string endRk,
            Action<BaseEntity, OperationContext> runOperationDelegate,
            string opName,
            bool expectSuccess,
            HttpStatusCode expectedStatusCode)
        {
            TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                runOperationDelegate,
                opName,
                expectSuccess,
                expectedStatusCode,
                false /* isRangeQuery */);
        }

        /// <summary>
        /// Test a table operation on entities inside and outside the given range.
        /// </summary>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        /// <param name="runOperationDelegate">A delegate with the table operation to test.</param>
        /// <param name="opName">The name of the operation being tested.</param>
        /// <param name="expectSuccess">Whether the operation should succeed on entities within the range.</param>
        /// <param name="expectedStatusCode">The status code expected for the response.</param>
        /// <param name="isRangeQuery">Specifies if the operation is a range query.</param>
        private void TestOperationWithRange(
            string tableName,
            string startPk,
            string startRk,
            string endPk,
            string endRk,
            Action<BaseEntity, OperationContext> runOperationDelegate,
            string opName,
            bool expectSuccess,
            HttpStatusCode expectedStatusCode,
            bool isRangeQuery)
        {
                        TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                runOperationDelegate,
                opName,
                expectSuccess,
                expectedStatusCode,
                isRangeQuery,
                false /* isPointQuery */);
        }

        /// <summary>
        /// Test a table operation on entities inside and outside the given range.
        /// </summary>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        /// <param name="runOperationDelegate">A delegate with the table operation to test.</param>
        /// <param name="opName">The name of the operation being tested.</param>
        /// <param name="expectSuccess">Whether the operation should succeed on entities within the range.</param>
        /// <param name="expectedStatusCode">The status code expected for the response.</param>
        /// <param name="isRangeQuery">Specifies if the operation is a range query.</param>
        /// <param name="isPointQuery">Specifies if the operation is a point query.</param>
        private void TestOperationWithRange(
            string tableName,
            string startPk,
            string startRk,
            string endPk,
            string endRk,
            Action<BaseEntity, OperationContext> runOperationDelegate,
            string opName,
            bool expectSuccess,
            HttpStatusCode expectedStatusCode,
            bool isRangeQuery,
            bool isPointQuery)
        {
            CloudTableClient referenceClient = GenerateCloudTableClient();

            string partitionKey = startPk ?? endPk ?? "M";
            string rowKey = startRk ?? endRk ?? "S";

            // if we expect a success for creation - avoid inserting duplicate entities
            BaseEntity tableEntity = new BaseEntity(partitionKey, rowKey);
            if (expectedStatusCode == HttpStatusCode.Created)
            {
                try
                {
                    tableEntity.ETag = "*";
                    referenceClient.GetTableReference(tableName).Execute(TableOperation.Delete(tableEntity));
                }
                catch (Exception)
                {
                }
            }
            else
            {
                // only for add we should not be adding the entity
                referenceClient.GetTableReference(tableName).Execute(TableOperation.InsertOrReplace(tableEntity));
            }

            if (expectSuccess)
            {
                runOperationDelegate(tableEntity, null);
            }
            else
            {
                TestHelper.ExpectedException(
                    (ctx) => runOperationDelegate(tableEntity, ctx),
                    string.Format("{0} without appropriate permission.", opName),
                    (int)HttpStatusCode.Forbidden);
            }

            if (startPk != null)
            {
                tableEntity.PartitionKey = "A";
                if (startPk.CompareTo(tableEntity.PartitionKey) <= 0)
                {
                    Assert.Inconclusive("Test error: partition key for this test must not be less than or equal to \"A\"");
                }

                TestHelper.ExpectedException(
                    (ctx) => runOperationDelegate(tableEntity, ctx),
                    string.Format("{0} before allowed partition key range", opName),
                    (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));
                tableEntity.PartitionKey = partitionKey;
            }

            if (endPk != null)
            {
                tableEntity.PartitionKey = "Z";
                if (endPk.CompareTo(tableEntity.PartitionKey) >= 0)
                {
                    Assert.Inconclusive("Test error: partition key for this test must not be greater than or equal to \"Z\"");
                }

                TestHelper.ExpectedException(
                    (ctx) => runOperationDelegate(tableEntity, ctx),
                    string.Format("{0} after allowed partition key range", opName),
                    (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

                tableEntity.PartitionKey = partitionKey;
            }

            if (startRk != null)
            {
                if (isRangeQuery || startPk != null)
                {
                    tableEntity.PartitionKey = startPk;
                    tableEntity.RowKey = "A";
                    if (startRk.CompareTo(tableEntity.RowKey) <= 0)
                    {
                        Assert.Inconclusive("Test error: row key for this test must not be less than or equal to \"A\"");
                    }

                    TestHelper.ExpectedException(
                        (ctx) => runOperationDelegate(tableEntity, ctx),
                        string.Format("{0} before allowed row key range", opName),
                        (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

                    tableEntity.RowKey = rowKey;
                }
            }

            if (endRk != null)
            {
                if (isRangeQuery || endPk != null)
                {
                    tableEntity.PartitionKey = endPk;
                    tableEntity.RowKey = "Z";
                    if (endRk.CompareTo(tableEntity.RowKey) >= 0)
                    {
                        Assert.Inconclusive("Test error: row key for this test must not be greater than or equal to \"Z\"");
                    }

                    TestHelper.ExpectedException(
                        (ctx) => runOperationDelegate(tableEntity, ctx),
                        string.Format("{0} after allowed row key range", opName),
                        (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

                    tableEntity.RowKey = rowKey;
                }
            }
        }
        #endregion

        #region SAS Error Conditions

        //[TestMethod] // Disabled until service bug is fixed
        [Description("Attempt to use SAS to authenticate table operations that must not work with SAS.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasInvalidOperations()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();
                // Prepare SAS authentication with full permissions
                string sasString = table.GetSharedAccessSignature(
                                        new SharedAccessTablePolicy
                                        {
                                            Permissions = SharedAccessTablePermissions.Delete,
                                            SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30)
                                        },
                                        null,
                                        null,
                                        null,
                                        null,
                                        null);

                CloudTableClient sasClient = new CloudTableClient(tableClient.BaseUri, new StorageCredentials(sasString));

                // Construct a valid set of service properties to upload.
                ServiceProperties properties = new ServiceProperties(new LoggingProperties(), new MetricsProperties(), new MetricsProperties());
                properties.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
                properties.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;
                properties.Logging.RetentionDays = 9;
                sasClient.GetServiceProperties();
                sasClient.SetServiceProperties(properties);

                // Test invalid client operations
                // BUGBUG: ListTables hides the exception. We should fix this
                // TestHelpers.ExpectedException(() => sasClient.ListTablesSegmented(), "List tables with SAS", HttpStatusCode.Forbidden);
                TestHelper.ExpectedException((ctx) => sasClient.GetServiceProperties(), "Get service properties with SAS", (int)HttpStatusCode.Forbidden);
                TestHelper.ExpectedException((ctx) => sasClient.SetServiceProperties(properties), "Set service properties with SAS", (int)HttpStatusCode.Forbidden);

                CloudTable sasTable = sasClient.GetTableReference(table.Name);

                // Verify that creation fails with SAS
                TestHelper.ExpectedException((ctx) => sasTable.Create(null, ctx), "Create a table with SAS", (int)HttpStatusCode.Forbidden);

                // Create the table.
                table.Create();

                // Test invalid table operations
                TestHelper.ExpectedException((ctx) => sasTable.Delete(null, ctx), "Delete a table with SAS", (int)HttpStatusCode.Forbidden);
                TestHelper.ExpectedException((ctx) => sasTable.GetPermissions(null, ctx), "Get ACL with SAS", (int)HttpStatusCode.Forbidden);
                TestHelper.ExpectedException((ctx) => sasTable.SetPermissions(new TablePermissions(), null, ctx), "Set ACL with SAS", (int)HttpStatusCode.Forbidden);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        #endregion

        #region Update SAS token
        [TestMethod]
        [Description("Update table SAS.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableUpdateSasTestSync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                BaseEntity entity = new BaseEntity("PK", "RK");
                table.Execute(TableOperation.Insert(entity));

                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Delete,
                };

                string sasToken = table.GetSharedAccessSignature(policy);
                StorageCredentials creds = new StorageCredentials(sasToken);
                CloudTable sasTable = new CloudTable(table.Uri, creds);
                TestHelper.ExpectedException(
                    () => sasTable.Execute(TableOperation.Insert(new BaseEntity("PK", "RK2"))),
                    "Try to insert an entity when SAS doesn't allow inserts",
                    HttpStatusCode.Forbidden);

                sasTable.Execute(TableOperation.Delete(entity));

                SharedAccessTablePolicy policy2 = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Delete | SharedAccessTablePermissions.Add,
                };

                string sasToken2 = table.GetSharedAccessSignature(policy2);
                creds.UpdateSASToken(sasToken2);

                sasTable = new CloudTable(table.Uri, creds);

                sasTable.Execute(TableOperation.Insert(new BaseEntity("PK", "RK2")));

            }
            finally
            {
                table.DeleteIfExists();
            }
        }
        #endregion

        #region SasUri
        [TestMethod]
        [Description("Use table SasUri.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasUriTestSync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                BaseEntity entity = new BaseEntity("PK", "RK");
                BaseEntity entity1 = new BaseEntity("PK", "RK1");
                table.Execute(TableOperation.Insert(entity));
                table.Execute(TableOperation.Insert(entity1));

                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Delete,
                };

                string sasToken = table.GetSharedAccessSignature(policy);
                StorageCredentials creds = new StorageCredentials(sasToken);
                CloudStorageAccount sasAcc = new CloudStorageAccount(creds, null /* blobEndpoint */, null /* queueEndpoint */, new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint), null /* fileEndpoint */);
                CloudTableClient client = sasAcc.CreateCloudTableClient();

                CloudTable sasTable = new CloudTable(client.Credentials.TransformUri(table.Uri));
                sasTable.Execute(TableOperation.Delete(entity));

                CloudTable sasTable2 = new CloudTable(new Uri(table.Uri.ToString() + sasToken));
                sasTable2.Execute(TableOperation.Delete(entity1));
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Use table SasUri.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableSasUriPkRkTestSync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Add,
                };

                string sasTokenPkRk = table.GetSharedAccessSignature(policy, null, "tables_batch_0", "00",
                    "tables_batch_1", "04");
                StorageCredentials credsPkRk = new StorageCredentials(sasTokenPkRk);

                // transform uri from credentials
                CloudTable sasTableTransformed = new CloudTable(credsPkRk.TransformUri(table.Uri));

                // create uri by appending sas
                CloudTable sasTableDirect = new CloudTable(new Uri(table.Uri.ToString() + sasTokenPkRk));

                BaseEntity pkrkEnt = new BaseEntity("tables_batch_0", "00");
                sasTableTransformed.Execute(TableOperation.Insert(pkrkEnt));

                pkrkEnt = new BaseEntity("tables_batch_0", "01");
                sasTableDirect.Execute(TableOperation.Insert(pkrkEnt));

                Action<BaseEntity, CloudTable, OperationContext> insertDelegate = (tableEntity, sasTable1, ctx) =>
                {
                    sasTable1.Execute(TableOperation.Insert(tableEntity), null, ctx);
                };

                pkrkEnt = new BaseEntity("tables_batch_2", "00");
                TestHelper.ExpectedException(
                    (ctx) => insertDelegate(pkrkEnt, sasTableTransformed, ctx),
                    string.Format("Inserted entity without appropriate SAS permissions."),
                    (int)HttpStatusCode.Forbidden);
                TestHelper.ExpectedException(
                     (ctx) => insertDelegate(pkrkEnt, sasTableDirect, ctx),
                    string.Format("Inserted entity without appropriate SAS permissions."),
                     (int)HttpStatusCode.Forbidden);

                pkrkEnt = new BaseEntity("tables_batch_1", "05");
                TestHelper.ExpectedException(
                    (ctx) => insertDelegate(pkrkEnt, sasTableTransformed, ctx),
                    string.Format("Inserted entity without appropriate SAS permissions."),
                    (int)HttpStatusCode.Forbidden);
                TestHelper.ExpectedException(
                     (ctx) => insertDelegate(pkrkEnt, sasTableDirect, ctx),
                    string.Format("Inserted entity without appropriate SAS permissions."),
                     (int)HttpStatusCode.Forbidden);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        #endregion

        #region SASIPAddressTests

        /// <summary>
        /// Helper function for testing the IPAddressOrRange funcitonality for tables
        /// </summary>
        /// <param name="generateInitialIPAddressOrRange">Function that generates an initial IPAddressOrRange object to use. This is expected to fail on the service.</param>
        /// <param name="generateFinalIPAddressOrRange">Function that takes in the correct IP address (according to the service) and returns the IPAddressOrRange object
        /// that should be accepted by the service</param>
        public void CloudTableSASIPAddressHelper(Func<IPAddressOrRange> generateInitialIPAddressOrRange, Func<IPAddress, IPAddressOrRange> generateFinalIPAddressOrRange)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();
                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    Permissions = SharedAccessTablePermissions.Query,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                DynamicTableEntity entity = new DynamicTableEntity("PK", "RK", null, new Dictionary<string, EntityProperty>() {{"prop", new EntityProperty(4)}});
                table.Execute(new TableOperation(entity, TableOperationType.Insert));

                // The plan then is to use an incorrect IP address to make a call to the service
                // ensure that we get an error message
                // parse the error message to get my actual IP (as far as the service sees)
                // then finally test the success case to ensure we can actually make requests

                IPAddressOrRange ipAddressOrRange = generateInitialIPAddressOrRange();
                string tableToken = table.GetSharedAccessSignature(policy, null, null, null, null, null, null, ipAddressOrRange);
                StorageCredentials tableSAS = new StorageCredentials(tableToken);
                Uri tableSASUri = tableSAS.TransformUri(table.Uri);
                StorageUri tableSASStorageUri = tableSAS.TransformUri(table.StorageUri);

                CloudTable tableWithSAS = new CloudTable(tableSASUri);
                OperationContext opContext = new OperationContext();
                IPAddress actualIP = null;

                bool exceptionThrown = false;
                TableResult result = null;
                DynamicTableEntity resultEntity = new DynamicTableEntity();
                TableOperation retrieveOp = new TableOperation(resultEntity, TableOperationType.Retrieve);
                retrieveOp.RetrievePartitionKey = entity.PartitionKey;
                retrieveOp.RetrieveRowKey = entity.RowKey;
                try
                {
                    result = tableWithSAS.Execute(retrieveOp, null, opContext);
                }
                catch (StorageException e)
                {
                    string[] parts = e.RequestInformation.HttpStatusMessage.Split(' ');
                    actualIP = IPAddress.Parse(parts[parts.Length - 1].Trim('.'));
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                ipAddressOrRange = generateFinalIPAddressOrRange(actualIP);
                tableToken = table.GetSharedAccessSignature(policy, null, null, null, null, null, null, ipAddressOrRange);
                tableSAS = new StorageCredentials(tableToken);
                tableSASUri = tableSAS.TransformUri(table.Uri);
                tableSASStorageUri = tableSAS.TransformUri(table.StorageUri);


                tableWithSAS = new CloudTable(tableSASUri);
                resultEntity = new DynamicTableEntity();
                retrieveOp = new TableOperation(resultEntity, TableOperationType.Retrieve);
                retrieveOp.RetrievePartitionKey = entity.PartitionKey;
                retrieveOp.RetrieveRowKey = entity.RowKey;
                resultEntity = (DynamicTableEntity)tableWithSAS.Execute(retrieveOp).Result;
    
                Assert.AreEqual(entity.Properties["prop"].PropertyType, resultEntity.Properties["prop"].PropertyType);
                Assert.AreEqual(entity.Properties["prop"].Int32Value.Value, resultEntity.Properties["prop"].Int32Value.Value);
                Assert.IsTrue(table.StorageUri.PrimaryUri.Equals(tableWithSAS.Uri));
                Assert.IsNull(tableWithSAS.StorageUri.SecondaryUri);

                tableWithSAS = new CloudTable(tableSASStorageUri, null);
                resultEntity = new DynamicTableEntity();
                retrieveOp = new TableOperation(resultEntity, TableOperationType.Retrieve);
                retrieveOp.RetrievePartitionKey = entity.PartitionKey;
                retrieveOp.RetrieveRowKey = entity.RowKey;
                resultEntity = (DynamicTableEntity)tableWithSAS.Execute(retrieveOp).Result;
                Assert.AreEqual(entity.Properties["prop"].PropertyType, resultEntity.Properties["prop"].PropertyType);
                Assert.AreEqual(entity.Properties["prop"].Int32Value.Value, resultEntity.Properties["prop"].Int32Value.Value);
                Assert.IsTrue(table.StorageUri.Equals(tableWithSAS.StorageUri));
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Perform a SAS request specifying an IP address or range and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableSASIPAddressQueryParam()
        {
            CloudTableSASIPAddressHelper(() =>
            {
                // We need an IP address that will never be a valid source
                IPAddress invalidIP = IPAddress.Parse("255.255.255.255");
                return new IPAddressOrRange(invalidIP.ToString());
            },
            (IPAddress actualIP) =>
            {
                return new IPAddressOrRange(actualIP.ToString());
            });
        }

        [TestMethod]
        [Description("Perform a SAS request specifying an IP address or range and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableSASIPRangeQueryParam()
        {
            CloudTableSASIPAddressHelper(() =>
            {
                // We need an IP address that will never be a valid source
                IPAddress invalidIPBegin = IPAddress.Parse("255.255.255.0");
                IPAddress invalidIPEnd = IPAddress.Parse("255.255.255.255");

                return new IPAddressOrRange(invalidIPBegin.ToString(), invalidIPEnd.ToString());
            },
                (IPAddress actualIP) =>
                {
                    byte[] actualAddressBytes = actualIP.GetAddressBytes();
                    byte[] initialAddressBytes = actualAddressBytes.ToArray();
                    initialAddressBytes[0]--;
                    byte[] finalAddressBytes = actualAddressBytes.ToArray();
                    finalAddressBytes[0]++;

                    return new IPAddressOrRange(new IPAddress(initialAddressBytes).ToString(), new IPAddress(finalAddressBytes).ToString());
                });
        }
        #endregion

        #region SASHttpsTests
        [TestMethod]
        [Description("Perform a SAS request specifying a shared protocol and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableSASSharedProtocolsQueryParam()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));
            try
            {
                table.Create();
                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    Permissions = SharedAccessTablePermissions.Query,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                DynamicTableEntity entity = new DynamicTableEntity("PK", "RK", null, new Dictionary<string, EntityProperty>() { { "prop", new EntityProperty(4) } });
                table.Execute(new TableOperation(entity, TableOperationType.Insert));

                foreach (SharedAccessProtocol? protocol in new SharedAccessProtocol?[] { null, SharedAccessProtocol.HttpsOrHttp, SharedAccessProtocol.HttpsOnly })
                {
                    string tableToken = table.GetSharedAccessSignature(policy, null, null, null, null, null, protocol, null);
                    StorageCredentials tableSAS = new StorageCredentials(tableToken);
                    Uri tableSASUri = new Uri(table.Uri + tableSAS.SASToken);
                    StorageUri tableSASStorageUri = new StorageUri(new Uri(table.StorageUri.PrimaryUri + tableSAS.SASToken), new Uri(table.StorageUri.SecondaryUri + tableSAS.SASToken));

                    int httpPort = tableSASUri.Port;
                    int securePort = 443;

                    if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.TableSecurePortOverride))
                    {
                        securePort = Int32.Parse(TestBase.TargetTenantConfig.TableSecurePortOverride);
                    }

                    var schemesAndPorts = new[] {
                        new { scheme = Uri.UriSchemeHttp, port = httpPort},
                        new { scheme = Uri.UriSchemeHttps, port = securePort}
                    };

                    CloudTable tableWithSAS = null;
                    TableOperation retrieveOp = TableOperation.Retrieve(entity.PartitionKey, entity.RowKey);

                    foreach (var item in schemesAndPorts)
                    {
                        tableSASUri = TransformSchemeAndPort(tableSASUri, item.scheme, item.port);
                        tableSASStorageUri = new StorageUri(TransformSchemeAndPort(tableSASStorageUri.PrimaryUri, item.scheme, item.port), TransformSchemeAndPort(tableSASStorageUri.SecondaryUri, item.scheme, item.port));

                        if (protocol.HasValue && protocol.Value == SharedAccessProtocol.HttpsOnly && string.CompareOrdinal(item.scheme, Uri.UriSchemeHttp) == 0)
                        {
                            tableWithSAS = new CloudTable(tableSASUri);
                            TestHelper.ExpectedException(() => tableWithSAS.Execute(retrieveOp), "Access a table using SAS with a shared protocols that does not match", HttpStatusCode.Unused);

                            tableWithSAS = new CloudTable(tableSASStorageUri, null);
                            TestHelper.ExpectedException(() => tableWithSAS.Execute(retrieveOp), "Access a table using SAS with a shared protocols that does not match", HttpStatusCode.Unused);
                        }
                        else
                        {
                            tableWithSAS = new CloudTable(tableSASUri);
                            TableResult result = tableWithSAS.Execute(retrieveOp);
                            Assert.AreEqual(entity.Properties["prop"], ((DynamicTableEntity)result.Result).Properties["prop"]);

                            tableWithSAS = new CloudTable(tableSASStorageUri, null);
                            result = tableWithSAS.Execute(retrieveOp);
                            Assert.AreEqual(entity.Properties["prop"], ((DynamicTableEntity)result.Result).Properties["prop"]);
                        }
                    }
                }
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        private static Uri TransformSchemeAndPort(Uri input, string scheme, int port)
        {
            UriBuilder builder = new UriBuilder(input);
            builder.Scheme = scheme;
            builder.Port = port;
            return builder.Uri;
        }
        #endregion

        #region Test Helpers
        internal static void AssertPermissionsEqual(TablePermissions permissions1, TablePermissions permissions2)
        {
            Assert.AreEqual(permissions1.SharedAccessPolicies.Count, permissions2.SharedAccessPolicies.Count);

            foreach (KeyValuePair<string, SharedAccessTablePolicy> pair in permissions1.SharedAccessPolicies)
            {
                SharedAccessTablePolicy policy1 = pair.Value;
                SharedAccessTablePolicy policy2 = permissions2.SharedAccessPolicies[pair.Key];

                Assert.IsNotNull(policy1);
                Assert.IsNotNull(policy2);

                Assert.AreEqual(policy1.Permissions, policy2.Permissions);
                if (policy1.SharedAccessStartTime != null)
                {
                    Assert.IsTrue(Math.Floor((policy1.SharedAccessStartTime.Value - policy2.SharedAccessStartTime.Value).TotalSeconds) == 0);
                }

                if (policy1.SharedAccessExpiryTime != null)
                {
                    Assert.IsTrue(Math.Floor((policy1.SharedAccessExpiryTime.Value - policy2.SharedAccessExpiryTime.Value).TotalSeconds) == 0);
                }
            }
        }
        #endregion
    }
}
