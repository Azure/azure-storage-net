// -----------------------------------------------------------------------------------------
// <copyright file="BlobManglerUnitTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Test.Network;
using Microsoft.WindowsAzure.Test.Network.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class BlobManglerUnitTests : BlobTestBase
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

        [Ignore]
        [TestMethod]
        [Description("Validate the Ingress Egress Counters for blob operations")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobIngressEgressCounters()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            container.CreateIfNotExists();
            CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
            
            string[] blockIds = new string[] { Convert.ToBase64String(Guid.NewGuid().ToByteArray()), Convert.ToBase64String(Guid.NewGuid().ToByteArray()), Convert.ToBase64String(Guid.NewGuid().ToByteArray()) };
            try
            {
                // 1 byte
                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(blob.Uri.ToString()), () =>
                    {
                        OperationContext opContext = new OperationContext();
                        blob.PutBlock(blockIds[0], new MemoryStream(GetRandomBuffer(1)), null, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                        return opContext.LastResult;
                    });

                // 1024
                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(blob.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    blob.PutBlock(blockIds[1], new MemoryStream(GetRandomBuffer(1024)), null, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                    return opContext.LastResult;
                });

                // 98765
                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(blob.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    blob.PutBlock(blockIds[2], new MemoryStream(GetRandomBuffer(98765)), null, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                    return opContext.LastResult;
                });

                // PutBlockList
                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(blob.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    blob.PutBlockList(blockIds, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                    return opContext.LastResult;
                });

                // GetBlockList
                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(blob.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    blob.DownloadBlockList(BlockListingFilter.All, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                    return opContext.LastResult;
                });

                // Download
                TestHelper.ValidateIngressEgress(Selectors.IfUrlContains(blob.Uri.ToString()), () =>
                {
                    OperationContext opContext = new OperationContext();
                    blob.DownloadToStream(Stream.Null, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, opContext);
                    return opContext.LastResult;
                });

                Assert.AreEqual(blob.Properties.Length, 98765 + 1024 + 1);

                // Error Case
                CloudBlockBlob nullBlob = container.GetBlockBlobReference("null");
                OperationContext errorContext = new OperationContext();
                try
                {
                    nullBlob.DownloadToStream(Stream.Null, null, new BlobRequestOptions() { RetryPolicy = new RetryPolicies.NoRetry() }, errorContext);
                    Assert.Fail("Null blob, null stream, no download possible.");
                }
                catch (StorageException)
                {
                    Assert.IsTrue(errorContext.LastResult.IngressBytes > 0);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Force blob download to retry")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadToStreamAPMRetry()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
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
                            Exception manglerEx = null;
                            using (HttpMangler proxy = new HttpMangler(false,
                                new[]
                                {
                                    TamperBehaviors.TamperNRequestsIf(
                                        session => ThreadPool.QueueUserWorkItem(state =>
                                            {
                                                Thread.Sleep(1000);
                                                try
                                                {
                                                    session.Abort();
                                                }
                                                catch (Exception e)
                                                {
                                                    manglerEx = e;
                                                }
                                            }),
                                            2,
                                            AzureStorageSelectors.BlobTraffic().IfHostNameContains(container.ServiceClient.Credentials.AccountName))
                                }))
                            {
                                OperationContext operationContext = new OperationContext();
                                result = blob.BeginDownloadToStream(downloadedBlob, null, null, operationContext,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                blob.EndDownloadToStream(result);
                                TestHelper.AssertStreamsAreEqual(originalBlob, downloadedBlob);
                            }

                            if (manglerEx != null)
                            {
                                throw manglerEx;
                            }
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
        [Description("Force range blob download to retry")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobDownloadRangeToStreamAPMRetry()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            int offset = 1024;
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = blob.BeginUploadFromStream(originalBlob,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);
                    }
                }

                using (MemoryStream originalBlob = new MemoryStream())
                {
                    originalBlob.Write(buffer, offset, buffer.Length - offset);

                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        using (MemoryStream downloadedBlob = new MemoryStream())
                        {
                            Exception manglerEx = null;
                            using (HttpMangler proxy = new HttpMangler(false,
                                new[]
                                    {
                                        TamperBehaviors.TamperNRequestsIf(
                                            session => ThreadPool.QueueUserWorkItem(state =>
                                                {
                                                    Thread.Sleep(1000);
                                                    try
                                                    {
                                                        session.Abort();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        manglerEx = e;
                                                    }
                                                }),
                                                2,
                                                AzureStorageSelectors.BlobTraffic().IfHostNameContains(container.ServiceClient.Credentials.AccountName))
                                    }))
                            {
                                OperationContext operationContext = new OperationContext();
                                BlobRequestOptions options = new BlobRequestOptions()
                                {
                                    UseTransactionalMD5 = true
                                };

                                ICancellableAsyncResult result = blob.BeginDownloadRangeToStream(downloadedBlob, offset, buffer.Length - offset, null, options, operationContext,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                blob.EndDownloadToStream(result);
                                TestHelper.AssertStreamsAreEqual(originalBlob, downloadedBlob);
                            }

                            if (manglerEx != null)
                            {
                                throw manglerEx;
                            }
                        }
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
    }
}
