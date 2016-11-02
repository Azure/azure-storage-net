// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileShareTest.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

#if NETCORE
using System.Globalization;
#else
using Windows.Globalization;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class CloudFileShareTest : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public CloudFileShareTest()
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
        [Description("Validate share references")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareReference()
        {
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare share = client.GetShareReference("share");
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
            CloudFileDirectory directory = rootDirectory.GetDirectoryReference("directory4");
            CloudFile file = directory.GetFileReference("file2");

            Assert.AreEqual(share, file.Share);
            Assert.AreEqual(share, rootDirectory.Share);
            Assert.AreEqual(share, directory.Share);
            Assert.AreEqual(share, directory.Parent.Share);
            Assert.AreEqual(share, file.Parent.Share);
        }

        [TestMethod]
        [Description("Create and delete a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                OperationContext operationContext = new OperationContext();
                Assert.ThrowsException<AggregateException>(
                    () => share.CreateAsync(null, operationContext).Wait(),
                    "Creating already exists share should fail");
                Assert.AreEqual((int)HttpStatusCode.Conflict, operationContext.LastResult.HttpStatusCode);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to create a share after it is created")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateIfNotExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                Assert.IsTrue(await share.CreateIfNotExistsAsync());
                Assert.IsFalse(await share.CreateIfNotExistsAsync());
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareDeleteIfExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            Assert.IsFalse(await share.DeleteIfExistsAsync());
            await share.CreateAsync();
            Assert.IsTrue(await share.DeleteIfExistsAsync());
            Assert.IsFalse(await share.DeleteIfExistsAsync());
        }

        [TestMethod]
        [Description("Check a share's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);

            try
            {
                Assert.IsFalse(await share2.ExistsAsync());

                await share.CreateAsync();

                Assert.IsTrue(await share2.ExistsAsync());
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }

            Assert.IsFalse(await share2.ExistsAsync());
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCreateWithMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("key1", "value1");
                await share.CreateAsync();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                await share2.FetchAttributesAsync();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                Assert.IsTrue(share2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareSetMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                await share2.FetchAttributesAsync();
                Assert.AreEqual(0, share2.Metadata.Count);

                share.Metadata.Add("key1", "value1");
                await share.SetMetadataAsync();

                await share2.FetchAttributesAsync();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                ShareResultSegment results = await share.ServiceClient.ListSharesSegmentedAsync(share.Name, ShareListingDetails.Metadata, null, null, null, null);
                CloudFileShare share3 = results.Results.First();
                Assert.AreEqual(1, share3.Metadata.Count);
                Assert.AreEqual("value1", share3.Metadata["key1"]);

                share.Metadata.Clear();
                await share.SetMetadataAsync();

                await share2.FetchAttributesAsync();
                Assert.AreEqual(0, share2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareRegionalSetMetadataAsync()
        {
#if NETCORE
            //CultureInfo currentCulture = CultureInfo.CurrentCulture;
            //CultureInfo.CurrentCulture = new CultureInfo("sk-SK");
#else
            string currentPrimaryLanguage = ApplicationLanguages.PrimaryLanguageOverride;
            ApplicationLanguages.PrimaryLanguageOverride = "sk-SK";
#endif

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("sequence", "value");
                share.Metadata.Add("schema", "value");
                await share.CreateAsync();
            }
            finally
            {
#if NETCORE
                //CultureInfo.CurrentCulture = currentCulture;
#else
                ApplicationLanguages.PrimaryLanguageOverride = currentPrimaryLanguage;
#endif
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareListFilesAndDirectoriesAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                List<string> fileNames = await CreateFilesAsync(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                IEnumerable<IListFileItem> results = await ListFilesAndDirectoriesAsync(rootDirectory, null, null, null);
                Assert.AreEqual(fileNames.Count, results.Count());
                foreach (IListFileItem fileItem in results)
                {
                    Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                    Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareListFilesAndDirectoriesSegmentedAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                List<string> fileNames = await CreateFilesAsync(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = await rootDirectory.ListFilesAndDirectoriesSegmentedAsync(1, token, null, null);
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    Assert.AreEqual(1, count);
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareSetPermissionsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.SharedAccessPolicies.Add("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });
                await share.SetPermissionsAsync(permissions);
                await Task.Delay(30 * 1000);

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                permissions = await share2.GetPermissionsAsync();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareSetPermissionsOverloadAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                await share.SetPermissionsAsync(permissions);
                await Task.Delay(30 * 1000);

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                permissions = await share2.GetPermissionsAsync();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Clear share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareClearPermissionsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                await share.SetPermissionsAsync(permissions);
                await Task.Delay(3 * 1000);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);

                Assert.AreEqual(true, permissions.SharedAccessPolicies.Contains(sharedAccessPolicy));
                Assert.AreEqual(true, permissions.SharedAccessPolicies.ContainsKey("key1"));
                permissions.SharedAccessPolicies.Clear();
                await share.SetPermissionsAsync(permissions);
                await Task.Delay(3 * 1000);
                permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Copy share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareCopyPermissionsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry2 = start.AddMinutes(30);
                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessFilePolicy>("key2", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessFilePermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);

                KeyValuePair<String, SharedAccessFilePolicy>[] sharedAccessPolicyArray = new KeyValuePair<string, SharedAccessFilePolicy>[2];
                permissions.SharedAccessPolicies.CopyTo(sharedAccessPolicyArray, 0);
                Assert.AreEqual(2, sharedAccessPolicyArray.Length);
                Assert.AreEqual(sharedAccessPolicy, sharedAccessPolicyArray[0]);
                Assert.AreEqual(sharedAccessPolicy2, sharedAccessPolicyArray[1]);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Remove share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareRemovePermissionsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessFilePolicy>("key2", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessFilePermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                await share.SetPermissionsAsync(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove(sharedAccessPolicy2);
                await share.SetPermissionsAsync(permissions);
                await Task.Delay(3 * 1000);

                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                await share.SetPermissionsAsync(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove("key2");
                await share.SetPermissionsAsync(permissions);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("TryGetValue for share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareTryGetValuePermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessFilePolicy>("key2", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessFilePermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                await share.SetPermissionsAsync(permissions);
                await Task.Delay(3 * 1000);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions = await share.GetPermissionsAsync();
                SharedAccessFilePolicy retrPolicy;
                permissions.SharedAccessPolicies.TryGetValue("key1", out retrPolicy);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, retrPolicy.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, retrPolicy.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, retrPolicy.SharedAccessExpiryTime);

                SharedAccessFilePolicy retrPolicy2;
                permissions.SharedAccessPolicies.TryGetValue("key2", out retrPolicy2);
                Assert.AreEqual(sharedAccessPolicy2.Value.Permissions, retrPolicy2.Permissions);
                Assert.AreEqual(sharedAccessPolicy2.Value.SharedAccessStartTime, retrPolicy2.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy2.Value.SharedAccessExpiryTime, retrPolicy2.SharedAccessExpiryTime);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("GetEnumerator for share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareGetEnumeratorPermissionsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessFilePolicy>("key2", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessFilePermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                IEnumerator<KeyValuePair<string, SharedAccessFilePolicy>> policies = permissions.SharedAccessPolicies.GetEnumerator();
                policies.MoveNext();
                Assert.AreEqual(sharedAccessPolicy, policies.Current);
                policies.MoveNext();
                Assert.AreEqual(sharedAccessPolicy2, policies.Current);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("GetValues for share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareGetValuesPermissionsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                FileSharePermissions permissions = await share.GetPermissionsAsync();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessFilePolicy>("key1", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessFilePermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessFilePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessFilePolicy>("key2", new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessFilePermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                ICollection<SharedAccessFilePolicy> values = permissions.SharedAccessPolicies.Values;
                Assert.AreEqual(2, values.Count);
                Assert.AreEqual(sharedAccessPolicy.Value, values.ElementAt(0));
                Assert.AreEqual(sharedAccessPolicy2.Value, values.ElementAt(1));
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Get permissions from string")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSharePermissionsFromStringAsync()
        {
            SharedAccessFilePolicy policy = new SharedAccessFilePolicy();
            policy.SharedAccessStartTime = DateTime.UtcNow;
            policy.SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(30);

            policy.Permissions = SharedAccessFilePolicy.PermissionsFromString("rwdl");
            Assert.AreEqual(SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write | SharedAccessFilePermissions.Delete | SharedAccessFilePermissions.List, policy.Permissions);

            policy.Permissions = SharedAccessFilePolicy.PermissionsFromString("rwl");
            Assert.AreEqual(SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write | SharedAccessFilePermissions.List, policy.Permissions);

            policy.Permissions = SharedAccessFilePolicy.PermissionsFromString("rw");
            Assert.AreEqual(SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write, policy.Permissions);

            policy.Permissions = SharedAccessFilePolicy.PermissionsFromString("rd");
            Assert.AreEqual(SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Delete, policy.Permissions);

            policy.Permissions = SharedAccessFilePolicy.PermissionsFromString("wl");
            Assert.AreEqual(SharedAccessFilePermissions.Write | SharedAccessFilePermissions.List, policy.Permissions);

            policy.Permissions = SharedAccessFilePolicy.PermissionsFromString("w");
            Assert.AreEqual(SharedAccessFilePermissions.Write, policy.Permissions);
        }

        /*
        [TestMethod]
        [Description("Test conditional access on a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileShareConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                await share.FetchAttributesAsync();

                string currentETag = share.Properties.ETag;
                DateTimeOffset currentModifiedTime = share.Properties.LastModified.Value;

                // ETag conditional tests
                share.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await share.SetMetadataAsync();

                await share.FetchAttributesAsync();
                string newETag = share.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                // LastModifiedTime tests
                currentModifiedTime = share.Properties.LastModified.Value;

                share.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                share.Metadata["DateConditionalName"] = "DateConditionalValue2";
                currentETag = share.Properties.ETag;

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await share.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                await share.FetchAttributesAsync();
                newETag = share.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
        */
    }
}
