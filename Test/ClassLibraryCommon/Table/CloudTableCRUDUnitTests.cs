// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableCRUDUnitTests.cs" company="Microsoft">
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
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Table
{
    /// <summary>
    /// Summary description for CloudTableCRUDUnitTests
    /// </summary>
    [TestClass]
    public class CloudTableCRUDUnitTests : TableTestBase
    {
        #region Locals + Ctors
        public CloudTableCRUDUnitTests()
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
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        //
        // Use TestInitialize to run code before running each test 
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

        [TestMethod]
        [Description("Test table name validation.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableNameValidation()
        {
            NameValidator.ValidateTableName("alpha");
            NameValidator.ValidateTableName("alphanum3r1c");
            NameValidator.ValidateTableName("CapsLock");
            NameValidator.ValidateTableName("$MetricsTransactionsBlob");
            NameValidator.ValidateTableName("$MetricsHourPrimaryTransactionsTable");
            NameValidator.ValidateTableName("$MetricsMinuteSecondaryTransactionsQueue");
            NameValidator.ValidateTableName("tables");
            NameValidator.ValidateTableName("$MetricsCapacityBlob");

            TestInvalidTableHelper(null, "Null not allowed.", "Invalid table name. The table name may not be null, empty, or whitespace only.");
            TestInvalidTableHelper("1numberstart", "Must start with a letter.", "Invalid table name. Check MSDN for more information about valid table naming.");
            TestInvalidTableHelper("middle-dash", "Alphanumeric only.", "Invalid table name. Check MSDN for more information about valid table naming.");
            TestInvalidTableHelper("illegal$char", "Alphanumeric only.", "Invalid table name. Check MSDN for more information about valid table naming.");
            TestInvalidTableHelper("illegal!char", "Alphanumeric only.", "Invalid table name. Check MSDN for more information about valid table naming.");
            TestInvalidTableHelper("white space", "Alphanumeric only.", "Invalid table name. Check MSDN for more information about valid table naming.");
            TestInvalidTableHelper("cc", "Between 3 and 63 characters.", "Invalid table name length. The table name must be between 3 and 63 characters long.");
            TestInvalidTableHelper(new string('n', 64), "Between 3 and 63 characters.", "Invalid table name length. The table name must be between 3 and 63 characters long.");
        }

        private void TestInvalidTableHelper(string tableName, string failMessage, string exceptionMessage)
        {
            try
            {
                NameValidator.ValidateTableName(tableName);
                Assert.Fail(failMessage);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual(exceptionMessage, e.Message);
            }
        }

        #region Table Create

        #region Sync

        [TestMethod]
        [Description("Test Table Create - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableCreateSync(payloadFormat);
            }
        }

        private void DoCloudTableCreateSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test Table Create When Table Already Exists - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateAlreadyExistsSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableCreateAlreadyExistsSync(payloadFormat);
            }
        }

        private void DoCloudTableCreateAlreadyExistsSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(tableRef.Exists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());

                // This should throw with no retries               
                tableRef.Create(null, ctx);
                Assert.Fail();
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.RequestInformation.ExtendedErrorInformation.ErrorCode, "TableAlreadyExists");
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, (int)HttpStatusCode.Conflict);
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test Table Create From URI - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateFromUriSync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            tableRef.Create();

            // Get reference from URI
            CloudTable sameTableRef = new CloudTable(tableRef.Uri);

            try
            {
                Assert.IsTrue(sameTableRef.Name.Equals(tableRef.Name));
                Assert.IsTrue(sameTableRef.Uri.Equals(tableRef.Uri));
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }
        #endregion

        #region APM

        [TestMethod]
        [Description("Test Table Create - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginCreate((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    tableRef.EndCreate(result);
                }

                Assert.IsTrue(tableRef.Exists());
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test Table Create When Table Already Exists - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateAlreadyExistsAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(tableRef.Exists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());

                // This should throw with no retries               
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginCreate(
                        null,
                        ctx,
                        (res) =>
                        {
                            result = res;
                            evt.Set();
                        },
                        null);
                    evt.WaitOne();

                    tableRef.EndCreate(result);
                }

                Assert.Fail();
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.RequestInformation.ExtendedErrorInformation.ErrorCode, "TableAlreadyExists");
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, (int)HttpStatusCode.Conflict);
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }
        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("Test Table Create - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);
                tableRef.CreateAsync().Wait();
                Assert.IsTrue(tableRef.ExistsAsync().Result);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table Create When Table Already Exists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateAlreadyExistsTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);
                tableRef.CreateAsync().Wait();
                Assert.IsTrue(tableRef.ExistsAsync().Result);

                // This should throw with no retries               
                tableRef.CreateAsync(null, ctx).Wait();
              
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                StorageException ex = e.InnerException as StorageException;
                if (ex == null)
                {
                    throw;
                }
                
                Assert.AreEqual(ex.RequestInformation.ExtendedErrorInformation.ErrorCode, "TableAlreadyExists");
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, (int)HttpStatusCode.Conflict);
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }
#endif

        #endregion

        #endregion

        #region Table CreateIfNotExists

        #region Sync

        [TestMethod]
        [Description("Test Table CreateIfNotExists - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateIfNotExistsSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableCreateIfNotExistsSync(payloadFormat);
            }
        }

        private void DoCloudTableCreateIfNotExistsSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                Assert.IsTrue(tableRef.CreateIfNotExists());
                Assert.IsTrue(tableRef.Exists());
                Assert.IsFalse(tableRef.CreateIfNotExists());
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        #endregion

        #region APM

        [TestMethod]
        [Description("Test Table CreateIfNotExists - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateIfNotExistsAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                // Assert Table does not exist
                Assert.IsFalse(tableRef.Exists());
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginCreateIfNotExists((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    // Table should have been created
                    Assert.IsTrue(tableRef.EndCreateIfNotExists(result));
                }

                // Assert Table exists
                Assert.IsTrue(tableRef.Exists());

                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginCreateIfNotExists((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    // Table should not have been created
                    Assert.IsFalse(tableRef.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }
        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("Test Table CreateIfNotExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateIfNotExistsTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);
                Assert.IsTrue(tableRef.CreateIfNotExistsAsync().Result);
                Assert.IsTrue(tableRef.ExistsAsync().Result);
                Assert.IsFalse(tableRef.CreateIfNotExistsAsync().Result);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table CreateIfNotExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateIfNotExistsCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable table = tableClient.GetTableReference(tableName);
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsTrue(table.CreateIfNotExistsAsync(cancellationToken).Result);
                Assert.IsTrue(table.ExistsAsync().Result);
                Assert.IsFalse(table.CreateIfNotExistsAsync(cancellationToken).Result);
                Assert.IsTrue(table.ExistsAsync().Result);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table CreateIfNotExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateIfNotExistsRequestOptionsOperationContextTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable table = tableClient.GetTableReference(tableName);
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            try
            {
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsTrue(table.CreateIfNotExistsAsync(requestOptions, operationContext).Result);
                Assert.IsTrue(table.ExistsAsync().Result);
                Assert.IsFalse(table.CreateIfNotExistsAsync(requestOptions, operationContext).Result);
                Assert.IsTrue(table.ExistsAsync().Result);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table CreateIfNotExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableCreateIfNotExistsRequestOptionsOperationContextCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable table = tableClient.GetTableReference(tableName);
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsTrue(table.CreateIfNotExistsAsync(requestOptions, operationContext, cancellationToken).Result);
                Assert.IsTrue(table.ExistsAsync().Result);
                Assert.IsFalse(table.CreateIfNotExistsAsync(requestOptions, operationContext, cancellationToken).Result);
                Assert.IsTrue(table.ExistsAsync().Result);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        #endregion

        #endregion

        #region Table Delete

        #region Sync

        [TestMethod]
        [Description("Test Table Delete - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableDeleteSync(payloadFormat);
            }
        }

        private void DoCloudTableDeleteSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());
                tableRef.Delete();
                Assert.IsFalse(tableRef.Exists());
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test Table Delete When Table Does Not Exist - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteWhenNotExistSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableDeleteWhenNotExistSync(payloadFormat);
            }
        }

        private void DoCloudTableDeleteWhenNotExistSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(tableRef.Exists());

                // This should throw with no retries               
                tableRef.Delete(null, ctx);
                Assert.Fail();
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, 404);
                Assert.AreEqual(ex.RequestInformation.ExtendedErrorInformation.ErrorCode, "ResourceNotFound");
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        #endregion

        #region APM

        [TestMethod]
        [Description("Test Table Delete - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());

                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginDelete((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);
                    evt.WaitOne();

                    tableRef.EndDelete(result);
                }

                Assert.IsFalse(tableRef.Exists());
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test Table Delete When Table Does Not Exist - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteWhenNotExistAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(tableRef.Exists());

                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginDelete(
                        null,
                        ctx,
                        (res) =>
                        {
                            result = res;
                            evt.Set();
                        },
                        null);
                    evt.WaitOne();

                    tableRef.EndDelete(result);
                }

                Assert.Fail();
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, 404);
                Assert.AreEqual(ex.RequestInformation.ExtendedErrorInformation.ErrorCode, "ResourceNotFound");
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }
        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("Test Table Delete - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);
                tableRef.CreateAsync().Wait();
                Assert.IsTrue(tableRef.ExistsAsync().Result);
                tableRef.DeleteAsync().Wait();
                Assert.IsFalse(tableRef.ExistsAsync().Result);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table Delete When Table Does Not Exist - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteWhenNotExistTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);
            OperationContext ctx = new OperationContext();

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);

                // This should throw with no retries               
                tableRef.DeleteAsync(null, ctx).Wait();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                StorageException ex = e.InnerException as StorageException;
                if (ex == null)
                {
                    throw;
                }

                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, 404);
                Assert.AreEqual(ex.RequestInformation.ExtendedErrorInformation.ErrorCode, "ResourceNotFound");
                TestHelper.AssertNAttempts(ctx, 1);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        #endregion

        #endregion

        #region Table DeleteIfExists

        #region Sync

        [TestMethod]
        [Description("Test Table DeleteIfExists - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteIfExistsSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableDeleteIfExistsSync(payloadFormat, false);
                DoCloudTableDeleteIfExistsSync(payloadFormat, true);
            }
        }

        private void DoCloudTableDeleteIfExistsSync(TablePayloadFormat format, bool simulateParallelDelete)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                Assert.IsFalse(tableRef.DeleteIfExists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());
                if (simulateParallelDelete)
                {
                    OperationContext context = new OperationContext();
                    context.SendingRequest += (sender, e) =>
                        {
                            if (e.Request.Method == "DELETE")
                            {
                                tableRef.Delete();
                            }
                        };
                    Assert.IsFalse(tableRef.DeleteIfExists(null /* requestOptions */, context));
                }
                else
                {
                    Assert.IsTrue(tableRef.DeleteIfExists());
                    Assert.IsFalse(tableRef.DeleteIfExists());
                }
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        #endregion

        #region APM

        [TestMethod]
        [Description("Test Table DeleteIfExists - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteIfExistsAPM()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableDeleteIfExistsAPM(payloadFormat, false);
                DoCloudTableDeleteIfExistsAPM(payloadFormat, true);
            }
        }

        private void DoCloudTableDeleteIfExistsAPM(TablePayloadFormat format, bool simulateParallelDelete)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                // Assert Table does not exist
                Assert.IsFalse(tableRef.Exists());
                using (AutoResetEvent evt = new AutoResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginDeleteIfExists((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    // Table should not have been deleted as it doesnt exist
                    Assert.IsFalse(tableRef.EndDeleteIfExists(result));

                    // Assert Table exists
                    tableRef.Create();
                    Assert.IsTrue(tableRef.Exists());

                    if (simulateParallelDelete)
                    {
                        OperationContext context = new OperationContext();
                        context.SendingRequest += (sender, e) =>
                        {
                            if (e.Request.Method == "DELETE")
                            {
                                tableRef.Delete();
                            }
                        };

                        result = null;
                        tableRef.BeginDeleteIfExists(null /* requestOptions */, context, (res) =>
                        {
                            result = res;
                            evt.Set();
                        }, null);

                        evt.WaitOne();

                        // Table should have been deleted
                        Assert.IsFalse(tableRef.EndDeleteIfExists(result));
                    }
                    else
                    {
                        result = null;
                        tableRef.BeginDeleteIfExists((res) =>
                        {
                            result = res;
                            evt.Set();
                        }, null);

                        evt.WaitOne();

                        // Table should have been deleted
                        Assert.IsTrue(tableRef.EndDeleteIfExists(result));

                        result = null;
                        tableRef.BeginDeleteIfExists((res) =>
                        {
                            result = res;
                            evt.Set();
                        }, null);

                        evt.WaitOne();

                        // Assert Table Was Deleted
                        Assert.IsFalse(tableRef.EndDeleteIfExists(result));
                    }
                }
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        #endregion
        
        #region Task

#if TASK
        [TestMethod]
        [Description("Test Table DeleteIfExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteIfExistsTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);
                Assert.IsFalse(tableRef.DeleteIfExistsAsync().Result);
                tableRef.CreateAsync().Wait();
                Assert.IsTrue(tableRef.ExistsAsync().Result);
                Assert.IsTrue(tableRef.DeleteIfExistsAsync().Result);
                Assert.IsFalse(tableRef.DeleteIfExistsAsync().Result);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table DeleteIfExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteIfExistsCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable table = tableClient.GetTableReference(tableName);
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsFalse(table.DeleteIfExistsAsync(cancellationToken).Result);
                table.CreateAsync().Wait();
                Assert.IsTrue(table.ExistsAsync().Result);
                Assert.IsTrue(table.DeleteIfExistsAsync(cancellationToken).Result);
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsFalse(table.DeleteIfExistsAsync(cancellationToken).Result);
                Assert.IsFalse(table.ExistsAsync().Result);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table DeleteIfExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteIfExistsRequestOptionsOperationContextTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable table = tableClient.GetTableReference(tableName);
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            try
            {
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsFalse(table.DeleteIfExistsAsync(requestOptions, operationContext).Result);
                table.CreateAsync().Wait();
                Assert.IsTrue(table.ExistsAsync().Result);
                Assert.IsTrue(table.DeleteIfExistsAsync(requestOptions, operationContext).Result);
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsFalse(table.DeleteIfExistsAsync(requestOptions, operationContext).Result);
                Assert.IsFalse(table.ExistsAsync().Result);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test Table DeleteIfExists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableDeleteIfExistsRequestOptionsOperationContextCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable table = tableClient.GetTableReference(tableName);
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsFalse(table.DeleteIfExistsAsync(requestOptions, operationContext, cancellationToken).Result);
                table.CreateAsync().Wait();
                Assert.IsTrue(table.ExistsAsync().Result);
                Assert.IsTrue(table.DeleteIfExistsAsync(requestOptions, operationContext, cancellationToken).Result);
                Assert.IsFalse(table.ExistsAsync().Result);
                Assert.IsFalse(table.DeleteIfExistsAsync(requestOptions, operationContext, cancellationToken).Result);
                Assert.IsFalse(table.ExistsAsync().Result);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        #endregion

        #endregion

        #region Table Exists

        #region Sync

        [TestMethod]
        [Description("Test Table Exists - Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableExistsSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableExistsSync(payloadFormat);
            }
        }

        private void DoCloudTableExistsSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.Exists());
                tableRef.Create();
                Assert.IsTrue(tableRef.Exists());
                tableRef.Delete();
                Assert.IsFalse(tableRef.Exists());
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }
        #endregion

        #region APM

        [TestMethod]
        [Description("Test Table Exists - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableExistsAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginExists((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    // Table should not have been deleted as it doesnt exist
                    Assert.IsFalse(tableRef.EndExists(result));
                }

                tableRef.Create();

                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginExists((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    // Table should not have been deleted as it doesnt exist
                    Assert.IsTrue(tableRef.EndExists(result));
                }

                tableRef.Delete();
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult result = null;
                    tableRef.BeginExists((res) =>
                    {
                        result = res;
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    // Table should not have been deleted as it doesnt exist
                    Assert.IsFalse(tableRef.EndExists(result));
                }
            }
            finally
            {
                tableRef.DeleteIfExists();
            }
        }

        #endregion
        
        #region Task

#if TASK
        [TestMethod]
        [Description("Test Table Exists - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableExistsTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string tableName = GenerateRandomTableName();
            CloudTable tableRef = tableClient.GetTableReference(tableName);

            try
            {
                Assert.IsFalse(tableRef.ExistsAsync().Result);
                tableRef.CreateAsync().Wait();
                Assert.IsTrue(tableRef.ExistsAsync().Result);
                tableRef.DeleteAsync().Wait();
                Assert.IsFalse(tableRef.ExistsAsync().Result);
            }
            finally
            {
                tableRef.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        #endregion

        #endregion
    }
}
