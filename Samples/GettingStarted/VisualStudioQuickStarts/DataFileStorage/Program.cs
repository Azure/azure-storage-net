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

namespace DataFileStorageSample
{    
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.File;
    
    /// <summary>
    /// Azure Storage File Sample - Demonstrate how to use the File Storage service. 
    /// 
    /// Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the 
    /// storage client libraries asynchronous API's. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to the storage service are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Files - http://blogs.msdn.com/b/windowsazurestorage/archive/2014/05/12/introducing-microsoft-azure-file-service.aspx
    /// - How to use Azure File Storage - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-files/
    /// - File Service Concepts - http://msdn.microsoft.com/en-us/library/dn166972.aspx
    /// - File Service REST API - http://msdn.microsoft.com/en-us/library/dn167006.aspx
    /// - File Service C# API - http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.storage.file.aspx
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/en-us/library/hh191443.aspx
    /// </summary>
    public class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run against Microsoft Azure Storage Service by updating the App.Config with your AccountName and AccountKey. 
        // 
        // To run the sample using the Storage Service     
        //      1. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //      2. Set breakpoints and run the project using F10. 
        // 
        // *************************************************************************************************************************        
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage File Sample\n ");

            BasicAzureFileOperationsAsync().Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

        /// <summary>
        /// Basic operations to work with Azure Files
        /// </summary>
        /// <returns>Task</returns>
        private static async Task BasicAzureFileOperationsAsync()
        {
            const string DemoShare = "demofileshare";
            const string DemoDirectory = "demofiledirectory";
            const string ImageToUpload = "HelloWorld.png";

            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a file client for interacting with the file service.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            // Create a share for organizing files and directories within the storage account.
            Console.WriteLine("1. Creating file share");
            CloudFileShare share = fileClient.GetShareReference(DemoShare);
            
            try
            {
                await share.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                Console.WriteLine("Please make sure your storage account has storage file endpoint enabled and specified correctly in the app.config - then restart the sample.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine(); 
                throw; 
            }

            // Get a reference to the root directory of the share.        
            CloudFileDirectory root = share.GetRootDirectoryReference();

            // Create a directory under the root directory 
            Console.WriteLine("2. Creating a directory under the root directory");
            CloudFileDirectory dir = root.GetDirectoryReference(DemoDirectory);
            await dir.CreateIfNotExistsAsync();

            // Uploading a local file to the directory created above 
            Console.WriteLine("3. Uploading a file to directory");
            CloudFile file = dir.GetFileReference(ImageToUpload);
            await file.UploadFromFileAsync(ImageToUpload, FileMode.Open);

            // List all files/directories under the root directory
            Console.WriteLine("4. List Files/Directories in root directory");
            List<IListFileItem> results = new List<IListFileItem>();
            FileContinuationToken token = null;
            do
            {
                FileResultSegment resultSegment = await share.GetRootDirectoryReference().ListFilesAndDirectoriesSegmentedAsync(token);
                results.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;
            }
            while (token != null);

            // Print all files/directories listed above
            foreach (IListFileItem listItem in results)
            {
                // listItem type will be CloudFile or CloudFileDirectory
                Console.WriteLine("- {0} (type: {1})", listItem.Uri, listItem.GetType());
            }

            // Download the uploaded file to your file system
            Console.WriteLine("5. Download file from {0}", file.Uri.AbsoluteUri);
            await file.DownloadToFileAsync(string.Format("./CopyOf{0}", ImageToUpload), FileMode.Create);

            // Clean up after the demo 
            Console.WriteLine("6. Delete file");
            await file.DeleteAsync();

            // When you delete a share it could take several seconds before you can recreate a share with the same
            // name - hence to enable you to run the demo in quick succession the share is not deleted. If you want 
            // to delete the share uncomment the line of code below. 
            // Console.WriteLine("7. Delete Share");
            // await share.DeleteAsync();
        }
       
        /// <summary>
        /// Validates the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        /// <param name="storageConnectionString">The storage connection string</param>
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
