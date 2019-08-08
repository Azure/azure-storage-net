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

namespace Microsoft.Azure.Storage.File
{
    [TestClass]
    public class CloudFileClientTest : FileTestBase
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
        public void CloudFileClientWithUppercaseAccountName()
        {
            StorageCredentials credentials = new StorageCredentials(TestBase.StorageCredentials.AccountName.ToUpper(), TestBase.StorageCredentials.ExportKey());
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint);
            CloudFileClient fileClient = new CloudFileClient(baseAddressUri, TestBase.StorageCredentials);
            CloudFileShare share = fileClient.GetShareReference("share");
            share.Exists();
        }

        [TestMethod]
        [Description("Create a service client with token")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientWithToken()
        {
            TokenCredential token = new TokenCredential(TestBase.GenerateOAuthToken());
            StorageCredentials credentials = new StorageCredentials(token);
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint);

            CloudFileClient client = new CloudFileClient(baseAddressUri, credentials);
            CloudFileShare share = client.GetShareReference("share");

            try
            {
                share.Exists();
                Assert.Fail();
            }
            catch (Exception ex)
            {
                // InvalidOperationException in legacy, but unable to port that specific behavior to
                // split library due to change in authentication handling
                Assert.IsInstanceOfType(ex, typeof(StorageException));
            }
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
        public void CloudFileClientListShares()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            DelegatingHandlerImpl delegatingHandlerImpl = new DelegatingHandlerImpl(new DelegatingHandlerImpl());
            CloudFileClient fileClient = GenerateCloudFileClient(delegatingHandlerImpl);

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            IEnumerable<CloudFileShare> results = fileClient.ListShares();

            foreach (CloudFileShare share in results)
            {
                Assert.IsTrue(share.Properties.Quota.HasValue);
                if (shareNames.Remove(share.Name))
                {
                    share.Delete();
                }
            }

            Assert.AreEqual(0, shareNames.Count);
            Assert.AreNotEqual(0, delegatingHandlerImpl.CallCount);
        }

        [TestMethod]
        [Description("List shares with prefix")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesWithPrefix()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            IEnumerable<CloudFileShare> results = fileClient.ListShares(name, ShareListingDetails.None, null, null);
            Assert.AreEqual(shareNames.Count, results.Count());
            foreach (CloudFileShare share in results)
            {
                Assert.IsTrue(shareNames.Remove(share.Name));
                share.Delete();
            }

            results = fileClient.ListShares(name, ShareListingDetails.None, null, null);
            Assert.AreEqual(0, results.Count());
        }

        [TestMethod]
        [Description("List shares with prefix using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesWithPrefixSegmented()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment = fileClient.ListSharesSegmented(name, ShareListingDetails.None, 1, token);
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
                fileClient.GetShareReference(shareName).Delete();
            }
        }

        [TestMethod]
        [Description("List shares with a prefix using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesWithPrefixSegmented2()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment = fileClient.ListSharesSegmented(name, token);
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudFileShare share in resultSegment.Results)
                {
                    count++;
                    listedShareNames.Add(share.Name);
                }
            }
            while (token != null);

            Assert.AreEqual(shareNames.Count, listedShareNames.Count);
            foreach (string shareName in listedShareNames)
            {
                Assert.IsTrue(shareNames.Remove(shareName));
                fileClient.GetShareReference(shareName).Delete();
            }
            Assert.AreEqual(0, shareNames.Count);
        }

        [TestMethod]
        [Description("List shares")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesWithPrefixSegmentedAPM()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result;
                do
                {
                    result = fileClient.BeginListSharesSegmented(name, token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    ShareResultSegment resultSegment = fileClient.EndListSharesSegmented(result);
                    token = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        count++;
                        listedShareNames.Add(share.Name);
                    }
                }
                while (token != null);

                Assert.AreEqual(shareNames.Count, listedShareNames.Count);
                foreach (string shareName in listedShareNames)
                {
                    Assert.IsTrue(shareNames.Remove(shareName));
                    fileClient.GetShareReference(shareName).Delete();
                }
                Assert.AreEqual(0, shareNames.Count);
            }
        }

#if TASK
        [TestMethod]
        [Description("List shares with prefix using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesWithPrefixSegmentedTask()
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
                ShareResultSegment resultSegment = await fileClient.ListSharesSegmentedAsync(name, token);
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudFileShare share in resultSegment.Results)
                {
                    count++;
                    listedShareNames.Add(share.Name);
                }
            }
            while (token != null);

            Assert.AreEqual(shareNames.Count, listedShareNames.Count);
            foreach (string shareName in listedShareNames)
            {
                Assert.IsTrue(shareNames.Remove(shareName));
                await fileClient.GetShareReference(shareName).DeleteAsync();
            }
            Assert.AreEqual(0, shareNames.Count);
        }
