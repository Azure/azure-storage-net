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

    [TestClass]
    public class NavigationHelperTests : TestBase
    {
        [TestMethod]
        [Description("GetAccountNameFromUri with container and blob")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriBlob()
        {
            // Arrange
            string uriString = "https://account.blob.core.windows.net/container/blob";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri with container and blob and query string")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriBlobQuery()
        {
            // Arrange
            string uriString = "https://account.blob.core.windows.net/container/blob?comp";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri with container")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriContainer()
        {
            // Arrange
            string uriString = "https://account.blob.core.windows.net/container";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri with container and query string")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriContainerQueryString()
        {
            // Arrange
            string uriString = "https://account.blob.core.windows.net/container?comp";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri with account")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriAccount()
        {
            // Arrange
            string uriString = "https://account.blob.core.windows.net/";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri with account and query string")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriAccountQuery()
        {
            // Arrange
            string uriString = "https://account.blob.core.windows.net?comp";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri IP Style URL account")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriIPStyleAccount()
        {
            // Arrange
            string uriString = "https://105.232.1.23/account";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), null);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri IP Style URL account and query string")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriIPStyleAccountQuery()
        {
            // Arrange
            string uriString = "https://105.232.1.23/account?comp=list";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), true);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri IP Style URL with container")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriIPStyleContainer()
        {
            // Arrange
            string uriString = "https://105.232.1.23/account/container";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), true);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri IP Style URL with container and query string")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriIPStyleContainerQuery()
        {
            // Arrange
            string uriString = "https://105.232.1.23/account/container?comp";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), true);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri IP Style URL with blob")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriIPStyleBlob()
        {
            // Arrange
            string uriString = "https://105.232.1.23/account/container/blob";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), true);

            // Assert
            Assert.AreEqual("account", accountName);
        }

        [TestMethod]
        [Description("GetAccountNameFromUri IP Style URL with blob and query")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void GetAccountNameFromUriIPStyleBlobQuery()
        {
            // Arrange
            string uriString = "https://105.232.1.23/account/container/blob?comp";

            // Act
            string accountName = NavigationHelper.GetAccountNameFromUri(new Uri(uriString), true);

            // Assert
            Assert.AreEqual("account", accountName);
        }
    }
}
