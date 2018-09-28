// -----------------------------------------------------------------------------------------
// <copyright file="FileServerEncryptionTests.cs" company="Microsoft">
//    Copyright 2016 Microsoft Corporation
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;


    [TestClass]
    public class FileServerEncryptionTests : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        public FileServerEncryptionTests()
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
        [Description("Download encrypted file attributes.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TestFileAttributesEncryptionAsync()
        {
            CloudFileShare share = GetRandomShareReference();

            try
            {
                await share.CreateIfNotExistsAsync();

                CloudFileDirectory directory = share.GetRootDirectoryReference();
                CloudFile file = directory.GetFileReference("file");
                await file.UploadTextAsync("test");

                await file.FetchAttributesAsync();
                Assert.IsTrue(file.Properties.IsServerEncrypted);

                CloudFile testFile = directory.GetFileReference(file.Name);
                await testFile.DownloadTextAsync();
                Assert.IsTrue(testFile.Properties.IsServerEncrypted);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload encrypted file.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TestFileEncryptionAsync()
        {
            bool requestFound = false;

            OperationContext ctxt = new OperationContext();
            CloudFileShare share = GetRandomShareReference();

            try
            {
                await share.CreateIfNotExistsAsync();

                CloudFileDirectory directory = share.GetRootDirectoryReference();
                CloudFile file = directory.GetFileReference("file");

                await file.UploadTextAsync("test");

                ctxt.RequestCompleted += (sender, args) =>
                {
                    Assert.IsTrue(args.RequestInformation.IsRequestServerEncrypted);
                    requestFound = true;
                };

                await file.UploadTextAsync("test", null, null, ctxt);
                Assert.IsTrue(requestFound);

                requestFound = false;
                await file.SetPropertiesAsync(null, null, ctxt);
                Assert.IsTrue(requestFound);

                requestFound = false;
                await file.SetMetadataAsync(null, null, ctxt);
                Assert.IsTrue(requestFound);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload encrypted directory.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TestFileDirectoryEncryptionAsync()
        {
            bool requestFound = false;

            OperationContext ctxt = new OperationContext();
            CloudFileShare share = GetRandomShareReference();

            try
            {
                await share.CreateIfNotExistsAsync();

                ctxt.RequestCompleted += (sender, args) =>
                {
                    Assert.IsTrue(args.RequestInformation.IsRequestServerEncrypted);
                    requestFound = true;
                };

                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("dir");

                await directory.CreateAsync(null, ctxt);
                Assert.IsTrue(requestFound);

                requestFound = false;
                await directory.SetMetadataAsync(null, null, ctxt);
                Assert.IsTrue(requestFound);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}