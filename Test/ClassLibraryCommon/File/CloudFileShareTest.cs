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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
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
                }
            }
            finally
            {
                share.DeleteIfExists();
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
                List<string> fileNames = CreateFiles(share, 6000);
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
