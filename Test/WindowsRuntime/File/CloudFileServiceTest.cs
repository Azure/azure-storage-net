// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileServiceTest.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.File
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.File.Protocol;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [TestClass]
    public class CloudFileServiceTest : FileTestBase
    {
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

#if !FACADE_NETCORE
        [TestMethod]
        [Description("Test Set/Get Service Properties Async")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileTestAnalyticsRoundTripAsync()
        {
            FileServiceProperties props = DefaultServiceProperties();
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

            CloudFileClient client = GenerateCloudFileClient();
            await client.SetServicePropertiesAsync(props);

            TestHelper.AssertFileServicePropertiesAreEqual(props, await client.GetServicePropertiesAsync());
        }
#endif
        private static FileServiceProperties DefaultServiceProperties()
        {
            FileServiceProperties props = new FileServiceProperties(new MetricsProperties(), new MetricsProperties(), new CorsProperties());

            props.HourMetrics.MetricsLevel = MetricsLevel.None;
            props.HourMetrics.RetentionDays = null;
            props.HourMetrics.Version = "1.0";

            props.MinuteMetrics.MetricsLevel = MetricsLevel.None;
            props.MinuteMetrics.RetentionDays = null;
            props.MinuteMetrics.Version = "1.0";
#if !FACADE_NETCORE
            props.Cors.CorsRules = new List<CorsRule>();
#endif
            return props;
        }
    }
}