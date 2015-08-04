// <copyright file="FileSASTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Core;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileSASTests : FileTestBase
    {
        private CloudFileShare testShare;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testShare = GetRandomShareReference();
            this.testShare.Create();

            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testShare.Delete();
            this.testShare = null;
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        private static void TestAccess(string sasToken, SharedAccessFilePermissions permissions, SharedAccessFileHeaders headers, CloudFileShare share, CloudFile file)
        {
            StorageCredentials credentials = string.IsNullOrEmpty(sasToken) ?
                new StorageCredentials() :
                new StorageCredentials(sasToken);

            if (share != null)
            {
                share = new CloudFileShare(credentials.TransformUri(share.Uri));
                file = share.GetRootDirectoryReference().GetFileReference(file.Name);
            }
            else
            {
                file = new CloudFile(credentials.TransformUri(file.Uri));
            }

            if (share != null)
            {
                if ((permissions & SharedAccessFilePermissions.List) == SharedAccessFilePermissions.List)
                {
                    share.GetRootDirectoryReference().ListFilesAndDirectories().ToArray();
                }
                else
                {
                    TestHelper.ExpectedException(
                        () => share.GetRootDirectoryReference().ListFilesAndDirectories().ToArray(),
                        "List files while SAS does not allow for listing",
                        HttpStatusCode.NotFound);
                }
            }

            if ((permissions & SharedAccessFilePermissions.Read) == SharedAccessFilePermissions.Read)
            {
                file.FetchAttributes();
                
                // Test headers
                if (headers != null)
                {
                    if (headers.CacheControl != null)
                    {
                        Assert.AreEqual(headers.CacheControl, file.Properties.CacheControl);
                    }

                    if (headers.ContentDisposition != null)
                    {
                        Assert.AreEqual(headers.ContentDisposition, file.Properties.ContentDisposition);
                    }

                    if (headers.ContentEncoding != null)
                    {
                        Assert.AreEqual(headers.ContentEncoding, file.Properties.ContentEncoding);
                    }

                    if (headers.ContentLanguage != null)
                    {
                        Assert.AreEqual(headers.ContentLanguage, file.Properties.ContentLanguage);
                    }

                    if (headers.ContentType != null)
                    {
                        Assert.AreEqual(headers.ContentType, file.Properties.ContentType);
                    }
                }
            }
            else
            {
                TestHelper.ExpectedException(
                    () => file.FetchAttributes(),
                    "Fetch file attributes while SAS does not allow for reading",
                    HttpStatusCode.NotFound);
            }

            if ((permissions & SharedAccessFilePermissions.Write) == SharedAccessFilePermissions.Write)
            {
                file.SetMetadata();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => file.SetMetadata(),
                    "Set file metadata while SAS does not allow for writing",
                    HttpStatusCode.NotFound);
            }

            if ((permissions & SharedAccessFilePermissions.Delete) == SharedAccessFilePermissions.Delete)
            {
                file.Delete();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => file.Delete(),
                    "Delete file while SAS does not allow for deleting",
                    HttpStatusCode.NotFound);
            }
        }

        private static void TestFileSAS(CloudFile testFile, SharedAccessFilePermissions permissions, SharedAccessFileHeaders headers)
        {
            UploadText(testFile, "file", Encoding.UTF8);

            SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = permissions,
            };

            string sasToken = testFile.GetSharedAccessSignature(policy, headers, null);
            TestAccess(sasToken, permissions, headers, null, testFile);
        }

        [TestMethod]
        [Description("Test updateSASToken")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareUpdateSASToken()
        {
            // Create a policy with read/write access and get SAS.
            SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
            };
            string sasToken = this.testShare.GetSharedAccessSignature(policy);
            //Thread.Sleep(35000);
            CloudFile testFile = this.testShare.GetRootDirectoryReference().GetFileReference("file");
            UploadText(testFile, "file", Encoding.UTF8);
            TestAccess(sasToken, SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write, null, this.testShare, testFile);

            StorageCredentials creds = new StorageCredentials(sasToken);

            // Change the policy to only read and update SAS.
            SharedAccessFilePolicy policy2 = new SharedAccessFilePolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessFilePermissions.Read
            };
            string sasToken2 = this.testShare.GetSharedAccessSignature(policy2);
            creds.UpdateSASToken(sasToken2);

            // Extra check to make sure that we have actually updated the SAS token.
            CloudFileShare share = new CloudFileShare(this.testShare.Uri, creds);
            CloudFile testFile2 = share.GetRootDirectoryReference().GetFileReference("file2");

            TestHelper.ExpectedException(
                () => UploadText(testFile2, "file", Encoding.UTF8),
                "Writing to a file while SAS does not allow for writing",
                HttpStatusCode.NotFound);

            CloudFile testFile3 = this.testShare.GetRootDirectoryReference().GetFileReference("file3");
            testFile3.Create(0);
            TestAccess(sasToken2, SharedAccessFilePermissions.Read, null, this.testShare, testFile);
        }

        [TestMethod]
        [Description("Test all combinations of file permissions against a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSASCombinations()
        {
            for (int i = 1; i < 16; i++)
            {
                SharedAccessFilePermissions permissions = (SharedAccessFilePermissions)i;
                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = permissions,
                };
                string sasToken = this.testShare.GetSharedAccessSignature(policy);

                CloudFile testFile = this.testShare.GetRootDirectoryReference().GetFileReference("file" + i);
                UploadText(testFile, "file", Encoding.UTF8);
                FileSASTests.TestAccess(sasToken, permissions, null, this.testShare, testFile);
            }
        }

        [TestMethod]
        [Description("Test all combinations of file permissions against a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSASCombinations()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudFile testFile = this.testShare.GetRootDirectoryReference().GetFileReference("file" + i);
                SharedAccessFilePermissions permissions = (SharedAccessFilePermissions)i;
                TestFileSAS(testFile, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of file permissions against a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSASHeaders()
        {
            for (int i = 1; i < 8; i++)
            {
                CloudFile testFile = this.testShare.GetRootDirectoryReference().GetFileReference("file" + i);
                SharedAccessFilePermissions permissions = (SharedAccessFilePermissions)i;
                SharedAccessFileHeaders headers = new SharedAccessFileHeaders()
                {
                    CacheControl = "no-transform",
                    ContentDisposition = "attachment",
                    ContentEncoding = "gzip",
                    ContentLanguage = "tr,en",
                    ContentType = "text/html"
                };

                TestFileSAS(testFile, permissions, headers);
            }
        }

        [TestMethod]
        [Description("Test SAS against a file directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectorySAS()
        {
            CloudFileDirectory dir = this.testShare.GetRootDirectoryReference().GetDirectoryReference("dirfile");
            CloudFile file = dir.GetFileReference("dirfile");

            dir.Create();
            file.Create(512);

            SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.List
            };

            string sasToken = file.GetSharedAccessSignature(policy);
            CloudFileDirectory sasDir = new CloudFileDirectory(new Uri(dir.Uri.AbsoluteUri + sasToken));
            TestHelper.ExpectedException(
                () => sasDir.FetchAttributes(),
                "Fetching attributes of a directory using a file SAS should fail",
                HttpStatusCode.Forbidden);

            sasToken = this.testShare.GetSharedAccessSignature(policy);
            sasDir = new CloudFileDirectory(new Uri(dir.Uri.AbsoluteUri + sasToken));
            sasDir.FetchAttributes();
        }
    }
}
