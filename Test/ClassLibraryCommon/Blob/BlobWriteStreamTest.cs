// -----------------------------------------------------------------------------------------
// <copyright file="BlobWriteStreamTest.cs" company="Microsoft">
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
using Microsoft.Azure.Storage.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Azure.Storage.Shared.Protocol;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Core.Util;

namespace Microsoft.Azure.Storage.Blob
{
    [TestClass]
    public class BlobWriteStreamTest : BlobTestBase
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
        [Description("Create blobs using blob stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobWriteStreamOpenAndClose()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                using (Stream blobStream = blockBlob.OpenWrite())
                {
                }

                CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(blockBlob.Name);
                blockBlob2.FetchAttributes();
                Assert.AreEqual(0, blockBlob2.Properties.Length);
                Assert.AreEqual(BlobType.BlockBlob, blockBlob2.Properties.BlobType);

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                TestHelper.ExpectedException(
                    () => pageBlob.OpenWrite(null),
                    "Opening a page blob stream with no size should fail on a blob that does not exist",
                    HttpStatusCode.NotFound);
                using (Stream blobStream = pageBlob.OpenWrite(1024))
                {
                }
                using (Stream blobStream = pageBlob.OpenWrite(null))
                {
                }

                CloudPageBlob pageBlob2 = container.GetPageBlobReference(pageBlob.Name);
                pageBlob2.FetchAttributes();
                Assert.AreEqual(1024, pageBlob2.Properties.Length);
                Assert.AreEqual(BlobType.PageBlob, pageBlob2.Properties.BlobType);

                CloudAppendBlob appendBlob = container.GetAppendBlobReference("blob3");
                using (Stream blobStream = appendBlob.OpenWrite(true))
                {
                }

                CloudAppendBlob appendBlob2 = container.GetAppendBlobReference(appendBlob.Name);
                appendBlob2.FetchAttributes();
                Assert.AreEqual(0, appendBlob2.Properties.Length);
                Assert.AreEqual(BlobType.AppendBlob, appendBlob2.Properties.BlobType);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamOpenWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudBlockBlob existingBlob = container.GetBlockBlobReference("blob");
                existingBlob.PutBlockList(new List<string>());

                CloudBlockBlob blob = container.GetBlockBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => blob.OpenWrite(accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.NotFound);

                blob = container.GetBlockBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = blob.OpenWrite(accessCondition);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = blob.OpenWrite(accessCondition);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = blob.OpenWrite(accessCondition);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = blob.OpenWrite(accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.NotModified);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = existingBlob.OpenWrite(accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.NotModified);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = existingBlob.OpenWrite(accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(accessCondition);
                existingBlob.SetProperties();
                TestHelper.ExpectedException(
                    () => blobStream.Dispose(),
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetBlockBlobReference("blob7");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = blob.OpenWrite(accessCondition);
                blob.PutBlockList(new List<string>());
                TestHelper.ExpectedException(
                    () => blobStream.Dispose(),
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value);
                blobStream = existingBlob.OpenWrite(accessCondition);

                // Wait 1 second so that the last modified time of the blob is in the past
                Thread.Sleep(TimeSpan.FromSeconds(1));

                existingBlob.SetProperties();
                TestHelper.ExpectedException(
                    () => blobStream.Dispose(),
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamOpenAPMWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudBlockBlob existingBlob = container.GetBlockBlobReference("blob");
                existingBlob.PutBlockList(new List<string>());

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudBlockBlob blob = container.GetBlockBlobReference("blob2");
                    AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    IAsyncResult result = blob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => blob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.NotFound);

                    blob = container.GetBlockBlobReference("blob3");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                    result = blob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Stream blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetBlockBlobReference("blob4");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = blob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetBlockBlobReference("blob5");
                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = blob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetBlockBlobReference("blob6");
                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = blob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.NotModified);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                       ar => waitHandle.Set(),
                       null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.Conflict);

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.NotModified);

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    existingBlob.SetProperties();
                    TestHelper.ExpectedException(
                        () => blobStream.Dispose(),
                        "BlobWriteStream.Dispose with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    blob = container.GetBlockBlobReference("blob7");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = blob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blob.PutBlockList(new List<string>());
                    TestHelper.ExpectedException(
                        () => blobStream.Dispose(),
                        "BlobWriteStream.Dispose with a non-met condition should fail",
                        HttpStatusCode.Conflict);

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value);
                    result = existingBlob.BeginOpenWrite(accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);

                    // Wait 1 second so that the last modified time of the blob is in the past
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    existingBlob.SetProperties();
                    TestHelper.ExpectedException(
                        () => blobStream.Dispose(),
                        "BlobWriteStream.Dispose with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);
                }
            }
            finally
            {
                container.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamOpenWithAccessConditionTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                CloudBlockBlob existingBlob = container.GetBlockBlobReference("blob");
                await existingBlob.PutBlockListAsync(new List<string>());

                CloudBlockBlob blob = container.GetBlockBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedExceptionTask(
                    blob.OpenWriteAsync(accessCondition, null, null),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.NotFound);

                blob = container.GetBlockBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = await blob.OpenWriteAsync(accessCondition, null ,null);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(accessCondition, null, null);
                blobStream.Dispose();
            }
            finally
            {
                await container.DeleteAsync();
            }
        }
