// -----------------------------------------------------------------------------------------
// <copyright file="FileManglerUnitTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Test.Network;
using Microsoft.WindowsAzure.Test.Network.Behaviors;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileManglerUnitTests : FileTestBase
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
        [Description("Force file download to retry")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToStreamAPMRetry()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);

                        using (MemoryStream downloadedFile = new MemoryStream())
                        {
                            Exception manglerEx = null;
                            using (HttpMangler proxy = new HttpMangler(false,
                                new[]
                                {
                                    TamperBehaviors.TamperNRequestsIf(
                                        session => ThreadPool.QueueUserWorkItem(state =>
                                            {
                                                Thread.Sleep(1000);
                                                try
                                                {
                                                    session.Abort();
                                                }
                                                catch (Exception e)
                                                {
                                                    manglerEx = e;
                                                }
                                            }),
                                            2,
                                            AzureStorageSelectors.FileTraffic().IfHostNameContains(share.ServiceClient.Credentials.AccountName))
                                }))
                            {
                                OperationContext operationContext = new OperationContext();
                                result = file.BeginDownloadToStream(downloadedFile, null, null, operationContext,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                file.EndDownloadToStream(result);
                                TestHelper.AssertStreamsAreEqual(originalFile, downloadedFile);
                            }

                            if (manglerEx != null)
                            {
                                throw manglerEx;
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

        [TestMethod]
        [Description("Force range file download to retry")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadRangeToStreamAPMRetry()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            int offset = 1024;
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);
                    }
                }

                using (MemoryStream originalFile = new MemoryStream())
                {
                    originalFile.Write(buffer, offset, buffer.Length - offset);

                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        using (MemoryStream downloadedFile = new MemoryStream())
                        {
                            Exception manglerEx = null;
                            using (HttpMangler proxy = new HttpMangler(false,
                                new[]
                                    {
                                        TamperBehaviors.TamperNRequestsIf(
                                            session => ThreadPool.QueueUserWorkItem(state =>
                                                {
                                                    Thread.Sleep(1000);
                                                    try
                                                    {
                                                        session.Abort();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        manglerEx = e;
                                                    }
                                                }),
                                                2,
                                                AzureStorageSelectors.FileTraffic().IfHostNameContains(share.ServiceClient.Credentials.AccountName))
                                    }))
                            {
                                OperationContext operationContext = new OperationContext();
                                FileRequestOptions options = new FileRequestOptions()
                                {
                                    UseTransactionalMD5 = true
                                };

                                ICancellableAsyncResult result = file.BeginDownloadRangeToStream(downloadedFile, offset, buffer.Length - offset, null, options, operationContext,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                file.EndDownloadToStream(result);
                                TestHelper.AssertStreamsAreEqual(originalFile, downloadedFile);
                            }

                            if (manglerEx != null)
                            {
                                throw manglerEx;
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
    }
}
