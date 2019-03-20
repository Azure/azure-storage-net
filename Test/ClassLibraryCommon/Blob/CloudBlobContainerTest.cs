// -----------------------------------------------------------------------------------------
// <copyright file="CloudBlobContainerTest.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class CloudBlobContainerTest : BlobTestBase
    {
        private static void TestAccess(BlobContainerPublicAccessType accessType, CloudBlobContainer container, CloudBlob inputBlob)
        {
            StorageCredentials credentials = new StorageCredentials();
            container = new CloudBlobContainer(container.Uri, credentials);
            CloudPageBlob blob = new CloudPageBlob(inputBlob.Uri, credentials);

            if (accessType.Equals(BlobContainerPublicAccessType.Container))
            {
                blob.FetchAttributes();
                container.ListBlobs().ToArray();
                container.FetchAttributes();
            }
            else if (accessType.Equals(BlobContainerPublicAccessType.Blob))
            {
                blob.FetchAttributes();
                TestHelper.ExpectedException(
                    () => container.ListBlobs().ToArray(),
                    "List blobs while public access does not allow for listing",
                    HttpStatusCode.NotFound);
                TestHelper.ExpectedException(
                    () => container.FetchAttributes(),
                    "Fetch container attributes while public access does not allow",
                    HttpStatusCode.NotFound);
            }
            else
            {
                TestHelper.ExpectedException(
                    () => blob.FetchAttributes(),
                    "Fetch blob attributes while public access does not allow",
                    HttpStatusCode.NotFound);
                TestHelper.ExpectedException(
                    () => container.ListBlobs().ToArray(),
                    "List blobs while public access does not allow for listing",
                    HttpStatusCode.NotFound);
                TestHelper.ExpectedException(
                    () => container.FetchAttributes(),
                    "Fetch container attributes while public access does not allow",
                    HttpStatusCode.NotFound);
            }
        }

#if TASK
        private static void TestAccessTask(BlobContainerPublicAccessType accessType, CloudBlobContainer container, CloudBlob inputBlob)
        {
            StorageCredentials credentials = new StorageCredentials();
            container = new CloudBlobContainer(container.Uri, credentials);
            CloudPageBlob blob = new CloudPageBlob(inputBlob.Uri, credentials);

            if (accessType.Equals(BlobContainerPublicAccessType.Container))
            {
                blob.FetchAttributesAsync().Wait();
                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(token);
                    results.Results.ToArray();
                    token = results.ContinuationToken;
                }
                while (token != null);
                container.FetchAttributesAsync().Wait();
            }
            else if (accessType.Equals(BlobContainerPublicAccessType.Blob))
            {
                blob.FetchAttributesAsync().Wait();

                TestHelper.ExpectedExceptionTask(
                    container.ListBlobsSegmentedAsync(null),
                    "List blobs while public access does not allow for listing",
                    HttpStatusCode.NotFound);
                TestHelper.ExpectedExceptionTask(
                    container.FetchAttributesAsync(),
                    "Fetch container attributes while public access does not allow",
                    HttpStatusCode.NotFound);
            }
            else
            {
                TestHelper.ExpectedExceptionTask(
                    blob.FetchAttributesAsync(),
                    "Fetch blob attributes while public access does not allow",
                    HttpStatusCode.NotFound);
                TestHelper.ExpectedExceptionTask(
                    container.ListBlobsSegmentedAsync(null),
                    "List blobs while public access does not allow for listing",
                    HttpStatusCode.NotFound);
                TestHelper.ExpectedExceptionTask(
                    container.FetchAttributesAsync(),
                    "Fetch container attributes while public access does not allow",
                    HttpStatusCode.NotFound);
            }
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
        [Description("Test container name validation.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerNameValidation()
        {
            NameValidator.ValidateContainerName("alpha");
            NameValidator.ValidateContainerName("4lphanum3r1c");
            NameValidator.ValidateContainerName("middle-dash");
            NameValidator.ValidateContainerName("$root");
            NameValidator.ValidateContainerName("$logs");

            TestInvalidContainerHelper(null, "Null containers invalid.", "Invalid container name. The container name may not be null, empty, or whitespace only.");
            TestInvalidContainerHelper("$ROOT", "Root container case sensitive.", "Invalid container name. Check MSDN for more information about valid container naming.");
            TestInvalidContainerHelper("double--dash", "Double dashes not allowed.", "Invalid container name. Check MSDN for more information about valid container naming.");
            TestInvalidContainerHelper("CapsLock", "Lowercase only.", "Invalid container name. Check MSDN for more information about valid container naming.");
            TestInvalidContainerHelper("illegal$char", "Only alphanumeric and hyphen characters.", "Invalid container name. Check MSDN for more information about valid container naming.");
            TestInvalidContainerHelper("illegal!char", "Only alphanumeric and hyphen characters.", "Invalid container name. Check MSDN for more information about valid container naming.");
            TestInvalidContainerHelper("white space", "Only alphanumeric and hyphen characters.", "Invalid container name. Check MSDN for more information about valid container naming.");
            TestInvalidContainerHelper("2c", "Root container case sensitive.", "Invalid container name length. The container name must be between 3 and 63 characters long.");
            TestInvalidContainerHelper(new string('n', 64), "Between 3 and 64 characters.", "Invalid container name length. The container name must be between 3 and 63 characters long.");
        }

        private void TestInvalidContainerHelper(string containerName, string failMessage, string exceptionMessage)
        {
            try
            {
                NameValidator.ValidateContainerName(containerName);
                Assert.Fail(failMessage);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual(exceptionMessage, e.Message);
            }
        }

        [TestMethod]
        [Description("Validate container references")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerReference()
        {
            CloudBlobClient client = GenerateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("container");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("directory1/blob1");
            CloudPageBlob pageBlob = container.GetPageBlobReference("directory2/blob2");
            CloudAppendBlob appendBlob = container.GetAppendBlobReference("directory3/blob3");
            CloudBlobDirectory directory = container.GetDirectoryReference("directory4");
            CloudBlobDirectory directory2 = directory.GetDirectoryReference("directory5");

            Assert.AreEqual(container, blockBlob.Container);
            Assert.AreEqual(container, pageBlob.Container);
            Assert.AreEqual(container, appendBlob.Container);
            Assert.AreEqual(container, directory.Container);
            Assert.AreEqual(container, directory2.Container);
            Assert.AreEqual(container, directory2.Parent.Container);
            Assert.AreEqual(container, blockBlob.Parent.Container);
            Assert.AreEqual(container, blockBlob.Parent.Container);
        }

        [TestMethod]
        [Description("Create and delete a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreate()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            TestHelper.ExpectedException(
                () => container.Create(),
                "Creating already exists container should fail",
                HttpStatusCode.Conflict);
            container.Delete();
        }

        [TestMethod]
        [Description("Create and delete a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = container.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                container.EndCreate(result);
                result = container.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.ExpectedException(
                    () => container.EndCreate(result),
                    "Creating already exists container should fail",
                    HttpStatusCode.Conflict);
            }
            container.Delete();
        }

#if TASK
        [TestMethod]
        [Description("Create and delete a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();

            AggregateException e = TestHelper.ExpectedException<AggregateException>(
                 container.CreateAsync().Wait,
                "Creating already exists container should fail");
            Assert.IsInstanceOfType(e.InnerException, typeof(StorageException));
            Assert.AreEqual((int)HttpStatusCode.Conflict, ((StorageException)e.InnerException).RequestInformation.HttpStatusCode);
            Task.Factory.FromAsync(container.BeginDelete, container.EndDelete, null).Wait();
        }

        [TestMethod]
        [Description("Create and delete a container with access type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateTaskWithContainerAccessTypeOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CancellationTokenSource cts = new CancellationTokenSource();

            container.CreateAsync(BlobContainerPublicAccessType.Container, null, null, cts.Token).Wait();

            CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
            blob1.Create(0);
            CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
            blob2.Create(0);

            TestAccess(BlobContainerPublicAccessType.Container, container, blob1);
            TestAccess(BlobContainerPublicAccessType.Container, container, blob2);

            Task.Factory.FromAsync(container.BeginDelete, container.EndDelete, null).Wait();
        }

        [TestMethod]
        [Description("Create and delete a container with access type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateTaskWithBlobAccessTypeOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CancellationTokenSource cts = new CancellationTokenSource();
            container.CreateAsync(BlobContainerPublicAccessType.Blob, null, null, cts.Token).Wait();

            CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
            blob1.Create(0);
            CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
            blob2.Create(0);

            TestAccess(BlobContainerPublicAccessType.Blob, container, blob1);
            TestAccess(BlobContainerPublicAccessType.Blob, container, blob2);

            Task.Factory.FromAsync(container.BeginDelete, container.EndDelete, null).Wait();
        }

        [TestMethod]
        [Description("Create and delete a container with access type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateTaskWithPrivateAccessTypeOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CancellationTokenSource cts = new CancellationTokenSource();
            container.CreateAsync(BlobContainerPublicAccessType.Off, null, null, cts.Token).Wait();

            CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
            blob1.Create(0);
            CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
            blob2.Create(0);

            TestAccess(BlobContainerPublicAccessType.Off, container, blob1);
            TestAccess(BlobContainerPublicAccessType.Off, container, blob2);

            Task.Factory.FromAsync(container.BeginDelete, container.EndDelete, null).Wait();
        }

        [TestMethod]
        [Description("Create and delete a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerCreateTaskWithCancellation()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CancellationTokenSource cts = new CancellationTokenSource();
            OperationContext ctx = new OperationContext();

            Task createTask = container.CreateAsync(BlobContainerPublicAccessType.Off, null, ctx, cts.Token);
            try
            {
                Thread.Sleep(0);
                cts.Cancel();
                await createTask;

                // Should throw aggregate exception
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(StorageException));
                Assert.IsNotNull(ex.InnerException);
                Assert.IsInstanceOfType(ex.InnerException, typeof(OperationCanceledException));
            }

            // Validate that we did attempt one request and it was cancelled
            TestHelper.AssertCancellation(ctx);
        }
#endif

        [TestMethod]
        [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsTrue(container.CreateIfNotExists());
                Assert.IsFalse(container.CreateIfNotExists());
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsWithContainerAccessType()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsTrue(container.CreateIfNotExists(BlobContainerPublicAccessType.Container));

                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Container, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Container, container, blob2);

                Assert.IsFalse(container.CreateIfNotExists(BlobContainerPublicAccessType.Container));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("APM - Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsWithContainerAccessTypeAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreateIfNotExists(BlobContainerPublicAccessType.Container, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(container.EndCreateIfNotExists(result));

                    CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                    blob1.Create(0);
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                    blob2.Create(0);

                    TestAccess(BlobContainerPublicAccessType.Container, container, blob1);
                    TestAccess(BlobContainerPublicAccessType.Container, container, blob2);

                    result = container.BeginCreateIfNotExists(BlobContainerPublicAccessType.Container, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(container.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsWithBlobAccessType()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsTrue(container.CreateIfNotExists(BlobContainerPublicAccessType.Blob));

                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Blob, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Blob, container, blob2);

                Assert.IsFalse(container.CreateIfNotExists(BlobContainerPublicAccessType.Container));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("APM - Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsWithBlobAccessTypeAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreateIfNotExists(BlobContainerPublicAccessType.Blob, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(container.EndCreateIfNotExists(result));

                    CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                    blob1.Create(0);
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                    blob2.Create(0);

                    TestAccess(BlobContainerPublicAccessType.Blob, container, blob1);
                    TestAccess(BlobContainerPublicAccessType.Blob, container, blob2);

                    result = container.BeginCreateIfNotExists(BlobContainerPublicAccessType.Blob, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(container.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsWithPrivateAccessType()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsTrue(container.CreateIfNotExists(BlobContainerPublicAccessType.Off));

                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Off, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Off, container, blob2);

                Assert.IsFalse(container.CreateIfNotExists(BlobContainerPublicAccessType.Container));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("APM - Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsWithPrivateAccessTypeAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreateIfNotExists(BlobContainerPublicAccessType.Off, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(container.EndCreateIfNotExists(result));

                    CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                    blob1.Create(0);
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                    blob2.Create(0);

                    TestAccess(BlobContainerPublicAccessType.Off, container, blob1);
                    TestAccess(BlobContainerPublicAccessType.Off, container, blob2);

                    result = container.BeginCreateIfNotExists(BlobContainerPublicAccessType.Off, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(container.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CloudBlobContainer container2 = GetRandomContainerReference();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(container.EndCreateIfNotExists(result));
                    result = container.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(container.EndCreateIfNotExists(result));

                    // Test the case where the callback is null.
                    // There is a race condition (inherent in the APM pattern) about what will happen if an exception is thrown in the callback
                    // This is why we need the sleep - to ensure that if our code nullref's in the null-callback case, the exception has time 
                    // to get processed before the End call.
                    OperationContext context = new OperationContext();
                    context.RequestCompleted += (sender, e) => waitHandle.Set();
                    result = container2.BeginCreateIfNotExists(null, context, null, null);
                    waitHandle.WaitOne();
                    Thread.Sleep(2000);
                    Assert.IsTrue(container2.EndCreateIfNotExists(result));
                    context = new OperationContext();
                    context.RequestCompleted += (sender, e) => waitHandle.Set();
                    result = container2.BeginCreateIfNotExists(null, context, null, null);
                    waitHandle.WaitOne();
                    Thread.Sleep(2000);
                    Assert.IsFalse(container2.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                container.DeleteIfExists();
                container2.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateIfNotExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsTrue(container.CreateIfNotExistsAsync().Result);
                Assert.IsFalse(container.CreateIfNotExistsAsync().Result);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
#endif

        [TestMethod]
        [Description("Try to delete a non-existing container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerDeleteIfExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            Assert.IsFalse(container.DeleteIfExists());
            container.Create();
            Assert.IsTrue(container.DeleteIfExists());
            Assert.IsFalse(container.DeleteIfExists());
        }

        [TestMethod]
        [Description("Try to delete a non-existing container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerDeleteIfExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = container.BeginDeleteIfExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(container.EndDeleteIfExists(result));
                result = container.BeginCreate(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                container.EndCreate(result);
                result = container.BeginDeleteIfExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsTrue(container.EndDeleteIfExists(result));
                result = container.BeginDeleteIfExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(container.EndDeleteIfExists(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to delete a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerDeleteTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();
            Assert.IsTrue(container.Exists());
            container.DeleteAsync().Wait();
            Assert.IsFalse(container.Exists());
        }

        [TestMethod]
        [Description("Try to delete a non-existing container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerDeleteIfExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            Assert.IsFalse(container.DeleteIfExistsAsync().Result);
            container.CreateAsync().Wait();
            Assert.IsTrue(container.DeleteIfExistsAsync().Result);
            Assert.IsFalse(container.DeleteIfExistsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Check a container's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);

            Assert.IsFalse(container2.Exists());

            container.Create();

            try
            {
                Assert.IsTrue(container2.Exists());
                Assert.IsNotNull(container2.Properties.ETag);
                Assert.IsTrue(container2.Properties.HasImmutabilityPolicy.HasValue);
                Assert.IsTrue(container2.Properties.HasLegalHold.HasValue);
                Assert.IsFalse(container2.Properties.HasImmutabilityPolicy.Value);
                Assert.IsFalse(container2.Properties.HasLegalHold.Value);
            }
            finally
            {
                container.Delete();
            }

            Assert.IsFalse(container2.Exists());
        }

        [TestMethod]
        [Description("Check a container's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = container2.BeginExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(container2.EndExists(result));

                container.Create();

                try
                {
                    result = container2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(container2.EndExists(result));
                    Assert.IsNotNull(container2.Properties.ETag);
                }
                finally
                {
                    container.Delete();
                }

                result = container2.BeginExists(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                Assert.IsFalse(container2.EndExists(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Check a container's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);

            Assert.IsFalse(container2.ExistsAsync().Result);

            container.CreateAsync().Wait();

            try
            {
                Assert.IsTrue(container2.ExistsAsync().Result);
                Assert.IsNotNull(container2.Properties.ETag);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }

            Assert.IsFalse(container2.ExistsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetPermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                permissions.SharedAccessPolicies.Add("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                container.SetPermissions(permissions);

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    permissions = container2.GetPermissions();
                    Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetPermissionsOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                container.SetPermissions(permissions);

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                    permissions = container2.GetPermissions();
                    Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetPermissionsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                permissions.SharedAccessPolicies.Add("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginSetPermissions(permissions, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    container.EndSetPermissions(result);

                    TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                    {
                        CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                        result = container.BeginGetPermissions(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        permissions = container.EndGetPermissions(result);
                        Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                        Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                        Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                        Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                        Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                    });
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetPermissionsAPMOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                permissions.SharedAccessPolicies.Add("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginSetPermissions(permissions, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    container.EndSetPermissions(result);

                    TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                    {
                        CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                        result = container.BeginGetPermissions(null, null, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        permissions = container.EndGetPermissions(result);
                        Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                        Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                        Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                        Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                        Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                        Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                    });
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetPermissionsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissionsAsync().Result;
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                permissions.SharedAccessPolicies.Add("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                container.SetPermissionsAsync(permissions).Wait();

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                    permissions = container2.GetPermissionsAsync().Result;
                    Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetPermissionsOverloadTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                BlobContainerPermissions permissions = container.GetPermissionsAsync().Result;
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                container.SetPermissionsAsync(permissions).Wait();

                TestHelper.SpinUpTo30SecondsIgnoringFailures(() =>
                {
                    CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                    permissions = container2.GetPermissionsAsync().Result;
                    Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                    Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                    Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                    Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                    Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                    Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
                });
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
#endif

        [TestMethod]
        [Description("Clear container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerClearPermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                container.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);

                Assert.AreEqual(true, permissions.SharedAccessPolicies.Contains(sharedAccessPolicy));
                Assert.AreEqual(true, permissions.SharedAccessPolicies.ContainsKey("key1"));
                permissions.SharedAccessPolicies.Clear();
                container.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                permissions = container.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Copy container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCopyPermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry2 = start.AddMinutes(30);
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessBlobPolicy>("key2", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);

                KeyValuePair<String, SharedAccessBlobPolicy>[] sharedAccessPolicyArray = new KeyValuePair<string, SharedAccessBlobPolicy>[2];
                permissions.SharedAccessPolicies.CopyTo(sharedAccessPolicyArray, 0);
                Assert.AreEqual(2, sharedAccessPolicyArray.Length);
                Assert.AreEqual(sharedAccessPolicy, sharedAccessPolicyArray[0]);
                Assert.AreEqual(sharedAccessPolicy2, sharedAccessPolicyArray[1]);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Remove container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerRemovePermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessBlobPolicy>("key2", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                container.SetPermissions(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove(sharedAccessPolicy2);
                container.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);

                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = container.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                container.SetPermissions(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove("key2");
                container.SetPermissions(permissions);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = container.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("TryGetValue for container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerTryGetValuePermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessBlobPolicy>("key2", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                container.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions = container.GetPermissions();
                SharedAccessBlobPolicy retrPolicy;
                permissions.SharedAccessPolicies.TryGetValue("key1", out retrPolicy);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, retrPolicy.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, retrPolicy.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, retrPolicy.SharedAccessExpiryTime);

                SharedAccessBlobPolicy retrPolicy2;
                permissions.SharedAccessPolicies.TryGetValue("key2", out retrPolicy2);
                Assert.AreEqual(sharedAccessPolicy2.Value.Permissions, retrPolicy2.Permissions);
                Assert.AreEqual(sharedAccessPolicy2.Value.SharedAccessStartTime, retrPolicy2.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy2.Value.SharedAccessExpiryTime, retrPolicy2.SharedAccessExpiryTime);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetEnumerator for container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetEnumeratorPermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessBlobPolicy>("key2", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                IEnumerator<KeyValuePair<string, SharedAccessBlobPolicy>> policies = permissions.SharedAccessPolicies.GetEnumerator();
                policies.MoveNext();
                Assert.AreEqual(sharedAccessPolicy, policies.Current);
                policies.MoveNext();
                Assert.AreEqual(sharedAccessPolicy2, policies.Current);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetValues for container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetValuesPermissions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                BlobContainerPermissions permissions = container.GetPermissions();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessBlobPolicy>("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessBlobPolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessBlobPolicy>("key2", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                ICollection<SharedAccessBlobPolicy> values = permissions.SharedAccessPolicies.Values;
                Assert.AreEqual(2, values.Count);
                Assert.AreEqual(sharedAccessPolicy.Value, values.ElementAt(0));
                Assert.AreEqual(sharedAccessPolicy2.Value, values.ElementAt(1));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Get permissions from string")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerPermissionsFromString()
        {
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy();
            policy.SharedAccessStartTime = DateTime.UtcNow;
            policy.SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(30);

            policy.Permissions = SharedAccessBlobPolicy.PermissionsFromString("rwdl");
            Assert.AreEqual(SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Delete | SharedAccessBlobPermissions.List, policy.Permissions);

            policy.Permissions = SharedAccessBlobPolicy.PermissionsFromString("rwl");
            Assert.AreEqual(SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List, policy.Permissions);

            policy.Permissions = SharedAccessBlobPolicy.PermissionsFromString("rw");
            Assert.AreEqual(SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write, policy.Permissions);

            policy.Permissions = SharedAccessBlobPolicy.PermissionsFromString("rd");
            Assert.AreEqual(SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Delete, policy.Permissions);

            policy.Permissions = SharedAccessBlobPolicy.PermissionsFromString("wl");
            Assert.AreEqual(SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List, policy.Permissions);

            policy.Permissions = SharedAccessBlobPolicy.PermissionsFromString("w");
            Assert.AreEqual(SharedAccessBlobPermissions.Write, policy.Permissions);
        }

        [TestMethod]
        [Description("Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithContainerAccessType()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create(BlobContainerPublicAccessType.Container);
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Container, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Container, container, blob2);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithContainerAccessTypeOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create(BlobContainerPublicAccessType.Container, null, null);
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Container, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Container, container, blob2);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("APM - Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithContainerAccessTypeAPMOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreate(BlobContainerPublicAccessType.Container, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    container.EndCreate(result);
                    CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                    blob1.Create(0);
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                    blob2.Create(0);

                    TestAccess(BlobContainerPublicAccessType.Container, container, blob1);
                    TestAccess(BlobContainerPublicAccessType.Container, container, blob2);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("APM - Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithContainerAccessTypeOverloadTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.CreateAsync(BlobContainerPublicAccessType.Container, null, null).Wait();
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.CreateAsync(0).Wait();
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.CreateAsync(0).Wait();

                TestAccessTask(BlobContainerPublicAccessType.Container, container, blob1);
                TestAccessTask(BlobContainerPublicAccessType.Container, container, blob2);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithBlobAccessType()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create(BlobContainerPublicAccessType.Blob);
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Blob, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Blob, container, blob2);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithBlobAccessTypeOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create(BlobContainerPublicAccessType.Blob, null, null);
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Blob, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Blob, container, blob2);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("APM - Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithBlobAccessTypeAPMOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreate(BlobContainerPublicAccessType.Blob, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    container.EndCreate(result);
                    CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                    blob1.Create(0);
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                    blob2.Create(0);

                    TestAccess(BlobContainerPublicAccessType.Blob, container, blob1);
                    TestAccess(BlobContainerPublicAccessType.Blob, container, blob2);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithPrivateAccessType()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create(BlobContainerPublicAccessType.Off);
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Off, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Off, container, blob2);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithPrivateAccessTypeOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create(BlobContainerPublicAccessType.Off, null, null);
                CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                blob1.Create(0);
                CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                blob2.Create(0);

                TestAccess(BlobContainerPublicAccessType.Off, container, blob1);
                TestAccess(BlobContainerPublicAccessType.Off, container, blob2);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("APM - Create a container with AccessType")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithPrivateAccessTypeAPMOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = container.BeginCreate(BlobContainerPublicAccessType.Off, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    container.EndCreate(result);
                    CloudPageBlob blob1 = container.GetPageBlobReference("blob1");
                    blob1.Create(0);
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob2");
                    blob2.Create(0);

                    TestAccess(BlobContainerPublicAccessType.Off, container, blob1);
                    TestAccess(BlobContainerPublicAccessType.Off, container, blob2);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Metadata.Add("key1", "value1");
                container.Create();

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                container2.FetchAttributes();
                Assert.AreEqual(1, container2.Metadata.Count);
                Assert.AreEqual("value1", container2.Metadata["key1"]);

                Assert.IsTrue(container2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(container2.Properties.ETag);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateWithMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Metadata.Add("key1", "value1");
                container.CreateAsync().Wait();

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                container2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, container2.Metadata.Count);
                Assert.AreEqual("value1", container2.Metadata["key1"]);

                Assert.IsTrue(container2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(container2.Properties.ETag);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                container2.FetchAttributes();
                Assert.AreEqual(0, container2.Metadata.Count);

                container.Metadata.Add("key1", "value1");
                container.SetMetadata();

                container2.FetchAttributes();
                Assert.AreEqual(1, container2.Metadata.Count);
                Assert.AreEqual("value1", container2.Metadata["key1"]);

                CloudBlobContainer container3 = container.ServiceClient.ListContainers(container.Name, ContainerListingDetails.Metadata).First();
                Assert.AreEqual(1, container3.Metadata.Count);
                Assert.AreEqual("value1", container3.Metadata["key1"]);

                container.Metadata.Clear();
                container.SetMetadata();

                container2.FetchAttributes();
                Assert.AreEqual(0, container2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerRegionalSetMetadata()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sk-SK");

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Metadata.Add("sequence", "value");
                container.Metadata.Add("schema", "value");
                container.Create();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetMetadataAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                    IAsyncResult result = container2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    container2.EndFetchAttributes(result);
                    Assert.AreEqual(0, container2.Metadata.Count);

                    container.Metadata.Add("key1", "value1");
                    result = container.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    container.EndSetMetadata(result);

                    result = container2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    container2.EndFetchAttributes(result);
                    Assert.AreEqual(1, container2.Metadata.Count);
                    Assert.AreEqual("value1", container2.Metadata["key1"]);

                    BlobContinuationToken ct = null;
                    IEnumerable<CloudBlobContainer> results = null;
                    do
                    {
                        result = container.ServiceClient.BeginListContainersSegmented(container.Name, ContainerListingDetails.Metadata, null, ct, null, null,
                        ar => waitHandle.Set(),
                        null);
                        waitHandle.WaitOne();

                        ContainerResultSegment resultSegment = container.ServiceClient.EndListContainersSegmented(result);
                        results = resultSegment.Results;
                        ct = resultSegment.ContinuationToken;

                    } while (ct != null && !results.Any());

                    CloudBlobContainer container3 = results.First();
                    Assert.AreEqual(1, container3.Metadata.Count);
                    Assert.AreEqual("value1", container3.Metadata["key1"]);

                    container.Metadata.Clear();
                    result = container.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    container.EndSetMetadata(result);

                    result = container2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    container2.EndFetchAttributes(result);
                    Assert.AreEqual(0, container2.Metadata.Count);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerSetMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                container2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, container2.Metadata.Count);

                container.Metadata.Add("key1", "value1");
                container.SetMetadataAsync().Wait();

                container2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, container2.Metadata.Count);
                Assert.AreEqual("value1", container2.Metadata["key1"]);

                CloudBlobContainer container3 =
                    container.ServiceClient.ListContainersSegmentedAsync(
                        container.Name, ContainerListingDetails.Metadata, null, null, null, null).Result.Results.First();
                Assert.AreEqual(1, container3.Metadata.Count);
                Assert.AreEqual("value1", container3.Metadata["key1"]);

                container.Metadata.Clear();
                container.SetMetadataAsync().Wait();

                container2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, container2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobs()
        {
            CloudBlobClient client = GenerateCloudBlobClient();
            client.DefaultRequestOptions.LocationMode = RetryPolicies.LocationMode.PrimaryThenSecondary;
            CloudBlobContainer container = client.GetContainerReference(GetRandomContainerName());
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                IEnumerable<IListBlobItem> results = container.ListBlobs();
                Assert.AreEqual(blobNames.Count, results.Count());
                foreach (IListBlobItem blobItem in results)
                {
                    Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                    Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                    Assert.AreEqual(RetryPolicies.LocationMode.PrimaryThenSecondary, ((CloudPageBlob)blobItem).ServiceClient.DefaultRequestOptions.LocationMode);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List many blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric)]
        public async Task CloudBlobContainerListManyBlobs()
        {
            int countPerType = 500;
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                List<string> pageBlobNames = await CreateBlobs(container, countPerType, BlobType.PageBlob);
                List<string> blockBlobNames = await CreateBlobs(container, countPerType, BlobType.BlockBlob);
                List<string> appendBlobNames = await CreateBlobs(container, countPerType, BlobType.AppendBlob);
                int count = 0;
                IEnumerable<IListBlobItem> results = container.ListBlobs();
                foreach (IListBlobItem blobItem in results)
                {
                    count++;
                    Assert.IsInstanceOfType(blobItem, typeof(CloudBlob));
                    CloudBlob blob = (CloudBlob)blobItem;
                    if (pageBlobNames.Remove(blob.Name))
                    {
                        Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    }
                    else if (blockBlobNames.Remove(blob.Name))
                    {
                        Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    }
                    else if (appendBlobNames.Remove(blob.Name))
                    {
                        Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    }
                    else
                    {
                        Assert.Fail("Unexpected blob: " + blob.Uri.AbsoluteUri);
                    }
                }

                Assert.AreEqual(3 * countPerType, count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmented()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(null, true, BlobListingDetails.None, 1, token, null, null);
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                        count++;
                    }
                    Assert.IsTrue(count <= 1);
                    token = results.ContinuationToken;
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
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmentedAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    BlobContinuationToken token = null;
                    do
                    {
                        IAsyncResult result = container.BeginListBlobsSegmented(null, true, BlobListingDetails.None, 1, token, null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        BlobResultSegment results = container.EndListBlobsSegmented(result);
                        int count = 0;
                        foreach (IListBlobItem blobItem in results.Results)
                        {
                            Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                            Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                            count++;
                        }
                        Assert.IsTrue(count <= 1);
                        token = results.ContinuationToken;
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
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerListBlobsSegmentedTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();
                List<string> blobNames = CreateBlobsTask(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, 1, token, null, null).Result;
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                        count++;
                    }
                    Assert.IsTrue(count <= 1);
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, blobNames.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmentedOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(token);
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                        count++;
                    }
                    token = results.ContinuationToken;
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
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmentedAPMOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    BlobContinuationToken token = null;
                    do
                    {
                        IAsyncResult result = container.BeginListBlobsSegmented(null, token,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        BlobResultSegment results = container.EndListBlobsSegmented(result);
                        int count = 0;
                        foreach (IListBlobItem blobItem in results.Results)
                        {
                            Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                            Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                            count++;
                        }
                        token = results.ContinuationToken;
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
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmentedOverloadTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmentedAsync(null, token).Result;
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                        count++;
                    }
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, blobNames.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmentedWithPrefixOverload()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented("pb", token);
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                        count++;
                    }
                    token = results.ContinuationToken;
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
        [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsWithSecondaryUri()
        {
            AssertSecondaryEndpoint();

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.BlockBlob);
                blobNames.AddRange(await CreateBlobs(container, 3, BlobType.PageBlob));

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(null, true, BlobListingDetails.None, 1, token, null, null);
                    foreach (CloudBlob blob in results.Results)
                    {
                        Assert.IsTrue(blobNames.Remove(blob.Name));
                        Assert.IsTrue(container.GetBlockBlobReference(blob.Name).StorageUri.Equals(blob.StorageUri));
                    }

                    token = results.ContinuationToken;
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
        [Description("Verify WriteXml/ReadXml Serialize/Deserialize")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobContinuationTokenVerifySerializer()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BlobContinuationToken));

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            StringReader reader;
            string tokenxml;

            BlobContinuationToken writeToken = new BlobContinuationToken
            {
                NextMarker = Guid.NewGuid().ToString(),
                TargetLocation = StorageLocation.Primary
            };

            BlobContinuationToken readToken = null;

            // Write with XmlSerializer
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, writeToken);
                tokenxml = writer.ToString();
            }

            // Read with XmlSerializer
            reader = new StringReader(tokenxml);
            readToken = (BlobContinuationToken)serializer.Deserialize(reader);
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XMLReaderExtensions.CreateAsAsync(new MemoryStream(Encoding.Unicode.GetBytes(tokenxml))))
            {
                readToken = new BlobContinuationToken();
                await readToken.ReadXmlAsync(xmlReader);
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
            readToken = (BlobContinuationToken)serializer.Deserialize(reader);
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XMLReaderExtensions.CreateAsAsync(new MemoryStream(Encoding.Unicode.GetBytes(sb.ToString()))))
            {
                readToken = new BlobContinuationToken();
                await readToken.ReadXmlAsync(xmlReader);
            }
            Assert.AreEqual(writeToken.NextMarker, readToken.NextMarker);
        }

        [TestMethod]
        [Description("Verify ReadXml Deserialization on BlobContinuationToken with empty TargetLocation")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobContinuationTokenVerifyEmptyTargetDeserializer()
        {
            BlobContinuationToken blobContinuationToken = new BlobContinuationToken { TargetLocation = null };
            StringBuilder stringBuilder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(stringBuilder))
            {
                blobContinuationToken.WriteXml(writer);
            }

            string stringToken = stringBuilder.ToString();
            BlobContinuationToken parsedToken = new BlobContinuationToken();
            await parsedToken.ReadXmlAsync(XMLReaderExtensions.CreateAsAsync(new MemoryStream(Encoding.Unicode.GetBytes(stringToken))));
            Assert.AreEqual(parsedToken.TargetLocation, null);
        }

        [TestMethod]
        [Description("Verify GetSchema, WriteXml and ReadXml on BlobContinuationToken")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobContinuationTokenVerifyXmlFunctions()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(null, true, BlobListingDetails.None, 1, token, null, null);
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
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

                        using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(new MemoryStream(Encoding.Unicode.GetBytes(sb.ToString()))))
                        {
                            token = new BlobContinuationToken();
                            await token.ReadXmlAsync(reader);
                        }
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
        [Description("Verify GetSchema, WriteXml and ReadXml on BlobContinuationToken within another Xml")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobContinuationTokenVerifyXmlWithinXml()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                List<string> blobNames = await CreateBlobs(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = container.ListBlobsSegmented(null, true, BlobListingDetails.None, 1, token, null, null);
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
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

                        using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(new MemoryStream(Encoding.Unicode.GetBytes(sb.ToString()))))
                        {
                            token = new BlobContinuationToken();
                            await reader.ReadStartElementAsync();
                            await reader.ReadStartElementAsync();
                            await token.ReadXmlAsync(reader);
                            await reader.ReadEndElementAsync();
                            await reader.ReadEndElementAsync();
                        }
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
        [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetBlobReferenceFromServer()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                blockBlob.PutBlockList(new string[] { });

                CloudPageBlob pageBlob = container.GetPageBlobReference("pb");
                pageBlob.Create(0);

                CloudAppendBlob appendBlob = container.GetAppendBlobReference("ab");
                appendBlob.CreateOrReplace();

                CloudBlobClient client;
                ICloudBlob blob;

                blob = container.GetBlobReferenceFromServer("bb");
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                CloudBlockBlob blockBlobSnapshot = ((CloudBlockBlob)blob).CreateSnapshot();
                blob.SetProperties();
                Uri blockBlobSnapshotUri = new Uri(blockBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + blockBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                blob = container.ServiceClient.GetBlobReferenceFromServer(blockBlobSnapshotUri);
                AssertAreEqual(blockBlobSnapshot.Properties, blob.Properties);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlobSnapshot.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.GetBlobReferenceFromServer("pb");
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                CloudPageBlob pageBlobSnapshot = ((CloudPageBlob)blob).CreateSnapshot();
                blob.SetProperties();
                Uri pageBlobSnapshotUri = new Uri(pageBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + pageBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                blob = container.ServiceClient.GetBlobReferenceFromServer(pageBlobSnapshotUri);
                AssertAreEqual(pageBlobSnapshot.Properties, blob.Properties);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlobSnapshot.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.GetBlobReferenceFromServer("ab");
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                CloudAppendBlob appendBlobSnapshot = ((CloudAppendBlob)blob).CreateSnapshot();
                blob.SetProperties();
                Uri appendBlobSnapshotUri = new Uri(appendBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + appendBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                blob = container.ServiceClient.GetBlobReferenceFromServer(appendBlobSnapshotUri);
                AssertAreEqual(appendBlobSnapshot.Properties, blob.Properties);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlobSnapshot.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(blockBlob.Uri);
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(pageBlob.Uri);
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(appendBlob.Uri);
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(blockBlob.StorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServer(pageBlob.StorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServer(appendBlob.StorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                string blockBlobToken = blockBlob.GetSharedAccessSignature(policy);
                StorageCredentials blockBlobSAS = new StorageCredentials(blockBlobToken);
                Uri blockBlobSASUri = blockBlobSAS.TransformUri(blockBlob.Uri);
                StorageUri blockBlobSASStorageUri = blockBlobSAS.TransformUri(blockBlob.StorageUri);

                string pageBlobToken = pageBlob.GetSharedAccessSignature(policy);
                StorageCredentials pageBlobSAS = new StorageCredentials(pageBlobToken);
                Uri pageBlobSASUri = pageBlobSAS.TransformUri(pageBlob.Uri);
                StorageUri pageBlobSASStorageUri = pageBlobSAS.TransformUri(pageBlob.StorageUri);

                string appendBlobToken = appendBlob.GetSharedAccessSignature(policy);
                StorageCredentials appendBlobSAS = new StorageCredentials(appendBlobToken);
                Uri appendBlobSASUri = appendBlobSAS.TransformUri(appendBlob.Uri);
                StorageUri appendBlobSASStorageUri = appendBlobSAS.TransformUri(appendBlob.StorageUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(blockBlobSASUri);
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(pageBlobSASUri);
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(appendBlobSASUri);
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServer(blockBlobSASStorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServer(pageBlobSASStorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServer(appendBlobSASStorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                client = new CloudBlobClient(container.ServiceClient.BaseUri, blockBlobSAS);
                blob = client.GetBlobReferenceFromServer(blockBlobSASUri);
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                client = new CloudBlobClient(container.ServiceClient.BaseUri, pageBlobSAS);
                blob = client.GetBlobReferenceFromServer(pageBlobSASUri);
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                client = new CloudBlobClient(container.ServiceClient.BaseUri, appendBlobSAS);
                blob = client.GetBlobReferenceFromServer(appendBlobSASUri);
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                client = new CloudBlobClient(container.ServiceClient.StorageUri, blockBlobSAS);
                blob = client.GetBlobReferenceFromServer(blockBlobSASStorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                client = new CloudBlobClient(container.ServiceClient.StorageUri, pageBlobSAS);
                blob = client.GetBlobReferenceFromServer(pageBlobSASStorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                client = new CloudBlobClient(container.ServiceClient.StorageUri, appendBlobSAS);
                blob = client.GetBlobReferenceFromServer(appendBlobSASStorageUri);
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetBlobReferenceFromServerAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                blockBlob.PutBlockList(new string[] { });

                CloudPageBlob pageBlob = container.GetPageBlobReference("pb");
                pageBlob.Create(0);

                CloudAppendBlob appendBlob = container.GetAppendBlobReference("ab");
                appendBlob.CreateOrReplace();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudBlobClient client;
                    ICloudBlob blob;
                    IAsyncResult result;

                    result = container.BeginGetBlobReferenceFromServer("bb",
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                    CloudBlockBlob blockBlobSnapshot = ((CloudBlockBlob)blob).CreateSnapshot();
                    blob.SetProperties();
                    Uri blockBlobSnapshotUri = new Uri(blockBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + blockBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlobSnapshotUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    AssertAreEqual(blockBlobSnapshot.Properties, blob.Properties);
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlobSnapshot.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.BeginGetBlobReferenceFromServer("pb",
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                    CloudPageBlob pageBlobSnapshot = ((CloudPageBlob)blob).CreateSnapshot();
                    blob.SetProperties();
                    Uri pageBlobSnapshotUri = new Uri(pageBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + pageBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlobSnapshotUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    AssertAreEqual(pageBlobSnapshot.Properties, blob.Properties);
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlobSnapshot.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.BeginGetBlobReferenceFromServer("ab",
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                    CloudAppendBlob appendBlobSnapshot = ((CloudAppendBlob)blob).CreateSnapshot();
                    blob.SetProperties();
                    Uri appendBlobSnapshotUri = new Uri(appendBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + appendBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlobSnapshotUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    AssertAreEqual(appendBlobSnapshot.Properties, blob.Properties);
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlobSnapshot.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlob.Uri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlob.Uri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlob.Uri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlob.StorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlob.StorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlob.StorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                    string blockBlobToken = blockBlob.GetSharedAccessSignature(policy);
                    StorageCredentials blockBlobSAS = new StorageCredentials(blockBlobToken);
                    Uri blockBlobSASUri = blockBlobSAS.TransformUri(blockBlob.Uri);
                    StorageUri blockBlobSASStorageUri = blockBlobSAS.TransformUri(blockBlob.StorageUri);

                    string pageBlobToken = pageBlob.GetSharedAccessSignature(policy);
                    StorageCredentials pageBlobSAS = new StorageCredentials(pageBlobToken);
                    Uri pageBlobSASUri = pageBlobSAS.TransformUri(pageBlob.Uri);
                    StorageUri pageBlobSASStorageUri = pageBlobSAS.TransformUri(pageBlob.StorageUri);

                    string appendBlobToken = appendBlob.GetSharedAccessSignature(policy);
                    StorageCredentials appendBlobSAS = new StorageCredentials(appendBlobToken);
                    Uri appendBlobSASUri = appendBlobSAS.TransformUri(appendBlob.Uri);
                    StorageUri appendBlobSASStorageUri = appendBlobSAS.TransformUri(appendBlob.StorageUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlobSASUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlobSASUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlobSASUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlobSASStorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlobSASStorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlobSASStorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                    client = new CloudBlobClient(container.ServiceClient.BaseUri, blockBlobSAS);
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlobSASUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    client = new CloudBlobClient(container.ServiceClient.BaseUri, pageBlobSAS);
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlobSASUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    client = new CloudBlobClient(container.ServiceClient.BaseUri, appendBlobSAS);
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlobSASUri,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                    Assert.IsNull(blob.StorageUri.SecondaryUri);

                    client = new CloudBlobClient(container.ServiceClient.StorageUri, blockBlobSAS);
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(blockBlobSASStorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                    client = new CloudBlobClient(container.ServiceClient.StorageUri, pageBlobSAS);
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(pageBlobSASStorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                    client = new CloudBlobClient(container.ServiceClient.StorageUri, appendBlobSAS);
                    result = container.ServiceClient.BeginGetBlobReferenceFromServer(appendBlobSASStorageUri, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob = container.EndGetBlobReferenceFromServer(result);
                    Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                    Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetBlobReferenceFromServerTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                blockBlob.PutBlockList(new string[] { });

                CloudPageBlob pageBlob = container.GetPageBlobReference("pb");
                pageBlob.Create(0);

                CloudAppendBlob appendBlob = container.GetAppendBlobReference("ab");
                appendBlob.CreateOrReplace();

                CloudBlobClient client;
                ICloudBlob blob;

                blob = container.GetBlobReferenceFromServerAsync("bb").Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                CloudBlockBlob blockBlobSnapshot = ((CloudBlockBlob)blob).CreateSnapshot();
                blob.SetProperties();
                Uri blockBlobSnapshotUri = new Uri(blockBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + blockBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(blockBlobSnapshotUri).Result;
                AssertAreEqual(blockBlobSnapshot.Properties, blob.Properties);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlobSnapshot.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.GetBlobReferenceFromServerAsync("pb").Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                CloudPageBlob pageBlobSnapshot = ((CloudPageBlob)blob).CreateSnapshot();
                blob.SetProperties();
                Uri pageBlobSnapshotUri = new Uri(pageBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + pageBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(pageBlobSnapshotUri).Result;
                AssertAreEqual(pageBlobSnapshot.Properties, blob.Properties);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlobSnapshot.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.GetBlobReferenceFromServerAsync("ab").Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                CloudAppendBlob appendBlobSnapshot = ((CloudAppendBlob)blob).CreateSnapshot();
                blob.SetProperties();
                Uri appendBlobSnapshotUri = new Uri(appendBlobSnapshot.Uri.AbsoluteUri + "?snapshot=" + appendBlobSnapshot.SnapshotTime.Value.UtcDateTime.ToString("o"));
                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(appendBlobSnapshotUri).Result;
                AssertAreEqual(appendBlobSnapshot.Properties, blob.Properties);
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlobSnapshot.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(blockBlob.Uri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(pageBlob.Uri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(appendBlob.Uri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(blockBlob.StorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(pageBlob.StorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(appendBlob.StorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                string blockBlobToken = blockBlob.GetSharedAccessSignature(policy);
                StorageCredentials blockBlobSAS = new StorageCredentials(blockBlobToken);
                Uri blockBlobSASUri = blockBlobSAS.TransformUri(blockBlob.Uri);
                StorageUri blockBlobSASStorageUri = blockBlobSAS.TransformUri(blockBlob.StorageUri);

                string pageBlobToken = pageBlob.GetSharedAccessSignature(policy);
                StorageCredentials pageBlobSAS = new StorageCredentials(pageBlobToken);
                Uri pageBlobSASUri = pageBlobSAS.TransformUri(pageBlob.Uri);
                StorageUri pageBlobSASStorageUri = pageBlobSAS.TransformUri(pageBlob.StorageUri);

                string appendBlobToken = appendBlob.GetSharedAccessSignature(policy);
                StorageCredentials appendBlobSAS = new StorageCredentials(appendBlobToken);
                Uri appendBlobSASUri = appendBlobSAS.TransformUri(appendBlob.Uri);
                StorageUri appendBlobSASStorageUri = appendBlobSAS.TransformUri(appendBlob.StorageUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(blockBlobSASUri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(pageBlobSASUri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(appendBlobSASUri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(blockBlobSASStorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(pageBlobSASStorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                blob = container.ServiceClient.GetBlobReferenceFromServerAsync(appendBlobSASStorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));

                client = new CloudBlobClient(container.ServiceClient.BaseUri, blockBlobSAS);
                blob = client.GetBlobReferenceFromServerAsync(blockBlobSASUri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                client = new CloudBlobClient(container.ServiceClient.BaseUri, pageBlobSAS);
                blob = client.GetBlobReferenceFromServerAsync(pageBlobSASUri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(pageBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                client = new CloudBlobClient(container.ServiceClient.BaseUri, appendBlobSAS);
                blob = client.GetBlobReferenceFromServerAsync(appendBlobSASUri).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.PrimaryUri.Equals(appendBlob.Uri));
                Assert.IsNull(blob.StorageUri.SecondaryUri);

                client = new CloudBlobClient(container.ServiceClient.StorageUri, blockBlobSAS);
                blob = client.GetBlobReferenceFromServerAsync(blockBlobSASStorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudBlockBlob));
                Assert.IsTrue(blob.StorageUri.Equals(blockBlob.StorageUri));

                client = new CloudBlobClient(container.ServiceClient.StorageUri, pageBlobSAS);
                blob = client.GetBlobReferenceFromServerAsync(pageBlobSASStorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudPageBlob));
                Assert.IsTrue(blob.StorageUri.Equals(pageBlob.StorageUri));

                client = new CloudBlobClient(container.ServiceClient.StorageUri, appendBlobSAS);
                blob = client.GetBlobReferenceFromServerAsync(appendBlobSASStorageUri, null, null, null).Result;
                Assert.IsInstanceOfType(blob, typeof(CloudAppendBlob));
                Assert.IsTrue(blob.StorageUri.Equals(appendBlob.StorageUri));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetBlobReferenceFromServerBlobNameCancellationTokenTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                string blobName = "myblob";
                CancellationToken cancellationToken = CancellationToken.None;

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                blockBlob.PutBlockListAsync(new string[0]).Wait();

                ICloudBlob actual = container.GetBlobReferenceFromServerAsync(blobName, cancellationToken).Result;

                Assert.IsInstanceOfType(actual, typeof(CloudBlockBlob));
                Assert.AreEqual(BlobType.BlockBlob, ((CloudBlockBlob)actual).BlobType);
                Assert.AreEqual(blobName, ((CloudBlockBlob)actual).Name);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetBlobReferenceFromServerBlobNameAccessConditionRequestOptionsOperationContextTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                string blobName = "myblob";
                AccessCondition accessCondition = new AccessCondition();
                accessCondition.IfModifiedSinceTime = DateTime.UtcNow.AddDays(-1.0);
                BlobRequestOptions requestOptions = new BlobRequestOptions();
                OperationContext operationContext = new OperationContext();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                blockBlob.PutBlockListAsync(new string[0]).Wait();

                ICloudBlob actual = container.GetBlobReferenceFromServerAsync(blobName, accessCondition, requestOptions, operationContext).Result;

                Assert.IsInstanceOfType(actual, typeof(CloudBlockBlob));
                Assert.AreEqual(BlobType.BlockBlob, ((CloudBlockBlob)actual).BlobType);
                Assert.AreEqual(blobName, ((CloudBlockBlob)actual).Name);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        class MockProxy : IWebProxy
        {
            private WebProxy webProxy;

            public MockProxy(Uri address, NetworkCredential credentials)
            {
                this.webProxy = new WebProxy { Address = address, Credentials = credentials };
            }

            public class MemberAccessEventArgs : EventArgs
            {
                public MemberAccessEventArgs(string memberName, object value)
                {
                    this.MemberName = memberName;
                    this.Value = value;
                }

                public string MemberName { get; private set; }
                public object Value { get; private set; }
            }

            public event EventHandler<MemberAccessEventArgs> MemberAccess
            {
                add
                {
                    memberAccess += value;
                }

                remove
                {
                    memberAccess -= value;
                }
            }

            EventHandler<MemberAccessEventArgs> memberAccess;

            private void OnMemberAccess(string memberName, object value)
            {
                EventHandler<MemberAccessEventArgs> h = this.memberAccess;

                if (h != null)
                {
                    h(this, new MemberAccessEventArgs(memberName, value));
                }
            }

            public ICredentials Credentials
            {
                get
                {
                    ICredentials value = this.webProxy.Credentials;
                    this.OnMemberAccess("Credentials", value);
                    return value;
                }

                set
                {
                    this.webProxy.Credentials = value;
                }
            }

            public Uri GetProxy(Uri destination)
            {
                this.OnMemberAccess("GetProxy", destination);
                return this.webProxy.GetProxy(destination);
            }

            public bool IsBypassed(Uri host)
            {
                this.OnMemberAccess("IsBypassed", host);
                return this.webProxy.IsBypassed(host);
            }
        }

        [TestMethod]
        [Description("Verify that a proxy gets used")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerVerifyProxyHit()
        {
            CloudBlobContainer container = null;

            try
            {
                const string proxyAddress = "http://127.0.0.1";
                const string proxyUser = "user";
                const string proxyPassword = "password";

                CancellationTokenSource cts = new CancellationTokenSource();

                bool proxyHit = false;

                MockProxy mockProxy =
                    new MockProxy(
                        new Uri(proxyAddress),
                        new NetworkCredential(proxyUser, proxyPassword)
                        );

                mockProxy.MemberAccess += (s, e) =>
                {
                    cts.Cancel();
                    proxyHit = true;
                };

                DelegatingHandlerImpl delegatingHandlerImpl = new DelegatingHandlerImpl(mockProxy);

                container = GetRandomContainerReference(delegatingHandlerImpl);

                OperationContext operationContext = new OperationContext();

                try
                {
                    await container.CreateAsync(BlobContainerPublicAccessType.Off, default(BlobRequestOptions), operationContext, cts.Token);
                }
                catch (StorageException)
                {
                    // expected, but not required
                }

                Assert.IsTrue(proxyHit, "Proxy not hit");
            }
            finally
            {
                container?.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that a proxy doesn't interfere")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [Ignore]
        public async Task CloudBlobContainerCreateWithProxy()
        {
            //CloudBlobContainer container = GetRandomContainerReference();

            //try
            //{
            //    const string proxyAddress = "http://localhost:8877"; // HttpMangler's proxy address
            //    const string proxyUser = "user";
            //    const string proxyPassword = "password";

            //    var cts = new CancellationTokenSource();

            //    var proxyHit = false;

            //    var mockProxy =
            //        new MockProxy(
            //            new Uri(proxyAddress),
            //            new NetworkCredential(proxyUser, proxyPassword)
            //            );

            //    mockProxy.MemberAccess += (s, e) =>
            //    {
            //        proxyHit = true;
            //    };

            //    OperationContext operationContext = new OperationContext()
            //    {
            //        Proxy = mockProxy
            //    };

            //    using (new Test.Network.HttpMangler())
            //    {
            //        await container.CreateAsync(BlobContainerPublicAccessType.Off, default(BlobRequestOptions), operationContext, cts.Token);
            //    }

            //    // if we get here without an exception, assume the call was 
            //    // successful and verify that the proxy was used
            //    Assert.IsTrue(proxyHit, "Proxy not hit");
            //}
            //finally
            //{
            //    container.DeleteIfExistsAsync().Wait();
            //}
        }

        [TestMethod]
        [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetBlobReferenceFromServerBlobNameAccessConditionRequestOptionsOperationContextCancellationTokenTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                string blobName = "myblob";
                AccessCondition accessCondition = new AccessCondition();
                accessCondition.IfModifiedSinceTime = DateTime.UtcNow.AddDays(-1.0);
                BlobRequestOptions requestOptions = new BlobRequestOptions();
                OperationContext operationContext = new OperationContext();
                CancellationToken cancellationToken = CancellationToken.None;

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                blockBlob.PutBlockListAsync(new string[0]).Wait();

                ICloudBlob actual = container.GetBlobReferenceFromServerAsync(blobName, accessCondition, requestOptions, operationContext, cancellationToken).Result;

                Assert.IsInstanceOfType(actual, typeof(CloudBlockBlob));
                Assert.AreEqual(BlobType.BlockBlob, ((CloudBlockBlob)actual).BlobType);
                Assert.AreEqual(blobName, ((CloudBlockBlob)actual).Name);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Test conditional access on a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerConditionalAccess()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                container.FetchAttributes();

                string currentETag = container.Properties.ETag;
                DateTimeOffset currentModifiedTime = container.Properties.LastModified.Value;

                // ETag conditional tests
                container.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                container.SetMetadata();

                container.FetchAttributes();
                string newETag = container.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                // LastModifiedTime tests
                currentModifiedTime = container.Properties.LastModified.Value;

                container.Metadata["DateConditionalName"] = "DateConditionalValue";

                TestHelper.ExpectedException(
                    () => container.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null),
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                container.Metadata["DateConditionalName"] = "DateConditionalValue2";
                currentETag = container.Properties.ETag;

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                container.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                container.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                container.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                container.FetchAttributes();
                newETag = container.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test to ensure CreateIfNotExists/DeleteIfNotExists succeeds with write-only Account SAS permissions - SYNC")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateAndDeleteWithWriteOnlySync()
        {
            CloudBlobContainer blobContainerWithSAS = GenerateRandomWriteOnlyBlobContainer();
            try 
            {
                Assert.IsFalse(blobContainerWithSAS.DeleteIfExists());
                Assert.IsTrue(blobContainerWithSAS.CreateIfNotExists());
                Assert.IsFalse(blobContainerWithSAS.CreateIfNotExists());
                Assert.IsTrue(blobContainerWithSAS.DeleteIfExists());
                Assert.IsTrue(blobContainerWithSAS.DeleteIfExists());
            }
            finally
            {
                blobContainerWithSAS.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test to ensure CreateIfNotExists/DeleteIfNotExists succeeds with write-only Account SAS permissions - APM ")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateAndDeleteWithWriteOnlyPermissionsAPM()
        {
            CloudBlobContainer blobContainerWithSAS = GenerateRandomWriteOnlyBlobContainer();
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    result = blobContainerWithSAS.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blobContainerWithSAS.EndDeleteIfExists(result));

                    result = blobContainerWithSAS.BeginCreateIfNotExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blobContainerWithSAS.EndCreateIfNotExists(result));

                    result = blobContainerWithSAS.BeginCreateIfNotExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blobContainerWithSAS.EndCreateIfNotExists(result));

                    result = blobContainerWithSAS.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blobContainerWithSAS.EndDeleteIfExists(result));

                    result = blobContainerWithSAS.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blobContainerWithSAS.EndDeleteIfExists(result));
                }
            }
            finally
            {
                blobContainerWithSAS.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Test to ensure CreateIfNotExists/DeleteIfNotExists succeeds with write-only Account SAS permissions - TASK ")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerCreateAndDeleteWithWriteOnlyAccountSAS()
        {
            CloudBlobContainer blobContainerWithSAS = GenerateRandomWriteOnlyBlobContainer();
            try
            {
                Assert.IsFalse(blobContainerWithSAS.DeleteIfExistsAsync().Result);
                Assert.IsTrue(blobContainerWithSAS.CreateIfNotExistsAsync().Result);
                Assert.IsFalse(blobContainerWithSAS.CreateIfNotExistsAsync().Result);
                Assert.IsTrue(blobContainerWithSAS.DeleteIfExistsAsync().Result);
                Assert.IsTrue(blobContainerWithSAS.DeleteIfExistsAsync().Result);
            }
            finally
            {
                blobContainerWithSAS.DeleteIfExists();
            }
        }
#endif

        private void ValidateWebContainer(CloudBlobContainer webContainer)
        {
            CloudBlockBlob blob0 = webContainer.GetBlockBlobReference("blob");
            blob0.Properties.ContentType = @"multipart/form-data; boundary=thingz";  // Content-type is important for the $web container
            CloudBlockBlob blob1 = webContainer.GetBlockBlobReference("blob/abcd");
            blob1.Properties.ContentType = @"image/gif";
            CloudBlockBlob blob2 = webContainer.GetBlockBlobReference("blob/other.html");
            blob2.Properties.ContentType = @"text/html; charset=utf-8";

            List<CloudBlockBlob> expectedBlobs = new List<CloudBlockBlob> { blob0, blob1, blob2 };
            List<string> texts = new List<string> { "blob0text", "blbo1text", "blob2text" };
            for (int i = 0; i < 3; i++)
            {
                expectedBlobs[i].UploadText(texts[i]);
            }

            List<CloudBlob> blobs = webContainer.ListBlobs(useFlatBlobListing: true, blobListingDetails: BlobListingDetails.All).Select(blob => (CloudBlob)blob).ToList();
            Assert.AreEqual(expectedBlobs.Count, blobs.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(expectedBlobs[i].Name, blobs[i].Name);
                Assert.AreEqual(expectedBlobs[i].Properties.ContentType, blobs[i].Properties.ContentType);
                Assert.AreEqual(texts[i], ((CloudBlockBlob)blobs[i]).DownloadText());
            }
        }

        [TestMethod]
        [Description("Test to ensure container operations work on the $web container.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerWebContainerOperations()
        {
            // Test operations with shard key
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer webContainer = blobClient.GetContainerReference("$web");
            try
            {
                webContainer.DeleteIfExists();
                Assert.IsFalse(webContainer.Exists());
                TestHelper.SpinUpToNSecondsIgnoringFailures(() => webContainer.Create(), 120);
                Assert.IsTrue(webContainer.Exists());
                Assert.IsTrue(blobClient.ListContainers("$").Any(container => container.Name == webContainer.Name));

                ValidateWebContainer(webContainer);

                // Clear out the old data, faster than deleting / re-creating the container.
                foreach (CloudBlob blob in webContainer.ListBlobs(useFlatBlobListing: true))
                {
                    blob.Delete();
                }

                // Test relevant operations with a service SAS.
                string webContainerSAS = webContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy() { SharedAccessExpiryTime = DateTime.Now + TimeSpan.FromDays(30), Permissions = SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Delete | SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Write });
                ValidateWebContainer(new CloudBlobContainer(new Uri(webContainer.Uri + webContainerSAS)));
                webContainer.Delete();
                Assert.IsFalse(blobClient.ListContainers("$").Any(container => container.Name == webContainer.Name));
            }
            finally
            {
                webContainer.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetAccountProperties via Blob container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobContainerGetAccountProperties()
        {
            CloudBlobContainer blobContainerWithSAS = GenerateRandomWriteOnlyBlobContainer();
            try
            {
                blobContainerWithSAS.Create();

                Shared.Protocol.AccountProperties result = blobContainerWithSAS.GetAccountPropertiesAsync().Result;

                Assert.IsNotNull(result);

                Assert.IsNotNull(result.SkuName);

                Assert.IsNotNull(result.AccountKind);
            }
            finally
            {
                blobContainerWithSAS.DeleteIfExists();
            }
        }

        private CloudBlobContainer GenerateRandomWriteOnlyBlobContainer()
        {
            string blobContainerName = "n" + Guid.NewGuid().ToString("N");

            SharedAccessAccountPolicy sasAccountPolicy = new SharedAccessAccountPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                Permissions = SharedAccessAccountPermissions.Write | SharedAccessAccountPermissions.Delete,
                Services = SharedAccessAccountServices.Blob,
                ResourceTypes = SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Container

            };

            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudStorageAccount account = new CloudStorageAccount(blobClient.Credentials, false);
            string accountSASToken = account.GetSharedAccessSignature(sasAccountPolicy);
            StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
            StorageUri storageUri = blobClient.StorageUri;
            CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, storageUri, null, null, null);
            CloudBlobClient blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
            CloudBlobContainer containerWithSAS = blobClientWithSAS.GetContainerReference(blobContainerName);
            return containerWithSAS;
        }
    }
}
