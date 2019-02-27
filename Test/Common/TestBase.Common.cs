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

using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Linq;
using System.ServiceModel.Channels;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if WINDOWS_DESKTOP || NETCOREAPP2_0
using Microsoft.IdentityModel.Clients.ActiveDirectory;
#endif

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
namespace Microsoft.WindowsAzure.Storage
{
    public partial class TestBase
    {
        public partial class TestConstants
        {
            public const int KB = 1024;
            public const int MB = KB * 1024;
            public const long GB = MB * 1024;
            public const long TB = GB * 1024L;
        }

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

#if WINDOWS_DESKTOP
        public static string GenerateOAuthToken()
        {
            string authority = string.Format("https://login.microsoftonline.com/{0}/oauth2/token",
                TestBase.TargetTenantConfig.ActiveDirectoryTenantId);

            ClientCredential credential = new ClientCredential(TestBase.TargetTenantConfig.ActiveDirectoryApplicationId,
                TestBase.TargetTenantConfig.ActiveDirectoryApplicationSecret);

            AuthenticationContext context = new AuthenticationContext(authority);
            AuthenticationResult result = context.AcquireTokenAsync("https://storage.azure.com", credential).Result;

            return result.AccessToken;
        }
#endif

        public static CloudBlobClient GenerateCloudBlobClient(DelegatingHandler delegatingHandler = null)
        {
            CloudBlobClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.BlobServiceEndpoint);
                client = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials, delegatingHandler);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(TestBase.TargetTenantConfig.BlobServiceEndpoint),
                    new Uri(TestBase.TargetTenantConfig.BlobServiceSecondaryEndpoint));
                client = new CloudBlobClient(baseAddressUri, TestBase.StorageCredentials, delegatingHandler);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;

#if WINDOWS_DESKTOP
            client.BufferManager = TableBufferManager;
#endif

            return client;
        }

        public static CloudFileClient GenerateCloudFileClient(DelegatingHandler delegatingHandler = null)
        {
            CloudFileClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.FileServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint);
                client = new CloudFileClient(baseAddressUri, TestBase.StorageCredentials, delegatingHandler);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(TestBase.TargetTenantConfig.FileServiceEndpoint),
                    new Uri(TestBase.TargetTenantConfig.FileServiceSecondaryEndpoint));
                client = new CloudFileClient(baseAddressUri, TestBase.StorageCredentials, delegatingHandler);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;
            return client;
        }

        public static CloudQueueClient GenerateCloudQueueClient(DelegatingHandler delegatingHandler = null)
        {
            CloudQueueClient client;
            if (string.IsNullOrEmpty(TestBase.TargetTenantConfig.QueueServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.QueueServiceEndpoint);
                client = new CloudQueueClient(baseAddressUri, TestBase.StorageCredentials, delegatingHandler);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(TestBase.TargetTenantConfig.QueueServiceEndpoint),
                    new Uri(TestBase.TargetTenantConfig.QueueServiceSecondaryEndpoint));
                client = new CloudQueueClient(baseAddressUri, TestBase.StorageCredentials, delegatingHandler);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;

#if WINDOWS_DESKTOP
            client.BufferManager = QueueBufferManager;
#endif

            return client;
        }

        public class DelegatingHandlerImpl : DelegatingHandler
        {
            public int CallCount { get; private set; }

            private readonly IWebProxy Proxy;

            private bool FirstCall = true;

            public DelegatingHandlerImpl() : base()
            {

            }

            public DelegatingHandlerImpl(HttpMessageHandler httpMessageHandler) : base(httpMessageHandler)
            {

            }

            public DelegatingHandlerImpl(IWebProxy proxy)
            {
                this.Proxy = proxy;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                if(FirstCall && this.Proxy != null)
                {
                    HttpClientHandler inner = (HttpClientHandler)this.InnerHandler;
                    inner.Proxy = this.Proxy;
                }
                FirstCall = false;
                return base.SendAsync(request, cancellationToken);
            }
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
