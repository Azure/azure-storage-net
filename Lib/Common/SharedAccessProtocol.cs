// -----------------------------------------------------------------------------------------
// <copyright file="SharedAccessProtocol.cs" company="Microsoft">
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
    /// Specifies the set of possible signed protocols for a shared access account policy.
    /// </summary>
    public enum SharedAccessProtocol
    {
        /// <summary>
        /// Permission to use SAS only through https granted.
        /// </summary>
        HttpsOnly = 0x1,

        /// <summary>
        /// Permission to use SAS through https or http granted. Equivalent to not specifying any permission at all.
        /// </summary>
        HttpsOrHttp = 0x2
    }
}
