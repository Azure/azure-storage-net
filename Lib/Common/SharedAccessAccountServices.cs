// -----------------------------------------------------------------------------------------
// <copyright file="SharedAccessAccountServices.cs" company="Microsoft">
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
    /// Specifies the set of possible signed services for a shared access account policy.
    /// </summary>
    [Flags]
    public enum SharedAccessAccountServices
    {
        /// <summary>
        /// No shared access granted.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Permission to access blob resources granted.
        /// </summary>
        Blob = 0x1,

        /// <summary>
        /// Permission to access file resources granted.
        /// </summary>
        File = 0x2,

        /// <summary>
        /// Permission to access queue resources granted.
        /// </summary>
        Queue = 0x4,

        /// <summary>
        /// Permission to access table resources granted.
        /// </summary>
        Table = 0x8
    }
}
