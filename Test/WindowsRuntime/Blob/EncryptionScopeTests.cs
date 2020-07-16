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
    public class EncryptionScopeTests : BlobTestBase
    {        
        [TestMethod]
        [Description("Create append blob with encryption scope task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobCreateEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                // Act
                await appendBlob.CreateOrReplaceAsync(null, options, context);
                await appendBlob.FetchAttributesAsync();

                // Assert
                Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Append blob and snapshot with encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobAppendBlockSnapshotEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
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
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

                    // Arrange
                    context = new OperationContext();

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        // Act
                        await appendBlob.DownloadRangeToStreamAsync(downloadedBlob, 0, 1024, null, options, context);

                        // Assert
                        TestHelper.AssertStreamsAreEqual(sourceStream, downloadedBlob);
                        Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                        Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);
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
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                    Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);
                }

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Append blob from url with encryption scope task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudAppendBlobAppendBlockFromUrlEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            var containerPermissions = await container.GetPermissionsAsync();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
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
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create page blob with encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCreateEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                // Act
                await appendBlob.CreateOrReplaceAsync(null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Page blob put page with encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobPutPageEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
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
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }

            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Page blob put page from URL with encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobPutPageFromUrlEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            var containerPermissions = await container.GetPermissionsAsync();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
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
                Assert.AreEqual(encryptionScope, context.RequestResults.First().EncryptionScope);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob and put block list with encryption scope task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobPutBlockPutBlockListEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
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
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

                    context = new OperationContext();

                    // Act
                    await blockBlob.PutBlockListAsync(new List<string>() { blockId }, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob from URL with encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobPutBlockFromUrlEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            var containerPermissions = await container.GetPermissionsAsync();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
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
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Blob get properties and set metadata encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobGetPropertiesSetMetadataEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            await appendBlob.CreateOrReplaceAsync(null, options, null);

            try
            {
                // Act
                var context = new OperationContext();
                await appendBlob.FetchAttributesAsync(null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);

                // Arrange
                appendBlob.Metadata.Add("foo", "bar");
                context = new OperationContext();

                // Act
                await appendBlob.SetMetadataAsync(null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("create container with encryption scope Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobCreateContainerEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();

            string defaultEncryptionScope = TestBase.TargetTenantConfig.EncryptionScope;

            BlobContainerEncryptionScopeOptions encryptionScopeOptions = new BlobContainerEncryptionScopeOptions
            {
                DefaultEncryptionScope = defaultEncryptionScope,
                PreventEncryptionScopeOverride = false
            };

            try
            {
                var context = new OperationContext();
                await container.CreateAsync(BlobContainerPublicAccessType.Off, encryptionScopeOptions, null, context, CancellationToken.None);

                await container.FetchAttributesAsync();
                Assert.AreEqual(defaultEncryptionScope, container.Properties.EncryptionScopeOptions.DefaultEncryptionScope);
                Assert.IsFalse(container.Properties.EncryptionScopeOptions.PreventEncryptionScopeOverride);
            }
            finally
            {
                await container.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("List blob segment with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobListBlobSegmentEncryptionScopeTask()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            await appendBlob.CreateOrReplaceAsync(null, options, null, CancellationToken.None);

            try
            {
                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = await container.ListBlobsSegmentedAsync(token);
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        CloudBlob blob = blobItem as CloudBlob;

                        // Assert
                        Assert.AreEqual(options.EncryptionScope, blob.Properties.EncryptionScope);
                    }
                    token = results.ContinuationToken;
                }
                while (token != null);
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
    }
}
