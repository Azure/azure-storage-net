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
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.Azure.Storage.Core;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace Microsoft.Azure.Storage.Blob
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
        public async Task CloudBlobContainerSASCombinations()
        {
            List<Task> tasks = new List<Task>();
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
                tasks.Add(Task.Run(() => SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testBlockBlob)));

                CloudPageBlob testPageBlob = this.testContainer.GetPageBlobReference("pageblob" + i);
                tasks.Add(Task.Run(() => SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testPageBlob)));

                CloudAppendBlob testAppendBlob = this.testContainer.GetAppendBlobReference("appendblob" + i);
                tasks.Add(Task.Run(() => SASTests.TestAccess(sasToken, permissions, null, this.testContainer, testAppendBlob)));

                // Limit the number of parallel tasks to 90
                while (tasks.Count > 50)
                {
                    Task t = await Task.WhenAny(tasks);
                    await t;
                    tasks.Remove(t);
                }
            }
            Task.WaitAll(tasks.ToArray());
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
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob));
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob));
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(null, SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, null, this.testContainer, testAppendBlob));

            permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            this.testContainer.SetPermissions(permissions);
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testBlockBlob));
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testPageBlob));
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(null, SharedAccessBlobPermissions.Read, null, this.testContainer, testAppendBlob));
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

            string blobUri = testBlockBlob.ServiceClient.BaseUri.AbsoluteUri;
            string accountString = "BlobEndpoint=" + blobUri;

            CloudStorageAccount acc = CloudStorageAccount.Parse(accountString);
            CloudBlobClient client = acc.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(this.testContainer.Name);
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => container.ListBlobs().ToArray());
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

            string sasToken = testBlockBlob.GetSharedAccessSignature(null, "testpolicy");
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(sasToken, policy.Permissions, null, null, testBlockBlob));

            sasToken = testPageBlob.GetSharedAccessSignature(null, "testpolicy");
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(sasToken, policy.Permissions, null, null, testPageBlob));

            sasToken = testAppendBlob.GetSharedAccessSignature(null, "testpolicy");
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(sasToken, policy.Permissions, null, null, testAppendBlob));

            sasToken = this.testContainer.GetSharedAccessSignature(null, "testpolicy");
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testBlockBlob));
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testPageBlob));
            TestHelper.SpinUpTo30SecondsIgnoringFailures(() => SASTests.TestAccess(sasToken, policy.Permissions, null, this.testContainer, testAppendBlob));
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
        [Description("Test creation of blob snapshot SAS and whether it can deliver a proper CloudBlob snapshot.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSnapshotSAS()
        {
            var blob2snap = this.testContainer.GetBlockBlobReference("blob--" + Guid.NewGuid());
            blob2snap.UploadText("placeholder");
            var snap = blob2snap.Snapshot();

            SharedAccessBlobPolicy genericSASPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(10),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Delete
            };
            var uri = snap.SnapshotQualifiedUri + snap.GetSharedAccessSignature(genericSASPolicy).Replace('?', '&');

            var sasSnap = new CloudBlockBlob(new Uri(uri));

            Assert.IsTrue(sasSnap.IsSnapshot, "CloudBlob made from snapshot SAS is not a snapshot.");
            Assert.IsNotNull(sasSnap.SnapshotTime, "CloudBlob made from snapshot SAS has no snapshot time.");
            Assert.IsTrue(sasSnap.DownloadText() == "placeholder"); // the actual REST interaction and validating data
            sasSnap.Delete();
            Assert.IsFalse(sasSnap.Exists(), "Blob snapshot SAS was unable to delete the snapshot.");


            // a URI for a blob but with a snapshot SAS token
            var badUri = blob2snap.Uri + snap.GetSharedAccessSignature(genericSASPolicy);
            var badSasSnap = new CloudBlockBlob(new Uri(badUri));

            TestHelper.ExpectedException(
                () => badSasSnap.DownloadText(),
                "Attempt to download text without the appropriate SAS.",
                HttpStatusCode.Forbidden
                );
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

                blob = new CloudBlob(blockBlobSASStorageUri, null, credentials:null);
                blob.FetchAttributes(operationContext: apiVersionCheckContext);
                Assert.AreEqual(blob.BlobType, BlobType.BlockBlob);
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                blob = new CloudBlob(pageBlobSASStorageUri, null, credentials: null);
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

                bool exceptionThrown = false;
                try
                {
                    blob.DownloadRangeToByteArray(target, 0, 0, 4, null, null, opContext);
                }
                catch (StorageException)
                {
                    exceptionThrown = true;
                    //The IP should not be included in the error details for security reasons
                    Assert.IsNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                ipAddressOrRange = null;
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

                blob = new CloudBlob(blockBlobSASStorageUri, null, credentials: null);
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

                    int securePort = 443;
                    int httpPort = (blockBlobSASUri.Port == securePort) ? 80 : blockBlobSASUri.Port;

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

                            blob = new CloudBlob(blockBlobSASStorageUri, null, credentials: null);
                            TestHelper.ExpectedException(() => blob.FetchAttributes(), "Access a blob using SAS with a shared protocols that does not match", HttpStatusCode.Unused);
                        }
                        else
                        {
                            blob = new CloudBlob(blockBlobSASUri);
                            blob.FetchAttributes();
                            Assert.AreEqual(blob.BlobType, BlobType.BlockBlob);

                            blob = new CloudBlob(blockBlobSASStorageUri, null, credentials:null);
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

        private CloudBlobClient GetOAuthClient()
        {
            TokenCredential tokenCredential = new TokenCredential(GenerateOAuthToken());
            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);

            Uri endpoint = new Uri(TargetTenantConfig.BlobServiceEndpoint);
            return new CloudBlobClient(endpoint, storageCredentials);
        }

        [TestMethod]
        [Description("Get a user delegation key")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetUserDelegationKey()
        {
            CloudBlobClient client = GetOAuthClient();

            var start = DateTimeOffset.Now.AddMinutes(-10);
            var end = DateTimeOffset.Now.AddMinutes(10);

            var options = new BlobRequestOptions();
            options.UseTransactionalMD5 = true;

            var key = client.GetUserDelegationKey(start, end, options: options);

            Assert.IsNotNull(key);
            Assert.IsNotNull(key.SignedOid);
            Assert.IsNotNull(key.SignedTid);
            Assert.IsNotNull(key.SignedStart);
            Assert.IsNotNull(key.SignedExpiry);
            Assert.IsNotNull(key.SignedVersion);
            Assert.IsNotNull(key.SignedService);
            Assert.IsNotNull(key.Value);
        }

        [TestMethod]
        [Description("Get a user delegation key")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetUserDelegationKeyAPM()
        {
            CloudBlobClient client = GetOAuthClient();

            var start = DateTimeOffset.Now.AddMinutes(-10);
            var end = DateTimeOffset.Now.AddMinutes(10);

            UserDelegationKey key;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                var result = client.BeginGetUserDelegationKey(start, end, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                key = client.EndGetUserDelegationKey(result);
            }

            Assert.IsNotNull(key);
            Assert.IsNotNull(key.SignedOid);
            Assert.IsNotNull(key.SignedTid);
            Assert.IsNotNull(key.SignedStart);
            Assert.IsNotNull(key.SignedExpiry);
            Assert.IsNotNull(key.SignedVersion);
            Assert.IsNotNull(key.SignedService);
            Assert.IsNotNull(key.Value);
        }

        [TestMethod]
        [Description("Get a user delegation key")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientGetUserDelegationKeyTaskAsync()
        {
            CloudBlobClient client = GetOAuthClient();

            var start = DateTimeOffset.Now.AddMinutes(-10);
            var end = DateTimeOffset.Now.AddMinutes(10);

            var key = await client.GetUserDelegationKeyAsync(start, end);

            Assert.IsNotNull(key);
            Assert.IsNotNull(key.SignedOid);
            Assert.IsNotNull(key.SignedTid);
            Assert.IsNotNull(key.SignedStart);
            Assert.IsNotNull(key.SignedExpiry);
            Assert.IsNotNull(key.SignedVersion);
            Assert.IsNotNull(key.SignedService);
            Assert.IsNotNull(key.Value);
        }

        [TestMethod]
        [Description("Assign various SAS tokens using Active Directory, rather than storage keys.")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobUserDelegationSAS()
        {
            CloudBlobClient client = GetOAuthClient();

            var start = DateTimeOffset.Now.AddMinutes(-10);
            var end = DateTimeOffset.Now.AddMinutes(10);
            var key = client.GetUserDelegationKey(start, end);
            var key2 = client.GetUserDelegationKey(start, end);

            Assert.AreEqual(key.SignedOid, key2.SignedOid, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedTid, key2.SignedTid, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedStart, key2.SignedStart, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedExpiry, key2.SignedExpiry, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedService, key2.SignedService, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedVersion, key2.SignedVersion, "Failed to request the same key twice.");
            Assert.AreEqual(key.Value, key2.Value, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedStart.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), start.UtcDateTime.ToString(Constants.DateTimeFormatter),
                string.Format(CultureInfo.InvariantCulture, "Start times do not equal. {0} != {1}.",
                    key.SignedStart.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), start.UtcDateTime.ToString(Constants.DateTimeFormatter)));
            Assert.AreEqual(key.SignedExpiry.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), end.UtcDateTime.ToString(Constants.DateTimeFormatter),
                string.Format(CultureInfo.InvariantCulture, "End times do not equal. {0} != {1}.",
                    key.SignedExpiry.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), end.UtcDateTime.ToString(Constants.DateTimeFormatter)));

            const string data = "placeholder";
            const string newData = "additional placeholder";
            var blob = testContainer.GetBlockBlobReference("testblob--" + Guid.NewGuid());
            blob.UploadText(data);
            var sasBlob = new CloudBlockBlob(new Uri(blob.Uri + blob.GetUserDelegationSharedAccessSignature(key, new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.Now.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(5),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            })));

            Assert.AreEqual(sasBlob.DownloadText(), data, "SAS failed to download the correct data.");
            sasBlob.UploadText(newData);
            Assert.AreEqual(sasBlob.DownloadText(), newData, "SAS failed to upload new data.");
        }

        [TestMethod]
        [Description("Assign various SAS tokens using Active Directory, rather than storage keys.")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobUserDelegationSASBadKey()
        {
            var oid = Guid.NewGuid();
            var tid = Guid.NewGuid();
            var start = DateTimeOffset.Now.AddMinutes(-5);
            var end = start.AddMinutes(10);
            var service = "b";
            var version = Constants.HeaderConstants.TargetStorageVersion;
            byte[] bytes = new byte[32];
            new Random().NextBytes(bytes);
            var value = Convert.ToBase64String(bytes);
            var keys = new List<UserDelegationKey>()
            {
                new UserDelegationKey()
                { SignedOid = null, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = null, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = null, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = null, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = null, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = null, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = null },
            };

            var policy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = end,
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };

            const string data = "placeholder";
            var blob = testContainer.GetBlockBlobReference("testblob--" + Guid.NewGuid());
            blob.UploadText(data);

            foreach (var key in keys)
            {
                TestHelper.ExpectedException<ArgumentNullException>(() => blob.GetUserDelegationSharedAccessSignature(key, policy), "Create an IDSAS.");
            }
        }

        [TestMethod]
        [Description("Assign various SAS tokens using Active Directory, rather than storage keys.")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerUserDelegationSAS()
        {
            CloudBlobClient client = GetOAuthClient();

            var start = DateTimeOffset.Now.AddMinutes(-10);
            var end = DateTimeOffset.Now.AddMinutes(10);
            var key = client.GetUserDelegationKey(start, end);
            var key2 = client.GetUserDelegationKey(start, end);

            Assert.AreEqual(key.SignedOid, key2.SignedOid, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedTid, key2.SignedTid, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedStart, key2.SignedStart, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedExpiry, key2.SignedExpiry, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedService, key2.SignedService, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedVersion, key2.SignedVersion, "Failed to request the same key twice.");
            Assert.AreEqual(key.Value, key2.Value, "Failed to request the same key twice.");
            Assert.AreEqual(key.SignedStart.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), start.UtcDateTime.ToString(Constants.DateTimeFormatter),
                string.Format(CultureInfo.InvariantCulture, "Start times do not equal. {0} != {1}.",
                    key.SignedStart.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), start.UtcDateTime.ToString(Constants.DateTimeFormatter)));
            Assert.AreEqual(key.SignedExpiry.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), end.UtcDateTime.ToString(Constants.DateTimeFormatter),
                string.Format(CultureInfo.InvariantCulture, "End times do not equal. {0} != {1}.",
                    key.SignedExpiry.Value.UtcDateTime.ToString(Constants.DateTimeFormatter), end.UtcDateTime.ToString(Constants.DateTimeFormatter)));


            var blobName = "blob--" + Guid.NewGuid().ToString();
            var blobData = "placeholder";
            var container = GetRandomContainerReference();
            container.CreateIfNotExists();
            container.GetBlockBlobReference(blobName).UploadText(blobData);

            var sasContainer = new CloudBlobContainer(new Uri(container.Uri + container.GetUserDelegationSharedAccessSignature(key, new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.Now.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(5),
                Permissions = SharedAccessBlobPermissions.List
            })));

            // successfully read a list of blobs with the sas
            Assert.IsTrue(sasContainer.ListBlobs().Where(item => item.Uri.ToString().Contains(blobName)).Count() > 0);
        }

        [TestMethod]
        [Description("Assign various SAS tokens using Active Directory, rather than storage keys.")]
        [TestCategory(ComponentCategory.Auth)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerUserDelegationSASBadKey()
        {
            var oid = Guid.NewGuid();
            var tid = Guid.NewGuid();
            var start = DateTimeOffset.Now.AddMinutes(-5);
            var end = start.AddMinutes(10);
            var service = "b";
            var version = Constants.HeaderConstants.TargetStorageVersion;
            byte[] bytes = new byte[32];
            new Random().NextBytes(bytes);
            var value = Convert.ToBase64String(bytes);
            var keys = new List<UserDelegationKey>()
            {
                new UserDelegationKey()
                { SignedOid = null, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = null, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = null, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = null, SignedService = service, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = null, SignedVersion = version, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = null, Value = value },
                new UserDelegationKey()
                { SignedOid = oid, SignedTid = tid, SignedStart = start, SignedExpiry = end, SignedService = service, SignedVersion = version, Value = null },
            };

            var policy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = end,
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };

            var container = GetRandomContainerReference();
            container.CreateIfNotExists();

            foreach (var key in keys)
            {
                TestHelper.ExpectedException<ArgumentNullException>(() => container.GetUserDelegationSharedAccessSignature(key, policy), "Create an IDSAS.");
            }
        }

        [TestMethod]
        [Description("Demo creating and using a stored access policy, and verify that everything works correctly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ContainerStoredPolicyTestSample()
        {
            string expectedContent = "sample content";
            string blobName = "testblob";
            string policyName = "policy";

            // Create a test blob in the container
            CloudBlockBlob blob = this.testContainer.GetBlockBlobReference(blobName);
            blob.UploadText(expectedContent);

            CloudBlobContainer containerWithSharedKey = testContainer;
            {
                #region sample_CloudBlobContainer_GetSetPermissions

                // If we want to set the permissions on a container, first we should get the existing permissions.
                // This is important, because "SetPermissions" uses "replace" semantics, not "merge" semantics.
                // If we skipped this step and just created a new BlobContainerPermissions object locally, 
                // any existing policies would be deleted.
                BlobContainerPermissions permissions = containerWithSharedKey.GetPermissions();

                // Create a policy with read access.
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessBlobPermissions.Read
                };

                // Once uploaded, these permissions will allow SAS tokens created with the named policy
                // to read from the container for 30 minutes, as specified in the policy.
                // This only applies to SAS tokens created referencing this specific policy name on this specific container.
                permissions.SharedAccessPolicies[policyName] = policy;

                // This call actually uploads the permissions to the Azure Storage Service.
                // Note that this can take up to 30 seconds after the call completes to take affect.
                containerWithSharedKey.SetPermissions(permissions);
                #endregion
            }

            Uri containerUri = this.testContainer.Uri;
            Uri blobUri = blob.Uri;

            string accountName = containerWithSharedKey.ServiceClient.Credentials.AccountName;
            {
                #region sample_CloudBlobContainer_GetSharedAccessSignatureWithNamedPolicy
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    // As we are using a stored access policy, in this case we are adding no additional restrictions to the SAS token,
                    // other than what is already specified in the stored access policy.
                };

                // sasToken will be something like:
                // ?sv=2015-04-05&sr=c&si=mypolicyname&sig=Z%2FRHIX5Xcg0Mq2rqI3OlWTjEg2tYkboXr1P9ZUXDtkk%3D
                // This string can used as the query string of a URI of this container, or a blob in this container.
                // It can also be used as the "sasToken" parameter to the constructor of a StorageCredentials object, as shown here.
                string sasToken = containerWithSharedKey.GetSharedAccessSignature(policy, policyName);

                StorageCredentials credentialsWithSAS = new StorageCredentials(sasToken);
                StorageUri storageUri = new StorageUri(containerUri);
                CloudBlobContainer containerWithSAS = new CloudBlobContainer(storageUri, credentialsWithSAS);
                CloudBlockBlob blobWithSAS = containerWithSAS.GetBlockBlobReference(blobName);

                #endregion

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    Assert.AreEqual(expectedContent, blobWithSAS.DownloadText());
                });
            }
        }

        private static Uri TransformSchemeAndPort(Uri input, string scheme, int port)
        {
            UriBuilder builder = new UriBuilder(input);
            builder.Scheme = scheme;
            builder.Port = port;
            return builder.Uri;
        }

        [TestMethod]
        [Description("Perform a SAS request specifying parameters unkown to the service.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobSASUnknownParams()
        {
           CloudBlobContainer container = GetRandomContainerReference();
           try
           {
               container.Create();

               // Create Source on server
               CloudBlockBlob blob = container.GetBlockBlobReference("source");

               string data = "String data";
               UploadText(blob, data, Encoding.UTF8);

               UriBuilder blobURIBuilder = new UriBuilder(blob.Uri);
               blobURIBuilder.Query = "MyQuery=value&YOURQUERY=value2"; // Add the query params unknown to the service

               // Source SAS must have read permissions
               SharedAccessBlobPermissions permissions = SharedAccessBlobPermissions.Read;
               SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
               {
                   SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                   SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                   Permissions = permissions,
               };
               string sasToken = blob.GetSharedAccessSignature(policy);

               // Replace one of the SAS keys with a capitalized version to ensure we are case-insensitive on expected parameter keys as well
               StorageCredentials credentials = new StorageCredentials(sasToken);
               StringBuilder sasString = new StringBuilder(credentials.TransformUri(blobURIBuilder.Uri).ToString());
               sasString.Replace("sp=", "SP=");
               CloudBlockBlob sasBlob = new CloudBlockBlob(new Uri(sasString.ToString()));

               // Validate that we can fetch the attributes on the blob (no exception thrown)
               sasBlob.FetchAttributes();
           }
           finally
           {
               container.DeleteIfExists();
           }
        }
    }
}
