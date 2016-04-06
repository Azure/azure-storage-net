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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Core;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Linq;

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

        private static void TestAccess(string sasToken, SharedAccessBlobPermissions permissions, SharedAccessBlobHeaders headers, CloudBlobContainer container, CloudBlob blob)
        {
            CloudBlob SASblob;
            StorageCredentials credentials = string.IsNullOrEmpty(sasToken) ?
                new StorageCredentials() :
                new StorageCredentials(sasToken);

            if (container != null)
            {
                container = new CloudBlobContainer(credentials.TransformUri(container.Uri));
                if (blob.BlobType == BlobType.BlockBlob)
                {
                    SASblob = container.GetBlockBlobReference(blob.Name);
                }
                else if (blob.BlobType == BlobType.PageBlob)
                {
                    SASblob = container.GetPageBlobReference(blob.Name);
                }
                else
                {
                    SASblob = container.GetAppendBlobReference(blob.Name);
                }
            }
            else
            {
                if (blob.BlobType == BlobType.BlockBlob)
                {
                    SASblob = new CloudBlockBlob(credentials.TransformUri(blob.Uri));
                }
                else if (blob.BlobType == BlobType.PageBlob)
                {
                    SASblob = new CloudPageBlob(credentials.TransformUri(blob.Uri));
                }
                else
                {
                    SASblob = new CloudAppendBlob(credentials.TransformUri(blob.Uri));
                }
            }

            HttpStatusCode failureCode = sasToken == null ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden;

            // We want to ensure that 'create', 'add', and 'write' permissions all allow for correct writing of blobs, as is reasonable.
            if (((permissions & SharedAccessBlobPermissions.Create) == SharedAccessBlobPermissions.Create) || ((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write))
            {
                if (blob.BlobType == BlobType.PageBlob)
                {
                    CloudPageBlob SASpageBlob = (CloudPageBlob)SASblob;
                    SASpageBlob.Create(512);
                    CloudPageBlob pageBlob = (CloudPageBlob)blob;
                    byte[] buffer = new byte[512];
                    buffer[0] = 2;  // random data

                    if (((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write))
                    {
                        SASpageBlob.UploadFromByteArray(buffer, 0, 512);
                    }
                    else
                    {
                        TestHelper.ExpectedException(
                            () => SASpageBlob.UploadFromByteArray(buffer, 0, 512),
                            "pageBlob SAS token without Write perms should not allow for writing/adding",
                            failureCode);
                        pageBlob.UploadFromByteArray(buffer, 0, 512);
                    }
                }
                else if (blob.BlobType == BlobType.BlockBlob)
                {
                    if ((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write)
                    {
                        UploadText(SASblob, "blob", Encoding.UTF8);
                    }
                    else
                    {
                        TestHelper.ExpectedException(
                            () => UploadText(SASblob, "blob", Encoding.UTF8),
                            "Block blob SAS token without Write or perms should not allow for writing",
                            failureCode);
                        UploadText(blob, "blob", Encoding.UTF8);
                    }
                }
                else // append blob
                {
                    // If the sas token contains Feb 2012, append won't be accepted 
                    if (sasToken.Contains(Constants.VersionConstants.February2012))
                    {
                        UploadText(blob, "blob", Encoding.UTF8);
                    }
                    else
                    {
                        CloudAppendBlob SASAppendBlob = SASblob as CloudAppendBlob;
                        SASAppendBlob.CreateOrReplace();

                        byte[] textAsBytes = Encoding.UTF8.GetBytes("blob");
                        using (MemoryStream stream = new MemoryStream())
                        {
                            stream.Write(textAsBytes, 0, textAsBytes.Length);
                            stream.Seek(0, SeekOrigin.Begin);

                            if (((permissions & SharedAccessBlobPermissions.Add) == SharedAccessBlobPermissions.Add) || ((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write))
                            {
                                SASAppendBlob.AppendBlock(stream, null);
                            }
                            else
                            {
                                TestHelper.ExpectedException(
                                    () => SASAppendBlob.AppendBlock(stream, null),
                                    "Append blob SAS token without Write or Add perms should not allow for writing/adding",
                                    failureCode);
                                stream.Seek(0, SeekOrigin.Begin);
                                ((CloudAppendBlob)blob).AppendBlock(stream, null);
                            }
                        }
                    }
                }
            }
            else
            {
                TestHelper.ExpectedException(
                        () => UploadText(SASblob, "blob", Encoding.UTF8),
                        "UploadText SAS does not allow for writing/adding",
                        ((blob.BlobType == BlobType.AppendBlob) && (sasToken != null) && (sasToken.Contains(Constants.VersionConstants.February2012))) ? HttpStatusCode.BadRequest : failureCode);
                UploadText(blob, "blob", Encoding.UTF8);
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
                        failureCode);
                }
            }

            // need to have written to the blob to read from it.
            if (((permissions & SharedAccessBlobPermissions.Read) == SharedAccessBlobPermissions.Read))
            {
                SASblob.FetchAttributes();

                // Test headers
                if (headers != null)
                {
                    if (headers.CacheControl != null)
                    {
                        Assert.AreEqual(headers.CacheControl, SASblob.Properties.CacheControl);
                    }

                    if (headers.ContentDisposition != null)
                    {
                        Assert.AreEqual(headers.ContentDisposition, SASblob.Properties.ContentDisposition);
                    }

                    if (headers.ContentEncoding != null)
                    {
                        Assert.AreEqual(headers.ContentEncoding, SASblob.Properties.ContentEncoding);
                    }

                    if (headers.ContentLanguage != null)
                    {
                        Assert.AreEqual(headers.ContentLanguage, SASblob.Properties.ContentLanguage);
                    }

                    if (headers.ContentType != null)
                    {
                        Assert.AreEqual(headers.ContentType, SASblob.Properties.ContentType);
                    }
                }
            }
            else
            {
                TestHelper.ExpectedException(
                    () => SASblob.FetchAttributes(),
                    "Fetch blob attributes while SAS does not allow for reading",
                    failureCode);
            }

            if ((permissions & SharedAccessBlobPermissions.Write) == SharedAccessBlobPermissions.Write)
            {
                SASblob.SetMetadata();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => SASblob.SetMetadata(),
                    "Set blob metadata while SAS does not allow for writing",
                    failureCode);
            }

            if ((permissions & SharedAccessBlobPermissions.Delete) == SharedAccessBlobPermissions.Delete)
            {
                SASblob.Delete();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => SASblob.Delete(),
                    "Delete blob while SAS does not allow for deleting",
                    failureCode);
            }
        }

        private static void TestBlobSAS(CloudBlob testBlob, SharedAccessBlobPermissions permissions, SharedAccessBlobHeaders headers)
        {
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = permissions,
            };

            string sasToken = testBlob.GetSharedAccessSignature(policy, headers, null);
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
                HttpStatusCode.Forbidden);

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");
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
            for (int i = 1; i < 0x40; i++)
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
                SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testBlockBlob);

                CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob" + i);
                SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testPageBlob);

                CloudAppendBlob testAppendBlob = this.testContainer.GetAppendBlobReference("appendblob" + i);
                SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testAppendBlob);
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

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");

            CloudAppendBlob testAppendBlob = this.testContainer.GetAppendBlobReference("appendblob");

            BlobContainerPermissions permissions = new BlobContainerPermissions();

            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            this.testContainer.SetPermissions(permissions);
            Thread.Sleep(35 * 1000);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testAppendBlob);

            permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            this.testContainer.SetPermissions(permissions);
            Thread.Sleep(35 * 1000);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob);
            SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testAppendBlob);
        }

        [TestMethod]
        [Description("Create client from storage account with anonymous creds.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobCreateClientWithAnonymousCreds()
        {
            CloudBlockBlob testBlockBlob = this.testContainer.GetBlockBlobReference("blockblob");
            UploadText(testBlockBlob, "blob", Encoding.UTF8);

            BlobContainerPermissions permissions = new BlobContainerPermissions();

            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            this.testContainer.SetPermissions(permissions);
            Thread.Sleep(35 * 1000);

            string blobUri = testBlockBlob.ServiceClient.BaseUri.AbsoluteUri;
            string accountString = "BlobEndpoint=" + blobUri;

            CloudStorageAccount acc = CloudStorageAccount.Parse(accountString);
            CloudBlobClient client = acc.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(this.testContainer.Name);
            container.ListBlobs().ToArray();
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

            CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob");

            CloudAppendBlob testAppendBlob = this.testContainer.GetAppendBlobReference("appendblob");

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

            sasToken = testAppendBlob.GetSharedAccessSignature(null, "testpolicy");
            SASTests.TestAccess(sasToken, policy.Permissions, null, null, testAppendBlob);

            sasToken = this.testContainer.GetSharedAccessSignature(null, "testpolicy");
            SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testBlockBlob);
            SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testPageBlob);
            SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testAppendBlob);
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobSASCombinations()
        {
            for (int i = 1; i < 0x40; i++)
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
            for (int i = 1; i < 0x40; i++)
            {
                CloudPageBlob testBlob = this.testContainer.GetPageBlobReference("blob" + i);
                SharedAccessBlobPermissions permissions = (SharedAccessBlobPermissions)i;
                TestBlobSAS(testBlob, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of blob permissions against an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSASCombinations()
        {
            for (int i = 1; i < 0x40; i++)
            {
                CloudAppendBlob testBlob = this.testContainer.GetAppendBlobReference("blob" + i);
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
            for (int i = 1; i < 0x40; i++)
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
            for (int i = 1; i < 0x40; i++)
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

        [TestMethod]
        [Description("Test all combinations of blob permissions against an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSASHeaders()
        {
            for (int i = 1; i < 0x40; i++)
            {
                CloudAppendBlob testBlob = this.testContainer.GetAppendBlobReference("blob" + i);
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
        [Description("Perform a SAS request and ensure that the api-version query param exists and the x-ms-version header does not.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSASApiVersionQueryParam()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                CloudBlob blob;

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                blockBlob.PutBlockList(new string[] { });

                CloudPageBlob pageBlob = container.GetPageBlobReference("pb");
                pageBlob.Create(0);

                CloudAppendBlob appendBlob = container.GetAppendBlobReference("ab");
                appendBlob.CreateOrReplace();

                string blockBlobToken = blockBlob.GetSharedAccessSignature(policy);
                StorageCredentials blockBlobSAS = new StorageCredentials(blockBlobToken);
                Uri blockBlobSASUri = blockBlobSAS.TransformUri(blockBlob.Uri);
                StorageUri blockBlobSASStorageUri = blockBlobSAS.TransformUri(blockBlob.StorageUri);

                string pageBlobToken = pageBlob.GetSharedAccessSignature(policy);
                StorageCredentials pageBlobSAS = new StorageCredentials(pageBlobToken);
                Uri pageBlobSASUri = pageBlobSAS.TransformUri(pageBlob.Uri);
                StorageUri pageBlobSASStorageUri = pageBlobSAS.TransformUri(pageBlob.StorageUri);

                string appendBlobToken = appendBlob.GetSharedAccessSignature(policy);
                StorageCredentials appendBlobSAS = new StorageCredentials(appendBlobToken);
                Uri appendBlobSASUri = appendBlobSAS.TransformUri(appendBlob.Uri);
                StorageUri appendBlobSASStorageUri = appendBlobSAS.TransformUri(appendBlob.StorageUri);

                OperationContext apiVersionCheckContext = new OperationContext();
                apiVersionCheckContext.SendingRequest += (sender, e) =>
                {
                    Assert.IsTrue(e.Request.RequestUri.Query.Contains("api-version"));
                };

                blob = new CloudBlob(blockBlobSASUri);
                blob.FetchAttributes(operationContext: apiVersionCheckContext);
                Assert.AreEqual(blob.BlobType, BlobType.BlockBlob);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = new CloudBlob(pageBlobSASUri);
                blob.FetchAttributes(operationContext: apiVersionCheckContext);
                Assert.AreEqual(blob.BlobType, BlobType.PageBlob);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = new CloudBlob(blockBlobSASStorageUri, null, null);
                blob.FetchAttributes(operationContext: apiVersionCheckContext);
                Assert.AreEqual(blob.BlobType, BlobType.BlockBlob);
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                blob = new CloudBlob(pageBlobSASStorageUri, null, null);
                blob.FetchAttributes(operationContext: apiVersionCheckContext);
                Assert.AreEqual(blob.BlobType, BlobType.PageBlob);
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Helper function for testing the IPAddressOrRange funcitonality for blobs
        /// </summary>
        /// <param name="generateInitialIPAddressOrRange">Function that generates an initial IPAddressOrRange object to use. This is expected to fail on the service.</param>
        /// <param name="generateFinalIPAddressOrRange">Function that takes in the correct IP address (according to the service) and returns the IPAddressOrRange object
        /// that should be accepted by the service</param>
        public void CloudBlobSASIPAddressHelper(Func<IPAddressOrRange> generateInitialIPAddressOrRange, Func<IPAddress, IPAddressOrRange> generateFinalIPAddressOrRange)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                CloudBlob blob;
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                byte[] data = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                blockBlob.UploadFromByteArray(data, 0, 4);

                // The plan then is to use an incorrect IP address to make a call to the service
                // ensure that we get an error message
                // parse the error message to get my actual IP (as far as the service sees)
                // then finally test the success case to ensure we can actually make requests

                IPAddressOrRange ipAddressOrRange = generateInitialIPAddressOrRange();
                string blockBlobToken = blockBlob.GetSharedAccessSignature(policy, null, null, null, ipAddressOrRange);
                StorageCredentials blockBlobSAS = new StorageCredentials(blockBlobToken);
                Uri blockBlobSASUri = blockBlobSAS.TransformUri(blockBlob.Uri);
                StorageUri blockBlobSASStorageUri = blockBlobSAS.TransformUri(blockBlob.StorageUri);

                blob = new CloudBlob(blockBlobSASUri);
                byte[] target = new byte[4];
                OperationContext opContext = new OperationContext();
                IPAddress actualIP = null;
                opContext.ResponseReceived += (sender, e) => 
                    {
                        Stream stream = e.Response.GetResponseStream();
                        stream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string text = reader.ReadToEnd();
                            XDocument xdocument = XDocument.Parse(text);
                            actualIP = IPAddress.Parse(xdocument.Descendants("SourceIP").First().Value);
                        }
                    };

                bool exceptionThrown = false;
                try
                {
                    blob.DownloadRangeToByteArray(target, 0, 0, 4, null, null, opContext);
                }
                catch (StorageException)
                {
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                ipAddressOrRange = generateFinalIPAddressOrRange(actualIP);
                blockBlobToken = blockBlob.GetSharedAccessSignature(policy, null, null, null, ipAddressOrRange);
                blockBlobSAS = new StorageCredentials(blockBlobToken);
                blockBlobSASUri = blockBlobSAS.TransformUri(blockBlob.Uri);
                blockBlobSASStorageUri = blockBlobSAS.TransformUri(blockBlob.StorageUri);


                blob = new CloudBlob(blockBlobSASUri);
                blob.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(data[i], target[i]);
                }
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = new CloudBlob(blockBlobSASStorageUri, null, null);
                blob.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(data[i], target[i]);
                }
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Perform a SAS request specifying an IP address or range and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSASIPAddressQueryParam()
        {
            CloudBlobSASIPAddressHelper(() =>
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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSASIPRangeQueryParam()
        {
            CloudBlobSASIPAddressHelper(() =>
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

        [TestMethod]
        [Description("Perform a SAS request specifying a shared protocol and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSASSharedProtocolsQueryParam()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                CloudBlob blob;
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                blockBlob.PutBlockList(new string[] { });

                foreach (SharedAccessProtocol? protocol in new SharedAccessProtocol?[] { null, SharedAccessProtocol.HttpsOrHttp, SharedAccessProtocol.HttpsOnly })
                {
                    string blockBlobToken = blockBlob.GetSharedAccessSignature(policy, null, null, protocol, null);
                    StorageCredentials blockBlobSAS = new StorageCredentials(blockBlobToken);
                    Uri blockBlobSASUri = new Uri(blockBlob.Uri + blockBlobSAS.SASToken);
                    StorageUri blockBlobSASStorageUri = new StorageUri(new Uri(blockBlob.StorageUri.PrimaryUri + blockBlobSAS.SASToken), new Uri(blockBlob.StorageUri.SecondaryUri + blockBlobSAS.SASToken));

                    int httpPort = blockBlobSASUri.Port;
                    int securePort = 443;

                    if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.BlobSecurePortOverride))
                    {
                        securePort = Int32.Parse(TestBase.TargetTenantConfig.BlobSecurePortOverride);
                    }

                    var schemesAndPorts = new[] {
                        new { scheme = Uri.UriSchemeHttp, port = httpPort},
                        new { scheme = Uri.UriSchemeHttps, port = securePort}
                    };
                    
                    foreach (var item in schemesAndPorts)
                    {
                        blockBlobSASUri = TransformSchemeAndPort(blockBlobSASUri, item.scheme, item.port);
                        blockBlobSASStorageUri = new StorageUri(TransformSchemeAndPort(blockBlobSASStorageUri.PrimaryUri, item.scheme, item.port), TransformSchemeAndPort(blockBlobSASStorageUri.SecondaryUri, item.scheme, item.port));

                        if (protocol.HasValue && protocol.Value == SharedAccessProtocol.HttpsOnly && string.CompareOrdinal(item.scheme, Uri.UriSchemeHttp) == 0)
                        {
                            blob = new CloudBlob(blockBlobSASUri);
                            TestHelper.ExpectedException(() => blob.FetchAttributes(), "Access a blob using SAS with a shared protocols that does not match", HttpStatusCode.Unused);

                            blob = new CloudBlob(blockBlobSASStorageUri, null, null);
                            TestHelper.ExpectedException(() => blob.FetchAttributes(), "Access a blob using SAS with a shared protocols that does not match", HttpStatusCode.Unused);
                        }
                        else
                        {
                            blob = new CloudBlob(blockBlobSASUri);
                            blob.FetchAttributes();
                            Assert.AreEqual(blob.BlobType, BlobType.BlockBlob);

                            blob = new CloudBlob(blockBlobSASStorageUri, null, null);
                            blob.FetchAttributes();
                            Assert.AreEqual(blob.BlobType, BlobType.BlockBlob);
                        }
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
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

        private static Uri TransformSchemeAndPort(Uri input, string scheme, int port)
        {
            UriBuilder builder = new UriBuilder(input);
            builder.Scheme = scheme;
            builder.Port = port;
            return builder.Uri;
        }
    }
}
