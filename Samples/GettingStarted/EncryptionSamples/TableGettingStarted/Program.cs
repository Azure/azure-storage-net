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
namespace TableGettingStartedUsingResolver
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// Demonstrates how to use encryption with the Azure Table service.
    /// </summary>
    public class Program
    {
        const string DemoTable = "demotable";

        static void Main(string[] args)
        {
            Console.WriteLine("Table encryption sample");

            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudTableClient client = storageAccount.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(DemoTable + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                // Create the IKey used for encryption.
                RsaKey key = new RsaKey("private:key1");           

                DynamicTableEntity ent = new DynamicTableEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
                ent.Properties.Add("EncryptedProp1", new EntityProperty(string.Empty));
                ent.Properties.Add("EncryptedProp2", new EntityProperty("bar"));
                ent.Properties.Add("NotEncryptedProp", new EntityProperty(1234));

                // This is used to indicate whether a property should be encrypted or not given the partition key, row key, 
                // and the property name.
                Func<string, string, string, bool> encryptionResolver = (pk, rk, propName) =>
                    {
                        if (propName.StartsWith("EncryptedProp"))
                        {
                            return true;
                        }

                        return false;
                    };

                TableRequestOptions insertOptions = new TableRequestOptions()
                {
                    EncryptionPolicy = new TableEncryptionPolicy(key, null),

                    EncryptionResolver = encryptionResolver
                };

                // Insert Entity
                Console.WriteLine("Inserting the encrypted entity.");
                table.Execute(TableOperation.Insert(ent), insertOptions, null);

                // For retrieves, a resolver can be set up that will help pick the key based on the key id.
                LocalResolver resolver = new LocalResolver();
                resolver.Add(key);

                TableRequestOptions retrieveOptions = new TableRequestOptions()
                {
                    EncryptionPolicy = new TableEncryptionPolicy(null, resolver)
                };

                // Retrieve Entity
                Console.WriteLine("Retrieving the encrypted entity.");
                TableOperation operation = TableOperation.Retrieve(ent.PartitionKey, ent.RowKey);
                TableResult result = table.Execute(operation, retrieveOptions, null);

                Console.WriteLine("Press enter key to exit");
                Console.ReadLine();
            }
            finally
            {
                table.DeleteIfExists();
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