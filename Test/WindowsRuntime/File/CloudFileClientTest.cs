// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileClientTest.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#if !NETCORE
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class CloudFileClientTest : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public CloudFileClientTest()
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
        [Description("Create a service client with URI and credentials")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientConstructor()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            Assert.IsTrue(fileClient.BaseUri.ToString().Contains(TestBase.TargetTenantConfig.FileServiceEndpoint));
            Assert.AreEqual(TestBase.StorageCredentials, fileClient.Credentials);
            Assert.AreEqual(AuthenticationScheme.SharedKey, fileClient.AuthenticationScheme);
        }

        [TestMethod]
        [Description("Create a service client with uppercase account name")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientWithUppercaseAccountNameAsync()
        {
            StorageCredentials credentials = new StorageCredentials(TestBase.StorageCredentials.AccountName.ToUpper(), Convert.ToBase64String(TestBase.StorageCredentials.ExportKey()));
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint);
            CloudFileClient fileClient = new CloudFileClient(baseAddressUri, TestBase.StorageCredentials);
            CloudFileShare share = fileClient.GetShareReference("share");
            await share.ExistsAsync();
        }

        [TestMethod]
        [Description("Compare service client properties of file objects")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientObjects()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference("share");
            Assert.AreEqual(fileClient, share.ServiceClient);
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
            Assert.AreEqual(fileClient, rootDirectory.ServiceClient);
            CloudFileDirectory directory = rootDirectory.GetDirectoryReference("directory");
            Assert.AreEqual(fileClient, directory.ServiceClient);
            CloudFile file = directory.GetFileReference("file");
            Assert.AreEqual(fileClient, file.ServiceClient);

            CloudFileShare share2 = GetRandomShareReference();
            Assert.AreNotEqual(fileClient, share2.ServiceClient);
        }

        [TestMethod]
        [Description("List shares")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedAsync()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                await fileClient.GetShareReference(shareName).CreateAsync();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment = await fileClient.ListSharesSegmentedAsync(token);
                token = resultSegment.ContinuationToken;

                foreach (CloudFileShare share in resultSegment.Results)
                {
                    Assert.IsTrue(fileClient.GetShareReference(share.Name).StorageUri.Equals(share.StorageUri));
                    listedShareNames.Add(share.Name);
                }
            }
            while (token != null);

            foreach (string shareName in listedShareNames)
            {
                if (shareNames.Remove(shareName))
                {
                    await fileClient.GetShareReference(shareName).DeleteAsync();
                }
            }

            Assert.AreEqual(0, shareNames.Count);
        }

        [TestMethod]
        [Description("List shares with prefix using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedWithPrefixAsync()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                await fileClient.GetShareReference(shareName).CreateAsync();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment = await fileClient.ListSharesSegmentedAsync(name, ShareListingDetails.None, 1, token, null, null);
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudFileShare share in resultSegment.Results)
                {
                    count++;
                    listedShareNames.Add(share.Name);
                }
                Assert.IsTrue(count <= 1);
            }
            while (token != null);

            Assert.AreEqual(shareNames.Count, listedShareNames.Count);
            foreach (string shareName in listedShareNames)
            {
                Assert.IsTrue(shareNames.Remove(shareName));
                await fileClient.GetShareReference(shareName).DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Test Create Share with Shared Key Lite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientCreateShareSharedKeyLiteAsync()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            fileClient.AuthenticationScheme = AuthenticationScheme.SharedKeyLite;

            string shareName = GetRandomShareName();
            CloudFileShare share = fileClient.GetShareReference(shareName);
            await share.CreateAsync();

            bool exists = await share.ExistsAsync();
            Assert.IsTrue(exists);

            await share.DeleteAsync();
        }

        [TestMethod]
        [Description("Upload a file with a small maximum execution time")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientMaximumExecutionTimeoutAsync()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(Guid.NewGuid().ToString("N"));
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
            byte[] buffer = FileTestBase.GetRandomBuffer(80 * 1024 * 1024);

            try
            {
                await share.CreateAsync();
                fileClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(5);

                CloudFile file = rootDirectory.GetFileReference("file");
                file.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    try
                    {
                        await file.UploadFromStreamAsync(ms);
                        Assert.Fail();
                    }
                    catch (AggregateException ex)
                    {
                        Assert.AreEqual("The client could not finish the operation within specified timeout.", RequestResult.TranslateFromExceptionMessage(ex.InnerException.Message).ExceptionInfo.Message);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
            finally
            {
                fileClient.DefaultRequestOptions.MaximumExecutionTime = null;
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Make sure MaxExecutionTime is not enforced when using streams")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientMaximumExecutionTimeoutShouldNotBeHonoredForStreamsAsync()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(Guid.NewGuid().ToString("N"));
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
            byte[] buffer = FileTestBase.GetRandomBuffer(1024 * 1024);

            try
            {
                await share.CreateAsync();

                fileClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(30);
                CloudFile file = rootDirectory.GetFileReference("file");
                file.StreamMinimumReadSizeInBytes = 1024 * 1024;

                using (var fileStream = await file.OpenWriteAsync(8 * 1024 * 1024))
                {
                    Stream fos = fileStream;

                    DateTime start = DateTime.Now;
                    for (int i = 0; i < 7; i++)
                    {
                        await fos.WriteAsync(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last write
                    int msRemaining = (int)(fileClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        await Task.Delay(msRemaining);
                    }

                    await fos.WriteAsync(buffer, 0, buffer.Length);
                    await fileStream.CommitAsync();
                }

                using (var fileStream = await file.OpenReadAsync())
                {
                    Stream fis = fileStream;

                    DateTime start = DateTime.Now;
                    int total = 0;
                    while (total < 7 * 1024 * 1024)
                    {
                        total += await fis.ReadAsync(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last read
                    int msRemaining = (int)(fileClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        await Task.Delay(msRemaining);
                    }

                    while (true)
                    {
                        int count = await fis.ReadAsync(buffer, 0, buffer.Length);
                        total += count;
                        if (count == 0)
                            break;
                    }
                }
            }
            finally
            {
                fileClient.DefaultRequestOptions.MaximumExecutionTime = null;
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Server timeout query parameter")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientServerTimeoutAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare share = client.GetShareReference("timeouttest");

            string timeout = null;
            OperationContext context = new OperationContext();
            context.SendingRequest += (sender, e) =>
            {
                IDictionary<string, string> query = HttpWebUtility.ParseQueryString(e.RequestUri.Query);
                if (!query.TryGetValue("timeout", out timeout))
                {
                    timeout = null;
                }
            };

            FileRequestOptions options = new FileRequestOptions();
            await share.ExistsAsync(null, context);
            Assert.IsNull(timeout);
            await share.ExistsAsync(options, context);
            Assert.IsNull(timeout);

            options.ServerTimeout = TimeSpan.FromSeconds(100);
            await share.ExistsAsync(options, context);
            Assert.AreEqual("100", timeout);

            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(90);
            await share.ExistsAsync(null, context);
            Assert.AreEqual("90", timeout);
            await share.ExistsAsync(options, context);
            Assert.AreEqual("100", timeout);

            options.ServerTimeout = null;
            await share.ExistsAsync(options, context);
            Assert.AreEqual("90", timeout);

            options.ServerTimeout = TimeSpan.Zero;
            await share.ExistsAsync(options, context);
            Assert.IsNull(timeout);
        }
    }
}
