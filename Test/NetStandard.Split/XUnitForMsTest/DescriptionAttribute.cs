// -----------------------------------------------------------------------------------------
// <copyright file="DescriptionAttribute.cs" company="Microsoft">
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

using System;

namespace Microsoft.VisualStudio.TestPlatform.UnitTestFramework
{
    /// <summary>
    /// Used to specify the description of the test. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the DescriptionAttribute class with a description.
        /// </summary>
        /// <param name="description">The description text.</param>
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Gets the description stored in this attribute.
        /// </summary>
        public string Description { get; private set; }
    }
}