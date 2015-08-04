//----------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------
namespace QueueGettingStarted
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Queue;
    using System;
    using System.IO;
    using System.Security.Cryptography;

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
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));
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

        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }
    }
}