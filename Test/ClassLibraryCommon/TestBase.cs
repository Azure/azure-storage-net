﻿// -----------------------------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Microsoft">
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Auth;
    using System.Xml.Linq;

    [TestClass]
    public partial class TestBase
    {
        static TestBase()
        {
            try
            {
                XElement element = XElement.Load(TestConfigurations.DefaultTestConfigFilePath);
                TestConfigurations configurations = TestConfigurations.ReadFromXml(element);

                TestBase.Initialize(configurations);
            }
            catch (System.IO.FileNotFoundException)
            {
                throw new System.IO.FileNotFoundException("To run tests you need to supply a TestConfigurations.xml file with credentials in the Test/Common folder. Use TestConfigurationsTemplate.xml as a template.");
            }            
        }
    }
}
