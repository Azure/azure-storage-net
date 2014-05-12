// -----------------------------------------------------------------------------------------
// <copyright file="RetryPoliciesTests.cs" company="Microsoft">
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading;

    [TestClass]
    public class RetryPoliciesTests : TableTestBase
    {
        [TestMethod]
        [Description("Test to ensure that the time when we wait for a retry is cancellable")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void RetryDelayShouldBeCancellable()
        {
            using (ManualResetEvent responseWaitHandle = new ManualResetEvent(false),
                callbackWaitHandle = new ManualResetEvent(false))
            {
                BlobRequestOptions options = new BlobRequestOptions();
                options.RetryPolicy = new OldStyleAlwaysRetry(TimeSpan.FromMinutes(1), 1);
                OperationContext context = new OperationContext();
                context.ResponseReceived += (sender, e) => responseWaitHandle.Set();

                CloudBlobClient blobClient = GenerateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("test" + DateTime.UtcNow.Ticks.ToString());
                ICancellableAsyncResult asyncResult = container.BeginFetchAttributes(null, options, context,
                    ar =>
                    {
                        try
                        {
                            container.EndFetchAttributes(ar);
                        }
                        catch (Exception)
                        {
                            // This is expected, because we went for an invalid domain name.
                        }

                        callbackWaitHandle.Set();
                    },
                    null);

                responseWaitHandle.WaitOne();
                Thread.Sleep(10 * 1000);

                Stopwatch stopwatch = Stopwatch.StartNew();
                
                asyncResult.Cancel();
                callbackWaitHandle.WaitOne();
                
                stopwatch.Stop();

                Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(10), stopwatch.Elapsed.ToString());
                Assert.AreEqual(1, context.RequestResults.Count);
            }
        }

        [TestMethod]
        [Description("Test to ensure that the backoff time is set correctly in LinearRetry")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void VerifyLinearRetryBackOffTime()
        {
            OperationContext opContext = new OperationContext();
            TimeSpan retryInterval;

            IRetryPolicy retryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 4);

            Assert.IsTrue(retryPolicy.ShouldRetry(0, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(10), retryInterval);

            Assert.IsTrue(retryPolicy.ShouldRetry(1, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(10), retryInterval);

            Assert.IsTrue(retryPolicy.ShouldRetry(2, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(10), retryInterval);

            Assert.IsTrue(retryPolicy.ShouldRetry(3, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(10), retryInterval);

            Assert.IsFalse(retryPolicy.ShouldRetry(4, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(10), retryInterval);

            retryPolicy = new LinearRetry();

            Assert.IsTrue(retryPolicy.ShouldRetry(0, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(30), retryInterval);

            Assert.IsTrue(retryPolicy.ShouldRetry(1, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(30), retryInterval);

            Assert.IsTrue(retryPolicy.ShouldRetry(2, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(30), retryInterval);

            Assert.IsFalse(retryPolicy.ShouldRetry(3, 503, new Exception(), out retryInterval, opContext));
            Assert.AreEqual(TimeSpan.FromSeconds(30), retryInterval);
        }

        [TestMethod]
        [Description("Setting retry policy to null should not throw an exception")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void NullRetryPolicyTest()
        {
            CloudBlobContainer container = BlobTestBase.GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.RetryPolicy = null;
            container.Exists();
        }

        [TestMethod]
        [Description("Test to ensure that the location is set correctly in retry policies")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void VerifyExponentialRetryResults()
        {
            // Both locations return InternalServerError

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.PrimaryOnly,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(3) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(6) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(10) });

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.SecondaryOnly,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(3) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(6) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(10) });

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.PrimaryThenSecondary,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(8) });

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.SecondaryThenPrimary,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(8) });

            // Primary location returns InternalServerError, while secondary location returns NotFound

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.NotFound,
                LocationMode.SecondaryOnly,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(3) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(6) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(10) });

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.NotFound,
                LocationMode.PrimaryThenSecondary,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(6) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(10) });

            VerifyRetryInfoList(
                new ExponentialRetry(TimeSpan.FromSeconds(1), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.NotFound,
                LocationMode.SecondaryThenPrimary,
                retryCount => (Math.Pow(2, retryCount) - 1) * 0.2 + 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(4) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(6) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(10) });
        }

        [TestMethod]
        [Description("Test to ensure that the location is set correctly in retry policies")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void VerifyLinearRetryResults()
        {
            // Both locations return InternalServerError

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.PrimaryOnly,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) });

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.SecondaryOnly,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) });

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.PrimaryThenSecondary,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(2) });

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                LocationMode.SecondaryThenPrimary,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryThenPrimary, RetryInterval = TimeSpan.FromSeconds(2) });

            // Primary location returns InternalServerError, while secondary location returns NotFound

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.NotFound,
                LocationMode.SecondaryOnly,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.SecondaryOnly, RetryInterval = TimeSpan.FromSeconds(2) });

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.NotFound,
                LocationMode.PrimaryThenSecondary,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Secondary, UpdatedLocationMode = LocationMode.PrimaryThenSecondary, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) });

            VerifyRetryInfoList(
                new LinearRetry(TimeSpan.FromSeconds(2), 4),
                HttpStatusCode.InternalServerError,
                HttpStatusCode.NotFound,
                LocationMode.SecondaryThenPrimary,
                retryCount => 0.1,
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(0) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) },
                new RetryInfo() { TargetLocation = StorageLocation.Primary, UpdatedLocationMode = LocationMode.PrimaryOnly, RetryInterval = TimeSpan.FromSeconds(2) });
        }

        private static StorageLocation GetInitialLocation(LocationMode locationMode)
        {
            switch (locationMode)
            {
                case LocationMode.PrimaryOnly:
                case LocationMode.PrimaryThenSecondary:
                    return StorageLocation.Primary;

                case LocationMode.SecondaryOnly:
                case LocationMode.SecondaryThenPrimary:
                    return StorageLocation.Secondary;

                default:
                    throw new ArgumentOutOfRangeException("locationMode");
            }
        }

        private static StorageLocation GetNextLocation(LocationMode locationMode, StorageLocation currentLocation)
        {
            switch (locationMode)
            {
                case LocationMode.PrimaryOnly:
                    return StorageLocation.Primary;

                case LocationMode.SecondaryOnly:
                    return StorageLocation.Secondary;

                case LocationMode.PrimaryThenSecondary:
                case LocationMode.SecondaryThenPrimary:
                    return currentLocation == StorageLocation.Primary ? StorageLocation.Secondary : StorageLocation.Primary;

                default:
                    throw new ArgumentOutOfRangeException("locationMode");
            }
        }

        private static void VerifyRetryInfoList(IExtendedRetryPolicy retryPolicy, HttpStatusCode primaryStatusCode, HttpStatusCode secondaryStatusCode, LocationMode locationMode, Func<int, double> allowedDelta, params RetryInfo[] expectedRetryInfoList)
        {
            StorageLocation initialLocation = GetInitialLocation(locationMode);
            StorageLocation currentLocation = GetNextLocation(locationMode, initialLocation);

            OperationContext operationContext = new OperationContext();
            RequestResult requestResult = new RequestResult()
            {
                Exception = new Exception(),
                TargetLocation = initialLocation,
                HttpStatusCode = initialLocation == StorageLocation.Primary ? (int)primaryStatusCode : (int)secondaryStatusCode,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
            };

            for (int i = 0; i < expectedRetryInfoList.Length; i++)
            {
                RetryInfo retryInfo = retryPolicy.Evaluate(new RetryContext(i, requestResult, currentLocation, locationMode), operationContext);

                string message = string.Format("Failed at retry {0}", i);
                Assert.IsNotNull(retryInfo, message);
                Assert.AreEqual(expectedRetryInfoList[i].TargetLocation, retryInfo.TargetLocation, message);
                Assert.AreEqual(expectedRetryInfoList[i].UpdatedLocationMode, retryInfo.UpdatedLocationMode, message);
                Assert.AreEqual(expectedRetryInfoList[i].RetryInterval.TotalSeconds, retryInfo.RetryInterval.TotalSeconds, allowedDelta(i), message);

                Thread.Sleep(retryInfo.RetryInterval);
                
                requestResult.TargetLocation = retryInfo.TargetLocation;
                requestResult.HttpStatusCode = retryInfo.TargetLocation == StorageLocation.Primary ? (int)primaryStatusCode : (int)secondaryStatusCode;
                requestResult.StartTime = DateTime.Now;
                requestResult.EndTime = DateTime.Now;
                locationMode = retryInfo.UpdatedLocationMode;
                currentLocation = GetNextLocation(locationMode, currentLocation);
            }

            Assert.IsNull(retryPolicy.Evaluate(new RetryContext(expectedRetryInfoList.Length, requestResult, currentLocation, locationMode), operationContext));
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void RetryPolicyEnsure304IsNotRetried()
        {
            CloudBlobContainer container = BlobTestBase.GetRandomContainerReference();
            container.Create();

            try
            {
                CloudBlockBlob blob = container.GetBlockBlobReference("blob");
                blob.UploadFromStream(new MemoryStream(new byte[50]));

                AccessCondition accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(blob.Properties.LastModified.Value.AddMinutes(10));
                OperationContext context = new OperationContext();

                TestHelper.ExpectedException(
                    () => blob.FetchAttributes(accessCondition, new BlobRequestOptions() { RetryPolicy = new ExponentialRetry() }, context),
                    "FetchAttributes with invalid modified condition should return NotModified",
                    HttpStatusCode.NotModified);

                TestHelper.AssertNAttempts(context, 1);

                context = new OperationContext();

                TestHelper.ExpectedException(
                    () => blob.FetchAttributes(accessCondition, new BlobRequestOptions() { RetryPolicy = new LinearRetry() }, context),
                    "FetchAttributes with invalid modified condition should return NotModified",
                    HttpStatusCode.NotModified);

                TestHelper.AssertNAttempts(context, 1);
            }
            finally
            {
                container.Delete();
            }
        }

        [TestMethod]
        [Description("Test to ensure that the backoff time does not overflow")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void VerifyBackoffTimeOverflow()
        {
            ExponentialRetry exponentialRetry = new ExponentialRetry(TimeSpan.FromSeconds(4), 100000);
            VerifyBackoffTimeOverflow(exponentialRetry, 100000);

            LinearRetry linearRetry = new LinearRetry(TimeSpan.FromSeconds(4), 100000);
            VerifyBackoffTimeOverflow(linearRetry, 100000);
        }

        private void VerifyBackoffTimeOverflow(IRetryPolicy retryPolicy, int maxAttempts)
        {
            StorageException e = new StorageException();
            OperationContext context = new OperationContext();
            TimeSpan retryInterval;
            TimeSpan previousRetryInterval = TimeSpan.FromMilliseconds(1); // larger than zero to ensure we never get zero back

            for (int i = 0; i < maxAttempts; i++)
            {
                Assert.IsTrue(retryPolicy.ShouldRetry(i, (int)HttpStatusCode.InternalServerError, e, out retryInterval, context), string.Format("Attempt: {0}", i));
                Assert.IsTrue(retryInterval >= previousRetryInterval, string.Format("Retry Interval: {0}, Previous Retry Interval: {1}, Attempt: {2}", retryInterval, previousRetryInterval, i));
                previousRetryInterval = retryInterval;
            }

            Assert.IsFalse(retryPolicy.ShouldRetry(maxAttempts, (int)HttpStatusCode.InternalServerError, e, out retryInterval, context));
        }
    }
}
