// -----------------------------------------------------------------------------------------
// <copyright file="UtilityTests.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System.Collections.Generic;

    [TestClass]
    public class UtilityTests : TestBase
    {
        [TestMethod]
        [Description("Test to ensure HttpWebUtility.ParseQueryString works like HttpUtility.ParseQueryString")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ParseQueryStringTest()
        {
            IDictionary<string, string> dictionary = HttpWebUtility.ParseQueryString("?a=1&b&=c&d=&a=&d=2&&&d=&d=");
            Assert.AreEqual(3, dictionary.Count);
            Assert.AreEqual("b,c,,", dictionary[""]);
            Assert.AreEqual("1,", dictionary["a"]);
            Assert.AreEqual(",2,,", dictionary["d"]);

            dictionary = HttpWebUtility.ParseQueryString("a=1&b");
            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual("b", dictionary[""]);
            Assert.AreEqual("1", dictionary["a"]);

            dictionary = HttpWebUtility.ParseQueryString("");
            Assert.AreEqual(0, dictionary.Count);

            dictionary = HttpWebUtility.ParseQueryString("?");
            Assert.AreEqual(0, dictionary.Count);
        }

        [TestMethod]
        [Description("Ensure that the fields of a query string are read as case insensitive to be consistent with the service")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ParseQueryStringCaseSensitivityTest()
        {
            IDictionary<string, string> dictionary = HttpWebUtility.ParseQueryString("?SNAPSHOT=a&SnApShOt=b");
            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("a,b", dictionary["snapshot"]);
            Assert.AreEqual("a,b", dictionary["sNaPsHoT"]);
        }
    }
}
