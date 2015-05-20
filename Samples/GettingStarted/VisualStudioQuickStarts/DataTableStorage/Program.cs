//----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
//
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

namespace DataTableStorageSample
{
    using DataTableStorageSample.Model;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks; 

    /// <summary>
    /// Azure Table Service Sample - Demonstrate how to perform common tasks using the Microsoft Azure Table storage 
    /// including creating a table, CRUD operations, batch operations and different querying techniques. 
    /// 
    /// Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the 
    /// storage client libraries asynchronous API's. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to the storage service are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Tables - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-tables/
    /// - Table Service Concepts - http://msdn.microsoft.com/en-us/library/dd179463.aspx
    /// - Table Service REST API - http://msdn.microsoft.com/en-us/library/dd179423.aspx
    /// - Table Service C# API - http://go.microsoft.com/fwlink/?LinkID=398944
    /// - Storage Emulator - http://msdn.microsoft.com/en-us/library/azure/hh403989.aspx
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/en-us/library/hh191443.aspx
    /// </summary>
     
    public class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run using either the Azure Storage Emulator that installs as part of this SDK - or by
        // updating the App.Config file with your AccountName and Key. 
        // 
        // To run the sample using the Storage Emulator (default option)
        //      1. Start the Azure Storage Emulator (once only) by pressing the Start button or the Windows key and searching for it
        //         by typing "Azure Storage Emulator". Select it from the list of applications to start it.
        //      2. Set breakpoints and run the project using F10. 
        // 
        // To run the sample using the Storage Service
        //      1. Open the app.config file and comment out the connection string for the emulator (UseDevelopmentStorage=True) and
        //         uncomment the connection string for the storage service (AccountName=[]...)
        //      2. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //      3. Set breakpoints and run the project using F10. 
        // 
        // *************************************************************************************************************************
        internal const string TableName = "customer";

        public static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage Table Sample\n");

            // Create or reference an existing table
            CloudTable table = CreateTableAsync().Result;

            // Demonstrate basic CRUD functionality 
            BasicTableOperationsAsync(table).Wait();

            // Demonstrate advanced functionality such as batch operations and segmented multi-entity queries
            AdvancedTableOperationsAsync(table).Wait(); 

            // When you delete a table it could take several seconds before you can recreate a table with the same
            // name - hence to enable you to run the demo in quick succession the table is not deleted. If you want 
            // to delete the table uncomment the line of code below. 
            //DeleteTableAsync(table).Wait();

