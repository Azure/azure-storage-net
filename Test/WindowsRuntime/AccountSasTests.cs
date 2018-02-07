﻿// -----------------------------------------------------------------------------------------
// <copyright file="AccountSasTests.cs" company="Microsoft">
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
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.File;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.Queue.Protocol;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Shared;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Windows.Networking;
    using Windows.Storage.Streams;
    using Windows.Networking.Connectivity;

    [TestClass]
    public class AccountSASTests : TestBase
    {
        [TestMethod]
        [Description("Test account SAS all permissions, all services")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASPermissions()
        {
            // Single-threaded, takes 10 minutes to run
            // Parallelized, 1 minute.
            for (int i = 0; i < 0x100; i++)
            {
                Task[] tasks = new Task[3]; //each permission (0x100) times four services.
                SharedAccessAccountPermissions permissions = (SharedAccessAccountPermissions)i;
                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                policy.Permissions = permissions;
                tasks[0] = this.RunPermissionsTestBlobs(policy);
                tasks[1] = this.RunPermissionsTestQueues(policy);
                tasks[2] = this.RunPermissionsTestFiles(policy);
                Task.WaitAll(tasks);
            }
            
        }

        [TestMethod]
        [Description("Test account SAS various combinations of resource types")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void AccountSASResourceTypes()
        {
            Task[] tasks = new Task[8*3];
            for (int i = 0; i < 0x8; i++)
            {
                SharedAccessAccountResourceTypes resourceTypes = (SharedAccessAccountResourceTypes)i;
                SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
                policy.ResourceTypes = resourceTypes;
                tasks[i] = this.RunPermissionsTestBlobs(policy);
                tasks[8 + i] = this.RunPermissionsTestQueues(policy);
                tasks[16 + i] = this.RunPermissionsTestFiles(policy);
            }
            Task.WaitAll(tasks);
        }

        
       [TestMethod]
       [Description("Test account SAS various combinations of services")]
       [TestCategory(ComponentCategory.Core)]
       [TestCategory(TestTypeCategory.UnitTest)]
       [TestCategory(SmokeTestCategory.NonSmoke)]
       [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
       public async Task AccountSASServices()
       {
           for (int i = 1; i < 0x10; i++)
           {
               OperationContext opContext = new OperationContext();
               SharedAccessAccountPolicy policy = this.GetPolicyWithFullPermissions();
               policy.Services = (SharedAccessAccountServices)i;
               bool expectBlobException = !((policy.Services & SharedAccessAccountServices.Blob) == SharedAccessAccountServices.Blob);
               bool expectQueueException = !((policy.Services & SharedAccessAccountServices.Queue) == SharedAccessAccountServices.Queue);
               bool expectTableException = !((policy.Services & SharedAccessAccountServices.Table) == SharedAccessAccountServices.Table);
               bool expectFileException = !((policy.Services & SharedAccessAccountServices.File) == SharedAccessAccountServices.File);
               
               if (expectBlobException)
               {
                   await TestHelper.ExpectedExceptionAsync((async () => await RunBlobTest(policy, null, opContext)), opContext, "Operation should have failed without Blob access.", HttpStatusCode.Forbidden, "AuthorizationServiceMismatch");
               }
               else
               {
                   await RunBlobTest(policy, null);
               }

               if (expectQueueException)
               {
                   await TestHelper.ExpectedExceptionAsync((async () => await RunQueueTest(policy, null, opContext)), opContext, "Operation should have failed without Queue access.", HttpStatusCode.Forbidden, "AuthorizationServiceMismatch");
               }
               else
               {
                   await RunQueueTest(policy, null);
               }

               if (expectFileException)
               {
                   await TestHelper.ExpectedExceptionAsync((async () => await RunFileTest(policy, null, opContext)), opContext, "Operation should have failed without File access.", HttpStatusCode.Forbidden, "AuthorizationServiceMismatch");
               }
               else 
               {
                   await RunFileTest(policy, null);
               }
           }
       } 

        [TestMethod]
        [Description("Test account SAS various combinations of start and expiry times")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AccountSASStartExpiryTimes()
        {
            int?[] startOffsets = new int?[] { null, -5, 5 };
            int[] endOffsets = new int[] { -5, 5 };

            foreach (int? startOffset in startOffsets)
            {
                foreach (int endOffset in endOffsets)
                {
                    OperationContext opContext = new OperationContext();
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

                    bool expectException;
                    if (!policy.SharedAccessStartTime.HasValue)
                    {
                        expectException = (policy.SharedAccessExpiryTime < DateTime.Now);
                    }
                    else
                    {
                        expectException = ((policy.SharedAccessStartTime.Value > DateTime.Now) || (policy.SharedAccessExpiryTime < DateTime.Now));
                    }
                   
                    if (expectException)
                    {
                        await TestHelper.ExpectedExceptionAsync((async () => await RunBlobTest(policy, null, opContext)), opContext, "Operation should have failed with invalid start/expiry times.", HttpStatusCode.Forbidden, "AuthenticationFailed");
                        await TestHelper.ExpectedExceptionAsync((async () => await RunQueueTest(policy, null, opContext)), opContext, "Operation should have failed with invalid start/expiry times.", HttpStatusCode.Forbidden, "AuthenticationFailed");
                        await TestHelper.ExpectedExceptionAsync((async () => await RunFileTest(policy, null, opContext)), opContext, "Operation should have failed with invalid start/expiry times.", HttpStatusCode.Forbidden, "AuthenticationFailed");
                    }
                    else
                    {
                        await RunBlobTest(policy, null);
                        await RunQueueTest(policy, null);
                        await RunFileTest(policy, null);
                    }
                } 
            }
        }

        [TestMethod]
        [Description("Test account SAS various combinations of signedIPs")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AccountSASSignedIPs()
        {
            OperationContext opContext = new OperationContext();
            HostName invalidIP = new HostName("255.255.255.255");

            SharedAccessAccountPolicy policy = GetPolicyWithFullPermissions();
            policy.IPAddressOrRange = new IPAddressOrRange(invalidIP.ToString());

            await TestHelper.ExpectedExceptionAsync((async () => await RunBlobTest(policy, null, opContext)), opContext, "Operation should have failed with invalid IP access.", HttpStatusCode.Forbidden, "AuthorizationFailure");
            await TestHelper.ExpectedExceptionAsync((async () => await RunQueueTest(policy, null, opContext)), opContext, "Operation should have failed with invalid IP access.", HttpStatusCode.Forbidden, "AuthorizationFailure");
            await TestHelper.ExpectedExceptionAsync((async () => await RunFileTest(policy, null, opContext)), opContext, "Operation should have failed with invalid IP access.", HttpStatusCode.Forbidden, "AuthorizationFailure");

            policy.IPAddressOrRange = null;
            await RunBlobTest(policy, null);

            await RunQueueTest(policy, null);

            await RunFileTest(policy, null);

            policy.IPAddressOrRange = new IPAddressOrRange(new HostName("255.255.255.0").ToString(), invalidIP.ToString());

            await TestHelper.ExpectedExceptionAsync((async () => await RunBlobTest(policy, null, opContext)), opContext, "Operation should have failed with invalid IP access.", HttpStatusCode.Forbidden, "AuthorizationFailure");
            await TestHelper.ExpectedExceptionAsync((async () => await RunQueueTest(policy, null, opContext)), opContext, "Operation should have failed with invalid IP access.", HttpStatusCode.Forbidden, "AuthorizationFailure");
            await TestHelper.ExpectedExceptionAsync((async () => await RunFileTest(policy, null, opContext)), opContext, "Operation should have failed with invalid IP access.", HttpStatusCode.Forbidden, "AuthorizationFailure");
        }
        
        [TestMethod]
        [Description("Test account SAS various combinations of signed protocols")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AccountSASSignedProtocols()
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

            OperationContext opContext = new OperationContext();
            for (int i = 0; i < 3; i++)
            {
                SharedAccessAccountPolicy policy = this.GetPolicyWithFullPermissions();
                policy.Protocols = i == 0 ? (SharedAccessProtocol?)null : (SharedAccessProtocol)i;

                bool expectException = !(!policy.Protocols.HasValue || (policy.Protocols == SharedAccessProtocol.HttpsOrHttp));

                if (expectException)
                {
                    await TestHelper.ExpectedExceptionAsync((async () => await RunBlobTest(policy, null, opContext)), opContext, "Operation should have failed without using Https.", HttpStatusCode.Unused, null);
                    await TestHelper.ExpectedExceptionAsync((async () => await RunQueueTest(policy, null, opContext)), opContext, "Operation should have failed without using Https.", HttpStatusCode.Unused, null);
                    await TestHelper.ExpectedExceptionAsync((async () => await RunFileTest(policy, null, opContext)), opContext, "Operation should have failed without using Https.", HttpStatusCode.Unused, null);
                }
                else
                {
                    await RunBlobTest(policy, null);
                    await RunQueueTest(policy, null);
                    await RunFileTest(policy, null);
                }

                await RunBlobTest(policy, blobHttpsPort);
                await RunQueueTest(policy, queueHttpsPort);
                await RunFileTest(policy, fileHttpsPort);
            }
        }

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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await containerWithSAS.CreateAsync(), "Create a container should fail with SAS without Create or Write and Container-level permissions.");
                    await container.CreateAsync();
                }

                Assert.IsTrue(await container.ExistsAsync());

                if (((policy.Permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service))
                {
                    ContainerResultSegment segment = null;
                    BlobContinuationToken ct =null;
                    IEnumerable<CloudBlobContainer> results = null;
                    do
                    {
                        segment = await blobClientWithSAS.ListContainersSegmentedAsync(container.Name, ct);
                        ct = segment.ContinuationToken;
                        results = segment.Results;

                    }while(ct != null && !results.Any());

                    Assert.AreEqual(container.Name, segment.Results.First().Name);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => { ContainerResultSegment segment = await blobClientWithSAS.ListContainersSegmentedAsync(container.Name, null); segment.Results.First(); }, "List containers should fail with SAS without List and Service-level permissions.");
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await appendBlobWithSAS.CreateOrReplaceAsync(), "Creating an append blob should fail with SAS without Create or Write and Object-level perms.");
                    await appendBlob.CreateOrReplaceAsync();
                }

                Assert.IsTrue(await appendBlob.ExistsAsync());

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
                    using (MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(blobText)))
                    {
                        await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await appendBlobWithSAS.AppendBlockAsync(memStream), "Append a block to an append blob should fail with SAS without Add or Write and Object-level perms.");
                        memStream.Seek(0, SeekOrigin.Begin);
                        await appendBlob.AppendBlockAsync(memStream);
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await appendBlobWithSAS.DownloadTextAsync(), "Reading a blob's contents with SAS without Read and Object-level permissions should fail.");
                }

                if (((policy.Permissions & SharedAccessAccountPermissions.Delete) == SharedAccessAccountPermissions.Delete) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await appendBlobWithSAS.DeleteAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await appendBlobWithSAS.DeleteAsync(), "Deleting a blob with SAS without Delete and Object-level perms should fail.");
                    await appendBlob.DeleteAsync();
                }

                Assert.IsFalse(await appendBlob.ExistsAsync());
            }
            finally
            {
                blobClient.GetContainerReference(containerName).DeleteIfExistsAsync().Wait();
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.CreateAsync(), "Creating a queue with SAS should fail without Add and Container-level permissions.");
                    await queue.CreateAsync();
                }
                Assert.IsTrue(await queue.ExistsAsync());

                if (((policy.Permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service))
                {
                    Assert.AreEqual(queueName, (await queueClientWithSAS.ListQueuesSegmentedAsync(queueName, null)).Results.First().Name);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => (await queueClientWithSAS.ListQueuesSegmentedAsync(queueName, null)).Results.First(), "Listing queues with SAS should fail without Read and Service-level permissions.");
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.SetMetadataAsync(), "Setting a queue's metadata with SAS should fail without Write and Container-level permissions.");
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.AddMessageAsync(message), "Adding a queue message should fail with SAS without Add and Object-level permissions.");
                    await queue.AddMessageAsync(message);
                }
                Assert.AreEqual(messageText, ((await queue.PeekMessageAsync()).AsString));

                if (((policy.Permissions & SharedAccessAccountPermissions.Read) == SharedAccessAccountPermissions.Read) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    Assert.AreEqual(messageText, (await queueWithSAS.PeekMessageAsync()).AsString);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.PeekMessageAsync(), "Peeking a queue message should fail with SAS without Read and Object-level permissions.");
                }

                CloudQueueMessage messageResult = null;
                if (((policy.Permissions & SharedAccessAccountPermissions.ProcessMessages) == SharedAccessAccountPermissions.ProcessMessages) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    messageResult = await queueWithSAS.GetMessageAsync();
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.GetMessageAsync(), "Getting a message should fail with SAS without Process and Object-level permissions.");
                    messageResult = await queue.GetMessageAsync();
                }
                Assert.AreEqual(messageText, messageResult.AsString);

                string newMessageContent = "new content";
                messageResult.SetMessageContent(newMessageContent);
                if (((policy.Permissions & SharedAccessAccountPermissions.Update) == SharedAccessAccountPermissions.Update) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await queueWithSAS.UpdateMessageAsync(messageResult, TimeSpan.Zero, MessageUpdateFields.Content | MessageUpdateFields.Visibility);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.UpdateMessageAsync(messageResult, TimeSpan.Zero, MessageUpdateFields.Content | MessageUpdateFields.Visibility), "Updating a message should fail with SAS without Update and Object-level permissions.");
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await queueWithSAS.ClearAsync(), "Clearing messages should fail with SAS without delete and Object-level permissions.");
                }
            }
            finally
            {
                queueClient.GetQueueReference(queueName).DeleteIfExistsAsync().Wait();
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await shareWithSAS.CreateAsync(), "Creating a share with SAS should fail without Create or Write and Container-level perms.");
                    await share.CreateAsync();
                }
                Assert.IsTrue(await share.ExistsAsync());

                if (((policy.Permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service))
                {
                    Assert.AreEqual(shareName, (await fileClientWithSAS.ListSharesSegmentedAsync(shareName, null)).Results.First().Name);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => (await fileClientWithSAS.ListSharesSegmentedAsync(shareName, null)).Results.First(), "Listing shared with SAS should fail without List and Service-level perms.");
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await fileWithSAS.CreateAsync(content.Length), "Creating a file with SAS should fail without Create or Write and Object-level perms.");
                    await file.CreateAsync(content.Length);
                }
                Assert.IsTrue(await file.ExistsAsync());

                using (MemoryStream stream = new MemoryStream(content))
                {
                    if (((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write) &&
                        ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                    {
                        await fileWithSAS.WriteRangeAsync(stream, 0, null);
                    }
                    else
                    {
                        await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await fileWithSAS.WriteRangeAsync(stream, 0, null), "Writing a range to a file with SAS should fail without Write and Object-level perms.");
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
                    await fileWithSAS.DownloadRangeToByteArrayAsync(result, 0, 0, content.Length);
                    for (int i = 0; i < content.Length; i++)
                    {
                        Assert.AreEqual(content[i], result[i]);
                    }
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await fileWithSAS.DownloadRangeToByteArrayAsync(result, 0, 0, content.Length), "Reading a file with SAS should fail without Read and Object-level perms.");
                }

                if (((policy.Permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write) &&
                    ((policy.ResourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object))
                {
                    await fileWithSAS.CreateAsync(2);
                }
                else
                {
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await fileWithSAS.CreateAsync(2), "Overwriting a file with SAS should fail without Write and Object-level perms.");
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
                    await TestHelper.ExpectedExceptionAsync<StorageException>(async () => await fileWithSAS.DeleteAsync(), "Deleting a file with SAS should fail without Delete and Object-level perms.");
                    await file.DeleteAsync();
                }

                Assert.IsFalse(await file.ExistsAsync());
            }
            finally
            {
                fileClient.GetShareReference(shareName).DeleteIfExistsAsync().Wait();
            }
        }

        public async Task RunBlobTest(SharedAccessAccountPolicy policy, int? httpsPort, OperationContext opContext = null)
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

                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, storageUri, null, null, null);
                CloudBlobClient blobClientWithSAS = accountWithSAS.CreateCloudBlobClient();
                CloudBlobContainer containerWithSAS = blobClientWithSAS.GetContainerReference(containerName);
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                await container.CreateAsync();
                string blobName = "blob";
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
                string blobText = "blobText";
                await blob.UploadTextAsync(blobText);

                CloudBlockBlob blobWithSAS = containerWithSAS.GetBlockBlobReference(blobName);

                Assert.AreEqual(blobText, await blobWithSAS.DownloadTextAsync(null, null, opContext));
            }
            finally
            {
                blobClient.GetContainerReference(containerName).DeleteIfExistsAsync().Wait();
            }
        }


        public async Task RunQueueTest(SharedAccessAccountPolicy policy, int? httpsPort, OperationContext opContext = null)
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

                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, storageUri, null, null);
                CloudQueueClient queueClientWithSAS = accountWithSAS.CreateCloudQueueClient();
                CloudQueue queueWithSAS = queueClientWithSAS.GetQueueReference(queueName);
                CloudQueue queue = queueClient.GetQueueReference(queueName);
                await queue.CreateAsync();
                string messageText = "message text";
                CloudQueueMessage message = new CloudQueueMessage(messageText);
                await queue.AddMessageAsync(message);

                Assert.AreEqual(messageText, (await queueWithSAS.GetMessageAsync(null, null, opContext)).AsString);
            }
            finally
            {
                queueClient.GetQueueReference(queueName).DeleteIfExistsAsync().Wait();
            }
        }

        public async Task RunFileTest(SharedAccessAccountPolicy policy, int? httpsPort, OperationContext opContext = null)
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

                CloudStorageAccount accountWithSAS = new CloudStorageAccount(accountSAS, null, null, null, storageUri);
                CloudFileClient fileClientWithSAS = accountWithSAS.CreateCloudFileClient();
                CloudFileShare shareWithSAS = fileClientWithSAS.GetShareReference(shareName);
                CloudFileShare share = fileClient.GetShareReference(shareName);
                await share.CreateAsync();
                string fileName = "file";
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(fileName);
                CloudFile fileWithSAS = shareWithSAS.GetRootDirectoryReference().GetFileReference(fileName);
                byte[] content = new byte[] { 0x1, 0x2, 0x3, 0x4 };
                await file.CreateAsync(content.Length);
                using (MemoryStream stream = new MemoryStream(content))
                {
                    await file.WriteRangeAsync(stream, 0, null);
                }

                byte[] result = new byte[content.Length];
                await fileWithSAS.DownloadRangeToByteArrayAsync(result, 0, 0, content.Length, null, null, opContext);
                for (int i = 0; i < content.Length; i++)
                {
                    Assert.AreEqual(content[i], result[i]);
                }
            }
            finally
            {
                fileClient.GetShareReference(shareName).DeleteIfExistsAsync().Wait();
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
