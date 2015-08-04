// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueueMeesageEncryptionTests.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Core;
    using Newtonsoft.Json;
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    [TestClass]
    public class CloudQueueMessageEncryptionTests : QueueTestBase 
    {
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.QueueBufferManager != null)
            {
                TestBase.QueueBufferManager.OutstandingBufferCount = 0;
            }
        }

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
        [Description("Test adding/updating encrypted message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueAddUpdateEncryptedMessage()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            RsaKey rsaKey = new RsaKey("asymencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);
            resolver.Add(rsaKey);

            DoCloudQueueAddUpdateEncryptedMessage(aesKey, resolver);
            DoCloudQueueAddUpdateEncryptedMessage(rsaKey, resolver);
        }

        private void DoCloudQueueAddUpdateEncryptedMessage(IKey key, DictionaryKeyResolver keyResolver)
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                queue.CreateIfNotExists();

                string messageStr = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageStr);

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(key, null);

                // Add message
                QueueRequestOptions createOptions = new QueueRequestOptions() { EncryptionPolicy = policy };
                queue.AddMessage(message, null, null, createOptions, null);

                // Retrieve message
                QueueEncryptionPolicy retrPolicy = new QueueEncryptionPolicy(null, keyResolver);
                QueueRequestOptions retrieveOptions = new QueueRequestOptions() { EncryptionPolicy = retrPolicy };
                CloudQueueMessage retrMessage = queue.GetMessage(null, retrieveOptions, null);
                Assert.AreEqual(messageStr, retrMessage.AsString);

                // Update message
                string updatedMessage = Guid.NewGuid().ToString("N");
                retrMessage.SetMessageContent(updatedMessage);
                queue.UpdateMessage(retrMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Content | MessageUpdateFields.Visibility, createOptions, null);

                // Retrieve updated message
                retrMessage = queue.GetMessage(null, retrieveOptions, null);
                Assert.AreEqual(updatedMessage, retrMessage.AsString);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test adding/updating encrypted message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueAddUpdateEncryptedMessageAPM()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            RsaKey rsaKey = new RsaKey("asymencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);
            resolver.Add(rsaKey);

            DoCloudQueueAddUpdateEncryptedMessageAPM(aesKey, resolver);
            DoCloudQueueAddUpdateEncryptedMessageAPM(rsaKey, resolver);
        }

        private void DoCloudQueueAddUpdateEncryptedMessageAPM(IKey key, DictionaryKeyResolver keyResolver)
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.CreateIfNotExists();

                string messageStr = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageStr);

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(key, null);
                QueueRequestOptions createOptions = new QueueRequestOptions() { EncryptionPolicy = policy };

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    // Add message
                    IAsyncResult result = queue.BeginAddMessage(message, null, null, createOptions, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queue.EndAddMessage(result);

                    policy = new QueueEncryptionPolicy(null, keyResolver);
                    QueueRequestOptions retrieveOptions = new QueueRequestOptions() { EncryptionPolicy = policy };

                    // Retrieve message
                    result = queue.BeginGetMessage(null, retrieveOptions, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudQueueMessage retrMessage = queue.EndGetMessage(result);
                    Assert.AreEqual(messageStr, retrMessage.AsString);

                    // Update message
                    string updatedMessage = Guid.NewGuid().ToString("N");
                    retrMessage.SetMessageContent(updatedMessage);
                    result = queue.BeginUpdateMessage(retrMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Content | MessageUpdateFields.Visibility, createOptions, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queue.EndUpdateMessage(result);

                    // Retrieve updated message
                    result = queue.BeginGetMessage(null, retrieveOptions, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    retrMessage = queue.EndGetMessage(result);
                    Assert.AreEqual(updatedMessage, retrMessage.AsString);
                }
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test adding/updating binary message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueAddUpdateEncryptedBinaryMessage()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                queue.CreateIfNotExists();

                byte[] messageBytes = new byte[100];
                Random rand = new Random();
                rand.NextBytes(messageBytes);

                CloudQueueMessage message = new CloudQueueMessage(messageBytes);

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(aesKey, null);
                QueueRequestOptions options = new QueueRequestOptions() { EncryptionPolicy = policy };

                // Add message
                queue.AddMessage(message, null, null, options, null);

                // Retrieve message
                CloudQueueMessage retrMessage = queue.GetMessage(null, options, null);
                TestHelper.AssertBuffersAreEqual(messageBytes, retrMessage.AsBytes);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test adding/updating base 64 encoded message.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueAddUpdateEncryptedEncodedMessage()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                queue.CreateIfNotExists();

                byte[] messageBytes = new byte[100];
                Random rand = new Random();
                rand.NextBytes(messageBytes);

                string inputMessage = Convert.ToBase64String(messageBytes);
                CloudQueueMessage message = new CloudQueueMessage(inputMessage);
                queue.EncodeMessage = false;

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(aesKey, null);
                QueueRequestOptions options = new QueueRequestOptions() { EncryptionPolicy = policy };

                // Add message
                queue.AddMessage(message, null, null, options, null);

                // Retrieve message
                CloudQueueMessage retrMessage = queue.GetMessage(null, options, null);
                Assert.AreEqual(inputMessage, retrMessage.AsString);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Adding a 64KB message should fail if encryption is enabled.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueAddEncrypted64KMessage()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                queue.CreateIfNotExists();

                string inputMessage = new string('a', 64 * 1024);
                CloudQueueMessage message = new CloudQueueMessage(inputMessage);
                queue.EncodeMessage = false;

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(aesKey, null);
                QueueRequestOptions options = new QueueRequestOptions() { EncryptionPolicy = policy };

                // Add message
                queue.AddMessage(message, null, null, null, null);

                // Add encrypted Message
                TestHelper.ExpectedException<ArgumentException>(
                    () => queue.AddMessage(message, null, null, options, null),
                    "Adding an encrypted message that exceeds message limits should throw");
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate queue message encryption.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueMessageValidateEncryption()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                queue.CreateIfNotExists();

                byte[] messageBytes = new byte[100];
                Random rand = new Random();
                rand.NextBytes(messageBytes);

                string inputMessage = Convert.ToBase64String(messageBytes);
                CloudQueueMessage message = new CloudQueueMessage(inputMessage);
                queue.EncodeMessage = false;

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(aesKey, null);
                QueueRequestOptions options = new QueueRequestOptions() { EncryptionPolicy = policy };

                // Add message
                queue.AddMessage(message, null, null, options, null);

                // Retrieve message without decrypting
                CloudQueueMessage retrMessage = queue.GetMessage(null, null, null);

                // Decrypt locally
                CloudQueueMessage decryptedMessage;
                CloudQueueEncryptedMessage encryptedMessage = JsonConvert.DeserializeObject<CloudQueueEncryptedMessage>(retrMessage.AsString);
                EncryptionData encryptionData = encryptedMessage.EncryptionData;

                byte[] contentEncryptionKey = aesKey.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result;

                using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
                {
                    myAes.Key = contentEncryptionKey;
                    myAes.IV = encryptionData.ContentEncryptionIV;

                    byte[] src = Convert.FromBase64String(encryptedMessage.EncryptedMessageContents);
                    using (ICryptoTransform decryptor = myAes.CreateDecryptor())
                    {
                        decryptedMessage = new CloudQueueMessage(decryptor.TransformFinalBlock(src, 0, src.Length));
                    }
                }

                TestHelper.AssertBuffersAreEqual(message.AsBytes, decryptedMessage.AsBytes);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test adding/retrieving message using RequireEncryption flag.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueMessageEncryptionWithStrictMode()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            CloudQueueClient client = GenerateCloudQueueClient();
            string name = GenerateNewQueueName();
            CloudQueue queue = client.GetQueueReference(name);
            try
            {
                queue.CreateIfNotExists();

                string messageStr = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageStr);

                QueueEncryptionPolicy policy = new QueueEncryptionPolicy(aesKey, null);

                // Add message with policy.
                QueueRequestOptions createOptions = new QueueRequestOptions() { EncryptionPolicy = policy };
                createOptions.RequireEncryption = true;

                queue.AddMessage(message, null, null, createOptions, null);

                // Set policy to null and add message while RequireEncryption flag is still set to true. This should throw.
                createOptions.EncryptionPolicy = null;

                TestHelper.ExpectedException<InvalidOperationException>(
                    () => queue.AddMessage(message, null, null, createOptions, null),
                    "Not specifying a policy when RequireEnryption is set to true should throw.");

                // Retrieve message
                QueueEncryptionPolicy retrPolicy = new QueueEncryptionPolicy(null, resolver);
                QueueRequestOptions retrieveOptions = new QueueRequestOptions() { EncryptionPolicy = retrPolicy };
                retrieveOptions.RequireEncryption = true;

                CloudQueueMessage retrMessage = queue.GetMessage(null, retrieveOptions, null);

                // Update message with plain text.
                string updatedMessage = Guid.NewGuid().ToString("N");
                retrMessage.SetMessageContent(updatedMessage);

                queue.UpdateMessage(retrMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Content | MessageUpdateFields.Visibility);

                // Retrieve updated message with RequireEncryption flag but no metadata on the service. This should throw.
                TestHelper.ExpectedException<StorageException>(
                    () => queue.GetMessage(null, retrieveOptions, null),
                    "Retrieving with RequireEncryption set to true and no metadata on the service should fail.");

                // Set RequireEncryption to false and retrieve.
                retrieveOptions.RequireEncryption = false;
                queue.GetMessage(null, retrieveOptions, null);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }
    }
}
