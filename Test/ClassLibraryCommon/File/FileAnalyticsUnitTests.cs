// -----------------------------------------------------------------------------------------
// <copyright file="FileAnalyticsUnitTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.File.Protocol;

    [TestClass]
    public class FileAnalyticsUnitTests : TestBase
    {
        #region Locals + Ctors

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        private static CloudFileClient client;
        private static FileServiceProperties props;
        private static FileServiceProperties startProperties = null;
        #endregion

        #region Additional test attributes

        /// <summary>
        /// You can use the following additional attributes as you write your tests:
        /// Use ClassInitialize to run code before running the first test in the class
        /// </summary>
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            client = GenerateCloudFileClient();
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

            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }

        /// <summary>
        /// Use TestCleanup to run code after each test has run
        /// </summary>
        [TestCleanup]
        public void MyTestCleanup()
        {
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }

            client.SetServiceProperties(startProperties);
        }

        #endregion

        #region Analytics RoundTrip

        #region Sync

        [TestMethod]
        [Description("Test Analytics Round Trip Sync")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestAnalyticsRoundTripSync()
        {
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

            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 8;

            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = 1;

            client.SetServiceProperties(props);

            TestHelper.AssertFileServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        #endregion

        #region APM

        [TestMethod]
        [Description("Test Analytics Round Trip APM")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestAnalyticsRoundTripAPM()
        {
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

            props.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            props.MinuteMetrics.RetentionDays = 8;

            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = 1;

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

            FileServiceProperties retrievedProps = null;
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

            TestHelper.AssertFileServicePropertiesAreEqual(props, retrievedProps);
        }

        #endregion

        #endregion

        #region Analytics Permutations

        [TestMethod]
        [Description("Test Analytics Disable Service Properties")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestAnalyticsDisable()
        {
            // These are set to defaults in the test initialization
            client.SetServiceProperties(props);

            // Check that the default service properties set in the Test Initialization were uploaded correctly
            TestHelper.AssertFileServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        [TestMethod]
        [Description("Test CORS with different rules.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestValidCorsRules()
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

            CloudFileClient client = GenerateCloudFileClient();

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
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestCorsExpectedExceptions()
        {
            CorsRule ruleEmpty = new CorsRule();

            CorsRule ruleInvalidMaxAge = new CorsRule()
                                             {
                                                 AllowedOrigins = new List<string>() { "www.xyz.com" },
                                                 AllowedMethods = CorsHttpMethods.Get,
                                                 MaxAgeInSeconds = -1
                                             };

            CloudFileClient client = GenerateCloudFileClient();

            TestHelper.ExpectedException<ArgumentException>(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleEmpty }), "Empty CORS Rules are not supported.");

            TestHelper.ExpectedException<ArgumentException>(
                () => this.TestCorsRules(client, new List<CorsRule>() { ruleInvalidMaxAge }),
                "MaxAgeInSeconds cannot have a value < 0.");
        }

        [TestMethod]
        [Description("Test CORS with a valid and invalid number of origin values sent to server.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestCorsMaxOrigins()
        {
            CorsRule ruleManyOrigins = new CorsRule() { AllowedMethods = CorsHttpMethods.Get, };

            // Add maximum number of allowed origins
            for (int i = 0; i < 64; i++)
            {
                ruleManyOrigins.AllowedOrigins.Add("www.xyz" + i + ".com");
            }

            CloudFileClient client = GenerateCloudFileClient();

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
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileTestCorsMaxHeaders()
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

            CloudFileClient client = GenerateCloudFileClient();

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
        #endregion

        #region Test Helpers
        private void TestCorsRules(CloudFileClient client, IList<CorsRule> corsProps)
        {
            props.Cors.CorsRules.Clear();

            foreach (CorsRule rule in corsProps)
            {
                props.Cors.CorsRules.Add(rule);
            }

            client.SetServiceProperties(props);
            TestHelper.AssertFileServicePropertiesAreEqual(props, client.GetServiceProperties());
        }

        private static FileServiceProperties DefaultServiceProperties()
        {
            FileServiceProperties props = new FileServiceProperties(new MetricsProperties(), new MetricsProperties(), new CorsProperties());
            props.MinuteMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;
            props.HourMetrics.Version = Constants.AnalyticsConstants.MetricsVersionV1;

            props.Cors.CorsRules = new List<CorsRule>();

            return props;
        }
        #endregion
    }
}
