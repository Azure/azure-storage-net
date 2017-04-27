﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudBlobClientTest.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#if NETCORE
using System.Threading;
#else
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class CloudBlobClientTest : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,

        public CloudBlobClientTest()
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
        [Description("Create a service client with URI and credentials")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientConstructor()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            Assert.IsTrue(blobClient.BaseUri.ToString().Contains(TestBase.TargetTenantConfig.BlobServiceEndpoint));
            Assert.AreEqual(TestBase.StorageCredentials, blobClient.Credentials);
            Assert.AreEqual(AuthenticationScheme.SharedKey, blobClient.AuthenticationScheme);
        }

        [TestMethod]
        [Description("Create a service client with uppercase account name")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientWithUppercaseAccountNameAsync()
        {
            StorageCredentials credentials = new StorageCredentials(TestBase.StorageCredentials.AccountName.ToUpper(), Convert.ToBase64String(TestBase.StorageCredentials.ExportKey()));
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.BlobServiceEndpoint);
            CloudBlobClient blobClient = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials);
            CloudBlobContainer container = blobClient.GetContainerReference("container");
            await container.ExistsAsync();
        }

        [TestMethod]
        [Description("Compare service client properties of blob objects")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientObjects()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("container");
            Assert.AreEqual(blobClient, container.ServiceClient);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("blockblob");
            Assert.AreEqual(blobClient, blockBlob.ServiceClient);
            CloudPageBlob pageBlob = container.GetPageBlobReference("pageblob");
            Assert.AreEqual(blobClient, pageBlob.ServiceClient);
            CloudAppendBlob appendBlob = container.GetAppendBlobReference("appendblob");
            Assert.AreEqual(blobClient, appendBlob.ServiceClient);

            CloudBlobContainer container2 = GetRandomContainerReference();
            Assert.AreNotEqual(blobClient, container2.ServiceClient);
            CloudBlockBlob blockBlob2 = container2.GetBlockBlobReference("blockblob");
            Assert.AreEqual(container2.ServiceClient, blockBlob2.ServiceClient);
            CloudPageBlob pageBlob2 = container2.GetPageBlobReference("pageblob");
            Assert.AreEqual(container2.ServiceClient, pageBlob2.ServiceClient);
            CloudAppendBlob appendBlob2 = container2.GetAppendBlobReference("appendblob");
            Assert.AreEqual(container2.ServiceClient, appendBlob2.ServiceClient);
        }

        [TestMethod]
        [Description("List blobs with prefix")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListBlobsSegmentedWithPrefixAsync()
        {
            string name = "bb" + GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer rootContainer = blobClient.GetRootContainerReference();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                await rootContainer.CreateIfNotExistsAsync();
                await container.CreateAsync();

                List<string> blobNames = await CreateBlobsAsync(container, 3, BlobType.BlockBlob);
                List<string> rootBlobNames = await CreateBlobsAsync(rootContainer, 2, BlobType.BlockBlob);

                BlobResultSegment results;
                BlobContinuationToken token = null;
                do
                {
                    results = await blobClient.ListBlobsSegmentedAsync("bb", token);
                    token = results.ContinuationToken;

                    foreach (CloudBlockBlob blob in results.Results)
                    {
                        await blob.DeleteAsync();
                        rootBlobNames.Remove(blob.Name);
                    }
                }
                while (token != null);
                Assert.AreEqual(0, rootBlobNames.Count);

                results = await blobClient.ListBlobsSegmentedAsync("bb", token);
                Assert.AreEqual(0, results.Results.Count());
                Assert.IsNull(results.ContinuationToken);

                results = await blobClient.ListBlobsSegmentedAsync(name, token);
                Assert.AreEqual(0, results.Results.Count());
                Assert.IsNull(results.ContinuationToken);

                token = null;
                do
                {
                    results = await blobClient.ListBlobsSegmentedAsync(name + "/", token);
                    token = results.ContinuationToken;

                    foreach (CloudBlockBlob blob in results.Results)
                    {
                        Assert.IsTrue(blobNames.Remove(blob.Name));
                    }
                }
                while (token != null);
                Assert.AreEqual(0, blobNames.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List containers")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListContainersSegmentedAsync()
        {
            AssertSecondaryEndpoint();

            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                await blobClient.GetContainerReference(containerName).CreateAsync();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = await blobClient.ListContainersSegmentedAsync(token);
                token = resultSegment.ContinuationToken;

                foreach (CloudBlobContainer container in resultSegment.Results)
                {
                    Assert.IsTrue(blobClient.GetContainerReference(container.Name).StorageUri.Equals(container.StorageUri));
                    listedContainerNames.Add(container.Name);
                }
            }
            while (token != null);

            foreach (string containerName in listedContainerNames)
            {
                if (containerNames.Remove(containerName))
                {
                    await blobClient.GetContainerReference(containerName).DeleteAsync();
                }
            }

            Assert.AreEqual(0, containerNames.Count);
        }

        [TestMethod]
        [Description("List containers with prefix using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListContainersSegmentedWithPrefixAsync()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                await blobClient.GetContainerReference(containerName).CreateAsync();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = await blobClient.ListContainersSegmentedAsync(name, ContainerListingDetails.None, 1, token, null, null);
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudBlobContainer container in resultSegment.Results)
                {
                    count++;
                    listedContainerNames.Add(container.Name);
                }
                Assert.IsTrue(count <= 1);
            }
            while (token != null);

            Assert.AreEqual(containerNames.Count, listedContainerNames.Count);
            foreach (string containerName in listedContainerNames)
            {
                Assert.IsTrue(containerNames.Remove(containerName));
                await blobClient.GetContainerReference(containerName).DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create a container with public access. Check public access is populated for Exists")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientCreateBlobAndCheckExistsWithPublicAccessAsync()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            BlobContainerPublicAccessType[] accessValues = { BlobContainerPublicAccessType.Container, BlobContainerPublicAccessType.Off, BlobContainerPublicAccessType.Blob };
            BlobContainerPermissions permissions = new BlobContainerPermissions();
            foreach (BlobContainerPublicAccessType access in accessValues)
            {
                string name = GetRandomContainerName();
                CloudBlobContainer container = blobClient.GetContainerReference(name);
                await container.CreateAsync(access, null, null);
                Assert.AreEqual(access, container.Properties.PublicAccess);

                CloudBlobContainer container2 = blobClient.GetContainerReference(name);
                BlobContainerPermissions containerAccess = await container2.GetPermissionsAsync();
                Assert.AreEqual(access, containerAccess.PublicAccess);
                Assert.AreEqual(access, container2.Properties.PublicAccess);

                CloudBlobContainer container3 = blobClient.GetContainerReference(name);
                await container3.ExistsAsync();
                Assert.AreEqual(access, container3.Properties.PublicAccess);

                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("List containers with public access")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListContainersWithPublicAccessAsync()
        {
            string name = GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            await container.CreateAsync();

            BlobContainerPublicAccessType[] accessValues = { BlobContainerPublicAccessType.Container, BlobContainerPublicAccessType.Off, BlobContainerPublicAccessType.Blob };
            BlobContainerPermissions permissions = new BlobContainerPermissions();
            foreach (BlobContainerPublicAccessType access in accessValues)
            {
                permissions.PublicAccess = access;
                await container.SetPermissionsAsync(permissions);
                Assert.AreEqual(access, container.Properties.PublicAccess);

                CloudBlobContainer container2 = blobClient.GetContainerReference(name);
                Assert.IsFalse(container2.Properties.PublicAccess.HasValue);
                await container2.FetchAttributesAsync();
                Assert.AreEqual(access, container2.Properties.PublicAccess);

                CloudBlobContainer container3 = blobClient.GetContainerReference(name);
                BlobContainerPermissions containerAccess = await container3.GetPermissionsAsync();
                Assert.AreEqual(access, containerAccess.PublicAccess);
                Assert.AreEqual(access, container3.Properties.PublicAccess);

                List<CloudBlobContainer> listedContainers = new List<CloudBlobContainer>();
                BlobContinuationToken token = null;
                do
                {
                    ContainerResultSegment resultSegment = await blobClient.ListContainersSegmentedAsync(name, token);
                    foreach (CloudBlobContainer returnedContainer in resultSegment.Results)
                    {
                        listedContainers.Add(returnedContainer);
                    }
                }
                while (token != null);

                Assert.AreEqual(1, listedContainers.Count());
                Assert.AreEqual(access, listedContainers.First().Properties.PublicAccess);
            }

            await container.DeleteAsync();
        }

        [TestMethod]
        [Description("Test Create Container with Shared Key Lite")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientCreateContainerSharedKeyLiteAsync()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.AuthenticationScheme = AuthenticationScheme.SharedKeyLite;

            string containerName = GetRandomContainerName();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateAsync();

            bool exists = await blobContainer.ExistsAsync();
            Assert.IsTrue(exists);

            await blobContainer.DeleteAsync();
        }

        [TestMethod]
        [Description("Upload a blob with a small maximum execution time")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientMaximumExecutionTimeoutAsync()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Guid.NewGuid().ToString("N"));
            byte[] buffer = BlobTestBase.GetRandomBuffer(80 * 1024 * 1024);

            try
            {
                await container.CreateAsync();
                blobClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(5);
                blobClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 2 * 1024 * 1024;

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                blockBlob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    try
                    {
                        await blockBlob.UploadFromStreamAsync(ms);
                        Assert.Fail();
                    }
                    catch (AggregateException ex)
                    {
#if !FACADE_NETCORE
                        Assert.AreEqual("The client could not finish the operation within specified timeout.", RequestResult.TranslateFromExceptionMessage(ex.InnerException.Message).ExceptionInfo.Message);
#else
                        Assert.AreEqual("The client could not finish the operation within specified timeout.", RequestResult.TranslateFromExceptionMessage(ex.InnerException.Message).Exception.Message);
#endif
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                pageBlob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    try
                    {
                        await pageBlob.UploadFromStreamAsync(ms);
                        Assert.Fail();
                    }
                    catch (AggregateException ex)
                    {
#if !FACADE_NETCORE
                        Assert.AreEqual("The client could not finish the operation within specified timeout.", RequestResult.TranslateFromExceptionMessage(ex.InnerException.Message).ExceptionInfo.Message);
#else
                        Assert.AreEqual("The client could not finish the operation within specified timeout.", RequestResult.TranslateFromExceptionMessage(ex.InnerException.Message).Exception.Message);
#endif
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
            finally
            {
                blobClient.DefaultRequestOptions.MaximumExecutionTime = null;
                container.DeleteIfExistsAsync().Wait();
            }
        }

#if !FACADE_NETCORE
        [TestMethod]
        [Description("Make sure MaxExecutionTime is not enforced when using streams")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientMaximumExecutionTimeoutShouldNotBeHonoredForStreamsAsync()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Guid.NewGuid().ToString("N"));
            byte[] buffer = BlobTestBase.GetRandomBuffer(1024 * 1024);

            try
            {
                await container.CreateAsync();

                blobClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(30);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                blockBlob.StreamWriteSizeInBytes = 1024 * 1024;
                blockBlob.StreamMinimumReadSizeInBytes = 1024 * 1024;
                pageBlob.StreamWriteSizeInBytes = 1024 * 1024;
                pageBlob.StreamMinimumReadSizeInBytes = 1024 * 1024;

                using (var bos = await blockBlob.OpenWriteAsync())
                {
                    DateTime start = DateTime.Now;
                    for (int i = 0; i < 7; i++)
                    {
                        await bos.WriteAsync(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last write
                    int msRemaining = (int)(blobClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        await Task.Delay(msRemaining);
                    }

                    await bos.WriteAsync(buffer, 0, buffer.Length);
                    await bos.CommitAsync();
                }

                using (Stream bis = (await blockBlob.OpenReadAsync()))
                {
                    DateTime start = DateTime.Now;
                    int total = 0;
                    while (total < 7 * 1024 * 1024)
                    {
                        total += await bis.ReadAsync(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last read
                    int msRemaining = (int)(blobClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        await Task.Delay(msRemaining);
                    }

                    while (true)
                    {
                        int count = await bis.ReadAsync(buffer, 0, buffer.Length);
                        total += count;
                        if (count == 0)
                            break;
                    }
                }

                using (var bos = await pageBlob.OpenWriteAsync(8 * 1024 * 1024))
                {
                    DateTime start = DateTime.Now;
                    for (int i = 0; i < 7; i++)
                    {
                        await bos.WriteAsync(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last write
                    int msRemaining = (int)(blobClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        await Task.Delay(msRemaining);
                    }

                    await bos.WriteAsync(buffer, 0, buffer.Length);
                    await bos.CommitAsync();
                }

                using (Stream bis = (await pageBlob.OpenReadAsync()))
                {
                    DateTime start = DateTime.Now;
                    int total = 0;
                    while (total < 7 * 1024 * 1024)
                    {
                        total += await bis.ReadAsync(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last read
                    int msRemaining = (int)(blobClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        await Task.Delay(msRemaining);
                    }

                    while (true)
                    {
                        int count = await bis.ReadAsync(buffer, 0, buffer.Length);
                        total += count;
                        if (count == 0)
                            break;
                    }
                }
            }

            finally
            {
                blobClient.DefaultRequestOptions.MaximumExecutionTime = null;
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientGetServiceStatsAsync()
        {
            AssertSecondaryEndpoint();

            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            TestHelper.VerifyServiceStats(await client.GetServiceStatsAsync());
        }

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - ASYNC")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientGetServiceStatsInvalidLocAsync()
        {
            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                TestHelper.VerifyServiceStats(await client.GetServiceStatsAsync());
                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException.");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }
#if !FACADE_NETCORE
        [TestMethod]
        [Description("Server timeout query parameter")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientServerTimeoutAsync()
        {
            CloudBlobClient client = GenerateCloudBlobClient();

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

            BlobRequestOptions options = new BlobRequestOptions();
            await client.GetServicePropertiesAsync(null, context);
            Assert.IsNull(timeout);
            await client.GetServicePropertiesAsync(options, context);
            Assert.IsNull(timeout);

            options.ServerTimeout = TimeSpan.FromSeconds(100);
            await client.GetServicePropertiesAsync(options, context);
            Assert.AreEqual("100", timeout);

            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(90);
            await client.GetServicePropertiesAsync(null, context);
            Assert.AreEqual("90", timeout);
            await client.GetServicePropertiesAsync(options, context);
            Assert.AreEqual("100", timeout);

            options.ServerTimeout = null;
            await client.GetServicePropertiesAsync(options, context);
            Assert.AreEqual("90", timeout);

            options.ServerTimeout = TimeSpan.Zero;
            await client.GetServicePropertiesAsync(options, context);
            Assert.IsNull(timeout);
        }
#endif
    }
}