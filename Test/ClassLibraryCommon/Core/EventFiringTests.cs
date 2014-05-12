// -----------------------------------------------------------------------------------------
// <copyright file="EventFiringTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.IO;
    using System.Linq;
    using System.Text;

#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Test.Network.Behaviors;
    using Microsoft.WindowsAzure.Test.Network;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    [TestClass]
    public class EventFiringTests : BlobTestBase
    {
        private static int SingleBlobUploadThresholdInBytes = 2 * 1024 * 1024;
        private static int maxRetries = 5;

        [TestMethod]
        [Description("Test to ensure that the proper events are getting fired when we make service calls")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TestEvents()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();
                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                TestEventsHelper(blob, 1 * 1024 * 1024, 1, 1, 1, 0, false);
                TestEventsHelper(blob, 3 * 1024 * 1024, 2, 2, 2, 0, false);
                blob = container.GetBlockBlobReference("blob2");
                TestEventsHelper(blob, 3 * 1024 * 1024, maxRetries + 1, maxRetries + 1, maxRetries + 1, maxRetries, true);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        public void TestEventsHelper(CloudBlockBlob blob, int bufferSize, int expectedSending, int expectedResponseReceived, int expectedRequestComplete, int expectedRetry, bool testRetry)
        {
            byte[] buffer = GetRandomBuffer(bufferSize);

            BlobRequestOptions blobOptions = new BlobRequestOptions();
            blobOptions.SingleBlobUploadThresholdInBytes = EventFiringTests.SingleBlobUploadThresholdInBytes;
            blobOptions.RetryPolicy = new OldStyleAlwaysRetry(new TimeSpan(1), maxRetries);
            int sendingRequestFires = 0;
            int responseReceivedFires = 0;
            int requestCompleteFires = 0;
            int retryFires = 0;

            OperationContext.GlobalSendingRequest += (sender, args) => sendingRequestFires++;
            OperationContext.GlobalResponseReceived += (sender, args) => responseReceivedFires++;
            OperationContext.GlobalRequestCompleted += (sender, args) => requestCompleteFires++;
            OperationContext.GlobalRetrying += (sender, args) => retryFires++;

            if (testRetry)
            {
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    try
                    {
                        blob.DownloadToStream(wholeBlob, null, blobOptions, null);
                    }
                    catch (StorageException e)
                    {
                        Assert.AreEqual(404, e.RequestInformation.HttpStatusCode);
                    }
                }
            }
            else
            {
                using (MemoryStream wholeBlob = new MemoryStream(buffer))
                {
                    blob.UploadFromStream(wholeBlob, null, blobOptions, null);
                }
            }

            Assert.AreEqual(expectedSending, sendingRequestFires);
            Assert.AreEqual(expectedResponseReceived, responseReceivedFires);
            Assert.AreEqual(expectedRequestComplete, requestCompleteFires);
            Assert.AreEqual(expectedRetry, retryFires);
        }
    }
}
