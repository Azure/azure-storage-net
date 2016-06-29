// -----------------------------------------------------------------------------------------
// <copyright file="SASTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class SASTests : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        public SASTests()
        {
            TestInitialize();
        }
        public void Dispose()
        {
            TestCleanup();
        }
#endif
        private CloudBlobContainer testContainer;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testContainer = GetRandomContainerReference();
            this.testContainer.CreateAsync().Wait();

            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testContainer.DeleteAsync().Wait();
            this.testContainer = null;
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        private static async Task TestAccessAsync(string sasToken, SharedAccessBlobPermissions permissions, SharedAccessBlobHeaders headers, CloudBlobContainer container, CloudBlob blob, HttpStatusCode setBlobMetadataWhileSasExpectedStatusCode = HttpStatusCode.Forbidden, HttpStatusCode deleteBlobWhileSasExpectedStatusCode = HttpStatusCode.Forbidden, HttpStatusCode listBlobWhileSasExpectedStatusCode = HttpStatusCode.Forbidden)
        {
            OperationContext operationContext = new OperationContext();
            StorageCredentials credentials = string.IsNullOrEmpty(sasToken) ?
                new StorageCredentials() :
                new StorageCredentials(sasToken);

            if (container != null)
            {
                container = new CloudBlobContainer(container.Uri, credentials);
                if (blob.BlobType == BlobType.BlockBlob)
                {
                    blob = container.GetBlockBlobReference(blob.Name);
                }
                else
                {
                    blob = container.GetPageBlobReference(blob.Name);
                }
            }
            else
            {
                if (blob.BlobType == BlobType.BlockBlob)
                {
                    blob = new CloudBlockBlob(blob.Uri, credentials);
                }
                else
                {
                    blob = new CloudPageBlob(blob.Uri, credentials);
                }
            }

            if (container != null)
            {
                if ((permissions & SharedAccessBlobPermissions.List) == SharedAccessBlobPermissions.List)
                {
                    await container.ListBlobsSegmentedAsync(null);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, null, null, null, operationContext),
                        operationContext,
                        "List blobs while SAS does not allow for listing",
                        listBlobWhileSasExpectedStatusCode);
                }
            }

            if ((permissions & SharedAccessBlobPermissions.Read) == SharedAccessBlobPermissions.Read)
            {
                await blob.FetchAttributesAsync();

                // Test headers
                if (headers != null)
                {
                    if (headers.CacheControl != null)
                    {
                        Assert.AreEqual(headers.CacheControl, blob.Properties.CacheControl);
                    }

                    if (headers.ContentDisposition != null)
                    {
                        Assert.AreEqual(headers.ContentDisposition, blob.Properties.ContentDisposition);
                    }

                    if (headers.ContentEncoding != null)
                    {
                        Assert.AreEqual(headers.ContentEncoding, blob.Properties.ContentEncoding);
                    }

                    if (headers.ContentLanguage != null)
                    {
                        Assert.AreEqual(headers.ContentLanguage, blob.Properties.ContentLanguage);
                    }

                    if (headers.ContentType != null)
                    {
                        Assert.AreEqual(headers.ContentType, blob.Properties.ContentType);
                    }
                }
            }
            else
            {
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.FetchAttributesAsync(null, null, operationContext),
                    operationContext,
                    "Fetch blob attributes while SAS does not allow for reading",
                    HttpStatusCode.Forbidden);
            }

            if ((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write)
            {
                await blob.SetMetadataAsync();
            }
            else
            {
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(null, null, operationContext),
                    operationContext,
                    "Set blob metadata while SAS does not allow for writing",
                    setBlobMetadataWhileSasExpectedStatusCode);
            }

            if ((permissions & SharedAccessBlobPermissions.Delete) == SharedAccessBlobPermissions.Delete)
            {
                await blob.DeleteAsync();
            }
            else
            {
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.DeleteAsync(DeleteSnapshotsOption.None, null, null, operationContext),
                    operationContext,
                    "Delete blob while SAS does not allow for deleting",
                    deleteBlobWhileSasExpectedStatusCode);
            }
        }

        private static async Task TestBlobSASAsync(CloudBlob testBlob, SharedAccessBlobPermissions permissions, SharedAccessBlobHeaders headers)
        {
            await UploadTextAsync(testBlob, "blob", Encoding.UTF8);

            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = permissions,
            };

            string sasToken = testBlob.GetSharedAccessSignature(policy, headers);
            await TestAccessAsync(sasToken, permissions, headers, null, testBlob);
        }

        [TestMethod]
        [Description("Test updateSASToken")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerUpdateSASTokenAsync()
        {
            // Create a policy with read/write acces and get SAS.
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
            };
            string sasToken = this.testContainer.GetSharedAccessSignature(policy);
            CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob");
            await UploadTextAsync(testBlockBlob, "blob", Encoding.UTF8);
            await TestAccessAsync(sasToken, SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write, null, this.testContainer, testBlockBlob);

            StorageCredentials creds = new StorageCredentials(sasToken);

            // Change the policy to only read and update SAS.
            SharedAccessBlobPolicy policy2 = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessBlobPermissions.Read
            };
            string sasToken2 = this.testContainer.GetSharedAccessSignature(policy2);
            creds.UpdateSASToken(sasToken2);
            
            // Extra check to make sure that we have actually uopdated the SAS token.
            CloudBlobContainer container = new CloudBlobContainer(this.testContainer.Uri, creds);
            CloudBlockBlob blob = container.GetBlockBlobReference("blockblob2");
            OperationContext operationContext = new OperationContext();

            await TestHelper.ExpectedExceptionAsync(
                async () => await UploadTextAsync(blob, "blob", Encoding.UTF8, null, null, operationContext),
                operationContext,
                "Writing to a blob while SAS does not allow for writing",
                HttpStatusCode.Forbidden);

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");
            await testPageBlob.CreateAsync(0);
            await TestAccessAsync(sasToken2, SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob);

        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerSASCombinationsAsync()
        {
            for (int i = 1; i < 16; i++)
            {
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = permissions,
                };
                string sasToken = this.testContainer.GetSharedAccessSignature(policy);

                CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob" + i);
                await UploadTextAsync(testBlockBlob, "blob", Encoding.UTF8);
                await SASTests.TestAccessAsync(sasToken, permissions, null, this.testContainer, testBlockBlob);

                CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob" + i);
                await UploadTextAsync(testPageBlob, "blob", Encoding.UTF8);
                await SASTests.TestAccessAsync(sasToken, permissions, null, this.testContainer, testPageBlob);
            }
        }

        [TestMethod]
        [Description("Test access on a public container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerPublicAccessAsync()
        {
            CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob");
            await UploadTextAsync(testBlockBlob, "blob", Encoding.UTF8);

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");
            await UploadTextAsync(testPageBlob, "blob", Encoding.UTF8);

            BlobContainerPermissions permissions = new BlobContainerPermissions();

            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await this.testContainer.SetPermissionsAsync(permissions);
            await Task.Delay(30 * 1000);
            await SASTests.TestAccessAsync(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob, setBlobMetadataWhileSasExpectedStatusCode: HttpStatusCode.NotFound, deleteBlobWhileSasExpectedStatusCode: HttpStatusCode.NotFound);
            await SASTests.TestAccessAsync(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob, setBlobMetadataWhileSasExpectedStatusCode: HttpStatusCode.NotFound, deleteBlobWhileSasExpectedStatusCode: HttpStatusCode.NotFound);

            permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            await this.testContainer.SetPermissionsAsync(permissions);
            await Task.Delay(30 * 1000);
            await SASTests.TestAccessAsync(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob, setBlobMetadataWhileSasExpectedStatusCode: HttpStatusCode.NotFound, deleteBlobWhileSasExpectedStatusCode: HttpStatusCode.NotFound, listBlobWhileSasExpectedStatusCode: HttpStatusCode.NotFound);
            await SASTests.TestAccessAsync(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob, setBlobMetadataWhileSasExpectedStatusCode: HttpStatusCode.NotFound, deleteBlobWhileSasExpectedStatusCode: HttpStatusCode.NotFound, listBlobWhileSasExpectedStatusCode: HttpStatusCode.NotFound);
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobSASCombinationsAsync()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudBlockBlob testBlob = this.testContainer.GetBlockBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                await TestBlobSASAsync(testBlob, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSASCombinationsAsync()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudPageBlob testBlob = this.testContainer.GetPageBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                await TestBlobSASAsync(testBlob, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobSASHeadersAsync()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudBlockBlob testBlob = this.testContainer.GetBlockBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                SharedAccessBlobHeaders headers = new SharedAccessBlobHeaders()
                {
                    CacheControl = "no-transform",
                    ContentDisposition = "attachment",
                    ContentEncoding = "gzip",
                    ContentLanguage = "tr,en",
                    ContentType = "text/html"
                };

                await TestBlobSASAsync(testBlob, permissions, headers);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSASHeadersAsync()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudPageBlob testBlob = this.testContainer.GetPageBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                SharedAccessBlobHeaders headers = new SharedAccessBlobHeaders()
                {
                    CacheControl = "no-transform",
                    ContentDisposition = "attachment",
                    ContentEncoding = "gzip",
                    ContentLanguage = "tr,en",
                    ContentType = "text/html"
                };

                await TestBlobSASAsync(testBlob, permissions, headers);
            }
        }

        [TestMethod]
        [Description("Perform a SAS request specifying a shared protocol and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSASSharedProtocolsQueryParamInvalid()
        {
            SharedAccessProtocol? protocol = default(SharedAccessProtocol);
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
            };

            CloudBlobContainer container = GetRandomContainerReference();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");

            TestHelper.ExpectedException<ArgumentException>(
            () => blockBlob.GetSharedAccessSignature(policy, null /* headers */, null /* stored access policy ID */, protocol, null /* IP address or range */),
            "Creating a SAS should throw when using an invalid value for the Protocol enum.",
            String.Format(SR.InvalidProtocolsInSAS, protocol));
        }
    }
}
