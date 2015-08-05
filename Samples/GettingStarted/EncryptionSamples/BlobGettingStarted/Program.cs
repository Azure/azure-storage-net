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

namespace BlobGettingStarted
{
    using System;
    using System.IO;
    using Microsoft.Azure.KeyVault;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Demonstrates how to use encryption with the Azure Blob service.
    /// </summary>
    public class Program
    {
        const string DemoContainer = "democontainer";

        static void Main(string[] args)
        {
            Console.WriteLine("Blob encryption sample");

            // Retrieve storage account information from connection string
            // How to create a storage connection string - https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/
            CloudStorageAccount storageAccount = EncryptionShared.Utility.CreateStorageAccountFromConnectionString();

            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(DemoContainer + Guid.NewGuid().ToString("N"));

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = new byte[size];

                Random rand = new Random();
                rand.NextBytes(buffer);

                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

                // Create the IKey used for encryption.
                RsaKey key = new RsaKey("private:key1");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(key, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                Console.WriteLine("Uploading the encrypted blob.");

                // Upload the encrypted contents to the blob.
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    blob.UploadFromStream(stream, size, null, uploadOptions, null);
                }

                // Download the encrypted blob.
                // For downloads, a resolver can be set up that will help pick the key based on the key id.
                LocalResolver resolver = new LocalResolver();
                resolver.Add(key);

                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                Console.WriteLine("Downloading the encrypted blob.");

                // Download and decrypt the encrypted contents from the blob.
                using (MemoryStream outputStream = new MemoryStream())
                {
                    blob.DownloadToStream(outputStream, null, downloadOptions, null);
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