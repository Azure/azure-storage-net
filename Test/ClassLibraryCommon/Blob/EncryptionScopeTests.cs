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
        [Description("Create append blob with EncryptionScope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobCreateEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

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
                appendBlob.CreateOrReplace(null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

            }
            finally
            {
                container.Delete();
            }
        }

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
        [Description("Create append blob with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobCreateEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    // Act
                    appendBlob.BeginCreateOrReplace(null, options, context, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Append blob and snapshot with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlockSnapshotEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            
            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            appendBlob.CreateOrReplace(null, options);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();

                    // Act
                    appendBlob.AppendBlock(sourceStream, null, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

                    // Arrange
                    context = new OperationContext();

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        // Act
                        appendBlob.DownloadRangeToStream(downloadedBlob, 0, 1024, null, options, context);

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
                    appendBlob.Snapshot(metadata, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                    Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);
                }

            }
            finally
            {
                container.Delete();
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
        [Description("Append blob and snapshot with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlockSnapshotEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            appendBlob.CreateOrReplace(null, options);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        var context = new OperationContext();

                        // Act
                        appendBlob.BeginAppendBlock(sourceStream, null, null, options, context, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();

                        // Assert
                        Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

                        // Arrange
                        context = new OperationContext();

                        using (MemoryStream downloadedBlob = new MemoryStream())
                        {
                            // Act
                            appendBlob.BeginDownloadRangeToStream(downloadedBlob, 0, 1024, null, options, context, ar => waitHandle.Set(), null);
                            waitHandle.WaitOne();

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
                        appendBlob.BeginSnapshot(metadata, null, options, context, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();

                        // Assert
                        Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                        Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);
                    }
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Append blob from url with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlockFromUrlEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            var containerPermissions = container.GetPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var sourceAppendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            sourceAppendBlob.CreateOrReplace(null, null);

            var destAppendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            destAppendBlob.CreateOrReplace(null, options);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceAppendBlob.AppendBlock(sourceStream, null, null, null);
                }
                sourceAppendBlob.FetchAttributes();

                var context = new OperationContext();

                // Act
                destAppendBlob.AppendBlock(sourceAppendBlob.Uri, 0, 1024, null, null, null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
            }
            finally
            {
                container.Delete();
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
        [Description("Append blob from url with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlockFromUrlEncryptionscopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            var containerPermissions = container.GetPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var sourceAppendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            sourceAppendBlob.CreateOrReplace(null, null);

            var destAppendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            destAppendBlob.CreateOrReplace(null, options);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        sourceAppendBlob.AppendBlock(sourceStream, null, null, null);
                    }
                    var context = new OperationContext();

                    // Act
                    destAppendBlob.BeginAppendBlock(sourceAppendBlob.Uri, 0, 1024, null, null, null, options, context, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Create page blob with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var pageBlob = container.GetPageBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                // Act
                pageBlob.Create(1024, null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

            }
            finally
            {
                container.DeleteAsync();
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
        [Description("Create page blob with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var pageBlob = container.GetPageBlobReference(GetRandomBlobName());

            try
            {
                var context = new OperationContext();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    // Act
                    pageBlob.BeginCreate(1024, null, options, context, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Page blob put page with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobPutPageEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var pageBlob = container.GetPageBlobReference(GetRandomBlobName());
            pageBlob.Create(1024, null, options);

            // Act
            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();
                    pageBlob.WritePages(sourceStream, 0, null, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }

            }
            finally
            {
                container.Delete();
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
        [Description("Page blob put page with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobPutPageEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var pageBlob = container.GetPageBlobReference(GetRandomBlobName());
            pageBlob.Create(1024, null, options);

            // Act
            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        var context = new OperationContext();
                        var result = pageBlob.BeginWritePages(sourceStream, 0, null, null, options, context, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();

                        // Assert
                        Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                    }
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Page blob put page from URL with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobPutPageFromUrlEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            var containerPermissions = container.GetPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var sourcePageBlob = container.GetPageBlobReference(GetRandomBlobName());
            sourcePageBlob.Create(1024);

            var destPageBlob = container.GetPageBlobReference(GetRandomBlobName());
            destPageBlob.Create(1024, null, options, null);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourcePageBlob.WritePages(sourceStream, 0);
                }

                var context = new OperationContext();

                // Act
                destPageBlob.WritePages(sourcePageBlob.Uri, 0, 1024, 0, null, null, null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
            }
            finally
            {
                container.Delete();
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
        [Description("Page blob put page from URL with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobPutPageFromUrlEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            var containerPermissions = container.GetPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(containerPermissions);

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var sourcePageBlob = container.GetPageBlobReference(GetRandomBlobName());
            sourcePageBlob.Create(1024);

            var destPageBlob = container.GetPageBlobReference(GetRandomBlobName());
            destPageBlob.Create(1024, null, options, null);

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        sourcePageBlob.WritePages(sourceStream, 0);
                    }

                    var context = new OperationContext();

                    // Act
                    destPageBlob.BeginWritePages(sourcePageBlob.Uri, 0, 1024, 0, null, null, null, options, context, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob and put block list with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobPutBlockPutBlockListEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

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
                    blockBlob.PutBlock(blockId, sourceStream, null, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

                    context = new OperationContext();

                    // Act
                    blockBlob.PutBlockList(new List<string>() { blockId }, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
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
        [Description("Block Blob put blob and put block list with encryption scope ARM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobPutBlockPutBlockListEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var blockBlob = container.GetBlockBlobReference(GetRandomBlobName());

            try
            {
                byte[] buffer = GetRandomBuffer(1024);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        var context = new OperationContext();
                        string blockId = GetBlockId();

                        // Act
                        var result = blockBlob.BeginPutBlock(blockId, sourceStream, null, null, options, context, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();

                        // Assert
                        Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);

                        context = new OperationContext();

                        // Act
                        result = blockBlob.BeginPutBlockList(new List<string>() { blockId }, null, options, context, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();

                        // Assert
                        Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                    }
                }

            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob from URL with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobPutBlockFromUrlEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            var containerPermissions = container.GetPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(containerPermissions);

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
                    sourceBlockBlob.UploadFromStream(sourceStream, null, null, null);
                }

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    var context = new OperationContext();

                    // Act
                    destBlockBlob.PutBlock(GetBlockId(), sourceBlockBlob.Uri, 0, 1024, null, null, options, context);

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Block Blob put blob from URL with encryption scope Task")]
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
        [Description("Block Blob put blob from URL with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobPutBlockFromUrlEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            var containerPermissions = container.GetPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(containerPermissions);

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
                    sourceBlockBlob.UploadFromStream(sourceStream, null, null, null);
                }

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        var context = new OperationContext();

                        // Act
                        var result = destBlockBlob.BeginPutBlock(GetBlockId(), sourceBlockBlob.Uri, 0, 1024, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();

                        // Assert
                        // Cannot assert because there is no CloudBlockBlob.BeginPutBlock overload that takes the OperationContext parameter.
                    }
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Blob get properties and set metadata encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobGetPropertiesSetMetadataEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            appendBlob.CreateOrReplace(null, options);

            try
            {
                // Act
                var context = new OperationContext();
                appendBlob.FetchAttributes(null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);

                // Arrange
                appendBlob.Metadata.Add("foo", "bar");
                context = new OperationContext();

                // Act
                appendBlob.SetMetadata(null, options, context);

                // Assert
                Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
            }
            finally
            {
                container.Delete();
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
        [Description("Blob get properties and set metadata encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobGetPropertiesSetMetadataEncryptionScopeAPM()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            appendBlob.CreateOrReplace(null, options);

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    // Act
                    var context = new OperationContext();
                    appendBlob.BeginFetchAttributes(null, options, context, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                    Assert.AreEqual(options.EncryptionScope, appendBlob.Properties.EncryptionScope);

                    // Arrange
                    appendBlob.Metadata.Add("foo", "bar");
                    context = new OperationContext();

                    // Act
                    appendBlob.BeginSetMetadata(null, options, context, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, context.RequestResults.First().EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("create container with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobCreateContainerEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();

            string defaultEncryptionScope = TestBase.TargetTenantConfig.EncryptionScope;

            BlobContainerEncryptionScopeOptions encryptionScopeOptions = new BlobContainerEncryptionScopeOptions
            {
                DefaultEncryptionScope = defaultEncryptionScope,
                PreventEncryptionScopeOverride = true
            };

            try
            {
                var context = new OperationContext();
                container.Create(BlobContainerPublicAccessType.Off, encryptionScopeOptions, null, context);
                //Assert.AreEqual(defaultEncryptionScope, container.Properties.);

                container.FetchAttributes();
                Assert.AreEqual(defaultEncryptionScope, container.Properties.EncryptionScopeOptions.DefaultEncryptionScope);
                Assert.IsTrue(container.Properties.EncryptionScopeOptions.PreventEncryptionScopeOverride);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("create container with encryption scope APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobCreateContainerEncryptionScopeAPM()
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
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    // Act
                    container.BeginCreate(BlobContainerPublicAccessType.Off, encryptionScopeOptions, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();

                    container.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.AreEqual(defaultEncryptionScope, container.Properties.EncryptionScopeOptions.DefaultEncryptionScope);
                    Assert.IsFalse(container.Properties.EncryptionScopeOptions.PreventEncryptionScopeOverride);
                }
            }
            finally
            {
                container.DeleteIfExists();
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
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List blob with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobListBlobEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            appendBlob.CreateOrReplace(null, options);

            try
            {
                foreach (var blobItem in container.ListBlobs())
                {
                    CloudBlob blob = blobItem as CloudBlob;

                    // Assert
                    Assert.AreEqual(options.EncryptionScope, blob.Properties.EncryptionScope);
                }
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("List blob segment with encryption scope")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobListBlobSegmentEncryptionScope()
        {
            // Arrange
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            var encryptionScope = TestBase.TargetTenantConfig.EncryptionScope;
            var options = new BlobRequestOptions
            {
                EncryptionScope = encryptionScope
            };

            var appendBlob = container.GetAppendBlobReference(GetRandomBlobName());
            appendBlob.CreateOrReplace(null, options);

            try
            {
                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(token);
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
                container.Delete();
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
                    Scheme = Uri.UriSchemeHttps,
                    Port = 443
                };
                client = new CloudBlobClient(uriBuilder.Uri, TestBase.StorageCredentials, null);
            }
            else
            {
                var primaryUriBuilder = new UriBuilder(TestBase.TargetTenantConfig.BlobServiceEndpoint)
                {
                    Scheme = Uri.UriSchemeHttps,
                    Port = 443
                };

                var secondaryUriBuilder = new UriBuilder(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint)
                {
                    Scheme = Uri.UriSchemeHttps,
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
