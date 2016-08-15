// -----------------------------------------------------------------------------------------
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
    using System.Threading.Tasks;

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
        }

        private void DoInsertPOCOEntityEncryptionWithAttributes(TablePayloadFormat format)
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

            // Since we store encrypted properties as byte arrays, if a POCO entity is being read as-is, odata will not assign the binary
            // values to strings. In JSON no metadata, the service does not return the types and the client lib does the parsing and reads the 
            // base64 encoded string as-is.
            if (format == TablePayloadFormat.JsonNoMetadata)
            {
                Assert.IsNotNull(retrievedEntity.foo);
                Assert.IsNotNull(retrievedEntity.A);
                Assert.IsNotNull(retrievedEntity.B);
                Assert.AreEqual(ent.foo.GetType(), retrievedEntity.foo.GetType());
                Assert.AreEqual(ent.A.GetType(), retrievedEntity.A.GetType());
                Assert.AreEqual(ent.B.GetType(), retrievedEntity.B.GetType());
            }
            else
            {
                Assert.IsNull(retrievedEntity.foo);
                Assert.IsNull(retrievedEntity.A);
                Assert.IsNull(retrievedEntity.B);
            }

            // Retrieve entity without decryption and confirm that all 3 properties were encrypted.
            // No need for an encryption resolver while retrieving the entity.
            operation = TableOperation.Retrieve<DynamicTableEntity>(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            result = currentTable.Execute(operation, null, null);

            DynamicTableEntity retrievedDynamicEntity = result.Result as DynamicTableEntity;
            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(ent.PartitionKey, retrievedDynamicEntity.PartitionKey);
            Assert.AreEqual(ent.RowKey, retrievedDynamicEntity.RowKey);

            if (format == TablePayloadFormat.JsonNoMetadata)
            {
                Assert.AreEqual(EdmType.String, retrievedDynamicEntity.Properties["foo"].PropertyType);
                Assert.AreEqual(EdmType.String, retrievedDynamicEntity.Properties["A"].PropertyType);
                Assert.AreEqual(EdmType.String, retrievedDynamicEntity.Properties["B"].PropertyType);
            }
            else
            {
                Assert.AreEqual(EdmType.Binary, retrievedDynamicEntity.Properties["foo"].PropertyType);
                Assert.AreEqual(EdmType.Binary, retrievedDynamicEntity.Properties["A"].PropertyType);
                Assert.AreEqual(EdmType.Binary, retrievedDynamicEntity.Properties["B"].PropertyType);
            }

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
        [Description("Checks to ensure that when querying with encryption, if we don't request any specific columns, we get all the data back.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryProjectionEncryptionNoSelect()
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

            tableClient.DefaultRequestOptions.PayloadFormat = TablePayloadFormat.Json;

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);
            TableEncryptionPolicy encryptionPolicy = new TableEncryptionPolicy(null, resolver);

            IEnumerable<EncryptedBaseEntity> entities = null;
            CloudTableClient encryptingTableClient = new CloudTableClient(this.tableClient.StorageUri, this.tableClient.Credentials);
            encryptingTableClient.DefaultRequestOptions.EncryptionPolicy = encryptionPolicy;
            encryptingTableClient.DefaultRequestOptions.RequireEncryption = true;

            entities = encryptingTableClient.GetTableReference(currentTable.Name).CreateQuery<EncryptedBaseEntity>().Select(ent => ent);

            foreach (EncryptedBaseEntity ent in entities)
            {
                ent.Validate();
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

        [TestMethod]
        [Description("Swap rows and ensure decryption fails.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableEncryptionValidateSwappingPropertiesThrows()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions()
            {
                EncryptionPolicy = new TableEncryptionPolicy(aesKey, null),

                EncryptionResolver = (pk, rk, propName) =>
                {
                    if (propName == "Prop1")
                    {
                        return true;
                    }

                    return false;
                }
            };

            // Insert Entities
            DynamicTableEntity baseEntity1 = new DynamicTableEntity("test1", "foo1");
            baseEntity1.Properties.Add("Prop1", new EntityProperty("Value1"));
            currentTable.Execute(TableOperation.Insert(baseEntity1), options);

            DynamicTableEntity baseEntity2 = new DynamicTableEntity("test1", "foo2");
            baseEntity2.Properties.Add("Prop1", new EntityProperty("Value2"));
            currentTable.Execute(TableOperation.Insert(baseEntity2), options);

            // Retrieve entity1 (Do not set encryption policy)
            TableResult result = currentTable.Execute(TableOperation.Retrieve(baseEntity1.PartitionKey, baseEntity1.RowKey));
            DynamicTableEntity retrievedEntity = result.Result as DynamicTableEntity;

            // Replace entity2 with encrypted entity1's properties (Do not set encryption policy).
            DynamicTableEntity replaceEntity = new DynamicTableEntity(baseEntity2.PartitionKey, baseEntity2.RowKey) { ETag = baseEntity2.ETag };
            replaceEntity.Properties = retrievedEntity.Properties;
            currentTable.Execute(TableOperation.Replace(replaceEntity));

            // Try to retrieve entity2
            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            TableRequestOptions retrieveOptions = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            try
            {
                result = currentTable.Execute(TableOperation.Retrieve(baseEntity2.PartitionKey, baseEntity2.RowKey), retrieveOptions);
                Assert.Fail();
            }
            catch (StorageException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(CryptographicException));
            }
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
            if (format == TablePayloadFormat.JsonNoMetadata)
            {
                // With DTE and Json no metadata, if an encryption policy is not set, the client lib just reads the byte arrays as strings.
                Assert.AreNotEqual(retrievedEntity.Properties["encprop"].StringValue, retrievedEntity.Properties["encprop2"].StringValue);
            }
            else
            {
                CollectionAssert.AreNotEqual(retrievedEntity.Properties["encprop"].BinaryValue, retrievedEntity.Properties["encprop2"].BinaryValue);
                Assert.AreNotEqual(ent.Properties["encprop"].PropertyType, retrievedEntity.Properties["encprop"].PropertyType);
                Assert.AreNotEqual(ent.Properties["encprop2"].PropertyType, retrievedEntity.Properties["encprop2"].PropertyType);
                Assert.AreNotEqual(ent.Properties["encprop3"].PropertyType, retrievedEntity.Properties["encprop3"].PropertyType);
            }

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

        [TestMethod]
        [Description("TableOperation Insert/Get with RequireEncryption flag.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationEncryptionWithStrictMode()
        {
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
                },

                RequireEncryption = true
            };

            currentTable.Execute(TableOperation.Insert(ent), options, null);

            // Insert an entity when RequireEncryption is set to true but no policy is specified. This should throw.
            options.EncryptionPolicy = null;

            TestHelper.ExpectedException<StorageException>(
                () => currentTable.Execute(TableOperation.Insert(ent), options, null),
                "Not specifying a policy when RequireEncryption is set to true should throw.");

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

                EncryptionPolicy = new TableEncryptionPolicy(null, resolver),

                RequireEncryption = true
            };

            TableOperation operation = TableOperation.Retrieve(ent.PartitionKey, ent.RowKey);
            Assert.IsFalse(operation.IsTableEntity);
            TableResult result = currentTable.Execute(operation, retrieveOptions, null);

            // Replace entity with plain text.
            ent.ETag = (result.Result as DynamicTableEntity).ETag;
            currentTable.Execute(TableOperation.Replace(ent));

            // Retrieve with RequireEncryption flag but no metadata on the service. This should throw.
            TestHelper.ExpectedException<StorageException>(
                () => currentTable.Execute(operation, retrieveOptions, null),
                "Retrieving with RequireEncryption set to true and no metadata on the service should fail.");

            // Set RequireEncryption flag to true and retrieve.
            retrieveOptions.RequireEncryption = false;
            result = currentTable.Execute(operation, retrieveOptions, null);
        }

        [TestMethod]
        [Description("TableOperation InsertOrMerge/Merge with RequireEncryption flag.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationEncryptionWithStrictModeOnMerge()
        {
            // Insert Entity
            DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent.Properties.Add("foo2", new EntityProperty(string.Empty));
            ent.Properties.Add("foo", new EntityProperty("bar"));
            ent.Properties.Add("fooint", new EntityProperty(1234));
            ent.ETag = "*";

            TableRequestOptions options = new TableRequestOptions()
            {
                RequireEncryption = true
            };

            try
            {
                currentTable.Execute(TableOperation.Merge(ent), options, null);
                Assert.Fail("Merge with RequireEncryption on should fail.");
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionPolicyMissingInStrictMode);
            }

            try
            {
                currentTable.Execute(TableOperation.InsertOrMerge(ent), options, null);
                Assert.Fail("InsertOrMerge with RequireEncryption on should fail.");
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionPolicyMissingInStrictMode);
            }

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            options.EncryptionPolicy = new TableEncryptionPolicy(aesKey, null);

            try
            {
                currentTable.Execute(TableOperation.Merge(ent), options, null);
                Assert.Fail("Merge with an EncryptionPolicy should fail.");
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionNotSupportedForOperation);
            }

            try
            {
                currentTable.Execute(TableOperation.InsertOrMerge(ent), options, null);
                Assert.Fail("InsertOrMerge with an EncryptionPolicy should fail.");
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionNotSupportedForOperation);
            }
        }

        [TestMethod]
        [Description("Basic query test with mixed mode.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryEncryptionMixedMode()
        {
            // Insert Entity
            EncryptedBaseEntity ent1 = new EncryptedBaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent1.Populate();

            EncryptedBaseEntity ent2 = new EncryptedBaseEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
            ent2.Populate();

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(aesKey, null) };

            // Insert an encrypted entity.
            currentTable.Execute(TableOperation.Insert(ent1), options, null);

            // Insert a non-encrypted entity.
            currentTable.Execute(TableOperation.Insert(ent2), null, null);

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(null, resolver) };

            // Set RequireEncryption to false and query. This will succeed.
            options.RequireEncryption = false;
            TableQuery<EncryptedBaseEntity> query = new TableQuery<EncryptedBaseEntity>();
            currentTable.ExecuteQuery(query, options).ToList();

            // Set RequireEncryption to true and query. This will fail because it can't find the metadata for the second enctity on the server.
            options.RequireEncryption = true;
            TestHelper.ExpectedException<StorageException>(
                () => currentTable.ExecuteQuery(query, options).ToList(),
                "All entities retrieved should be encrypted when RequireEncryption is set to true.");
        }

        [TestMethod]
        [Description("Test that sync table operations that should not get encrypted still function properly if you supply an encryption policy.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationsIgnoreEncryption()
        {
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(aesKey, null), RequireEncryption = true };

            CloudTable testTable = this.tableClient.GetTableReference(GenerateRandomTableName());

            try
            {
                // Check Create()
                testTable.Create(options, null);
                Assert.IsTrue(testTable.Exists(), "Table failed to be created when encryption policy was supplied.");

                // Check Exists()
                Assert.IsTrue(testTable.Exists(options, null), "Table.Exists() failed when encryption policy was supplied.");

                // Check ListTables().  ListTables() does not call ListTablesSegmented(), so we need to check both.
                Assert.AreEqual(testTable.Name, this.tableClient.ListTables(testTable.Name, options, null).First().Name, "ListTables failed when an encryption policy was specified.");
                Assert.AreEqual(testTable.Name, this.ListAllTables(this.tableClient, testTable.Name, options).First().Name, "ListTables failed when an encryption policy was specified.");

                // Check Get and Set Permissions
                TablePermissions permissions = testTable.GetPermissions();
                string policyName = "samplePolicy";
                permissions.SharedAccessPolicies.Add(policyName, new SharedAccessTablePolicy() { Permissions = SharedAccessTablePermissions.Query, SharedAccessExpiryTime = DateTime.Now + TimeSpan.FromDays(1) });
                testTable.SetPermissions(permissions, options, null);
                Assert.AreEqual(policyName, testTable.GetPermissions().SharedAccessPolicies.First().Key);
                Assert.AreEqual(policyName, testTable.GetPermissions(options, null).SharedAccessPolicies.First().Key);

                // Check Delete
                testTable.Delete(options, null);
                Assert.IsFalse(testTable.Exists());
            }
            finally
            {
                testTable.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test that async table operations that should not get encrypted still function properly if you supply an encryption policy.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableOperationsIgnoreEncryptionAsync()
        {
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = new TableEncryptionPolicy(aesKey, null), RequireEncryption = true };

            CloudTable testTable = this.tableClient.GetTableReference(GenerateRandomTableName());

            try
            {
                // Check Create()
                testTable.CreateAsync(options, null).Wait();
                Assert.IsTrue(testTable.Exists(), "Table failed to be created when encryption policy was supplied.");

                // Check Exists()
                Assert.IsTrue(testTable.ExistsAsync(options, null).Result, "Table.Exists() failed when encryption policy was supplied.");

                // Check ListTables().
                Assert.AreEqual(testTable.Name, this.ListAllTablesAsync(this.tableClient, testTable.Name, options).Result.First().Name, "ListTables failed when an encryption policy was specified.");

                // Check Get and Set Permissions
                TablePermissions permissions = testTable.GetPermissions();
                string policyName = "samplePolicy";
                permissions.SharedAccessPolicies.Add(policyName, new SharedAccessTablePolicy() { Permissions = SharedAccessTablePermissions.Query, SharedAccessExpiryTime = DateTime.Now + TimeSpan.FromDays(1) });
                testTable.SetPermissionsAsync(permissions, options, null).Wait();
                Assert.AreEqual(policyName, testTable.GetPermissions().SharedAccessPolicies.First().Key);
                Assert.AreEqual(policyName, testTable.GetPermissionsAsync(options, null).Result.SharedAccessPolicies.First().Key);

                // Check Delete
                testTable.DeleteAsync(options, null).Wait();
                Assert.IsFalse(testTable.Exists());
            }
            finally
            {
                testTable.DeleteIfExists();
            }
        }

        private List<CloudTable> ListAllTables(CloudTableClient tableClient, string prefix, TableRequestOptions options)
        {
            TableContinuationToken token = null;
            List<CloudTable> tables = new List<CloudTable>();
            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmented(prefix, null /* maxResults*/, token, options, null);
                tables.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;
            } while (token != null);

            return tables;
        }

        private async Task<List<CloudTable>> ListAllTablesAsync(CloudTableClient tableClient, string prefix, TableRequestOptions options)
        {
            TableContinuationToken token = null;
            List<CloudTable> tables = new List<CloudTable>();
            do
            {
                TableResultSegment resultSegment = await tableClient.ListTablesSegmentedAsync(prefix, null /* maxResults*/, token, options, null);
                tables.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;
            } while (token != null);

            return tables;
        }

        private static DynamicTableEntity GenerateRandomEntity(string pk)
        {
            DynamicTableEntity ent = new DynamicTableEntity();
            ent.Properties.Add("foo", new EntityProperty("bar"));

            ent.PartitionKey = pk;
            ent.RowKey = Guid.NewGuid().ToString();
            return ent;
        }

        [TestMethod]
        [Description("Test decrypting entities encoded with Java's v1 encryption algorithm")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableEncryptionCrossPlatformTesting()
        {
            CloudTable testTable = this.tableClient.GetTableReference(GenerateRandomTableName());
            try
            {
                testTable.CreateIfNotExists();

                // Hard code some sample data, then see if we can decrypt it.
                // This key is used only for test, do not use to encrypt any sensitive data.
                SymmetricKey sampleKEK = new SymmetricKey("key1", Convert.FromBase64String(@"rFz7+tv4hRiWdWUJMFlxl1xxtU/qFUeTriGaxwEcxjU="));

                // This data here was created using Fiddler to capture the .NET library uploading an encrypted entity, encrypted with the specified KEK and CEK.
                // Note that this data is lacking the library information in the KeyWrappingMetadata.
                DynamicTableEntity dteNetOld = new DynamicTableEntity("pk", "netUp");
                dteNetOld.Properties["sampleProp"] = new EntityProperty(Convert.FromBase64String(@"27cLSlSFqy9C0xUCr57XAA=="));
                dteNetOld.Properties["sampleProp2"] = new EntityProperty(Convert.FromBase64String(@"pZR6Ln/DwbwyyOCEezL/hg=="));
                dteNetOld.Properties["sampleProp3"] = new EntityProperty(Convert.FromBase64String(@"JOix4N8eX/WuCtIvlD2QxQ=="));
                dteNetOld.Properties["_ClientEncryptionMetadata1"] = new EntityProperty("{\"WrappedContentKey\":{\"KeyId\":\"key1\",\"EncryptedKey\":\"pwSKxpJkwCS2zCaykh0m8e4OApeLuQ4FiahZ9zdwxaLL1HsWqQ4DSw==\",\"Algorithm\":\"A256KW\"},\"EncryptionAgent\":{\"Protocol\":\"1.0\",\"EncryptionAlgorithm\":\"AES_CBC_256\"},\"ContentEncryptionIV\":\"obTAQcYeFQ3IU7Jfcema7Q==\",\"KeyWrappingMetadata\":{}}");
                dteNetOld.Properties["_ClientEncryptionMetadata2"] = new EntityProperty(Convert.FromBase64String(@"MWA7LlvXSJnKhf8f7MVhfjWECkxrCyCXGIlYY6ucpr34IVDU7fN6IHvKxV15WiXp"));

                testTable.Execute(TableOperation.Insert(dteNetOld));

                // This data here was created using Fiddler to capture the Java library uploading an encrypted entity, encrypted with the specified KEK and CEK.
                // Note that this data is lacking the KeyWrappingMetadata.  It also constructs an IV with PK + RK + column name.
                DynamicTableEntity dteJavaOld = new DynamicTableEntity("pk", "javaUp");
                dteJavaOld.Properties["sampleProp"] = new EntityProperty(Convert.FromBase64String(@"sa3bCvXq79ImSPveChS+cg=="));
                dteJavaOld.Properties["sampleProp2"] = new EntityProperty(Convert.FromBase64String(@"KXjuBNn9DesCmMcdVpamJw=="));
                dteJavaOld.Properties["sampleProp3"] = new EntityProperty(Convert.FromBase64String(@"wykVEni1rV+H6oNjoNml6A=="));
                dteJavaOld.Properties["_ClientEncryptionMetadata1"] = new EntityProperty("{\"WrappedContentKey\":{\"KeyId\":\"key1\",\"EncryptedKey\":\"2F4rIuDmGPgEmhpvTtE7x6281BetKz80EsgRwGxTjL8rRt7Z7GrOgg==\",\"Algorithm\":\"A256KW\"},\"EncryptionAgent\":{\"Protocol\":\"1.0\",\"EncryptionAlgorithm\":\"AES_CBC_256\"},\"ContentEncryptionIV\":\"8st/uXffG+6DxBhw4D1URw==\"}");
                dteJavaOld.Properties["_ClientEncryptionMetadata2"] = new EntityProperty(Convert.FromBase64String(@"WznUoytxkvl9KhZ4mNlqkBvRTUHN/D5IgJmNl7kQBOtFBOSgZZrTfZXKH8GjmvKA"));

                testTable.Execute(TableOperation.Insert(dteJavaOld));

                TableEncryptionPolicy policy = new TableEncryptionPolicy(sampleKEK, null);
                TableRequestOptions options = new TableRequestOptions() { EncryptionPolicy = policy };
                options.EncryptionResolver = (pk, rk, propName) => true;

                foreach (DynamicTableEntity dte in testTable.ExecuteQuery(new TableQuery(), options))
                {
                    Assert.AreEqual(dte.Properties["sampleProp"].StringValue, "sampleValue", "String not properly decoded.");
                    Assert.AreEqual(dte.Properties["sampleProp2"].StringValue, "sampleValue", "String not properly decoded.");
                    Assert.AreEqual(dte.Properties["sampleProp3"].StringValue, "sampleValue", "String not properly decoded.");
                    Assert.AreEqual(dte.Properties.Count, 3, "Incorrect number of properties returned.");
                }
            }
            finally
            {
                testTable.DeleteIfExists();
            }
        }
    }
}