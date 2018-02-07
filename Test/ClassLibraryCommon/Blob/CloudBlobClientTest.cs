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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class CloudBlobClientTest : BlobTestBase
    {
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
        public void CloudBlobClientWithUppercaseAccountName()
        {
            StorageCredentials credentials = new StorageCredentials(TestBase.StorageCredentials.AccountName.ToUpper(), TestBase.StorageCredentials.ExportKey());
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.BlobServiceEndpoint);
            CloudBlobClient blobClient = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials);
            CloudBlobContainer container = blobClient.GetContainerReference("container");
            container.Exists();
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
        public async Task CloudBlobClientListBlobsWithPrefix()
        {
            string name = "bb" + GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer rootContainer = blobClient.GetRootContainerReference();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                rootContainer.CreateIfNotExists();
                container.Create();

                List<string> blobNames = await CreateBlobs(container, 3, BlobType.BlockBlob);
                List<string> rootBlobNames = await CreateBlobs(rootContainer, 2, BlobType.BlockBlob);

                IEnumerable<IListBlobItem> results = blobClient.ListBlobs("bb");
                foreach (CloudBlockBlob blob in results)
                {
                    blob.Delete();
                    rootBlobNames.Remove(blob.Name);
                }
                Assert.AreEqual(0, rootBlobNames.Count);
                Assert.AreEqual(0, blobClient.ListBlobs("bb").Count());

                Assert.AreEqual(0, blobClient.ListBlobs(name).Count());
                results = blobClient.ListBlobs(name + "/");
                foreach (CloudBlockBlob blob in results)
                {
                    Assert.IsTrue(blobNames.Remove(blob.Name));
                }
                Assert.AreEqual(0, blobNames.Count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List blobs with prefix")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListBlobsSegmentedWithPrefix()
        {
            string name = "bb" + GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer rootContainer = blobClient.GetRootContainerReference();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                rootContainer.CreateIfNotExists();
                container.Create();

                List<string> blobNames = await CreateBlobs(container, 3, BlobType.BlockBlob);
                List<string> rootBlobNames = await CreateBlobs(rootContainer, 2, BlobType.BlockBlob);

                BlobResultSegment results;
                BlobContinuationToken token = null;
                do
                {
                    results = blobClient.ListBlobsSegmented("bb", token);
                    token = results.ContinuationToken;

                    foreach (CloudBlockBlob blob in results.Results)
                    {
                        blob.Delete();
                        rootBlobNames.Remove(blob.Name);
                    }
                }
                while (token != null);
                Assert.AreEqual(0, rootBlobNames.Count);

                results = blobClient.ListBlobsSegmented("bb", token);
                Assert.AreEqual(0, results.Results.Count());
                Assert.IsNull(results.ContinuationToken);

                results = blobClient.ListBlobsSegmented(name, token);
                Assert.AreEqual(0, results.Results.Count());
                Assert.IsNull(results.ContinuationToken);

                token = null;
                do
                {
                    results = blobClient.ListBlobsSegmented(name + "/", token);
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
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List blobs with empty prefix")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListBlobsSegmentedWithEmptyPrefix()
        {
            string name = "bb" + GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.SetServiceProperties(new ServiceProperties()
            {
                DeleteRetentionPolicy = new DeleteRetentionPolicy()
                {
                    RetentionDays = 10,
                    Enabled = true
                }
            });
            CloudBlobContainer rootContainer = blobClient.GetRootContainerReference();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                rootContainer.CreateIfNotExists();
                container.Create();
                List<Uri> preExistingBlobs = rootContainer.ListBlobs().Select(b => b.Uri).ToList();

                List<string> blobNames = await CreateBlobs(container, 3, BlobType.BlockBlob);
                List<string> rootBlobNames = await CreateBlobs(rootContainer, 2, BlobType.BlockBlob);

                BlobResultSegment results;
                BlobContinuationToken token = null;
                List<Uri> listedBlobs = new List<Uri>();
                do
                {
                    results = blobClient.ListBlobsSegmented("", token);
                    token = results.ContinuationToken;

                    foreach (IListBlobItem blob in results.Results)
                    {
                        if (preExistingBlobs.Contains(blob.Uri))
                        {
                            continue;
                        }
                        else
                        {
                            if (blob is CloudPageBlob)
                            {
                                ((CloudPageBlob)blob).Delete();
                            }
                            else
                            {
                                ((CloudBlockBlob)blob).Delete();
                            }

                            listedBlobs.Add(blob.Uri);
                        }
                    }
                }
                while (token != null);

                Assert.AreEqual(2, listedBlobs.Count);
                do
                {
                    results = container.ListBlobsSegmented("", false, BlobListingDetails.None, null, token, null, null);
                    token = results.ContinuationToken;

                    foreach (IListBlobItem blob in results.Results)
                    {
                        if (preExistingBlobs.Contains(blob.Uri))
                        {
                            continue;
                        }
                        else
                        {
                            if (blob is CloudPageBlob)
                            {
                                ((CloudPageBlob)blob).Delete();
                            }
                            else
                            {
                                ((CloudBlockBlob)blob).Delete();
                            }

                            listedBlobs.Add(blob.Uri);
                        }
                    }
                }
                while (token != null);

                Assert.AreEqual(5, listedBlobs.Count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List blobs with prefix")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobClientListBlobsSegmentedWithPrefixAPM()
        {
            string name = "bb" + GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer rootContainer = blobClient.GetRootContainerReference();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                rootContainer.CreateIfNotExists();
                container.Create();

                List<string> blobNames = await CreateBlobs(container, 3, BlobType.BlockBlob);
                List<string> rootBlobNames = await CreateBlobs(rootContainer, 2, BlobType.BlockBlob);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    BlobResultSegment results;
                    BlobContinuationToken token = null;
                    do
                    {
                        result = blobClient.BeginListBlobsSegmented("bb", token,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        results = blobClient.EndListBlobsSegmented(result);
                        token = results.ContinuationToken;

                        foreach (CloudBlockBlob blob in results.Results)
                        {
                            blob.Delete();
                            rootBlobNames.Remove(blob.Name);
                        }
                    }
                    while (token != null);
                    Assert.AreEqual(0, rootBlobNames.Count);

                    result = blobClient.BeginListBlobsSegmented("bb", token,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    results = blobClient.EndListBlobsSegmented(result);
                    Assert.AreEqual(0, results.Results.Count());
                    Assert.IsNull(results.ContinuationToken);

                    result = blobClient.BeginListBlobsSegmented(name, token,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    results = blobClient.EndListBlobsSegmented(result);
                    Assert.AreEqual(0, results.Results.Count());
                    Assert.IsNull(results.ContinuationToken);

                    token = null;
                    do
                    {
                        result = blobClient.BeginListBlobsSegmented(name + "/", token,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        results = blobClient.EndListBlobsSegmented(result);
                        token = results.ContinuationToken;

                        foreach (CloudBlockBlob blob in results.Results)
                        {
                            Assert.IsTrue(blobNames.Remove(blob.Name));
                        }
                    }
                    while (token != null);
                    Assert.AreEqual(0, blobNames.Count);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("List blobs with prefix")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListBlobsSegmentedWithPrefixTask()
        {
            string name = "bb" + GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer rootContainer = blobClient.GetRootContainerReference();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                rootContainer.CreateIfNotExistsAsync().Wait();
                container.CreateAsync().Wait();

                List<string> blobNames = CreateBlobsTask(container, 3, BlobType.BlockBlob);
                List<string> rootBlobNames = CreateBlobsTask(rootContainer, 2, BlobType.BlockBlob);

                BlobResultSegment results;
                BlobContinuationToken token = null;
                do
                {
                    results = blobClient.ListBlobsSegmentedAsync("bb", token).Result;
                    token = results.ContinuationToken;

                    foreach (CloudBlockBlob blob in results.Results)
                    {
                        blob.DeleteAsync().Wait();
                        rootBlobNames.Remove(blob.Name);
                    }
                }
                while (token != null);
                Assert.AreEqual(0, rootBlobNames.Count);

                results = blobClient.ListBlobsSegmentedAsync("bb", token).Result;
                Assert.AreEqual(0, results.Results.Count());
                Assert.IsNull(results.ContinuationToken);

                results = blobClient.ListBlobsSegmentedAsync(name, token).Result;
                Assert.AreEqual(0, results.Results.Count());
                Assert.IsNull(results.ContinuationToken);

                token = null;
                do
                {
                    results = blobClient.ListBlobsSegmentedAsync(name + "/", token).Result;
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
        [Description("Test BlobClient ListBlobsSegmented - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListBlobsSegmentedPrefixCurrentTokenTask()
        {
            string containerName = GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            int blobCount = 3;

            string prefix = containerName + "/bb";
            BlobContinuationToken currentToken = null;

            try
            {
                container.CreateAsync().Wait();

                List<string> blobNames = CreateBlobsTask(container, blobCount, BlobType.BlockBlob);

                int totalCount = 0;
                do
                {
                    BlobResultSegment resultSegment = blobClient.ListBlobsSegmentedAsync(prefix, currentToken).Result;
                    currentToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlockBlob blockBlob in resultSegment.Results)
                    {
                        Assert.AreEqual(BlobType.BlockBlob, blockBlob.BlobType);
                        Assert.IsTrue(blockBlob.Name.StartsWith("bb"));
                        ++count;
                    }

                    totalCount += count;
                }
                while (currentToken != null);

                Assert.AreEqual(blobCount, totalCount);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test BlobClient ListBlobsSegmented - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListBlobsSegmentedPrefixCurrentTokenCancellationTokenTask()
        {
            string containerName = GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            int blobCount = 3;

            string prefix = containerName + "/bb";
            BlobContinuationToken currentToken = null;
            CancellationToken cancellationToken = new CancellationToken();

            try
            {
                container.CreateAsync().Wait();

                List<string> blobNames = CreateBlobsTask(container, blobCount, BlobType.BlockBlob);

                int totalCount = 0;
                do
                {
                    BlobResultSegment resultSegment = blobClient.ListBlobsSegmentedAsync(prefix, currentToken, cancellationToken).Result;
                    currentToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlockBlob blockBlob in resultSegment.Results)
                    {
                        Assert.AreEqual(BlobType.BlockBlob, blockBlob.BlobType);
                        Assert.IsTrue(blockBlob.Name.StartsWith("bb"));
                        ++count;
                    }

                    totalCount += count;
                }
                while (currentToken != null);

                Assert.AreEqual(blobCount, totalCount);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test BlobClient ListBlobsSegmented - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListBlobsSegmentedPrefixUseFlatBlobListingDetailsMaxResultsCurrentTokenOptionsOperationContextTask()
        {
            string containerName = GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            int blobCount = 3;

            string prefix = containerName + "/bb";
            bool useFlatBlobListing = false;
            BlobListingDetails blobListingDetails = BlobListingDetails.None;
            int? maxResults = 10;
            BlobContinuationToken currentToken = null;
            BlobRequestOptions options = new BlobRequestOptions();
            OperationContext operationContext = new OperationContext();

            try
            {
                container.CreateAsync().Wait();

                List<string> blobNames = CreateBlobsTask(container, blobCount, BlobType.BlockBlob);

                int totalCount = 0;
                do
                {
                    BlobResultSegment resultSegment = blobClient.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext).Result;
                    currentToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlockBlob blockBlob in resultSegment.Results)
                    {
                        Assert.AreEqual(BlobType.BlockBlob, blockBlob.BlobType);
                        Assert.IsTrue(blockBlob.Name.StartsWith("bb"));
                        ++count;
                    }

                    totalCount += count;

                    Assert.IsTrue(count <= maxResults.Value);
                }
                while (currentToken != null);

                Assert.AreEqual(blobCount, totalCount);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test BlobClient ListBlobsSegmented - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListBlobsSegmentedPrefixUseFlatBlobListingDetailsMaxResultsCurrentTokenOptionsOperationContextCancellationTokenTask()
        {
            string containerName = GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            int blobCount = 3;

            string prefix = containerName + "/bb";
            bool useFlatBlobListing = false;
            BlobListingDetails blobListingDetails = BlobListingDetails.None;
            int? maxResults = 10;
            BlobContinuationToken currentToken = null;
            BlobRequestOptions options = new BlobRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = new CancellationToken();

            try
            {
                container.CreateAsync().Wait();

                List<string> blobNames = CreateBlobsTask(container, blobCount, BlobType.BlockBlob);

                int totalCount = 0;
                do
                {
                    BlobResultSegment resultSegment = blobClient.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext, cancellationToken).Result;
                    currentToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlockBlob blockBlob in resultSegment.Results)
                    {
                        Assert.AreEqual(BlobType.BlockBlob, blockBlob.BlobType);
                        Assert.IsTrue(blockBlob.Name.StartsWith("bb"));
                        ++count;
                    }

                    totalCount += count;

                    Assert.IsTrue(count <= maxResults.Value);
                }
                while (currentToken != null);

                Assert.AreEqual(blobCount, totalCount);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("List containers")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainers()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            IEnumerable<CloudBlobContainer> results = blobClient.ListContainers();
            foreach (CloudBlobContainer container in results)
            {
                if (containerNames.Remove(container.Name))
                {
                    container.Delete();
                }
            }

            Assert.AreEqual(0, containerNames.Count);
        }

        [TestMethod]
        [Description("List containers with prefix")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersWithPrefix()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            IEnumerable<CloudBlobContainer> results = blobClient.ListContainers(name, ContainerListingDetails.None, null, null);
            Assert.AreEqual(containerNames.Count, results.Count());
            foreach (CloudBlobContainer container in results)
            {
                Assert.IsTrue(containerNames.Remove(container.Name));
                container.Delete();
            }

            results = blobClient.ListContainers(name, ContainerListingDetails.None, null, null);
            Assert.AreEqual(0, results.Count());
        }

        [TestMethod]
        [Description("Create a container with public access. Check public access is populated for Exists")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientCreateBlobAndCheckExistsWithPublicAccess()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            BlobContainerPublicAccessType[] accessValues = {BlobContainerPublicAccessType.Container, BlobContainerPublicAccessType.Off, BlobContainerPublicAccessType.Blob};
            BlobContainerPermissions permissions = new BlobContainerPermissions();
            foreach (BlobContainerPublicAccessType access in accessValues)
            {
                string name = GetRandomContainerName();
                CloudBlobContainer container = blobClient.GetContainerReference(name);
                container.Create(access);
                Assert.AreEqual(access, container.Properties.PublicAccess);

                CloudBlobContainer container2 = blobClient.GetContainerReference(name);
                Assert.AreEqual(access, container2.GetPermissions().PublicAccess);
                Assert.AreEqual(access, container2.Properties.PublicAccess);

                CloudBlobContainer container3 = blobClient.GetContainerReference(name);
                container3.Exists();
                Assert.AreEqual(access, container3.Properties.PublicAccess);

                container.Delete();
            }
        }

        [TestMethod]
        [Description("List containers and fetch attributes with public access")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersAndFetchAttributesWithPublicAccess()
        {
            string name = GetRandomContainerName();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            container.Create();

            BlobContainerPublicAccessType[] accessValues = { BlobContainerPublicAccessType.Container, BlobContainerPublicAccessType.Off, BlobContainerPublicAccessType.Blob };
            BlobContainerPermissions permissions = new BlobContainerPermissions();
            foreach (BlobContainerPublicAccessType access in accessValues)
            {
                permissions.PublicAccess = access;
                container.SetPermissions(permissions);
                Assert.AreEqual(access, container.Properties.PublicAccess);

                CloudBlobContainer container2 = blobClient.GetContainerReference(name);
                Assert.IsFalse(container2.Properties.PublicAccess.HasValue);
                container2.FetchAttributes();
                Assert.AreEqual(access, container2.Properties.PublicAccess);

                CloudBlobContainer container3 = blobClient.GetContainerReference(name);
                Assert.AreEqual(access, container3.GetPermissions().PublicAccess);
                Assert.AreEqual(access, container3.Properties.PublicAccess);

                IEnumerable<CloudBlobContainer> results = blobClient.ListContainers(name, ContainerListingDetails.None, null, null);
                Assert.AreEqual(1, results.Count());
                Assert.AreEqual(access, results.First().Properties.PublicAccess);
            }

            container.Delete();
        }

        [TestMethod]
        [Description("List containers with prefix using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersWithPrefixSegmented()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmented(name, ContainerListingDetails.None, 1, token);
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
                blobClient.GetContainerReference(containerName).Delete();
            }
        }

        [TestMethod]
        [Description("List containers with a prefix using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersWithPrefixSegmented2()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmented(name, token);
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudBlobContainer container in resultSegment.Results)
                {
                    count++;
                    listedContainerNames.Add(container.Name);
                }
            }
            while (token != null);

            Assert.AreEqual(containerNames.Count, listedContainerNames.Count);
            foreach (string containerName in listedContainerNames)
            {
                Assert.IsTrue(containerNames.Remove(containerName));
                blobClient.GetContainerReference(containerName).Delete();
            }
            Assert.AreEqual(0, containerNames.Count);
        }

        [TestMethod]
        [Description("List containers")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersWithPrefixSegmentedAPM()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result;
                do
                {
                    result = blobClient.BeginListContainersSegmented(name, token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    ContainerResultSegment resultSegment = blobClient.EndListContainersSegmented(result);
                    token = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        count++;
                        listedContainerNames.Add(container.Name);
                    }
                }
                while (token != null);

                Assert.AreEqual(containerNames.Count, listedContainerNames.Count);
                foreach (string containerName in listedContainerNames)
                {
                    Assert.IsTrue(containerNames.Remove(containerName));
                    blobClient.GetContainerReference(containerName).Delete();
                }
                Assert.AreEqual(0, containerNames.Count);
            }
        }

#if TASK
        [TestMethod]
        [Description("List containers with prefix using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersWithPrefixSegmentedTask()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).CreateAsync().Wait();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;

            do
            {
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(name, token).Result;
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudBlobContainer container in resultSegment.Results)
                {
                    count++;
                    listedContainerNames.Add(container.Name);
                }
            }
            while (token != null);

            Assert.AreEqual(containerNames.Count, listedContainerNames.Count);
            foreach (string containerName in listedContainerNames)
            {
                Assert.IsTrue(containerNames.Remove(containerName));
                blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
            }
            Assert.AreEqual(0, containerNames.Count);
        }
#endif

        [TestMethod]
        [Description("List more than 5K containers with prefix using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric)]
        public async Task CloudBlobClientListManyContainersSegmentedWithPrefix()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 5050; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                tasks.Add(Task.Run(() => blobClient.GetContainerReference(containerName).Create()));
                while (tasks.Count > 50)
                {
                    Task t = await Task.WhenAny(tasks);
                    await t;
                    tasks.Remove(t);
                }
            }
            await Task.WhenAll(tasks);

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmented(name, ContainerListingDetails.None, null, token);
                token = resultSegment.ContinuationToken;

                foreach (CloudBlobContainer container in resultSegment.Results)
                {
                    listedContainerNames.Add(container.Name);
                }
            }
            while (token != null);

            Assert.AreEqual(containerNames.Count, listedContainerNames.Count);

            tasks = new List<Task>();
            foreach (string containerName in listedContainerNames)
            {
                Assert.IsTrue(containerNames.Remove(containerName));
                tasks.Add(Task.Run(() => blobClient.GetContainerReference(containerName).Delete()));
                while (tasks.Count > 50)
                {
                    Task t = await Task.WhenAny(tasks);
                    await t;
                    tasks.Remove(t);
                }
            }
            await Task.WhenAll(tasks);
        }

        [TestMethod]
        [Description("Test Create Container with Shared Key Lite")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientCreateContainerSharedKeyLite()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.AuthenticationScheme = AuthenticationScheme.SharedKeyLite;

            string containerName = GetRandomContainerName();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            blobContainer.Create();

            bool exists = blobContainer.Exists();
            Assert.IsTrue(exists);

            blobContainer.Delete();
        }

        [TestMethod]
        [Description("List containers using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmented()
        {
            AssertSecondaryEndpoint();

            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmented(token);
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
                    blobClient.GetContainerReference(containerName).Delete();
                }
            }

            Assert.AreEqual(0, containerNames.Count);
        }

        [TestMethod]
        [Description("List containers")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedAPM()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).Create();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result;
                do
                {
                    result = blobClient.BeginListContainersSegmented(token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    ContainerResultSegment resultSegment = blobClient.EndListContainersSegmented(result);
                    token = resultSegment.ContinuationToken;

                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        listedContainerNames.Add(container.Name);
                    }
                }
                while (token != null);

                foreach (string containerName in listedContainerNames)
                {
                    if (containerNames.Remove(containerName))
                    {
                        blobClient.GetContainerReference(containerName).Delete();
                    }
                }

                Assert.AreEqual(0, containerNames.Count);
            }
        }

#if TASK
        [TestMethod]
        [Description("List containers using segmented listing")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedTask()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            for (int i = 0; i < 3; i++)
            {
                string containerName = name + i.ToString();
                containerNames.Add(containerName);
                blobClient.GetContainerReference(containerName).CreateAsync().Wait();
            }

            List<string> listedContainerNames = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(token).Result;
                token = resultSegment.ContinuationToken;

                foreach (CloudBlobContainer container in resultSegment.Results)
                {
                    listedContainerNames.Add(container.Name);
                }
            }
            while (token != null);

            foreach (string containerName in listedContainerNames)
            {
                if (containerNames.Remove(containerName))
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }

            Assert.AreEqual(0, containerNames.Count);
        }

        [TestMethod]
        [Description("CloudBlobClient ListContainersSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedContinuationTokenTask()
        {
            int containerCount = 3;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            BlobContinuationToken continuationToken = null;

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(continuationToken).Result;
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }

        [TestMethod]
        [Description("CloudBlobClient ListContainersSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedContinuationTokenCancellationTokenTask()
        {
            int containerCount = 3;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            BlobContinuationToken continuationToken = null;
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(continuationToken, cancellationToken).Result;
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }

        [TestMethod]
        [Description("CloudBlobClient ListContainersSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedPrefixContinuationToken()
        {
            int containerCount = 3;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            string prefix = containerNamePrefix;
            BlobContinuationToken continuationToken = null;

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(prefix, continuationToken).Result;
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }

        [TestMethod]
        [Description("CloudBlobClient ListContainersSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedPrefixContinuationTokenCancellationTokenTask()
        {
            int containerCount = 3;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            string prefix = containerNamePrefix;
            BlobContinuationToken continuationToken = null;
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(prefix, continuationToken, cancellationToken).Result;
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }

        [TestMethod]
        [Description("A test to validate basic blob container continuation with null target location")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainerWithContinuationTokenNullTarget()
        {
            int containerCount = 8;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            BlobRequestOptions requestOptions = new BlobRequestOptions();
            OperationContext operationContext = new OperationContext();
            string prefix = containerNamePrefix;
            BlobContinuationToken continuationToken = null;
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                int tokenCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(containerNamePrefix, ContainerListingDetails.All, 1, continuationToken, requestOptions, operationContext, cancellationToken).Result;
                    continuationToken = resultSegment.ContinuationToken;
                    //first result segment might not actually return any results
                    if(resultSegment.Results.Any())
                        tokenCount++;

                    if (tokenCount < containerCount)
                    {
                        Assert.IsNotNull(continuationToken);
                        continuationToken.TargetLocation = null;
                    }

                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
                Assert.AreEqual(containerCount, tokenCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }

        [TestMethod]
        [Description("CloudBlobClient ListContainersSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedPrefixDetailsIncludedMaxResultsContinuationTokenOptionsOperationContextTask()
        {
            int containerCount = 3;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            string prefix = containerNamePrefix;
            ContainerListingDetails detailsIncluded = ContainerListingDetails.None;
            int? maxResults = 10;
            BlobContinuationToken continuationToken = null;
            BlobRequestOptions options = new BlobRequestOptions();
            OperationContext operationContext = new OperationContext();

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(prefix, detailsIncluded, maxResults, continuationToken, options, operationContext).Result;
                    continuationToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                        ++count;
                    }

                    Assert.IsTrue(count <= maxResults.Value);
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }

        [TestMethod]
        [Description("CloudBlobClient ListContainersSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersSegmentedPrefixDetailsIncludedMaxResultsContinuationTokenOptionsOperationContextCancellationTokenTask()
        {
            int containerCount = 3;
            string containerNamePrefix = GetRandomContainerName();
            List<string> containerNames = new List<string>(containerCount);
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            string prefix = containerNamePrefix;
            ContainerListingDetails detailsIncluded = ContainerListingDetails.None;
            int? maxResults = 10;
            BlobContinuationToken continuationToken = null;
            BlobRequestOptions options = new BlobRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < containerCount; ++i)
                {
                    string containerName = containerNamePrefix + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).CreateAsync().Wait();
                }

                int totalCount = 0;
                do
                {
                    ContainerResultSegment resultSegment = blobClient.ListContainersSegmentedAsync(prefix, detailsIncluded, maxResults, continuationToken, options, operationContext, cancellationToken).Result;
                    continuationToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudBlobContainer container in resultSegment.Results)
                    {
                        if (containerNames.Contains(container.Name))
                        {
                            ++totalCount;
                        }
                        ++count;
                    }

                    Assert.IsTrue(count <= maxResults.Value);
                }
                while (continuationToken != null);

                Assert.AreEqual(containerCount, totalCount);
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).DeleteAsync().Wait();
                }
            }
        }
#endif

        [TestMethod]
        [Description("Ensure continuing a listing in another location fails")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientListContainersInMultiLocations()
        {
            string name = GetRandomContainerName();
            List<string> containerNames = new List<string>();
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    string containerName = name + i.ToString();
                    containerNames.Add(containerName);
                    blobClient.GetContainerReference(containerName).Create();
                }

                List<string> listedContainerNames = new List<string>();
                ContainerResultSegment resultSegment = blobClient.ListContainersSegmented(name, ContainerListingDetails.None, 1, null);
                Assert.AreEqual(StorageLocation.Primary, resultSegment.ContinuationToken.TargetLocation);

                BlobRequestOptions options = new BlobRequestOptions()
                {
                    LocationMode = LocationMode.SecondaryOnly,
                };

                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => blobClient.ListContainersSegmented(name, ContainerListingDetails.None, 1, resultSegment.ContinuationToken, options),
                    "Continuing a listing operation in a different location should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
            }
            finally
            {
                foreach (string containerName in containerNames)
                {
                    blobClient.GetContainerReference(containerName).Delete();
                }
            }
        }

        [TestMethod]
        [Description("Upload a blob with a small maximum execution time")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientMaximumExecutionTimeout()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(GetRandomContainerName());
            byte[] buffer = BlobTestBase.GetRandomBuffer(80 * 1024 * 1024);

            try
            {
                container.Create();
                blobClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(5);
                blobClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 2 * 1024 * 1024;

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                blockBlob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    try
                    {
                        blockBlob.UploadFromStream(ms);
                        Assert.Fail();
                    }
                    catch (TimeoutException ex)
                    {
                        Assert.IsInstanceOfType(ex, typeof(TimeoutException));
                    }
                    catch (StorageException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(TimeoutException));
                    }
                }

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                pageBlob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    try
                    {
                        pageBlob.UploadFromStream(ms);
                        Assert.Fail();
                    }
                    catch (TimeoutException ex)
                    {
                        Assert.IsInstanceOfType(ex, typeof(TimeoutException));
                    }
                    catch (StorageException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(TimeoutException));
                    }
                }
            }
            finally
            {
                blobClient.DefaultRequestOptions.MaximumExecutionTime = null;
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Make sure MaxExecutionTime is not enforced when using streams")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientMaximumExecutionTimeoutShouldNotBeHonoredForStreams()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Guid.NewGuid().ToString("N"));
            byte[] buffer = BlobTestBase.GetRandomBuffer(1024 * 1024);

            try
            {
                container.Create();

                blobClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(2);
                CloudBlob[] blobs = new CloudBlob[] {
                container.GetBlockBlobReference("blob1"),
                container.GetPageBlobReference("blob2"),
                container.GetAppendBlobReference("blob3") };
                Func<CloudBlob, CloudBlobStream>[] generateWriteStream = new Func<CloudBlob, CloudBlobStream>[]
                {
                    blob => ((CloudBlockBlob)blob).OpenWrite(),
                    blob => ((CloudPageBlob)blob).OpenWrite(8 * 1024 * 1024),
                    blob => ((CloudAppendBlob)blob).OpenWrite(true),
                };
                List<Task> tasks = new List<Task>();

                for (int blobID = 0; blobID < 3; blobID++)
                {
                    int curBlobID = blobID;
                    CloudBlob blob = blobs[curBlobID];
                    ((dynamic)blob).StreamWriteSizeInBytes = 1024 * 1024;
                    blob.StreamMinimumReadSizeInBytes = 1024 * 1024;
                    tasks.Add(Task.Run(() =>
                    {
                        using (CloudBlobStream bos = generateWriteStream[curBlobID](blob))
                        {
                            DateTime start = DateTime.Now;

                            for (int i = 0; i < 7; i++)
                            {
                                bos.Write(buffer, 0, buffer.Length);
                            }

                            // Sleep to ensure we are over the Max execution time when we do the last write
                            int msRemaining = (int)(blobClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                            if (msRemaining > 0)
                            {
                                Thread.Sleep(msRemaining);
                            }

                            bos.Write(buffer, 0, buffer.Length);
                        }

                        using (Stream bis = blob.OpenRead())
                        {
                            DateTime start = DateTime.Now;

                            int total = 0;
                            while (total < 7 * 1024 * 1024)
                            {
                                total += bis.Read(buffer, 0, buffer.Length);
                            }

                            // Sleep to ensure we are over the Max execution time when we do the last read
                            int msRemaining = (int)(blobClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                            if (msRemaining > 0)
                            {
                                Thread.Sleep(msRemaining);
                            }

                            while (true)
                            {
                                int count = bis.Read(buffer, 0, buffer.Length);
                                total += count;
                                if (count == 0)
                                    break;
                            }
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray());
            }

            finally
            {

                blobClient.DefaultRequestOptions.MaximumExecutionTime = null;
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetServiceStats()
        {
            AssertSecondaryEndpoint();

            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            TestHelper.VerifyServiceStats(client.GetServiceStats());
        }

        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetServiceStatsAPM()
        {
            AssertSecondaryEndpoint();

            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = client.BeginGetServiceStats(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.VerifyServiceStats(client.EndGetServiceStats(result));

                result = client.BeginGetServiceStats(
                    null,
                    new OperationContext(),
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.VerifyServiceStats(client.EndGetServiceStats(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetServiceStatsAsync()
        {
            AssertSecondaryEndpoint();

            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            TestHelper.VerifyServiceStats(client.GetServiceStatsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - SYNC")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetServiceStatsInvalidLoc()
        {
            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                client.GetServiceStats();
                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException.");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetServiceStatsInvalidLocAPM()
        {
            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = client.BeginGetServiceStats(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                }

                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - ASYNC")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientGetServiceStatsInvalidLocAsync()
        {
            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                client.GetServiceStatsAsync();
                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException.");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        [Description("Server timeout query parameter")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientServerTimeout()
        {
            CloudBlobClient client = GenerateCloudBlobClient();

            string timeout = null;
            OperationContext context = new OperationContext();
            context.SendingRequest += (sender, e) =>
            {
                IDictionary<string, string> query = HttpWebUtility.ParseQueryString(e.Request.RequestUri.Query);
                if (!query.TryGetValue("timeout", out timeout))
                {
                    timeout = null;
                }
            };

            BlobRequestOptions options = new BlobRequestOptions();
            client.GetServiceProperties(null, context);
            Assert.IsNull(timeout);
            client.GetServiceProperties(options, context);
            Assert.IsNull(timeout);

            options.ServerTimeout = TimeSpan.FromSeconds(100);
            client.GetServiceProperties(options, context);
            Assert.AreEqual("100", timeout);

            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(90);
            client.GetServiceProperties(null, context);
            Assert.AreEqual("90", timeout);
            client.GetServiceProperties(options, context);
            Assert.AreEqual("100", timeout);

            options.ServerTimeout = null;
            client.GetServiceProperties(options, context);
            Assert.AreEqual("90", timeout);

            options.ServerTimeout = TimeSpan.Zero;
            client.GetServiceProperties(options, context);
            Assert.IsNull(timeout);
        }

        [TestMethod]
        [Description("Check for invalid delimiter \\")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientDelimiterCheck()
        {
            try
            {
                CloudBlobClient client = GenerateCloudBlobClient();
                client.DefaultDelimiter = "\\";
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentException));
            }
        }

        [TestMethod]
        [Description("Check for maximum execution time limit")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobClientMaximumExecutionTimeCheck()
        {
            try
            {
                CloudBlobClient client = GenerateCloudBlobClient();
                client.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromDays(25.0);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
            }
        }
    }
}