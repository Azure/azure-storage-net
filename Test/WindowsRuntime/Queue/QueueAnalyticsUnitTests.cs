// -----------------------------------------------------------------------------------------
// <copyright file="QueueAnalyticsUnitTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;

    [TestClass]
    public class QueueAnalyticsUnitTests : TestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        public QueueAnalyticsUnitTests()
        {
            MyClassInitialize(null);
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyClassCleanup();
            MyTestCleanup();
        }
#endif
        #region Locals + Ctors
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

        private static CloudQueueClient client;
        private static ServiceProperties props;
        private static ServiceProperties startProperties = null;
        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            client = GenerateCloudQueueClient();
            startProperties = client.GetServicePropertiesAsync().Result;
        }

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
#if NETCORE
            client.SetServicePropertiesAsync(startProperties).Wait();
#else
            client.SetServicePropertiesAsync(startProperties).Wait();
#endif
        }

        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            props = DefaultServiceProperties();

            if (TestBase.QueueBufferManager != null)
            {
                TestBase.QueueBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.QueueBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.QueueBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Analytics RoundTrip

        [TestMethod]
        [Description("Test Analytics Round Trip Async")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsRoundTripAsync()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = "1.0";

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = "1.0";

            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 6;
            props.MinuteMetrics.Version = "1.0";

            props.Cors.CorsRules.Add(
                new CorsRule()
                {
                    AllowedOrigins = new List<string>() { "www.ab.com", "www.bc.com" },
                    AllowedMethods = CorsHttpMethods.Get | CorsHttpMethods.Put,
                    MaxAgeInSeconds = 500,
                    ExposedHeaders =
                        new List<string>()
                                {
                                    "x-ms-meta-data*",
                                    "x-ms-meta-source*",
                                    "x-ms-meta-abc",
                                    "x-ms-meta-bcd"
                                },
                    AllowedHeaders =
                        new List<string>()
                                {
                                    "x-ms-meta-data*",
                                    "x-ms-meta-target*",
                                    "x-ms-meta-xyz",
                                    "x-ms-meta-foo"
                                }
                });

            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        #endregion

        #region Analytics Permutations

        [TestMethod]
        [Description("Test Analytics Disable Service Properties")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsDisableAsync()
        {
            // These are set to defaults in the test initialization
            await client.SetServicePropertiesAsync(props);

            // Check that the default service properties set in the Test Initialization were uploaded correctly
            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Default Service VersionThrows")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsDefaultServiceVersionThrowsAsync()
        {
            OperationContext ctx = new OperationContext();

            props.DefaultServiceVersion = "2009-09-19";

            try
            {
                await client.SetServicePropertiesAsync(props, null, ctx);               
            }
            catch (Exception)
            {
                Assert.AreEqual(ctx.LastResult.Exception.Message, "XML specified is not syntactically valid.");
                Assert.AreEqual(ctx.LastResult.HttpStatusCode, (int)HttpStatusCode.BadRequest);
                TestHelper.AssertNAttempts(ctx, 1);
                return;
            }

            Assert.Fail("Should not be able to set default Service Version for non Blob Client");
        }

        [TestMethod]
        [Description("Test Analytics Logging Operations")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsLoggingOperationsAsync()
        {
            // None
            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = "1.0";

            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // None
            props.Logging.LoggingOperations = LoggingOperations.All;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Metrics Level")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsMetricsLevelAsync()
        {
            // None
            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = "1.0";
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Service
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // ServiceAndAPI
            props.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Metrics Level")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsMinuteMetricsLevelAsync()
        {
            // None
            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.Version = "1.0";
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Service
            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // ServiceAndAPI
            props.MinuteMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        [TestMethod]
        [Description("Test Analytics Retention Policies")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestAnalyticsRetentionPoliciesAsync()
        {
            // Set retention policy null with metrics disabled.
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with metrics disabled.
            props.HourMetrics.RetentionDays = 1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 1;
            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with metrics enabled.
            props.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            props.HourMetrics.RetentionDays = 2;
            props.MinuteMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            props.MinuteMetrics.RetentionDays = 2;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy null with logging disabled.
            props.Logging.RetentionDays = null;
            props.Logging.LoggingOperations = LoggingOperations.None;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with logging disabled.
            props.Logging.RetentionDays = 3;
            props.Logging.LoggingOperations = LoggingOperations.None;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy null with logging enabled.
            props.Logging.RetentionDays = null;
            props.Logging.LoggingOperations = LoggingOperations.All;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());

            // Set retention policy not null with logging enabled.
            props.Logging.RetentionDays = 4;
            props.Logging.LoggingOperations = LoggingOperations.All;
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }
        
        [TestMethod]
        [Description("Test CORS with different rules.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestValidCorsRulesAsync()
        {
            CorsRule ruleMinRequired = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.xyz.com" },
                AllowedMethods = CorsHttpMethods.Get
            };

            CorsRule ruleBasic = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.ab.com", "www.bc.com" },
                AllowedMethods = CorsHttpMethods.Get | CorsHttpMethods.Put,
                MaxAgeInSeconds = 500,
                ExposedHeaders =
                    new List<string>()
                                                 {
                                                     "x-ms-meta-data*",
                                                     "x-ms-meta-source*",
                                                     "x-ms-meta-abc",
                                                     "x-ms-meta-bcd"
                                                 },
                AllowedHeaders =
                    new List<string>()
                                                 {
                                                     "x-ms-meta-data*",
                                                     "x-ms-meta-target*",
                                                     "x-ms-meta-xyz",
                                                     "x-ms-meta-foo"
                                                 }
            };

            CorsRule ruleAllMethods = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.xyz.com" },
                AllowedMethods =
                    CorsHttpMethods.Put | CorsHttpMethods.Trace
                    | CorsHttpMethods.Connect | CorsHttpMethods.Delete
                    | CorsHttpMethods.Get | CorsHttpMethods.Head
                    | CorsHttpMethods.Options | CorsHttpMethods.Post
                    | CorsHttpMethods.Merge
            };

            CorsRule ruleSingleExposedHeader = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.ab.com" },
                AllowedMethods = CorsHttpMethods.Get,
                ExposedHeaders = new List<string>() { "x-ms-meta-bcd" },
            };

            CorsRule ruleSingleExposedPrefixHeader = new CorsRule()
            {
                AllowedOrigins =
                    new List<string>() { "www.ab.com" },
                AllowedMethods = CorsHttpMethods.Get,
                ExposedHeaders =
                    new List<string>() { "x-ms-meta-data*" },
            };

            CorsRule ruleSingleAllowedHeader = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.ab.com" },
                AllowedMethods = CorsHttpMethods.Get,
                AllowedHeaders = new List<string>() { "x-ms-meta-xyz", },
            };

            CorsRule ruleSingleAllowedPrefixHeader = new CorsRule()
            {
                AllowedOrigins =
                    new List<string>() { "www.ab.com" },
                AllowedMethods = CorsHttpMethods.Get,
                AllowedHeaders =
                    new List<string>() { "x-ms-meta-target*" },
            };

            CorsRule ruleAllowAll = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "*" },
                AllowedMethods = CorsHttpMethods.Get,
                AllowedHeaders = new List<string>() { "*" },
                ExposedHeaders = new List<string>() { "*" }
            };

            CloudQueueClient client = GenerateCloudQueueClient();

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleBasic });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleMinRequired });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleAllMethods });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleSingleExposedHeader });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleSingleExposedPrefixHeader });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleSingleAllowedHeader });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleSingleAllowedPrefixHeader });

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleAllowAll });

            // Empty rule set should delete all rules
            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { });

            // Test duplicate rules
            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleBasic, ruleBasic });

            // Test max number of  rules (five)
            await this.TestCorsRulesAsync(
                client,
                null,
                new List<CorsRule>()
                    {
                        ruleBasic,
                        ruleMinRequired,
                        ruleAllMethods,
                        ruleSingleExposedHeader,
                        ruleSingleExposedPrefixHeader
                    });


            // Test max number of rules + 1 (six)
            OperationContext context = new OperationContext();
            await TestHelper.ExpectedExceptionAsync(
                    async () => await this.TestCorsRulesAsync(
                        client,
                        context,
                        new List<CorsRule>()
                            {
                                ruleBasic,
                                ruleMinRequired,
                                ruleAllMethods,
                                ruleSingleExposedHeader,
                                ruleSingleExposedPrefixHeader,
                                ruleSingleAllowedHeader
                            }),
                    context,
                    "Services are limited to a maximum of five CORS rules.",
                    HttpStatusCode.BadRequest,
                    "InvalidXmlDocument");
        }

        [TestMethod]
        [Description("Test CORS with invalid values.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestCorsExpectedExceptionsAsync()
        {
            CorsRule ruleEmpty = new CorsRule();

            CorsRule ruleInvalidMaxAge = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.xyz.com" },
                AllowedMethods = CorsHttpMethods.Get,
                MaxAgeInSeconds = -1
            };

            CloudQueueClient client = GenerateCloudQueueClient();

            await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                async () => await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleEmpty }), "Empty CORS Rules are not supported.");

            await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                async () => await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleInvalidMaxAge }),
                "MaxAgeInSeconds cannot have a value < 0.");
        }

        [TestMethod]
        [Description("Test CORS with a valid and invalid number of origin values sent to server.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestCorsMaxOriginsAsync()
        {
            CorsRule ruleManyOrigins = new CorsRule() { AllowedMethods = CorsHttpMethods.Get, };

            // Add maximum number of allowed origins
            for (int i = 0; i < 64; i++)
            {
                ruleManyOrigins.AllowedOrigins.Add("www.xyz" + i + ".com");
            }

            CloudQueueClient client = GenerateCloudQueueClient();

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleManyOrigins });

            ruleManyOrigins.AllowedOrigins.Add("www.xyz64.com");

            OperationContext context = new OperationContext();
            await TestHelper.ExpectedExceptionAsync(
               async () => await this.TestCorsRulesAsync(client, context, new List<CorsRule>() { ruleManyOrigins }),
               context,
               "A maximum of 64 origins are allowed.",
               HttpStatusCode.BadRequest,
               "InvalidXmlNodeValue");
        }

        [TestMethod]
        [Description("Test CORS with a valid and invalid number of header values sent to server.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueTestCorsMaxHeadersAsync()
        {
            CorsRule ruleManyHeaders = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.xyz.com" },
                AllowedMethods = CorsHttpMethods.Get,
                AllowedHeaders =
                    new List<string>()
                                                       {
                                                           "x-ms-meta-target*",
                                                           "x-ms-meta-other*"
                                                       },
                ExposedHeaders =
                    new List<string>()
                                                       {
                                                           "x-ms-meta-data*",
                                                           "x-ms-meta-source*"
                                                       }
            };

            // Add maximum number of non-prefixed headers
            for (int i = 0; i < 64; i++)
            {
                ruleManyHeaders.ExposedHeaders.Add("x-ms-meta-" + i);
                ruleManyHeaders.AllowedHeaders.Add("x-ms-meta-" + i);
            }

            CloudQueueClient client = GenerateCloudQueueClient();

            await this.TestCorsRulesAsync(client, null, new List<CorsRule>() { ruleManyHeaders });

            // Test with too many Exposed Headers (65)
            ruleManyHeaders.ExposedHeaders.Add("x-ms-meta-toomany");

            OperationContext context = new OperationContext();
            await TestHelper.ExpectedExceptionAsync(
                async () => await this.TestCorsRulesAsync(client, context, new List<CorsRule>() { ruleManyHeaders }),
                context,
                "A maximum of 64 literal exposed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.ExposedHeaders.Remove("x-ms-meta-toomany");

            // Test with too many Allowed Headers (65)
            ruleManyHeaders.AllowedHeaders.Add("x-ms-meta-toomany");

            await TestHelper.ExpectedExceptionAsync(
                async () => await this.TestCorsRulesAsync(client, context, new List<CorsRule>() { ruleManyHeaders }),
                context,
                "A maximum of 64 literal allowed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.AllowedHeaders.Remove("x-ms-meta-toomany");

            // Test with too many Exposed Prefixed Headers (three)
            ruleManyHeaders.ExposedHeaders.Add("x-ms-meta-toomany*");

            await TestHelper.ExpectedExceptionAsync(
                async () => await this.TestCorsRulesAsync(client, context, new List<CorsRule>() { ruleManyHeaders }),
                context,
                "A maximum of two prefixed exposed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.ExposedHeaders.Remove("x-ms-meta-toomany*");

            // Test with too many Allowed Prefixed Headers (three)
            ruleManyHeaders.AllowedHeaders.Add("x-ms-meta-toomany*");

            await TestHelper.ExpectedExceptionAsync(
                async () => await this.TestCorsRulesAsync(client, context, new List<CorsRule>() { ruleManyHeaders }),
                context,
                "A maximum of two prefixed allowed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.AllowedHeaders.Remove("x-ms-meta-toomany*");
        }
#endregion

#region Test Helpers
        private async Task TestCorsRulesAsync(CloudQueueClient client, OperationContext context, IList<CorsRule> corsProps)
        {
            props.Cors.CorsRules.Clear();

            foreach (CorsRule rule in corsProps)
            {
                props.Cors.CorsRules.Add(rule);
            }

            await client.SetServicePropertiesAsync(props, null, context);
            TestHelper.AssertServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }

        private static ServiceProperties DefaultServiceProperties()
        {
            ServiceProperties props = new ServiceProperties(new LoggingProperties(), new MetricsProperties(), new MetricsProperties(), new CorsProperties());

            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = "1.0";

            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = "1.0";

            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.Version = "1.0";

            props.Cors.CorsRules = new List<CorsRule>();

            return props;
        }
        #endregion
    }
}
