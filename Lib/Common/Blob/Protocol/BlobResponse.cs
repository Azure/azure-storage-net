//-----------------------------------------------------------------------
// <copyright file="BlobResponse.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Auth;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Provides a set of helper methods for constructing a request against the Blob service.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        static class BlobResponse
    {
        internal static void ValidateCPKHeaders(HttpResponseMessage response, BlobRequestOptions options, bool upload)
        {
            if(options?.CustomerProvidedKey == null)
            {
                return;
            }

            BlobCustomerProvidedKey key = options.CustomerProvidedKey;

            // Get ms-encryption-key-sha256 header from the response
            string encryptionKeySHA256Hash = HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.ClientProvidedEncyptionKeyHash);

            if (!string.Equals(key.KeySHA256, encryptionKeySHA256Hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new StorageException(SR.ClientProvidedKeyBadHash);
            }

            // If this is an upload
            if(upload)
            {
                // Get ms-request-server-encrypted header from the response
                string serverRequestEncrypted = HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.ServerRequestEncrypted);

                // If header != "true"
                if (!string.Equals(Constants.HeaderConstants.TrueHeader, serverRequestEncrypted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new StorageException(SR.ClientProvidedKeyEncryptionFailure);
                }
            }
            else
            {
                // Get ms-server-encrypted header
                string serviceEncrypted = HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.ServerEncrypted);

                // If header != "true"
                if (!string.Equals(Constants.HeaderConstants.TrueHeader, serviceEncrypted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new StorageException(SR.ClientProvidedKeyEncryptionFailure);
                }
            }
        }
    }
}
