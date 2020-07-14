//-----------------------------------------------------------------------
// <copyright file="BlobContainerEncryptionScopeOptions.cs" company="Microsoft">
//    Copyright 2019 Microsoft Corporation
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Storage.Blob
{
    /// <summary>
    /// Encryption scope options to be used when creating a container.
    /// </summary>
    public class BlobContainerEncryptionScopeOptions
    {
        /// <summary>
        /// Specifies the default encryption scope to set on the container and use for all future writes.
        /// </summary>
        public string DefaultEncryptionScope { get; set; }

        /// <summary>
        /// If true, prevents any request from specifying a different encryption scope than the scope set on the container.
        /// </summary>
        public bool PreventEncryptionScopeOverride { get; set; }
    }
}
