﻿// -----------------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    public class TestConfigurations
    {
        public const string DefaultTestConfigFilePath = @"TestConfigurations.xml";
        public string TargetTenantName { get; private set; }
        public string TargetPremiumBlobTenantName { get; private set; }
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

            List<TenantConfiguration> tenantConfigurationList = new List<TenantConfiguration>();
            foreach (XElement tenantConfigurationElement in testConfigurationsElement.Element("TenantConfigurations").Elements("TenantConfiguration"))
            {
                TenantConfiguration config = new TenantConfiguration();
                config.TenantName = (string)tenantConfigurationElement.Element("TenantName");
                config.AccountName = (string)tenantConfigurationElement.Element("AccountName");
                config.AccountKey = (string)tenantConfigurationElement.Element("AccountKey");
                config.BlobServiceEndpoint = (string)tenantConfigurationElement.Element("BlobServiceEndpoint");
                config.FileServiceEndpoint = (string)tenantConfigurationElement.Element("FileServiceEndpoint");
                config.QueueServiceEndpoint = (string)tenantConfigurationElement.Element("QueueServiceEndpoint");
                config.TableServiceEndpoint = (string)tenantConfigurationElement.Element("TableServiceEndpoint");
                config.BlobServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("BlobServiceSecondaryEndpoint");
                config.FileServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("FileServiceSecondaryEndpoint");
                config.QueueServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("QueueServiceSecondaryEndpoint");
                config.TableServiceSecondaryEndpoint = (string)tenantConfigurationElement.Element("TableServiceSecondaryEndpoint");
                config.TenantType = (TenantType)Enum.Parse(typeof(TenantType), (string)tenantConfigurationElement.Element("TenantType"), true);
                config.BlobSecurePortOverride = (string)tenantConfigurationElement.Element("BlobSecurePortOverride");
                config.FileSecurePortOverride = (string)tenantConfigurationElement.Element("FileSecurePortOverride");
                config.QueueSecurePortOverride = (string)tenantConfigurationElement.Element("QueueSecurePortOverride");
                config.TableSecurePortOverride = (string)tenantConfigurationElement.Element("TableSecurePortOverride");
                tenantConfigurationList.Add(config);
            }

            result.TenantConfigurations = tenantConfigurationList;
            return result;
        }
    }
}
