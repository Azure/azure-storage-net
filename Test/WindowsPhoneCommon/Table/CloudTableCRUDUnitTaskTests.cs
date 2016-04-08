// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableCRUDUnitTaskTests.cs" company="Microsoft">
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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    /// <summary>
    /// Summary description for CloudTableCRUDUnitTaskTests
    /// </summary>
    [TestClass]
    public class CloudTableCRUDUnitTaskTests : TableTestBase
    {
        #region Locals + Ctors
        public CloudTableCRUDUnitTaskTests()
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

        static List<CloudTable> createdTables = new List<CloudTable>();

        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        //[TestInitialize()]
        // public async void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        // public async void MyTestCleanup() { }
        //
        #endregion

        #region Table Create

        [TestMethod]
        [Description("Test Table Create - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableCreateAsync()
        {
            await DoCloudTableCreateAsync(TablePayloadFormat.Json);
            await DoCloudTableCreateAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableCreateAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableCreateAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());
                await tableRef.CreateAsync();
                Assert.IsTrue(await tableRef.ExistsAsync());
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table Create When Table Already Exists - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableCreateAlreadyExistsAsync()
        {
            await DoCloudTableCreateAlreadyExistsAsync(TablePayloadFormat.Json);
            await DoCloudTableCreateAlreadyExistsAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableCreateAlreadyExistsAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableCreateAlreadyExistsAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());
                await tableRef.CreateAsync();
                Assert.IsTrue(await tableRef.ExistsAsync());

                // This should throw with no retries               
                await tableRef.CreateAsync(null, ctx);
                Assert.Fail();
            }
            catch (Exception)
            {
                Assert.AreEqual(ctx.LastResult.ExtendedErrorInformation.ErrorCode, "TableAlreadyExists");
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        #endregion

        #region Table CreateIfNotExists

        [TestMethod]
        [Description("Test Table CreateIfNotExists - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableCreateIfNotExistsAsync()
        {
            await DoCloudTableCreateIfNotExistsAsync(TablePayloadFormat.Json);
            await DoCloudTableCreateIfNotExistsAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableCreateIfNotExistsAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableCreateIfNotExistsAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());
                Assert.IsTrue(await tableRef.CreateIfNotExistsAsync());
                Assert.IsTrue(await tableRef.ExistsAsync());
                Assert.IsFalse(await tableRef.CreateIfNotExistsAsync());
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }
        #endregion

        #region Table Delete

        [TestMethod]
        [Description("Test Table Delete - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableDeleteAsync()
        {
            await DoCloudTableDeleteAsync(TablePayloadFormat.Json);
            await DoCloudTableDeleteAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableDeleteAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableDeleteAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());
                await tableRef.CreateAsync();
                Assert.IsTrue(await tableRef.ExistsAsync());
                await tableRef.DeleteAsync();
                Assert.IsFalse(await tableRef.ExistsAsync());
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table Delete When Table Does Not Exist - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableDeleteWhenNotExistAsync()
        {
            await DoCloudTableDeleteWhenNotExistAsync(TablePayloadFormat.Json);
            await DoCloudTableDeleteWhenNotExistAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableDeleteWhenNotExistAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableDeleteWhenNotExistAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());

                // This should throw with no retries               
                await tableRef.DeleteAsync(null, ctx);
                Assert.Fail();
            }
            catch (Exception)
            {
                Assert.AreEqual(ctx.LastResult.HttpStatusCode, 404);
                Assert.AreEqual(ctx.LastResult.ExtendedErrorInformation.ErrorCode, "ResourceNotFound");
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }
        #endregion

        #region Table DeleteIfExists

        [TestMethod]
        [Description("Test Table DeleteIfExists - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableDeleteIfExistsAsync()
        {
            await DoCloudTableDeleteIfExistsAsync(TablePayloadFormat.Json);
            await DoCloudTableDeleteIfExistsAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableDeleteIfExistsAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableDeleteIfExistsAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());
                Assert.IsFalse(await tableRef.DeleteIfExistsAsync());
                await tableRef.CreateAsync();
                Assert.IsTrue(await tableRef.ExistsAsync());
                Assert.IsTrue(await tableRef.DeleteIfExistsAsync());
                Assert.IsFalse(await tableRef.DeleteIfExistsAsync());
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }
        #endregion

        #region Table Exists

        [TestMethod]
        [Description("Test Table Exists - Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableExistsAsync()
        {
            await DoCloudTableExistsAsync(TablePayloadFormat.Json);
            await DoCloudTableExistsAsync(TablePayloadFormat.JsonNoMetadata);
            await DoCloudTableExistsAsync(TablePayloadFormat.JsonFullMetadata);
        }

        private async Task DoCloudTableExistsAsync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(await tableRef.ExistsAsync());
                await tableRef.CreateAsync();
                Assert.IsTrue(await tableRef.ExistsAsync());
                await tableRef.DeleteAsync();
                Assert.IsFalse(await tableRef.ExistsAsync());
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }
        #endregion
    }
}
