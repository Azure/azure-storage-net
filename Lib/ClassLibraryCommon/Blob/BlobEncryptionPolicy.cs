// <copyright file="BlobEncryptionPolicy.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;

    /// <summary>
    /// Represents an encryption policy for performing envelope encryption/decryption of Azure blobs.
    /// </summary>
    public sealed class BlobEncryptionPolicy
    {
        /// <summary>
        /// Gets and sets the blob encryption mode.
        /// </summary>
        /// <value>A <see cref="BlobEncryptionMode"/> enum value. </value>
        internal BlobEncryptionMode EncryptionMode { get; set; }

        /// <summary>
        /// An object of type <see cref="IKey"/> that is used to wrap/unwrap the content key during encryption.
        /// </summary>
        public IKey Key { get; private set; }

        /// <summary>
        /// Gets or sets the key resolver used to select the correct key for decrypting existing blobs.
        /// </summary>
        /// <value>A resolver that returns an <see cref="IKey"/>, given a key ID.</value>
        public IKeyResolver KeyResolver { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobEncryptionPolicy"/> class with the specified key and resolver.
        /// </summary>
        /// <param name="key">An object of type <see cref="IKey"/> that is used to wrap/unwrap the content key during encryption.</param>
        /// <param name="keyResolver">The key resolver used to select the correct key for decrypting existing blobs.</param>
        /// <remarks>If the generated policy is to be used for encryption, users are expected to provide a key at the minimum.
        /// The absence of key will cause an exception to be thrown during encryption.<br/>
        /// If the generated policy is intended to be used for decryption, users can provide a key resolver. The client library will:<br/>
        /// 1. Invoke the key resolver, if specified, to get the key.<br/>
        /// 2. If resolver is not specified but a key is specified, the client library will match the key ID against the key and use the key.</remarks> 
        public BlobEncryptionPolicy(IKey key, IKeyResolver keyResolver)
        {
            this.Key = key;
            this.KeyResolver = keyResolver;
            this.EncryptionMode = BlobEncryptionMode.FullBlob;
        }

        /// <summary>
        /// Return a reference to a <see cref="CryptoStream"/> object, given a user stream. This method is used for decrypting blobs.
        /// </summary>
        /// <param name="userProvidedStream">The output stream provided by the user.</param>
        /// <param name="metadata">A reference to a dictionary containing blob metadata that includes the encryption data.</param>
        /// <param name="transform">The <see cref="ICryptoTransform"/> function for the request.</param>
        /// <param name="requireEncryption">A boolean value to indicate whether the data read from the server should be encrypted.</param>
        /// <param name="iv">The iv to use if pre-buffered. Used only for range reads.</param>
        /// <param name="noPadding">Value indicating if the padding mode should be set or not.</param>
        /// <returns>A reference to a <see cref="CryptoStream"/> that will be written to.</returns>
        internal Stream DecryptBlob(Stream userProvidedStream, IDictionary<string, string> metadata, out ICryptoTransform transform, bool? requireEncryption, byte[] iv = null, bool noPadding = false)
        {
            CommonUtility.AssertNotNull("metadata", metadata);

            string encryptionDataString = null;

            // If encryption policy is set but the encryption metadata is absent, throw an exception.
            bool encryptionMetadataAvailable = metadata.TryGetValue(Constants.EncryptionConstants.BlobEncryptionData, out encryptionDataString);
            
            if (requireEncryption.HasValue && requireEncryption.Value && !encryptionMetadataAvailable)
            {
                throw new StorageException(SR.EncryptionDataNotPresentError, null) { IsRetryable = false };
            }

            try
            {
                if (encryptionDataString != null)
                {
                    BlobEncryptionData encryptionData = JsonConvert.DeserializeObject<BlobEncryptionData>(encryptionDataString);

                    CommonUtility.AssertNotNull("ContentEncryptionIV", encryptionData.ContentEncryptionIV);
                    CommonUtility.AssertNotNull("EncryptedKey", encryptionData.WrappedContentKey.EncryptedKey);

                    // Throw if the encryption protocol on the blob doesn't match the version that this client library understands
                    // and is able to decrypt.
                    if (encryptionData.EncryptionAgent.Protocol != Constants.EncryptionConstants.EncryptionProtocolV1)
                    {
                        throw new StorageException(SR.EncryptionProtocolVersionInvalid, null) { IsRetryable = false };
                    }

                    // Throw if neither the key nor the resolver are set.
                    if (this.Key == null && this.KeyResolver == null)
                    {
                        throw new StorageException(SR.KeyAndResolverMissingError, null) { IsRetryable = false };
                    }

                    byte[] contentEncryptionKey = null;

                    // 1. Invoke the key resolver if specified to get the key. If the resolver is specified but does not have a
                    // mapping for the key id, an error should be thrown. This is important for key rotation scenario.
                    // 2. If resolver is not specified but a key is specified, match the key id on the key and and use it.
                    // Calling UnwrapKeyAsync synchronously is fine because for the storage client scenario, unwrap happens
                    // locally. No service call is made.
                    if (this.KeyResolver != null)
                    {
                        IKey keyEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => this.KeyResolver.ResolveKeyAsync(encryptionData.WrappedContentKey.KeyId, CancellationToken.None).Result);

                        CommonUtility.AssertNotNull("KeyEncryptionKey", keyEncryptionKey);
                        contentEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => keyEncryptionKey.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result);
                    }
                    else
                    {
                        if (this.Key.Kid == encryptionData.WrappedContentKey.KeyId)
                        {
                            contentEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => this.Key.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result);
                        }
                        else
                        {
                            throw new StorageException(SR.KeyMismatch, null) { IsRetryable = false };
                        }
                    }

                    switch (encryptionData.EncryptionAgent.EncryptionAlgorithm)
                    {
                        case EncryptionAlgorithm.AES_CBC_256:
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
#else
                        using (AesManaged aesProvider = new AesManaged())
#endif
                            {
                                aesProvider.IV = iv != null ? iv : encryptionData.ContentEncryptionIV;
                                aesProvider.Key = contentEncryptionKey;

                                if (noPadding)
                                {
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                                    aesProvider.Padding = PaddingMode.None;
#endif
                                }

                                transform = aesProvider.CreateDecryptor();
                                return new CryptoStream(userProvidedStream, transform, CryptoStreamMode.Write);
                            }

                        default:
                            throw new StorageException(SR.InvalidEncryptionAlgorithm, null) { IsRetryable = false };
                    }
                }
                else
                {
                    transform = null;
                    return userProvidedStream;
                }
            }
            catch (JsonException ex)
            {
                throw new StorageException(SR.EncryptionMetadataError, ex) { IsRetryable = false };
            }
            catch (StorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StorageException(SR.DecryptionLogicError, ex) { IsRetryable = false };
            }
        }

        /// <summary>
        /// Internal helper method to wrap a user provided stream with the appropriate crypto stream.
        /// </summary>
        internal static Stream WrapUserStreamWithDecryptStream(CloudBlob blob, Stream userProvidedStream, BlobRequestOptions options, BlobAttributes attributes, bool rangeRead, out ICryptoTransform transform, long? endOffset = null, long? userSpecifiedLength = null, int discardFirst = 0, bool bufferIV = false)
        {
            if (!rangeRead)
            {
                // The user provided stream should be wrapped in a NonCloseableStream in order to 
                // avoid closing the user stream when the crypto stream is closed to flush the final decrypted 
                // block of data.
                Stream decryptStream = options.EncryptionPolicy.DecryptBlob(new NonCloseableStream(userProvidedStream), attributes.Metadata, out transform, options.RequireEncryption, null, blob.BlobType == BlobType.PageBlob);
                return decryptStream;
            }
            else
            {
                // Check if end offset lies in the last AES block and send this information over to set the correct padding mode.
                bool noPadding = blob.BlobType == BlobType.PageBlob || (endOffset.HasValue && endOffset.Value < attributes.Properties.Length - 16);
                transform = null;
                return new BlobDecryptStream(userProvidedStream, attributes.Metadata, userSpecifiedLength, discardFirst, bufferIV, noPadding, options.EncryptionPolicy, options.RequireEncryption);
            }
        }

        /// <summary>
        /// Set up the encryption context required for encrypting blobs.
        /// </summary>
        /// <param name="metadata">Reference to blob metadata object that is used to set the encryption materials.</param>
        /// <param name="noPadding">Value indicating if the padding mode should be set or not.</param>
        internal ICryptoTransform CreateAndSetEncryptionContext(IDictionary<string, string> metadata, bool noPadding)
        {
            CommonUtility.AssertNotNull("metadata", metadata);

            // The Key should be set on the policy for encryption. Otherwise, throw an error.
            if (this.Key == null)
            {
                throw new InvalidOperationException(SR.KeyMissingError, null);
            }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
            { 
                if (noPadding)
                {
                    aesProvider.Padding = PaddingMode.None;
                }
#else
            using (AesManaged aesProvider = new AesManaged())
            {
#endif
                BlobEncryptionData encryptionData = new BlobEncryptionData();
                encryptionData.EncryptionAgent = new EncryptionAgent(Constants.EncryptionConstants.EncryptionProtocolV1, EncryptionAlgorithm.AES_CBC_256);

                // Wrap always happens locally, irrespective of local or cloud key. So it is ok to call it synchronously.
                Tuple<byte[], string> wrappedKey = CommonUtility.RunWithoutSynchronizationContext(() => this.Key.WrapKeyAsync(aesProvider.Key, null /* algorithm */, CancellationToken.None).Result);
                encryptionData.WrappedContentKey = new WrappedKey(this.Key.Kid, wrappedKey.Item1, wrappedKey.Item2);
                encryptionData.EncryptionMode = this.EncryptionMode.ToString();
                encryptionData.KeyWrappingMetadata = new Dictionary<string, string>();
                encryptionData.KeyWrappingMetadata[Constants.EncryptionConstants.AgentMetadataKey] = Constants.EncryptionConstants.AgentMetadataValue;
                encryptionData.ContentEncryptionIV = aesProvider.IV;
                metadata[Constants.EncryptionConstants.BlobEncryptionData] = JsonConvert.SerializeObject(encryptionData);
                return aesProvider.CreateEncryptor();
            }
        }

        internal long GetEncryptedLength(long unencryptedLength, bool noPadding)
        {
            // Note that this will only work for the AES_CBC_256 alrogithm we're currently using - if we change algorithms, 
            // or give the user a choice, we'll need to change this code appropriately.
            if (noPadding)
            {
                return unencryptedLength;
            }
            else
            {
                return unencryptedLength + (16 - (unencryptedLength % 16));
            }
        }
    }
}
