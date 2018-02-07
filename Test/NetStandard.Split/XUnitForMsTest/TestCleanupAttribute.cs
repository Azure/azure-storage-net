// -----------------------------------------------------------------------------------------
// <copyright file="TestCleanupAttribute.cs" company="Microsoft">
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
    /// Identifies a method that contains code that must be used after the test has run and 
    /// to free resources obtained by all the tests in the test class. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestCleanupAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the TestCleanupAttribute class.
        /// </summary>
        public TestCleanupAttribute() { }
    }
}