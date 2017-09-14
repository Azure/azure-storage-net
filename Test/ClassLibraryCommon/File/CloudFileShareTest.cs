﻿// -----------------------------------------------------------------------------------------
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class CloudFileShareTest : FileTestBase
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
        [Description("Test share name validation.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudShareNameValidation()
        {
            NameValidator.ValidateShareName("alpha");
            NameValidator.ValidateShareName("4lphanum3r1c");
            NameValidator.ValidateShareName("middle-dash");

            TestInvalidShareHelper(null, "Null not allowed.", "Invalid share name. The share name may not be null, empty, or whitespace only.");
            TestInvalidShareHelper("$root", "Alphanumeric or dashes only.", "Invalid share name. Check MSDN for more information about valid share naming.");
            TestInvalidShareHelper("double--dash", "No double dash.", "Invalid share name. Check MSDN for more information about valid share naming.");
            TestInvalidShareHelper("CapsLock", "Lowercase only.", "Invalid share name. Check MSDN for more information about valid share naming.");
            TestInvalidShareHelper("illegal$char", "Alphanumeric or dashes only.", "Invalid share name. Check MSDN for more information about valid share naming.");
            TestInvalidShareHelper("illegal!char", "Alphanumeric or dashes only.", "Invalid share name. Check MSDN for more information about valid share naming.");
            TestInvalidShareHelper("white space", "Alphanumeric or dashes only.", "Invalid share name. Check MSDN for more information about valid share naming.");
            TestInvalidShareHelper("2c", "Between 3 and 63 characters.", "Invalid share name length. The share name must be between 3 and 63 characters long.");
            TestInvalidShareHelper(new string('n', 64), "Between 3 and 63 characters.", "Invalid share name length. The share name must be between 3 and 63 characters long.");
        }

        private void TestInvalidShareHelper(string shareName, string failMessage, string exceptionMessage)
        {
            try
            {
                NameValidator.ValidateShareName(shareName);
                Assert.Fail(failMessage);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual(exceptionMessage, e.Message);
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
        public void CloudFileShareCreate()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            TestHelper.ExpectedException(
                () => share.Create(),
                "Creating already exists share should fail",
                HttpStatusCode.Conflict);
            share.Delete();
        }

        [TestMethod]
        [Description("Create and delete a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                share.EndCreate(result);
                result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException(
                    () => share.EndCreate(result),
                    "Creating already exists share should fail",
                    HttpStatusCode.Conflict);
            }
            share.Delete();
        }

#if TASK
        [TestMethod]
        [Description("Create and delete a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();

            AggregateException e = TestHelper.ExpectedException<AggregateException>(
                 share.CreateAsync().Wait,
                "Creating already exists share should fail");
            Assert.IsInstanceOfType(e.InnerException, typeof(StorageException));
            Assert.AreEqual((int)HttpStatusCode.Conflict, ((StorageException)e.InnerException).RequestInformation.HttpStatusCode);
            Task.Factory.FromAsync(share.BeginDelete, share.EndDelete, null).Wait();
        }

        [TestMethod]
        [Description("Create and delete a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateTaskWithCancellation()
        {
            CloudFileShare share = GetRandomShareReference();
            CancellationTokenSource cts = new CancellationTokenSource();
            OperationContext ctx = new OperationContext();

            Task createTask = share.CreateAsync(null, ctx, cts.Token);
            try
            {
                Thread.Sleep(0);
                cts.Cancel();
                createTask.Wait();

                // Should throw aggregate exception
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(AggregateException));
                Assert.IsNotNull(ex.InnerException);
                Assert.IsInstanceOfType(ex.InnerException, typeof(OperationCanceledException));
            }

            // Validate that we did attempt one request and it was cancelled
            TestHelper.AssertCancellation(ctx);
        }
#endif

        [TestMethod]
        [Description("Try to create a share after it is created")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateIfNotExists()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                Assert.IsTrue(share.CreateIfNotExists());
                Assert.IsFalse(share.CreateIfNotExists());
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to create a share after it is created")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateIfNotExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileShare share2 = GetRandomShareReference();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = share.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(share.EndCreateIfNotExists(result));
                    result = share.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(share.EndCreateIfNotExists(result));

                    // Test the case where the callback is null.
                    // There is a race condition (inherent in the APM pattern) about what will happen if an exception is thrown in the callback
                    // This is why we need the sleep - to ensure that if our code nullref's in the null-callback case, the exception has time 
                    // to get processed before the End call.
                    OperationContext context = new OperationContext();
                    context.RequestCompleted += (sender, e) => waitHandle.Set();
                    result = share2.BeginCreateIfNotExists(null, context, null, null);
                    waitHandle.WaitOne();
                    Thread.Sleep(2000);
                    Assert.IsTrue(share2.EndCreateIfNotExists(result));
                    context = new OperationContext();
                    context.RequestCompleted += (sender, e) => waitHandle.Set();
                    result = share2.BeginCreateIfNotExists(null, context, null, null);
                    waitHandle.WaitOne();
                    Thread.Sleep(2000);
                    Assert.IsFalse(share2.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                share.DeleteIfExists();
                share2.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to create a share after it is created")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateIfNotExistsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                Assert.IsTrue(share.CreateIfNotExistsAsync().Result);
                Assert.IsFalse(share.CreateIfNotExistsAsync().Result);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
#endif

        [TestMethod]
        [Description("Try to delete a non-existing share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteIfExists()
        {
            CloudFileShare share = GetRandomShareReference();
            Assert.IsFalse(share.DeleteIfExists());
            share.Create();
            Assert.IsTrue(share.DeleteIfExists());
            Assert.IsFalse(share.DeleteIfExists());
        }

        [TestMethod]
        [Description("Try to delete a non-existing share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteIfExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share.BeginDeleteIfExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(share.EndDeleteIfExists(result));
                result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                share.EndCreate(result);
                result = share.BeginDeleteIfExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsTrue(share.EndDeleteIfExists(result));
                result = share.BeginDeleteIfExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(share.EndDeleteIfExists(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to delete a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            Assert.IsTrue(share.Exists());
            share.DeleteAsync().Wait();
            Assert.IsFalse(share.Exists());
        }

        [TestMethod]
        [Description("Try to delete a non-existing share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteIfExistsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            Assert.IsFalse(share.DeleteIfExistsAsync().Result);
            share.CreateAsync().Wait();
            Assert.IsTrue(share.DeleteIfExistsAsync().Result);
            Assert.IsFalse(share.DeleteIfExistsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Check a share's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareExists()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);

            Assert.IsFalse(share2.Exists());

            share.Create();

            try
            {
                Assert.IsTrue(share2.Exists());
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.Delete();
            }

            Assert.IsFalse(share2.Exists());
        }

        [TestMethod]
        [Description("Check a share's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share2.BeginExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(share2.EndExists(result));

                share.Create();

                try
                {
                    result = share2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(share2.EndExists(result));
                    Assert.IsNotNull(share2.Properties.ETag);
                }
                finally
                {
                    share.Delete();
                }

                result = share2.BeginExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(share2.EndExists(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Check a share's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareExistsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);

            Assert.IsFalse(share2.ExistsAsync().Result);

            share.CreateAsync().Wait();

            try
            {
                Assert.IsTrue(share2.ExistsAsync().Result);
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }

            Assert.IsFalse(share2.ExistsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateWithMetadata()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("key1", "value1");
                share.Create();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                share2.FetchAttributes();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                Assert.IsTrue(share2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(share2.Properties.ETag);

                CloudFileShare share3 = share.ServiceClient.GetShareReference(share.Name);
                share3.Exists();
                Assert.AreEqual(1, share3.Metadata.Count);
                Assert.AreEqual("value1", share3.Metadata["key1"]);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateWithMetadataTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("key1", "value1");
                share.CreateAsync().Wait();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                share2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                Assert.IsTrue(share2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(share2.Properties.ETag);
            }
            finally
            {
                share.DeleteIfExistsAsync();
            }
        }
#endif

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetMetadata()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                share2.FetchAttributes();
                Assert.AreEqual(0, share2.Metadata.Count);

                share.Metadata.Add("key1", "value1");
                share.SetMetadata();

                share2.FetchAttributes();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                CloudFileShare share3 = share.ServiceClient.ListShares(share.Name, ShareListingDetails.Metadata).First();
                Assert.AreEqual(1, share3.Metadata.Count);
                Assert.AreEqual("value1", share3.Metadata["key1"]);

                share.Metadata.Clear();
                share.SetMetadata();

                share2.FetchAttributes();
                Assert.AreEqual(0, share2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareRegionalSetMetadata()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sk-SK");

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Metadata.Add("sequence", "value");
                share.Metadata.Add("schema", "value");
                share.Create();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetMetadataAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                    IAsyncResult result = share2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share2.EndFetchAttributes(result);
                    Assert.AreEqual(0, share2.Metadata.Count);

                    share.Metadata.Add("key1", "value1");
                    result = share.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share.EndSetMetadata(result);

                    result = share2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share2.EndFetchAttributes(result);
                    Assert.AreEqual(1, share2.Metadata.Count);
                    Assert.AreEqual("value1", share2.Metadata["key1"]);

                    result = share.ServiceClient.BeginListSharesSegmented(share.Name, ShareListingDetails.Metadata, null, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    CloudFileShare share3 = share.ServiceClient.EndListSharesSegmented(result).Results.First();
                    Assert.AreEqual(1, share3.Metadata.Count);
                    Assert.AreEqual("value1", share3.Metadata["key1"]);

                    share.Metadata.Clear();
                    result = share.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share.EndSetMetadata(result);

                    result = share2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share2.EndFetchAttributes(result);
                    Assert.AreEqual(0, share2.Metadata.Count);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a share with metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetMetadataTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                share2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, share2.Metadata.Count);

                share.Metadata.Add("key1", "value1");
                share.SetMetadataAsync().Wait();

                share2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, share2.Metadata.Count);
                Assert.AreEqual("value1", share2.Metadata["key1"]);

                CloudFileShare share3 =
                    share.ServiceClient.ListSharesSegmentedAsync(
                        share.Name, ShareListingDetails.Metadata, null, null, null, null).Result.Results.First();
                Assert.AreEqual(1, share3.Metadata.Count);
                Assert.AreEqual("value1", share3.Metadata["key1"]);

                share.Metadata.Clear();
                share.SetMetadataAsync().Wait();

                share2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, share2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.SetPermissions(permissions);

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                    permissions = share2.GetPermissions();
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPermissionsOverload()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.SetPermissions(permissions);

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                    permissions = share2.GetPermissions();
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPermissionsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = share.BeginSetPermissions(permissions, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSetPermissions(result);

                    TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                    {
                        CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                        result = share.BeginGetPermissions(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        permissions = share.EndGetPermissions(result);
                        Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                        Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                        Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                        Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                    });
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPermissionsAPMOverload()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = share.BeginSetPermissions(permissions, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSetPermissions(result);

                    TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                    {
                        CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                        result = share.BeginGetPermissions(null, null, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        permissions = share.EndGetPermissions(result);
                        Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                        Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                        Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                        Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                    });
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPermissionsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissionsAsync().Result;
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
                share.SetPermissionsAsync(permissions).Wait();

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                    permissions = share2.GetPermissionsAsync().Result;
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Set share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPermissionsOverloadTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                FileSharePermissions permissions = share.GetPermissionsAsync().Result;
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
                share.SetPermissionsAsync(permissions).Wait();

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudFileShare share2 = share.ServiceClient.GetShareReference(share.Name);
                    permissions = share2.GetPermissionsAsync().Result;
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessFilePermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
#endif

        [TestMethod]
        [Description("Clear share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareClearPermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);

                Assert.AreEqual(true, permissions.SharedAccessPolicies.Contains(sharedAccessPolicy));
                Assert.AreEqual(true, permissions.SharedAccessPolicies.ContainsKey("key1"));
                permissions.SharedAccessPolicies.Clear();
                share.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                permissions = share.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Copy share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCopyPermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Remove share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareRemovePermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.SetPermissions(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove(sharedAccessPolicy2);
                share.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);

                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = share.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                share.SetPermissions(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove("key2");
                share.SetPermissions(permissions);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = share.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("TryGetValue for share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareTryGetValuePermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions = share.GetPermissions();
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
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetEnumerator for share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareGetEnumeratorPermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetValues for share permissions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareGetValuesPermissions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                FileSharePermissions permissions = share.GetPermissions();
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
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Get permissions from string")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSharePermissionsFromString()
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

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectories()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                IEnumerable<IListFileItem> results = rootDirectory.ListFilesAndDirectories();
                Assert.AreEqual(fileNames.Count, results.Count());
                foreach (IListFileItem fileItem in results)
                {
                    Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                    Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List many files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        public void CloudFileShareListManyFiles()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFilesTask(share, 6000);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                int count = 0;
                IEnumerable<IListFileItem> results = rootDirectory.ListFilesAndDirectories();
                foreach (IListFileItem fileItem in results)
                {
                    count++;
                    Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                    CloudFile file = (CloudFile)fileItem;
                    if (fileNames.Remove(file.Name))
                    {
                        Assert.IsInstanceOfType(file, typeof(CloudFile));
                    }
                    else
                    {
                        Assert.Fail("Unexpected file: " + file.Uri.AbsoluteUri);
                    }
                }

                Assert.AreEqual(6000, count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectoriesSegmented()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = rootDirectory.ListFilesAndDirectoriesSegmented(1, token, null, null);
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    Assert.IsTrue(count <= 1);
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectoriesSegmentedAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    FileContinuationToken token = null;
                    do
                    {
                        IAsyncResult result = rootDirectory.BeginListFilesAndDirectoriesSegmented(1, token, null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        FileResultSegment results = rootDirectory.EndListFilesAndDirectoriesSegmented(result);
                        int count = 0;
                        foreach (IListFileItem fileItem in results.Results)
                        {
                            Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                            Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                            count++;
                        }
                        Assert.IsTrue(count <= 1);
                        token = results.ContinuationToken;
                    }
                    while (token != null);
                    Assert.AreEqual(0, fileNames.Count);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectoriesSegmentedTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();
                List<string> fileNames = CreateFilesTask(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = rootDirectory.ListFilesAndDirectoriesSegmentedAsync(1, token, null, null).Result;
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    Assert.IsTrue(count <= 1);
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
#endif

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectoriesSegmentedOverload()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = rootDirectory.ListFilesAndDirectoriesSegmented(token);
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectoriesSegmentedAPMOverload()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    FileContinuationToken token = null;
                    do
                    {
                        IAsyncResult result = rootDirectory.BeginListFilesAndDirectoriesSegmented(token,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        FileResultSegment results = rootDirectory.EndListFilesAndDirectoriesSegmented(result);
                        int count = 0;
                        foreach (IListFileItem fileItem in results.Results)
                        {
                            Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                            Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                            count++;
                        }
                        token = results.ContinuationToken;
                    }
                    while (token != null);
                    Assert.AreEqual(0, fileNames.Count);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("List files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareListFilesAndDirectoriesSegmentedOverloadTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = rootDirectory.ListFilesAndDirectoriesSegmentedAsync(token).Result;
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync();
            }
        }
#endif

        [TestMethod]
        [Description("Verify WriteXml/ReadXml Serialize/Deserialize")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileContinuationTokenVerifySerializer()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(FileContinuationToken));

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            StringReader reader;
            string tokenxml;

            FileContinuationToken writeToken = new FileContinuationToken
            {
                NextMarker = Guid.NewGuid().ToString(),
                TargetLocation = StorageLocation.Primary
            };

            FileContinuationToken readToken = null;

            // Write with XmlSerializer
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, writeToken);
                tokenxml = writer.ToString();
            }

            // Read with XmlSerializer
            reader = new StringReader(tokenxml);
            readToken = (FileContinuationToken)serializer.Deserialize(reader);
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XmlReader.Create(new StringReader(tokenxml)))
            {
                readToken = new FileContinuationToken();
                readToken.ReadXml(xmlReader);
            }
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);

            // Write with token.WriteXml
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writeToken.WriteXml(writer);
            }

            // Read with XmlSerializer
            reader = new StringReader(sb.ToString());
            readToken = (FileContinuationToken)serializer.Deserialize(reader);
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XmlReader.Create(new StringReader(sb.ToString())))
            {
                readToken = new FileContinuationToken();
                readToken.ReadXml(xmlReader);
            }
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);
        }

        [TestMethod]
        [Description("Verify ReadXml Deserialization on FileContinuationToken with empty TargetLocation")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileContinuationTokenVerifyEmptyTargetDeserializer()
        {
            FileContinuationToken fileContinuationToken = new FileContinuationToken { TargetLocation = null };
            StringBuilder stringBuilder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(stringBuilder))
            {
                fileContinuationToken.WriteXml(writer);
            }

            string stringToken = stringBuilder.ToString();
            FileContinuationToken parsedToken = new FileContinuationToken();
            parsedToken.ReadXml(XmlReader.Create(new System.IO.StringReader(stringToken)));
            Assert.AreEqual(parsedToken.TargetLocation, null);
        }

        [TestMethod]
        [Description("Verify GetSchema, WriteXml and ReadXml on FileContinuationToken")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileContinuationTokenVerifyXmlFunctions()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = rootDirectory.ListFilesAndDirectoriesSegmented(1, token, null, null);
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    Assert.IsTrue(count <= 1);
                    token = results.ContinuationToken;

                    if (token != null)
                    {
                        Assert.AreEqual(null, token.GetSchema());

                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        StringBuilder sb = new StringBuilder();
                        using (XmlWriter writer = XmlWriter.Create(sb, settings))
                        {
                            token.WriteXml(writer);
                        }

                        using (XmlReader reader = XmlReader.Create(new StringReader(sb.ToString())))
                        {
                            token = new FileContinuationToken();
                            token.ReadXml(reader);
                        }
                    }
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify GetSchema, WriteXml and ReadXml on FileContinuationToken within another Xml")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void FileContinuationTokenVerifyXmlWithinXml()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                List<string> fileNames = CreateFiles(share, 3);
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                FileContinuationToken token = null;
                do
                {
                    FileResultSegment results = rootDirectory.ListFilesAndDirectoriesSegmented(1, token, null, null);
                    int count = 0;
                    foreach (IListFileItem fileItem in results.Results)
                    {
                        Assert.IsInstanceOfType(fileItem, typeof(CloudFile));
                        Assert.IsTrue(fileNames.Remove(((CloudFile)fileItem).Name));
                        count++;
                    }
                    Assert.IsTrue(count <= 1);
                    token = results.ContinuationToken;

                    if (token != null)
                    {
                        Assert.AreEqual(null, token.GetSchema());

                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        StringBuilder sb = new StringBuilder();
                        using (XmlWriter writer = XmlWriter.Create(sb, settings))
                        {
                            writer.WriteStartElement("test1");
                            writer.WriteStartElement("test2");
                            token.WriteXml(writer);
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                        }

                        using (XmlReader reader = XmlReader.Create(new StringReader(sb.ToString())))
                        {
                            token = new FileContinuationToken();
                            reader.ReadStartElement();
                            reader.ReadStartElement();
                            token.ReadXml(reader);
                            reader.ReadEndElement();
                            reader.ReadEndElement();
                        }
                    }
                }
                while (token != null);
                Assert.AreEqual(0, fileNames.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Get share stats")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareGetShareStats()
        {
            int megabyteInBytes = 1024 * 1024;
            CloudFileShare share = GetRandomShareReference();

            try
            {
                share.Create();
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory.Create();

                // should begin empty
                ShareStats stats1 = share.GetStats();
                Assert.AreEqual(0, stats1.Usage);

                // should round up, upload 1 MB. 
                CloudFile file = directory.GetFileReference("file1");
                file.UploadFromByteArray(GetRandomBuffer(megabyteInBytes), 0, megabyteInBytes); //one mb

                ShareStats stats2 = share.GetStats();
                Assert.AreEqual(1, stats2.Usage);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Get share stats")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareGetShareStatsAPM()
        {
            int megabyteInBytes = 1024 * 1024;
            CloudFileShare share = GetRandomShareReference();

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                try
                {
                    IAsyncResult result = share.BeginCreate(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share.EndCreate(result);

                    // should begin empty
                    result = share.BeginGetStats(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();

                    ShareStats stats1 = share.EndGetStats(result);
                    Assert.AreEqual(0, stats1.Usage);

                    // should round up, upload 1 MB and assert the usage is 1 GB. 
                    CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                    CloudFile file = directory.GetFileReference("file1");
                    result = directory.BeginCreate(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndCreate(result);
                    result = file.BeginUploadFromByteArray(
                        GetRandomBuffer(megabyteInBytes),
                        0,
                        megabyteInBytes,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndUploadFromByteArray(result);

                    result = share.BeginGetStats(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();

                    ShareStats stats2 = share.EndGetStats(result);
                    Assert.AreEqual(1, stats2.Usage);

                }
                finally
                {
                    IAsyncResult result = share.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share.EndDeleteIfExists(result);
                }
            }
        }

#if TASK
        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareGetShareStatsTask()
        {
            int megabyteInBytes = 1024 * 1024;
            CloudFileShare share = GetRandomShareReference();

            try
            {
                share.CreateAsync().Wait();

                // should begin empty
                Task<ShareStats> statsTask1 = share.GetStatsAsync();
                statsTask1.Wait();
                ShareStats stats1 = statsTask1.Result;
                Assert.AreEqual(0, stats1.Usage);

                // should round up, upload 1 MB and assert the usage is 1 GB. 
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                CloudFile file = directory.GetFileReference("file1");
                directory.CreateAsync().Wait();
                file.UploadFromByteArrayAsync(GetRandomBuffer(megabyteInBytes), 0, megabyteInBytes).Wait(); //one mb

                Task<ShareStats> statsTask2 = share.GetStatsAsync();
                statsTask2.Wait();
                ShareStats stats2 = statsTask2.Result;
                Assert.AreEqual(1, stats2.Usage);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Verify setting the properties of a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetProperties()
        {
            string shareName = GetRandomShareName();
            CloudFileClient client = GenerateCloudFileClient();

            try
            {
                CloudFileShare share1 = client.GetShareReference(shareName);
                share1.Create();

                share1.FetchAttributes();
                Assert.AreEqual(5120, share1.Properties.Quota);

                share1.Properties.Quota = 8;
                share1.SetProperties();

                CloudFileShare share2 = client.GetShareReference(shareName);
                share2.FetchAttributes();
                Assert.AreEqual(8, share2.Properties.Quota);
            }
            finally
            {
                client.GetShareReference(shareName).DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPropertiesAPM()
        {
            string shareName = GetRandomShareName();
            CloudFileClient client = GenerateCloudFileClient();

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                try
                {
                    CloudFileShare share1 = client.GetShareReference(shareName);
                    IAsyncResult result = share1.BeginCreate(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share1.EndCreate(result);

                    result = share1.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share1.EndFetchAttributes(result);

                    Assert.AreEqual(5120, share1.Properties.Quota);

                    share1.Properties.Quota = 8;
                    result = share1.BeginSetProperties(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share1.EndSetProperties(result);

                    CloudFileShare share2 = client.GetShareReference(shareName);
                    result = share2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    share2.EndFetchAttributes(result);

                    Assert.AreEqual(8, share2.Properties.Quota);
                }
                finally
                {
                    IAsyncResult result = client.GetShareReference(shareName).BeginDeleteIfExists(
                                            ar => waitHandle.Set(),
                                            null);
                    waitHandle.WaitOne();
                    client.GetShareReference(shareName).EndDeleteIfExists(result);
                }
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify setting the properties of a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareSetPropertiesTask()
        {
            string shareName = GetRandomShareName();
            CloudFileClient client = GenerateCloudFileClient();

            try
            {
                CloudFileShare share1 = client.GetShareReference(shareName);
                share1.CreateAsync().Wait();

                share1.FetchAttributesAsync().Wait();
                Assert.AreEqual(5120, share1.Properties.Quota);

                share1.Properties.Quota = 8;
                share1.SetPropertiesAsync().Wait();

                CloudFileShare share2 = client.GetShareReference(shareName);
                share2.FetchAttributesAsync().Wait();
                Assert.AreEqual(8, share2.Properties.Quota);
            }
            finally
            {
                client.GetShareReference(shareName).DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Test to ensure CreateIfNotExists/DeleteIfNotExists succeeds with write-only Account SAS permissions - SYNC")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateAndDeleteWithWriteOnlyPermissionsSync()
        {
            CloudFileShare fileShareWithSAS = GenerateRandomWriteOnlyFileShare();
            try
            {
                Assert.IsFalse(fileShareWithSAS.DeleteIfExists());
                Assert.IsTrue(fileShareWithSAS.CreateIfNotExists());
                Assert.IsFalse(fileShareWithSAS.CreateIfNotExists());
                Assert.IsTrue(fileShareWithSAS.DeleteIfExists());
                // Design mechanic, delete takes time to propagate on the service.
                Assert.IsTrue(fileShareWithSAS.DeleteIfExists());
            }
            finally
            {
                fileShareWithSAS.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test to ensure CreateIfNotExists/DeleteIfNotExists succeeds with write-only Account SAS permissions - APM ")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateAndDeleteWithWriteOnlyPermissionsAPM()
        {
            CloudFileShare fileShareWithSAS = GenerateRandomWriteOnlyFileShare();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    result = fileShareWithSAS.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(fileShareWithSAS.EndDeleteIfExists(result));

                    result = fileShareWithSAS.BeginCreateIfNotExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(fileShareWithSAS.EndCreateIfNotExists(result));

                    result = fileShareWithSAS.BeginCreateIfNotExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(fileShareWithSAS.EndCreateIfNotExists(result));

                    result = fileShareWithSAS.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(fileShareWithSAS.EndDeleteIfExists(result));

                    result = fileShareWithSAS.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(fileShareWithSAS.EndDeleteIfExists(result));
                }
            }
            finally
            {
                fileShareWithSAS.DeleteIfExists();
            }

        }

#if TASK
        [TestMethod]
        [Description("Test to ensure CreateIfNotExists/DeleteIfNotExists succeeds with write-only Account SAS permissions - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareCreateAndDeleteWithWriteOnlyPermissionsTask()
        {
            CloudFileShare fileShareWithSAS = GenerateRandomWriteOnlyFileShare();
            try
            {
                Assert.IsFalse(fileShareWithSAS.DeleteIfExistsAsync().Result);
                Assert.IsTrue(fileShareWithSAS.CreateIfNotExistsAsync().Result);
                Assert.IsFalse(fileShareWithSAS.CreateIfNotExistsAsync().Result);
                Assert.IsTrue(fileShareWithSAS.DeleteIfExistsAsync().Result);
                Assert.IsTrue(fileShareWithSAS.DeleteIfExistsAsync().Result);
            }
            finally
            {
                fileShareWithSAS.DeleteIfExists();
            }
        }
#endif

        private CloudFileShare GenerateRandomWriteOnlyFileShare()
        {
            string fileName = "n" + Guid.NewGuid().ToString("N");

            SharedAccessAccountPolicy sasAccountPolicy = new SharedAccessAccountPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessAccountPermissions.Write | SharedAccessAccountPermissions.Delete,
                Services = SharedAccessAccountServices.File,
                ResourceTypes = SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Container
            };

            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudStorageAccount account = new CloudStorageAccount(fileClient.Credentials, false);
            string accountSASToken = account.GetSharedAccessSignature(sasAccountPolicy);
            StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
            StorageUri storageUri = fileClient.StorageUri;
            CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, null, null, fileClient.StorageUri);
            CloudFileClient fileClientWithSAS = accountWithSAS.CreateCloudFileClient();
            CloudFileShare fileShareWithSAS = fileClientWithSAS.GetShareReference(fileName);
            return fileShareWithSAS;
        }

        [TestMethod]
        [Description("Test share snapshot create")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateShareSnapshot()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            share.Metadata["key1"] = "value1";
            share.SetMetadata();

            CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("dir1");
            dir1.Create();
            CloudFile file1 = dir1.GetFileReference("file1");
            file1.Create(1024);
            byte[] buffer = GetRandomBuffer(1024);
            file1.UploadFromByteArray(buffer, 0, 1024);
            dir1.Metadata["key2"] = "value2";
            dir1.SetMetadata();

            CloudFileShare snapshot = share.Snapshot();
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare snapshotRef = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
            Assert.IsTrue(snapshotRef.Exists());
            Assert.IsTrue(snapshotRef.Metadata.Count == 1 && snapshotRef.Metadata["key1"].Equals("value1"));

            CloudFileShare snapshotRef2 = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
            snapshotRef2.FetchAttributes();
            Assert.IsTrue(snapshotRef2.Metadata.Count == 1 && snapshotRef2.Metadata["key1"].Equals("value1"));

            Assert.IsTrue(snapshot.Metadata.Count == 1 && snapshot.Metadata["key1"].Equals("value1"));

            CloudFileDirectory snapshotDir1 = snapshot.GetRootDirectoryReference().GetDirectoryReference("dir1");
            snapshotDir1.Exists();
            Assert.IsTrue(snapshotDir1.Metadata.Count == 1 && snapshotDir1.Metadata["key2"].Equals("value2"));

            CloudFileDirectory snapshotDir2 = snapshot.GetRootDirectoryReference().GetDirectoryReference("dir1");
            snapshotDir2.FetchAttributes();
            Assert.IsTrue(snapshotDir2.Metadata.Count == 1 && snapshotDir2.Metadata["key2"].Equals("value2"));

            // create snapshot with metadata
            IDictionary<string, string> shareMeta2 = new Dictionary<string, string>();
            shareMeta2.Add("abc", "def");
            CloudFileShare snapshotRef3 = share.Snapshot(shareMeta2, null, null, null);
            CloudFileShare snapshotRef4 = client.GetShareReference(snapshotRef3.Name, snapshotRef3.SnapshotTime);
            Assert.IsTrue(snapshotRef4.Exists());
            Assert.IsTrue(snapshotRef4.Metadata.Count == 1 && snapshotRef4.Metadata["abc"].Equals("def"));
        }

        [TestMethod]
        [Description("Test share snapshot create - APM")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateShareSnapshotAPM()
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

                CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("dir1");
                result = dir1.BeginCreate(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                dir1.EndCreate(result);

                CloudFile file1 = dir1.GetFileReference("file1");
                result = file1.BeginCreate(1024, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                file1.EndCreate(result);

                byte[] buffer = GetRandomBuffer(1024);
                result = file1.BeginUploadFromByteArray(buffer, 0, 1024, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                file1.EndUploadFromByteArray(result);

                dir1.Metadata["key2"] = "value2";
                result = dir1.BeginSetMetadata(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                dir1.EndSetMetadata(result);

                result = share.BeginSnapshot(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                CloudFileShare snapshot = share.EndSnapshot(result);

                CloudFileClient client = GenerateCloudFileClient();
                CloudFileShare snapshotRef = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
                result = snapshotRef.BeginExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(snapshotRef.EndExists(result));

                result = snapshotRef.BeginFetchAttributes(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                snapshotRef.EndFetchAttributes(result);

                Assert.IsTrue(snapshotRef.Metadata.Count == 1 && snapshotRef.Metadata["key1"].Equals("value1"));
                Assert.IsTrue(snapshot.Metadata.Count == 1 && snapshot.Metadata["key1"].Equals("value1"));
                CloudFileDirectory snapshotDir1 = snapshot.GetRootDirectoryReference().GetDirectoryReference("dir1");
                result = snapshotDir1.BeginFetchAttributes(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                snapshotDir1.EndFetchAttributes(result);
                Assert.IsTrue(snapshotDir1.Metadata.Count == 1 && snapshotDir1.Metadata["key2"].Equals("value2"));

                // create snapshot with metadata
                IDictionary<string, string> shareMeta2 = new Dictionary<string, string>();
                shareMeta2.Add("abc", "def");
                result = share.BeginSnapshot(shareMeta2, null, null, null, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                CloudFileShare snapshotRef3 = share.EndSnapshot(result);

                CloudFileShare snapshotRef4 = client.GetShareReference(snapshotRef3.Name, snapshotRef3.SnapshotTime);
                result = snapshotRef4.BeginExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(snapshotRef4.EndExists(result));
                Assert.IsTrue(snapshotRef4.Metadata.Count == 1 && snapshotRef4.Metadata["abc"].Equals("def"));
            }
        }

#if TASK
        [TestMethod]
        [Description("Test share snapshot create - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateShareSnapshotTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            share.Metadata["key1"] = "value1";
            share.SetMetadataAsync().Wait();

            CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("dir1");
            dir1.CreateAsync().Wait();
            CloudFile file1 = dir1.GetFileReference("file1");
            file1.CreateAsync(1024).Wait();
            byte[] buffer = GetRandomBuffer(1024);
            file1.UploadFromByteArrayAsync(buffer, 0, 1024).Wait();
            dir1.Metadata["key2"] = "value2";
            dir1.SetMetadataAsync().Wait();

            CloudFileShare snapshot = share.SnapshotAsync().Result;
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare snapshotRef = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
            Assert.IsTrue(snapshotRef.ExistsAsync().Result);
            Assert.IsTrue(snapshotRef.Metadata.Count == 1 && snapshotRef.Metadata["key1"].Equals("value1"));

            CloudFileShare snapshotRef2 = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
            snapshotRef2.FetchAttributesAsync().Wait();
            Assert.IsTrue(snapshotRef2.Metadata.Count == 1 && snapshotRef2.Metadata["key1"].Equals("value1"));

            Assert.IsTrue(snapshot.Metadata.Count == 1 && snapshot.Metadata["key1"].Equals("value1"));

            CloudFileDirectory snapshotDir1 = snapshot.GetRootDirectoryReference().GetDirectoryReference("dir1");
            snapshotDir1.FetchAttributesAsync().Wait();
            Assert.IsTrue(snapshotDir1.Metadata.Count == 1 && snapshotDir1.Metadata["key2"].Equals("value2"));

            // create snapshot with metadata
            IDictionary<string, string> shareMeta2 = new Dictionary<string, string>();
            shareMeta2.Add("abc", "def");
            CloudFileShare snapshotRef3 = share.SnapshotAsync(shareMeta2, null, null, null).Result;
            CloudFileShare snapshotRef4 = client.GetShareReference(snapshotRef3.Name, snapshotRef3.SnapshotTime);
            Assert.IsTrue(snapshotRef4.ExistsAsync().Result);
            Assert.IsTrue(snapshotRef4.Metadata.Count == 1 && snapshotRef4.Metadata["abc"].Equals("def"));
        }
#endif

        [TestMethod]
        [Description("Test deleting a share that contains snapshots")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteSnapshotOptions()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            CloudFileShare snapshot = share.Snapshot();

            try
            {
                share.Delete(DeleteShareSnapshotsOption.None, null, null, null);
                Assert.Fail("Should not be able to delete a share that has snapshots");
            }
            catch (StorageException e)
            {
                Assert.AreEqual("ShareHasSnapshots", e.RequestInformation.ExtendedErrorInformation.ErrorCode);
            }

            share.Delete(DeleteShareSnapshotsOption.IncludeSnapshots, null, null, null);

            Assert.IsFalse(share.Exists());
            Assert.IsFalse(snapshot.Exists());
        }

        [TestMethod]
        [Description("Test deleting a share that contains snapshots - APM")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteSnapshotOptionsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                share.EndCreate(result);

                result = share.BeginSnapshot(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                CloudFileShare snapshot = share.EndSnapshot(result);

                try
                {
                    result = share.BeginDelete( DeleteShareSnapshotsOption.None, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndDelete(result);

                    Assert.Fail("Should not be able to delete a share that has snapshots");
                }
                catch (StorageException e)
                {
                    Assert.AreEqual("ShareHasSnapshots", e.RequestInformation.ExtendedErrorInformation.ErrorCode);
                }

                result = share.BeginDelete(DeleteShareSnapshotsOption.IncludeSnapshots, null, null, null, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                share.EndDelete(result);

                result = share.BeginExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsFalse(share.EndExists(result));

                result = snapshot.BeginExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsFalse(snapshot.EndExists(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Test deleting a share that contains snapshots - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareDeleteSnapshotOptionsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();

            CloudFileShare snapshot = share.SnapshotAsync().Result;

            try
            {
                share.DeleteAsync(DeleteShareSnapshotsOption.None, null, null, null, CancellationToken.None).Wait();
                Assert.Fail("Should not be able to delete a share that has snapshots");
            }
            catch (AggregateException ae)
            {
                StorageException se = ae.InnerException as StorageException;
                Assert.AreEqual("ShareHasSnapshots", se.RequestInformation.ExtendedErrorInformation.ErrorCode);
            }

            share.DeleteAsync(DeleteShareSnapshotsOption.IncludeSnapshots, null, null, null, CancellationToken.None).Wait();

            Assert.IsFalse(share.ExistsAsync().Result);
            Assert.IsFalse(snapshot.ExistsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Test invalid APIs on a share snapshot")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileInvalidApisForShareSnapshot()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            CloudFileShare snapshot = share.Snapshot();
            try
            {
                snapshot.Create();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.GetPermissions();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.GetStats();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SetMetadata();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SetPermissions(null);
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SetProperties();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.Snapshot();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
        }

        [TestMethod]
        [Description("Test invalid APIs on a share snapshot - APM")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileInvalidApisForShareSnapshotAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                share.EndCreate(result);

                result = share.BeginSnapshot(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                CloudFileShare snapshot = share.EndSnapshot(result);

                try
                {
                    result = snapshot.BeginCreate(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSnapshot(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
                try
                {
                    result = snapshot.BeginGetPermissions(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndGetPermissions(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
                try
                {
                    result = snapshot.BeginGetStats(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndGetStats(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
                try
                {
                    result = snapshot.BeginSetMetadata(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSetMetadata(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
                try
                {
                    result = snapshot.BeginSetPermissions(null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSetPermissions(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
                try
                {
                    result = snapshot.BeginSetProperties(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSetProperties(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
                try
                {
                    result = snapshot.BeginSnapshot(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    share.EndSnapshot(result);
                }
                catch (InvalidOperationException e)
                {
                    Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
                }
            }
        }

#if TASK
        [TestMethod]
        [Description("Test invalid APIs on a share snapshot - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileInvalidApisForShareSnapshotTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();

            CloudFileShare snapshot = share.SnapshotAsync().Result;
            try
            {
                snapshot.CreateAsync().Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.GetPermissionsAsync().Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.GetStatsAsync().Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SetMetadataAsync().Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SetPermissionsAsync(null).Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SetPropertiesAsync().Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                snapshot.SnapshotAsync().Wait();
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
        }
#endif

        [TestMethod]
        [Description("Test list files and directories within a snapshot")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListFilesAndDirectoriesWithinSnapshot()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            CloudFileDirectory myDir = share.GetRootDirectoryReference().GetDirectoryReference("mydir");

            myDir.Create();
            myDir.GetFileReference("myfile").Create(1024);;
            myDir.GetDirectoryReference("yourDir").Create();
            Assert.IsTrue(share.Exists());
            CloudFileShare snapshot = share.Snapshot();
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare snapshotRef = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
            IEnumerable<IListFileItem> listResult = snapshotRef.GetRootDirectoryReference().ListFilesAndDirectories();
            int count = 0;
            foreach (IListFileItem listFileItem in listResult)
            {
                count++;
                Assert.AreEqual("mydir", ((CloudFileDirectory)listFileItem).Name);
            }

            Assert.AreEqual(1, count);

            count = 0;
            listResult = snapshotRef.GetRootDirectoryReference().GetDirectoryReference("mydir").ListFilesAndDirectories();
            foreach (IListFileItem listFileItem in listResult) {
                if (listFileItem is CloudFileDirectory)
                {
                    count++;
                    CloudFileDirectory listedDir = (CloudFileDirectory)listFileItem;
                    Assert.IsTrue(listedDir.SnapshotQualifiedUri.ToString().Contains(
                        "sharesnapshot=" + snapshot.SnapshotTime.Value.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture)));
                    Assert.AreEqual("yourDir", listedDir.Name);
                }
                else
                {
                    count++;
                    CloudFile listedFile = (CloudFile)listFileItem;
                    Assert.IsTrue(listedFile.SnapshotQualifiedUri.ToString().Contains(
                        "sharesnapshot=" + snapshot.SnapshotTime.Value.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture)));
                    Assert.AreEqual("myfile", listedFile.Name);
                }
            }

            Assert.AreEqual(2, count);

            snapshot.Delete();
            share.Delete();
        }

        [TestMethod]
        [Description("Test list files and directories within a snapshot - APM")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListFilesAndDirectoriesWithinSnapshotAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = share.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                share.EndCreate(result);

                CloudFileDirectory myDir = share.GetRootDirectoryReference().GetDirectoryReference("mydir");
                result = myDir.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                myDir.EndCreate(result);

                myDir.GetFileReference("myfile").Create(1024);
                CloudFileDirectory yourDir = myDir.GetDirectoryReference("yourDir");
                result = yourDir.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                yourDir.EndCreate(result);

                result = share.BeginExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(share.EndExists(result));

                result = share.BeginSnapshot(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                CloudFileShare snapshot = share.EndSnapshot(result);

                CloudFileClient client = GenerateCloudFileClient();
                CloudFileShare snapshotRef = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
                result = snapshotRef.GetRootDirectoryReference().BeginListFilesAndDirectoriesSegmented(null, ArgIterator => waitHandle.Set(), null);
                waitHandle.WaitOne();
                IEnumerable<IListFileItem> listResult = snapshotRef.GetRootDirectoryReference().EndListFilesAndDirectoriesSegmented(result).Results;
                int count = 0;
                foreach (IListFileItem listFileItem in listResult)
                {
                    count++;
                    Assert.AreEqual("mydir", ((CloudFileDirectory)listFileItem).Name);
                }

                Assert.AreEqual(1, count);

                count = 0;
                result = snapshotRef.GetRootDirectoryReference().GetDirectoryReference("mydir").BeginListFilesAndDirectoriesSegmented(null, ArgIterator => waitHandle.Set(), null);
                waitHandle.WaitOne();
                listResult = snapshotRef.GetRootDirectoryReference().EndListFilesAndDirectoriesSegmented(result).Results;
                foreach (IListFileItem listFileItem in listResult)
                {
                    if (listFileItem is CloudFileDirectory)
                    {
                        count++;
                        CloudFileDirectory listedDir = (CloudFileDirectory)listFileItem;
                        Assert.IsTrue(listedDir.SnapshotQualifiedUri.ToString().Contains(
                            "sharesnapshot=" + snapshot.SnapshotTime.Value.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture)));
                        Assert.AreEqual("yourDir", listedDir.Name);
                    }
                    else
                    {
                        count++;
                        CloudFile listedFile = (CloudFile)listFileItem;
                        Assert.IsTrue(listedFile.SnapshotQualifiedUri.ToString().Contains(
                            "sharesnapshot=" + snapshot.SnapshotTime.Value.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture)));
                        Assert.AreEqual("myfile", listedFile.Name);
                    }
                }

                Assert.AreEqual(2, count);

                result = snapshot.BeginDelete(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                snapshot.EndDelete(result);

                result = share.BeginDelete(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                share.EndDelete(result);
            }
        }

#if TASK
        [TestMethod]
        [Description("Test list files and directories within a snapshot - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListFilesAndDirectoriesWithinSnapshotTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            CloudFileDirectory myDir = share.GetRootDirectoryReference().GetDirectoryReference("mydir");

            myDir.CreateAsync().Wait();
            myDir.GetFileReference("myfile").CreateAsync(1024);
            myDir.GetDirectoryReference("yourDir").CreateAsync();
            Assert.IsTrue(share.ExistsAsync().Result);
            CloudFileShare snapshot = share.SnapshotAsync().Result;
            CloudFileClient client = GenerateCloudFileClient();
            CloudFileShare snapshotRef = client.GetShareReference(snapshot.Name, snapshot.SnapshotTime);
            List<IListFileItem> listedFileItems = new List<IListFileItem>();
            FileContinuationToken token = null;
            do
            {
                FileResultSegment resultSegment = snapshotRef.GetRootDirectoryReference().ListFilesAndDirectoriesSegmentedAsync(token).Result;
                token = resultSegment.ContinuationToken;

                foreach (IListFileItem listResultItem in resultSegment.Results)
                {
                    listedFileItems.Add(listResultItem);
                }
            }
            while (token != null);

            int count = 0;
            foreach (IListFileItem listFileItem in listedFileItems)
            {
                count++;
                Assert.AreEqual("mydir", ((CloudFileDirectory)listFileItem).Name);
            }

            Assert.AreEqual(1, count);

            token = null;
            listedFileItems.Clear();
            do
            {
                FileResultSegment resultSegment = snapshotRef.GetRootDirectoryReference().GetDirectoryReference("mydir").ListFilesAndDirectoriesSegmentedAsync(token).Result;
                token = resultSegment.ContinuationToken;

                foreach (IListFileItem listResultItem in resultSegment.Results)
                {
                    listedFileItems.Add(listResultItem);
                }
            }
            while (token != null);

            count = 0;
            foreach (IListFileItem listFileItem in listedFileItems)
            {
                if (listFileItem is CloudFileDirectory)
                {
                    count++;
                    CloudFileDirectory listedDir = (CloudFileDirectory)listFileItem;
                    Assert.IsTrue(listedDir.SnapshotQualifiedUri.ToString().Contains(
                        "sharesnapshot=" + snapshot.SnapshotTime.Value.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture)));
                    Assert.AreEqual("yourDir", listedDir.Name);
                }
                else
                {
                    count++;
                    CloudFile listedFile = (CloudFile)listFileItem;
                    Assert.IsTrue(listedFile.SnapshotQualifiedUri.ToString().Contains(
                        "sharesnapshot=" + snapshot.SnapshotTime.Value.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture)));
                    Assert.AreEqual("myfile", listedFile.Name);
                }
            }

            Assert.AreEqual(2, count);

            snapshot.DeleteAsync().Wait();
            share.DeleteAsync().Wait();
        }
#endif

        /*
        [TestMethod]
        [Description("Test conditional access on a share")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileShareConditionalAccess()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                share.FetchAttributes();

                string currentETag = share.Properties.ETag;
                DateTimeOffset currentModifiedTime = share.Properties.LastModified.Value;

                // ETag conditional tests
                share.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                share.SetMetadata();

                share.FetchAttributes();
                string newETag = share.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                // LastModifiedTime tests
                currentModifiedTime = share.Properties.LastModified.Value;

                share.Metadata["DateConditionalName"] = "DateConditionalValue";

                TestHelper.ExpectedException(
                    () => share.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null),
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                share.Metadata["DateConditionalName"] = "DateConditionalValue2";
                currentETag = share.Properties.ETag;

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                share.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                share.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                share.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                share.FetchAttributes();
                newETag = share.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
        */
    }
}
