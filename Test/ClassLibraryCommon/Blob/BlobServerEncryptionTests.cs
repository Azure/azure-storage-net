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
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class BlobServerEncryptionTests : BlobTestBase
    {
        private CloudBlobContainer container;
        private CloudBlockBlob blob;

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }

            this.container = GetRandomContainerReference();
            this.container.CreateIfNotExists();
            this.blob = this.container.GetBlockBlobReference(BlobTestBase.GetRandomContainerName());
            this.blob.UploadText("test");
        }

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }

            this.container.DeleteIfExists();
        }

        [TestMethod]
        [Description("Download encrypted blob attributes.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestBlobAttributesEncryption()
        {
            this.blob.FetchAttributes();
            Assert.IsTrue(this.blob.Properties.IsServerEncrypted);

            CloudBlockBlob testBlob = this.container.GetBlockBlobReference(this.blob.Name);
            testBlob.DownloadText();
            Assert.IsTrue(testBlob.Properties.IsServerEncrypted);
        }

        [TestMethod]
        [Description("List encrypted blob(s).")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestListBlobsEncryption()
        {
            bool blobFound = false;

            foreach(IListBlobItem b in this.container.ListBlobs()) {
                CloudBlob blob = (CloudBlob) b;
                Assert.IsTrue(blob.Properties.IsServerEncrypted);
                
                blobFound = true;
            }

            Assert.IsTrue(blobFound);
        }

        [TestMethod]
        [Description("Upload encrypted blob.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestBlobEncryption()
        {
            bool requestFound = false;

            OperationContext ctxt = new OperationContext();
            ctxt.RequestCompleted += (sender, args) =>
            {
                Assert.IsTrue(args.RequestInformation.IsRequestServerEncrypted);
                requestFound = true;
            };

            this.blob.UploadText("test", null, null, null, ctxt);
            Assert.IsTrue(requestFound);
        }
    }
}