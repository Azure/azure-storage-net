//-----------------------------------------------------------------------
// <copyright file="BlobRequest.cs" company="Microsoft">
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
        static class BlobRequest
    {
        /// <summary>
        /// Writes a collection of shared access policies to the specified stream in XML format.
        /// </summary>
        /// <param name="sharedAccessPolicies">A collection of shared access policies.</param>
        /// <param name="outputStream">An output stream.</param>
        public static void WriteSharedAccessIdentifiers(SharedAccessBlobPolicies sharedAccessPolicies, Stream outputStream)
        {
            Request.WriteSharedAccessIdentifiers(
                sharedAccessPolicies,
                outputStream,
                (policy, writer) =>
                {
                    writer.WriteElementString(
                        Constants.Start,
                        SharedAccessSignatureHelper.GetDateTimeOrEmpty(policy.SharedAccessStartTime));
                    writer.WriteElementString(
                        Constants.Expiry,
                        SharedAccessSignatureHelper.GetDateTimeOrEmpty(policy.SharedAccessExpiryTime));
                    writer.WriteElementString(
                        Constants.Permission,
                        SharedAccessBlobPolicy.PermissionsToString(policy.Permissions));
                });
        }

        /// <summary>
        /// Writes the body of the block list to the specified stream in XML format.
        /// </summary>
        /// <param name="blocks">An enumerable collection of <see cref="PutBlockListItem"/> objects.</param>
        /// <param name="outputStream">The stream to which the block list is written.</param>
        public static void WriteBlockListBody(IEnumerable<PutBlockListItem> blocks, Stream outputStream)
        {
            CommonUtility.AssertNotNull("blocks", blocks);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            using (XmlWriter writer = XmlWriter.Create(outputStream, settings))
            {
                writer.WriteStartElement(Constants.BlockListElement);

                foreach (PutBlockListItem block in blocks)
                {
                    if (block.SearchMode == BlockSearchMode.Committed)
                    {
                        writer.WriteElementString(Constants.CommittedElement, block.Id);
                    }
                    else if (block.SearchMode == BlockSearchMode.Uncommitted)
                    {
                        writer.WriteElementString(Constants.UncommittedElement, block.Id);
                    }
                    else if (block.SearchMode == BlockSearchMode.Latest)
                    {
                        writer.WriteElementString(Constants.LatestElement, block.Id);
                    }
                }

                writer.WriteEndDocument();
            }
        }
        internal static void ApplyCustomerProvidedKey(StorageRequestMessage request, BlobCustomerProvidedKey customerProvidedKey, bool isSource)
        {
            if ((null == customerProvidedKey))
            {
                return;
            }

            if (isSource)
            {
                request.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKeySource, customerProvidedKey.Key);
                request.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKeyHashSource, customerProvidedKey.KeySHA256);
                request.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKeyAlgorithmSource, customerProvidedKey.EncryptionAlgorithm);
            }
            else
            {
                request.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKey, customerProvidedKey.Key);
                request.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKeyHash, customerProvidedKey.KeySHA256);
                request.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionAlgorithm, customerProvidedKey.EncryptionAlgorithm);
            }
        }

        internal static void ApplyCustomerProvidedKeyOrEncryptionScope(StorageRequestMessage request, BlobRequestOptions options, bool isSource)
        {
            var customerProvidedKey = options?.CustomerProvidedKey;
            var encryptionScope = options?.EncryptionScope;

            if (null != customerProvidedKey)
            {
                ApplyCustomerProvidedKey(request, customerProvidedKey, isSource);
            }
            else if (null != encryptionScope)
            {
                request.Headers.Add(Constants.HeaderConstants.EncryptionScopeHeader, encryptionScope);
            }
        }

        internal static void VerifyHttpsCustomerProvidedKey(Uri uri, BlobRequestOptions options)
        {
            if (options?.CustomerProvidedKey != null && !String.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.ClientProvidedKeyRequiresHttps);
            }
        }
    }
}