#endif

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamOpenWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudPageBlob existingBlob = container.GetPageBlobReference("blob");
                existingBlob.Create(1024);

                CloudPageBlob blob = container.GetPageBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => blob.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetPageBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = blob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = blob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = blob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = blob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(1024, accessCondition),
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = existingBlob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = existingBlob.OpenWrite(1024, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(1024, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamOpenAPMWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudPageBlob existingBlob = container.GetPageBlobReference("blob");
                existingBlob.Create(1024);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudPageBlob blob = container.GetPageBlobReference("blob2");
                    AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    IAsyncResult result = blob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => blob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    blob = container.GetPageBlobReference("blob3");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                    result = blob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Stream blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetPageBlobReference("blob4");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = blob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetPageBlobReference("blob5");
                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = blob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetPageBlobReference("blob6");
                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = blob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "BlobWriteStream.Dispose with a non-met condition should fail",
                        HttpStatusCode.Conflict);

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingBlob.BeginOpenWrite(1024, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);
                }
            }
            finally
            {
                container.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamOpenWithAccessConditionTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                CloudPageBlob existingBlob = container.GetPageBlobReference("blob");
                await existingBlob.CreateAsync(1024);

                CloudPageBlob blob = container.GetPageBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedExceptionTask(
                    blob.OpenWriteAsync(1024, accessCondition, null, null),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetPageBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, null);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, null);
                blobStream.Dispose();
            }
            finally
            {
                await container.DeleteAsync();
            }
        }
