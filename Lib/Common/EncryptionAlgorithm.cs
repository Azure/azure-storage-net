//-----------------------------------------------------------------------
// <copyright file="EncryptionAlgorithm.cs" company="Microsoft">
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
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage
{
    /// <summary>
    /// Specifies the encryption algorithm used to encrypt a resource.
    /// </summary>
    internal enum EncryptionAlgorithm
    {
        /// <summary>
        /// AES-CBC using a 256 bit key.
        /// </summary>
        AES_CBC_256
    }
}
