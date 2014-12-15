// -----------------------------------------------------------------------------------------
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

#if ASPNET_K || WINDOWS_DESKTOP

namespace Microsoft.WindowsAzure.Storage
{
#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using System.IO;
    using System.Xml.Linq;

    /// <summary>
    /// Base test class
    /// </summary>
    [TestClass]
    public partial class TestBase
    {
        /// <summary>
        /// Initialize unit tests with configuration
        /// </summary>
        static TestBase()
        {   
            XElement element;
            using (var stream = new FileStream(TestConfigurations.DefaultTestConfigFilePath, FileMode.Open))
            {
                element = XElement.Load(stream);
            }

            TestConfigurations configurations = TestConfigurations.ReadFromXml(element);

            TestBase.Initialize(configurations);
        }
    }
}

#endif