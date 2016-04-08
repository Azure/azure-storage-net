// -----------------------------------------------------------------------------------------
// <copyright file="CopyFileTest.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class CopyFileTest : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public CopyFileTest()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("CopyFromFile with Unicode source file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CopyFileUsingUnicodeFileNameAsync()
        {
            string _unicodeFileName = "繁体字14a6c";
            string _nonUnicodeFileName = "sample_file";

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile fileUnicodeSource = share.GetRootDirectoryReference().GetFileReference(_unicodeFileName);
                string data = "Test content";
                await UploadTextAsync(fileUnicodeSource, data, Encoding.UTF8);
                CloudFile fileAsciiSource = share.GetRootDirectoryReference().GetFileReference(_nonUnicodeFileName);
                await UploadTextAsync(fileAsciiSource, data, Encoding.UTF8);

                //Copy files over
                CloudFile fileAsciiDest = share.GetRootDirectoryReference().GetFileReference(_nonUnicodeFileName + "_copy");
                string asciiCopyId = await fileAsciiDest.StartCopyAsync(TestHelper.Defiddler(fileAsciiSource));
                await WaitForCopyAsync(fileAsciiDest);

                CloudFile fileUnicodeDest = share.GetRootDirectoryReference().GetFileReference(_unicodeFileName + "_copy");
                string unicodeCopyId = await fileUnicodeDest.StartCopyAsync(TestHelper.Defiddler(fileUnicodeSource));
                await WaitForCopyAsync(fileUnicodeDest);

                // Check ascii copy
                Assert.AreEqual(CopyStatus.Success, fileAsciiDest.CopyState.Status);
                Assert.AreEqual(fileAsciiSource.Uri.AbsolutePath, fileAsciiDest.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, fileAsciiDest.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, fileAsciiDest.CopyState.BytesCopied);
                Assert.AreEqual(asciiCopyId, fileAsciiDest.CopyState.CopyId);
                Assert.IsTrue(fileAsciiDest.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                // Check unicode copy
                Assert.AreEqual(CopyStatus.Success, fileUnicodeDest.CopyState.Status);
                Assert.AreEqual(fileUnicodeSource.Uri.AbsolutePath, fileUnicodeDest.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, fileUnicodeDest.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, fileUnicodeDest.CopyState.BytesCopied);
                Assert.AreEqual(unicodeCopyId, fileUnicodeDest.CopyState.CopyId);
                Assert.IsTrue(fileUnicodeDest.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        private static async Task CloudFileCopyAsync(bool sourceIsSas, bool destinationIsSas)
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                // Create Source on server
                CloudFile source = share.GetRootDirectoryReference().GetFileReference("source");

                string data = "String data";
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                // Create Destination on server
                CloudFile destination = share.GetRootDirectoryReference().GetFileReference("destination");
                await destination.CreateAsync(1);

                CloudFile copySource = source;
                CloudFile copyDestination = destination;

                if (sourceIsSas)
                {
                    // Source SAS must have read permissions
                    SharedAccessFilePermissions permissions = SharedAccessFilePermissions.Read;
                    SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                    {
                        SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                        SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                        Permissions = permissions,
                    };
                    string sasToken = source.GetSharedAccessSignature(policy);

                    // Get source 
                    StorageCredentials credentials = new StorageCredentials(sasToken);
                    copySource = new CloudFile(credentials.TransformUri(source.Uri));
                }

                if (destinationIsSas)
                {
                    Assert.IsTrue(sourceIsSas);

                    // Destination SAS must have write permissions
                    SharedAccessFilePermissions permissions = SharedAccessFilePermissions.Write;
                    SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                    {
                        SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                        SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                        Permissions = permissions,
                    };
                    string sasToken = destination.GetSharedAccessSignature(policy);

                    // Get destination 
                    StorageCredentials credentials = new StorageCredentials(sasToken);
                    copyDestination = new CloudFile(credentials.TransformUri(destination.Uri));
                }

                // Start copy and wait for completion
                string copyId = await copyDestination.StartCopyAsync(TestHelper.Defiddler(copySource));
                await WaitForCopyAsync(destination);

                // Check original file references for equality
                Assert.AreEqual(CopyStatus.Success, destination.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, destination.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, destination.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, destination.CopyState.BytesCopied);
                Assert.AreEqual(copyId, destination.CopyState.CopyId);
                Assert.IsTrue(destination.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                if (!destinationIsSas)
                {
                    // Abort Copy is not supported for SAS destination
                    OperationContext context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await copyDestination.AbortCopyAsync(copyId, null, null, context),
                        context,
                        "Aborting a copy operation after completion should fail",
                        HttpStatusCode.Conflict,
                        "NoPendingCopyOperation");
                }

                await source.FetchAttributesAsync();
                Assert.IsNotNull(destination.Properties.ETag);
                Assert.AreNotEqual(source.Properties.ETag, destination.Properties.ETag);
                Assert.IsTrue(destination.Properties.LastModified > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                string copyData = await DownloadTextAsync(destination, Encoding.UTF8);
                Assert.AreEqual(data, copyData, "Data inside copy of file not equal.");

                await destination.FetchAttributesAsync();
                FileProperties prop1 = destination.Properties;
                FileProperties prop2 = source.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value", destination.Metadata["Test"], false, "Copied metadata not same");

                await destination.DeleteAsync();
                await source.DeleteAsync();
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Copy a file and then verify its contents, properties, and metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCopySasToSasTestAsync()
        {
            await CloudFileCopyAsync(true, true);
        }

        [TestMethod]
        [Description("Copy a file and then verify its contents, properties, and metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCopyFromSasTestAsync()
        {
            await CloudFileCopyAsync(true, false);
        }

        [TestMethod]
        [Description("Copy a file and then verify its contents, properties, and metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCopyTestAsync()
        {
            await CloudFileCopyAsync(false, false);
        }

        [TestMethod]
        [Description("Copy a file and override metadata during copy")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCopyTestWithMetadataOverrideAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile source = share.GetRootDirectoryReference().GetFileReference("source");

                string data = "String data";
                await UploadTextAsync(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                await source.SetMetadataAsync();

                CloudFile copy = share.GetRootDirectoryReference().GetFileReference("copy");
                copy.Metadata["Test2"] = "value2";
                string copyId = await copy.StartCopyAsync(TestHelper.Defiddler(source));
                await WaitForCopyAsync(copy);
                Assert.AreEqual(CopyStatus.Success, copy.CopyState.Status);
                Assert.AreEqual(source.Uri.AbsolutePath, copy.CopyState.Source.AbsolutePath);
                Assert.AreEqual(data.Length, copy.CopyState.TotalBytes);
                Assert.AreEqual(data.Length, copy.CopyState.BytesCopied);
                Assert.AreEqual(copyId, copy.CopyState.CopyId);
                Assert.IsTrue(copy.CopyState.CompletionTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

                string copyData = await DownloadTextAsync(copy, Encoding.UTF8);
                Assert.AreEqual(data, copyData, "Data inside copy of file not similar");

                await copy.FetchAttributesAsync();
                await source.FetchAttributesAsync();
                FileProperties prop1 = copy.Properties;
                FileProperties prop2 = source.Properties;

                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentDisposition, prop2.ContentDisposition);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);

                Assert.AreEqual("value2", copy.Metadata["Test2"], false, "Copied metadata not same");
                Assert.IsFalse(copy.Metadata.ContainsKey("Test"), "Source Metadata should not appear in destination file");

                await copy.DeleteAsync();
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
