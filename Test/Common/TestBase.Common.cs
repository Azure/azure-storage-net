// -----------------------------------------------------------------------------------------
// <copyright file="TestBase.Common.cs" company="Microsoft">
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

using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Linq;
using System.ServiceModel.Channels;

#if WINDOWS_DESKTOP || NETCOREAPP2_0
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace Microsoft.Azure.Storage
{
    public partial class TestBase
    {
        private const AuthenticationScheme DefaultAuthenticationScheme = AuthenticationScheme.SharedKey;

        public static byte[] GetRandomBuffer(long size)
        {
            byte[] buffer = new byte[size];
            Random random = new Random();
            random.NextBytes(buffer);
            return buffer;
        }

        public static void AssertSecondaryEndpoint()
        {
            if ((TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint == null) ||
                (TestBase.TargetTenantConfig.QueueServiceSecondaryEndpoint == null) ||
                (TestBase.TargetTenantConfig.TableServiceSecondaryEndpoint == null))
            {
                Assert.Inconclusive("Secondary endpoints are not defined for target tenant");
            }
        }

#if WINDOWS_DESKTOP
        public static WCFBufferManagerAdapter BlobBufferManager = new WCFBufferManagerAdapter(BufferManager.CreateBufferManager(512 * (int)Constants.MB, 64 * (int)Constants.KB), 64 * (int)Constants.KB);

        public static WCFBufferManagerAdapter FileBufferManager = new WCFBufferManagerAdapter(BufferManager.CreateBufferManager(512 * (int)Constants.MB, 64 * (int)Constants.KB), 64 * (int)Constants.KB);

        public static WCFBufferManagerAdapter TableBufferManager = new WCFBufferManagerAdapter(BufferManager.CreateBufferManager(256 * (int)Constants.MB, 64 * (int)Constants.KB), 64 * (int)Constants.KB);

        public static WCFBufferManagerAdapter QueueBufferManager = new WCFBufferManagerAdapter(BufferManager.CreateBufferManager(64 * (int)Constants.MB, (int)Constants.KB), (int)Constants.KB);
#else
        public static MockBufferManager BlobBufferManager = new MockBufferManager(64 * (int)Constants.KB);

        public static MockBufferManager FileBufferManager = new MockBufferManager(64 * (int)Constants.KB);

        public static MockBufferManager TableBufferManager = new MockBufferManager(64 * (int)Constants.KB);

        public static MockBufferManager QueueBufferManager = new MockBufferManager((int)Constants.KB);
#endif

        public static CloudBlobClient GenerateCloudBlobClient()
        {
            CloudBlobClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.BlobServiceEndpoint);
                client = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(TestBase.TargetTenantConfig.BlobServiceEndpoint),
                    new Uri(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint));
                client = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;

#if WINDOWS_DESKTOP
            client.BufferManager = TableBufferManager;
#endif

            return client;
        }

        public static CloudFileClient GenerateCloudFileClient()
        {
            CloudFileClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.FileServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint);
                client = new CloudFileClient(baseAddressUri, TestBase.StorageCredentials);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint),
                    new Uri(TestBase.TargetTenantConfig.FileServiceSecondaryEndpoint));
                client = new CloudFileClient(baseAddressUri, TestBase.StorageCredentials);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;
            return client;
        }

        public static CloudQueueClient GenerateCloudQueueClient()
        {
            CloudQueueClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.QueueServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.QueueServiceEndpoint);
                client = new CloudQueueClient(baseAddressUri, TestBase.StorageCredentials);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(TestBase.TargetTenantConfig.QueueServiceEndpoint),
                    new Uri(TestBase.TargetTenantConfig.QueueServiceSecondaryEndpoint));
                client = new CloudQueueClient(baseAddressUri, TestBase.StorageCredentials);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;

#if WINDOWS_DESKTOP
            client.BufferManager = QueueBufferManager;
#endif

            return client;
        }

        public static TenantConfiguration TargetTenantConfig { get; private set; }

        public static TenantConfiguration PremiumBlobTenantConfig { get; private set; }

        public static TenantType CurrentTenantType { get; private set; }

        public static StorageCredentials StorageCredentials { get; private set; }

        public static StorageCredentials PremiumBlobStorageCredentials { get; private set; }

        private static void Initialize(TestConfigurations configurations)
        {
            TestBase.TargetTenantConfig = configurations.TenantConfigurations.Single(config => config.TenantName == configurations.TargetTenantName);
            TestBase.StorageCredentials = new StorageCredentials(TestBase.TargetTenantConfig.AccountName, TestBase.TargetTenantConfig.AccountKey);
            TestBase.CurrentTenantType = TargetTenantConfig.TenantType;

            try
            {
                TestBase.PremiumBlobTenantConfig = configurations.TenantConfigurations.Single(config => config.TenantName == configurations.TargetPremiumBlobTenantName);
                TestBase.PremiumBlobStorageCredentials = new StorageCredentials(TestBase.PremiumBlobTenantConfig.AccountName, TestBase.PremiumBlobTenantConfig.AccountKey);
            }
            catch (InvalidOperationException) { }

#if WINDOWS_DESKTOP
            System.Threading.ThreadPool.SetMinThreads(100, 100);
#endif
        }
    }
}
