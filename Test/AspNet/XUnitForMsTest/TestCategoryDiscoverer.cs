// -----------------------------------------------------------------------------------------
// <copyright file="TestCategoryDiscoverer.cs" company="Microsoft">
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

using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.TestPlatform.UnitTestFramework
{
    /// <summary>
    /// Discover the Traits from classes that implement TestCategoryAttribute
    /// </summary>
    public class TestCategoryDiscoverer : ITraitDiscoverer
    {
        /// <summary>
        /// Gets the trait values from the trait attribute.
        /// </summary>
        /// <param name="traitAttribute">The trait attribute containing the trait values.</param>
        /// <returns>The trait values</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            IEnumerator<object> enumerator = traitAttribute.GetConstructorArguments().GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return new KeyValuePair<string, string>(enumerator.Current.ToString(), "");
            }
        }
    }
}