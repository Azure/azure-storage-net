// -----------------------------------------------------------------------------------------
// <copyright file="DoesServiceRequestAttribute.cs" company="Microsoft">
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

    /// <summary>
    /// Specifies that the method will make one or more requests to the storage service. 
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class DoesServiceRequestAttribute : System.Attribute
    {
    }
}
