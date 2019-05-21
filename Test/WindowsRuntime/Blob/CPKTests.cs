// -----------------------------------------------------------------------------------------
// <copyright file="CPKTests.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class CPKTests : BlobTestBase
    {
        [TestMethod]
        [Description("Create append blob with CPK task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobCreateCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                // Act
                await appendBlob.CreateOrReplaceAsync(null, options, context);

                // Assert
                Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Append blob and snapshot with CPK Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobAppendBlockSnapshotCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            await appendBlob.CreateOrReplaceAsync(null, options, null);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();

                    // Act
                    await appendBlob.AppendBlockAsync(sourceStream, null, null, options, context);

                    // Assert
                    Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                    Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);

                    // Arrange
                    context = new OperationContext();

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        // Act
                        await appendBlob.DownloadRangeToStreamAsync(downloadedBlob, 0, 1024, null, options, context);

                        // Assert
                        TestHelper.AssertStreamsAreEqual(sourceStream, downloadedBlob);
                        Assert.IsTrue(context.RequestResults.First().IsServiceEncrypted);
                        Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
                    }

                    // Arrange
                    context = new OperationContext();
                    var metadata = new Dictionary<string, string>
                    {
                        { "foo", "bar" }
                    };

                    // Act
                    await appendBlob.SnapshotAsync(metadata, null, options, context);

                    // Assert
                    Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                    Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
                }

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Append blob from url with CPK task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobAppendBlockFromUrlCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            var containerPermissions = await container.GetPermissionsAsync();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(containerPermissions);

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey,
            };

            var sourceAppendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            await sourceAppendBlob.CreateOrReplaceAsync();

            var destAppendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            await destAppendBlob.CreateOrReplaceAsync(null, options, null);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    await sourceAppendBlob.AppendBlockAsync(sourceStream);
                }
                var context = new OperationContext();

                // Act
                await destAppendBlob.AppendBlockAsync(sourceAppendBlob.Uri, 0, 1024, null, null, null, options, context, CancellationToken.None);

                // Assert
                Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create page blob with CPK Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCreateCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                // Act
                await appendBlob.CreateOrReplaceAsync(null, options, context);

                // Assert
                Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Page blob put page with CPK Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobPutPageCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var pageBlob = container.GetPageBlobReference(GetRandomBlobName());
            await pageBlob.CreateAsync(1024, null, options, null);

            // Act
            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();
                    await pageBlob.WritePagesAsync(sourceStream, 0, null, null, options, context);

                    // Assert
                    Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                    Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
                }

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Page blob put page from URL with CPK Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobPutPageFromUrlCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            var containerPermissions = await container.GetPermissionsAsync();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(containerPermissions);

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey,
            };

            var sourcePageBlob = container.GetPageBlobReference(GetRandomBlobName());
            await sourcePageBlob.CreateAsync(1024);

            var destPageBlob = container.GetPageBlobReference(GetRandomBlobName());
            await destPageBlob.CreateAsync(1024, null, options, null);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    await sourcePageBlob.WritePagesAsync(sourceStream, 0, null);
                }

                var context = new OperationContext();

                // Act
                await destPageBlob.WritePagesAsync(sourcePageBlob.Uri, 0, 1024, 0, null, null, null, options, context, CancellationToken.None);

                // Assert
                Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob and put block list with CPK task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobPutBlockPutBlockListCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var blockBlob = container.GetBlockBlobReference(GetRandomBlobName());

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();
                    string blockId = GetBlockId();

                    // Act
                    await blockBlob.PutBlockAsync(blockId, sourceStream, null, null, options, context);

                    // Assert
                    Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                    Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);

                    context = new OperationContext();

                    // Act
                    await blockBlob.PutBlockListAsync(new List<string>() { blockId }, null, options, context);

                    // Assert
                    Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                    Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob from URL with CPK Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobPutBlockFromUrlCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            var containerPermissions = await container.GetPermissionsAsync();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(containerPermissions);

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var sourceBlockBlob = container.GetBlockBlobReference(GetRandomBlobName());
            var destBlockBlob = container.GetBlockBlobReference(GetRandomBlobName());

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    await sourceBlockBlob.UploadFromStreamAsync(sourceStream, null, null, null);
                }

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();

                    // Act
                    await destBlockBlob.PutBlockAsync(GetBlockId(), sourceBlockBlob.Uri, 0, 1024, null, null, options, context);

                    // Assert
                    Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                    Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Blob get properties and set metadata CPK Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobGetPropertiesSetMetadataCPKTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var customerProvidedKey = BuildCustomerProvidedKey();
            var options = new BlobRequestOptions
            {
                CustomerProvidedKey = customerProvidedKey
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            await appendBlob.CreateOrReplaceAsync(null, options, null);

            try
            {
                // Act
                var context = new OperationContext();
                await appendBlob.FetchAttributesAsync(null, options, context);

                // Assert
                Assert.IsTrue(context.RequestResults.First().IsServiceEncrypted);
                Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);

                // Arrange
                appendBlob.Metadata.Add("foo", "bar");
                context = new OperationContext();

                // Act
                await appendBlob.SetMetadataAsync(null, options, context);

                // Assert
                Assert.IsTrue(context.RequestResults.First().IsRequestServerEncrypted);
                Assert.AreEqual(options.CustomerProvidedKey.KeySHA256, context.RequestResults.First().EncryptionKeySHA256);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        public static CloudBlobContainer GetRandomContainerReference()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            string name = GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            return container;
        }

        public static CloudBlobClient GenerateCloudBlobClient()
        {
            CloudBlobClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint))
            {
                var uriBuilder = new UriBuilder(TestBase.TargetTenantConfig.BlobServiceEndpoint)
                {
                    Scheme = "https",
                    Port = 443
                };
                client = new CloudBlobClient(uriBuilder.Uri, TestBase.StorageCredentials, null);
            }
            else
            {
                var primaryUriBuilder = new UriBuilder(TestBase.TargetTenantConfig.BlobServiceEndpoint)
                {
                    Scheme = "https",
                    Port = 443
                };

                var secondaryUriBuilder = new UriBuilder(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint)
                {
                    Scheme = "https",
                    Port = 443
                };

                StorageUri baseAddressUri = new StorageUri(
                    primaryUriBuilder.Uri,
                    secondaryUriBuilder.Uri);
                client = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials, null);
            }

            client.AuthenticationScheme = AuthenticationScheme.SharedKey; ;

#if WINDOWS_DESKTOP
            client.BufferManager = TableBufferManager;
#endif

            return client;
        }

        private BlobCustomerProvidedKey BuildCustomerProvidedKey()
        {
            using (var aes = Aes.Create())
            {
                return new BlobCustomerProvidedKey(aes.Key);
            }
        }
    }
}
