// -----------------------------------------------------------------------------------------
// <copyright file="StorageExceptionTests.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.File;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.Queue.Protocol;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    [TestClass]
    public class AccountSASTests : TestBase
    {
        public async Task RunPermissionsTestBlobs(SharedAccessAccountPolicy policy)
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            string containerName = "c" + Guid.NewGuid().ToString("N");
            try
            {
                CloudStorageAccount account = new CloudStorageAccount(blobClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, blobClient.StorageUri, null, null, null);
                CloudBlobClient blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
                CloudBlobContainer containerWithSAS = blobClientWithSAS.GetContainerReference(containerName);
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                // General pattern - If current perms support doing a thing with SAS, do the thing with SAS and validate with shared
                // Otherwise, make sure SAS fails and then do the thing with shared key.

                // Things to do:
                // Create the container (Create / Write perms, Container RT)
                // List containers with prefix (List perms, Service RT)
                // Create an append blob (Create / Write perms, Object RT)
                // Append a block to append blob (Add / Write perms, Object RT)
                // Read the data from the append blob (Read perms, Object RT)
                // Delete the blob (Delete perms, Object RT)

                if ((((policy.Permissions & SharedAccessAccountPermissions.Create) == SharedAccessAccountPermissions.Create) || ((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Container) == SharedAccessAccountResourceTypes.Container))
                {
                    await containerWithSAS.CreateAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => containerWithSAS.CreateAsync(), "Create a container should fail with SAS without Create or Write and Container-level permissions.");
                    await container.CreateAsync();
                }

                Assert.IsTrue(container.Exists());

                if (((policy.Permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service))
                {
                    Assert.AreEqual(container.Name, blobClientWithSAS.ListContainers(container.Name).First().Name);
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => blobClientWithSAS.ListContainers(container.Name).First(), "List containers should fail with SAS without List and Service-level permissions.");
                }

                string blobName = "blob";
                CloudAppendBlob appendBlob = container.GetAppendBlobReference(blobName);
                CloudAppendBlob appendBlobWithSAS = containerWithSAS.GetAppendBlobReference(blobName);

                //Try creating credentials using SAS Uri directly
                CloudAppendBlob appendBlobWithSASUri = new CloudAppendBlob(new Uri(container.Uri + accountSASToken));

                if ((((policy.Permissions & SharedAccessAccountPermissions.Create) == SharedAccessAccountPermissions.Create) || ((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await appendBlobWithSAS.CreateOrReplaceAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => appendBlobWithSAS.CreateOrReplaceAsync(), "Creating an append blob should fail with SAS without Create or Write and Object-level perms.");
                    await appendBlob.CreateOrReplaceAsync();
                }

                Assert.IsTrue(appendBlob.Exists());

                string blobText = "blobText";
                if ((((policy.Permissions & SharedAccessAccountPermissions.Add) == SharedAccessAccountPermissions.Add) || ((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(blobText)))
                    {
                        await appendBlobWithSAS.AppendBlockAsync(stream);
                    }
                }
                else
                {
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(blobText)))
                    {
                        TestHelper.ExpectedException<StorageException>(() => appendBlobWithSAS.AppendBlock(stream), "Append a block to an append blob should fail with SAS without Add or Write and Object-level perms.");
                        stream.Seek(0, SeekOrigin.Begin);
                        await appendBlob.AppendBlockAsync(stream);
                    }
                }

                Assert.AreEqual(blobText, await appendBlob.DownloadTextAsync());

                if (((policy.Permissions & SharedAccessAccountPermissions.Read) == SharedAccessAccountPermissions.Read) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    Assert.AreEqual(blobText, await appendBlobWithSAS.DownloadTextAsync());
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => appendBlobWithSAS.DownloadText(), "Reading a blob's contents with SAS without Read and Object-level permissions should fail.");
                }

                if (((policy.Permissions & SharedAccessAccountPermissions.Delete) == SharedAccessAccountPermissions.Delete) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await appendBlobWithSAS.DeleteAsync();
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => appendBlobWithSAS.Delete(), "Deleting a blob with SAS without Delete and Object-level perms should fail.");
                    await appendBlob.DeleteAsync();
                }

                Assert.IsFalse(appendBlob.Exists());
            }
            finally
            {
                await blobClient.GetContainerReference(containerName).DeleteIfExistsAsync();
            }
        }

        public async Task RunPermissionsTestQueues(SharedAccessAccountPolicy policy)
        {
            CloudQueueClient queueClient = GenerateCloudQueueClient();
            string queueName = "q" + Guid.NewGuid().ToString("N");
            try
            {
                CloudStorageAccount account = new CloudStorageAccount(queueClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, queueClient.StorageUri, null, null);
                CloudQueueClient queueClientWithSAS = accountWithSAS.CreateCloudQueueClient();
                CloudQueue queueWithSAS = queueClientWithSAS.GetQueueReference(queueName);
                CloudQueue queue = queueClient.GetQueueReference(queueName);

                // General pattern - If current perms support doing a thing with SAS, do the thing with SAS and validate with shared
                // Otherwise, make sure SAS fails and then do the thing with shared key.

                // Things to do:
                // Create the queue (Create or Write perms, Container RT)
                // List queues (List perms, Service RT)
                // Set queue metadata (Write perms, Container RT)
                // Insert a message (Add perms, Object RT)
                // Peek a message (Read perms, Object RT)
                // Get a message (Process perms, Object RT)
                // Update a message (Update perms, Object RT)
                // Clear all messages (Delete perms, Object RT)

                if ((((policy.Permissions & SharedAccessAccountPermissions.Create) == SharedAccessAccountPermissions.Create) || ((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Container) == SharedAccessAccountResourceTypes.Container))
                {
                    await queueWithSAS.CreateAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => 
                    queueWithSAS.CreateAsync(), 
                    "Creating a queue with SAS should fail without Add and Container-level permissions.");
                    await queue.CreateAsync();
                }
                Assert.IsTrue(queue.Exists());

                if (((policy.Permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service))
                {
                    Assert.AreEqual(queueName, queueClientWithSAS.ListQueues(queueName).First().Name);
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => queueClientWithSAS.ListQueues(queueName).First(), "Listing queues with SAS should fail without Read and Service-level permissions.");
                }

                queueWithSAS.Metadata["metadatakey"] = "metadatavalue";
                if (((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Container) == SharedAccessAccountResourceTypes.Container))
                {
                    await queueWithSAS.SetMetadataAsync();
                    await queue.FetchAttributesAsync();
                    Assert.AreEqual("metadatavalue", queue.Metadata["metadatakey"]);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => queueWithSAS.SetMetadataAsync(), "Setting a queue's metadata with SAS should fail without Write and Container-level permissions.");
                }

                string messageText = "messageText";
                CloudQueueMessage message = new CloudQueueMessage(messageText);
                if (((policy.Permissions & SharedAccessAccountPermissions.Add) == SharedAccessAccountPermissions.Add) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await queueWithSAS.AddMessageAsync(message);
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => queueWithSAS.AddMessage(message), "Adding a queue message should fail with SAS without Add and Object-level permissions.");
                    await queue.AddMessageAsync(message);
                }
                Assert.AreEqual(messageText, (await queue.PeekMessageAsync()).AsString);

                if (((policy.Permissions & SharedAccessAccountPermissions.Read) == SharedAccessAccountPermissions.Read) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    Assert.AreEqual(messageText, (await queueWithSAS.PeekMessageAsync()).AsString);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => queueWithSAS.PeekMessageAsync(), "Peeking a queue message should fail with SAS without Read and Object-level permissions.");
                }

                CloudQueueMessage messageResult = null;
                if (((policy.Permissions & SharedAccessAccountPermissions.ProcessMessages) == SharedAccessAccountPermissions.ProcessMessages) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    messageResult = await queueWithSAS.GetMessageAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => queueWithSAS.GetMessageAsync(), "Getting a message should fail with SAS without Process and Object-level permissions.");
                    messageResult = await queue.GetMessageAsync();
                }
                Assert.AreEqual(messageText, messageResult.AsString);

                string newMessageContent = "new content";
                messageResult.SetMessageContent(newMessageContent);
                if (((policy.Permissions & SharedAccessAccountPermissions.Update) == SharedAccessAccountPermissions.Update) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    queueWithSAS.UpdateMessage(messageResult, TimeSpan.Zero, MessageUpdateFields.Content | MessageUpdateFields.Visibility);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => queueWithSAS.UpdateMessageAsync(messageResult, TimeSpan.Zero, MessageUpdateFields.Content | MessageUpdateFields.Visibility), "Updating a message should fail with SAS without Update and Object-level permissions.");
                    await queue.UpdateMessageAsync(messageResult, TimeSpan.Zero, MessageUpdateFields.Content | MessageUpdateFields.Visibility);
                }
                messageResult = await queue.PeekMessageAsync();
                Assert.AreEqual(newMessageContent, messageResult.AsString);

                if (((policy.Permissions & SharedAccessAccountPermissions.Delete) == SharedAccessAccountPermissions.Delete) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await queueWithSAS.ClearAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => queueWithSAS.ClearAsync(), "Clearing messages should fail with SAS without delete and Object-level permissions.");
                }
            }
            finally
            {
                await queueClient.GetQueueReference(queueName).DeleteIfExistsAsync();
            }
        }
        public async Task RunPermissionsTestFiles(SharedAccessAccountPolicy policy)
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            string shareName = "s" + Guid.NewGuid().ToString("N");
            try
            {
                CloudStorageAccount account = new CloudStorageAccount(fileClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, null, null, fileClient.StorageUri);
                CloudFileClient fileClientWithSAS = accountWithSAS.CreateCloudFileClient();
                CloudFileShare shareWithSAS = fileClientWithSAS.GetShareReference(shareName);
                CloudFileShare share = fileClient.GetShareReference(shareName);

                // General pattern - If current perms support doing a thing with SAS, do the thing with SAS and validate with shared
                // Otherwise, make sure SAS fails and then do the thing with shared key.

                // Things to do:
                // Create the share (Create / Write perms, Container RT)
                // List shares with prefix (List perms, Service RT)
                // Create a new file (Create / Write, Object RT)
                // Add a range to the file (Write, Object RT)
                // Read the data from the file (Read, Object RT)
                // Overwrite a file (Write, Object RT)
                // Delete the file (Delete perms, Object RT)

                if ((((policy.Permissions & SharedAccessAccountPermissions.Create) == SharedAccessAccountPermissions.Create) || ((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Container) == SharedAccessAccountResourceTypes.Container))
                {
                    await shareWithSAS.CreateAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => shareWithSAS.CreateAsync(), "Creating a share with SAS should fail without Create or Write and Container-level perms.");
                    share.Create();
                }
                Assert.IsTrue(share.Exists());

                if (((policy.Permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service))
                {
                    Assert.AreEqual(shareName, fileClientWithSAS.ListShares(shareName).First().Name);
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => fileClientWithSAS.ListShares(shareName).First(), "Listing shared with SAS should fail without List and Service-level perms.");
                }

                string filename = "fileName";
                CloudFile fileWithSAS = shareWithSAS.GetRootDirectoryReference().GetFileReference(filename);
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(filename);

                //Try creating credentials using SAS Uri directly
                CloudFile fileWithSASUri = new CloudFile(new Uri(share.Uri + accountSASToken));

                byte[] content = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                if ((((policy.Permissions & SharedAccessAccountPermissions.Create) == SharedAccessAccountPermissions.Create) || ((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await fileWithSAS.CreateAsync(content.Length);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => fileWithSAS.CreateAsync(content.Length), "Creating a file with SAS should fail without Create or Write and Object-level perms.");
                    await file.CreateAsync(content.Length);
                }
                Assert.IsTrue(file.Exists());

                using (Stream stream = new MemoryStream(content))
                {
                    if (((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write) &&
                        ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                    {
                        await fileWithSAS.WriteRangeAsync(stream, 0, null);
                    }
                    else
                    {
                        TestHelper.ExpectedException<StorageException>(() => fileWithSAS.WriteRange(stream, 0), "Writing a range to a file with SAS should fail without Write and Object-level perms.");
                        stream.Seek(0, SeekOrigin.Begin);
                        await file.WriteRangeAsync(stream, 0, null);
                    }
                }

                byte[] result = new byte[content.Length];
                await file.DownloadRangeToByteArrayAsync(result, 0, 0, content.Length);
                for (int i = 0; i < content.Length; i++)
                {
                    Assert.AreEqual(content[i], result[i]);
                }

                if (((policy.Permissions & SharedAccessAccountPermissions.Read) == SharedAccessAccountPermissions.Read) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    result = new byte[content.Length];
                    fileWithSAS.DownloadRangeToByteArray(result, 0, 0, content.Length);
                    for (int i = 0; i < content.Length; i++)
                    {
                        Assert.AreEqual(content[i], result[i]);
                    }
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => fileWithSAS.DownloadRangeToByteArray(result, 0, 0, content.Length), "Reading a file with SAS should fail without Read and Object-level perms.");
                }

                if (((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await fileWithSAS.CreateAsync(2);
                }
                else
                {
                    TestHelper.ExpectedException<StorageException>(() => fileWithSAS.Create(2), "Overwriting a file with SAS should fail without Write and Object-level perms.");
                    await file.CreateAsync(2);
                }

                result = new byte[content.Length];
                await file.DownloadRangeToByteArrayAsync(result, 0, 0, content.Length);
                for (int i = 0; i < content.Length; i++)
                {
                    Assert.AreEqual(0, result[i]);
                }

                if (((policy.Permissions & SharedAccessAccountPermissions.Delete) == SharedAccessAccountPermissions.Delete) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await fileWithSAS.DeleteAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(() => fileWithSAS.DeleteAsync(), "Deleting a file with SAS should fail without Delete and Object-level perms.");
                    await file.DeleteAsync();
                }

                Assert.IsFalse(await file.ExistsAsync());
            }
            finally
            {
                await fileClient.GetShareReference(shareName).DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Test account SAS all permissions, all services")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AccountSASPermissions()
        {
            // Single-threaded, takes 10 minutes to run
            // Parallelized, 1 minute.

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 0x100; i++)
            {
                SharedAccessAccountPermissions permissions = (SharedAccessAccountPermissions)i;
                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                policy.Permissions = permissions;
                await Task.WhenAll(
                    this.RunPermissionsTestBlobs(policy),
                    this.RunPermissionsTestQueues(policy),
                    this.RunPermissionsTestFiles(policy));
            }
        }

        [TestMethod]
        [Description("Test account SAS various combinations of resource types")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AccountSASResourceTypes()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 0x8; i++)
            {
                SharedAccessAccountResourceTypes resourceTypes = (SharedAccessAccountResourceTypes)i;
                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                policy.ResourceTypes = resourceTypes;

                await Task.WhenAll(
                    this.RunPermissionsTestBlobs(policy),
                    this.RunPermissionsTestQueues(policy),
                    this.RunPermissionsTestFiles(policy));
            }
        }

        public void RunBlobTest(SharedAccessAccountPolicy policy, Action<Action> testHandler, int? httpsPort)
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            string containerName = "c" + Guid.NewGuid().ToString("N");
            try
            {
                CloudStorageAccount account = new CloudStorageAccount(blobClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);

                StorageUri storageUri = blobClient.StorageUri;
                if (httpsPort != null)
                {
                    storageUri = new StorageUri(TransformSchemeAndPort(storageUri.PrimaryUri, "https", httpsPort.Value), TransformSchemeAndPort(storageUri.SecondaryUri, "https", httpsPort.Value));
                }
                else
                {
                    storageUri = new StorageUri(TransformSchemeAndPort(storageUri.PrimaryUri, "http", 80), TransformSchemeAndPort(storageUri.SecondaryUri, "http", 80));
                }

                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, storageUri, null, null, null);
                CloudBlobClient blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
                CloudBlobContainer containerWithSAS = blobClientWithSAS.GetContainerReference(containerName);
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                container.Create();
                string blobName = "blob";
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
                string blobText = "blobText";
                blob.UploadText(blobText);

                CloudBlockBlob blobWithSAS = containerWithSAS.GetBlockBlobReference(blobName);

                testHandler(() => Assert.AreEqual(blobText, blobWithSAS.DownloadText()));
            }
            finally
            {
                blobClient.GetContainerReference(containerName).DeleteIfExists();
            }
        }

        public void RunQueueTest(SharedAccessAccountPolicy policy, Action<Action> testHandler, int? httpsPort)
        {
            CloudQueueClient queueClient = GenerateCloudQueueClient();
            string queueName = "q" + Guid.NewGuid().ToString("N");
            try
            {
                CloudStorageAccount account = new CloudStorageAccount(queueClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);

                StorageUri storageUri = queueClient.StorageUri;
                if (httpsPort != null)
                {
                    storageUri = new StorageUri(TransformSchemeAndPort(storageUri.PrimaryUri, "https", httpsPort.Value), TransformSchemeAndPort(storageUri.SecondaryUri, "https", httpsPort.Value));
                }
                else
                {
                    storageUri = new StorageUri(TransformSchemeAndPort(storageUri.PrimaryUri, "http", 80), TransformSchemeAndPort(storageUri.SecondaryUri, "http", 80));
                }


                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, storageUri, null, null);
                CloudQueueClient queueClientWithSAS = accountWithSAS.CreateCloudQueueClient();
                CloudQueue queueWithSAS = queueClientWithSAS.GetQueueReference(queueName);
                CloudQueue queue = queueClient.GetQueueReference(queueName);
                queue.Create();
                string messageText = "message text";
                CloudQueueMessage message = new CloudQueueMessage(messageText);
                queue.AddMessage(message);

                testHandler(() => Assert.AreEqual(messageText, queueWithSAS.GetMessage().AsString));
            }
            finally
            {
                queueClient.GetQueueReference(queueName).DeleteIfExists();
            }
        }

        public void RunFileTest(SharedAccessAccountPolicy policy, Action<Action> testHandler, int? httpsPort)
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            string shareName = "s" + Guid.NewGuid().ToString("N");
            try
            {
                CloudStorageAccount account = new CloudStorageAccount(fileClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);

                StorageUri storageUri = fileClient.StorageUri;
                if (httpsPort != null)
                {
                    storageUri = new StorageUri(TransformSchemeAndPort(storageUri.PrimaryUri, "https", httpsPort.Value), TransformSchemeAndPort(storageUri.SecondaryUri, "https", httpsPort.Value));
                }
                else
                {
                    storageUri = new StorageUri(TransformSchemeAndPort(storageUri.PrimaryUri, "http", 80), TransformSchemeAndPort(storageUri.SecondaryUri, "http", 80));
                }


                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, null, null, storageUri);
                CloudFileClient fileClientWithSAS = accountWithSAS.CreateCloudFileClient();
                CloudFileShare shareWithSAS = fileClientWithSAS.GetShareReference(shareName);
                CloudFileShare share = fileClient.GetShareReference(shareName);
                share.Create();
                string fileName = "file";
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(fileName);
                CloudFile fileWithSAS = shareWithSAS.GetRootDirectoryReference().GetFileReference(fileName);
                byte[] content = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                file.Create(content.Length);
                using (Stream stream = new MemoryStream(content))
                {
                    file.WriteRange(stream, 0);
                }

                testHandler(() =>
                    {
                        byte[] result = new byte[content.Length];
                        fileWithSAS.DownloadRangeToByteArray(result, 0, 0, content.Length);
                        for (int i = 0; i < content.Length; i++)
                        {
                            Assert.AreEqual(content[i], result[i]);
                        }
                    });
            }
            finally
            {
                fileClient.GetShareReference(shareName).DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test account SAS various combinations of services")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASServices()
        {
            for (int i = 0; i < 0x10; i++)
            {
                SharedAccessAccountPolicy policy = this.GetPolicyWithFullPermissions();
                policy.Services = (SharedAccessAccountServices)i;
                RunBlobTest(policy, action =>
                    {
                        if ((policy.Services & SharedAccessAccountServices.Blob) == SharedAccessAccountServices.Blob)
                        {
                            action();
                        }
                        else
                        {
                            TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed without Blob access.");
                        }
                    }, null);
                RunQueueTest(policy, action =>
                {
                    if ((policy.Services & SharedAccessAccountServices.Queue) == SharedAccessAccountServices.Queue)
                    {
                        action();
                    }
                    else
                    {
                        TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed without Queue access.");
                    }
                }, null);
                RunFileTest(policy, action =>
                {
                    if ((policy.Services & SharedAccessAccountServices.File) == SharedAccessAccountServices.File)
                    {
                        action();
                    }
                    else
                    {
                        TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed without File access.");
                    }
                }, null);
            }
        }

        [TestMethod]
        [Description("Test account SAS various combinations of start and expiry times")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASStartExpiryTimes()
        {
            int?[] startOffsets = new int?[] { null, -5, 5 };
            int[] endOffsets = new int[] { -5, 5 };

            foreach (int? startOffset in startOffsets)
            {
                foreach (int endOffset in endOffsets)
                {
                    SharedAccessAccountPolicy policy = this.GetPolicyWithFullPermissions();

                    if (startOffset.HasValue)
                    {
                        policy.SharedAccessStartTime = DateTime.Now + TimeSpan.FromMinutes(startOffset.Value);
                    }
                    else
                    {
                        policy.SharedAccessStartTime = null;
                    }

                    policy.SharedAccessExpiryTime = DateTime.Now + TimeSpan.FromMinutes(endOffset);

                    bool pass = (!policy.SharedAccessStartTime.HasValue || (policy.SharedAccessStartTime.Value < DateTime.Now)) && (policy.SharedAccessExpiryTime > DateTime.Now);

                    RunBlobTest(policy, action =>
                    {
                        if (pass)
                        {
                            action();
                        }
                        else
                        {
                            TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid start/expiry times.");
                        }
                    }, null);

                    RunQueueTest(policy, action =>
                    {
                        if (pass)
                        {
                            action();
                        }
                        else
                        {
                            TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid start/expiry times.");
                        }
                    }, null);

                    RunFileTest(policy, action =>
                    {
                        if (pass)
                        {
                            action();
                        }
                        else
                        {
                            TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid start/expiry times.");
                        }
                    }, null);
                }
            }
        }

        [TestMethod]
        [Description("Test account SAS various combinations of signedIPs")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASSignedIPs()
        {
            IPAddress invalidIP = IPAddress.Parse("255.255.255.255");

            SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
            policy.IPAddressOrRange = new IPAddressOrRange(invalidIP.ToString());

            RunBlobTest(policy, action => TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid IP access."), null);
            RunQueueTest(policy, action => TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid IP access."), null);
            RunFileTest(policy, action => TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid IP access."), null);

            policy.IPAddressOrRange = null;
            RunBlobTest(policy, action => action(), null);
            RunQueueTest(policy, action => action(), null);
            RunFileTest(policy, action => action(), null);

            policy.IPAddressOrRange = new IPAddressOrRange(IPAddress.Parse("255.255.255.0").ToString(), invalidIP.ToString());

            RunBlobTest(policy, action => TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid IP access."), null);
            RunQueueTest(policy, action => TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid IP access."), null);
            RunFileTest(policy, action => TestHelper.ExpectedException<StorageException>(() => action(), "Operation should have failed with invalid IP access."), null);
        }

        [TestMethod]
        [Description("Test account SAS various combinations of signed protocols")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASSignedProtocols()
        {
            int blobHttpsPort = 443;
            int tableHttpsPort = 443;
            int queueHttpsPort = 443;
            int fileHttpsPort = 443;

            if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.BlobSecurePortOverride))
            {
                blobHttpsPort = Int32.Parse(TestBase.TargetTenantConfig.BlobSecurePortOverride);
            }

            if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.TableSecurePortOverride))
            {
                tableHttpsPort = Int32.Parse(TestBase.TargetTenantConfig.TableSecurePortOverride);
            }

            if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.QueueSecurePortOverride))
            {
                queueHttpsPort = Int32.Parse(TestBase.TargetTenantConfig.QueueSecurePortOverride);
            }

            if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.FileSecurePortOverride))
            {
                fileHttpsPort = Int32.Parse(TestBase.TargetTenantConfig.FileSecurePortOverride);
            }

            for (int i = 0; i < 3; i++)
            {
                SharedAccessAccountPolicy policy = this.GetPolicyWithFullPermissions();
                policy.Protocols = i == 0 ? (SharedAccessProtocol?)null : (SharedAccessProtocol)i;

                RunBlobTest(policy, action =>
                {
                    if (!policy.Protocols.HasValue || (policy.Protocols == SharedAccessProtocol.HttpsOrHttp))
                    {
                        action();
                    }
                    else
                    {
                        TestHelper.ExpectedException(() => action(), "Operation should have failed without using Https.", HttpStatusCode.Unused);
                    }
                }, null);

                RunBlobTest(policy, action =>
                {
                    action();
                }, blobHttpsPort);

                RunQueueTest(policy, action =>
                {
                    if (!policy.Protocols.HasValue || (policy.Protocols == SharedAccessProtocol.HttpsOrHttp))
                    {
                        action();
                    }
                    else
                    {
                        TestHelper.ExpectedException(() => action(), "Operation should have failed without using Https.", HttpStatusCode.Unused);
                    }
                }, null);

                RunQueueTest(policy, action =>
                {
                    action();
                }, queueHttpsPort);

                RunFileTest(policy, action =>
                {
                    if (!policy.Protocols.HasValue || (policy.Protocols == SharedAccessProtocol.HttpsOrHttp))
                    {
                        action();
                    }
                    else
                    {
                        TestHelper.ExpectedException(() => action(), "Operation should have failed without using Https.", HttpStatusCode.Unused);
                    }
                }, null);

                RunFileTest(policy, action =>
                {
                    action();
                }, fileHttpsPort);
            }
        }

        [TestMethod]
        [Description("Test account SAS all parameters blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASSample()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            string containerName = "c" + Guid.NewGuid().ToString("N");
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            try
            {
                container.Create();
                string blobName = "blob";
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                byte[] data = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                blockBlob.UploadFromByteArray(data, 0, 4);

                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                IPAddress invalidIP = IPAddress.Parse("255.255.255.255");
                policy.IPAddressOrRange = new IPAddressOrRange(invalidIP.ToString());

                CloudStorageAccount account = new CloudStorageAccount(blobClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, blobClient.StorageUri, null, null, null);
                CloudBlobClient blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
                CloudBlobContainer containerWithSAS = blobClientWithSAS.GetContainerReference(containerName);
                CloudBlockBlob blockblobWithSAS = containerWithSAS.GetBlockBlobReference(blobName);

                byte[] target = new byte[4];
                OperationContext opContext = new OperationContext();
                IPAddress actualIP = null;
                opContext.ResponseReceived += (sender, e) =>
                {
                    Stream stream = HttpResponseParsers.GetResponseStream(e.Response);
                    stream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string text = reader.ReadToEnd();
                        XDocument xdocument = XDocument.Parse(text);
                        actualIP = IPAddress.Parse(xdocument.Descendants("SourceIP").First().Value);
                    }
                };

                bool exceptionThrown = false;
                try
                {
                    blockblobWithSAS.DownloadRangeToByteArray(target, 0, 0, 4, null, null, opContext);
                }
                catch (StorageException)
                {
                    exceptionThrown = true;
                    //The IP should not be included in the error details for security reasons
                    Assert.IsNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);

                policy.IPAddressOrRange = null;
                accountSASToken = account.GetSharedAccessSignature(policy);
                accountSAS = new StorageCredentials(accountSASToken);
                accountWithSAS = new CloudStorageAccount(accountSAS, blobClient.StorageUri, null, null, null);
                blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
                containerWithSAS = blobClientWithSAS.GetContainerReference(containerName);
                blockblobWithSAS = containerWithSAS.GetBlockBlobReference(blobName);

                blockblobWithSAS.DownloadRangeToByteArray(target, 0, 0, 4, null, null, null);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(data[i], target[i]);
                }
                Assert.IsTrue(blockblobWithSAS.StorageUri.PrimaryUri.Equals(blockBlob.Uri));
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        private IPAddress GetMyBlobIPAddressFromService()
        {
            CloudBlobClient blobClient = GenerateCloudBlobClient();
            string containerName = "c" + Guid.NewGuid().ToString("N");
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            try
            {
                container.Create();
                string blobName = "blob";
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                byte[] data = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                blockBlob.UploadFromByteArray(data, 0, 4);

                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                IPAddress invalidIP = IPAddress.Parse("255.255.255.255");
                policy.IPAddressOrRange = new IPAddressOrRange(invalidIP.ToString());

                CloudStorageAccount account = new CloudStorageAccount(blobClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, blobClient.StorageUri, null, null, null);
                CloudBlobClient blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
                CloudBlobContainer containerWithSAS = blobClientWithSAS.GetContainerReference(containerName);
                CloudBlockBlob blockblobWithSAS = containerWithSAS.GetBlockBlobReference(blobName);

                byte[] target = new byte[4];
                IPAddress actualIP = null;
                bool exceptionThrown = false;
                try
                {
                    blockblobWithSAS.DownloadRangeToByteArray(target, 0, 0, 4);
                }
                catch (StorageException e)
                {
                    actualIP = IPAddress.Parse(e.RequestInformation.ExtendedErrorInformation.AdditionalDetails["SourceIP"]);
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                return actualIP;
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        private IPAddress GetMyQueueIPAddressFromService()
        {
            CloudQueueClient queueClient = GenerateCloudQueueClient();
            string queueName = "c" + Guid.NewGuid().ToString("N");
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            try
            {
                queue.Create();
                CloudQueueMessage message = new CloudQueueMessage("content");
                queue.AddMessage(message);

                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                IPAddress invalidIP = IPAddress.Parse("255.255.255.255");
                policy.IPAddressOrRange = new IPAddressOrRange(invalidIP.ToString());

                CloudStorageAccount account = new CloudStorageAccount(queueClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, queueClient.StorageUri, null, null);
                CloudQueueClient queueClientWithSAS = accountWithSAS.CreateCloudQueueClient();
                CloudQueue queueWithSAS = queueClientWithSAS.GetQueueReference(queueName);

                IPAddress actualIP = null;
                bool exceptionThrown = false;
                try
                {
                    queueWithSAS.GetMessage();
                }
                catch (StorageException e)
                {
                    actualIP = IPAddress.Parse(e.RequestInformation.ExtendedErrorInformation.AdditionalDetails["SourceIP"]);
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                return actualIP;
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        private IPAddress GetMyFileIPAddressFromService()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();
            string shareName = "c" + Guid.NewGuid().ToString("N");
            CloudFileShare share = fileClient.GetShareReference(shareName);
            try
            {
                share.Create();
                string fileName = "file";
                share.GetRootDirectoryReference().CreateIfNotExists();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(fileName);
                file.Create(1024);
                byte[] data = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                file.UploadFromByteArray(data, 0, 4);

                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                IPAddress invalidIP = IPAddress.Parse("255.255.255.255");
                policy.IPAddressOrRange = new IPAddressOrRange(invalidIP.ToString());

                CloudStorageAccount account = new CloudStorageAccount(fileClient.Credentials, false);
                string accountSASToken = account.GetSharedAccessSignature(policy);
                StorageCredentials accountSAS = new StorageCredentials(accountSASToken);
                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, null, null, fileClient.StorageUri);
                CloudFileClient fileClientWithSAS = accountWithSAS.CreateCloudFileClient();
                CloudFileShare shareWithSAS = fileClientWithSAS.GetShareReference(shareName);
                CloudFile fileWithSAS = shareWithSAS.GetRootDirectoryReference().GetFileReference(fileName);

                byte[] target = new byte[4];
                IPAddress actualIP = null;
                bool exceptionThrown = false;
                try
                {
                    fileWithSAS.DownloadRangeToByteArray(target, 0, 0, 4);
                }
                catch (StorageException e)
                {
                    actualIP = IPAddress.Parse(e.RequestInformation.ExtendedErrorInformation.AdditionalDetails["SourceIP"]);
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                return actualIP;
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        private SharedAccessAccountPolicy GetPolicyWithFullPermissions()
        {
            SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy();
            policy.SharedAccessStartTime = DateTime.Now - TimeSpan.FromMinutes(5);
            policy.SharedAccessExpiryTime = DateTime.Now + TimeSpan.FromMinutes(30);
            policy.Permissions = (SharedAccessAccountPermissions)(0x100 - 0x1);
            policy.Services = (SharedAccessAccountServices)(0x10 - 0x1);
            policy.ResourceTypes = (SharedAccessAccountResourceTypes)(0x8 - 0x1);
            policy.Protocols = SharedAccessProtocol.HttpsOrHttp;
            policy.IPAddressOrRange = null;
            return policy;
        }

        private static Uri TransformSchemeAndPort(Uri input, string scheme, int port)
        {
            UriBuilder builder = new UriBuilder(input);
            builder.Scheme = scheme;
            builder.Port = port;
            return builder.Uri;
        }
    }
}
