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

namespace TableGettingStartedUsingAttributes
{
    using System;
    using Microsoft.Azure.KeyVault;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

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
            // How to create a storage connection string - https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/
            CloudStorageAccount storageAccount = EncryptionShared.Utility.CreateStorageAccountFromConnectionString();
            CloudTableClient client = storageAccount.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(DemoTable + Guid.NewGuid().ToString("N"));

            try
            {
                table.Create();

                // Create the IKey used for encryption.
                RsaKey key = new RsaKey("private:key1");

                EncryptedEntity ent = new EncryptedEntity() { PartitionKey = Guid.NewGuid().ToString(), RowKey = DateTime.Now.Ticks.ToString() };
                ent.Populate();

                TableRequestOptions insertOptions = new TableRequestOptions()
                {
                    EncryptionPolicy = new TableEncryptionPolicy(key, null)
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
    }
}