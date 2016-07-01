// -----------------------------------------------------------------------------------------
// <copyright file="BlobServerEncryptionTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;


    [TestClass]
    public class BlobServerEncryptionTests : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        public BlobServerEncryptionTests()
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
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Download encrypted blob attributes.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TestBlobAttributesEncryptionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference(BlobTestBase.GetRandomContainerName());
                await blob.UploadTextAsync("test");

                await blob.FetchAttributesAsync();
                Assert.IsTrue(blob.Properties.IsServerEncrypted);

                CloudBlockBlob testBlob = container.GetBlockBlobReference(blob.Name);
                await testBlob.DownloadTextAsync();
                Assert.IsTrue(testBlob.Properties.IsServerEncrypted);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List encrypted blob(s).")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TestListBlobsEncryptionAsync()
        {
            bool blobFound = false; 
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                    await container.CreateIfNotExistsAsync();

                    CloudBlockBlob blob = container.GetBlockBlobReference(BlobTestBase.GetRandomContainerName());
                    await blob.UploadTextAsync("test");

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null);
                foreach (IListBlobItem b in results.Results)
                {
                    CloudBlob cloudBlob = (CloudBlob)b;
                    Assert.IsTrue(cloudBlob.Properties.IsServerEncrypted);

                    blobFound = true;
                }

                Assert.IsTrue(blobFound);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload encrypted blob.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TestBlobEncryptionAsync()
        {
            bool requestFound = false;

            OperationContext ctxt = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference(); 

            try
            {
                    await container.CreateIfNotExistsAsync();

                    CloudBlockBlob blob = container.GetBlockBlobReference(BlobTestBase.GetRandomContainerName());
                    await blob.UploadTextAsync("test");

                ctxt.RequestCompleted += (sender, args) =>
                {
                    Assert.IsTrue(args.RequestInformation.IsRequestServerEncrypted);
                    requestFound = true;
                };

                await blob.UploadTextAsync("test", null, null, null, ctxt);
                Assert.IsTrue(requestFound);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
    }
}