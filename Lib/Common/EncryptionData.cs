// <copyright file="EncryptionData.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the encryption data that is stored on the service.
    /// </summary>
    internal class EncryptionData
    {
        /// <summary>
        /// Gets or sets the wrapped key that is used to store the wrapping algorithm, key identifier and the encrypted key bytes.
        /// </summary>
        /// <value>A <see cref="WrappedContentKey"/> object that stores the wrapping algorithm, key identifier and the encrypted key bytes.</value>
        public WrappedKey WrappedContentKey { get; set; }

        /// <summary>
        /// Gets or sets the encryption agent that is used to identify the encryption protocol version and encryption algorithm.
        /// </summary>
        /// <value>The encryption agent.</value>
        public EncryptionAgent EncryptionAgent { get; set; }

        /// <summary>
        /// Gets or sets the content encryption IV.
        /// </summary>
        /// <value>The content encryption IV.</value>
        public byte[] ContentEncryptionIV { get; set; }

        /// <summary>
        /// Gets or sets the user-defined encryption metadata.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the encryption metadata as a collection of name-value pairs.</value>
        public IDictionary<string, string> KeyWrappingMetadata { get; set; }
    }
}