            Console.WriteLine("Press any key to exit");
            Console.Read();
        }

        /// <summary>
        /// Create a table for the sample application to process messages in. 
        /// </summary>
        /// <returns>A CloudTable object</returns>
        private static async Task<CloudTable> CreateTableAsync()
        {
            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a table client for interacting with the table service
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            Console.WriteLine("1. Create a Table for the demo");

            // Create a table client for interacting with the table service 
            CloudTable table = tableClient.GetTableReference(TableName);
            try
            {
                if (await table.CreateIfNotExistsAsync())
                {
                    Console.WriteLine("Created Table named: {0}", TableName);
                }
                else
                {
                    Console.WriteLine("Table {0} already exists", TableName);
                }
            }
            catch (StorageException)
            {
                Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return table;
        }

        /// <summary>
        /// Demonstrate basic Table CRUD operations. 
        /// </summary>
        /// <param name="table">The sample table</param>
        private static async Task BasicTableOperationsAsync(CloudTable table)
        {
            // Create an instance of a customer entity. See the Model\CustomerEntity.cs for a description of the entity.
            CustomerEntity customer = new CustomerEntity("Harp", "Walter")
            {
                Email = "Walter@contoso.com",
                PhoneNumber = "425-555-0101"
            };

            // Demonstrate how to Update the entity by changing the phone number
            Console.WriteLine("2. Update an existing Entity using the InsertOrMerge Upsert Operation.");
            customer.PhoneNumber = "425-555-0105";
            customer = await InsertOrMergeEntityAsync(table, customer);

            // Demonstrate how to Read the updated entity using a point query 
            Console.WriteLine("3. Reading the updated Entity.");
            customer = await RetrieveEntityUsingPointQueryAsync(table, "Harp", "Walter");

            // Demonstrate how to Delete an entity
            Console.WriteLine("4. Delete the entity. ");
            await DeleteEntityAsync(table, customer);
        }

        /// <summary>
        /// Demonstrate advanced table functionality including batch operations and segmented queries
        /// </summary>
        /// <param name="table">The sample table</param>
        private static async Task AdvancedTableOperationsAsync(CloudTable table)
        {
            // Demonstrate upsert and batch table operations
            Console.WriteLine("4. Inserting a batch of entities. ");
            await BatchInsertOfCustomerEntitiesAsync(table); 

            // Query a range of data within a partition
            Console.WriteLine("5. Retrieving entities with surname of Smith and first names >= 1 and <= 75");
            await PartitionRangeQueryAsync(table, "Smith", "0001", "0075");

            // Query for all the data within a partition 
            Console.WriteLine("6. Retrieve entities with surname of Smith.");
            await PartitionScanAsync(table, "Smith");
        }

        /// <summary>
        /// Validate the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        /// <param name="storageConnectionString">Connection string for the storage service or the emulator</param>
        /// <returns>CloudStorageAccount object</returns>
        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }

        /// <summary>
        /// The Table Service supports two main types of insert operations. 
        ///  1. Insert - insert a new entity. If an entity already exists with the same PK + RK an exception will be thrown.
        ///  2. Replace - replace an existing entity. Replace an existing entity with a new entity. 
        ///  3. Insert or Replace - insert the entity if the entity does not exist, or if the entity exists, replace the existing one.
        ///  4. Insert or Merge - insert the entity if the entity does not exist or, if the entity exists, merges the provided entity properties with the already existing ones.
        /// </summary>
        /// <param name="table">The sample table name</param>
        /// <param name="entity">The entity to insert or merge</param>
        /// <returns></returns>
        private static async Task<CustomerEntity> InsertOrMergeEntityAsync(CloudTable table, CustomerEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            // Create the InsertOrReplace  TableOperation
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

            // Execute the operation.
            TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
            CustomerEntity insertedCustomer = result.Result as CustomerEntity;
            return insertedCustomer;
        }

        /// <summary>
        /// Demonstrate the most efficient storage query - the point query - where both partition key and row key are specified. 
        /// </summary>
        /// <param name="table">Sample table name</param>
        /// <param name="partitionKey">Partition key - ie - last name</param>
        /// <param name="rowKey">Row key - ie - first name</param>
        private static async Task<CustomerEntity> RetrieveEntityUsingPointQueryAsync(CloudTable table, string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(partitionKey, rowKey);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            CustomerEntity customer = result.Result as CustomerEntity;
            if (customer != null)
            {
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", customer.PartitionKey, customer.RowKey, customer.Email, customer.PhoneNumber);
            }

            return customer;
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        /// <param name="table">Sample table name</param>
        /// <param name="deleteEntity">Entity to delete</param>
        private static async Task DeleteEntityAsync(CloudTable table, CustomerEntity deleteEntity)
        {
            if (deleteEntity == null)
            {
                throw new ArgumentNullException("deleteEntity");
            }

            TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
            await table.ExecuteAsync(deleteOperation);
        }

        /// <summary>
        /// Demonstrate inserting of a large batch of entities. Some considerations for batch operations:
        ///  1. You can perform updates, deletes, and inserts in the same single batch operation.
        ///  2. A single batch operation can include up to 100 entities.
        ///  3. All entities in a single batch operation must have the same partition key.
        ///  4. While it is possible to perform a query as a batch operation, it must be the only operation in the batch.
        ///  5. Batch size must be <= 4MB
        /// </summary>
        /// <param name="table">Sample table name</param>
        private static async Task BatchInsertOfCustomerEntitiesAsync(CloudTable table)
        {
            // Create the batch operation. 
            TableBatchOperation batchOperation = new TableBatchOperation();

            // The following code  generates test data for use during the query samples.  
            for (int i = 0; i < 100; i++)
            {
                batchOperation.InsertOrMerge(new CustomerEntity("Smith", string.Format("{0}", i.ToString("D4")))
                    {
                        Email = string.Format("{0}@contoso.com", i.ToString("D4")),
                        PhoneNumber = string.Format("425-555-{0}", i.ToString("D4"))
                    });
            }

            // Execute the batch operation.
            IList<TableResult> results = await table.ExecuteBatchAsync(batchOperation);
            foreach (var res in results)
            {
                var customerInserted = res.Result as CustomerEntity;
                Console.WriteLine("Inserted entity with\t Etag = {0} and PartitionKey = {1}, RowKey = {2}", customerInserted.ETag, customerInserted.PartitionKey, customerInserted.RowKey);
            }

        }

        /// <summary>
        /// Demonstrate a partition range query whereby we are searching within a partition for a set of entities that are within a 
        /// specific range. The async API's require the user to implement paging themselves using continuation tokens. 
        /// </summary>
        /// <param name="table">Sample table name</param>
        /// <param name="partitionKey">The partition within which to search</param>
        /// <param name="startRowKey">The lowest bound of the row key range within which to search</param>
        /// <param name="endRowKey">The highest bound of the row key range within which to search</param>
        private static async Task PartitionRangeQueryAsync(CloudTable table, string partitionKey, string startRowKey, string endRowKey)
        {
            // Create the range query using the fluid API 
            TableQuery<CustomerEntity> rangeQuery = new TableQuery<CustomerEntity>().Where(
                TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                        TableOperators.And,
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, startRowKey),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, endRowKey))));

            // Page through the results - requesting 50 results at a time from the server. 
            TableContinuationToken token = null;
            rangeQuery.TakeCount = 50; 
            do
            {
                TableQuerySegment<CustomerEntity> segment = await table.ExecuteQuerySegmentedAsync(rangeQuery, token);
                token = segment.ContinuationToken;
                foreach (CustomerEntity entity in segment)
                {
                    Console.WriteLine("Customer: {0},{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber);
                }
            }
            while (token != null);
        }

        /// <summary>
        /// Demonstrate a partition scan whereby we are searching for all the entities within a partition. Note this is not as efficient 
        /// as a range scan - but definitely more efficient than a full table scan. The async API's require the user to implement 
        /// paging themselves using continuation tokens. 
        /// </summary>
        /// <param name="table">Sample table name</param>
        /// <param name="partitionKey">The partition within which to search</param>
        private static async Task PartitionScanAsync(CloudTable table, string partitionKey)
        {
            TableQuery<CustomerEntity> partitionScanQuery = new TableQuery<CustomerEntity>().Where
                (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            TableContinuationToken token = null; 
            // Page through the results
            do
            {
                TableQuerySegment<CustomerEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
                token = segment.ContinuationToken;
                foreach (CustomerEntity entity in segment)
                {
                    Console.WriteLine("Customer: {0},{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber);
                }
            }
            while (token != null);
        }

        /// <summary>
        /// Delete a table
        /// </summary>
        /// <param name="table">Sample table name</param>
        private static async Task DeleteTableAsync(CloudTable table)
        {
            await table.DeleteIfExistsAsync();
        }
    }
}
