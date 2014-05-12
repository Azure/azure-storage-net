// -----------------------------------------------------------------------------------------
// <copyright file="FileCancellationUnitTests.cs" company="Microsoft">
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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileCancellationUnitTests : FileTestBase
    {
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
        [Description("Cancel file download to stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadToStreamCancelAsync()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(originalFile);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        OperationContext operationContext = new OperationContext();
                        CancellationTokenSource source = new CancellationTokenSource(100);
                        Task task = file.DownloadToStreamAsync(downloadedFile, null, null, operationContext, source.Token);
                        try
                        {
                            await task;
                        }
                        catch (Exception)
                        {
                            Assert.AreEqual(operationContext.LastResult.Exception.Message, "Operation was canceled by user.");
                            Assert.AreEqual(operationContext.LastResult.HttpStatusCode, 306);
                            //Assert.AreEqual(operationContext.LastResult.HttpStatusMessage, "Unused");
                        }
                        TestHelper.AssertNAttempts(operationContext, 1);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Cancel upload from stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamCancelAsync()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                    {
                        OperationContext operationContext = new OperationContext();
                        CancellationTokenSource source = new CancellationTokenSource(100);
                        Task task = file.UploadFromStreamAsync(originalFile, null, null, operationContext, source.Token);
                        try
                        {
                            await task;
                        }
                        catch (Exception)
                        {
                            Assert.AreEqual(operationContext.LastResult.Exception.Message, "Operation was canceled by user.");
                            Assert.AreEqual(operationContext.LastResult.HttpStatusCode, 306);
                            //Assert.AreEqual(operationContext.LastResult.HttpStatusMessage, "Unused");
                        }
                        TestHelper.AssertNAttempts(operationContext, 1);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
