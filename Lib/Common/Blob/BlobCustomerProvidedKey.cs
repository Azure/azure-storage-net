//-----------------------------------------------------------------------
// <copyright file="BlobCustomerProvidedKey.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Immutable wrapper for an encryption key to be used with client provided key encryption.
    /// </summary>
    public sealed class BlobCustomerProvidedKey
    {
        /// <summary>
        /// Base64 encoded string of the encryption key.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Base64 encoded string of the encryption key's SHA256 hash.
        /// </summary>
        public string KeySHA256 { get; private set; }

        /// <summary>
        /// The algorithm for Azure Blob Storage to encrypt with.
        /// Azure Blob Storage only offers AES256 encryption.
        /// </summary>
        public string EncryptionAlgorithm { get; private set; }

        /// <summary>
        /// Creates a new wrapper for a client provided key.
        /// </summary>
        /// <param name="key">The encryption key encoded as a base64 string.</param>
        public BlobCustomerProvidedKey(string key)
        {
            this.Key = key;
            this.EncryptionAlgorithm = Constants.AES256;
            using (var sha256 = SHA256.Create())
            {
                byte[] encodedHash = sha256.ComputeHash(Convert.FromBase64String(key));
                this.KeySHA256 = Convert.ToBase64String(encodedHash);
            }
        }

        /// <summary>
        /// Creates a new wrapper for a client provided key.
        /// </summary>
        /// <param name="key">The encryption key bytes.</param>
        public BlobCustomerProvidedKey(byte[] key)
        {
            this.Key = Convert.ToBase64String(key);
            this.EncryptionAlgorithm = Constants.AES256;
            using (var sha256 = SHA256.Create())
            {
                this.KeySHA256 = Convert.ToBase64String(sha256.ComputeHash(key));
            }
        }
    }
}
