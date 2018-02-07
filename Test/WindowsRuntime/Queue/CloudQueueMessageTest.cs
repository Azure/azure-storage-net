﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueueMessageTest.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Queue
{
    [TestClass]
    public class CloudQueueMessageTest : QueueTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        public CloudQueueMessageTest()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.QueueBufferManager != null)
            {
                TestBase.QueueBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.QueueBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.QueueBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Test CloudQueueMessage constructor.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueCreateMessageAsync()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                await queue.CreateIfNotExistsAsync();

                CloudQueueMessage message = new CloudQueueMessage(Guid.NewGuid().ToString());
                await queue.AddMessageAsync(message);
                VerifyAddMessageResult(message);

                CloudQueueMessage retrMessage = await queue.GetMessageAsync();
                string messageId = retrMessage.Id;
                string popReceipt = retrMessage.PopReceipt;

                // Recreate the message using the messageId and popReceipt.
                CloudQueueMessage newMessage = new CloudQueueMessage(messageId, popReceipt);
                Assert.AreEqual(messageId, newMessage.Id);
                Assert.AreEqual(popReceipt, newMessage.PopReceipt);

                await queue.UpdateMessageAsync(newMessage, TimeSpan.FromSeconds(30), MessageUpdateFields.Visibility);
                CloudQueueMessage retrMessage2 = await queue.GetMessageAsync();
                Assert.AreEqual(null, retrMessage2);
            }
            finally
            {
                queue.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test add message with full parameter.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueAddMessageFullParameterAsync()
        {
            CloudQueueMessage futureMessage = new CloudQueueMessage("This message is for the future.");
            CloudQueueMessage presentMessage = new CloudQueueMessage("This message is for the present.");

            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                await queue.CreateIfNotExistsAsync();

                await queue.AddMessageAsync(futureMessage, null, TimeSpan.FromDays(2), null, null);
                VerifyAddMessageResult(futureMessage);

                // We should not be able to see the future message yet.
                CloudQueueMessage retrievedMessage = await queue.GetMessageAsync();
                Assert.IsNull(retrievedMessage);

                await queue.AddMessageAsync(presentMessage, null, TimeSpan.Zero, null, null);
                VerifyAddMessageResult(presentMessage);
                await queue.AddMessageAsync(presentMessage, TimeSpan.FromDays(1), null, null, null);
                VerifyAddMessageResult(presentMessage);

                // We should be able to see the present message.
                retrievedMessage = await queue.GetMessageAsync();
                Assert.IsNotNull(retrievedMessage);
                Assert.AreEqual<string>(presentMessage.AsString, retrievedMessage.AsString);

                await queue.AddMessageAsync(futureMessage, TimeSpan.FromDays(2), TimeSpan.FromDays(1), null, null);
                VerifyAddMessageResult(futureMessage);

                await queue.ClearAsync();

                // -1 seconds should set an infinite ttl
                await queue.AddMessageAsync(presentMessage, TimeSpan.FromSeconds(-1), null, null, null);
                retrievedMessage = await queue.PeekMessageAsync();
                Assert.AreEqual(DateTime.MaxValue.Year, retrievedMessage.ExpirationTime.Value.Year);

                // There should be no upper bound on ttl
                await queue.AddMessageAsync(presentMessage, TimeSpan.MaxValue, null, null, null);

                // Check other edge cases
                await queue.AddMessageAsync(presentMessage, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0), null, null);
                await queue.AddMessageAsync(presentMessage, TimeSpan.FromSeconds(7 * 24 * 60 * 60), TimeSpan.FromSeconds(7 * 24 * 60 * 60 - 1), null, null);
                await queue.AddMessageAsync(presentMessage, TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1), null, null);

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                            async () => await queue.AddMessageAsync(futureMessage, TimeSpan.FromDays(1), TimeSpan.FromDays(2), null, null),
                            "Using a visibility timeout longer than the time to live should fail");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                            async () => await queue.AddMessageAsync(futureMessage, null, TimeSpan.FromDays(8), null, null),
                            "Using a visibility longer than the maximum visibility timeout should fail");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                            async () => await queue.AddMessageAsync(futureMessage, null, TimeSpan.FromMinutes(-1), null, null),
                            "Using a negative visibility should fail");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                            async () => await queue.AddMessageAsync(futureMessage, TimeSpan.FromMinutes(-1), null, null, null),
                            "Using a negative TTL other than -1 seconds (infinite) should fail");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        () => queue.AddMessageAsync(futureMessage, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), null, null),
                        "Visibility timeout must be strictly less than the TTL");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        () => queue.AddMessageAsync(presentMessage, null, CloudQueueMessage.MaxVisibilityTimeout, null, null),
                        "Null TTL will default to 7 days, which is the max visibility timeout. They cannot be equal.");
            }
            finally
            {
                queue.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test that add message does not alter content.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueAddMessageVerifyContent()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                await queue.CreateIfNotExistsAsync();

                string msgContent = Guid.NewGuid().ToString("N");
                CloudQueueMessage message = new CloudQueueMessage(msgContent);
                message.NextVisibleTime = null;

                await queue.AddMessageAsync(message);
                VerifyAddMessageResult(message);
                Assert.IsTrue(message.AsString == msgContent);
            }
            finally
            {
                queue.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test that the pop receipt returned by add message works for deleting a message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueDeleteMessageWithAddMessagePopReceipt()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            await queue.CreateAsync();

            string msgContent = Guid.NewGuid().ToString("N");
            CloudQueueMessage message = new CloudQueueMessage(msgContent);
            await queue.AddMessageAsync(message);
            VerifyAddMessageResult(message);
            await queue.DeleteMessageAsync(message.Id, message.PopReceipt);

            CloudQueueMessage receivedMessage = await queue.GetMessageAsync();
            Assert.IsNull(receivedMessage);

            await queue.DeleteAsync();
        }

        [TestMethod]
        [Description("Test add/delete message")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueMessageAddDelete()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            await queue.CreateAsync();

            await queue.AddMessageAsync(new CloudQueueMessage("abcde"));

            CloudQueueMessage receivedMessage1 = await queue.GetMessageAsync();

            await queue.DeleteMessageAsync(receivedMessage1.Id, receivedMessage1.PopReceipt);

            CloudQueueMessage receivedMessage2 = await queue.GetMessageAsync();
            Assert.IsNull(receivedMessage2);
        }

        [TestMethod]
        [Description("Test whether get message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueGetMessageAsync()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            await queue.CreateAsync();

            CloudQueueMessage emptyMessage = await queue.GetMessageAsync();
            Assert.IsNull(emptyMessage);

            string msgContent = Guid.NewGuid().ToString("N");
            CloudQueueMessage message = new CloudQueueMessage(msgContent);
            await queue.AddMessageAsync(message);
            VerifyAddMessageResult(message);

            CloudQueueMessage receivedMessage1 = await queue.GetMessageAsync();

            Assert.IsTrue(receivedMessage1.AsString == message.AsString);

            await queue.DeleteAsync();
        }

        [TestMethod]
        [Description("Test whether get messages.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueGetMessagesAsync()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            await queue.CreateAsync();

            int messageCount = 30;

            List<CloudQueueMessage> emptyMessages = (await queue.GetMessagesAsync(messageCount)).ToList();
            Assert.AreEqual(0, emptyMessages.Count);

            List<string> messageContentList = new List<string>();
            for (int i = 0; i < messageCount; i++)
            {
                string messageContent = i.ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageContent);
                await queue.AddMessageAsync(message);
                VerifyAddMessageResult(message);
                messageContentList.Add(messageContent);
            }

            List<CloudQueueMessage> receivedMessages = (await queue.GetMessagesAsync(messageCount)).ToList();
            Assert.AreEqual(messageCount, receivedMessages.Count);

            for (int i = 0; i < messageCount; i++)
            {
                Assert.IsTrue(messageContentList.Contains(receivedMessages[i].AsString));
            }

            await queue.DeleteAsync();
        }

        [TestMethod]
        [Description("Test whether peek message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueuePeekMessageAsync()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            await queue.CreateAsync();

            CloudQueueMessage emptyMessage = await queue.PeekMessageAsync();
            Assert.IsNull(emptyMessage);

            string msgContent = Guid.NewGuid().ToString("N");
            CloudQueueMessage message = new CloudQueueMessage(msgContent);
            await queue.AddMessageAsync(message);
            VerifyAddMessageResult(message);

            CloudQueueMessage receivedMessage1 = await queue.PeekMessageAsync();

            Assert.IsTrue(receivedMessage1.AsString == message.AsString);

            await queue.DeleteAsync();
        }

        [TestMethod]
        [Description("Test whether peek messages.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueuePeekMessagesAsync()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            await queue.CreateAsync();

            int messageCount = 30;

            List<CloudQueueMessage> emptyMessages = (await queue.PeekMessagesAsync(messageCount)).ToList();
            Assert.AreEqual(0, emptyMessages.Count);

            List<string> messageContentList = new List<string>();
            for (int i = 0; i < messageCount; i++)
            {
                string messageContent = i.ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageContent);
                await queue.AddMessageAsync(message);
                VerifyAddMessageResult(message);
                messageContentList.Add(messageContent);
            }

            List<CloudQueueMessage> receivedMessages = (await queue.PeekMessagesAsync(messageCount)).ToList();
            Assert.AreEqual(messageCount, receivedMessages.Count);

            for (int i = 0; i < messageCount; i++)
            {
                Assert.IsTrue(messageContentList.Contains(receivedMessages[i].AsString));
            }

            await queue.DeleteAsync();
        }

        [TestMethod]
        [Description("Test whether clear message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudQueueClearMessageAsync()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            await queue.CreateAsync();

            string msgContent = Guid.NewGuid().ToString("N");
            CloudQueueMessage message = new CloudQueueMessage(msgContent);
            await queue.AddMessageAsync(message);
            VerifyAddMessageResult(message);
            CloudQueueMessage receivedMessage1 = await queue.PeekMessageAsync();
            Assert.IsTrue(receivedMessage1.AsString == message.AsString);
            await queue.ClearAsync();
            Assert.IsNull(await queue.PeekMessageAsync());
            await queue.DeleteAsync();
        }

#region Test Helpers
        private void VerifyAddMessageResult(CloudQueueMessage originalMessage, bool base64Encoded = false)
        {
            Assert.IsFalse(string.IsNullOrEmpty(originalMessage.Id));
            Assert.IsTrue(originalMessage.InsertionTime.HasValue);
            Assert.IsTrue(originalMessage.ExpirationTime.HasValue);
            Assert.IsFalse(string.IsNullOrEmpty(originalMessage.PopReceipt));

            Assert.IsTrue(originalMessage.NextVisibleTime.HasValue);
        }
#endregion
    }
}