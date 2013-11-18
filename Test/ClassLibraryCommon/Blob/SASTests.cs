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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class SASTests : BlobTestBase
    {
        private CloudBlobContainer testContainer;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testContainer = GetRandomContainerReference();
            this.testContainer.Create();

            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testContainer.Delete();
            this.testContainer = null;
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        private static void TestAccess(string sasToken, SharedAccessBlobPermissions permissions, SharedAccessBlobHeaders headers, CloudBlobContainer container, ICloudBlob blob)
        {
            StorageCredentials credentials = string.IsNullOrEmpty(sasToken) ?
                new StorageCredentials() :
                new StorageCredentials(sasToken);

            if (container != null)
            {
                container = new CloudBlobContainer(credentials.TransformUri(container.Uri));
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
                    blob = new CloudBlockBlob(credentials.TransformUri(blob.Uri));
                }
                else
                {
                    blob = new CloudPageBlob(credentials.TransformUri(blob.Uri));
                }
            }

            if (container != null)
            {
                if ((permissions & SharedAccessBlobPermissions.List) == SharedAccessBlobPermissions.List)
                {
                    container.ListBlobs().ToArray();
                }
                else
                {
                    TestHelper.ExpectedException(
                        () => container.ListBlobs().ToArray(),
                        "List blobs while SAS does not allow for listing",
                        HttpStatusCode.NotFound);
                }
            }

            if ((permissions & SharedAccessBlobPermissions.Read) == SharedAccessBlobPermissions.Read)
            {
                blob.FetchAttributes();
                
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
                TestHelper.ExpectedException(
                    () => blob.FetchAttributes(),
                    "Fetch blob attributes while SAS does not allow for reading",
                    HttpStatusCode.NotFound);
            }

            if ((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write)
            {
                blob.SetMetadata();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => blob.SetMetadata(),
                    "Set blob metadata while SAS does not allow for writing",
                    HttpStatusCode.NotFound);
            }

            if ((permissions & SharedAccessBlobPermissions.Delete) == SharedAccessBlobPermissions.Delete)
            {
                blob.Delete();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => blob.Delete(),
                    "Delete blob while SAS does not allow for deleting",
                    HttpStatusCode.NotFound);
            }
        }

        private static void TestBlobSAS(ICloudBlob testBlob, SharedAccessBlobPermissions permissions, SharedAccessBlobHeaders headers)
        {
            UploadText(testBlob, "blob", Encoding.UTF8);

            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = permissions,
            };

            string sasToken = testBlob.GetSharedAccessSignature(policy, headers);
            TestAccess(sasToken, permissions, headers, null, testBlob);
        }

        [TestMethod]
        [Description("Test updateSASToken")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerUpdateSASToken()
        {
            // Create a policy with read/write access and get SAS.
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
            };
            string sasToken = this.testContainer.GetSharedAccessSignature(policy);
            //Thread.Sleep(35000);
            CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob");
            UploadText(testBlockBlob, "blob", Encoding.UTF8);
            TestAccess(sasToken, SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write, null, this.testContainer, testBlockBlob);

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

            // Extra check to make sure that we have actually updated the SAS token.
            CloudBlobContainer container = new CloudBlobContainer(this.testContainer.Uri, creds);
            CloudBlockBlob blob = container.GetBlockBlobReference("blockblob2");

            TestHelper.ExpectedException(
                () => UploadText(blob, "blob", Encoding.UTF8),
                "Writing to a blob while SAS does not allow for writing",
                HttpStatusCode.NotFound);

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");
            testPageBlob.Create(0);
            TestAccess(sasToken2, SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob);
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSASCombinations()
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
                UploadText(testBlockBlob, "blob", Encoding.UTF8);
                SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testBlockBlob);

                CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob" + i);
                UploadText(testPageBlob, "blob", Encoding.UTF8);
                SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testPageBlob);
            }
        }

        [TestMethod]
        [Description("Test access on a public container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerPublicAccess()
        {
            CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob");
            UploadText(testBlockBlob, "blob", Encoding.UTF8);

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");
            UploadText(testPageBlob, "blob", Encoding.UTF8);

            BlobContainerPermissions permissions = new BlobContainerPermissions();

            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            this.testContainer.SetPermissions(permissions);
            Thread.Sleep(35 * 1000);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob);

            permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            this.testContainer.SetPermissions(permissions);
            Thread.Sleep(35 * 1000);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob);
        }

        [TestMethod]
        [Description("Test access on a public container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerPolicy()
        {
            CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob");
            UploadText(testBlockBlob, "blob", Encoding.UTF8);

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");
            UploadText(testPageBlob, "blob", Encoding.UTF8);

            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessBlobPermissions.Read,
            };

            BlobContainerPermissions permissions = new BlobContainerPermissions();
            permissions.SharedAccessPolicies.Add("testpolicy", policy);
            this.testContainer.SetPermissions(permissions);
            Thread.Sleep(35 * 1000);

            string sasToken = testBlockBlob.GetSharedAccessSignature(null, "testpolicy");
            SASTests.TestAccess(sasToken, policy.Permissions, null, null, testBlockBlob);

            sasToken = testPageBlob.GetSharedAccessSignature(null, "testpolicy");
            SASTests.TestAccess(sasToken, policy.Permissions, null, null, testPageBlob);

            sasToken = this.testContainer.GetSharedAccessSignature(null, "testpolicy");
            SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testBlockBlob);
            SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testPageBlob);
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobSASCombinations()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudBlockBlob testBlob = this.testContainer.GetBlockBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                TestBlobSAS(testBlob, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSASCombinations()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudPageBlob testBlob = this.testContainer.GetPageBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                TestBlobSAS(testBlob, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobSASHeaders()
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

                TestBlobSAS(testBlob, permissions, headers);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSASHeaders()
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

                TestBlobSAS(testBlob, permissions, headers);
            }
        }
    }
}
