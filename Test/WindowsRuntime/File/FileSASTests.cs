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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Core;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileSASTests : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public FileSASTests()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif
        private CloudFileShare testShare;

        [TestInitialize]
        public void MyTestInitialize()
        {
            this.testShare = GetRandomShareReference();
            this.testShare.CreateAsync().Wait();

            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            this.testShare.DeleteAsync().Wait();
            this.testShare = null;
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        private static async Task TestAccess(string sasToken, SharedAccessFilePermissions permissions, SharedAccessFileHeaders headers, CloudFileShare share, CloudFile file)
        {
            CloudFileShare SASshare = null;
            CloudFile SASfile;
            OperationContext context = new OperationContext();
            StorageCredentials credentials = string.IsNullOrEmpty(sasToken) ?
                new StorageCredentials() :
                new StorageCredentials(sasToken);
            string fileText = "file";

            if (share != null)
            {
                SASshare = new CloudFileShare(credentials.TransformUri(share.Uri));
                SASfile = SASshare.GetRootDirectoryReference().GetFileReference(file.Name);
            }
            else
            {
                SASfile = new CloudFile(credentials.TransformUri(file.Uri));
            }

            if ((permissions & SharedAccessFilePermissions.Write) == SharedAccessFilePermissions.Write)
            {
                await UploadTextAsync(SASfile, fileText, Encoding.UTF8);
            }
            else if ((permissions & SharedAccessFilePermissions.Create) == SharedAccessFilePermissions.Create)
            {
                await SASfile.CreateAsync(Encoding.UTF8.GetBytes(fileText).Length);
                context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await UploadTextAsync(SASfile, fileText, Encoding.UTF8, operationContext:context),
                    context,
                    "UploadText SAS does not allow for writing",
                    HttpStatusCode.Forbidden,
                    "");
                await UploadTextAsync(file, fileText, Encoding.UTF8);

            }
            else
            {
                context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                        async () => await SASfile.CreateAsync(Encoding.UTF8.GetBytes(fileText).Length, null /* accessCondition */, null /* options */, context),
                        context,
                        "Create file succeeded but SAS does not allow for writing/creating",
                        HttpStatusCode.Forbidden,
                        "");
                context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await UploadTextAsync(SASfile, fileText, Encoding.UTF8, operationContext:context),
                        context,
                        "UploadText SAS does not allow for writing/creating",
                        HttpStatusCode.Forbidden,
                        "");
                await UploadTextAsync(file, fileText, Encoding.UTF8);
            }

            if (SASshare != null)
            {
                if ((permissions & SharedAccessFilePermissions.List) == SharedAccessFilePermissions.List)
                {
                    var results = await SASshare.GetRootDirectoryReference().ListFilesAndDirectoriesSegmentedAsync(null);
                }
                else
                {
                    context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => { var results = await SASshare.GetRootDirectoryReference().ListFilesAndDirectoriesSegmentedAsync(null /* maxResults */, null /* currentToken */, null /* options */, context); },
                        context,
                        "List files while SAS does not allow for listing",
                        HttpStatusCode.Forbidden,
                        "");
                }
            }

            if ((permissions & SharedAccessFilePermissions.Read) == SharedAccessFilePermissions.Read)
            {
                await SASfile.FetchAttributesAsync();

                // Test headers
                if (headers != null)
                {
                    if (headers.CacheControl != null)
                    {
                        Assert.AreEqual(headers.CacheControl, SASfile.Properties.CacheControl);
                    }

                    if (headers.ContentDisposition != null)
                    {
                        Assert.AreEqual(headers.ContentDisposition, SASfile.Properties.ContentDisposition);
                    }

                    if (headers.ContentEncoding != null)
                    {
                        Assert.AreEqual(headers.ContentEncoding, SASfile.Properties.ContentEncoding);
                    }

                    if (headers.ContentLanguage != null)
                    {
                        Assert.AreEqual(headers.ContentLanguage, SASfile.Properties.ContentLanguage);
                    }

                    if (headers.ContentType != null)
                    {
                        Assert.AreEqual(headers.ContentType, SASfile.Properties.ContentType);
                    }
                }
            }
            else
            {
                context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await SASfile.FetchAttributesAsync(null /* accessCondition */, null /* options */, context),
                    context,
                    "Fetch file attributes while SAS does not allow for reading",
                    HttpStatusCode.Forbidden,
                    "");
            }

            if ((permissions & SharedAccessFilePermissions.Write) == SharedAccessFilePermissions.Write)
            {
                await SASfile.SetMetadataAsync();
            }
            else
            {
                context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await SASfile.SetMetadataAsync(null /* accessCondition */, null /* options */, context),
                    context,
                    "Set file metadata while SAS does not allow for writing",
                    HttpStatusCode.Forbidden,
                    "");
            }

            if ((permissions & SharedAccessFilePermissions.Delete) == SharedAccessFilePermissions.Delete)
            {
                await SASfile.DeleteAsync();
            }
            else
            {
                context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await SASfile.DeleteAsync(null /* accessCondition */, null /* options */, context),
                    context,
                    "Delete file while SAS does not allow for deleting",
                    HttpStatusCode.Forbidden,
                    "");
            }
        }

        private static async Task TestFileSAS(CloudFile testFile, SharedAccessFilePermissions permissions, SharedAccessFileHeaders headers)
        {
            SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = permissions,
            };

            string sasToken = testFile.GetSharedAccessSignature(policy, headers, null);
            await TestAccess(sasToken, permissions, headers, null, testFile);
        }

        [TestMethod]
        [Description("Test updateSASToken")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareUpdateSASTokenAsync()
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
            await UploadTextAsync(testFile, "file", Encoding.UTF8);
            await TestAccess(sasToken, SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write, null, this.testShare, testFile);

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

            OperationContext context = new OperationContext();
            await TestHelper.ExpectedExceptionAsync(
                async () => await UploadTextAsync(testFile2, "file", Encoding.UTF8, operationContext:context),
                context,
                "Writing to a file while SAS does not allow for writing",
                HttpStatusCode.Forbidden,
                "");

            CloudFile testFile3 = this.testShare.GetRootDirectoryReference().GetFileReference("file3");
            await testFile3.CreateAsync(0);
            await TestAccess(sasToken2, SharedAccessFilePermissions.Read, null, this.testShare, testFile);
        }

        [TestMethod]
        [Description("Test all combinations of file permissions against a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareSASCombinationsAsync()
        {
            for (int i = 1; i < 0x20; i++)
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
                await TestAccess(sasToken, permissions, null, this.testShare, testFile);
            }
        }

        [TestMethod]
        [Description("Test all combinations of file permissions against a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSASCombinationsAsync()
        {
            for (int i = 1; i < 0x20; i++)
            {
                CloudFile testFile = this.testShare.GetRootDirectoryReference().GetFileReference("file" + i);
                SharedAccessFilePermissions permissions = (SharedAccessFilePermissions)i;
                await TestFileSAS(testFile, permissions, null);
            }
        }

        [TestMethod]
        [Description("Test all combinations of file permissions against a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSASHeadersAsync()
        {
            for (int i = 1; i < 0x20; i++)
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

                await TestFileSAS(testFile, permissions, headers);
            }
        }

        [TestMethod]
        [Description("Test SAS against a file directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectorySASAsync()
        {
            CloudFileDirectory dir = this.testShare.GetRootDirectoryReference().GetDirectoryReference("dirfile");
            CloudFile file = dir.GetFileReference("dirfile");

            await dir.CreateAsync();
            await file.CreateAsync(512);

            SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.List
            };

            string sasToken = file.GetSharedAccessSignature(policy);
            CloudFileDirectory sasDir = new CloudFileDirectory(new Uri(dir.Uri.AbsoluteUri + sasToken));
            OperationContext context = new OperationContext();
            await TestHelper.ExpectedExceptionAsync(
                async () => await sasDir.FetchAttributesAsync(null /* accessCondition */, null /* options */, context),
                context,
                "Fetching attributes of a directory using a file SAS should fail",
                HttpStatusCode.Forbidden,
                "");

            sasToken = this.testShare.GetSharedAccessSignature(policy);
            sasDir = new CloudFileDirectory(new Uri(dir.Uri.AbsoluteUri + sasToken));
            await sasDir.FetchAttributesAsync();
        }

        [TestMethod]
        [Description("Perform a SAS request specifying a shared protocol and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSASSharedProtocolsQueryParamAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file;
                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudFile fileWithKey = share.GetRootDirectoryReference().GetFileReference("filefile");
                byte[] data = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                byte[] target = new byte[4];
                await fileWithKey.UploadFromByteArrayAsync(data, 0, 4);

                foreach (SharedAccessProtocol? protocol in new SharedAccessProtocol?[] { null, SharedAccessProtocol.HttpsOrHttp, SharedAccessProtocol.HttpsOnly })
                {
                    string fileToken = fileWithKey.GetSharedAccessSignature(policy, null, null, protocol, null);
                    StorageCredentials fileSAS = new StorageCredentials(fileToken);
                    Uri fileSASUri = new Uri(fileWithKey.Uri + fileSAS.SASToken);
                    StorageUri fileSASStorageUri = new StorageUri(new Uri(fileWithKey.StorageUri.PrimaryUri + fileSAS.SASToken), new Uri(fileWithKey.StorageUri.SecondaryUri + fileSAS.SASToken));

                    int httpPort = fileSASUri.Port;
                    int securePort = 443;

                    if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.FileSecurePortOverride))
                    {
                        securePort = Int32.Parse(TestBase.TargetTenantConfig.FileSecurePortOverride);
                    }

                    var schemesAndPorts = new[] {
                        new { scheme = "HTTP", port = httpPort},
                        new { scheme = "HTTPS", port = securePort}
                    };

                    foreach (var item in schemesAndPorts)
                    {
                        fileSASUri = TransformSchemeAndPort(fileSASUri, item.scheme, item.port);
                        fileSASStorageUri = new StorageUri(TransformSchemeAndPort(fileSASStorageUri.PrimaryUri, item.scheme, item.port), TransformSchemeAndPort(fileSASStorageUri.SecondaryUri, item.scheme, item.port));

                        if (protocol.HasValue && protocol == SharedAccessProtocol.HttpsOnly && string.CompareOrdinal(item.scheme, "HTTP") == 0)
                        {
                            file = new CloudFile(fileSASUri);
                            OperationContext context = new OperationContext();
                            await TestHelper.ExpectedExceptionAsync(
                                async () => await file.FetchAttributesAsync(null /* accessCondition */, null /* options */, context),
                                context,
                                "Access a file using SAS with a shared protocols that does not match",
                                HttpStatusCode.Unused,
                                "");

                            file = new CloudFile(fileSASStorageUri, null);
                            context = new OperationContext();
                            await TestHelper.ExpectedExceptionAsync(
                                async () => await file.FetchAttributesAsync(null /* accessCondition */, null /* options */, context),
                                context,
                                "Access a file using SAS with a shared protocols that does not match",
                                HttpStatusCode.Unused,
                                "");
                        }
                        else
                        {
                            file = new CloudFile(fileSASUri);
                            await file.DownloadRangeToByteArrayAsync(target, 0, 0, 4, null, null, null);
                            for (int i = 0; i < 4; i++)
                            {
                                Assert.AreEqual(data[i], target[i]);
                            }

                            file = new CloudFile(fileSASStorageUri, null);
                            await file.DownloadRangeToByteArrayAsync(target, 0, 0, 4, null, null, null);
                            for (int i = 0; i < 4; i++)
                            {
                                Assert.AreEqual(data[i], target[i]);
                            }
                        }
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
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
