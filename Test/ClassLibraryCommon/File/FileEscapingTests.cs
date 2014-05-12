// -----------------------------------------------------------------------------------------
// <copyright file="FileEscapingTests.cs" company="Microsoft">
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
using System;
using System.Linq;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileEscapingTests : FileTestBase
    {
        internal const string UnreservedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-._~";
        internal const string GenDelimeters = "#@";
        internal const string SubDelimeters = "!$'()";

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
        [Description("The test case for unsafe chars")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDirectoryTestWithSpace()
        {
            FileDirectoryEscapingTest("directory test", "file test");
        }

        [TestMethod]
        [Description("The test case for escape chars")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDirectoryTestWithPercent20()
        {
            FileDirectoryEscapingTest("directory%20test", "file%20test");
        }

        [TestMethod]
        [Description("The test case for unreserved chars")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDirectoryTestWithUnreservedCharacters()
        {
            FileDirectoryEscapingTest(UnreservedCharacters, UnreservedCharacters);
        }

        [TestMethod]
        [Description("The test case for reserved chars(Gen-Delimeters)")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDirectoryTestWithGenDelimeter()
        {
            FileDirectoryEscapingTest(GenDelimeters, GenDelimeters);
        }

        [TestMethod]
        [Description("The test case for reserved chars(Sub-Delimeters)")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDirectoryTestWithSubDelimeter()
        {
            FileDirectoryEscapingTest(SubDelimeters, SubDelimeters);
        }

        [TestMethod]
        [Description("The test case for unicode chars")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileDirectoryTestWithUnicode()
        {
            FileDirectoryEscapingTest("directory中文test", "char中文test");
        }

        private void FileDirectoryEscapingTest(string directoryName, string fileName)
        {
            CloudFileClient service = GenerateCloudFileClient();
            CloudFileShare share = GetRandomShareReference();

            try
            {
                share.Create();
                string text = Guid.NewGuid().ToString();

                // Create from CloudFileShare.
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference(directoryName);
                directory.Create();
                CloudFile originalFile = directory.GetFileReference(fileName);
                originalFile.Create(0);

                // List directories from share.
                IListFileItem directoryFromShareListingFiles = share.GetRootDirectoryReference().ListFilesAndDirectories().First();
                Assert.AreEqual(directory.Uri, directoryFromShareListingFiles.Uri);

                // List files from directory.
                IListFileItem fileFromDirectoryListingFiles = directory.ListFilesAndDirectories().First();
                Assert.AreEqual(originalFile.Uri, fileFromDirectoryListingFiles.Uri);

                // Check Name
                Assert.AreEqual<string>(fileName, originalFile.Name);

                // Absolute URI access from CloudFile
                CloudFile fileInfo = new CloudFile(originalFile.Uri, service.Credentials);
                fileInfo.FetchAttributes();

                // Access from CloudFileDirectory
                CloudFileDirectory cloudFileDirectory = share.GetRootDirectoryReference().GetDirectoryReference(directoryName);
                CloudFile fileFromCloudFileDirectory = cloudFileDirectory.GetFileReference(fileName);
                Assert.AreEqual(fileInfo.Uri, fileFromCloudFileDirectory.Uri);
            }
            finally
            {
                share.Delete();
            }
        }
    }
}
