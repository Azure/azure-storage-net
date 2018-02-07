﻿// -----------------------------------------------------------------------------------------
// <copyright file="LoggingTests.cs" company="Microsoft">
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

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.Azure.Storage.Blob;

namespace Microsoft.Azure.Storage.Core
{
    [TestClass]
    public class LoggingTests : TestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public LoggingTests()
        {
            TestInitialize();
        }
        public void Dispose()
        {
            TestCleanup();
        }
#endif
        private TestLogListener traceListener;

        [TestInitialize]
        public void TestInitialize()
        {
            this.traceListener = new TestLogListener();
            TestLogListener.Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestLogListener.Stop();
        }

        [TestMethod]
        [Description("Do a set of operations and verify if everything is logged correctly")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task LoggingTestAsync()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("logging" + Guid.NewGuid().ToString("N"));
            try
            {
                OperationContext operationContext = new OperationContext();
                operationContext.ClientRequestID = Guid.NewGuid().ToString();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await container.FetchAttributesAsync(null, null, operationContext),
                    operationContext,
                    "Fetching a non-existent container's attributes should fail",
                    HttpStatusCode.NotFound);
                Assert.AreEqual(1, TestLogListener.RequestCount);
                Assert.AreEqual(1, TestLogListener.ErrorCount);
                Assert.AreEqual(1, TestLogListener.WarningCount);
                Assert.AreEqual(operationContext.ClientRequestID, TestLogListener.LastRequestID);

                operationContext.ClientRequestID = Guid.NewGuid().ToString();
                await container.CreateIfNotExistsAsync(null, operationContext);
                Assert.AreEqual(2, TestLogListener.RequestCount);
                Assert.AreEqual(1, TestLogListener.ErrorCount);
                Assert.AreEqual(1, TestLogListener.WarningCount);
                Assert.AreEqual(operationContext.ClientRequestID, TestLogListener.LastRequestID);

                string lastLoggedRequestID = TestLogListener.LastRequestID;
                operationContext.ClientRequestID = Guid.NewGuid().ToString();
                operationContext.LogLevel = LogLevel.Off;
                await container.DeleteIfExistsAsync(null, null, operationContext);
                Assert.AreEqual(2, TestLogListener.RequestCount);
                Assert.AreEqual(1, TestLogListener.ErrorCount);
                Assert.AreEqual(1, TestLogListener.WarningCount);
                Assert.AreEqual(lastLoggedRequestID, TestLogListener.LastRequestID);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
