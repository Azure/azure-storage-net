// -----------------------------------------------------------------------------------------
// <copyright file="TestConfigurations.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    public class TestConfigurations
    {
        public const string DefaultTestConfigFilePath = @"TestConfigurations.xml";
        public string TargetTenantName { get; private set; }
        public string TargetPremiumBlobTenantName { get; private set; }
        public string TargetOauthTenantName { get; private set; }
        public IEnumerable<TenantConfiguration> TenantConfigurations { get; private set; }

        public static TestConfigurations ReadFromXml(XDocument testConfigurationsDoc)
        {
            XElement testConfigurationsElement = testConfigurationsDoc.Element("TestConfigurations");
            return ReadFromXml(testConfigurationsElement);
        }

        public static TestConfigurations ReadFromXml(XElement testConfigurationsElement)
        {
            TestConfigurations result = new TestConfigurations();
            result.TargetTenantName = (string)testConfigurationsElement.Element("TargetTestTenant");
            result.TargetPremiumBlobTenantName = (string)testConfigurationsElement.Element("TargetPremiumBlobTenant");
            result.TargetOauthTenantName = (string)testConfigurationsElement.Element("TargetOauthTenant");

            List<TenantConfiguration> tenantConfigurationList = new List<TenantConfiguration>();
            foreach (XElement tenantConfigurationElement in testConfigurationsElement.Element("TenantConfigurations").Elements("TenantConfiguration"))
            {
                TenantConfiguration config = new TenantConfiguration
                {
                    TenantName = (string)tenantConfigurationElement.Element("TenantName"),
                    AccountName = (string)tenantConfigurationElement.Element("AccountName"),
                    AccountKey = (string)tenantConfigurationElement.Element("AccountKey"),
                    BlobServiceEndpoint = (string)tenantConfigurationElement.Element("BlobServiceEndpoint"),
                    FileServiceEndpoint = (string)tenantConfigurationElement.Element("FileServiceEndpoint"),
                    QueueServiceEndpoint = (string)tenantConfigurationElement.Element("QueueServiceEndpoint"),
                    TableServiceEndpoint = (string)tenantConfigurationElement.Element("TableServiceEndpoint"),
                    BlobServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("BlobServiceSecondaryEndpoint"),
                    FileServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("FileServiceSecondaryEndpoint"),
                    QueueServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("QueueServiceSecondaryEndpoint"),
                    TableServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("TableServiceSecondaryEndpoint"),
                    TenantType = (TenantType)Enum.Parse(typeof(TenantType), (string)tenantConfigurationElement.Element("TenantType"), true),
                    BlobSecurePortOverride = (string)tenantConfigurationElement.Element("BlobSecurePortOverride"),
                    FileSecurePortOverride = (string)tenantConfigurationElement.Element("FileSecurePortOverride"),
                    QueueSecurePortOverride = (string)tenantConfigurationElement.Element("QueueSecurePortOverride"),
                    TableSecurePortOverride = (string)tenantConfigurationElement.Element("TableSecurePortOverride"),
                    ActiveDirectoryApplicationId = (string)tenantConfigurationElement.Element("ActiveDirectoryApplicationId"),
                    ActiveDirectoryApplicationSecret = (string)tenantConfigurationElement.Element("ActiveDirectoryApplicationSecret"),
                    ActiveDirectoryTenantId = (string)tenantConfigurationElement.Element("ActiveDirectoryTenantId"),
                    ActiveDirectoryAuthEndpoint = (string)tenantConfigurationElement.Element("ActiveDirectoryAuthEndpoint")
                };

                tenantConfigurationList.Add(config);
            }

            result.TenantConfigurations = tenantConfigurationList;
            return result;
        }
    }
}
