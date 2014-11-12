// -----------------------------------------------------------------------------------------
// <copyright file="ExceptionHResultTest.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class ExceptionHResultTest : TestBase
    {
        private readonly CloudFileClient DefaultFileClient = new CloudFileClient(new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint), TestBase.StorageCredentials);

        [TestMethod]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateNegativeBadRequestAsync()
        {
            try
            {
                string name = "ABCD";
                CloudFileShare share = DefaultFileClient.GetShareReference(name);
                await share.CreateAsync();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(WindowsAzureErrorCode.HttpBadRequest, e.HResult);
            }
        }

        [TestMethod]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateNegativeConflictAsync()
        {
            try
            {
                string name = "abc";
                CloudFileShare share = DefaultFileClient.GetShareReference(name);
                await share.CreateAsync();
                await share.CreateAsync();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(WindowsAzureErrorCode.HttpConflict, e.HResult);
            }
        }

        [TestMethod]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadTimeoutAsync()
        {
            CloudFileShare share = DefaultFileClient.GetShareReference(Guid.NewGuid().ToString("N"));
            byte[] buffer = FileTestBase.GetRandomBuffer(4 * 1024 * 1024);

            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                FileRequestOptions requestOptions = new FileRequestOptions()
                {
                    MaximumExecutionTime = TimeSpan.FromMilliseconds(10),
                    RetryPolicy = new NoRetry()
                };

                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(ms.AsInputStream(), null, requestOptions, null);
                }

                Assert.Fail();
            }
            catch (Exception e)
            {
#if ASPNET_K
                Assert.AreEqual(WindowsAzureErrorCode.TimeoutException, e.InnerException.InnerException.HResult);
#else
                Assert.AreEqual(WindowsAzureErrorCode.HttpRequestTimeout, e.InnerException.InnerException.HResult);
#endif
            }
            finally
            {
                share.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadCancellationAsync()
        {
            CloudFileShare share = DefaultFileClient.GetShareReference(Guid.NewGuid().ToString("N"));
            byte[] buffer = FileTestBase.GetRandomBuffer(4 * 1024 * 1024);

            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                FileRequestOptions requestOptions = new FileRequestOptions()
                {
                    RetryPolicy = new NoRetry()
                };

                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token; 

                new Task(() =>
                {
                    new System.Threading.ManualResetEvent(false).WaitOne(10);
                    cts.Cancel(false);
                }).Start();

                using (MemoryStream ms = new MemoryStream(buffer))
                {
#if ASPNET_K
                    file.UploadFromStreamAsync(ms, ms.Length, null, requestOptions, null, token).Wait();
#else
                    file.UploadFromStreamAsync(ms.AsInputStream(), null, requestOptions, null).AsTask(token).Wait();
#endif
                }

                Assert.Fail();
            }
            catch (AggregateException e)
            {
                TaskCanceledException ex = new TaskCanceledException();
                Assert.AreEqual(ex.HResult, e.InnerException.HResult);
            }
            finally
            {
                share.DeleteIfExistsAsync().AsTask().Wait();
            }
        }
    }
}
