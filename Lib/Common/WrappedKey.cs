// -----------------------------------------------------------------------------------------
// <copyright file="WrappedKey.cs" company="Microsoft">
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
    /// <summary>
    /// Represents the envelope key details stored on the service.
    /// </summary>
    internal class WrappedKey
    {
        /// <summary>
        /// Gets or sets the key identifier. This identifier is used to identify the key that is used to wrap/unwrap the content encryption key.
        /// </summary>
        /// <value>The key identifier string.</value>
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted content encryption key.
        /// </summary>
        /// <value>The encrypted content encryption key.</value>
        public byte[] EncryptedKey { get; set; }

        /// <summary>
        /// The algorithm used for wrapping.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WrappedKey"/> class using the specified key id, encrypted key and the algorithm.
        /// </summary>
        /// <param name="keyId">The key identifier string.</param>
        /// <param name="encryptedKey">The encrypted content encryption key.</param>
        /// <param name="algorithm">The algorithm used for wrapping.</param>
        public WrappedKey(string keyId, byte[] encryptedKey, string algorithm)
        {
            this.KeyId = keyId;
            this.EncryptedKey = encryptedKey;
            this.Algorithm = algorithm;
        }
    }
}