#endif

        [TestMethod]
        [Description("List more than 5K shares with prefix using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        public async Task CloudFileClientListManySharesSegmentedWithPrefix()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 5050; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                tasks.Add(Task.Run(() => fileClient.GetShareReference(shareName).Create()));
                while (tasks.Count > 50)
                {
                    Task t = await Task.WhenAny(tasks);
                    await t;
                    tasks.Remove(t);
                }
            }
            await Task.WhenAll(tasks);

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment = fileClient.ListSharesSegmented(name, ShareListingDetails.None, null, token);
                token = resultSegment.ContinuationToken;

                foreach (CloudFileShare share in resultSegment.Results)
                {
                    listedShareNames.Add(share.Name);
                }
            }
            while (token != null);

            Assert.AreEqual(shareNames.Count, listedShareNames.Count);
            tasks = new List<Task>();
            foreach (string shareName in listedShareNames)
            {
                Assert.IsTrue(shareNames.Remove(shareName));
                tasks.Add(Task.Run(() => fileClient.GetShareReference(shareName).Delete()));
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
        [Description("Test Create Share with Shared Key Lite")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientCreateShareSharedKeyLite()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            fileClient.AuthenticationScheme = AuthenticationScheme.SharedKeyLite;

            string shareName = GetRandomShareName();
            CloudFileShare share = fileClient.GetShareReference(shareName);
            share.Create();

            bool exists = share.Exists();
            Assert.IsTrue(exists);

            share.Delete();
        }

        [TestMethod]
        [Description("List shares using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesSegmented()
        {
            AssertSecondaryEndpoint();

            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment = fileClient.ListSharesSegmented(token);
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
                    fileClient.GetShareReference(shareName).Delete();
                }
            }

            Assert.AreEqual(0, shareNames.Count);
        }

        [TestMethod]
        [Description("List shares")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesSegmentedAPM()
        {
            string name = GetRandomShareName();
            List<string> shareNames = new List<string>();
            CloudFileClient fileClient = GenerateCloudFileClient();

            for (int i = 0; i < 3; i++)
            {
                string shareName = name + i.ToString();
                shareNames.Add(shareName);
                fileClient.GetShareReference(shareName).Create();
            }

            List<string> listedShareNames = new List<string>();
            FileContinuationToken token = null;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result;
                do
                {
                    result = fileClient.BeginListSharesSegmented(token, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    ShareResultSegment resultSegment = fileClient.EndListSharesSegmented(result);
                    token = resultSegment.ContinuationToken;

                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        listedShareNames.Add(share.Name);
                    }
                }
                while (token != null);

                foreach (string shareName in listedShareNames)
                {
                    if (shareNames.Remove(shareName))
                    {
                        fileClient.GetShareReference(shareName).Delete();
                    }
                }

                Assert.AreEqual(0, shareNames.Count);
            }
        }

#if TASK
        [TestMethod]
        [Description("List shares using segmented listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedTask()
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
        [Description("CloudFileClient ListSharesSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedContinuationTokenTask()
        {
            int shareCount = 3;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();

            FileContinuationToken continuationToken = null;

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    await fileClient.GetShareReference(shareName).CreateAsync();
                }

                int totalCount = 0;
                do
                {
                    ShareResultSegment resultSegment = await fileClient.ListSharesSegmentedAsync(continuationToken);
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    await fileClient.GetShareReference(shareName).DeleteAsync();
                }
            }
        }

        [TestMethod]
        [Description("A test to validate basic file share list continuation with null target location")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesSegmentedWithContinuationTokenNullTarget()
        {
            int shareCount = 8;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();
            FileContinuationToken continuationToken = null;

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    fileClient.GetShareReference(shareName).Create();
                }

                int totalCount = 0;
                int tokenCount = 0;
                do
                {
                    ShareResultSegment resultSegment = fileClient.ListSharesSegmented(shareNamePrefix, ShareListingDetails.All, 1, continuationToken, null, null);
                    tokenCount++;
                    continuationToken = resultSegment.ContinuationToken;
                    if (tokenCount < shareCount)
                    {
                        Assert.IsNotNull(continuationToken);
                        continuationToken.TargetLocation = null;
                    }

                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
                Assert.AreEqual(shareCount, tokenCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    fileClient.GetShareReference(shareName).Delete();
                }
            }
        }

        [TestMethod]
        [Description("CloudFileClient ListSharesSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedContinuationTokenCancellationTokenTask()
        {
            int shareCount = 3;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();

            FileContinuationToken continuationToken = null;
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    await fileClient.GetShareReference(shareName).CreateAsync();
                }

                int totalCount = 0;
                do
                {
                    ShareResultSegment resultSegment = await fileClient.ListSharesSegmentedAsync(continuationToken, cancellationToken);
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    await fileClient.GetShareReference(shareName).DeleteAsync();
                }
            }
        }

        [TestMethod]
        [Description("CloudFileClient ListSharesSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientListSharesSegmentedPrefixContinuationToken()
        {
            int shareCount = 3;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();

            string prefix = shareNamePrefix;
            FileContinuationToken continuationToken = null;

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    fileClient.GetShareReference(shareName).Create();
                }

                int totalCount = 0;
                do
                {
                    ShareResultSegment resultSegment = fileClient.ListSharesSegmented(prefix, continuationToken);
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    fileClient.GetShareReference(shareName).Delete();
                }
            }
        }

        [TestMethod]
        [Description("CloudFileClient ListSharesSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedPrefixContinuationTokenCancellationTokenTask()
        {
            int shareCount = 3;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();

            string prefix = shareNamePrefix;
            FileContinuationToken continuationToken = null;
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    await fileClient.GetShareReference(shareName).CreateAsync();
                }

                int totalCount = 0;
                do
                {
                    ShareResultSegment resultSegment 
                        = await fileClient.ListSharesSegmentedAsync(prefix, continuationToken, cancellationToken);
                    continuationToken = resultSegment.ContinuationToken;

                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                    }
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    await fileClient.GetShareReference(shareName).DeleteAsync();
                }
            }
        }

        [TestMethod]
        [Description("CloudFileClient ListSharesSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedPrefixDetailsIncludedMaxResultsContinuationTokenOptionsOperationContextTask()
        {
            int shareCount = 3;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();

            string prefix = shareNamePrefix;
            ShareListingDetails detailsIncluded = ShareListingDetails.None;
            int? maxResults = 10;
            FileContinuationToken continuationToken = null;
            FileRequestOptions options = new FileRequestOptions();
            OperationContext operationContext = new OperationContext();

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    await fileClient.GetShareReference(shareName).CreateAsync();
                }

                int totalCount = 0;
                do
                {
                    ShareResultSegment resultSegment 
                        = await fileClient.ListSharesSegmentedAsync(prefix, detailsIncluded, maxResults, continuationToken, options, operationContext);
                    continuationToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                        ++count;
                    }

                    Assert.IsTrue(count <= maxResults.Value);
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    await fileClient.GetShareReference(shareName).DeleteAsync();
                }
            }
        }

        [TestMethod]
        [Description("CloudFileClient ListSharesSegmentedAsync - Task")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileClientListSharesSegmentedPrefixDetailsIncludedMaxResultsContinuationTokenOptionsOperationContextCancellationTokenTask()
        {
            int shareCount = 3;
            string shareNamePrefix = GetRandomShareName();
            List<string> shareNames = new List<string>(shareCount);
            CloudFileClient fileClient = GenerateCloudFileClient();

            string prefix = shareNamePrefix;
            ShareListingDetails detailsIncluded = ShareListingDetails.None;
            int? maxResults = 10;
            FileContinuationToken continuationToken = null;
            FileRequestOptions options = new FileRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                for (int i = 0; i < shareCount; ++i)
                {
                    string shareName = shareNamePrefix + i.ToString();
                    shareNames.Add(shareName);
                    await fileClient.GetShareReference(shareName).CreateAsync();
                }

                int totalCount = 0;
                do
                {
                    ShareResultSegment resultSegment 
                        = await fileClient.ListSharesSegmentedAsync(prefix, detailsIncluded, maxResults, continuationToken, options, operationContext, cancellationToken);
                    continuationToken = resultSegment.ContinuationToken;

                    int count = 0;
                    foreach (CloudFileShare share in resultSegment.Results)
                    {
                        if (shareNames.Contains(share.Name))
                        {
                            ++totalCount;
                        }
                        ++count;
                    }

                    Assert.IsTrue(count <= maxResults.Value);
                }
                while (continuationToken != null);

                Assert.AreEqual(shareCount, totalCount);
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    await fileClient.GetShareReference(shareName).DeleteAsync();
                }
            }
        }
