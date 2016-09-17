// <copyright file="TableEncryptionPolicy.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Represents an encryption policy for performing envelope encryption/decryption of entities in Azure tables.
    /// </summary>
    public class TableEncryptionPolicy
    {
        /// <summary>
        /// An object of type <see cref="IKey"/> that is used to wrap/unwrap the content key during encryption.
        /// </summary>
        public IKey Key { get; private set; }

        /// <summary>
        /// Gets or sets the key resolver used to select the correct key for decrypting existing table entities.
        /// </summary>
        /// <value>A resolver that returns an <see cref="IKeyResolver"/>, given a key ID.</value>
        public IKeyResolver KeyResolver { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEncryptionPolicy"/> class with the specified key and resolver.
        /// </summary>
        /// <param name="key">An object of type <see cref="IKey"/> that is used to wrap/unwrap the content encryption key.</param>
        /// <param name="keyResolver">The key resolver used to select the correct key for decrypting existing table entities.</param>
        /// <remarks>If the generated policy is to be used for encryption, users are expected to provide a key at the minimum.
        /// The absence of key will cause an exception to be thrown during encryption.<br/>
        /// If the generated policy is intended to be used for decryption, users can provide a key resolver. The client library will:<br/>
        /// 1. Invoke the key resolver, if specified, to get the key.<br/>
        /// 2. If resolver is not specified but a key is specified, the client library will match the key ID against the key and use the key.</remarks> 
        public TableEncryptionPolicy(IKey key, IKeyResolver keyResolver)
        {
            this.Key = key;
            this.KeyResolver = keyResolver;
        }

        /// <summary>
        /// Return an encrypted entity. This method is used for encrypting entity properties.
        /// </summary>
        internal Dictionary<string, EntityProperty> EncryptEntity(IDictionary<string, EntityProperty> properties, string partitionKey, string rowKey, Func<string, string, string, bool> encryptionResolver)
        {
            CommonUtility.AssertNotNull("properties", properties);

            // The Key should be set on the policy for encryption. Otherwise, throw an error.
            if (this.Key == null)
            {
                throw new InvalidOperationException(SR.KeyMissingError, null);
            }

            EncryptionData encryptionData = new EncryptionData();
            encryptionData.EncryptionAgent = new EncryptionAgent(Constants.EncryptionConstants.EncryptionProtocolV1, EncryptionAlgorithm.AES_CBC_256);
            encryptionData.KeyWrappingMetadata = new Dictionary<string, string>();
            encryptionData.KeyWrappingMetadata[Constants.EncryptionConstants.AgentMetadataKey] = Constants.EncryptionConstants.AgentMetadataValue;

            Dictionary<string, EntityProperty> encryptedProperties = new Dictionary<string, EntityProperty>();
            HashSet<string> encryptionPropertyDetailsSet = new HashSet<string>();

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
            {
                using (SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider())
#else
            using (AesManaged myAes = new AesManaged())
            {
                using (SHA256Managed sha256 = new SHA256Managed())
#endif
                {
                    encryptionData.ContentEncryptionIV = myAes.IV;

                    // Wrap always happens locally, irrespective of local or cloud key. So it is ok to call it synchronously.
                    Tuple<byte[], string> wrappedKey = CommonUtility.RunWithoutSynchronizationContext(() => this.Key.WrapKeyAsync(myAes.Key, null /* algorithm */, CancellationToken.None).Result);
                    encryptionData.WrappedContentKey = new WrappedKey(this.Key.Kid, wrappedKey.Item1, wrappedKey.Item2);

                    foreach (KeyValuePair<string, EntityProperty> kvp in properties)
                    {
                        if (encryptionResolver != null && encryptionResolver(partitionKey, rowKey, kvp.Key))
                        {
                            // Throw if users try to encrypt null properties. This could happen in the DynamicTableEntity case
                            // where a user adds a new property as follows - ent.Properties.Add("foo2", null);
                            if (kvp.Value == null)
                            {
                                throw new InvalidOperationException(SR.EncryptingNullPropertiesNotAllowed);
                            }

                            kvp.Value.IsEncrypted = true;
                        }

                        // IsEncrypted is set to true when either the EncryptPropertyAttribute is set on a property or when it is 
                        // specified in the encryption resolver or both.
                        if (kvp.Value != null && kvp.Value.IsEncrypted)
                        {
                            // Throw if users try to encrypt non-string properties.
                            if (kvp.Value.PropertyType != EdmType.String)
                            {
                                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SR.UnsupportedPropertyTypeForEncryption, kvp.Value.PropertyType));
                            }

                            byte[] columnIV = sha256.ComputeHash(CommonUtility.BinaryAppend(encryptionData.ContentEncryptionIV, Encoding.UTF8.GetBytes(string.Join(partitionKey, rowKey, kvp.Key))));
                            Array.Resize<byte>(ref columnIV, 16);
                            myAes.IV = columnIV;

                            using (ICryptoTransform transform = myAes.CreateEncryptor())
                            {
                                // Throw if users try to encrypt null properties. This could happen in the DynamicTableEntity or POCO
                                // case when the property value is null.
                                if (kvp.Value.IsNull)
                                {
                                    throw new InvalidOperationException(SR.EncryptingNullPropertiesNotAllowed);
                                }

                                byte[] src = Encoding.UTF8.GetBytes(kvp.Value.StringValue);
                                byte[] dest = transform.TransformFinalBlock(src, 0, src.Length);

                                // Store the encrypted properties as binary values on the service instead of base 64 encoded strings because strings are stored as a sequence of 
                                // WCHARs thereby further reducing the allowed size by half. During retrieve, it is handled by the response parsers correctly 
                                // even when the service does not return the type for JSON no-metadata.
                                encryptedProperties.Add(kvp.Key, new EntityProperty(dest));
                                encryptionPropertyDetailsSet.Add(kvp.Key);
                            }
                        }
                        else
                        {
                            encryptedProperties.Add(kvp.Key, kvp.Value);
                        }
                    }

                    // Encrypt the property details set and add it to entity properties.
                    byte[] metadataIV = sha256.ComputeHash(CommonUtility.BinaryAppend(encryptionData.ContentEncryptionIV, Encoding.UTF8.GetBytes(string.Join(partitionKey, rowKey, Constants.EncryptionConstants.TableEncryptionPropertyDetails))));
                    Array.Resize<byte>(ref metadataIV, 16);
                    myAes.IV = metadataIV;

                    using (ICryptoTransform transform = myAes.CreateEncryptor())
                    {
                        byte[] src = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(encryptionPropertyDetailsSet));
                        byte[] dest = transform.TransformFinalBlock(src, 0, src.Length);
                        encryptedProperties.Add(Constants.EncryptionConstants.TableEncryptionPropertyDetails, new EntityProperty(dest));
                    }
                }
            }

            // Add the key details to entity properties.
            encryptedProperties.Add(Constants.EncryptionConstants.TableEncryptionKeyDetails, new EntityProperty(JsonConvert.SerializeObject(encryptionData)));
            return encryptedProperties;
        }

        internal byte[] DecryptMetadataAndReturnCEK(string partitionKey, string rowKey, EntityProperty encryptionKeyProperty, EntityProperty propertyDetailsProperty, out EncryptionData encryptionData, out bool isJavaV1)
        {
            // Throw if neither the key nor the resolver are set.
            if (this.Key == null && this.KeyResolver == null)
            {
                throw new StorageException(SR.KeyAndResolverMissingError, null) { IsRetryable = false };
            }

            try
            {
                encryptionData = JsonConvert.DeserializeObject<EncryptionData>(encryptionKeyProperty.StringValue);
                EncryptionData encryptionDataCopy = encryptionData; // This is necessary because you cannot use "out" variables in a lambda.

                CommonUtility.AssertNotNull("ContentEncryptionIV", encryptionData.ContentEncryptionIV);
                CommonUtility.AssertNotNull("EncryptedKey", encryptionData.WrappedContentKey.EncryptedKey);

                // Throw if the encryption protocol on the entity doesn't match the version that this client library understands
                // and is able to decrypt.
                if (encryptionData.EncryptionAgent.Protocol != Constants.EncryptionConstants.EncryptionProtocolV1)
                {
                    throw new StorageException(SR.EncryptionProtocolVersionInvalid, null) { IsRetryable = false };
                }

                isJavaV1 = (encryptionData.EncryptionAgent.Protocol == Constants.EncryptionConstants.EncryptionProtocolV1) 
                            && ((encryptionData.KeyWrappingMetadata == null)
                                || (encryptionData.KeyWrappingMetadata.ContainsKey(Constants.EncryptionConstants.AgentMetadataKey) 
                                 && encryptionData.KeyWrappingMetadata[Constants.EncryptionConstants.AgentMetadataKey].Contains("Java")));

                byte[] contentEncryptionKey = null;

                // 1. Invoke the key resolver if specified to get the key. If the resolver is specified but does not have a
                // mapping for the key id, an error should be thrown. This is important for key rotation scenario.
                // 2. If resolver is not specified but a key is specified, match the key id on the key and and use it.
                // Calling UnwrapKeyAsync synchronously is fine because for the storage client scenario, unwrap happens
                // locally. No service call is made.
                if (this.KeyResolver != null)
                {
                    IKey keyEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => this.KeyResolver.ResolveKeyAsync(encryptionDataCopy.WrappedContentKey.KeyId, CancellationToken.None).Result);

                    CommonUtility.AssertNotNull("keyEncryptionKey", keyEncryptionKey);
                    contentEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => keyEncryptionKey.UnwrapKeyAsync(encryptionDataCopy.WrappedContentKey.EncryptedKey, encryptionDataCopy.WrappedContentKey.Algorithm, CancellationToken.None).Result);
                }
                else
                {
                    if (this.Key.Kid == encryptionData.WrappedContentKey.KeyId)
                    {
                        contentEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => this.Key.UnwrapKeyAsync(encryptionDataCopy.WrappedContentKey.EncryptedKey, encryptionDataCopy.WrappedContentKey.Algorithm, CancellationToken.None).Result);
                    }
                    else
                    {
                        throw new StorageException(SR.KeyMismatch, null) { IsRetryable = false };
                    }
                }

                // Decrypt the property details set and add it to entity properties.
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
                {
                    using (SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider())
#else
                using (AesManaged myAes = new AesManaged())
                {
                    using (SHA256Managed sha256 = new SHA256Managed())
#endif
                    {
                        // Here we are correcting for a bug in Java's v1 encryption.
                        // Java v1 constructed the IV as PK + RK + column name.  Other libraries use RK + PK + column name.
                        string IVString = isJavaV1 ? string.Concat(partitionKey, rowKey, Constants.EncryptionConstants.TableEncryptionPropertyDetails) : string.Concat(rowKey, partitionKey, Constants.EncryptionConstants.TableEncryptionPropertyDetails);
                        byte[] metadataIV = sha256.ComputeHash(CommonUtility.BinaryAppend(encryptionData.ContentEncryptionIV, Encoding.UTF8.GetBytes(IVString)));
                        Array.Resize<byte>(ref metadataIV, 16);
                        myAes.IV = metadataIV;
                        myAes.Key = contentEncryptionKey;

                        using (ICryptoTransform transform = myAes.CreateDecryptor())
                        {
                            byte[] src = propertyDetailsProperty.BinaryValue;
                            propertyDetailsProperty.BinaryValue = transform.TransformFinalBlock(src, 0, src.Length);
                        }
                    }
                }

                return contentEncryptionKey;
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
        /// Return a decrypted entity. This method is used for decrypting entity properties.
        /// </summary>
        internal Dictionary<string, EntityProperty> DecryptEntity(IDictionary<string, EntityProperty> properties, HashSet<string> encryptedPropertyDetailsSet, string partitionKey, string rowKey, byte[] contentEncryptionKey, EncryptionData encryptionData, bool isJavav1)
        {
            try
            {
                Dictionary<string, EntityProperty> decryptedProperties = new Dictionary<string, EntityProperty>();

                switch (encryptionData.EncryptionAgent.EncryptionAlgorithm)
                {
                    case EncryptionAlgorithm.AES_CBC_256:
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                        using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
                        {
                            using (SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider())
#else
                        using (AesManaged myAes = new AesManaged())
                        {
                            using (SHA256Managed sha256 = new SHA256Managed())
#endif
                            {
                                myAes.Key = contentEncryptionKey;

                                foreach (KeyValuePair<string, EntityProperty> kvp in properties)
                                {
                                    if (kvp.Key == Constants.EncryptionConstants.TableEncryptionKeyDetails || kvp.Key == Constants.EncryptionConstants.TableEncryptionPropertyDetails)
                                    {
                                        // Do nothing. Do not add to the result properties.
                                    }
                                    else if (encryptedPropertyDetailsSet.Contains(kvp.Key))
                                    {
                                        byte[] columnIV = sha256.ComputeHash(CommonUtility.BinaryAppend(encryptionData.ContentEncryptionIV, Encoding.UTF8.GetBytes(isJavav1 ? string.Concat(partitionKey, rowKey, kvp.Key) : string.Concat(rowKey, partitionKey, kvp.Key))));
                                        Array.Resize<byte>(ref columnIV, 16);
                                        myAes.IV = columnIV;

                                        byte[] src = kvp.Value.BinaryValue;
                                        using (ICryptoTransform transform = myAes.CreateDecryptor())
                                        {
                                            byte[] dest = transform.TransformFinalBlock(src, 0, src.Length);
                                            string destString = Encoding.UTF8.GetString(dest, 0, dest.Length);
                                            decryptedProperties.Add(kvp.Key, new EntityProperty(destString));
                                        }
                                    }
                                    else
                                    {
                                        decryptedProperties.Add(kvp.Key, kvp.Value);
                                    }
                                }
                            }
                        }

                        return decryptedProperties;

                    default:
                        throw new StorageException(SR.InvalidEncryptionAlgorithm, null) { IsRetryable = false };
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
    }
}