#endif

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamBasicTest()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        ChecksumOptions =
                            new ChecksumOptions
                            {
                                StoreContentMD5 = true,
                                StoreContentCRC64 = true
                            }
                    };
                    using (Stream blobStream = blob.OpenWrite(null, options))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamOneByteTest()
        {
            byte buffer = 127;

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 16 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        ChecksumOptions =
                            new ChecksumOptions
                            {
                                StoreContentMD5 = true,
                                StoreContentCRC64 = true
                            }
                    };
                    using (Stream blobStream = blob.OpenWrite(null, options))
                    {
                        for (int i = 0; i < 1 * 1024 * 1024; i++)
                        {
                            blobStream.WriteByte(buffer);
                            wholeBlob.WriteByte(buffer);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamBasicTestAPM()
        {
            byte[] buffer = GetRandomBuffer(1024 * 1024);

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.DefaultRequestOptions.ParallelOperationThreadCount = 4;
            string name = GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamWriteSizeInBytes = buffer.Length;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        ChecksumOptions =
                            new ChecksumOptions
                            {
                                StoreContentMD5 = true,
                                StoreContentCRC64 = true
                            }
                    };
                    using (CloudBlobStream blobStream = blob.OpenWrite(null, options))
                    {
                        IAsyncResult[] results = new IAsyncResult[blobClient.DefaultRequestOptions.ParallelOperationThreadCount.Value * 2];
                        for (int i = 0; i < results.Length; i++)
                        {
                            results[i] = blobStream.BeginWrite(buffer, 0, buffer.Length, null, null);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        for (int i = 0; i < blobClient.DefaultRequestOptions.ParallelOperationThreadCount; i++)
                        {
                            Assert.IsTrue(results[i].IsCompleted);
                        }

                        for (int i = blobClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i < results.Length; i++)
                        {
                            Assert.IsFalse(results[i].IsCompleted);
                        }

                        for (int i = 0; i < results.Length; i++)
                        {
                            blobStream.EndWrite(results[i]);
                        }

                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {
                            IAsyncResult result = blobStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndCommit(result);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Seek in a blob write stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamSeekTest()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (Stream blobStream = blob.OpenWrite())
                {
                    TestHelper.ExpectedException<NotSupportedException>(
                        () => blobStream.Seek(1, SeekOrigin.Begin),
                        "Block blob write stream should not be seekable");
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlockBlobWriteStreamFlushTest()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = blob.OpenWrite(null, null, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(1, opContext.RequestResults.Count);

                        blobStream.Flush();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        blobStream.Flush();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        blobStream.Commit();

                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public void BlockBlobWriteStreamFlushTestAPM()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = blob.OpenWrite(null, null, opContext))
                    {
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            IAsyncResult result;
                            for (int i = 0; i < 3; i++)
                            {
                                result = blobStream.BeginWrite(
                                    buffer,
                                    0,
                                    buffer.Length,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                blobStream.EndWrite(result);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                            }

                            Assert.AreEqual(1, opContext.RequestResults.Count);

                            ICancellableAsyncResult cancellableResult = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            Assert.IsFalse(cancellableResult.IsCompleted);
                            cancellableResult.Cancel();
                            waitHandle.WaitOne();
                            blobStream.EndFlush(cancellableResult);

                            result = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            Assert.IsFalse(result.IsCompleted);
                            waitHandle.WaitOne();
                            blobStream.EndFlush(result);

                            Assert.AreEqual(2, opContext.RequestResults.Count);

                            result = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndFlush(result);

                            Assert.AreEqual(2, opContext.RequestResults.Count);

                            result = blobStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);

                            Assert.AreEqual(2, opContext.RequestResults.Count);

                            cancellableResult = blobStream.BeginFlush(null, null);
                            Assert.IsFalse(cancellableResult.IsCompleted);
                            blobStream.EndFlush(cancellableResult);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndCommit(result);

                            Assert.AreEqual(5, opContext.RequestResults.Count);
                        }
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamBasicTest()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        ChecksumOptions =
                            new ChecksumOptions
                            {
                                StoreContentMD5 = true,
                                StoreContentCRC64 = true
                            }
                    };

                    using (Stream blobStream = blob.OpenWrite(buffer.Length * 3, null, options))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }

                    TestHelper.ExpectedException<ArgumentException>(
                        () => blob.OpenWrite(null, null, options),
                        "OpenWrite with StoreBlobContentMD5/CRC64 on an existing page blob should fail");

                    using (Stream blobStream = blob.OpenWrite(null))
                    {
                        blobStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeBlob.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        wholeBlob.Seek(0, SeekOrigin.End);
                    }

                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        options.ChecksumOptions.DisableContentMD5Validation = true;
                        options.ChecksumOptions.DisableContentCRC64Validation = true;
                        blob.DownloadToStream(downloadedBlob, null, options);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamOneByteTest()
        {
            byte buffer = 127;

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 16 * 1024;

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        ChecksumOptions =
                            new ChecksumOptions
                            {
                                StoreContentMD5 = true,
                                StoreContentCRC64 = true
                            }
                    };

                    using (Stream blobStream = blob.OpenWrite(1 * 1024 * 1024, null, options))
                    {
                        for (int i = 0; i < 1 * 1024 * 1024; i++)
                        {
                            blobStream.WriteByte(buffer);
                            wholeBlob.WriteByte(buffer);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamBasicTestAPM()
        {
            byte[] buffer = GetRandomBuffer(2 * 1024 * 1024);

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.DefaultRequestOptions.ParallelOperationThreadCount = 4;
            string name = GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = buffer.Length;

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        ChecksumOptions =
                            new ChecksumOptions
                            {
                                StoreContentMD5 = true,
                                StoreContentCRC64 = true
                            }
                    };

                    IAsyncResult result;

                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                         result = blob.BeginOpenWrite(blobClient.DefaultRequestOptions.ParallelOperationThreadCount * 2 * buffer.Length, null, options, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        using (CloudBlobStream blobStream = blob.EndOpenWrite(result))
                        {
                            IAsyncResult[] results = new IAsyncResult[blobClient.DefaultRequestOptions.ParallelOperationThreadCount.Value * 2];
                            for (int i = 0; i < results.Length; i++)
                            {
                                results[i] = blobStream.BeginWrite(buffer, 0, buffer.Length, null, null);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                                Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                            }

                            for (int i = 0; i < blobClient.DefaultRequestOptions.ParallelOperationThreadCount; i++)
                            {
                                Assert.IsTrue(results[i].IsCompleted);
                            }

                            for (int i = blobClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i < results.Length; i++)
                            {
                                Assert.IsFalse(results[i].IsCompleted);
                            }

                            for (int i = 0; i < results.Length; i++)
                            {
                                blobStream.EndWrite(results[i]);
                            }

                            result = blobStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndCommit(result);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }

                    blobClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        result = blob.BeginOpenWrite(null, null, options, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException<ArgumentException>(
                            () => blob.EndOpenWrite(result),
                            "BeginOpenWrite with StoreBlobContentMD5/CRC64 on an existing page blob should fail");

                        result = blob.BeginOpenWrite(null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        using (Stream blobStream = blob.EndOpenWrite(result))
                        {
                            blobStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                            wholeBlob.Seek(buffer.Length / 2, SeekOrigin.Begin);

                            IAsyncResult[] results = new IAsyncResult[blobClient.DefaultRequestOptions.ParallelOperationThreadCount.Value * 2];
                            for (int i = 0; i < results.Length; i++)
                            {
                                results[i] = blobStream.BeginWrite(buffer, 0, buffer.Length, null, null);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                                Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                            }

                            for (int i = 0; i < blobClient.DefaultRequestOptions.ParallelOperationThreadCount; i++)
                            {
                                Assert.IsTrue(results[i].IsCompleted);
                            }

                            for (int i = blobClient.DefaultRequestOptions.ParallelOperationThreadCount.Value; i < results.Length; i++)
                            {
                                Assert.IsFalse(results[i].IsCompleted);
                            }

                            for (int i = 0; i < results.Length; i++)
                            {
                                blobStream.EndWrite(results[i]);
                            }

                            wholeBlob.Seek(0, SeekOrigin.End);
                        }

                        blob.FetchAttributes();
                        Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                        //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                        using (MemoryStream downloadedBlob = new MemoryStream())
                        {
                            options.ChecksumOptions.DisableContentMD5Validation = true;
                            options.ChecksumOptions.DisableContentCRC64Validation = true;
                            blob.DownloadToStream(downloadedBlob, null, options);
                            TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
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
        [Description("Seek in a blob write stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamRandomSeekTest()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (Stream blobStream = blob.OpenWrite(buffer.Length))
                    {
                        TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                            () => blobStream.Seek(1, SeekOrigin.Begin),
                            "Page blob stream should not allow unaligned seeks");

                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length / 512) * 512;
                            TestHelper.SeekRandomly(blobStream, offset);
                            blobStream.Write(buffer, 0, buffer.Length - offset);
                            wholeBlob.Seek(offset, SeekOrigin.Begin);
                            wholeBlob.Write(buffer, 0, buffer.Length - offset);
                        }
                    }

                    blob.FetchAttributes();
                    Assert.IsNull(blob.Properties.ContentChecksum.MD5);
                    Assert.IsNull(blob.Properties.ContentChecksum.CRC64);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void PageBlobWriteStreamFlushTest()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options = 
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = blob.OpenWrite(4 * 512, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        blobStream.Flush();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        blobStream.Flush();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        blobStream.Commit();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public void PageBlobWriteStreamFlushTestAPM()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = blob.OpenWrite(4 * buffer.Length, null, options, opContext))
                    {
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            IAsyncResult result;
                            for (int i = 0; i < 3; i++)
                            {
                                result = blobStream.BeginWrite(
                                    buffer,
                                    0,
                                    buffer.Length,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                blobStream.EndWrite(result);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                            }

                            Assert.AreEqual(2, opContext.RequestResults.Count);

                            ICancellableAsyncResult cancellableResult = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            Assert.IsFalse(cancellableResult.IsCompleted);
                            cancellableResult.Cancel();
                            waitHandle.WaitOne();
                            blobStream.EndFlush(cancellableResult);

                            result = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            Assert.IsFalse(result.IsCompleted);
                            //This line is commented out since with the new async version of write stream, multiple flushes are not expected to throw
                            //TestHelper.ExpectedException<InvalidOperationException>(
                            //    () => blobStream.BeginFlush(null, null),
                            //    null);
                            waitHandle.WaitOne();
                            blobStream.EndFlush(result);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndFlush(result);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndCommit(result);

                            Assert.AreEqual(5, opContext.RequestResults.Count);
                        }
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamBasicTest()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };

                    using (Stream blobStream = blob.OpenWrite(true, null, options))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamOneByteTest()
        {
            byte buffer = 127;

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 16 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    using (Stream blobStream = blob.OpenWrite(true, null, options, null))
                    {
                        for (int i = 0; i < 1 * 1024 * 1024; i++)
                        {
                            blobStream.WriteByte(buffer);
                            wholeBlob.WriteByte(buffer);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();
                    blob.FetchAttributes();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Seek in a blob write stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamSeekTest()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                using (Stream blobStream = blob.OpenWrite(true))
                {
                    TestHelper.ExpectedException<NotSupportedException>(
                        () => blobStream.Seek(1, SeekOrigin.Begin),
                        "Append blob write stream should not be seekable");
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamOpenWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudAppendBlob existingBlob = container.GetAppendBlobReference("blob");
                existingBlob.CreateOrReplace();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => blob.OpenWrite(true, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetAppendBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = blob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = blob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = blob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = blob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(true, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = existingBlob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(true, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(true, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = existingBlob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(true, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = existingBlob.OpenWrite(true, accessCondition);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                TestHelper.ExpectedException(
                    () => existingBlob.OpenWrite(true, accessCondition),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamOpenAPMWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.Create();

            try
            {
                CloudAppendBlob existingBlob = container.GetAppendBlobReference("blob");
                existingBlob.CreateOrReplace();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudAppendBlob blob = container.GetAppendBlobReference("blob2");
                    AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    IAsyncResult result = blob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => blob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    blob = container.GetAppendBlobReference("blob3");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                    result = blob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Stream blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetAppendBlobReference("blob4");
                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = blob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetAppendBlobReference("blob5");
                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = blob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    blob = container.GetAppendBlobReference("blob6");
                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = blob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = blob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "BlobWriteStream.Dispose with a non-met condition should fail",
                        HttpStatusCode.Conflict);

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    blobStream = existingBlob.EndOpenWrite(result);
                    blobStream.Dispose();

                    accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                    result = existingBlob.BeginOpenWrite(true, accessCondition, null, null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    TestHelper.ExpectedException(
                        () => existingBlob.EndOpenWrite(result),
                        "OpenWrite with a non-met condition should fail",
                        HttpStatusCode.PreconditionFailed);
                }
            }
            finally
            {
                container.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamOpenWithAccessConditionTask()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                CloudAppendBlob existingBlob = container.GetAppendBlobReference("blob");
                await existingBlob.CreateOrReplaceAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                TestHelper.ExpectedExceptionTask(
                    blob.OpenWriteAsync(true, accessCondition, null, null),
                    "OpenWrite with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetAppendBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = await blob.OpenWriteAsync(true, accessCondition, null, null);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, null);
                blobStream.Dispose();
            }
            finally
            {
                await container.DeleteAsync();
            }
        }
#endif

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamFlushTest()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = blob.OpenWrite(true, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        blobStream.Flush();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        blobStream.Flush();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        blobStream.Commit();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamFlushTestAPM()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = blob.OpenWrite(true, null, options, opContext))
                    {
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            IAsyncResult result;
                            for (int i = 0; i < 3; i++)
                            {
                                result = blobStream.BeginWrite(
                                    buffer,
                                    0,
                                    buffer.Length,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                blobStream.EndWrite(result);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                            }

                            Assert.AreEqual(2, opContext.RequestResults.Count);

                            ICancellableAsyncResult cancellableResult = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            Assert.IsFalse(cancellableResult.IsCompleted);
                            cancellableResult.Cancel();
                            waitHandle.WaitOne();
                            blobStream.EndFlush(cancellableResult);


                            result = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);

                            waitHandle.WaitOne();
                            blobStream.EndFlush(result);
                            Assert.IsTrue(result.IsCompleted);
                            //In the new Async Flush we will not throw in case of multiple flushes
                            blobStream.BeginFlush(ar => waitHandle.Set(), null);
                            waitHandle.WaitOne();
                            result = blobStream.BeginFlush(null, null);
                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginFlush(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndFlush(result);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);

                            Assert.AreEqual(3, opContext.RequestResults.Count);

                            result = blobStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndCommit(result);

                            Assert.AreEqual(5, opContext.RequestResults.Count);
                        }
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate that we use user's access condition for the first attempt write.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamAppendOffsetTest()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    blob.AppendBlock(new MemoryStream(buffer));
                    wholeBlob.Write(buffer, 0, buffer.Length);

                    AccessCondition condition = new AccessCondition()
                    {
                        IfMatchETag = blob.Properties.ETag
                    };

                    OperationContext context = new OperationContext();
                    context.SendingRequest += (sender, e) =>
                    {
                        if (HttpRequestParsers.GetIfMatch(e.Request) == null)
                        {
                            Assert.AreEqual(buffer.Length, int.Parse(HttpRequestParsers.GetHeader(e.Request, "x-ms-blob-condition-appendpos")));
                        }
                    };

                    // Even though the condition does not have an append position set, the client lib will internally
                    // make a FetchAttributes call to set the correct append position for the first operation.
                    using (Stream blobStream = blob.OpenWrite(false, condition, null, context))
                    {
                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);
                    }

                    blob.FetchAttributes();

                    // The length is 6MB since we uploaded 3 MB using AppendBlobFromStream and then 3MB using Write.
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                    wholeBlob.Seek(0, SeekOrigin.Begin);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate that we use user's access condition for the first attempt write.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamAppendOffsetTestAPM()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.CreateOrReplace();

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    blob.AppendBlock(new MemoryStream(buffer));
                    wholeBlob.Write(buffer, 0, buffer.Length);

                    AccessCondition condition = new AccessCondition()
                    {
                        IfMatchETag = blob.Properties.ETag
                    };

                    OperationContext context = new OperationContext();
                    context.SendingRequest += (sender, e) =>
                    {
                        if (HttpRequestParsers.GetIfMatch(e.Request) == null)
                        {
                            Assert.AreEqual(buffer.Length, int.Parse(HttpRequestParsers.GetHeader(e.Request, "x-ms-blob-condition-appendpos")));
                        }
                    };

                    // Even though the condition does not have an append position set, the client lib will internally
                    // make a FetchAttributes call to set the correct append position for the first operation.
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        IAsyncResult result = blob.BeginOpenWrite(false, condition, null, context,
                                        ar => waitHandle.Set(),
                                        null);
                        waitHandle.WaitOne();
                        using (Stream blobStream = blob.EndOpenWrite(result))
                        {
                            result = blobStream.BeginWrite(
                                   buffer,
                                   0,
                                   buffer.Length,
                                   ar => waitHandle.Set(),
                                   null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                        }
                    }

                    blob.FetchAttributes();

                    // The length is 6MB since we uploaded 3 MB using AppendBlobFromStream and then 3MB using Write.
                    Assert.AreEqual(6 * 1024 * 1024, blob.Properties.Length);

                    wholeBlob.Seek(0, SeekOrigin.Begin);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify that max conditions is passed through")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamMaxSizeConditionTest()
        {
            byte[] buffer = GetRandomBuffer(16 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 16 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    AccessCondition accessCondition = new AccessCondition() { IfMaxSizeLessThanOrEqual = 34 * 1024 };
                    try
                    {
                        using (Stream blobStream = blob.OpenWrite(true, accessCondition, null))
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                blobStream.Write(buffer, 0, buffer.Length);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                                Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                            }
                        }

                        Assert.Fail("No exception received while expecting condition failure");
                    }
                    catch (IOException ex)
                    {
                        Assert.AreEqual(SR.InvalidBlockSize, ex.Message);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify that max conditions is passed through")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AppendBlobWriteStreamMaxSizeConditionTestAPM()
        {
            byte[] buffer = GetRandomBuffer(16 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 16 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    AccessCondition accessCondition = new AccessCondition() { IfMaxSizeLessThanOrEqual = 34 * 1024 };
                    try
                    {
                        // Even though the condition does not have an append position set, the client lib will internally
                        // make a FetchAttributes call to set the correct append position for the first operation.
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            IAsyncResult result = blob.BeginOpenWrite(true, accessCondition, null, null,
                                                    ar => waitHandle.Set(),
                                                    null);
                            waitHandle.WaitOne();
                            using (Stream blobStream = blob.EndOpenWrite(result))
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    result = blobStream.BeginWrite(
                                           buffer,
                                           0,
                                           buffer.Length,
                                           ar => waitHandle.Set(),
                                           null);
                                    waitHandle.WaitOne();
                                    blobStream.EndWrite(result);
                                    wholeBlob.Write(buffer, 0, buffer.Length);
                                    Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                                }
                            }

                            Assert.Fail("No exception received while expecting condition failure");
                        }
                    }
                    catch (IOException ex)
                    {
                        Assert.AreEqual(SR.InvalidBlockSize, ex.Message);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }


        [TestMethod]
        [Description("Create blobs using blob stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobWriteStreamOpenAndCloseAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                using (CloudBlobStream writeStream = await blockBlob.OpenWriteAsync())
                {
                }

                CloudBlockBlob blockBlob2 = container.GetBlockBlobReference("blob1");
                await blockBlob2.FetchAttributesAsync();
                Assert.AreEqual(0, blockBlob2.Properties.Length);
                Assert.AreEqual(BlobType.BlockBlob, blockBlob2.Properties.BlobType);

                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await pageBlob.OpenWriteAsync(null, null, null, opContext),
                    opContext,
                    "Opening a page blob stream with no size should fail on a blob that does not exist",
                    HttpStatusCode.NotFound);
                using (CloudBlobStream writeStream = await pageBlob.OpenWriteAsync(1024))
                {
                }
                using (CloudBlobStream writeStream = await pageBlob.OpenWriteAsync(null))
                {
                }

                CloudPageBlob pageBlob2 = container.GetPageBlobReference("blob2");
                await pageBlob2.FetchAttributesAsync();
                Assert.AreEqual(1024, pageBlob2.Properties.Length);
                Assert.AreEqual(BlobType.PageBlob, pageBlob2.Properties.BlobType);

                CloudAppendBlob appendBlob = container.GetAppendBlobReference("blob3");
                using (CloudBlobStream writeStream = await appendBlob.OpenWriteAsync(true))
                {
                }

                CloudAppendBlob appendBlob2 = container.GetAppendBlobReference("blob3");
                await appendBlob2.FetchAttributesAsync();
                Assert.AreEqual(0, appendBlob2.Properties.Length);
                Assert.AreEqual(BlobType.AppendBlob, appendBlob2.Properties.BlobType);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamOpenWithAccessConditionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudBlockBlob existingBlob = container.GetBlockBlobReference("blob");
                await existingBlob.PutBlockListAsync(new List<string>());

                CloudBlockBlob blob = container.GetBlockBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.NotFound);

                blob = container.GetBlockBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                CloudBlobStream blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.NotModified);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.NotModified);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                await existingBlob.SetPropertiesAsync();
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetBlockBlobReference("blob7");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                await blob.PutBlockListAsync(new List<string>());
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                blob = container.GetBlockBlobReference("blob8");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);

                // wait for a second so that the LastModified time of existingBlob is in the past, as the precision is in seconds
                await Task.Delay(TimeSpan.FromSeconds(1));
                await existingBlob.SetPropertiesAsync();
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamOpenWithAccessConditionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudPageBlob existingBlob = container.GetPageBlobReference("blob");
                await existingBlob.CreateAsync(1024);

                CloudPageBlob blob = container.GetPageBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetPageBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                CloudBlobStream blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamOpenWithAccessConditionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudAppendBlob existingBlob = container.GetAppendBlobReference("blob");
                await existingBlob.CreateOrReplaceAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetAppendBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                CloudBlobStream blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            ChecksumWrapper hasher = new ChecksumWrapper();
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            string name = GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    using (CloudBlobStream writeStream = await blob.OpenWriteAsync(null, options, null))
                    {
                        Stream blobStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);

                        }

                        await blobStream.FlushAsync();
                    }
                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();

                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamSeekTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (CloudBlobStream writeStream = await blob.OpenWriteAsync())
                {
                    Stream blobStream = writeStream;

                    TestHelper.ExpectedException<NotSupportedException>(
                        () => blobStream.Seek(1, SeekOrigin.Begin),
                        "Block blob write stream should not be seekable");
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = await blob.OpenWriteAsync(null, null, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        }

                        // todo: Make some other better logic for this test to be reliable.
                        System.Threading.Thread.Sleep(500);

                        Assert.AreEqual(1, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.CommitAsync();

                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);
            
            ChecksumWrapper hasher = new ChecksumWrapper();

            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };

                    using (CloudBlobStream writeStream = await blob.OpenWriteAsync(buffer.Length * 3, null, options, null))
                    {
                        Stream blobStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);

                        }

                        await blobStream.FlushAsync();
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();

                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }

                    await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await blob.OpenWriteAsync(null, null, options, null),
                        "OpenWrite with StoreBlobContentMD5/CRC64 on an existing page blob should fail");

                    using (CloudBlobStream writeStream = await blob.OpenWriteAsync(null))
                    {
                        Stream blobStream = writeStream;
                        blobStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeBlob.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        await blobStream.FlushAsync();
                    }

                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        options.ChecksumOptions.DisableContentMD5Validation = true;
                        options.ChecksumOptions.DisableContentCRC64Validation = true;
                        await blob.DownloadToStreamAsync(downloadedBlob, null, options, null);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamRandomSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (CloudBlobStream writeStream = await blob.OpenWriteAsync(buffer.Length))
                    {
                        Stream blobStream = writeStream;
                        TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                            () => blobStream.Seek(1, SeekOrigin.Begin),
                            "Page blob stream should not allow unaligned seeks");

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length / 512) * 512;
                            TestHelper.SeekRandomly(blobStream, offset);
                            await blobStream.WriteAsync(buffer, 0, buffer.Length - offset);
                            wholeBlob.Seek(offset, SeekOrigin.Begin);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length - offset);
                        }
                    }

                    await blob.FetchAttributesAsync();
                    Assert.IsNull(blob.Properties.ContentChecksum.MD5);
                    Assert.IsNull(blob.Properties.ContentChecksum.CRC64);

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }
        
        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = await blob.OpenWriteAsync(4 * 512, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        }

                        // todo: Make some other better logic for this test to be reliable.
                        System.Threading.Thread.Sleep(500);

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.CommitAsync();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            ChecksumWrapper hasher = new ChecksumWrapper();

            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };

                    using (CloudBlobStream writeStream = await blob.OpenWriteAsync(true, null, options, null))
                    {
                        Stream blobStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);

                        }

                        await blobStream.FlushAsync();
                    }

                    wholeBlob.Seek(0, SeekOrigin.Begin);
                    hasher.UpdateHash(wholeBlob.ToArray(), 0, (int)wholeBlob.Length);
                    string md5 = hasher.MD5.ComputeHash();
                    string crc64 = hasher.CRC64.ComputeHash();

                    await blob.FetchAttributesAsync();
                    Assert.AreEqual(md5, blob.Properties.ContentChecksum.MD5);
                    //Assert.AreEqual(crc64, blob.Properties.ContentChecksum.CRC64); // not supported

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }

                    await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await blob.OpenWriteAsync(false, null, options, null),
                        "OpenWrite with StoreBlobContentMD5/CRC64 on an existing page blob should fail");
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamSeekTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                using (CloudBlobStream writeStream = await blob.OpenWriteAsync(true))
                {
                    Stream blobStream = writeStream;

                    TestHelper.ExpectedException<NotSupportedException>(
                        () => blobStream.Seek(1, SeekOrigin.Begin),
                        "Append blob write stream should not be seekable");
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }
        
        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    BlobRequestOptions options =
                        new BlobRequestOptions()
                        {
                            ChecksumOptions =
                                new ChecksumOptions
                                {
                                    StoreContentMD5 = true,
                                    StoreContentCRC64 = true
                                }
                        };
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = await blob.OpenWriteAsync(true, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        }

                        // todo: Make some other better logic for this test to be reliable.
                        System.Threading.Thread.Sleep(500);


                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.CommitAsync();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryOutputStream downloadedBlob = new MemoryOutputStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob.UnderlyingStream);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify that max conditions is passed through")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamMaxSizeConditionTestAsync()
        {
            byte[] buffer = GetRandomBuffer(16 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 16 * 1024;
                OperationContext context = new OperationContext();
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    AccessCondition accessCondition = new AccessCondition() { IfMaxSizeLessThanOrEqual = 34 * 1024 };
                    try
                    {
                        using (CloudBlobStream writeStream = await blob.OpenWriteAsync(true, accessCondition, null, context))
                        {
                            Stream blobStream = writeStream;

                            for (int i = 0; i < 3; i++)
                            {
                                await blobStream.WriteAsync(buffer, 0, buffer.Length);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                                Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                                await Task.Delay(5000);
                            }
                        }

                        Assert.Fail("No exception received while expecting condition failure");
                    }
                    catch (Exception ex)
                    {
                        Assert.AreEqual("Append block data should not exceed the maximum blob size condition value.", ex.Message);
                    }
                }
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

    }
}
