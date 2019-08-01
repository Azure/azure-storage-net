// -----------------------------------------------------------------------------------------
// <copyright file="WriteToSyncTests.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Core
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System.Xml.Linq;
    using System.Linq;
    using System.Collections.Generic;

    [TestClass]
    public class ServicePropertiesTests : TestBase
    {
        [TestMethod]
        [Description("ReadCorsPropertiesFromXml will return nothing when nothing is passed as xml element")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ReadCorsPropertiesFromXmlReturnsNothingWhenNothingProvided()
        {
            // Arrange

            // Act
            var result = ServiceProperties.ReadCorsPropertiesFromXml(null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        [Description("ReadCorsPropertiesFromXml should parse a simple CORSE rule configuration")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ReadCorsPropertiesFromXmlParseValidConfiguration()
        {
            ReadCorsPropertiesFromXmlTester("*", "GET", CorsHttpMethods.Get, "*", "*", 60);
        }

        [TestMethod]
        [Description("ReadCorsPropertiesFromXml should allow for multiple methods")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ReadCorsPropertiesFromXmlAllowMultipleMethods()
        {
            // Exclude PATCH as this is not part of the CorsHttpMethods
            ReadCorsPropertiesFromXmlTester("*", "DELETE,GET,HEAD,MERGE,POST,OPTIONS,PUT", 
                CorsHttpMethods.Delete | CorsHttpMethods.Get | CorsHttpMethods.Head | CorsHttpMethods.Merge | CorsHttpMethods.Post | CorsHttpMethods.Options | CorsHttpMethods.Put, 
                "*", "*", 60);
        }

        private void ReadCorsPropertiesFromXmlTester(string origins, string methods, CorsHttpMethods expectedMethods, string headers, string exposedHeaders, int maxAgeInSeconds)
        { 

            // Arrange
            var rules = XDocument.Parse($"<root><{ServiceProperties.CorsRuleName}>" +
                $"<{ServiceProperties.AllowedOriginsName}>{origins}</{ServiceProperties.AllowedOriginsName}>" +
                $"<{ServiceProperties.AllowedMethodsName}>{methods}</{ServiceProperties.AllowedMethodsName}>" +
                $"<{ServiceProperties.AllowedHeadersName}>{headers}</{ServiceProperties.AllowedHeadersName}>" +
                $"<{ServiceProperties.ExposedHeadersName}>{exposedHeaders}</{ServiceProperties.ExposedHeadersName}>" +
                $"<{ServiceProperties.MaxAgeInSecondsName}>{maxAgeInSeconds}</{ServiceProperties.MaxAgeInSecondsName}>" +
                $"</{ServiceProperties.CorsRuleName}></root>").Root;

            // Act
            var result = ServiceProperties.ReadCorsPropertiesFromXml(rules);

            // Assert
            var rule = result.CorsRules.FirstOrDefault();
            Assert.IsNotNull(rule);

            AreListEqual(origins.Split(','), rule.AllowedOrigins, "AllowedOrigins");
            Assert.AreEqual(expectedMethods, rule.AllowedMethods);
            AreListEqual(headers.Split(','), rule.AllowedHeaders, "AllowedHeaders");
            AreListEqual(exposedHeaders.Split(','), rule.ExposedHeaders, "ExposedHeaders");
            Assert.AreEqual(maxAgeInSeconds, rule.MaxAgeInSeconds);
        }

        private void AreListEqual<T>(IList<T> expected, IList<string> actual, string message, params object[] parameters)
        {
            Assert.AreEqual(expected.Count(), actual.Count(), $"{message} -> different size", parameters);
            var enumerateExpected = expected.GetEnumerator();
            var enumerateActual = actual.GetEnumerator();
            while (enumerateExpected.MoveNext() && enumerateActual.MoveNext())
            {
                Assert.AreEqual(enumerateExpected.Current, enumerateActual.Current, $"{message} -> different value", parameters);
            }
        }

    }
}