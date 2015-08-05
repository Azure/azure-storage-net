// <copyright file="Program.cs" company="Microsoft">
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

namespace KeyRotation
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Demonstrates how to use encryption along with Azure Key Vault integration for the Azure Blob service.
    /// </summary>
    public class Program
    {
        const string DemoContainer = "democontainer";

        static void Main(string[] args)
        {
            Console.WriteLine("Blob encryption with Key Vault integration demonstrating key rotation from Key 1 to Key 2");
            Console.WriteLine();

            // Create two secrets and obtain their IDs. This is normally a one-time setup step.
            // Although it is possible to use keys (rather than secrets) stored in Key Vault, this prevents caching.
            // Therefore it is recommended to use secrets along with a caching resolver (see below).
            string keyID1 = EncryptionShared.KeyVaultUtility.SetUpKeyVaultSecret("KeyRotationSampleSecret1");
            string keyID2 = EncryptionShared.KeyVaultUtility.SetUpKeyVaultSecret("KeyRotationSampleSecret2");

            // Retrieve storage account information from connection string
            // How to create a storage connection string - https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/
            CloudStorageAccount storageAccount = EncryptionShared.Utility.CreateStorageAccountFromConnectionString();
            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(DemoContainer + Guid.NewGuid().ToString("N"));

            // Construct a resolver capable of looking up keys and secrets stored in Key Vault.
            KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(EncryptionShared.KeyVaultUtility.GetAccessToken);

            // Set up a caching resolver so the secrets can be cached on the client. This is the recommended usage
            // pattern since the throttling targets for Storage and Key Vault services are orders of magnitude
            // different.
            CachingKeyResolver cachingResolver = new CachingKeyResolver(2, cloudResolver);

            // Create key instances corresponding to the key IDs. This will cache the secrets.
            IKey cloudKey1 = cachingResolver.ResolveKeyAsync(keyID1, CancellationToken.None).GetAwaiter().GetResult();
            IKey cloudKey2 = cachingResolver.ResolveKeyAsync(keyID2, CancellationToken.None).GetAwaiter().GetResult();

            // We begin with cloudKey1, and a resolver capable of resolving and caching Key Vault secrets.
            BlobEncryptionPolicy encryptionPolicy = new BlobEncryptionPolicy(cloudKey1, cachingResolver);
            client.DefaultRequestOptions.EncryptionPolicy = encryptionPolicy;
            client.DefaultRequestOptions.RequireEncryption = true;

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer1 = new byte[size];
                byte[] buffer2 = new byte[size];

                Random rand = new Random();
                rand.NextBytes(buffer1);
                rand.NextBytes(buffer2);

                // Upload the first blob using the secret stored in Azure Key Vault.
                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob1");

                Console.WriteLine("Uploading Blob 1 using Key 1.");

                // Upload the encrypted contents to the first blob.
                using (MemoryStream stream = new MemoryStream(buffer1))
                {
                    blob.UploadFromStream(stream, size);
                }

                Console.WriteLine("Downloading and decrypting Blob 1.");

                // Download and decrypt the encrypted contents from the first blob.
                using (MemoryStream outputStream = new MemoryStream())
                {
                    blob.DownloadToStream(outputStream);
                }

                // At this point we will rotate our keys so new encrypted content will use the
                // second key. Note that the same resolver is used, as this resolver is capable
                // of decrypting blobs encrypted using either key.
                Console.WriteLine("Rotating the active encryption key to Key 2.");

                client.DefaultRequestOptions.EncryptionPolicy = new BlobEncryptionPolicy(cloudKey2, cachingResolver);

                // Upload the second blob using the key stored in Azure Key Vault.
                CloudBlockBlob blob2 = container.GetBlockBlobReference("blockblob2");

                Console.WriteLine("Uploading Blob 2 using Key 2.");

                // Upload the encrypted contents to the second blob.
                using (MemoryStream stream = new MemoryStream(buffer2))
                {
                    blob2.UploadFromStream(stream, size);
                }

                Console.WriteLine("Downloading and decrypting Blob 2.");

                // Download and decrypt the encrypted contents from the second blob.
                using (MemoryStream outputStream = new MemoryStream())
                {
                    blob2.DownloadToStream(outputStream);
                }

                // Here we download and re-upload the first blob. This has the effect of updating
                // the blob to use the new key.
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Console.WriteLine("Downloading and decrypting Blob 1.");
                    blob.DownloadToStream(memoryStream);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    Console.WriteLine("Re-uploading Blob 1 using Key 2.");
                    blob.UploadFromStream(memoryStream);
                }

                // For the purposes of demonstration, we now override the encryption policy to only recognize key 2.
                BlobEncryptionPolicy key2OnlyPolicy = new BlobEncryptionPolicy(cloudKey2, null);
                BlobRequestOptions key2OnlyOptions = new BlobRequestOptions()
                {
                    EncryptionPolicy = key2OnlyPolicy
                };

                Console.WriteLine("Downloading and decrypting Blob 1.");

                // The first blob can still be decrypted because it is using the second key.
                using (MemoryStream outputStream = new MemoryStream())
                {
                    blob.DownloadToStream(outputStream, options: key2OnlyOptions);
                }

                Console.WriteLine("Press enter key to exit");
                Console.ReadLine();
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
    }
}