#endif

        [TestMethod]
        [Description("Upload a file with a small maximum execution time")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientMaximumExecutionTimeout()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(GetRandomShareName());
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
            byte[] buffer = FileTestBase.GetRandomBuffer(80 * 1024 * 1024);

            try
            {
                share.Create();
                fileClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(5);

                CloudFile file = rootDirectory.GetFileReference("file1");
                file.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    try
                    {
                        file.UploadFromStream(ms);
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
                fileClient.DefaultRequestOptions.MaximumExecutionTime = null;
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Make sure MaxExecutionTime is not enforced when using streams")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientMaximumExecutionTimeoutShouldNotBeHonoredForStreams()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(Guid.NewGuid().ToString("N"));
            byte[] buffer = FileTestBase.GetRandomBuffer(1024 * 1024);

            try
            {
                share.Create();

                fileClient.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(2);
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                file.StreamWriteSizeInBytes = 1024 * 1024;
                file.StreamMinimumReadSizeInBytes = 1024 * 1024;

                using (CloudFileStream bos = file.OpenWrite(8 * 1024 * 1024))
                {
                    DateTime start = DateTime.Now;

                    for (int i = 0; i < 7; i++)
                    {
                        bos.Write(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last write
                    int msRemaining = (int)(fileClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

                    if (msRemaining > 0)
                    {
                        Thread.Sleep(msRemaining);
                    }

                    bos.Write(buffer, 0, buffer.Length);
                }

                using (Stream bis = file.OpenRead())
                {
                    DateTime start = DateTime.Now;
                    int total = 0;
                    while (total < 7 * 1024 * 1024)
                    {
                        total += bis.Read(buffer, 0, buffer.Length);
                    }

                    // Sleep to ensure we are over the Max execution time when we do the last read
                    int msRemaining = (int)(fileClient.DefaultRequestOptions.MaximumExecutionTime.Value - (DateTime.Now - start)).TotalMilliseconds;

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
            }
            finally
            {
                fileClient.DefaultRequestOptions.MaximumExecutionTime = null;
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Server timeout query parameter")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientServerTimeout()
        {
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare share = client.GetShareReference("timeouttest");

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

            FileRequestOptions options = new FileRequestOptions();
            share.Exists(null, context);
            Assert.IsNull(timeout);
            share.Exists(options, context);
            Assert.IsNull(timeout);

            options.ServerTimeout = TimeSpan.FromSeconds(100);
            share.Exists(options, context);
            Assert.AreEqual("100", timeout);

            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(90);
            share.Exists(null, context);
            Assert.AreEqual("90", timeout);
            share.Exists(options, context);
            Assert.AreEqual("100", timeout);

            options.ServerTimeout = null;
            share.Exists(options, context);
            Assert.AreEqual("90", timeout);

            options.ServerTimeout = TimeSpan.Zero;
            share.Exists(options, context);
            Assert.IsNull(timeout);
        }

        [TestMethod]
        [Description("Check for maximum execution time limit")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileClientMaximumExecutionTimeCheck()
        {
            try
            {
                CloudFileClient client = GenerateCloudFileClient();
                client.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromDays(25.0);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
            }
        }

        [TestMethod]
        [Description("Test list shares with a snapshot")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListSharesWithSnapshot()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            share.Metadata["key1"] = "value1";
            share.SetMetadata();

            CloudFileShare snapshot = share.Snapshot();
            share.Metadata["key2"] = "value2";
            share.SetMetadata();

            CloudFileClient client = GenerateCloudFileClient();
            IEnumerable<CloudFileShare> listResult = client.ListShares(share.Name, ShareListingDetails.All, null, null);

            int count = 0;
            bool originalFound = false;
            bool snapshotFound = false;
            foreach (CloudFileShare listShareItem in listResult)
            {
                if (listShareItem.Name.Equals(share.Name) && !listShareItem.IsSnapshot && !originalFound)
                {
                    count++;
                    originalFound = true;
                    Assert.AreEqual(2, listShareItem.Metadata.Count);
                    Assert.AreEqual("value2", listShareItem.Metadata["key2"]);
                    // Metadata keys should be case-insensitive
                    Assert.AreEqual("value2", listShareItem.Metadata["KEY2"]);
                    Assert.AreEqual("value1", listShareItem.Metadata["key1"]);
                    Assert.AreEqual(share.StorageUri, listShareItem.StorageUri);
                }
                else if (listShareItem.Name.Equals(share.Name) &&
                        listShareItem.IsSnapshot && !snapshotFound)
                {
                    count++;
                    snapshotFound = true;
                    Assert.AreEqual(1, listShareItem.Metadata.Count);
                    Assert.AreEqual("value1", listShareItem.Metadata["key1"]);
                    Assert.AreEqual(snapshot.StorageUri, listShareItem.StorageUri);
                }
            }

            Assert.AreEqual(2, count);

            snapshot.Delete();
            share.Delete();
        }

        [TestMethod]
        [Description("Test list shares with a snapshot - APM")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListSharesWithSnapshotAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                share.EndCreate(result);

                share.Metadata["key1"] = "value1";
                result = share.BeginSetMetadata(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                share.EndSetMetadata(result);

                result = share.BeginSnapshot(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                CloudFileShare snapshot = share.EndSnapshot(result);


                share.Metadata["key2"] = "value2";
                result = share.BeginSetMetadata(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                share.EndSetMetadata(result);

                CloudFileClient client = GenerateCloudFileClient();
                result = client.BeginListSharesSegmented(share.Name, ShareListingDetails.All, null, null, null, null, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                IEnumerable<CloudFileShare> listResult = client.EndListSharesSegmented(result).Results;

                int count = 0;
                bool originalFound = false;
                bool snapshotFound = false;
                foreach (CloudFileShare listShareItem in listResult)
                {
                    if (listShareItem.Name.Equals(share.Name) && !listShareItem.IsSnapshot && !originalFound)
                    {
                        count++;
                        originalFound = true;
                        Assert.AreEqual(2, listShareItem.Metadata.Count);
                        Assert.AreEqual("value2", listShareItem.Metadata["key2"]);
                        // Metadata keys should be case-insensitive
                        Assert.AreEqual("value2", listShareItem.Metadata["KEY2"]);
                        Assert.AreEqual("value1", listShareItem.Metadata["key1"]);
                        Assert.AreEqual(share.StorageUri, listShareItem.StorageUri);
                    }
                    else if (listShareItem.Name.Equals(share.Name) &&
                            listShareItem.IsSnapshot && !snapshotFound)
                    {
                        count++;
                        snapshotFound = true;
                        Assert.AreEqual(1, listShareItem.Metadata.Count);
                        Assert.AreEqual("value1", listShareItem.Metadata["key1"]);
                        Assert.AreEqual(snapshot.StorageUri, listShareItem.StorageUri);
                    }
                }

                Assert.AreEqual(2, count);

                snapshot.Delete();
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Test list shares with a snapshot - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileListSharesWithSnapshotTask()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            share.Metadata["key1"] = "value1";
            await share.SetMetadataAsync();

            CloudFileShare snapshot = share.Snapshot();
            share.Metadata["key2"] = "value2";
            await share.SetMetadataAsync();

            CloudFileClient client = GenerateCloudFileClient();
            List<CloudFileShare> listedShares = new List<CloudFileShare>();
            FileContinuationToken token = null;
            do
            {
                ShareResultSegment resultSegment 
                    = await client.ListSharesSegmentedAsync(share.Name, ShareListingDetails.All, null, token, null, null);
                token = resultSegment.ContinuationToken;

                foreach (CloudFileShare listResultShare in resultSegment.Results)
                {
                    listedShares.Add(listResultShare);
                }
            }
            while (token != null);

            int count = 0;
            bool originalFound = false;
            bool snapshotFound = false;
            foreach (CloudFileShare listShareItem in listedShares)
            {
                if (listShareItem.Name.Equals(share.Name) && !listShareItem.IsSnapshot && !originalFound)
                {
                    count++;
                    originalFound = true;
                    Assert.AreEqual(2, listShareItem.Metadata.Count);
                    Assert.AreEqual("value2", listShareItem.Metadata["key2"]);
                    // Metadata keys should be case-insensitive
                    Assert.AreEqual("value2", listShareItem.Metadata["KEY2"]);
                    Assert.AreEqual("value1", listShareItem.Metadata["key1"]);
                    Assert.AreEqual(share.StorageUri, listShareItem.StorageUri);
                }
                else if (listShareItem.Name.Equals(share.Name) &&
                        listShareItem.IsSnapshot && !snapshotFound)
                {
                    count++;
                    snapshotFound = true;
                    Assert.AreEqual(1, listShareItem.Metadata.Count);
                    Assert.AreEqual("value1", listShareItem.Metadata["key1"]);
                    Assert.AreEqual(snapshot.StorageUri, listShareItem.StorageUri);
                }
            }

            Assert.AreEqual(2, count);

            await snapshot.DeleteAsync();
            await share.DeleteAsync();
        }
#endif
    }
}
