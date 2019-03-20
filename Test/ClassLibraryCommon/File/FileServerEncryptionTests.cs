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

namespace Microsoft.Azure.Storage.File
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class FileServerEncryptionTests : FileTestBase
    {
        private CloudFileShare share;
        private CloudFileDirectory directory;
        private CloudFile file;

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }

            this.share = GetRandomShareReference();
            this.share.CreateIfNotExists();
            CloudFileDirectory directory = share.GetRootDirectoryReference();
            this.directory = directory.GetDirectoryReference("directory");
            this.directory.Create();
            this.file = this.directory.GetFileReference("file");
            this.file.UploadText("test");
        }

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }

            this.share.DeleteIfExists();
        }

        [TestMethod]
        [Description("Download encrypted file attributes.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestFileAttributesEncryption()
        {
            this.file.FetchAttributes();
            Assert.IsTrue(this.file.Properties.IsServerEncrypted);

            CloudFile testFile = this.directory.GetFileReference(this.file.Name);
            testFile.DownloadText();
            Assert.IsTrue(testFile.Properties.IsServerEncrypted);
        }

        [TestMethod]
        [Description("Upload encrypted file.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestFileEncryption()
        {
            bool requestFound = false;

            OperationContext ctxt = new OperationContext();
            ctxt.RequestCompleted += (sender, args) =>
            {
                Assert.IsTrue(args.RequestInformation.IsRequestServerEncrypted);
                requestFound = true;
            };

            this.file.UploadText("test", null, null, null, ctxt);
            Assert.IsTrue(requestFound);

            requestFound = false;
            this.file.SetProperties(null, null, ctxt);
            Assert.IsTrue(requestFound);

            requestFound = false;
            this.file.SetMetadata(null, null, ctxt);
            Assert.IsTrue(requestFound);
        }

        [TestMethod]
        [Description("Upload encrypted file directory.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestFileDirectoryEncryption()
        {
            bool requestFound = false;

            OperationContext ctxt = new OperationContext();
            ctxt.RequestCompleted += (sender, args) =>
            {
                Assert.IsTrue(args.RequestInformation.IsRequestServerEncrypted);
                requestFound = true;
            };

            CloudFileDirectory dir2 = this.share.GetRootDirectoryReference().GetDirectoryReference("dir2");
            dir2.Create(null, ctxt);
            Assert.IsTrue(requestFound);

            requestFound = false;
            dir2.SetMetadata(null, null, ctxt);
            Assert.IsTrue(requestFound);
        }
    }
}