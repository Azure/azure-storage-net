// -----------------------------------------------------------------------------------------
// <copyright file="TableAnalyticsUnitTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;

    [TestClass]
    public class TableAnalyticsUnitTests : TableTestBase
    {
        #region Locals + Ctors
        public TableAnalyticsUnitTests()
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

        private static CloudTableClient client;
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
            client = GenerateCloudTableClient();
            startProperties = client.GetServiceProperties();
        }

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            client.SetServiceProperties(startProperties);
        }

        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            props = DefaultServiceProperties();

            if (TestBase.TableBufferManager != null)
            {
                TestBase.TableBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }
        #endregion

        #region Analytics RoundTrip

        #region Sync

        [TestMethod]
        [Description("Test Analytics Round Trip Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsRoundTripSync()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 6;
            props.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

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

            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        #endregion

        #region APM

        [TestMethod]
        [Description("Test Analytics Round Trip APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsRoundTripAPM()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 6;
            props.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

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

            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult result = null;
                client.BeginSetServiceProperties(props, (res) =>
                {
                    result = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                client.EndSetServiceProperties(result);
            }

            ServiceProperties retrievedProps = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult result = null;
                client.BeginGetServiceProperties((res) =>
                {
                    result = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                retrievedProps = client.EndGetServiceProperties(result);
            }

            TestHelper.AssertServicePropertiesAreEqual(props, retrievedProps);
        }

        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("Test Analytics Round Trip Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsRoundTripTask()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props).Wait();

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync().Result);
        }

        [TestMethod]
        [Description("Test Table GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGetSetServicePropertiesTask()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 8;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 8;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props).Wait();

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync().Result);
        }

        [TestMethod]
        [Description("Test Table GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableGetSetServicePropertiesCancellationTokenTask()
        {
            CancellationToken cancellationToken = CancellationToken.None;

            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 9;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 9;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props, cancellationToken).Wait();

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync(cancellationToken).Result);
        }

        [TestMethod]
        [Description("Test Table GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableGetSetServicePropertiesRequestOptionsOperationContextTask()
        {
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 10;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 10;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props, requestOptions, operationContext).Wait();

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync(requestOptions, operationContext).Result);
        }

        [TestMethod]
        [Description("Test Table GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableGetSetServicePropertiesRequestOptionsOperationContextCancellationTokenTask()
        {
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 11;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 11;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props, requestOptions, operationContext).Wait();

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync(requestOptions, operationContext).Result);
        }
#endif

        #endregion

        #endregion

        #region Analytics Permutations

        [TestMethod]
        [Description("Test Analytics Disable Service Properties")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsDisable()
        {
            // These are set to defaults in the test initialization
            client.SetServiceProperties(props);

            // Check that the default service properties set in the Test Initialization were uploaded correctly
            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Default Service VersionThrows")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsDefaultServiceVersionThrows()
        {
            OperationContext ctx = new OperationContext();

            props.DefaultServiceVersion = "2009-09-19";

            try
            {
                client.SetServiceProperties(props, null, ctx);
                Assert.Fail("Should not be able to set default Service Version for non Blob Client");
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.Message, "The remote server returned an error: (400) Bad Request.");
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, (int)HttpStatusCode.BadRequest);
                TestHelper.AssertNAttempts(ctx, 1);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        [Description("Test Analytics Logging Operations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsLoggingOperations()
        {
            // None
            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // None
            props.Logging.LoggingOperations = LoggingOperations.All;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Metrics Level")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsMetricsLevel()
        {
            // None
            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Service
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // ServiceAndAPI
            props.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Minute Metrics Level")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsMinuteMetricsLevel()
        {
            // None
            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Service
            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // ServiceAndAPI
            props.MinuteMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Retention Policies")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsRetentionPolicies()
        {
            // Set retention policy null with metrics disabled.
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Set retention policy not null with metrics enabled.
            props.HourMetrics.RetentionDays = 1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 1;
            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Set retention policy not null with metrics enabled.
            props.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            props.HourMetrics.RetentionDays = 2;
            props.MinuteMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            props.MinuteMetrics.RetentionDays = 2;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Set retention policy null with logging disabled.
            props.Logging.RetentionDays = null;
            props.Logging.LoggingOperations = LoggingOperations.None;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Set retention policy not null with logging disabled.
            props.Logging.RetentionDays = 3;
            props.Logging.LoggingOperations = LoggingOperations.None;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Set retention policy null with logging enabled.
            props.Logging.RetentionDays = null;
            props.Logging.LoggingOperations = LoggingOperations.All;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // Set retention policy not null with logging enabled.
            props.Logging.RetentionDays = 4;
            props.Logging.LoggingOperations = LoggingOperations.All;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }
        
        [TestMethod]
        [Description("Test CORS with different rules.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestValidCorsRules()
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

            CloudTableClient client = GenerateCloudTableClient();

            this.TestCorsRules(client, new List<CorsRule>() { ruleBasic });

            this.TestCorsRules(client, new List<CorsRule>() { ruleMinRequired });

            this.TestCorsRules(client, new List<CorsRule>() { ruleAllMethods });

            this.TestCorsRules(client, new List<CorsRule>() { ruleSingleExposedHeader });

            this.TestCorsRules(client, new List<CorsRule>() { ruleSingleExposedPrefixHeader });

            this.TestCorsRules(client, new List<CorsRule>() { ruleSingleAllowedHeader });

            this.TestCorsRules(client, new List<CorsRule>() { ruleSingleAllowedPrefixHeader });

            this.TestCorsRules(client, new List<CorsRule>() { ruleAllowAll });

            // Empty rule set should delete all rules
            this.TestCorsRules(client, new List<CorsRule>() { });

            // Test duplicate rules
            this.TestCorsRules(client, new List<CorsRule>() { ruleBasic, ruleBasic });

            // Test max number of  rules (five)
            this.TestCorsRules(
                client,
                new List<CorsRule>()
                    {
                        ruleBasic,
                        ruleMinRequired,
                        ruleAllMethods,
                        ruleSingleExposedHeader,
                        ruleSingleExposedPrefixHeader
                    });


            // Test max number of rules + 1 (six)
            TestHelper.ExpectedException(
                () =>
                this.TestCorsRules(
                    client,
                    new List<CorsRule>()
                        {
                            ruleBasic,
                            ruleMinRequired,
                            ruleAllMethods,
                            ruleSingleExposedHeader,
                            ruleSingleExposedPrefixHeader,
                            ruleSingleAllowedHeader
                        }),
                "Services are limited to a maximum of five CORS rules.",
                HttpStatusCode.BadRequest,
                "InvalidXmlDocument");
        }

        [TestMethod]
        [Description("Test CORS with invalid values.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestCorsExpectedExceptions()
        {
            CorsRule ruleEmpty = new CorsRule();

            CorsRule ruleInvalidMaxAge = new CorsRule()
            {
                AllowedOrigins = new List<string>() { "www.xyz.com" },
                AllowedMethods = CorsHttpMethods.Get,
                MaxAgeInSeconds = -1
            };

            CloudTableClient client = GenerateCloudTableClient();

            TestHelper.ExpectedException<ArgumentException>(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleEmpty }), "Empty CORS Rules are not supported.");

            TestHelper.ExpectedException<ArgumentException>(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleInvalidMaxAge }),
                "MaxAgeInSeconds cannot have a value < 0.");
        }

        [TestMethod]
        [Description("Test CORS with a valid and invalid number of origin values sent to server.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestCorsMaxOrigins()
        {
            CorsRule ruleManyOrigins = new CorsRule() { AllowedMethods = CorsHttpMethods.Get, };

            // Add maximum number of allowed origins
            for (int i = 0; i < 64; i++)
            {
                ruleManyOrigins.AllowedOrigins.Add("www.xyz" + i + ".com");
            }

            CloudTableClient client = GenerateCloudTableClient();

            this.TestCorsRules(client, new List<CorsRule>() { ruleManyOrigins });

            ruleManyOrigins.AllowedOrigins.Add("www.xyz64.com");

            TestHelper.ExpectedException(
               () => this.TestCorsRules(client, new List<CorsRule>() { ruleManyOrigins }),
               "A maximum of 64 origins are allowed.",
               HttpStatusCode.BadRequest,
               "InvalidXmlNodeValue");
        }

        [TestMethod]
        [Description("Test CORS with a valid and invalid number of header values sent to server.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestCorsMaxHeaders()
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

            CloudTableClient client = GenerateCloudTableClient();

            this.TestCorsRules(client, new List<CorsRule>() { ruleManyHeaders });

            // Test with too many Exposed Headers (65)
            ruleManyHeaders.ExposedHeaders.Add("x-ms-meta-toomany");

            TestHelper.ExpectedException(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleManyHeaders }),
                "A maximum of 64 literal exposed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.ExposedHeaders.Remove("x-ms-meta-toomany");

            // Test with too many Allowed Headers (65)
            ruleManyHeaders.AllowedHeaders.Add("x-ms-meta-toomany");

            TestHelper.ExpectedException(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleManyHeaders }),
                "A maximum of 64 literal allowed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.AllowedHeaders.Remove("x-ms-meta-toomany");

            // Test with too many Exposed Prefixed Headers (three)
            ruleManyHeaders.ExposedHeaders.Add("x-ms-meta-toomany*");

            TestHelper.ExpectedException(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleManyHeaders }),
                "A maximum of two prefixed exposed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.ExposedHeaders.Remove("x-ms-meta-toomany*");

            // Test with too many Allowed Prefixed Headers (three)
            ruleManyHeaders.AllowedHeaders.Add("x-ms-meta-toomany*");

            TestHelper.ExpectedException(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleManyHeaders }),
                "A maximum of two prefixed allowed headers are allowed.",
                HttpStatusCode.BadRequest,
                "InvalidXmlNodeValue");

            ruleManyHeaders.AllowedHeaders.Remove("x-ms-meta-toomany*");
        }

        [TestMethod]
        [Description("Test Analytics Optional Properties Sync")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableTestAnalyticsOptionalPropertiesSync()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 6;
            props.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.Cors.CorsRules.Clear();

            client.SetServiceProperties(props);

            ServiceProperties newProps = new ServiceProperties(cors: new CorsProperties());

            newProps.Cors.CorsRules.Add(
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

            client.SetServiceProperties(newProps);

            // Test that the other properties did not change.
            props.Cors = newProps.Cors;
            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            newProps.Logging = props.Logging;
            newProps.HourMetrics = props.HourMetrics;
            newProps.MinuteMetrics = props.MinuteMetrics;
            newProps.Cors = null;
            client.SetServiceProperties(newProps);

            // Test that the CORS rules did not change.
            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }
        #endregion

        #region Test Helpers
        private void TestCorsRules(CloudTableClient client, IList<CorsRule> corsProps)
        {
            props.Cors.CorsRules.Clear();

            foreach (CorsRule rule in corsProps)
            {
                props.Cors.CorsRules.Add(rule);
            }

            client.SetServiceProperties(props);
            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }
        private static ServiceProperties DefaultServiceProperties()
        {
            ServiceProperties props = new ServiceProperties(new LoggingProperties(), new MetricsProperties(), new MetricsProperties(), new CorsProperties());

            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.Cors.CorsRules = new List<CorsRule>();

            return props;
        }
        #endregion
    }
}
