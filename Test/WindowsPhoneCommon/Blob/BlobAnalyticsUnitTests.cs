// -----------------------------------------------------------------------------------------
// <copyright file="BlobAnalyticsUnitTests.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class BlobAnalyticsUnitTests : TestBase
    {
        #region Locals + Ctors
        public BlobAnalyticsUnitTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        private static CloudBlobClient client;
        private static ServiceProperties props;
        private static ServiceProperties startProperties = null;
        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static async Task MyClassInitialize(TestContext testContext)
        {
            client = GenerateCloudBlobClient();
            startProperties = await client.GetServicePropertiesAsync();
        }

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static async Task MyClassCleanup()
        {
            await client.SetServicePropertiesAsync(startProperties);
        }

        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            props = DefaultServiceProperties();
            
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

        #endregion

        #region Analytics RoundTrip

        [TestMethod]
        [Description("Test Analytics Round Trip Async")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobTestAnalyticsRoundTripAsync()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = "1.0";

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = "1.0";

            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);

            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        #endregion

        #region Analytics Permutations

        [TestMethod]
        [Description("Test Analytics Disable Service Properties")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobTestAnalyticsDisableAsync()
        {
            // These are set to defaults in the test initializatio
            await client.SetServicePropertiesAsync(props);

            // Check that the default service properties set in the Test Initialization were uploaded correctly
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Default Service Version")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobTestAnalyticsDefaultServiceVersionAsync()
        {
            props.DefaultServiceVersion = "2009-09-19";
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);

            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            props.DefaultServiceVersion = "2011-08-18";
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            props.DefaultServiceVersion = "2012-02-12";
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            props.DefaultServiceVersion = "2013-08-15";
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Logging Operations")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobTestAnalyticsLoggingOperationsAsync()
        {
            // None
            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = "1.0";

            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // None
            props.Logging.LoggingOperations = LoggingOperations.All;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Metrics Level")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobTestAnalyticsMetricsLevelAsync()
        {
            // None
            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = "1.0";
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Service
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // ServiceAndAPI
            props.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Retention Policies")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobTestAnalyticsRetentionPoliciesAsync()
        {
            // Set retention policy null with metrics disabled.
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with metrics disabled.
            props.HourMetrics.RetentionDays = 1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with metrics enabled.
            props.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            props.HourMetrics.RetentionDays = 2;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy null with logging disabled.
            props.Logging.RetentionDays = null;
            props.Logging.LoggingOperations = LoggingOperations.None;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with logging disabled.
            props.Logging.RetentionDays = 3;
            props.Logging.LoggingOperations = LoggingOperations.None;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy null with logging enabled.
            props.Logging.RetentionDays = null;
            props.Logging.LoggingOperations = LoggingOperations.All;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with logging enabled.
            props.Logging.RetentionDays = 4;
            props.Logging.LoggingOperations = LoggingOperations.All;
            await client.SetServicePropertiesAsync(props);

            // Wait for analytics server to update
            await Task.Delay(60 * 1000);
            AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }
        #endregion

        #region Test Helpers

        private void AssertServicePropertiesAreEqual(ServiceProperties propsA, ServiceProperties propsB)
        {
            Assert.AreEqual(propsA.Logging.LoggingOperations, propsB.Logging.LoggingOperations);
            Assert.AreEqual(propsA.Logging.RetentionDays, propsB.Logging.RetentionDays);
            Assert.AreEqual(propsA.Logging.Version, propsB.Logging.Version);

            Assert.AreEqual(propsA.HourMetrics.MetricsLevel, propsB.HourMetrics.MetricsLevel);
            Assert.AreEqual(propsA.HourMetrics.RetentionDays, propsB.HourMetrics.RetentionDays);
            Assert.AreEqual(propsA.HourMetrics.Version, propsB.HourMetrics.Version);

            Assert.AreEqual(propsA.DefaultServiceVersion, propsB.DefaultServiceVersion);
        }

        private static ServiceProperties DefaultServiceProperties()
        {
            ServiceProperties props = new ServiceProperties(new LoggingProperties(), new MetricsProperties(), new MetricsProperties());

            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = "1.0";

            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = "1.0";

            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.Version = "1.0";

            props.DefaultServiceVersion = "2013-08-15";

            return props;
        }
        #endregion
    }
}
