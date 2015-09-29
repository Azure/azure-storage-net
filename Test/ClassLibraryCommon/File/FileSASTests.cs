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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Core;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.IO;

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
            CloudFileShare SASshare = null;
            CloudFile SASfile;
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
                UploadText(SASfile, fileText, Encoding.UTF8);
            }
            else if ((permissions & SharedAccessFilePermissions.Create) == SharedAccessFilePermissions.Create)
            {
                SASfile.Create(Encoding.UTF8.GetBytes(fileText).Length);
                TestHelper.ExpectedException(
                    () => UploadText(SASfile, fileText, Encoding.UTF8),
                    "UploadText SAS does not allow for writing",
                    HttpStatusCode.Forbidden);
                UploadText(file, fileText, Encoding.UTF8);

            }
            else 
            {
                TestHelper.ExpectedException(
                        () => SASfile.Create(Encoding.UTF8.GetBytes(fileText).Length),
                        "Create file succeeded but SAS does not allow for writing/creating",
                        HttpStatusCode.Forbidden);
                TestHelper.ExpectedException(
                        () => UploadText(SASfile, fileText, Encoding.UTF8),
                        "UploadText SAS does not allow for writing/creating",
                        HttpStatusCode.Forbidden);
                UploadText(file, fileText, Encoding.UTF8);
            }

            if (SASshare != null)
            {
                if ((permissions & SharedAccessFilePermissions.List) == SharedAccessFilePermissions.List)
                {
                    SASshare.GetRootDirectoryReference().ListFilesAndDirectories().ToArray();
                }
                else
                {
                    TestHelper.ExpectedException(
                        () => SASshare.GetRootDirectoryReference().ListFilesAndDirectories().ToArray(),
                        "List files while SAS does not allow for listing",
                        HttpStatusCode.Forbidden);
                }
            }

            if ((permissions & SharedAccessFilePermissions.Read) == SharedAccessFilePermissions.Read)
            {
                SASfile.FetchAttributes();
                
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
                TestHelper.ExpectedException(
                    () => SASfile.FetchAttributes(),
                    "Fetch file attributes while SAS does not allow for reading",
                    HttpStatusCode.Forbidden);
            }

            if ((permissions & SharedAccessFilePermissions.Write) == SharedAccessFilePermissions.Write)
            {
                SASfile.SetMetadata();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => SASfile.SetMetadata(),
                    "Set file metadata while SAS does not allow for writing",
                    HttpStatusCode.Forbidden);
            }

            if ((permissions & SharedAccessFilePermissions.Delete) == SharedAccessFilePermissions.Delete)
            {
                SASfile.Delete();
            }
            else
            {
                TestHelper.ExpectedException(
                    () => SASfile.Delete(),
                    "Delete file while SAS does not allow for deleting",
                    HttpStatusCode.Forbidden);
            }
        }

        private static void TestFileSAS(CloudFile testFile, SharedAccessFilePermissions permissions, SharedAccessFileHeaders headers)
        {
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
                HttpStatusCode.Forbidden);

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
            for (int i = 1; i < 0x20; i++)
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

                TestFileSAS(testFile, permissions, headers);
            }
        }

        public void CloudFileSASIPAddressHelper(Func<IPAddressOrRange> generateInitialIPAddressOrRange, Func<IPAddress, IPAddressOrRange> generateFinalIPAddressOrRange)
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file;
                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudFile fileWithKey = share.GetRootDirectoryReference().GetFileReference("filefile");
                byte[] data = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                fileWithKey.UploadFromByteArray(data, 0, 4);

                // We need an IP address that will never be a valid source
                IPAddressOrRange ipAddressOrRange = generateInitialIPAddressOrRange();
                string fileToken = fileWithKey.GetSharedAccessSignature(policy, null, null, null, ipAddressOrRange);
                StorageCredentials fileSAS = new StorageCredentials(fileToken);
                Uri fileSASUri = fileSAS.TransformUri(fileWithKey.Uri);
                StorageUri fileSASStorageUri = fileSAS.TransformUri(fileWithKey.StorageUri);

                file = new CloudFile(fileSASUri);
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
                    file.DownloadRangeToByteArray(target, 0, 0, 4, null, null, opContext);
                }
                catch (StorageException)
                {
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                ipAddressOrRange = generateFinalIPAddressOrRange(actualIP);
                fileToken = fileWithKey.GetSharedAccessSignature(policy, null, null, null, ipAddressOrRange);
                fileSAS = new StorageCredentials(fileToken);
                fileSASUri = fileSAS.TransformUri(fileWithKey.Uri);
                fileSASStorageUri = fileSAS.TransformUri(fileWithKey.StorageUri);

                file = new CloudFile(fileSASUri);
                file.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(data[i], target[i]);
                }

                Assert.IsTrue(file.StorageUri.PrimaryUri.Equals(fileWithKey.Uri));
                Assert.IsNull(file.StorageUri.SecondaryUri);

                file = new CloudFile(fileSASStorageUri, null);
                file.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(data[i], target[i]);
                }

                Assert.IsTrue(file.StorageUri.Equals(fileWithKey.StorageUri));
            }
            finally
            {
                share.DeleteIfExists();
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

        [TestMethod]
        [Description("Perform a SAS request specifying an IP address or range and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSASIPAddressQueryParam()
        {
            CloudFileSASIPAddressHelper(() =>
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
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSASIPRangeQueryParam()
        {
            CloudFileSASIPAddressHelper(() =>
                {
                    // We need an IP address that will never be a valid source
                    return new IPAddressOrRange("255.255.255.0", "255.255.255.255");
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
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSASSharedProtocolsQueryParam()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
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
                fileWithKey.UploadFromByteArray(data, 0, 4);

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
                        new { scheme = Uri.UriSchemeHttp, port = httpPort},
                        new { scheme = Uri.UriSchemeHttps, port = securePort}
                    };
                    
                    foreach (var item in schemesAndPorts)
                    {
                        fileSASUri = TransformSchemeAndPort(fileSASUri, item.scheme, item.port);
                        fileSASStorageUri = new StorageUri(TransformSchemeAndPort(fileSASStorageUri.PrimaryUri, item.scheme, item.port), TransformSchemeAndPort(fileSASStorageUri.SecondaryUri, item.scheme, item.port));

                        if (protocol.HasValue && protocol == SharedAccessProtocol.HttpsOnly && string.CompareOrdinal(item.scheme, Uri.UriSchemeHttp) == 0)
                        {
                            file = new CloudFile(fileSASUri);
                            TestHelper.ExpectedException(() => file.FetchAttributes(), "Access a file using SAS with a shared protocols that does not match", HttpStatusCode.Unused);

                            file = new CloudFile(fileSASStorageUri, null);
                            TestHelper.ExpectedException(() => file.FetchAttributes(), "Access a file using SAS with a shared protocols that does not match", HttpStatusCode.Unused);
                        }
                        else
                        {
                            file = new CloudFile(fileSASUri);
                            file.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
                            for (int i = 0; i < 4; i++)
                            {
                                Assert.AreEqual(data[i], target[i]);
                            }

                            file = new CloudFile(fileSASStorageUri, null);
                            file.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
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
                share.DeleteIfExists();
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
