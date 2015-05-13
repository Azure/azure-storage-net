// -----------------------------------------------------------------------------------------
// <copyright file="StorageUriTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.File;
    using System;
    using System.Collections.Generic;
    using System.Net;

#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    [TestClass]
    public class StorageUriTests : TestBase
    {
        private const string AccountName = "account";
        private const string SecondarySuffix = "-secondary";
        private const string EndpointSuffix = ".core.windows.net";
        private const string BlobService = ".blob";
        private const string QueueService = ".queue";
        private const string TableService = ".table";
        private const string FileService = ".file";

        [TestMethod]
        [Description("StorageUri should contain 2 URIs")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void StorageUriWithTwoUris()
        {
            Uri primaryClientUri = new Uri("http://" + AccountName + BlobService + EndpointSuffix);
            Uri primaryContainerUri = new Uri(primaryClientUri, "container");
            Uri secondaryClientUri = new Uri("http://" + AccountName + SecondarySuffix + BlobService + EndpointSuffix);
            Uri dummyClientUri = new Uri("http://" + AccountName + "-dummy" + BlobService + EndpointSuffix);

            StorageUri singleUri = new StorageUri(primaryClientUri);
            Assert.IsTrue(primaryClientUri.Equals(singleUri.PrimaryUri));
            Assert.IsNull(singleUri.SecondaryUri);

            StorageUri singleUri2 = new StorageUri(primaryClientUri);
            Assert.IsTrue(singleUri.Equals(singleUri2));

            StorageUri singleUri3 = new StorageUri(secondaryClientUri);
            Assert.IsFalse(singleUri.Equals(singleUri3));

            StorageUri multiUri = new StorageUri(primaryClientUri, secondaryClientUri);
            Assert.IsTrue(primaryClientUri.Equals(multiUri.PrimaryUri));
            Assert.IsTrue(secondaryClientUri.Equals(multiUri.SecondaryUri));
            Assert.IsFalse(multiUri.Equals(singleUri));

            StorageUri multiUri2 = new StorageUri(primaryClientUri, secondaryClientUri);
            Assert.IsTrue(multiUri.Equals(multiUri2));

            TestHelper.ExpectedException<ArgumentException>(
                () => new StorageUri(primaryClientUri, primaryContainerUri),
                "StorageUri constructor should fail if both URIs do not point to the same resource");

            StorageUri multiUri3 = new StorageUri(primaryClientUri, dummyClientUri);
            Assert.IsFalse(multiUri.Equals(multiUri3));

            StorageUri multiUri4 = new StorageUri(dummyClientUri, secondaryClientUri);
            Assert.IsFalse(multiUri.Equals(multiUri4));

            StorageUri multiUri5 = new StorageUri(secondaryClientUri, primaryClientUri);
            Assert.IsFalse(multiUri.Equals(multiUri5));
        }

        [TestMethod]
        [Description("StorageUri should contain 2 URIs")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void DevelopmentStorageWithTwoUris()
        {
            CloudStorageAccount account = CloudStorageAccount.DevelopmentStorageAccount;
            Uri primaryClientUri = account.BlobStorageUri.PrimaryUri;
            Uri primaryContainerUri = new Uri(primaryClientUri + "/container");
            Uri secondaryClientUri = account.BlobStorageUri.SecondaryUri;

            StorageUri singleUri = new StorageUri(primaryClientUri);
            Assert.IsTrue(primaryClientUri.Equals(singleUri.PrimaryUri));
            Assert.IsNull(singleUri.SecondaryUri);

            StorageUri singleUri2 = new StorageUri(primaryClientUri);
            Assert.IsTrue(singleUri.Equals(singleUri2));

            StorageUri singleUri3 = new StorageUri(secondaryClientUri);
            Assert.IsFalse(singleUri.Equals(singleUri3));

            StorageUri multiUri = new StorageUri(primaryClientUri, secondaryClientUri);
            Assert.IsTrue(primaryClientUri.Equals(multiUri.PrimaryUri));
            Assert.IsTrue(secondaryClientUri.Equals(multiUri.SecondaryUri));
            Assert.IsFalse(multiUri.Equals(singleUri));

            StorageUri multiUri2 = new StorageUri(primaryClientUri, secondaryClientUri);
            Assert.IsTrue(multiUri.Equals(multiUri2));

            TestHelper.ExpectedException<ArgumentException>(
                () => new StorageUri(primaryClientUri, primaryContainerUri),
                "StorageUri constructor should fail if both URIs do not point to the same resource");

            StorageUri multiUri3 = new StorageUri(secondaryClientUri, primaryClientUri);
            Assert.IsFalse(multiUri.Equals(multiUri3));
        }

        [TestMethod]
        [Description("CloudStorageAccount should work with StorageUri")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudStorageAccountWithStorageUri()
        {
            StorageUri blobEndpoint = new StorageUri(
                new Uri("http://" + AccountName + BlobService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + BlobService + EndpointSuffix));

            StorageUri queueEndpoint = new StorageUri(
                new Uri("http://" + AccountName + QueueService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + QueueService + EndpointSuffix));

            StorageUri tableEndpoint = new StorageUri(
                new Uri("http://" + AccountName + TableService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + TableService + EndpointSuffix));

            StorageUri fileEndpoint = new StorageUri(
                new Uri("http://" + AccountName + FileService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + FileService + EndpointSuffix));

#if WINDOWS_RT
            CloudStorageAccount account = CloudStorageAccount.Create(new StorageCredentials(), blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint);
#else
            CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(), blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint);
#endif
            Assert.IsTrue(blobEndpoint.Equals(account.BlobStorageUri));
            Assert.IsTrue(queueEndpoint.Equals(account.QueueStorageUri));
            Assert.IsTrue(tableEndpoint.Equals(account.TableStorageUri));
            Assert.IsTrue(fileEndpoint.Equals(account.FileStorageUri));

            account = new CloudStorageAccount(new StorageCredentials(AccountName, TestBase.StorageCredentials.ExportBase64EncodedKey()), false);
            Assert.IsTrue(blobEndpoint.Equals(account.BlobStorageUri));
            Assert.IsTrue(queueEndpoint.Equals(account.QueueStorageUri));
            Assert.IsTrue(tableEndpoint.Equals(account.TableStorageUri));
            Assert.IsTrue(fileEndpoint.Equals(account.FileStorageUri));

            account = CloudStorageAccount.Parse(string.Format("DefaultEndpointsProtocol=http;AccountName={0};AccountKey=", AccountName));
            Assert.IsTrue(blobEndpoint.Equals(account.BlobStorageUri));
            Assert.IsTrue(queueEndpoint.Equals(account.QueueStorageUri));
            Assert.IsTrue(tableEndpoint.Equals(account.TableStorageUri));
            Assert.IsTrue(fileEndpoint.Equals(account.FileStorageUri));

            Assert.IsTrue(blobEndpoint.Equals(account.CreateCloudBlobClient().StorageUri));
            Assert.IsTrue(queueEndpoint.Equals(account.CreateCloudQueueClient().StorageUri));
            Assert.IsTrue(tableEndpoint.Equals(account.CreateCloudTableClient().StorageUri));
            Assert.IsTrue(fileEndpoint.Equals(account.CreateCloudFileClient().StorageUri));

            Assert.IsTrue(blobEndpoint.PrimaryUri.Equals(account.BlobEndpoint));
            Assert.IsTrue(queueEndpoint.PrimaryUri.Equals(account.QueueEndpoint));
            Assert.IsTrue(tableEndpoint.PrimaryUri.Equals(account.TableEndpoint));
            Assert.IsTrue(fileEndpoint.PrimaryUri.Equals(account.FileEndpoint));
        }

        [TestMethod]
        [Description("Blob types should work with StorageUri")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void BlobTypesWithStorageUri()
        {
            StorageUri endpoint = new StorageUri(
                new Uri("http://" + AccountName + BlobService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + BlobService + EndpointSuffix));

            CloudBlobClient client = new CloudBlobClient(endpoint, new StorageCredentials());
            Assert.IsTrue(endpoint.Equals(client.StorageUri));
            Assert.IsTrue(endpoint.PrimaryUri.Equals(client.BaseUri));

            StorageUri containerUri = new StorageUri(
                new Uri(endpoint.PrimaryUri + "container"),
                new Uri(endpoint.SecondaryUri + "container"));

            CloudBlobContainer container = client.GetContainerReference("container");
            Assert.IsTrue(containerUri.Equals(container.StorageUri));
            Assert.IsTrue(containerUri.PrimaryUri.Equals(container.Uri));
            Assert.IsTrue(endpoint.Equals(container.ServiceClient.StorageUri));

            container = new CloudBlobContainer(containerUri, client.Credentials);
            Assert.IsTrue(containerUri.Equals(container.StorageUri));
            Assert.IsTrue(containerUri.PrimaryUri.Equals(container.Uri));
            Assert.IsTrue(endpoint.Equals(container.ServiceClient.StorageUri));

            StorageUri directoryUri = new StorageUri(
                new Uri(containerUri.PrimaryUri + "/directory/"),
                new Uri(containerUri.SecondaryUri + "/directory/"));

            StorageUri subdirectoryUri = new StorageUri(
                new Uri(directoryUri.PrimaryUri + "subdirectory/"),
                new Uri(directoryUri.SecondaryUri + "subdirectory/"));

            CloudBlobDirectory directory = container.GetDirectoryReference("directory");
            Assert.IsTrue(directoryUri.Equals(directory.StorageUri));
            Assert.IsTrue(directoryUri.PrimaryUri.Equals(directory.Uri));
            Assert.IsNotNull(directory.Parent);
            Assert.IsTrue(containerUri.Equals(directory.Container.StorageUri));
            Assert.IsTrue(endpoint.Equals(directory.ServiceClient.StorageUri));

            CloudBlobDirectory subdirectory = directory.GetDirectoryReference("subdirectory");
            Assert.IsTrue(subdirectoryUri.Equals(subdirectory.StorageUri));
            Assert.IsTrue(subdirectoryUri.PrimaryUri.Equals(subdirectory.Uri));
            Assert.IsTrue(directoryUri.Equals(subdirectory.Parent.StorageUri));
            Assert.IsTrue(containerUri.Equals(subdirectory.Container.StorageUri));
            Assert.IsTrue(endpoint.Equals(subdirectory.ServiceClient.StorageUri));

            StorageUri blobUri = new StorageUri(
                new Uri(subdirectoryUri.PrimaryUri + "blob"),
                new Uri(subdirectoryUri.SecondaryUri + "blob"));

            CloudBlockBlob blockBlob = subdirectory.GetBlockBlobReference("blob");
            Assert.IsTrue(blobUri.Equals(blockBlob.StorageUri));
            Assert.IsTrue(blobUri.PrimaryUri.Equals(blockBlob.Uri));
            Assert.IsTrue(subdirectoryUri.Equals(blockBlob.Parent.StorageUri));
            Assert.IsTrue(containerUri.Equals(blockBlob.Container.StorageUri));
            Assert.IsTrue(endpoint.Equals(blockBlob.ServiceClient.StorageUri));

            blockBlob = new CloudBlockBlob(blobUri, null, client.Credentials);
            Assert.IsTrue(blobUri.Equals(blockBlob.StorageUri));
            Assert.IsTrue(blobUri.PrimaryUri.Equals(blockBlob.Uri));
            Assert.IsTrue(subdirectoryUri.Equals(blockBlob.Parent.StorageUri));
            Assert.IsTrue(containerUri.Equals(blockBlob.Container.StorageUri));
            Assert.IsTrue(endpoint.Equals(blockBlob.ServiceClient.StorageUri));

            CloudPageBlob pageBlob = subdirectory.GetPageBlobReference("blob");
            Assert.IsTrue(blobUri.Equals(pageBlob.StorageUri));
            Assert.IsTrue(blobUri.PrimaryUri.Equals(pageBlob.Uri));
            Assert.IsTrue(subdirectoryUri.Equals(pageBlob.Parent.StorageUri));
            Assert.IsTrue(containerUri.Equals(pageBlob.Container.StorageUri));
            Assert.IsTrue(endpoint.Equals(pageBlob.ServiceClient.StorageUri));

            pageBlob = new CloudPageBlob(blobUri, null, client.Credentials);
            Assert.IsTrue(blobUri.Equals(pageBlob.StorageUri));
            Assert.IsTrue(blobUri.PrimaryUri.Equals(pageBlob.Uri));
            Assert.IsTrue(subdirectoryUri.Equals(pageBlob.Parent.StorageUri));
            Assert.IsTrue(containerUri.Equals(pageBlob.Container.StorageUri));
            Assert.IsTrue(endpoint.Equals(pageBlob.ServiceClient.StorageUri));
        }

        [TestMethod]
        [Description("Queue types should work with StorageUri")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void QueueTypesWithStorageUri()
        {
            StorageUri endpoint = new StorageUri(
                new Uri("http://" + AccountName + QueueService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + QueueService + EndpointSuffix));

            CloudQueueClient client = new CloudQueueClient(endpoint, new StorageCredentials());
            Assert.IsTrue(endpoint.Equals(client.StorageUri));
            Assert.IsTrue(endpoint.PrimaryUri.Equals(client.BaseUri));

            StorageUri queueUri = new StorageUri(
                new Uri(endpoint.PrimaryUri + "queue"),
                new Uri(endpoint.SecondaryUri + "queue"));

            CloudQueue queue = client.GetQueueReference("queue");
            Assert.IsTrue(queueUri.Equals(queue.StorageUri));
            Assert.IsTrue(queueUri.PrimaryUri.Equals(queue.Uri));
            Assert.IsTrue(endpoint.Equals(queue.ServiceClient.StorageUri));

            queue = new CloudQueue(queueUri, client.Credentials);
            Assert.IsTrue(queueUri.Equals(queue.StorageUri));
            Assert.IsTrue(queueUri.PrimaryUri.Equals(queue.Uri));
            Assert.IsTrue(endpoint.Equals(queue.ServiceClient.StorageUri));
        }

        [TestMethod]
        [Description("Table types should work with StorageUri")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TableTypesWithStorageUri()
        {
            StorageUri endpoint = new StorageUri(
                new Uri("http://" + AccountName + TableService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + TableService + EndpointSuffix));

            CloudTableClient client = new CloudTableClient(endpoint, new StorageCredentials());
            Assert.IsTrue(endpoint.Equals(client.StorageUri));
            Assert.IsTrue(endpoint.PrimaryUri.Equals(client.BaseUri));

            StorageUri tableUri = new StorageUri(
                new Uri(endpoint.PrimaryUri + "table"),
                new Uri(endpoint.SecondaryUri + "table"));

            CloudTable table = client.GetTableReference("table");
            Assert.IsTrue(tableUri.Equals(table.StorageUri));
            Assert.IsTrue(tableUri.PrimaryUri.Equals(table.Uri));
            Assert.IsTrue(endpoint.Equals(table.ServiceClient.StorageUri));

            table = new CloudTable(tableUri, client.Credentials);
            Assert.IsTrue(tableUri.Equals(table.StorageUri));
            Assert.IsTrue(tableUri.PrimaryUri.Equals(table.Uri));
            Assert.IsTrue(endpoint.Equals(table.ServiceClient.StorageUri));
        }

        [TestMethod]
        [Description("File types should work with StorageUri")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void FileTypesWithStorageUri()
        {
            StorageUri endpoint = new StorageUri(
                new Uri("http://" + AccountName + FileService + EndpointSuffix),
                new Uri("http://" + AccountName + SecondarySuffix + FileService + EndpointSuffix));

            CloudFileClient client = new CloudFileClient(endpoint, new StorageCredentials());
            Assert.IsTrue(endpoint.Equals(client.StorageUri));
            Assert.IsTrue(endpoint.PrimaryUri.Equals(client.BaseUri));

            StorageUri shareUri = new StorageUri(
                new Uri(endpoint.PrimaryUri + "share"),
                new Uri(endpoint.SecondaryUri + "share"));

            CloudFileShare share = client.GetShareReference("share");
            Assert.IsTrue(shareUri.Equals(share.StorageUri));
            Assert.IsTrue(shareUri.PrimaryUri.Equals(share.Uri));
            Assert.IsTrue(endpoint.Equals(share.ServiceClient.StorageUri));

            share = new CloudFileShare(shareUri, client.Credentials);
            Assert.IsTrue(shareUri.Equals(share.StorageUri));
            Assert.IsTrue(shareUri.PrimaryUri.Equals(share.Uri));
            Assert.IsTrue(endpoint.Equals(share.ServiceClient.StorageUri));

            StorageUri directoryUri = new StorageUri(
                new Uri(shareUri.PrimaryUri + "/directory"),
                new Uri(shareUri.SecondaryUri + "/directory"));

            StorageUri subdirectoryUri = new StorageUri(
                new Uri(directoryUri.PrimaryUri + "/subdirectory"),
                new Uri(directoryUri.SecondaryUri + "/subdirectory"));

            CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory");
            Assert.IsTrue(directoryUri.Equals(directory.StorageUri));
            Assert.IsTrue(directoryUri.PrimaryUri.Equals(directory.Uri));
            Assert.IsNotNull(directory.Parent);
            Assert.IsTrue(shareUri.Equals(directory.Share.StorageUri));
            Assert.IsTrue(endpoint.Equals(directory.ServiceClient.StorageUri));

            CloudFileDirectory subdirectory = directory.GetDirectoryReference("subdirectory");
            Assert.IsTrue(subdirectoryUri.Equals(subdirectory.StorageUri));
            Assert.IsTrue(subdirectoryUri.PrimaryUri.Equals(subdirectory.Uri));
            Assert.IsTrue(directoryUri.Equals(subdirectory.Parent.StorageUri));
            Assert.IsTrue(shareUri.Equals(subdirectory.Share.StorageUri));
            Assert.IsTrue(endpoint.Equals(subdirectory.ServiceClient.StorageUri));

            StorageUri fileUri = new StorageUri(
                new Uri(subdirectoryUri.PrimaryUri + "/file"),
                new Uri(subdirectoryUri.SecondaryUri + "/file"));

            CloudFile file = subdirectory.GetFileReference("file");
            Assert.IsTrue(fileUri.Equals(file.StorageUri));
            Assert.IsTrue(fileUri.PrimaryUri.Equals(file.Uri));
            Assert.IsTrue(subdirectoryUri.Equals(file.Parent.StorageUri));
            Assert.IsTrue(shareUri.Equals(file.Share.StorageUri));
            Assert.IsTrue(endpoint.Equals(file.ServiceClient.StorageUri));

            file = new CloudFile(fileUri, client.Credentials);
            Assert.IsTrue(fileUri.Equals(file.StorageUri));
            Assert.IsTrue(fileUri.PrimaryUri.Equals(file.Uri));
            Assert.IsTrue(subdirectoryUri.Equals(file.Parent.StorageUri));
            Assert.IsTrue(shareUri.Equals(file.Share.StorageUri));
            Assert.IsTrue(endpoint.Equals(file.ServiceClient.StorageUri));
        }
    }
}
