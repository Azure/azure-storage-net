// -----------------------------------------------------------------------------------------
// <copyright file="CloudAppendBlobTest.cs" company="Microsoft">
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
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class CloudAppendBlobTest : BlobTestBase
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
        [Description("Create an append blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobCreateAndDelete()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                Assert.IsTrue(blob.Exists());
                blob.Delete();
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create an append blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobCreateAndDeleteAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                    result = blob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreateOrReplace(result);

                    result = blob.BeginExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blob.EndExists(result));

                    result = blob.BeginDelete(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndDelete(result);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a zero-length append blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobCreateAndDeleteTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();
                Assert.IsTrue(blob.ExistsAsync().Result);
                blob.DeleteAsync().Wait();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Get an append blob reference using its constructor")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobConstructor()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
            CloudAppendBlob blob2 = new CloudAppendBlob(blob.StorageUri, null, null);
            Assert.AreEqual(blob.Name, blob2.Name);
            Assert.AreEqual(blob.StorageUri, blob2.StorageUri);
            Assert.AreEqual(blob.Container.StorageUri, blob2.Container.StorageUri);
            Assert.AreEqual(blob.ServiceClient.StorageUri, blob2.ServiceClient.StorageUri);
            Assert.AreEqual(blob.BlobType, blob2.BlobType);
        }

        [TestMethod]
        [Description("Try to delete a non-existing append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDeleteIfExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                Assert.IsFalse(blob.DeleteIfExists());
                blob.CreateOrReplace();
                Assert.IsTrue(blob.DeleteIfExists());
                Assert.IsFalse(blob.DeleteIfExists());
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDeleteIfExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                    IAsyncResult result = blob.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blob.EndDeleteIfExists(result));
                    result = blob.BeginCreateOrReplace(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreateOrReplace(result);
                    result = blob.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blob.EndDeleteIfExists(result));
                    result = blob.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blob.EndDeleteIfExists(result));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to delete a non-existing append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobDeleteIfExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                Assert.IsFalse(blob.DeleteIfExistsAsync().Result);
                blob.CreateOrReplaceAsync().Wait();
                Assert.IsTrue(blob.DeleteIfExistsAsync().Result);
                Assert.IsFalse(blob.DeleteIfExistsAsync().Result);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Check a blob's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");

                Assert.IsFalse(blob2.Exists());

                blob.CreateOrReplace();

                Assert.IsTrue(blob2.Exists());
                Assert.AreEqual(0, blob2.Properties.Length);

                blob.Delete();

                Assert.IsFalse(blob2.Exists());
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Check a blob's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = blob2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blob2.EndExists(result));

                    blob.CreateOrReplace();

                    result = blob2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blob2.EndExists(result));
                    Assert.AreEqual(0, blob2.Properties.Length);

                    blob.Delete();

                    result = blob2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blob2.EndExists(result));
                }
            }
            finally
            {
                container.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Check a blob's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();

            try
            {
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");

                Assert.IsFalse(blob2.ExistsAsync().Result);

                blob.CreateOrReplaceAsync().Wait();

                Assert.IsTrue(blob2.ExistsAsync().Result);
                Assert.AreEqual(0, blob2.Properties.Length);

                blob.DeleteAsync().Wait();

                Assert.IsFalse(blob2.ExistsAsync().Result);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Verify the attributes of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobFetchAttributes()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                Assert.AreEqual(0, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentDisposition);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.AppendBlob, blob.Properties.BlobType);

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(0, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                Assert.IsNull(blob2.Properties.CacheControl);
                Assert.IsNull(blob2.Properties.ContentDisposition);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.AppendBlob, blob2.Properties.BlobType);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobFetchAttributesAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                    IAsyncResult result = blob.BeginCreateOrReplace(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreateOrReplace(result);
                    Assert.AreEqual(0, blob.Properties.Length);
                    Assert.IsNotNull(blob.Properties.ETag);
                    Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                    Assert.IsNull(blob.Properties.CacheControl);
                    Assert.IsNull(blob.Properties.ContentDisposition);
                    Assert.IsNull(blob.Properties.ContentEncoding);
                    Assert.IsNull(blob.Properties.ContentLanguage);
                    Assert.IsNull(blob.Properties.ContentType);
                    Assert.IsNull(blob.Properties.ContentMD5);
                    Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                    Assert.AreEqual(BlobType.AppendBlob, blob.Properties.BlobType);

                    CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(0, blob2.Properties.Length);
                    Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                    Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                    Assert.IsNull(blob2.Properties.CacheControl);
                    Assert.IsNull(blob2.Properties.ContentDisposition);
                    Assert.IsNull(blob2.Properties.ContentEncoding);
                    Assert.IsNull(blob2.Properties.ContentLanguage);
                    Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                    Assert.IsNull(blob2.Properties.ContentMD5);
                    Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                    Assert.AreEqual(BlobType.AppendBlob, blob2.Properties.BlobType);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify the attributes of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobFetchAttributesTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();
                Assert.AreEqual(0, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.AppendBlob, blob.Properties.BlobType);

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                Assert.IsNull(blob2.Properties.CacheControl);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.AppendBlob, blob2.Properties.BlobType);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Verify setting the properties of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSetProperties()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                string eTag = blob.Properties.ETag;
                DateTimeOffset lastModified = blob.Properties.LastModified.Value;

                Thread.Sleep(1000);

                blob.Properties.CacheControl = "no-transform";
                blob.Properties.ContentDisposition = "attachment";
                blob.Properties.ContentEncoding = "gzip";
                blob.Properties.ContentLanguage = "tr,en";
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.Properties.ContentType = "text/html";
                blob.SetProperties();
                Assert.IsTrue(blob.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, blob.Properties.ETag);

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("attachment", blob2.Properties.ContentDisposition);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudAppendBlob blob3 = container.GetAppendBlobReference("blob1");
                using (MemoryStream stream = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    blob3.DownloadToStream(stream, null, options);
                }
                AssertAreEqual(blob2.Properties, blob3.Properties);

                CloudAppendBlob blob4 = (CloudAppendBlob)container.ListBlobs().First();
                AssertAreEqual(blob2.Properties, blob4.Properties);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSetPropertiesAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                    IAsyncResult result = blob.BeginCreateOrReplace(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreateOrReplace(result);
                    string eTag = blob.Properties.ETag;
                    DateTimeOffset lastModified = blob.Properties.LastModified.Value;

                    Thread.Sleep(1000);

                    blob.Properties.CacheControl = "no-transform";
                    blob.Properties.ContentDisposition = "attachment";
                    blob.Properties.ContentEncoding = "gzip";
                    blob.Properties.ContentLanguage = "tr,en";
                    blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    blob.Properties.ContentType = "text/html";
                    result = blob.BeginSetProperties(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetProperties(result);
                    Assert.IsTrue(blob.Properties.LastModified > lastModified);
                    Assert.AreNotEqual(eTag, blob.Properties.ETag);

                    CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                    Assert.AreEqual("attachment", blob2.Properties.ContentDisposition);
                    Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                    Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                    Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                    Assert.AreEqual("text/html", blob2.Properties.ContentType);

                    CloudAppendBlob blob3 = container.GetAppendBlobReference("blob1");
                    using (MemoryStream stream = new MemoryStream())
                    {
                        BlobRequestOptions options = new BlobRequestOptions()
                        {
                            DisableContentMD5Validation = true,
                        };
                        result = blob3.BeginDownloadToStream(stream, null, options, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob3.EndDownloadToStream(result);
                    }
                    AssertAreEqual(blob2.Properties, blob3.Properties);

                    result = container.BeginListBlobsSegmented(null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    BlobResultSegment results = container.EndListBlobsSegmented(result);
                    CloudAppendBlob blob4 = (CloudAppendBlob)results.Results.First();
                    AssertAreEqual(blob2.Properties, blob4.Properties);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify setting the properties of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSetPropertiesTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();
                string eTag = blob.Properties.ETag;
                DateTimeOffset lastModified = blob.Properties.LastModified.Value;

                Thread.Sleep(1000);

                blob.Properties.CacheControl = "no-transform";
                blob.Properties.ContentEncoding = "gzip";
                blob.Properties.ContentLanguage = "tr,en";
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.Properties.ContentType = "text/html";
                blob.SetPropertiesAsync().Wait();
                Assert.IsTrue(blob.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, blob.Properties.ETag);

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudAppendBlob blob3 = container.GetAppendBlobReference("blob1");
                using (MemoryStream stream = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    blob3.DownloadToStreamAsync(stream, null, options, null).Wait();
                }
                AssertAreEqual(blob2.Properties, blob3.Properties);

                CloudAppendBlob blob4 = (CloudAppendBlob)container.ListBlobsSegmentedAsync(null).Result.Results.First();
                AssertAreEqual(blob2.Properties, blob4.Properties);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Try retrieving properties of an append blob using a block blob reference")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobFetchAttributesInvalidType()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob1");
                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => blob2.FetchAttributes(),
                    "Fetching attributes of an append blob using block blob reference should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that creating an append blob can also set its metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobCreateWithMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.Metadata["key1"] = "value1";
                blob.CreateOrReplace();

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that empty metadata on an append blob can be retrieved.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobGetEmptyMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                blob.Metadata["key1"] = "value1";

                OperationContext context = new OperationContext();
                context.SendingRequest += (sender, e) =>
                {
                    e.Request.Headers["x-ms-meta-key1"] = string.Empty;
                };

                blob.SetMetadata(operationContext: context);
                blob2.FetchAttributes();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual(string.Empty, blob2.Metadata["key1"]);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that an append blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSetMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(0, blob2.Metadata.Count);

                blob.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetMetadata(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "";
                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetMetadata(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = " ";
                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetMetadata(),
                    "Metadata keys should have a non-whitespace only value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "value1";
                blob.SetMetadata();

                blob2.FetchAttributes();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);

                CloudAppendBlob blob3 = (CloudAppendBlob)container.ListBlobs(null, true, BlobListingDetails.Metadata, null, null).First();
                Assert.AreEqual(1, blob3.Metadata.Count);
                Assert.AreEqual("value1", blob3.Metadata["key1"]);

                blob.Metadata.Clear();
                blob.SetMetadata();

                blob2.FetchAttributes();
                Assert.AreEqual(0, blob2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that an append blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSetMetadataAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                    IAsyncResult result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(0, blob2.Metadata.Count);

                    blob.Metadata["key1"] = null;
                    result = blob.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Exception e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetMetadata(result),
                        "Metadata keys should have a non-null value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    blob.Metadata["key1"] = "";
                    result = blob.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetMetadata(result),
                        "Metadata keys should have a non-empty value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    blob.Metadata["key1"] = " ";
                    result = blob.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetMetadata(result),
                        "Metadata keys should have a non-whitespace only value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    blob.Metadata["key1"] = "value1";
                    result = blob.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetMetadata(result);

                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(1, blob2.Metadata.Count);
                    Assert.AreEqual("value1", blob2.Metadata["key1"]);

                    result = container.BeginListBlobsSegmented(null, true, BlobListingDetails.Metadata, null, null, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    BlobResultSegment results = container.EndListBlobsSegmented(result);
                    CloudAppendBlob blob3 = (CloudAppendBlob)results.Results.First();
                    Assert.AreEqual(1, blob3.Metadata.Count);
                    Assert.AreEqual("value1", blob3.Metadata["key1"]);

                    blob.Metadata.Clear();
                    result = blob.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetMetadata(result);

                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(0, blob2.Metadata.Count);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify that a append blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSetMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();

                CloudAppendBlob blob2 = container.GetAppendBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, blob2.Metadata.Count);

                blob.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetMetadataAsync(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "";
                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetMetadataAsync(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = " ";
                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetMetadataAsync(),
                    "Metadata keys should have a non-whitespace only value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "value1";
                blob.SetMetadataAsync().Wait();

                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);

                CloudAppendBlob blob3 =
                    (CloudAppendBlob)
                    container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.Metadata, null, null, null, null)
                             .Result
                             .Results
                             .First();

                Assert.AreEqual(1, blob3.Metadata.Count);
                Assert.AreEqual("value1", blob3.Metadata["key1"]);

                blob.Metadata.Clear();
                blob.SetMetadataAsync().Wait();

                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, blob2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlock()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                this.CloudAppendBlock(container, 6 * 512, null, 0, false);
                this.CloudAppendBlock(container, 6 * 512, null, 1024, false);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlockAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                this.CloudAppendBlock(container, 2 * 1024, null, 0, true);
                this.CloudAppendBlock(container, 2 * 1024, null, 1024, true);
            }
            finally
            {
                container.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendBlockTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();
            try
            {
                this.CloudAppendBlockTask(container, 2 * 1024, null, 0);
                this.CloudAppendBlockTask(container, 2 * 1024, null, 1024);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }
#endif

        private void CloudAppendBlock(CloudBlobContainer container, int size, AccessCondition accessCondition, int startOffset, bool isAsync)
        {
            byte[] buffer = GetRandomBuffer(size);

            CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
            blob.CreateOrReplace();

            using (MemoryStream originalBlob = new MemoryStream())
            {
                originalBlob.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        UseTransactionalMD5 = true,
                    };
                    if (isAsync)
                    {
                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {

                                ICancellableAsyncResult result = blob.BeginAppendBlock(
                                    sourceStream, null, accessCondition, options, null, ar => waitHandle.Set(), null);
                                waitHandle.WaitOne();
                                blob.EndAppendBlock(result);
                        }
                    }
                    else
                    {
                            blob.AppendBlock(sourceStream, null, accessCondition, options);
                    }
                }

                blob.FetchAttributes();

                using (MemoryStream downloadedBlob = new MemoryStream())
                {
                    if (isAsync)
                    {
                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {
                            ICancellableAsyncResult result = blob.BeginDownloadToStream(downloadedBlob,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blob.EndDownloadToStream(result);
                        }
                    }
                    else
                    {
                        blob.DownloadToStream(downloadedBlob);
                    }

                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalBlob,
                        downloadedBlob,
                        0,
                        0,
                        (int)originalBlob.Length);
                }
            }
        }

#if TASK
        private void CloudAppendBlockTask(CloudBlobContainer container, int size, AccessCondition accessCondition, int startOffset)
        {
            try
            {
                byte[] buffer = GetRandomBuffer(size);

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();

                using (MemoryStream originalBlob = new MemoryStream())
                {
                    originalBlob.Write(buffer, startOffset, buffer.Length - startOffset);

                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        sourceStream.Seek(startOffset, SeekOrigin.Begin);
                        BlobRequestOptions options = new BlobRequestOptions()
                        {
                            UseTransactionalMD5 = true,
                        };

                        blob.AppendBlockAsync(sourceStream, null, accessCondition, options, null).Wait();
                    }

                    blob.FetchAttributesAsync().Wait();
               
                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStreamAsync(downloadedBlob).Wait();

                        TestHelper.AssertStreamsAreEqualAtIndex(
                            originalBlob,
                            downloadedBlob,
                            0,
                            0,
                            (int)originalBlob.Length);
                    }
                }
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
#endif

        [TestMethod]
        [Description("Validate UploadFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateUploadFromStream()
        {
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");

                blob.UploadFromStream(new MemoryStream(buffer));
                blob.FetchAttributes();
                Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                blob.UploadFromStream(new MemoryStream(buffer));
                blob.FetchAttributes();
                Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                blob.UploadFromStream(new MemoryStream(buffer), null /* accessCondition */, null /* options */, null /* operationContext */);
                blob.FetchAttributes();
                Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate UploadFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateUploadFromStreamAPM()
        {
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream memStream = new MemoryStream(buffer))
                    {
                        result = blob.BeginUploadFromStream(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                        memStream.Seek(0, SeekOrigin.Begin);

                        result = blob.BeginUploadFromStream(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                        memStream.Seek(0, SeekOrigin.Begin);

                        result = blob.BeginUploadFromStream(
                                    memStream, null /* accessCondition */, null /* options */, null /* operationContext */, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateAppendFromStream()
        {
            // Every time append a buffer that is bigger than a single block.
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                blob.AppendFromStream(new MemoryStream(buffer));
                blob.FetchAttributes();
                Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                blob.AppendFromStream(new MemoryStream(buffer));
                blob.FetchAttributes();
                Assert.AreEqual(12 * 1024 * 1024, blob.Properties.Length);

                blob.AppendFromStream(new MemoryStream(buffer), null /* accessCondition */, null /* options */, null /* operationContext */);
                blob.FetchAttributes();
                Assert.AreEqual(18 * 1024 * 1024, blob.Properties.Length);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateAppendFromStreamWithLength()
        {
            // Every time append a buffer that is bigger than a single block.
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                blob.AppendFromStream(new MemoryStream(buffer), 5 * 1024 * 1024);
                blob.FetchAttributes();
                Assert.AreEqual(5 * 1024 * 1024, blob.Properties.Length);

                blob.AppendFromStream(new MemoryStream(buffer), 5 * 1024 * 1024);
                blob.FetchAttributes();
                Assert.AreEqual(10 * 1024 * 1024, blob.Properties.Length);

                blob.AppendFromStream(new MemoryStream(buffer), 5 * 1024 * 1024, null /* accessCondition */, null /* options */, null /* operationContext */);
                blob.FetchAttributes();
                Assert.AreEqual(15 * 1024 * 1024, blob.Properties.Length);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateAppendFromStreamAPM()
        {
            // Every time append a buffer that is bigger than a single block.
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream memStream = new MemoryStream(buffer))
                    {
                        result = blob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndCreateOrReplace(result);

                        result = blob.BeginAppendFromStream(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndAppendFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                        memStream.Seek(0, SeekOrigin.Begin);

                        result = blob.BeginAppendFromStream(
                                    memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndAppendFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(12 * 1024 * 1024, blob.Properties.Length);

                        memStream.Seek(0, SeekOrigin.Begin);

                        result = blob.BeginAppendFromStream(
                                    memStream, null /* accessCondition */, null /* options */, null /* operationContext */, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndAppendFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(18 * 1024 * 1024, blob.Properties.Length);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateAppendFromStreamWithLengthAPM()
        {
            // Every time append a buffer that is bigger than a single block.
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream memStream = new MemoryStream(buffer))
                    {
                        result = blob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndCreateOrReplace(result);

                        result = blob.BeginAppendFromStream(
                                        memStream, 5 * 1024 * 1024, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndAppendFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(5 * 1024 * 1024, blob.Properties.Length);

                        memStream.Seek(0, SeekOrigin.Begin);

                        result = blob.BeginAppendFromStream(
                                    memStream, 5 * 1024 * 1024, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndAppendFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(10 * 1024 * 1024, blob.Properties.Length);

                        memStream.Seek(0, SeekOrigin.Begin);

                        result = blob.BeginAppendFromStream(
                                    memStream, 5 * 1024 * 1024, null /* accessCondition */, null /* options */, null /* operationContext */, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndAppendFromStream(result);

                        result = blob.BeginFetchAttributes(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndFetchAttributes(result);
                        Assert.AreEqual(15 * 1024 * 1024, blob.Properties.Length);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateAppendFromStreamTask()
        {
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();

                using (MemoryStream memStream = new MemoryStream(buffer))
                {
                    blob.AppendFromStreamAsync(memStream).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    blob.AppendFromStreamAsync(memStream, CancellationToken.None).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(12 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    blob.AppendFromStreamAsync(memStream, null /* accessCondition */, null /* options */, null /* operationContext */).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(18 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    blob.AppendFromStreamAsync(memStream, null /* accessCondition */, null /* options */, null /* operationContext */, CancellationToken.None).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(24 * 1024 * 1024, blob.Properties.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Validate AppendFromStream.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobValidateAppendFromStreamWithLengthTask()
        {
            byte[] buffer = GetRandomBuffer(6 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();

                using (MemoryStream memStream = new MemoryStream(buffer))
                {
                    blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(5 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024, CancellationToken.None).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(10 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024, null /* accessCondition */, null /* options */, null /* operationContext */).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(15 * 1024 * 1024, blob.Properties.Length);

                    memStream.Seek(0, SeekOrigin.Begin);
                    blob.AppendFromStreamAsync(memStream, 5 * 1024 * 1024, null /* accessCondition */, null /* options */, null /* operationContext */, CancellationToken.None).Wait();
                    blob.FetchAttributesAsync().Wait();
                    Assert.AreEqual(20 * 1024 * 1024, blob.Properties.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Verify the append offset returned by the service.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobVerifyAppendOffset()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                long offset = blob.AppendBlock(new MemoryStream(buffer));
                Assert.AreEqual(0, offset);

                offset = blob.AppendBlock(new MemoryStream(buffer));
                Assert.AreEqual(2 * 1024 * 1024, offset);

                offset = blob.AppendBlock(new MemoryStream(buffer));
                Assert.AreEqual(4 * 1024 * 1024, offset);

                offset = blob.AppendBlock(new MemoryStream(buffer));
                Assert.AreEqual(6 * 1024 * 1024, offset);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify the append offset returned by the service.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobVerifyAppendOffsetAPM()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    using (MemoryStream memStream = new MemoryStream(buffer))
                    {
                        result = blob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndCreateOrReplace(result);

                        result = blob.BeginAppendBlock(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        long offset = blob.EndAppendBlock(result);
                        Assert.AreEqual(0, offset);

                        memStream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginAppendBlock(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        offset = blob.EndAppendBlock(result);
                        Assert.AreEqual(2 * 1024 * 1024, offset);

                        memStream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginAppendBlock(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        offset = blob.EndAppendBlock(result);
                        Assert.AreEqual(4 * 1024 * 1024, offset);

                        memStream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginAppendBlock(
                                        memStream, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        offset = blob.EndAppendBlock(result);
                        Assert.AreEqual(6 * 1024 * 1024, offset);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify the append offset returned by the service.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobVerifyAppendOffsetTask()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();

                using (MemoryStream memStream = new MemoryStream(buffer))
                {
                    Assert.AreEqual(0, blob.AppendBlockAsync(new MemoryStream(buffer)).Result);
                    memStream.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(2 * 1024 * 1024, blob.AppendBlockAsync(new MemoryStream(buffer)).Result);
                    memStream.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(4 * 1024 * 1024, blob.AppendBlockAsync(new MemoryStream(buffer)).Result);
                    memStream.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(6 * 1024 * 1024, blob.AppendBlockAsync(new MemoryStream(buffer)).Result);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Upload and download text")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadText()
        {
            this.DoTextUploadDownload("test", false, false);
            this.DoTextUploadDownload("char中文test", true, false);
        }

        [TestMethod]
        [Description("Upload and download text")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadTextAPM()
        {
            this.DoTextUploadDownload("test", false, true);
            this.DoTextUploadDownload("char中文test", true, true);
        }

#if TASK
        [TestMethod]
        [Description("Upload and download text")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadTextTask()
        {
            this.DoTextUploadDownloadTask("test", false);
            this.DoTextUploadDownloadTask("char中文test", true);
        }
#endif

        private void DoTextUploadDownload(string text, bool checkDifferentEncoding, bool isAsync)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateIfNotExists();
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");

                if (isAsync)
                {
                    IAsyncResult result;
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        result = blob.BeginUploadText(text,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadText(result);
                        result = blob.BeginDownloadText(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        Assert.AreEqual(text, blob.EndDownloadText(result));
                        if (checkDifferentEncoding)
                        {
                            result = blob.BeginDownloadText(Encoding.Unicode, null, null, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            Assert.AreNotEqual(text, blob.EndDownloadText(result));
                        }

                        OperationContext context = new OperationContext();
                        result = blob.BeginUploadText(text, Encoding.Unicode, null, null, context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadText(result);

                        // 3 because of Create and Appendblock
                        Assert.AreEqual(2, context.RequestResults.Count);
                        result = blob.BeginDownloadText(Encoding.Unicode, null, null, context,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        Assert.AreEqual(text, blob.EndDownloadText(result));
                        Assert.AreEqual(3, context.RequestResults.Count);
                        if (checkDifferentEncoding)
                        {
                            result = blob.BeginDownloadText(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            Assert.AreNotEqual(text, blob.EndDownloadText(result));
                        }
                    }
                }
                else
                {
                    blob.UploadText(text);
                    Assert.AreEqual(text, blob.DownloadText());
                    if (checkDifferentEncoding)
                    {
                        Assert.AreNotEqual(text, blob.DownloadText(Encoding.Unicode));
                    }

                    blob.UploadText(text, Encoding.Unicode);
                    Assert.AreEqual(text, blob.DownloadText(Encoding.Unicode));
                    if (checkDifferentEncoding)
                    {
                        Assert.AreNotEqual(text, blob.DownloadText());
                    }

                    OperationContext context = new OperationContext();
                    blob.UploadText(text, Encoding.Unicode, null, null, context);

                    // 3 because of Create and Appendblock
                    Assert.AreEqual(2, context.RequestResults.Count);
                    blob.DownloadText(Encoding.Unicode, null, null, context);
                    Assert.AreEqual(3, context.RequestResults.Count);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        private void DoTextUploadDownloadTask(string text, bool checkDifferentEncoding)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateIfNotExistsAsync().Wait();
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");

                blob.UploadTextAsync(text).Wait();
                Assert.AreEqual(text, blob.DownloadTextAsync().Result);
                if (checkDifferentEncoding)
                {
                    Assert.AreNotEqual(text, blob.DownloadTextAsync(Encoding.Unicode, null, null, null).Result);
                }

                blob.UploadTextAsync(text, Encoding.Unicode, null, null, null).Wait();
                Assert.AreEqual(text, blob.DownloadTextAsync(Encoding.Unicode, null, null, null).Result);
                if (checkDifferentEncoding)
                {
                    Assert.AreNotEqual(text, blob.DownloadTextAsync().Result);
                }

                OperationContext context = new OperationContext();
                blob.UploadTextAsync(text, Encoding.Unicode, null, null, context).Wait();

                // 3 because of Create and Appendblock
                Assert.AreEqual(2, context.RequestResults.Count);
                blob.DownloadTextAsync(Encoding.Unicode, null, null, context).Wait();
                Assert.AreEqual(3, context.RequestResults.Count);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
#endif

        [TestMethod]
        [Description("Create snapshots of a append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSnapshot()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                blob.AppendBlock(originalData, null);
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudAppendBlob snapshot1 = blob.CreateSnapshot();
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudAppendBlob snapshot2 = blob.CreateSnapshot();
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                snapshot1.FetchAttributes();
                snapshot2.FetchAttributes();
                blob.FetchAttributes();
                AssertAreEqual(snapshot1.Properties, blob.Properties, false);

                CloudAppendBlob snapshot1Clone = new CloudAppendBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                snapshot1Clone.FetchAttributes();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties, false);

                CloudAppendBlob snapshotCopy = container.GetAppendBlobReference("blob2");
                snapshotCopy.StartCopy(TestHelper.Defiddler(snapshot1.Uri));
                WaitForCopy(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                using (Stream snapshotStream = snapshot1.OpenRead())
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                blob.CreateOrReplace();

                using (Stream snapshotStream = snapshot1.OpenRead())
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                List<IListBlobItem> blobs = container.ListBlobs(null, true, BlobListingDetails.All, null, null).ToList();
                Assert.AreEqual(4, blobs.Count);
                AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                AssertAreEqual(blob, (CloudBlob)blobs[2]);
                AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create snapshots of an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSnapshotAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreateOrReplace(result);

                    result = blob.BeginAppendBlock(originalData, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndAppendBlock(result);
                    Assert.IsFalse(blob.IsSnapshot);
                    Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                    Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                    Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                    result = blob.BeginCreateSnapshot(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudAppendBlob snapshot1 = blob.EndCreateSnapshot(result);
                    Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                    Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                    Assert.IsTrue(snapshot1.IsSnapshot);
                    Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                    Assert.AreEqual(blob.Uri, snapshot1.Uri);
                    Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                    Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                    Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                    result = blob.BeginCreateSnapshot(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudAppendBlob snapshot2 = blob.EndCreateSnapshot(result);
                    Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                    snapshot1.FetchAttributes();
                    snapshot2.FetchAttributes();
                    blob.FetchAttributes();
                    AssertAreEqual(snapshot1.Properties, blob.Properties);

                    CloudAppendBlob snapshotCopy = container.GetAppendBlobReference("blob2");
                    result = snapshotCopy.BeginStartCopy(snapshot1, null, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    snapshotCopy.EndStartCopy(result);
                    WaitForCopy(snapshotCopy);
                    Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                    result = snapshot1.BeginOpenRead(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    using (Stream snapshotStream = snapshot1.EndOpenRead(result))
                    {
                        snapshotStream.Seek(0, SeekOrigin.End);
                        TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                    }

                    result = blob.BeginCreateOrReplace(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreateOrReplace(result);

                    result = snapshot1.BeginOpenRead(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    using (Stream snapshotStream = snapshot1.EndOpenRead(result))
                    {
                        snapshotStream.Seek(0, SeekOrigin.End);
                        TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                    }

                    List<IListBlobItem> blobs = container.ListBlobs(null, true, BlobListingDetails.All, null, null).ToList();
                    Assert.AreEqual(4, blobs.Count);
                    AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                    AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                    AssertAreEqual(blob, (CloudBlob)blobs[2]);
                    AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create snapshots of an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSnapshotTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync().Wait();
                blob.AppendBlockAsync(originalData, null).Wait();
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudAppendBlob snapshot1 = blob.CreateSnapshotAsync().Result;
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudAppendBlob snapshot2 = blob.CreateSnapshotAsync().Result;
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                snapshot1.FetchAttributesAsync().Wait();
                snapshot2.FetchAttributesAsync().Wait();
                blob.FetchAttributesAsync().Wait();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudAppendBlob snapshot1Clone = new CloudAppendBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                snapshot1Clone.FetchAttributesAsync().Wait();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties);

                CloudAppendBlob snapshotCopy = container.GetAppendBlobReference("blob2");
                snapshotCopy.StartCopyAsync(snapshot1, null, null, null, null).Wait();
                WaitForCopy(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                using (Stream snapshotStream = snapshot1.OpenReadAsync().Result)
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                blob.CreateOrReplaceAsync().Wait();

                using (Stream snapshotStream = snapshot1.OpenReadAsync().Result)
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                List<IListBlobItem> blobs =
                    container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, null, null, null)
                             .Result
                             .Results
                             .ToList();
                Assert.AreEqual(4, blobs.Count);
                AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                AssertAreEqual(blob, (CloudBlob)blobs[2]);
                AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Create a snapshot with explicit metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSnapshotMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadata();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudAppendBlob snapshot = blob.CreateSnapshot(snapshotMetadata);

                // Test the client view against the expected metadata
                // None of the original metadata should be present
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                // Test the server view against the expected metadata
                snapshot.FetchAttributes();
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a snapshot with explicit metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSnapshotMetadataAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadata();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginCreateSnapshot(snapshotMetadata, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudAppendBlob snapshot = blob.EndCreateSnapshot(result);

                    // Test the client view against the expected metadata
                    // None of the original metadata should be present
                    Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                    Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                    Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                    // Test the server view against the expected metadata
                    snapshot.FetchAttributes();
                    Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                    Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                    Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a snapshot with explicit metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobSnapshotMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplaceAsync(null, null, new OperationContext()).Wait();

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadataAsync().Wait();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudAppendBlob snapshot = blob.CreateSnapshotAsync(snapshotMetadata, null, null, null).Result;

                // Test the client view against the expected metadata
                // None of the original metadata should be present
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                // Test the server view against the expected metadata
                snapshot.FetchAttributesAsync().Wait();
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Test conditional access on a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobConditionalAccess()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                blob.FetchAttributes();

                string currentETag = blob.Properties.ETag;
                DateTimeOffset currentModifiedTime = blob.Properties.LastModified.Value;

                // ETag conditional tests
                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                blob.SetMetadata(AccessCondition.GenerateIfMatchCondition(currentETag), null);

                blob.FetchAttributes();
                string newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETag should be modified on write metadata");

                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue2";

                TestHelper.ExpectedException(
                    () => blob.SetMetadata(AccessCondition.GenerateIfNoneMatchCondition(newETag), null),
                    "If none match on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                string invalidETag = "\"0x10101010\"";
                TestHelper.ExpectedException(
                    () => blob.SetMetadata(AccessCondition.GenerateIfMatchCondition(invalidETag), null),
                    "Invalid ETag on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                currentETag = blob.Properties.ETag;
                blob.SetMetadata(AccessCondition.GenerateIfNoneMatchCondition(invalidETag), null);

                blob.FetchAttributes();
                newETag = blob.Properties.ETag;

                // LastModifiedTime tests
                currentModifiedTime = blob.Properties.LastModified.Value;

                blob.Metadata["DateConditionalName"] = "DateConditionalValue";

                TestHelper.ExpectedException(
                    () => blob.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null),
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                blob.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                blob.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                blob.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                currentModifiedTime = blob.Properties.LastModified.Value;

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                TestHelper.ExpectedException(
                    () => blob.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                TestHelper.ExpectedException(
                    () => blob.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                TestHelper.ExpectedException(
                    () => blob.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                blob.Metadata["DateConditionalName"] = "DateConditionalValue2";

                currentETag = blob.Properties.ETag;
                blob.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null);

                blob.FetchAttributes();
                newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test conditional access on a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobMaxSizeConditionalAccess()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);

            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                AccessCondition condition = AccessCondition.GenerateIfMaxSizeLessThanOrEqualCondition(2 * 1024 * 1024);
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    blob.AppendBlock(originalBlob, null, condition, null, null);

                    // Seek and upload the 1MB again
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    blob.AppendBlock(originalBlob, null, condition, null, null);

                    // Seek and try to upload the 1MB again. This time it should fail with a Pre-condition failed error.
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedException(
                    () => blob.AppendBlock(originalBlob, null, condition, null, null),
                    "IfMaxSizeLessThanOrEqual conditional should throw",
                    HttpStatusCode.PreconditionFailed,
                    "MaxBlobSizeConditionNotMet");

                    originalBlob.Seek(0, SeekOrigin.Begin);
                    blob.AppendBlock(originalBlob, null, AccessCondition.GenerateIfMaxSizeLessThanOrEqualCondition(3 * 1024 * 1024), null, null);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test conditional access on a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobAppendOffsetConditionalAccess()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);

            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    blob.AppendBlock(originalBlob, null, AccessCondition.GenerateIfAppendPositionEqualCondition(0), null, null);

                    // Seek and upload the 1MB again
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    blob.AppendBlock(originalBlob, null, AccessCondition.GenerateIfAppendPositionEqualCondition(1 * 1024 * 1024), null, null);

                    // Seek and upload the 1MB again. This time it should throw since the append offset does not match
                    originalBlob.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedException(
                    () => blob.AppendBlock(originalBlob, null, AccessCondition.GenerateIfAppendPositionEqualCondition(1 * 1024 * 1024), null, null),
                    "IfAppendPositionEqual conditional should throw",
                    HttpStatusCode.PreconditionFailed,
                    "AppendPositionConditionNotMet");
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test append blob methods on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobMethodsOnBlockBlob()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                List<string> blobs = CreateBlobs(container, 1, BlobType.BlockBlob);
                CloudAppendBlob blob = container.GetAppendBlobReference(blobs.First());

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    TestHelper.ExpectedException(
                        () => blob.AppendBlock(stream, null),
                        "Append operations should fail on block blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test block blob methods on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobMethodsOnAppendBlob()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                List<string> blocks = GetBlockIdList(1);
                List<string> blobs = CreateBlobs(container, 1, BlobType.AppendBlob);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobs.First());

                TestHelper.ExpectedException(
                    () => blob.PutBlockList(blocks),
                    "Block operations should fail on append blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test page blob methods on an append blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobMethodsOnAppendBlob()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                List<string> blobs = CreateBlobs(container, 1, BlobType.AppendBlob);
                CloudPageBlob blob = container.GetPageBlobReference(blobs.First());

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    TestHelper.ExpectedException(
                        () => blob.WritePages(stream, 0),
                        "Page operations should fail on append blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try operations with an invalid Sas and snapshot")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobInvalidSasAndSnapshot()
        {
            // Sas token creds.
            string token = "?sp=abcde&sig=1";
            StorageCredentials creds = new StorageCredentials(token);
            Assert.IsTrue(creds.IsSAS);

            // Client with shared key access.
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(GetRandomContainerName());
            try
            {
                container.Create();

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                };
                string sasToken = container.GetSharedAccessSignature(policy);

                string blobUri = container.Uri.AbsoluteUri + "/blob1" + sasToken;
                TestHelper.ExpectedException<ArgumentException>(
                    () => new CloudAppendBlob(new Uri(blobUri), container.ServiceClient.Credentials),
                    "Try to use SAS creds in Uri on a shared key client");

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();
                CloudAppendBlob snapshot = blob.CreateSnapshot();
                DateTimeOffset? wrongTime = snapshot.SnapshotTime.Value + TimeSpan.FromSeconds(10);

                string snapshotUri = snapshot.Uri + "?snapshot=" + wrongTime.Value.ToString();
                TestHelper.ExpectedException<ArgumentException>(
                    () => new CloudAppendBlob(new Uri(snapshotUri), snapshot.SnapshotTime, container.ServiceClient.Credentials),
                    "Snapshot in Uri does not match snapshot on blob");

            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Use IASyncResult's WaitHandle")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void IAsyncWaitHandleTest()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                result = blob.BeginCreateOrReplace(null, null);
                result.AsyncWaitHandle.WaitOne();
                blob.EndCreateOrReplace(result);

                result = blob.BeginExists(null, null);
                result.AsyncWaitHandle.WaitOne();
                Assert.IsTrue(blob.EndExists(result));

                result = blob.BeginDelete(null, null);
                result.AsyncWaitHandle.WaitOne();
                blob.EndDelete(result);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Generate SAS for Snapshots")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobGenerateSASForSnapshot()
        {
            // Client with shared key access.
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(GetRandomContainerName());
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                container.Create();
                CloudAppendBlob blob = container.GetAppendBlobReference("Testing");
                blob.CreateOrReplace();
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                };

                CloudAppendBlob snapshot = blob.CreateSnapshot();
                string sas = snapshot.GetSharedAccessSignature(policy);
                Assert.IsNotNull(sas);
                StorageCredentials credentials = new StorageCredentials(sas);
                Uri snapshotUri = snapshot.SnapshotQualifiedUri;
                CloudAppendBlob blob1 = new CloudAppendBlob(snapshotUri, credentials);
                blob1.DownloadToStream(memoryStream);
                Assert.IsNotNull(memoryStream);
            }
            finally
            {
                container.DeleteIfExists();
                memoryStream.Close();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromStream()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, true, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, true, 1024, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, false, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, false, 1024, false, true);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromStreamAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, true, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, true, 1024, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, false, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, null, null, true, false, 1024, true, true);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromStreamLength()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                // Upload 2MB of a 5MB stream
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, true, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, true, 1024, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, false, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, false, 1024, false, true);

                // Exclude last byte
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024 - 1, null, true, true, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024 - 1, null, true, true, 1024, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024 - 1, null, true, false, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024 - 1, null, true, false, 1024, false, true);

                // Upload exact amount
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024, null, true, true, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024, null, true, true, 1024, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024, null, true, false, 0, false, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024, null, true, false, 1024, false, true);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromStreamLengthAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                // Upload 2MB of a 5MB stream
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, true, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, true, 1024, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, false, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 2 * 1024 * 1024, null, true, false, 1024, true, true);

                // Exclude last byte
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024 - 1, null, true, true, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024 - 1, null, true, true, 1024, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024 - 1, null, true, false, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024 - 1, null, true, false, 1024, true, true);

                // Upload exact amount
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024, null, true, true, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024, null, true, true, 1024, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 5 * 1024 * 1024, null, true, false, 0, true, true);
                this.CloudAppendBlobUploadFromStream(container, 5 * 1024 * 1024, 4 * 1024 * 1024, null, true, false, 1024, true, true);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromStreamLengthInvalid()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, true, true, 0, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, true, true, 1024, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, false, true, 0, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, false, true, 1024, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, true, false, 0, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, true, false, 1024, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, false, false, 0, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, false, false, 1024, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobUploadFromStreamLengthInvalidAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, true, true, 0, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, true, true, 1024, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, false, true, 0, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, false, true, 1024, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, true, false, 0, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, true, false, 1024, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 + 1, null, false, false, 0, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () =>
                    this.CloudAppendBlobUploadFromStream(
                        container, 2 * 1024 * 1024, 2 * 1024 * 1024 - 1023, null, false, false, 1024, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                container.Delete();
            }
        }

        private void CloudAppendBlobUploadFromStream(CloudBlobContainer container, int size, long? copyLength, AccessCondition accessCondition, bool seekableSourceStream, bool allowSinglePut, int startOffset, bool isAsync, bool testMd5)
        {
            byte[] buffer = GetRandomBuffer(size);

            MD5 hasher = MD5.Create();

            string md5 = string.Empty;
            if (testMd5)
            {
                md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
            }

            CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
            blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;

            using (MemoryStream originalBlobStream = new MemoryStream())
            {
                originalBlobStream.Write(buffer, startOffset, buffer.Length - startOffset);

                Stream sourceStream;
                if (seekableSourceStream)
                {
                    MemoryStream stream = new MemoryStream(buffer);
                    stream.Seek(startOffset, SeekOrigin.Begin);
                    sourceStream = stream;
                }
                else
                {
                    NonSeekableMemoryStream stream = new NonSeekableMemoryStream(buffer);
                    stream.ForceSeek(startOffset, SeekOrigin.Begin);
                    sourceStream = stream;
                }

                using (sourceStream)
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        StoreBlobContentMD5 = true,
                    };
                    if (isAsync)
                    {
                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {
                            if (copyLength.HasValue)
                            {
                                ICancellableAsyncResult result = blob.BeginUploadFromStream(
                                    sourceStream, copyLength.Value, accessCondition, options, null, ar => waitHandle.Set(), null);
                                waitHandle.WaitOne();
                                blob.EndUploadFromStream(result);
                            }
                            else
                            {
                                ICancellableAsyncResult result = blob.BeginUploadFromStream(
                                    sourceStream, accessCondition, options, null, ar => waitHandle.Set(), null);
                                waitHandle.WaitOne();
                                blob.EndUploadFromStream(result);
                            }
                        }
                    }
                    else
                    {
                        if (copyLength.HasValue)
                        {
                            blob.UploadFromStream(sourceStream, copyLength.Value, accessCondition, options);
                        }
                        else
                        {
                            blob.UploadFromStream(sourceStream, accessCondition, options);
                        }
                    }
                }

                blob.FetchAttributes();

                if (testMd5)
                {
                    Assert.AreEqual(md5, blob.Properties.ContentMD5);
                }

                using (MemoryStream downloadedBlobStream = new MemoryStream())
                {
                    if (isAsync)
                    {
                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {
                            ICancellableAsyncResult result = blob.BeginDownloadToStream(
                                downloadedBlobStream, ar => waitHandle.Set(), null);
                            waitHandle.WaitOne();
                            blob.EndDownloadToStream(result);
                        }
                    }
                    else
                    {
                        blob.DownloadToStream(downloadedBlobStream);
                    }

                    Assert.AreEqual(copyLength ?? originalBlobStream.Length, downloadedBlobStream.Length);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalBlobStream,
                        downloadedBlobStream,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalBlobStream.Length);
                }
            }

            blob.Delete();
        }
    }
}