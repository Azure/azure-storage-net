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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;

    [TestClass]
    public class BlobAnalyticsUnitTests : TestBase
    {
        #region Locals + Ctors

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        private static CloudBlobClient client;
        private static ServiceProperties props;
        private static ServiceProperties startProperties = null;
        #endregion

        #region Additional test attributes
        
        /// <summary>
        /// You can use the following additional attributes as you write your tests:
        /// Use ClassInitialize to run code before running the first test in the class
        /// </summary>
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            client = GenerateCloudBlobClient();
            startProperties = client.GetServiceProperties();
        }

        /// <summary>
        /// Use ClassCleanup to run code after all tests in a class have run
        /// </summary>
        [ClassCleanup]
        public static void MyClassCleanup()
        {
            client.SetServiceProperties(startProperties);
        }

        /// <summary>
        /// Use TestInitialize to run code before running each test 
        /// </summary>
        [TestInitialize]
        public void MyTestInitialize()
        {
            props = DefaultServiceProperties();
            
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        /// <summary>
        /// Use TestCleanup to run code after each test has run
        /// </summary>
        [TestCleanup]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Analytics RoundTrip

        #region Sync

        [TestMethod]
        [Description("Test Analytics Round Trip Sync")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsRoundTripSync()
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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsRoundTripAPM()
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

#region TASK
        [TestMethod]
        [Description("Test Analytics Round Trip Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsRoundTripTask()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            props.Logging.RetentionDays = 5;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 6;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props).Wait();

            // Wait for analytics server to update
            Thread.Sleep(60 * 1000);
            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync().Result);
        }

        [TestMethod]
        [Description("Test Blob GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobGetSetServicePropertiesTask()
        {
            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 8;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 8;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props).Wait();

            ServiceProperties actual = client.GetServicePropertiesAsync().Result;

            TestHelper.AssertServicePropertiesAreEqual(props, actual);
        }

        [TestMethod]
        [Description("Test Blob GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void BlobGetSetServicePropertiesCancellationTokenTask()
        {
            CancellationToken cancellationToken = CancellationToken.None;

            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 9;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 9;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props, cancellationToken).Wait();

            ServiceProperties actual = client.GetServicePropertiesAsync(cancellationToken).Result;

            TestHelper.AssertServicePropertiesAreEqual(props, actual);
        }

        [TestMethod]
        [Description("Test Blob GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void BlobGetSetServicePropertiesRequestOptionsOperationContextTask()
        {
            BlobRequestOptions requestOptions = new BlobRequestOptions();
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
        [Description("Test Blob GetServiceProperties and SetServiceProperties - Task")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void BlobGetSetServicePropertiesRequestOptionsOperationContextCancellationTokenTask()
        {
            BlobRequestOptions requestOptions = new BlobRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            props.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write | LoggingOperations.Delete;
            props.Logging.RetentionDays = 11;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;
            props.HourMetrics.MetricsLevel = MetricsLevel.Service;
            props.HourMetrics.RetentionDays = 11;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServicePropertiesAsync(props, requestOptions, operationContext, cancellationToken).Wait();

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServicePropertiesAsync(requestOptions, operationContext).Result);
        }
#endregion

        #endregion

        #region Analytics Permutations

        [TestMethod]
        [Description("Test Analytics Disable Service Properties")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsDisable()
        {
            // These are set to defaults in the test initialization
            client.SetServiceProperties(props);

            // Check that the default service properties set in the Test Initialization were uploaded correctly
            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Default Service Version")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsDefaultServiceVersion()
        {
            props.DefaultServiceVersion = "2009-09-19";
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            props.DefaultServiceVersion = "2011-08-18";
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            props.DefaultServiceVersion = "2012-02-12";
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            props.DefaultServiceVersion = "2013-08-15";
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Logging Operations")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsLoggingOperations()
        {
            // None
            props.Logging.LoggingOperations = LoggingOperations.None;
            props.Logging.RetentionDays = null;
            props.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());

            // All
            props.Logging.LoggingOperations = LoggingOperations.All;
            client.SetServiceProperties(props);

            TestHelper.AssertServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test Analytics Metrics Level")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsMetricsLevel()
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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsMinuteMetricsLevel()
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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestAnalyticsRetentionPolicies()
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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestValidCorsRules()
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

            CloudBlobClient client = GenerateCloudBlobClient();

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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestCorsExpectedExceptions()
        {
            CorsRule ruleEmpty = new CorsRule();

            CorsRule ruleInvalidMaxAge = new CorsRule()
                                             {
                                                 AllowedOrigins = new List<string>() { "www.xyz.com" },
                                                 AllowedMethods = CorsHttpMethods.Get,
                                                 MaxAgeInSeconds = -1
                                             };

            CloudBlobClient client = GenerateCloudBlobClient();

            TestHelper.ExpectedException<ArgumentException>(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleEmpty }), "Empty CORS Rules are not supported.");

            TestHelper.ExpectedException<ArgumentException>(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleInvalidMaxAge }),
                "MaxAgeInSeconds cannot have a value < 0.");
        }

        [TestMethod]
        [Description("Test CORS with a valid and invalid number of origin values sent to server.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestCorsMaxOrigins()
        {
            CorsRule ruleManyOrigins = new CorsRule() { AllowedMethods = CorsHttpMethods.Get, };

            // Add maximum number of allowed origins
            for (int i = 0; i < 64; i++)
            {
                ruleManyOrigins.AllowedOrigins.Add("www.xyz" + i + ".com");
            }

            CloudBlobClient client = GenerateCloudBlobClient();

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
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobTestCorsMaxHeaders()
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

            CloudBlobClient client = GenerateCloudBlobClient();

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
        public void CloudBlobTestAnalyticsOptionalPropertiesSync()
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


        [TestMethod]
        [Description("Test Blob SetServiceProperties with empty Logging and Metrics properties")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestSetServicePropertiesWithoutMetricsAndLoggingProperties()
        {
            ServiceProperties serviceProperties = new ServiceProperties(cors: new CorsProperties());
            Microsoft.WindowsAzure.Storage.Shared.Protocol.CorsRule rule = new Microsoft.WindowsAzure.Storage.Shared.Protocol.CorsRule();
            rule.AllowedHeaders.Add("x-ms-meta-xyz");
            rule.AllowedHeaders.Add("x-ms-meta-data*");
            rule.AllowedMethods = CorsHttpMethods.Get | CorsHttpMethods.Put;
            rule.ExposedHeaders.Add("x-ms-meta-source*");
            rule.AllowedOrigins.Add("*");
            rule.AllowedMethods = CorsHttpMethods.Get;
            serviceProperties.Cors.CorsRules.Add(rule);

            client.SetServiceProperties(serviceProperties);
        }

        [TestMethod]
        [Description("Test Blob SetServiceProperties with empty cors properties")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestSetServicePropertiesWithoutCorsProperties()
        {
            ServiceProperties serviceProperties = new ServiceProperties(new LoggingProperties(), new MetricsProperties(), new MetricsProperties());

            serviceProperties.Logging.LoggingOperations = LoggingOperations.Read | LoggingOperations.Write;
            serviceProperties.Logging.RetentionDays = 5;
            serviceProperties.Logging.Version = Constants.AnalyticsConstants.LoggingVersionV1;

            serviceProperties.HourMetrics.MetricsLevel = MetricsLevel.Service;
            serviceProperties.HourMetrics.RetentionDays = 6;
            serviceProperties.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            serviceProperties.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            serviceProperties.MinuteMetrics.RetentionDays = 6;
            serviceProperties.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            client.SetServiceProperties(serviceProperties);
        }

        [TestMethod]
        [Description("Test Blob SetServiceProperties with no properties set")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TestSetServicePropertiesWithNoProperties()
        {
            ServiceProperties serviceProperties = new ServiceProperties();
            TestHelper.ExpectedException<ArgumentException>(() => client.SetServiceProperties(serviceProperties), "At least one service property needs to be non-null for SetServiceProperties API.");
        }

        #region Test Helpers
        private void TestCorsRules(CloudBlobClient client, IList<CorsRule> corsProps)
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

            props.DefaultServiceVersion = "2013-08-15";

            return props;
        }
        #endregion
    }
}
