// -----------------------------------------------------------------------------------------
// <copyright file="CorsHttpMethods.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using System;

    /// <summary>
    /// HTTP methods that are supported by CORS.
    /// </summary>
    [Flags]
    public enum CorsHttpMethods
    {
        /// <summary>
        /// Represents no HTTP method in a CORS rule.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents the GET HTTP method in a CORS rule.
        /// </summary>
        Get = 0x1,

        /// <summary>
        /// Represents the HEAD HTTP method in a CORS rule.
        /// </summary>
        Head = 0x2,

        /// <summary>
        /// Represents the POST HTTP method in a CORS rule.
        /// </summary>
        Post = 0x4,

        /// <summary>
        /// Represents the PUT HTTP method in a CORS rule.
        /// </summary>
        Put = 0x8,

        /// <summary>
        /// Represents the DELETE HTTP method in a CORS rule.
        /// </summary>
        Delete = 0x10,

        /// <summary>
        /// Represents the TRACE HTTP method in a CORS rule.
        /// </summary>
        Trace = 0x20,

        /// <summary>
        /// Represents the OPTIONS HTTP method in a CORS rule.
        /// </summary>
        Options = 0x40,

        /// <summary>
        /// Represents the CONNECT HTTP method in a CORS rule.
        /// </summary>
        Connect = 0x80,

        /// <summary>
        /// Represents the MERGE HTTP method in a CORS rule.
        /// </summary>
        Merge = 0x100
    }
}
