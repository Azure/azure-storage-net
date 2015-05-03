﻿// -----------------------------------------------------------------------------------------
// <copyright file="TableEntityEncryptionTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Table.Entities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    [TestClass]
    public class TableEntityEncryptionTests : TableTestBase
    {
#region Locals + Ctors
        CloudTable currentTable = null;
        CloudTableClient tableClient = null;
#endregion

#region Additional test attributes
        [TestInitialize()]
        public void MyTestInitialize()
        {
            tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            currentTable.CreateIfNotExists();
            TableEntity.DisableCompiledSerializers = false;

            if (TestBase.TableBufferManager != null)
            {
                TestBase.TableBufferManager.OutstandingBufferCount = 0;
            }
        }

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

        [TestMethod]
        [Description("TableOperation Insert DynamicTableEntity Encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationInsertDTEEncryption()
        {
            DoInsertDynamicTableEntityEncryptionSync(TablePayloadFormat.Json);
            DoInsertDynamicTableEntityEncryptionSync(TablePayloadFormat.JsonNoMetadata);
            DoInsertDynamicTableEntityEncryptionSync(TablePayloadFormat.JsonFullMetadata);
            DoInsertDynamicTableEntityEncryptionSync(TablePayloadFormat.AtomPub);
        }

        private void DoInsertDynamicTableEntityEncryptionSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent.Properties.Add("foo2", new EntityProperty(string.Empty));
            ent.Properties.Add("foo", new EntityProperty("bar"));
            ent.Properties.Add("fooint", new EntityProperty(1234));

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "foo" || propName == "foo2")
                    {
                        return true;
                    }

                    return false;
                }
            };

            currentTable.Execute(TableOperation.Insert(ent), options, null);

            // Retrieve Entity
            TableRequestOptions retrieveOptions = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) =>
                {
                    if (propName == "fooint")
                    {
                        return EdmType.Int32;
                    }

                    return (EdmType)0;
                },

                EncryptionPolicy = new TableEncryptionPolicy(null, resolver)
            };

            TableOperation operation = TableOperation.Retrieve(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            TableResult result = currentTable.Execute(operation, retrieveOptions, null);

            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);
            Assert.AreEqual(ent.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(ent.Properties["foo"].StringValue, retrievedEntity.Properties["foo"].StringValue);
            Assert.AreEqual(ent.Properties["foo"], retrievedEntity.Properties["foo"]);
            Assert.AreEqual(ent.Properties["foo2"].StringValue, retrievedEntity.Properties["foo2"].StringValue);
            Assert.AreEqual(ent.Properties["foo2"], retrievedEntity.Properties["foo2"]);
            Assert.AreEqual(ent.Properties["fooint"], retrievedEntity.Properties["fooint"]);
            Assert.AreEqual(ent.Properties["fooint"].Int32Value, retrievedEntity.Properties["fooint"].Int32Value);
        }

        [TestMethod]
        [Description("TableOperation Insert POCO Entity Encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationInsertPOCOEncryptionWithResolverSync()
        {
            DoInsertPOCOEntityEncryptionWithResolver(TablePayloadFormat.Json);
            DoInsertPOCOEntityEncryptionWithResolver(TablePayloadFormat.JsonNoMetadata);
            DoInsertPOCOEntityEncryptionWithResolver(TablePayloadFormat.JsonFullMetadata);
            DoInsertPOCOEntityEncryptionWithResolver(TablePayloadFormat.AtomPub);
        }

        private void DoInsertPOCOEntityEncryptionWithResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            BaseEntity ent = new BaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent.Populate();

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions insertOptions = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),
                EncryptionResolver = (pk, rk, propName) =>
                    {
                        if (propName == "A" || propName == "foo")
                        {
                            return true;
                        }

                        return false;
                    }
            };

            currentTable.Execute(TableOperation.Insert(ent), insertOptions, null);

            // Retrieve Entity
            // No need for an encryption resolver while retrieving the entity.
            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            TableOperation operation = TableOperation.Retrieve<BaseEntity>(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            TableResult result = currentTable.Execute(operation, retrieveOptions, null);

            BaseEntity retrievedEntity = result.Result as BaseEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);
            retrievedEntity.Validate();
        }

        [TestMethod]
        [Description("TableOperation Insert POCO Entity Encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationInsertPOCOEncryptionWithAttributesSync()
        {
            DoInsertPOCOEntityEncryptionWithAttributes(TablePayloadFormat.Json);
            DoInsertPOCOEntityEncryptionWithAttributes(TablePayloadFormat.JsonNoMetadata);
            DoInsertPOCOEntityEncryptionWithAttributes(TablePayloadFormat.JsonFullMetadata);
            DoInsertPOCOEntityEncryptionWithAttributes(TablePayloadFormat.AtomPub);
        }

        private void DoInsertPOCOEntityEncryptionWithAttributes(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            EncryptedBaseEntity ent = new EncryptedBaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() + format};
            ent.Populate();

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions insertOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(aesKey, null) };
            currentTable.Execute(TableOperation.Insert(ent), insertOptions, null);

            // Retrieve Entity
            // No need for an encryption resolver while retrieving the entity.
            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            TableOperation operation = TableOperation.Retrieve<EncryptedBaseEntity>(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            TableResult result = currentTable.Execute(operation, retrieveOptions, null);

            EncryptedBaseEntity retrievedEntity = result.Result as EncryptedBaseEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);
            retrievedEntity.Validate();
        }

        [TestMethod]
        [Description("TableOperation Insert POCO Entity Encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationInsertPOCOEncryptionWithAttributesAndResolverSync()
        {
            DoInsertPOCOEntityEncryptionWithAttributesAndResolver(TablePayloadFormat.Json);
            DoInsertPOCOEntityEncryptionWithAttributesAndResolver(TablePayloadFormat.JsonNoMetadata);
            DoInsertPOCOEntityEncryptionWithAttributesAndResolver(TablePayloadFormat.JsonFullMetadata);
            DoInsertPOCOEntityEncryptionWithAttributesAndResolver(TablePayloadFormat.AtomPub);
        }

        private void DoInsertPOCOEntityEncryptionWithAttributesAndResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            EncryptedBaseEntity ent = new EncryptedBaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() + format };
            ent.Populate();

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions insertOptions = new TableRequestOptions() 
            { 
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "B")
                    {
                        return true;
                    }

                    return false;
                }
            };

            // Since we have specified attributes and resolver, properties A, B and foo will be encrypted.
            currentTable.Execute(TableOperation.Insert(ent), insertOptions, null);

            // Retrieve entity without decryption and confirm that all 3 properties were encrypted.
            // No need for an encryption resolver while retrieving the entity.
            TableOperation operation = TableOperation.Retrieve<EncryptedBaseEntity>(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            TableResult result = currentTable.Execute(operation, null, null);
            
            EncryptedBaseEntity retrievedEntity = result.Result as EncryptedBaseEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);

            // Since we store encrypted properties as byte arrays, if a POCO entity is being read as-is, it will not be assign the binary
            // values to strings.
            Assert.IsNull(retrievedEntity.foo);
            Assert.IsNull(retrievedEntity.A);
            Assert.IsNull(retrievedEntity.B);

            // Retrieve entity without decryption and confirm that all 3 properties were encrypted.
            // No need for an encryption resolver while retrieving the entity.
            operation = TableOperation.Retrieve<DynamicTableEntity>(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            result = currentTable.Execute(operation, null, null);

            DynamicTableEntity retrievedDynamicEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedDynamicEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedDynamicEntity.RowKey);
            Assert.AreNotEqual(ent.foo.GetType(), retrievedDynamicEntity.Properties["foo"].GetType());
            Assert.AreNotEqual(ent.A.GetType(), retrievedDynamicEntity.Properties["A"].GetType());
            Assert.AreNotEqual(ent.B.GetType(), retrievedDynamicEntity.Properties["B"].GetType());

            // Retrieve entity and decrypt.
            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            operation = TableOperation.Retrieve<EncryptedBaseEntity>(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            result = currentTable.Execute(operation, retrieveOptions, null);

            retrievedEntity = result.Result as EncryptedBaseEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);
            retrievedEntity.Validate();
        }

        [TestMethod]
        [Description("Basic projection test with encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryPOCOProjectionEncryption()
        {
            // Insert Entity
            EncryptedBaseEntity ent1 = new EncryptedBaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent1.Populate();

            EncryptedBaseEntity ent2 = new EncryptedBaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent2.Populate();

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(aesKey, null) };
            currentTable.Execute(TableOperation.Insert(ent1), options, null);
            currentTable.Execute(TableOperation.Insert(ent2), options, null);

            // Query with different payload formats.
            DoTableQueryPOCOProjectionEncryption(TablePayloadFormat.Json, aesKey);
            DoTableQueryPOCOProjectionEncryption(TablePayloadFormat.JsonNoMetadata, aesKey);
            DoTableQueryPOCOProjectionEncryption(TablePayloadFormat.JsonFullMetadata, aesKey);
            DoTableQueryPOCOProjectionEncryption(TablePayloadFormat.AtomPub, aesKey);
        }

        private void DoTableQueryPOCOProjectionEncryption(TablePayloadFormat format, SymmetricKey aesKey)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            TableQuery<EncryptedBaseEntity> query = new TableQuery<EncryptedBaseEntity>().Select(new List<string>() { "A", "C" });

            foreach (EncryptedBaseEntity ent in currentTable.ExecuteQuery(query, options))
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
        [Description("Basic projection test with encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryDTEProjectionEncryption()
        {
            // Insert Entity
            DynamicTableEntity ent1 = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent1.Properties.Add("A", new EntityProperty(String.Empty));
            ent1.Properties.Add("B", new EntityProperty("b"));

            DynamicTableEntity ent2 = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent2.Properties.Add("A", new EntityProperty("a"));
            ent2.Properties.Add("B", new EntityProperty("b"));

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "A")
                    {
                        return true;
                    }

                    return false;
                }
            };

            currentTable.Execute(TableOperation.Insert(ent1), options, null);
            currentTable.Execute(TableOperation.Insert(ent2), options, null);

            // Query with different payload formats.
            DoTableQueryDTEProjectionEncryption(TablePayloadFormat.Json, aesKey);
            DoTableQueryDTEProjectionEncryption(TablePayloadFormat.JsonNoMetadata, aesKey);
            DoTableQueryDTEProjectionEncryption(TablePayloadFormat.JsonFullMetadata, aesKey);
            DoTableQueryDTEProjectionEncryption(TablePayloadFormat.AtomPub, aesKey);
        }

        private void DoTableQueryDTEProjectionEncryption(TablePayloadFormat format, SymmetricKey aesKey)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            TableQuery query = new TableQuery().Select(new List<string>() { "A" });

            foreach (DynamicTableEntity ent in currentTable.ExecuteQuery(query, options))
            {
                Assert.IsNotNull(ent.PartitionKey);
                Assert.IsNotNull(ent.RowKey);
                Assert.IsNotNull(ent.Timestamp);

                Assert.IsTrue(ent.Properties["A"].StringValue == "a" || ent.Properties["A"].StringValue == String.Empty);
            }
        }

        [TestMethod]
        [Description("TableOperation Replace with encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationReplaceEncryption()
        {
            DoTableOperationReplaceEncryption(TablePayloadFormat.Json);
            DoTableOperationReplaceEncryption(TablePayloadFormat.JsonNoMetadata);
            DoTableOperationReplaceEncryption(TablePayloadFormat.JsonFullMetadata);
            DoTableOperationReplaceEncryption(TablePayloadFormat.AtomPub);
        }

        private void DoTableOperationReplaceEncryption(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "A" || propName == "B")
                    {
                        return true;
                    }

                    return false;
                }
            };

            // Insert Entity
            DynamicTableEntity baseEntity = new DynamicTableEntity("test", "foo" + format.ToString());
            baseEntity.Properties.Add("A", new EntityProperty("a"));
            currentTable.Execute(TableOperation.Insert(baseEntity), options);

            // ReplaceEntity
            DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity.PartitionKey, baseEntity.RowKey) { ETag = baseEntity.ETag };
            replaceEntity.Properties.Add("B", new EntityProperty("b"));
            currentTable.Execute(TableOperation.Replace(replaceEntity), options);

            // Retrieve Entity & Verify Contents
            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };
            TableResult result = currentTable.Execute(TableOperation.Retrieve(baseEntity.PartitionKey, baseEntity.RowKey), retrieveOptions);
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replaceEntity.Properties.Count, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["B"], retrievedEntity.Properties["B"]);
        }

        #region batch
        [TestMethod]
        [Description("TableOperation Insert Or Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchInsertOrReplaceEncryption()
        {
            DoTableBatchInsertOrReplaceEncryption(TablePayloadFormat.Json);
            DoTableBatchInsertOrReplaceEncryption(TablePayloadFormat.JsonNoMetadata);
            DoTableBatchInsertOrReplaceEncryption(TablePayloadFormat.JsonFullMetadata);
            DoTableBatchInsertOrReplaceEncryption(TablePayloadFormat.AtomPub);
        }

        private void DoTableBatchInsertOrReplaceEncryption(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "A" || propName == "B")
                    {
                        return true;
                    }

                    return false;
                }
            };

            // Insert Or Replace with no pre-existing entity
            DynamicTableEntity insertOrReplaceEntity = new DynamicTableEntity("insertOrReplace entity", "foo" + format.ToString());
            insertOrReplaceEntity.Properties.Add("A", new EntityProperty("a"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.InsertOrReplace(insertOrReplaceEntity);
            currentTable.ExecuteBatch(batch, options);

            // Retrieve Entity & Verify Contents
            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };
            TableResult result = currentTable.Execute(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey), retrieveOptions);
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrReplaceEntity.Properties.Count, retrievedEntity.Properties.Count);

            DynamicTableEntity replaceEntity = new DynamicTableEntity(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey);
            replaceEntity.Properties.Add("B", new EntityProperty("b"));

            TableBatchOperation batch2 = new TableBatchOperation();
            batch2.InsertOrReplace(replaceEntity);
            currentTable.ExecuteBatch(batch2, options);

            // Retrieve Entity & Verify Contents
            result = currentTable.Execute(TableOperation.Retrieve(insertOrReplaceEntity.PartitionKey, insertOrReplaceEntity.RowKey), retrieveOptions);
            retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(1, retrievedEntity.Properties.Count);
            Assert.AreEqual(replaceEntity.Properties["B"], retrievedEntity.Properties["B"]);
        }

        [TestMethod]
        [Description("A test to check batch retrieve with ITableEntity")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableBatchRetrieveEncryptedEntitySync()
        {
            DoTableBatchRetrieveEncryptedEntitySync(TablePayloadFormat.Json);
            DoTableBatchRetrieveEncryptedEntitySync(TablePayloadFormat.JsonNoMetadata);
            DoTableBatchRetrieveEncryptedEntitySync(TablePayloadFormat.JsonFullMetadata);
            DoTableBatchRetrieveEncryptedEntitySync(TablePayloadFormat.AtomPub);
        }

        private void DoTableBatchRetrieveEncryptedEntitySync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "A" || propName == "B")
                    {
                        return true;
                    }

                    return false;
                }
            };

            // Add insert
            DynamicTableEntity sendEnt = GenerateRandomEntity(Guid.NewGuid().ToString());

            // generate a set of properties for all supported Types
            sendEnt.Properties = new ComplexEntity().WriteEntity(null);
            sendEnt.Properties.Add("foo", new EntityProperty("bar"));

            TableBatchOperation batch = new TableBatchOperation();
            batch.Retrieve<DynamicTableEntity>(sendEnt.PartitionKey, sendEnt.RowKey);

            // not found
            IList<TableResult> results = currentTable.ExecuteBatch(batch, options);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.NotFound);
            Assert.IsNull(results.First().Result);
            Assert.IsNull(results.First().Etag);

            // insert entity
            currentTable.Execute(TableOperation.Insert(sendEnt), options);
            
            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            // Success
            results = currentTable.ExecuteBatch(batch, retrieveOptions);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results.First().HttpStatusCode, (int)HttpStatusCode.OK);

            DynamicTableEntity retrievedEntity = results.First().Result as DynamicTableEntity;

            // Validate entity
            Assert.AreEqual(sendEnt["foo"], retrievedEntity["foo"]);
        }

        #endregion

        #region encryption validation
        [TestMethod]
        [Description("TableOperation Insert DynamicTableEntity Encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationValidateEncryption()
        {
            DoTableOperationValidateEncryption(TablePayloadFormat.Json);
            DoTableOperationValidateEncryption(TablePayloadFormat.JsonNoMetadata);
            DoTableOperationValidateEncryption(TablePayloadFormat.JsonFullMetadata);
            DoTableOperationValidateEncryption(TablePayloadFormat.AtomPub);
        }

        private void DoTableOperationValidateEncryption(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Insert Entity
            DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent.Properties.Add("encprop", new EntityProperty(String.Empty));
            ent.Properties.Add("encprop2", new EntityProperty(String.Empty));
            ent.Properties.Add("encprop3", new EntityProperty("bar"));
            ent.Properties.Add("notencprop", new EntityProperty(1234));

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions uploadOptions = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) =>
                {
                    if (propName == "notencprop")
                    {
                        return EdmType.Int32;
                    }

                    return (EdmType)0;
                },

                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName.StartsWith("encprop"))
                    {
                        return true;
                    }

                    return false;
                }
            };

            currentTable.Execute(TableOperation.Insert(ent), uploadOptions, null);

            TableRequestOptions downloadOptions = new TableRequestOptions()
            {
                PropertyResolver = (pk, rk, propName, propValue) =>
                {
                    if (propName == "notencprop")
                    {
                        return EdmType.Int32;
                    }

                    return (EdmType)0;
                }
            };

            // Retrieve Entity without decrypting
            TableOperation operation = TableOperation.Retrieve(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            TableResult result = currentTable.Execute(operation, downloadOptions, null);

            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedEntity.RowKey);

            // Properties having the same value should be encrypted to different values.
            CollectionAssert.AreNotEqual(retrievedEntity.Properties["encprop"].BinaryValue, retrievedEntity.Properties["encprop2"].BinaryValue);
            Assert.AreNotEqual(ent.Properties["encprop"].PropertyType, retrievedEntity.Properties["encprop"].PropertyType);
            Assert.AreNotEqual(ent.Properties["encprop2"].PropertyType, retrievedEntity.Properties["encprop2"].PropertyType);
            Assert.AreNotEqual(ent.Properties["encprop3"].PropertyType, retrievedEntity.Properties["encprop3"].PropertyType);
            Assert.AreEqual(ent.Properties["notencprop"].Int32Value, retrievedEntity.Properties["notencprop"].Int32Value);
        }

        [TestMethod]
        [Description("TableOperation Insert DynamicTableEntity Encryption")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableEncryptingUnsupportedPropertiesShouldThrow()
        {
            // Insert Entity
            DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent.Properties.Add("foo2", new EntityProperty(string.Empty));
            ent.Properties.Add("fooint", new EntityProperty(1234));

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName.StartsWith("foo"))
                    {
                        return true;
                    }

                    return false;
                }
            };

            StorageException e = TestHelper.ExpectedException<StorageException>(
                () => currentTable.Execute(TableOperation.Insert(ent), options, null),
                "Encrypting non-string properties should fail");
            Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));

            ent.Properties.Remove("fooint");
            ent.Properties.Add("foo", null);

            e = TestHelper.ExpectedException<StorageException>(
                () => currentTable.Execute(TableOperation.Insert(ent), options, null),
                "Encrypting null properties should fail");
            Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
        }
#endregion

        private static DynamicTableEntity GenerateRandomEntity(string pk)
        {
            DynamicTableEntity ent = new DynamicTableEntity();
            ent.Properties.Add("foo", new EntityProperty("bar"));

            ent.PartitionKey = pk;
            ent.RowKey = Guid.NewGuid().ToString();
            return ent;
        }
    }
}
