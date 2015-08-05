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

namespace QueueGettingStarted
{
    using System;
    using Microsoft.Azure.KeyVault;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;

    /// <summary>
    /// Demonstrates how to use encryption with the Azure Queue service.
    /// </summary>
    public class Program
    {
        const string DemoQueue = "demoqueue";

        static void Main(string[] args)
        {
            Console.WriteLine("Queue encryption sample");

            // Retrieve storage account information from connection string
            // How to create a storage connection string - https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/
            CloudStorageAccount storageAccount = EncryptionShared.Utility.CreateStorageAccountFromConnectionString();
            CloudQueueClient client = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(DemoQueue + Guid.NewGuid().ToString("N"));

            try
            {
                queue.Create();

                // Create the IKey used for encryption.
                RsaKey key = new RsaKey("private:key1");           

                // Create the encryption policy to be used for insert and update.
                QueueEncryptionPolicy insertPolicy = new QueueEncryptionPolicy(key, null);

                // Set the encryption policy on the request options.
                QueueRequestOptions insertOptions = new QueueRequestOptions() { EncryptionPolicy = insertPolicy };

                string messageStr = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageStr);

                // Add message
                Console.WriteLine("Inserting the encrypted message.");
                queue.AddMessage(message, null, null, insertOptions, null);

                // For retrieves, a resolver can be set up that will help pick the key based on the key id.
                LocalResolver resolver = new LocalResolver();
                resolver.Add(key);

                QueueEncryptionPolicy retrPolicy = new QueueEncryptionPolicy(null, resolver);
                QueueRequestOptions retrieveOptions = new QueueRequestOptions() { EncryptionPolicy = retrPolicy };

                // Retrieve message
                Console.WriteLine("Retrieving the encrypted message.");
                CloudQueueMessage retrMessage = queue.GetMessage(null, retrieveOptions, null);

                // Update message
                Console.WriteLine("Updating the encrypted message.");
                string updatedMessage = Guid.NewGuid().ToString("N");
                retrMessage.SetMessageContent(updatedMessage);
                queue.UpdateMessage(retrMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Content | MessageUpdateFields.Visibility, insertOptions, null);

                // Retrieve updated message
                Console.WriteLine("Retrieving the updated encrypted message.");
                retrMessage = queue.GetMessage(null, retrieveOptions, null);

                Console.WriteLine("Press enter key to exit");
                Console.ReadLine();
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }
    }
}