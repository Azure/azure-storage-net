﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudPageBlobTest.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class CloudPageBlobTest : BlobTestBase
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
        [Description("Create a zero-length page blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateAndDelete()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(0);
                Assert.IsTrue(blob.Exists());
                blob.Delete();
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a zero-length page blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateAndDeleteAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob1");
                    result = blob.BeginCreate(0, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);

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
        [Description("Create a zero-length page blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateAndDeleteTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(0).Wait();
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
        [Description("Get a page blob reference using its constructor")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobConstructor()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            CloudPageBlob blob = container.GetPageBlobReference("blob1");
            CloudPageBlob blob2 = new CloudPageBlob(blob.StorageUri, null, null);
            Assert.AreEqual(blob.Name, blob2.Name);
            Assert.AreEqual(blob.StorageUri, blob2.StorageUri);
            Assert.AreEqual(blob.Container.StorageUri, blob2.Container.StorageUri);
            Assert.AreEqual(blob.ServiceClient.StorageUri, blob2.ServiceClient.StorageUri);
        }

        [TestMethod]
        [Description("Resize a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobResize()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                blob.Create(1024);
                Assert.AreEqual(1024, blob.Properties.Length);
                blob2.FetchAttributes();
                Assert.AreEqual(1024, blob2.Properties.Length);
                blob2.Properties.ContentType = "text/plain";
                blob2.SetProperties();
                blob.Resize(2048);
                Assert.AreEqual(2048, blob.Properties.Length);
                blob.FetchAttributes();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                blob2.FetchAttributes();
                Assert.AreEqual(2048, blob2.Properties.Length);

                // Resize to 0 length
                blob.Resize(0);
                Assert.AreEqual(0, blob.Properties.Length);
                blob.FetchAttributes();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                blob2.FetchAttributes();
                Assert.AreEqual(0, blob2.Properties.Length);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Resize a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobResizeAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = blob.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);
                    Assert.AreEqual(1024, blob.Properties.Length);
                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    blob2.Properties.ContentType = "text/plain";
                    result = blob2.BeginSetProperties(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndSetProperties(result);
                    Assert.AreEqual(1024, blob2.Properties.Length);
                    result = blob.BeginResize(2048,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndResize(result);
                    Assert.AreEqual(2048, blob.Properties.Length);
                    result = blob.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndFetchAttributes(result);
                    Assert.AreEqual("text/plain", blob.Properties.ContentType);
                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(2048, blob2.Properties.Length);

                    // Resize to 0 length
                    result = blob.BeginResize(0,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndResize(result);
                    Assert.AreEqual(0, blob.Properties.Length);
                    result = blob.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndFetchAttributes(result);
                    Assert.AreEqual("text/plain", blob.Properties.ContentType);
                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(0, blob2.Properties.Length);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Resize a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobResizeTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                blob.CreateAsync(1024).Wait();
                Assert.AreEqual(1024, blob.Properties.Length);
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1024, blob2.Properties.Length);
                blob2.Properties.ContentType = "text/plain";
                blob2.SetPropertiesAsync().Wait();
                blob.ResizeAsync(2048).Wait();
                Assert.AreEqual(2048, blob.Properties.Length);
                blob.FetchAttributesAsync().Wait();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(2048, blob2.Properties.Length);

                // Resize to 0 length
                blob.ResizeAsync(0).Wait();
                Assert.AreEqual(0, blob.Properties.Length);
                blob.FetchAttributesAsync().Wait();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, blob2.Properties.Length);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Use sequence number conditions on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSequenceNumber()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");

                blob.Create(buffer.Length);
                Assert.IsNull(blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumber(SequenceNumberAction.Update, 5);
                Assert.AreEqual(5, blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumber(SequenceNumberAction.Max, 7);
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumber(SequenceNumberAction.Max, 3);
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumber(SequenceNumberAction.Increment, null);
                Assert.AreEqual(8, blob.Properties.PageBlobSequenceNumber);

                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetSequenceNumber(SequenceNumberAction.Update, null),
                    "SetSequenceNumber with Update should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetSequenceNumber(SequenceNumberAction.Update, -1),
                    "Negative sequence numbers are not supported");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentOutOfRangeException));

                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetSequenceNumber(SequenceNumberAction.Max, null),
                    "SetSequenceNumber with Max should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetSequenceNumber(SequenceNumberAction.Increment, 1),
                    "SetSequenceNumber with Increment should require null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null);
                    blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null);
                    blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null);
                    blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null);
                    blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedException(
                        () => blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null),
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    TestHelper.ExpectedException(
                        () => blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null),
                        "Sequence number condition should cause Clear Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedException(
                        () => blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null),
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    TestHelper.ExpectedException(
                        () => blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null),
                        "Sequence number condition should cause Clear Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedException(
                        () => blob.WritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null),
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    TestHelper.ExpectedException(
                        () => blob.ClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null),
                        "Sequence number condition should cause Clear Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.UploadFromStream(stream, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.UploadFromStream(stream, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.UploadFromStream(stream, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Use sequence number conditions on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSequenceNumberAPM()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = blob.BeginCreate(buffer.Length,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);
                    Assert.IsNull(blob.Properties.PageBlobSequenceNumber);

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Update, 5,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetSequenceNumber(result);
                    Assert.AreEqual(5, blob.Properties.PageBlobSequenceNumber);

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Max, 7,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetSequenceNumber(result);
                    Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Max, 3,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetSequenceNumber(result);
                    Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Increment, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndSetSequenceNumber(result);
                    Assert.AreEqual(8, blob.Properties.PageBlobSequenceNumber);

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Update, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    StorageException e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetSequenceNumber(result),
                        "SetSequenceNumber with Update should require a value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Update, -1,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetSequenceNumber(result),
                        "Negative sequence numbers are not supported");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentOutOfRangeException));

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Max, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetSequenceNumber(result),
                        "SetSequenceNumber with Max should require a value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                    result = blob.BeginSetSequenceNumber(SequenceNumberAction.Increment, 1,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndSetSequenceNumber(result),
                        "SetSequenceNumber with Increment should require null value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndWritePages(result);
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndClearPages(result);

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndWritePages(result);
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndClearPages(result);

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndWritePages(result);
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndClearPages(result);

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndWritePages(result);
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndClearPages(result);

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => blob.EndWritePages(result),
                            "Sequence number condition should cause Put Page to fail",
                            HttpStatusCode.PreconditionFailed,
                            "SequenceNumberConditionNotMet");
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => blob.EndClearPages(result),
                            "Sequence number condition should cause Put Page to fail",
                            HttpStatusCode.PreconditionFailed,
                            "SequenceNumberConditionNotMet");

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => blob.EndWritePages(result),
                            "Sequence number condition should cause Put Page to fail",
                            HttpStatusCode.PreconditionFailed,
                            "SequenceNumberConditionNotMet");
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => blob.EndClearPages(result),
                            "Sequence number condition should cause Put Page to fail",
                            HttpStatusCode.PreconditionFailed,
                            "SequenceNumberConditionNotMet");

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginWritePages(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => blob.EndWritePages(result),
                            "Sequence number condition should cause Put Page to fail",
                            HttpStatusCode.PreconditionFailed,
                            "SequenceNumberConditionNotMet");
                        result = blob.BeginClearPages(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => blob.EndClearPages(result),
                            "Sequence number condition should cause Put Page to fail",
                            HttpStatusCode.PreconditionFailed,
                            "SequenceNumberConditionNotMet");

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginUploadFromStream(stream, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginUploadFromStream(stream, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);

                        stream.Seek(0, SeekOrigin.Begin);
                        result = blob.BeginUploadFromStream(stream, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);
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
        [Description("Use sequence number conditions on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSequenceNumberTask()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");

                blob.CreateAsync(buffer.Length).Wait();
                Assert.IsNull(blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumberAsync(SequenceNumberAction.Update, 5).Wait();
                Assert.AreEqual(5, blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumberAsync(SequenceNumberAction.Max, 7).Wait();
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumberAsync(SequenceNumberAction.Max, 3).Wait();
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                blob.SetSequenceNumberAsync(SequenceNumberAction.Increment, null).Wait();
                Assert.AreEqual(8, blob.Properties.PageBlobSequenceNumber);

                StorageException e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetSequenceNumberAsync(SequenceNumberAction.Update, null),
                    "SetSequenceNumber with Update should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetSequenceNumberAsync(SequenceNumberAction.Update, -1),
                    "Negative sequence numbers are not supported");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentOutOfRangeException));

                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetSequenceNumberAsync(SequenceNumberAction.Max, null),
                    "SetSequenceNumber with Max should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.SetSequenceNumberAsync(SequenceNumberAction.Increment, 1),
                    "SetSequenceNumber with Increment should require null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null).Wait();
                    blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null).Wait();

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null).Wait();
                    blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null).Wait();

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null).Wait();
                    blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null).Wait();

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null).Wait();
                    blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null).Wait();

                    stream.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedExceptionTask(
                        blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null),
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    TestHelper.ExpectedExceptionTask(
                        blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null),
                        "Sequence number condition should cause Clear Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedExceptionTask(
                        blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null),
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    TestHelper.ExpectedExceptionTask(
                        blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null),
                        "Sequence number condition should cause Clear Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    TestHelper.ExpectedExceptionTask(
                        blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null),
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    TestHelper.ExpectedExceptionTask(
                        blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null),
                        "Sequence number condition should cause Clear Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null).Wait();

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null).Wait();

                    stream.Seek(0, SeekOrigin.Begin);
                    blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null).Wait();
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Page blob creation should fail with invalid size")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateInvalidSize()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                TestHelper.ExpectedException(
                () => blob.Create(-1),
                "Creating a page blob with size<0 should fail",
                HttpStatusCode.BadRequest);
                TestHelper.ExpectedException(
                    () => blob.Create(1L * 1024 * 1024 * 1024 * 1024 + 1),
                    "Creating a page blob with size > 1TB should fail",
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDeleteIfExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                Assert.IsFalse(blob.DeleteIfExists());
                blob.Create(0);
                Assert.IsTrue(blob.DeleteIfExists());
                Assert.IsFalse(blob.DeleteIfExists());
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDeleteIfExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob1");
                    IAsyncResult result = blob.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blob.EndDeleteIfExists(result));
                    result = blob.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);
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
        [Description("Try to delete a non-existing page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDeleteIfExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                Assert.IsFalse(blob.DeleteIfExistsAsync().Result);
                blob.CreateAsync(0).Wait();
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobExists()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                Assert.IsFalse(blob2.Exists());

                blob.Create(2048);

                Assert.IsTrue(blob2.Exists());
                Assert.AreEqual(2048, blob2.Properties.Length);

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobExistsAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = blob2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(blob2.EndExists(result));

                    blob.Create(2048);

                    result = blob2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(blob2.EndExists(result));
                    Assert.AreEqual(2048, blob2.Properties.Length);

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobExistsTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();

            try
            {
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                Assert.IsFalse(blob2.ExistsAsync().Result);

                blob.CreateAsync(2048).Wait();

                Assert.IsTrue(blob2.ExistsAsync().Result);
                Assert.AreEqual(2048, blob2.Properties.Length);

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobFetchAttributes()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);
                Assert.AreEqual(1024, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentDisposition);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob.Properties.BlobType);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(1024, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                Assert.IsNull(blob2.Properties.CacheControl);
                Assert.IsNull(blob2.Properties.ContentDisposition);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob2.Properties.BlobType);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobFetchAttributesAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob1");
                    IAsyncResult result = blob.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);
                    Assert.AreEqual(1024, blob.Properties.Length);
                    Assert.IsNotNull(blob.Properties.ETag);
                    Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                    Assert.IsNull(blob.Properties.CacheControl);
                    Assert.IsNull(blob.Properties.ContentDisposition);
                    Assert.IsNull(blob.Properties.ContentEncoding);
                    Assert.IsNull(blob.Properties.ContentLanguage);
                    Assert.IsNull(blob.Properties.ContentType);
                    Assert.IsNull(blob.Properties.ContentMD5);
                    Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                    Assert.AreEqual(BlobType.PageBlob, blob.Properties.BlobType);

                    CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                    result = blob2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(1024, blob2.Properties.Length);
                    Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                    Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                    Assert.IsNull(blob2.Properties.CacheControl);
                    Assert.IsNull(blob2.Properties.ContentDisposition);
                    Assert.IsNull(blob2.Properties.ContentEncoding);
                    Assert.IsNull(blob2.Properties.ContentLanguage);
                    Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                    Assert.IsNull(blob2.Properties.ContentMD5);
                    Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                    Assert.AreEqual(BlobType.PageBlob, blob2.Properties.BlobType);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobFetchAttributesTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(1024).Wait();
                Assert.AreEqual(1024, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob.Properties.BlobType);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1024, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                Assert.IsNull(blob2.Properties.CacheControl);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob2.Properties.BlobType);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
                Assert.IsNull(blob3.Properties.ContentMD5);
                byte[] target = new byte[4];
                BlobRequestOptions options2 = new BlobRequestOptions();
                options2.UseTransactionalMD5 = true;
                blob3.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob3.DownloadRangeToByteArray(target, 0, 0, 4, options: options2);
                Assert.IsNull(blob3.Properties.ContentMD5);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSetProperties()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);
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

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("attachment", blob2.Properties.ContentDisposition);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
                using (MemoryStream stream = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    blob3.DownloadToStream(stream, null, options);
                }
                AssertAreEqual(blob2.Properties, blob3.Properties);

                CloudPageBlob blob4 = (CloudPageBlob)container.ListBlobs().First();
                AssertAreEqual(blob2.Properties, blob4.Properties);

                CloudPageBlob blob5 = container.GetPageBlobReference("blob1");
                Assert.IsNull(blob5.Properties.ContentMD5);
                byte[] target = new byte[4];
                blob5.DownloadRangeToByteArray(target, 0, 0, 4);
                Assert.AreEqual("MDAwMDAwMDA=", blob5.Properties.ContentMD5);

                CloudPageBlob blob6 = container.GetPageBlobReference("blob1");
                Assert.IsNull(blob6.Properties.ContentMD5);
                target = new byte[4];
                BlobRequestOptions options2 = new BlobRequestOptions();
                options2.UseTransactionalMD5 = true;
                blob6.DownloadRangeToByteArray(target, 0, 0, 4, options: options2);
                Assert.AreEqual("MDAwMDAwMDA=", blob6.Properties.ContentMD5);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSetPropertiesAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob1");
                    IAsyncResult result = blob.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);
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

                    CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
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

                    CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
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
                    CloudPageBlob blob4 = (CloudPageBlob)results.Results.First();
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSetPropertiesTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(1024).Wait();
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

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
                using (MemoryStream stream = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    blob3.DownloadToStreamAsync(stream, null, options, null).Wait();
                }
                AssertAreEqual(blob2.Properties, blob3.Properties);

                CloudPageBlob blob4 = (CloudPageBlob)container.ListBlobsSegmentedAsync(null).Result.Results.First();
                AssertAreEqual(blob2.Properties, blob4.Properties);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Try retrieving properties of a page blob using a block blob reference")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobFetchAttributesInvalidType()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);

                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob1");
                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => blob2.FetchAttributes(),
                    "Fetching attributes of a page blob using block blob reference should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that creating a page blob can also set its metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobCreateWithMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Metadata["key1"] = "value1";
                blob.Create(1024);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
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
        [Description("Verify that empty metadata on a page blob can be retrieved.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobGetEmptyMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob.Create(1024);
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
        [Description("Verify that a page blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSetMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            var op = new OperationContext
            {
                CustomUserAgent = "dood"
            };
            
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(0, blob2.Metadata.Count);

                blob.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetMetadata(null, null, op),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "";
                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetMetadata(null, null, op),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = " ";
                e = TestHelper.ExpectedException<StorageException>(
                    () => blob.SetMetadata(null, null, op),
                    "Metadata keys should have a non-whitespace only value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "value1";
                blob.SetMetadata();

                blob2.FetchAttributes();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);

                CloudPageBlob blob3 = (CloudPageBlob)container.ListBlobs(null, true, BlobListingDetails.Metadata, null, null).First();
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
        [Description("Verify that a page blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSetMetadataAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
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
                    CloudPageBlob blob3 = (CloudPageBlob)results.Results.First();
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
        [Description("Verify that a page blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSetMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(1024).Wait();

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
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

                CloudPageBlob blob3 =
                    (CloudPageBlob)
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
        [Description("Upload/clear pages in a page blob and then verify page ranges")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobGetPageRanges()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    blob.WritePages(memoryStream, 512);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    blob.WritePages(memoryStream, 3 * 1024);
                }

                blob.ClearPages(1024, 1024);
                blob.ClearPages(0, 512);

                IEnumerable<PageRange> pageRanges = blob.GetPageRanges();
                List<string> expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                pageRanges = blob.GetPageRanges(1024, 1024);
                Assert.AreEqual(0, pageRanges.Count());

                pageRanges = blob.GetPageRanges(512, 3 * 1024);
                expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                Exception e = TestHelper.ExpectedException<StorageException>(
                    () => blob.GetPageRanges(1024),
                    "Get Page Ranges with an offset but no count should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload/clear pages in a page blob and then verify page ranges")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobGetPageRangesAPM()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    blob.WritePages(memoryStream, 512);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    blob.WritePages(memoryStream, 3 * 1024);
                }

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = blob.BeginClearPages(1024, 1024, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndClearPages(result);

                    result = blob.BeginClearPages(0, 512, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndClearPages(result);

                    result = blob.BeginGetPageRanges(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    IEnumerable<PageRange> pageRanges = blob.EndGetPageRanges(result);
                    List<string> expectedPageRanges = new List<string>()
                    {
                        new PageRange(512, 1023).ToString(),
                        new PageRange(3 * 1024, 4 * 1024 - 1).ToString(),
                    };
                    foreach (PageRange pageRange in pageRanges)
                    {
                        Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                    }
                    Assert.AreEqual(0, expectedPageRanges.Count);

                    result = blob.BeginGetPageRanges(1024,
                        1024,
                        null,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    pageRanges = blob.EndGetPageRanges(result);
                    expectedPageRanges = new List<string>();
                    foreach (PageRange pageRange in pageRanges)
                    {
                        Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                    }
                    Assert.AreEqual(0, expectedPageRanges.Count);

                    result = blob.BeginGetPageRanges(512,
                        3 * 1024,
                        null,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    pageRanges = blob.EndGetPageRanges(result);
                    expectedPageRanges = new List<string>()
                    {
                        new PageRange(512, 1023).ToString(),
                        new PageRange(3 * 1024, 7 * 512 - 1).ToString(),
                    };
                    foreach (PageRange pageRange in pageRanges)
                    {
                        Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                    }
                    Assert.AreEqual(0, expectedPageRanges.Count);

                    result = blob.BeginGetPageRanges(1024,
                        null,
                        null,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    StorageException e = TestHelper.ExpectedException<StorageException>(
                        () => blob.EndGetPageRanges(result),
                        "Get Page Ranges with an offset but no count should fail");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Upload/clear pages in a page blob and then verify page ranges")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobGetPageRangesTask()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(4 * 1024).Wait();

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    blob.WritePagesAsync(memoryStream, 512, null).Wait();
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    blob.WritePagesAsync(memoryStream, 3 * 1024, null).Wait();
                }

                blob.ClearPagesAsync(1024, 1024).Wait();
                blob.ClearPagesAsync(0, 512).Wait();

                IEnumerable<PageRange> pageRanges = blob.GetPageRangesAsync().Result;
                List<string> expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                pageRanges = blob.GetPageRangesAsync(1024, 1024, null, null, null).Result;
                Assert.AreEqual(0, pageRanges.Count());

                pageRanges = blob.GetPageRangesAsync(512, 3 * 1024, null, null, null).Result;
                expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                Exception e = TestHelper.ExpectedExceptionTask<StorageException>(
                    blob.GetPageRangesAsync(1024, null, null, null, null),
                    "Get Page Ranges with an offset but no count should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Upload pages to a page blob and then verify the contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobWritePages()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(4 * 1024 * 1024);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => blob.WritePages(memoryStream, 0),
                        "Zero-length WritePages should fail");

                    memoryStream.SetLength(4 * 1024 * 1024 + 1);
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => blob.WritePages(memoryStream, 0),
                        ">4MB WritePages should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        TestHelper.ExpectedException(
                            () => blob.WritePages(memoryStream, 512),
                            "Writing out-of-range pages should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidPageRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        blob.WritePages(memoryStream, 0, contentMD5);
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        TestHelper.ExpectedException(
                            () => blob.WritePages(memoryStream, 0, contentMD5),
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        blob.WritePages(memoryStream, 0);
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        blob.WritePages(memoryStream, 1024);
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream blobData = new MemoryStream())
                    {
                        blob.DownloadToStream(blobData);
                        Assert.AreEqual(resultingData.Length, blobData.Length);

                        Assert.IsTrue(blobData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToStreamAPM()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = blob.BeginUploadFromStream(originalBlob,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);

                        using (MemoryStream downloadedBlob = new MemoryStream())
                        {
                            OperationContext context = new OperationContext();
                            result = blob.BeginDownloadRangeToStream(downloadedBlob,
                                0, /* offset */
                                buffer.Length, /* Length */
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blob.EndDownloadRangeToStream(result);
                            TestHelper.AssertStreamsAreEqual(originalBlob, downloadedBlob);
                        }
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload pages to a page blob and then verify the contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobWritePagesAPM()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(4 * 1024 * 1024);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                            () => blob.BeginWritePages(memoryStream, 0, null, null, null),
                            "Zero-length WritePages should fail");

                        memoryStream.SetLength(4 * 1024 * 1024 + 1);
                        TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                            () => blob.BeginWritePages(memoryStream, 0, null, null, null),
                            ">4MB WritePages should fail");
                    }

                    using (MemoryStream resultingData = new MemoryStream())
                    {
                        using (MemoryStream memoryStream = new MemoryStream(buffer))
                        {
                            result = blob.BeginWritePages(memoryStream, 512, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            TestHelper.ExpectedException(
                                () => blob.EndWritePages(result),
                                "Writing out-of-range pages should fail",
                                HttpStatusCode.RequestedRangeNotSatisfiable,
                                "InvalidPageRange");

                            memoryStream.Seek(0, SeekOrigin.Begin);
                            result = blob.BeginWritePages(memoryStream, 0, contentMD5,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blob.EndWritePages(result);
                            resultingData.Write(buffer, 0, buffer.Length);

                            int offset = buffer.Length - 1024;
                            memoryStream.Seek(offset, SeekOrigin.Begin);
                            result = blob.BeginWritePages(memoryStream, 0, contentMD5,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            TestHelper.ExpectedException(
                                () => blob.EndWritePages(result),
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                            memoryStream.Seek(offset, SeekOrigin.Begin);
                            result = blob.BeginWritePages(memoryStream, 0, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blob.EndWritePages(result);
                            resultingData.Seek(0, SeekOrigin.Begin);
                            resultingData.Write(buffer, offset, buffer.Length - offset);

                            offset = buffer.Length - 2048;
                            memoryStream.Seek(offset, SeekOrigin.Begin);
                            result = blob.BeginWritePages(memoryStream, 1024, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blob.EndWritePages(result);
                            resultingData.Seek(1024, SeekOrigin.Begin);
                            resultingData.Write(buffer, offset, buffer.Length - offset);
                        }

                        using (MemoryStream blobData = new MemoryStream())
                        {
                            blob.DownloadToStream(blobData);
                            Assert.AreEqual(resultingData.Length, blobData.Length);

                            Assert.IsTrue(blobData.ToArray().SequenceEqual(resultingData.ToArray()));
                        }
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
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobDownloadToStreamTask()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    blob.UploadFromStreamAsync(originalBlob).Wait();

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        OperationContext context = new OperationContext();
                        blob.DownloadRangeToStreamAsync(downloadedBlob, 0, buffer.Length).Wait();
                        TestHelper.AssertStreamsAreEqual(originalBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload pages to a page blob and then verify the contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobWritePagesTask()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(4 * 1024 * 1024).Wait();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => blob.WritePagesAsync(memoryStream, 0, null),
                        "Zero-length WritePages should fail");

                    memoryStream.SetLength(4 * 1024 * 1024 + 1);
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => blob.WritePagesAsync(memoryStream, 0, null),
                        ">4MB WritePages should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        TestHelper.ExpectedExceptionTask(
                            blob.WritePagesAsync(memoryStream, 512, null),
                            "Writing out-of-range pages should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidPageRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        blob.WritePagesAsync(memoryStream, 0, contentMD5).Wait();
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        TestHelper.ExpectedExceptionTask(
                            blob.WritePagesAsync(memoryStream, 0, contentMD5),
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        blob.WritePagesAsync(memoryStream, 0, null).Wait();
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        blob.WritePagesAsync(memoryStream, 1024, null).Wait();
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream blobData = new MemoryStream())
                    {
                        blob.DownloadToStreamAsync(blobData).Wait();
                        Assert.AreEqual(resultingData.Length, blobData.Length);

                        Assert.IsTrue(blobData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, false, true);

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, false, true),
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, false, true);

                blob = container.GetPageBlobReference("blob3");
                blob.Create(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, false, true),
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, false, true);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamAPMWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("\"*\"");
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, true, true);

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, true, true),
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, true, true);

                blob = container.GetPageBlobReference("blob3");
                blob.Create(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, true, true),
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, accessCondition, 0, true, true);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamWithAccessConditionTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, accessCondition, 0, true);

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(1024).Wait();
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, accessCondition, 0, true),
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, accessCondition, 0, true);

                blob = container.GetPageBlobReference("blob3");
                blob.CreateAsync(1024).Wait();
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, accessCondition, 0, true),
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, accessCondition, 0, true);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStream()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, null, 0, false, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, null, 1024, false, true);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, null, 0, true, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, null, null, 1024, true, true);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();
            try
            {
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, null, 0, true);
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, null, null, 1024, true);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamLength()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                // Upload half of the stream
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 3 * 512, null, 0, false, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 3 * 512, null, 1024, false, true);

                // Upload full stream
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 6 * 512, null, 0, false, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 4 * 512, null, 1024, false, true);

                // Exclude last page
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 5 * 512, null, 0, false, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 3 * 512, null, 1024, false, true);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamLengthAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                // Upload half of the stream
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 3 * 512, null, 0, true, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 3 * 512, null, 1024, true, true);

                // Upload full stream
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 6 * 512, null, 0, true, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 4 * 512, null, 1024, true, true);

                // Exclude last page
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 5 * 512, null, 0, true, true);
                this.CloudPageBlobUploadFromStream(container, 6 * 512, 3 * 512, null, 1024, true, true);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamLengthTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();
            try
            {
                // Upload half of the stream
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, 3 * 512, null, 0, true);
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, 3 * 512, null, 1024, true);

                // Upload full stream
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, 6 * 512, null, 0, true);
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, 4 * 512, null, 1024, true);

                // Exclude last page
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, 5 * 512, null, 0, true);
                this.CloudPageBlobUploadFromStreamTask(container, 6 * 512, 3 * 512, null, 1024, true);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamLengthInvalid()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudPageBlobUploadFromStream(container, 3 * 512, 3 * 512 + 1, null, 0, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudPageBlobUploadFromStream(container, 3 * 512, 3 * 512 + 1025, null, 1024, false, false),
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamLengthInvalidAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudPageBlobUploadFromStream(container, 3 * 512, 3 * 512 + 1, null, 0, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudPageBlobUploadFromStream(container, 3 * 512, 3 * 512 + 1025, null, 1024, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadFromStreamLengthInvalidTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateAsync().Wait();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudPageBlobUploadFromStreamTask(container, 3 * 512, 3 * 512 + 1, null, 0, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudPageBlobUploadFromStreamTask(container, 3 * 512, 3 * 512 + 1025, null, 1024, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }
#endif

        private void CloudPageBlobUploadFromStream(CloudBlobContainer container, int size, long? copyLength, AccessCondition accessCondition, int startOffset, bool isAsync, bool testMd5)
        {
            byte[] buffer = GetRandomBuffer(size);

            MD5 hasher = MD5.Create();
            string md5 = string.Empty;
            if (testMd5)
            {
                md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
            }

            CloudPageBlob blob = container.GetPageBlobReference("blob1");
            blob.StreamWriteSizeInBytes = 512;

            using (MemoryStream originalBlob = new MemoryStream())
            {
                originalBlob.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
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
                        copyLength.HasValue ? (int)copyLength : (int)originalBlob.Length);
                }
            }
        }

#if TASK
        private void CloudPageBlobUploadFromStreamTask(CloudBlobContainer container, int size, long? copyLength, AccessCondition accessCondition, int startOffset, bool testMd5)
        {
            try
            {
                byte[] buffer = GetRandomBuffer(size);

                MD5 hasher = MD5.Create();
                string md5 = string.Empty;
                if (testMd5)
                {
                    md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
                }

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 512;

                using (MemoryStream originalBlob = new MemoryStream())
                {
                    originalBlob.Write(buffer, startOffset, buffer.Length - startOffset);

                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        sourceStream.Seek(startOffset, SeekOrigin.Begin);
                        BlobRequestOptions options = new BlobRequestOptions()
                        {
                            StoreBlobContentMD5 = true,
                        };

                        if (copyLength.HasValue)
                        {
                            blob.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, options, null).Wait();
                        }
                        else
                        {
                            blob.UploadFromStreamAsync(sourceStream, accessCondition, options, null).Wait();
                        }
                    }

                    blob.FetchAttributesAsync().Wait();
                    if (testMd5)
                    {
                        Assert.AreEqual(md5, blob.Properties.ContentMD5);
                    }

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStreamAsync(downloadedBlob).Wait();

                        TestHelper.AssertStreamsAreEqualAtIndex(
                            originalBlob,
                            downloadedBlob,
                            0,
                            0,
                            copyLength.HasValue ? (int)copyLength : (int)originalBlob.Length);
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
        [Description("Create snapshots of a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSnapshot()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.UploadFromStream(originalData);
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudPageBlob snapshot1 = blob.CreateSnapshot();
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudPageBlob snapshot2 = blob.CreateSnapshot();
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                snapshot1.FetchAttributes();
                snapshot2.FetchAttributes();
                blob.FetchAttributes();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudPageBlob snapshot1Clone = new CloudPageBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                snapshot1Clone.FetchAttributes();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties);

                CloudPageBlob snapshotCopy = container.GetPageBlobReference("blob2");
                snapshotCopy.StartCopy(TestHelper.Defiddler(snapshot1.Uri));
                WaitForCopy(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                TestHelper.ExpectedException<InvalidOperationException>(
                    () => snapshot1.OpenWrite(1024),
                    "Trying to write to a blob snapshot should fail");

                using (Stream snapshotStream = snapshot1.OpenRead())
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                blob.Create(1024);

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
        [Description("Create snapshots of a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSnapshotAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                IAsyncResult result;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    result = blob.BeginUploadFromStream(originalData, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndUploadFromStream(result);
                    Assert.IsFalse(blob.IsSnapshot);
                    Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                    Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                    Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                    result = blob.BeginCreateSnapshot(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudPageBlob snapshot1 = blob.EndCreateSnapshot(result);
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
                    CloudPageBlob snapshot2 = blob.EndCreateSnapshot(result);
                    Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                    snapshot1.FetchAttributes();
                    snapshot2.FetchAttributes();
                    blob.FetchAttributes();
                    AssertAreEqual(snapshot1.Properties, blob.Properties);

                    CloudPageBlob snapshotCopy = container.GetPageBlobReference("blob2");
                    result = snapshotCopy.BeginStartCopy(snapshot1, null, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    snapshotCopy.EndStartCopy(result);
                    WaitForCopy(snapshotCopy);
                    Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                    TestHelper.ExpectedException<InvalidOperationException>(
                        () => snapshot1.BeginOpenWrite(1024, ar => waitHandle.Set(), null),
                        "Trying to write to a blob snapshot should fail");

                    result = snapshot1.BeginOpenRead(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    using (Stream snapshotStream = snapshot1.EndOpenRead(result))
                    {
                        snapshotStream.Seek(0, SeekOrigin.End);
                        TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                    }

                    result = blob.BeginCreate(1024, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);

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
        [Description("Create snapshots of a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSnapshotTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.UploadFromStreamAsync(originalData).Wait();
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudPageBlob snapshot1 = blob.CreateSnapshotAsync().Result;
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudPageBlob snapshot2 = blob.CreateSnapshotAsync().Result;
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                snapshot1.FetchAttributesAsync().Wait();
                snapshot2.FetchAttributesAsync().Wait();
                blob.FetchAttributesAsync().Wait();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudPageBlob snapshot1Clone = new CloudPageBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                snapshot1Clone.FetchAttributesAsync().Wait();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties);

                CloudPageBlob snapshotCopy = container.GetPageBlobReference("blob2");
                snapshotCopy.StartCopyAsync(snapshot1, null, null, null, null).Wait();
                WaitForCopy(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                TestHelper.ExpectedException<InvalidOperationException>(
                    () => snapshot1.OpenWriteAsync(1024),
                    "Trying to write to a blob snapshot should fail");

                using (Stream snapshotStream = snapshot1.OpenReadAsync().Result)
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                blob.CreateAsync(1024).Wait();

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSnapshotMetadata()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadata();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudPageBlob snapshot = blob.CreateSnapshot(snapshotMetadata);

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSnapshotMetadataAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);

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
                    CloudPageBlob snapshot = blob.EndCreateSnapshot(result);

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobSnapshotMetadataTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(1024, null, null, new OperationContext()).Wait();

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                blob.SetMetadataAsync().Wait();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudPageBlob snapshot = blob.CreateSnapshotAsync(snapshotMetadata, null, null, null).Result;

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobConditionalAccess()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);
                blob.FetchAttributes();

                string currentETag = blob.Properties.ETag;
                DateTimeOffset currentModifiedTime = blob.Properties.LastModified.Value;

                // ETag conditional tests
                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                blob.SetMetadata(AccessCondition.GenerateIfMatchCondition(currentETag), null);

                blob.FetchAttributes();
                string newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

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
        [Description("Test page blob methods on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobMethodsOnBlockBlob()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                List<string> blobs = await CreateBlobs(container, 1, BlobType.BlockBlob);
                CloudPageBlob blob = container.GetPageBlobReference(blobs.First());

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    TestHelper.ExpectedException(
                        () => blob.WritePages(stream, 0),
                        "Page operations should fail on block blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
                }

                TestHelper.ExpectedException(
                    () => blob.ClearPages(0, 512),
                    "Page operations should fail on block blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");

                TestHelper.ExpectedException(
                    () => blob.GetPageRanges(),
                    "Page operations should fail on block blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test 512-byte page alignment")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobAlignment()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                CloudPageBlob blob = container.GetPageBlobReference("blob1");

                TestHelper.ExpectedException(
                    () => blob.Create(511),
                    "Page operations that are not 512-byte aligned should fail",
                    HttpStatusCode.BadRequest);

                TestHelper.ExpectedException(
                    () => blob.Create(513),
                    "Page operations that are not 512-byte aligned should fail",
                    HttpStatusCode.BadRequest);

                blob.Create(512);

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(511);
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => blob.WritePages(stream, 0),
                        "Page operations that are not 512-byte aligned should fail");
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(513);
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => blob.WritePages(stream, 0),
                        "Page operations that are not 512-byte aligned should fail");
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    blob.WritePages(stream, 0);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload and download null/empty data")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobUploadDownloadNoData()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob");
                TestHelper.ExpectedException<ArgumentNullException>(
                    () => blob.UploadFromStream(null),
                    "Uploading from a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    blob.UploadFromStream(stream);
                }

                TestHelper.ExpectedException<ArgumentNullException>(
                    () => blob.DownloadToStream(null),
                    "Downloading to a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    blob.DownloadToStream(stream);
                    Assert.AreEqual(0, stream.Length);
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobInvalidSasAndSnapshot()
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
                    () => new CloudPageBlob(new Uri(blobUri), container.ServiceClient.Credentials),
                    "Try to use SAS creds in Uri on a shared key client");

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(0);
                CloudPageBlob snapshot = blob.CreateSnapshot();
                DateTimeOffset? wrongTime = snapshot.SnapshotTime.Value + TimeSpan.FromSeconds(10);

                string snapshotUri = snapshot.Uri + "?snapshot=" + wrongTime.Value.ToString();
                TestHelper.ExpectedException<ArgumentException>(
                    () => new CloudPageBlob(new Uri(snapshotUri), snapshot.SnapshotTime, container.ServiceClient.Credentials),
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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void IAsyncWaitHandleTest()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                result = blob.BeginCreate(0, null, null);
                result.AsyncWaitHandle.WaitOne();
                blob.EndCreate(result);

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
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobGenerateSASForSnapshot()
        {
            // Client with shared key access.
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(GetRandomContainerName());
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                container.Create();
                CloudPageBlob blob = container.GetPageBlobReference("Testing");
                blob.Create(0);
                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                };
                CloudPageBlob snapshot = blob.CreateSnapshot();
                string sas = snapshot.GetSharedAccessSignature(policy);
                Assert.IsNotNull(sas);
                StorageCredentials credentials = new StorageCredentials(sas);
                Uri snapshotUri = snapshot.SnapshotQualifiedUri;
                CloudPageBlob blob1 = new CloudPageBlob(snapshotUri, credentials);
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
        [Description("List blobs with an incremental copied blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListBlobsWithIncrementalCopiedBlobTest()
        {
            for (int i = 0; i < 4; i++)
            {
                ListBlobsWithIncrementalCopyImpl(i);
            }
        }

        private void ListBlobsWithIncrementalCopyImpl(int overload)
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob source = container.GetPageBlobReference("source");

                string data = new string('a', 512);
                UploadText(source, data, Encoding.UTF8);

                source.Metadata["Test"] = "value";
                source.SetMetadata();

                CloudPageBlob sourceSnapshot = source.CreateSnapshot();
                System.IO.MemoryStream downloadedBlob = new System.IO.MemoryStream();
                sourceSnapshot.DownloadToStream(downloadedBlob);

                CloudPageBlob copy = container.GetPageBlobReference("copy");

                SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                };

                string sasToken = sourceSnapshot.GetSharedAccessSignature(policy);

                StorageCredentials blobSAS = new StorageCredentials(sasToken);
                Uri sourceSnapshotUri = blobSAS.TransformUri(TestHelper.Defiddler(sourceSnapshot).SnapshotQualifiedUri);

                StorageCredentials accountSAS = new StorageCredentials(sasToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, source.ServiceClient.StorageUri, null, null, null);
                CloudPageBlob snapshotWithSas = accountWithSAS.CreateCloudBlobClient().GetBlobReferenceFromServer(sourceSnapshot.SnapshotQualifiedUri) as CloudPageBlob;

                string copyId = copy.StartIncrementalCopy(TestHelper.Defiddler(snapshotWithSas));
                WaitForCopy(copy);
                List<IListBlobItem> listResults = container.ListBlobs(useFlatBlobListing: true, blobListingDetails: BlobListingDetails.All).ToList();

                Assert.AreEqual(listResults.Count(), 4);

                bool incrementalCopyFound = false;
                foreach (IListBlobItem blobItem in listResults)
                {
                    CloudPageBlob blob = blobItem as CloudPageBlob;

                    if (blob.Name == "copy" && blob.IsSnapshot)
                    {
                        // Check that the incremental copied blob is found exactly once
                        Assert.IsFalse(incrementalCopyFound);
                        Assert.IsTrue(blob.Properties.IsIncrementalCopy);
                        Assert.IsTrue(blob.CopyState.DestinationSnapshotTime.HasValue);
                        incrementalCopyFound = true;
                    }
                    else if (blob.Name == "copy")
                    {
                        Assert.IsTrue(blob.CopyState.DestinationSnapshotTime.HasValue);
                    }
                }

                Assert.IsTrue(incrementalCopyFound);

            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Set blob tier when creating a premium page blob and fetch attributes")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierOnCreate()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(0, PremiumPageBlobTier.P40, null, null, null);
                Assert.AreEqual(PremiumPageBlobTier.P40, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.BlobTierInferred.Value);
                Assert.IsFalse(blob.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob.Properties.RehydrationStatus.HasValue);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(PremiumPageBlobTier.P40, blob2.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob2.Properties.BlobTierInferred.Value);
                Assert.IsFalse(blob2.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob2.Properties.RehydrationStatus.HasValue);

                byte[] data = GetRandomBuffer(512);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob3");
                blob3.UploadFromByteArray(data, 0, data.Length, PremiumPageBlobTier.P10, null, null, null);
                Assert.AreEqual(PremiumPageBlobTier.P10, blob3.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob3.Properties.BlobTierInferred.Value);
                Assert.IsFalse(blob3.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob3.Properties.RehydrationStatus.HasValue);

                string inputFileName = "i_" + Path.GetRandomFileName();
                using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(data, 0, data.Length);
                }

                CloudPageBlob blob4 = container.GetPageBlobReference("blob4");
                blob4.UploadFromFile(inputFileName, PremiumPageBlobTier.P20, null, null, null);
                Assert.AreEqual(PremiumPageBlobTier.P20, blob4.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob4.Properties.BlobTierInferred.Value);

                using (MemoryStream memStream = new MemoryStream(data))
                {
                    CloudPageBlob blob5 = container.GetPageBlobReference("blob5");
                    blob5.UploadFromStream(memStream, PremiumPageBlobTier.P30, null, null, null);
                    Assert.AreEqual(PremiumPageBlobTier.P30, blob5.Properties.PremiumPageBlobTier);
                    Assert.IsFalse(blob5.Properties.BlobTierInferred.Value);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set blob tier when creating a premium page blob and fetch attributes - APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierOnCreateAPM()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob1");
                    result = blob.BeginCreate(0, PremiumPageBlobTier.P50, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);
                    Assert.AreEqual(PremiumPageBlobTier.P50, blob.Properties.PremiumPageBlobTier);

                    CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                    result = blob2.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(PremiumPageBlobTier.P50, blob2.Properties.PremiumPageBlobTier);

                    byte[] data = GetRandomBuffer(512);

                    CloudPageBlob blob3 = container.GetPageBlobReference("blob3");
                    result = blob3.BeginUploadFromByteArray(data, 0, data.Length, PremiumPageBlobTier.P10, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob3.EndUploadFromByteArray(result);
                    Assert.AreEqual(PremiumPageBlobTier.P10, blob3.Properties.PremiumPageBlobTier);

                    string inputFileName = "i_" + Path.GetRandomFileName();
                    using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(data, 0, data.Length);
                    }

                    CloudPageBlob blob4 = container.GetPageBlobReference("blob4");
                    result = blob4.BeginUploadFromFile(inputFileName, PremiumPageBlobTier.P20, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob4.EndUploadFromFile(result);
                    Assert.AreEqual(PremiumPageBlobTier.P20, blob4.Properties.PremiumPageBlobTier);

                    using (MemoryStream memStream = new MemoryStream(data))
                    {
                        CloudPageBlob blob5 = container.GetPageBlobReference("blob5");
                        result = blob5.BeginUploadFromStream(memStream, PremiumPageBlobTier.P30, null, null, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob5.EndUploadFromStream(result);
                        Assert.AreEqual(PremiumPageBlobTier.P30, blob5.Properties.PremiumPageBlobTier);
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
        [Description("Set blob tier on creating a premium page blob and fetch attributes - TASK")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierOnCreateTask()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(0, PremiumPageBlobTier.P60, null, null, null, CancellationToken.None).Wait();
                Assert.AreEqual(PremiumPageBlobTier.P60, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.BlobTierInferred.Value);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(PremiumPageBlobTier.P60, blob2.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob2.Properties.BlobTierInferred.Value);

                byte[] data = GetRandomBuffer(512);

                CloudPageBlob blob4 = container.GetPageBlobReference("blob4");
                blob4.UploadFromByteArrayAsync(data, 0, data.Length, PremiumPageBlobTier.P10, null, null, null, CancellationToken.None).Wait();
                Assert.AreEqual(PremiumPageBlobTier.P10, blob4.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob4.Properties.BlobTierInferred.Value);

                string inputFileName = "i_" + Path.GetRandomFileName();
                using (FileStream fs = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.WriteAsync(data, 0, data.Length).Wait();
                }

                CloudPageBlob blob5 = container.GetPageBlobReference("blob5");
                blob5.UploadFromFileAsync(inputFileName, PremiumPageBlobTier.P20, null, null, null, CancellationToken.None).Wait();
                Assert.AreEqual(PremiumPageBlobTier.P20, blob5.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob5.Properties.BlobTierInferred.Value);

                using (MemoryStream memStream = new MemoryStream(data))
                {
                    CloudPageBlob blob6 = container.GetPageBlobReference("blob6");
                    blob6.UploadFromStreamAsync(memStream, PremiumPageBlobTier.P30, null, null, null, CancellationToken.None).Wait();
                    Assert.AreEqual(PremiumPageBlobTier.P30, blob6.Properties.PremiumPageBlobTier);
                    Assert.IsFalse(blob6.Properties.BlobTierInferred.Value);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Set premium blob tier and fetch attributes")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTier()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Create(1024);
                Assert.IsFalse(blob.Properties.BlobTierInferred.HasValue);
                blob.FetchAttributes();
                Assert.IsTrue(blob.Properties.BlobTierInferred.Value);
                Assert.AreEqual(PremiumPageBlobTier.P10, blob.Properties.PremiumPageBlobTier);

                blob.SetPremiumBlobTier(PremiumPageBlobTier.P30);
                Assert.AreEqual(PremiumPageBlobTier.P30, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.BlobTierInferred.Value);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributes();
                Assert.AreEqual(PremiumPageBlobTier.P30, blob2.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob2.Properties.BlobTierInferred.Value);

                CloudPageBlob blob3 = (CloudPageBlob)container.ListBlobs().ToList().First();
                Assert.AreEqual(PremiumPageBlobTier.P30, blob3.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob3.Properties.BlobTierInferred.HasValue);

                CloudPageBlob blob4 = container.GetPageBlobReference("blob4");
                blob4.Create(125 * Constants.GB);
                try
                {
                    blob4.SetPremiumBlobTier(PremiumPageBlobTier.P6);
                    Assert.Fail("Expected failure when setting blob tier size to be less than content length");
                }
                catch (StorageException e)
                {
                    Assert.IsFalse(blob4.Properties.BlobTierInferred.HasValue);
                    Assert.AreEqual("The remote server returned an error: (409) Conflict.", e.Message);
                }

                try
                {
                    blob2.SetPremiumBlobTier(PremiumPageBlobTier.P4);
                    Assert.Fail("Expected failure when attempted to set the tier to a lower value than previously");
                }
                catch (StorageException e)
                {
                    Assert.AreEqual("The remote server returned an error: (409) Conflict.", e.Message);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set premium blob tier and fetch attributes - APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierAPM()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob1");
                    result = blob.BeginCreate(0, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);

                    result = blob.BeginSetPremiumBlobTier(PremiumPageBlobTier.P6, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndSetPremiumBlobTier(result);
                    Assert.AreEqual(PremiumPageBlobTier.P6, blob.Properties.PremiumPageBlobTier);
                    Assert.IsFalse(blob.Properties.StandardBlobTier.HasValue);
                    Assert.IsFalse(blob.Properties.RehydrationStatus.HasValue);

                    CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                    result = blob2.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob2.EndFetchAttributes(result);
                    Assert.AreEqual(PremiumPageBlobTier.P6, blob2.Properties.PremiumPageBlobTier);
                    Assert.IsFalse(blob2.Properties.StandardBlobTier.HasValue);
                    Assert.IsFalse(blob2.Properties.RehydrationStatus.HasValue);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Set premium blob tier and fetch attributes - TASK")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierTask()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.CreateAsync(0).Wait();

                blob.SetPremiumBlobTierAsync(PremiumPageBlobTier.P20).Wait();
                Assert.AreEqual(PremiumPageBlobTier.P20, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob.Properties.RehydrationStatus.HasValue);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                blob2.FetchAttributesAsync().Wait();
                Assert.AreEqual(PremiumPageBlobTier.P20, blob.Properties.PremiumPageBlobTier);
                Assert.IsFalse(blob2.Properties.StandardBlobTier.HasValue);
                Assert.IsFalse(blob2.Properties.RehydrationStatus.HasValue);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Set premium blob tier when copying from an existing blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierOnCopy()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.Create();

                CloudPageBlob source = container.GetPageBlobReference("source");
                source.Create(1024, PremiumPageBlobTier.P10, null, null, null);

                // copy to larger disk
                CloudPageBlob copy = container.GetPageBlobReference("copy");
                Assert.IsFalse(copy.Properties.BlobTierInferred.HasValue);

                string copyId = copy.StartCopy(TestHelper.Defiddler(source), PremiumPageBlobTier.P30);
                Assert.AreEqual(BlobType.PageBlob, copy.BlobType);
                Assert.AreEqual(PremiumPageBlobTier.P30, copy.Properties.PremiumPageBlobTier);
                Assert.AreEqual(PremiumPageBlobTier.P10, source.Properties.PremiumPageBlobTier);
                Assert.IsFalse(source.Properties.BlobTierInferred.Value);
                Assert.IsFalse(copy.Properties.BlobTierInferred.Value);
                WaitForCopy(copy);

                CloudPageBlob copyRef = container.GetPageBlobReference("copy");
                copyRef.FetchAttributes();
                Assert.IsFalse(copyRef.Properties.BlobTierInferred.Value);
                Assert.AreEqual(PremiumPageBlobTier.P30, copyRef.Properties.PremiumPageBlobTier);

                // copy where source does not have a tier
                CloudPageBlob source2 = container.GetPageBlobReference("source2");
                try
                {
                    source2.Create(1024);
                    CloudPageBlob copy3 = container.GetPageBlobReference("copy3");
                    string copyId3 = copy3.StartCopy(TestHelper.Defiddler(source2), PremiumPageBlobTier.P60);
                    Assert.AreEqual(BlobType.PageBlob, copy3.BlobType);
                    Assert.AreEqual(PremiumPageBlobTier.P60, copy3.Properties.PremiumPageBlobTier);
                    Assert.IsFalse(copy3.Properties.BlobTierInferred.Value);
                }
                finally
                {
                    source2.FetchAttributes();
                    Assert.IsTrue(source2.Properties.BlobTierInferred.Value);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set premium blob tier when copying from an existing blob - APM")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierOnCopyAPM()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.Create();

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("source");
                    result = blob.BeginCreate(0, PremiumPageBlobTier.P10, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndCreate(result);

                    CloudPageBlob copy = container.GetPageBlobReference("copy");
                    result = copy.BeginStartCopy(TestHelper.Defiddler(blob), PremiumPageBlobTier.P30, null, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    copy.EndStartCopy(result);

                    result = copy.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    copy.EndFetchAttributes(result);
                    Assert.AreEqual(PremiumPageBlobTier.P30, copy.Properties.PremiumPageBlobTier);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Set premium blob tier when copying from an existing blob - TASK")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Premium)]
        public void CloudPageBlobSetPremiumBlobTierOnCopyTask()
        {
            CloudBlobContainer container = GetRandomPremiumBlobContainerReference();
            try
            {
                container.CreateAsync().Wait();

                CloudPageBlob blob = container.GetPageBlobReference("source");
                blob.CreateAsync(0, PremiumPageBlobTier.P4, null, null, null, CancellationToken.None).Wait();

                CloudPageBlob copy = container.GetPageBlobReference("copy");
                copy.StartCopyAsync(blob, PremiumPageBlobTier.P60, null, null, null, null, CancellationToken.None).Wait();
                Assert.AreEqual(PremiumPageBlobTier.P4, blob.Properties.PremiumPageBlobTier);
                Assert.AreEqual(PremiumPageBlobTier.P60, copy.Properties.PremiumPageBlobTier);

                CloudPageBlob copy2 = container.GetPageBlobReference("copy");
                copy2.FetchAttributesAsync().Wait();
                Assert.AreEqual(PremiumPageBlobTier.P60, copy2.Properties.PremiumPageBlobTier);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
#endif
    }
}

