// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueueTest.cs" company="Microsoft">
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

#if WINDOWS_DESKTOP
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
#endif

namespace Microsoft.WindowsAzure.Storage.Queue
{
    [TestClass]
    public class CloudQueueTest : QueueTestBase
    {
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
        [Description("Test queue name validation.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueNameValidation()
        {
            NameValidator.ValidateQueueName("alpha");
            NameValidator.ValidateQueueName("4lphanum3r1c");
            NameValidator.ValidateQueueName("middle-dash");

            TestInvalidQueueHelper(null, "Null not allowed.", "Invalid queue name. The queue name may not be null, empty, or whitespace only.");
            TestInvalidQueueHelper("$root", "Alphanumeric or dashes only.", "Invalid queue name. Check MSDN for more information about valid queue naming.");
            TestInvalidQueueHelper("double--dash", "No double dash.", "Invalid queue name. Check MSDN for more information about valid queue naming.");
            TestInvalidQueueHelper("CapsLock", "Lowercase only.", "Invalid queue name. Check MSDN for more information about valid queue naming.");
            TestInvalidQueueHelper("illegal$char", "Alphanumeric or dashes only.", "Invalid queue name. Check MSDN for more information about valid queue naming.");
            TestInvalidQueueHelper("illegal!char", "Alphanumeric or dashes only.", "Invalid queue name. Check MSDN for more information about valid queue naming.");
            TestInvalidQueueHelper("white space", "Alphanumeric or dashes only.", "Invalid queue name. Check MSDN for more information about valid queue naming.");
            TestInvalidQueueHelper("2c", "Between 3 and 63 characters.", "Invalid queue name length. The queue name must be between 3 and 63 characters long.");
            TestInvalidQueueHelper(new string('n', 64), "Between 3 and 63 characters.", "Invalid queue name length. The queue name must be between 3 and 63 characters long.");
        }

        private void TestInvalidQueueHelper(string queueName, string failMessage, string exceptionMessage)
        {
            try
            {
                NameValidator.ValidateQueueName(queueName);
                Assert.Fail(failMessage);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual(exceptionMessage, e.Message);
            }
        }

        [TestMethod]
        [Description("Create and delete a queue")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateAndDelete()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);
            queue.Create();
            queue.Create();
            queue.Delete();
        }

        [TestMethod]
        [Description("Create and delete a queue with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateAndDeleteAPM()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = queue.BeginCreate(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndCreate(result);

                result = queue.BeginCreate(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndCreate(result);

                result = queue.BeginExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(queue.EndExists(result));

                result = queue.BeginDelete(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndDelete(result);
            }
        }

#if TASK
        [TestMethod]
        [Description("Create and delete a queue")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateAndDeleteTask()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);
            queue.CreateAsync().Wait();
            queue.CreateAsync().Wait();
            Assert.IsTrue(queue.ExistsAsync().Result);
            queue.DeleteAsync().Wait();
        }
#endif

        [TestMethod]
        [Description("Create and delete a queue with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateAndDeleteFullParameterAPM()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = queue.BeginCreate(null, new OperationContext(), ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndCreate(result);

                result = queue.BeginExists(null, new OperationContext(), ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(queue.EndExists(result));

                result = queue.BeginDelete(null, new OperationContext(), ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndDelete(result);
            }
        }

#if TASK
        [TestMethod]
        [Description("Create and delete a queue with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateAndDeleteFullParameterTask()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            queue.CreateAsync(null, new OperationContext()).Wait();

            Assert.IsTrue(queue.ExistsAsync(null, new OperationContext()).Result);

            queue.DeleteAsync(null, new OperationContext()).Wait();
        }
#endif

        [TestMethod]
        [Description("Create and delete a queue")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateFromUri()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);
            queue.Create();

            // Create cloud queue from constructor
            CloudQueue sameQueue = new CloudQueue(queue.Uri);

            // Test that queue is the same
            Assert.IsTrue(sameQueue.Name.Equals(queue.Name));
            Assert.IsTrue(sameQueue.Uri.Equals(queue.Uri));

            queue.Delete();
        }
      
        [TestMethod]
        [Description("Try to create a queue after it is created")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateIfNotExists()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            try
            {
                Assert.IsTrue(queue.CreateIfNotExists());
                Assert.IsFalse(queue.CreateIfNotExists());
            }
            finally
            {
                queue.Delete();
            }
        }

        [TestMethod]
        [Description("Try to create a queue after it is created with APM.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateIfNotExistsAPM()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = queue.BeginCreateIfNotExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(queue.EndCreateIfNotExists(result));

                    result = queue.BeginCreateIfNotExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(queue.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                queue.Delete();
            }
        }

        [TestMethod]
        [Description("Try to create a queue after it is created with APM.")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateIfNotExistsFullParameterAPM()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = queue.BeginCreateIfNotExists(null, new OperationContext(), ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(queue.EndCreateIfNotExists(result));

                    result = queue.BeginCreateIfNotExists(null, new OperationContext(), ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(queue.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                queue.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to create a queue after it is created")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateIfNotExistsTask()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            try
            {
                Assert.IsTrue(queue.CreateIfNotExistsAsync().Result);
                Assert.IsFalse(queue.CreateIfNotExistsAsync().Result);
            }
            finally
            {
                queue.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to create a queue after it is created")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCreateIfNotExistsFullParameterTask()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            try
            {
                Assert.IsTrue(queue.CreateIfNotExistsAsync(null, new OperationContext()).Result);
                Assert.IsFalse(queue.CreateIfNotExistsAsync(null, new OperationContext()).Result);
            }
            finally
            {
                queue.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Try to delete a non-existing queue")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueDeleteIfExists()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            Assert.IsFalse(queue.DeleteIfExists());
            queue.Create();
            Assert.IsTrue(queue.DeleteIfExists());
            Assert.IsFalse(queue.DeleteIfExists());
        }

        [TestMethod]
        [Description("Try to delete a non-existing queue with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueDeleteIfExistsAPM()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = queue.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsFalse(queue.EndDeleteIfExists(result));

                result = queue.BeginCreate(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndCreate(result);

                result = queue.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(queue.EndDeleteIfExists(result));

                result = queue.BeginDeleteIfExists(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsFalse(queue.EndDeleteIfExists(result));
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing queue with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueDeleteIfExistsFullParameterAPM()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = queue.BeginDeleteIfExists(null, new OperationContext(), ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsFalse(queue.EndDeleteIfExists(result));

                result = queue.BeginCreate(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                queue.EndCreate(result);

                result = queue.BeginDeleteIfExists(null, new OperationContext(), ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsTrue(queue.EndDeleteIfExists(result));

                result = queue.BeginDeleteIfExists(null, new OperationContext(), ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                Assert.IsFalse(queue.EndDeleteIfExists(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to delete a non-existing queue")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueDeleteIfExistsTask()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            Assert.IsFalse(queue.DeleteIfExistsAsync().Result);
            queue.CreateAsync().Wait();
            Assert.IsTrue(queue.DeleteIfExistsAsync().Result);
            Assert.IsFalse(queue.DeleteIfExistsAsync().Result);
        }

        [TestMethod]
        [Description("Try to delete a non-existing queue")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueDeleteIfExistsFullParametersTask()
        {
            string name = GenerateNewQueueName();
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(name);

            Assert.IsFalse(queue.DeleteIfExistsAsync(null, new OperationContext()).Result);
            queue.CreateAsync().Wait();
            Assert.IsTrue(queue.DeleteIfExistsAsync(null, new OperationContext()).Result);
            Assert.IsFalse(queue.DeleteIfExistsAsync(null, new OperationContext()).Result);
        }
#endif

        [TestMethod]
        [Description("Set/get queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueGetSetPermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();
                QueuePermissions emptyPermission = queue.GetPermissions();
                Assert.AreEqual(emptyPermission.SharedAccessPolicies.Count, 0);
                string id = Guid.NewGuid().ToString();
                DateTime start = DateTime.UtcNow;
                DateTime expiry = start.AddMinutes(30);
                QueuePermissions permissions = new QueuePermissions();

                SharedAccessQueuePermissions queuePerm = SharedAccessQueuePermissions.Add
                                                         | SharedAccessQueuePermissions.ProcessMessages
                                                         | SharedAccessQueuePermissions.Read
                                                         | SharedAccessQueuePermissions.Update;
                permissions.SharedAccessPolicies.Add(
                    id,
                    new SharedAccessQueuePolicy()
                        {
                            SharedAccessStartTime = start,
                            SharedAccessExpiryTime = expiry,
                            Permissions = queuePerm
                        });

                queue.SetPermissions(permissions);
                Thread.Sleep(30 * 1000);

                CloudQueue queueToRetrieve = client.GetQueueReference(queue.Name);
                QueuePermissions permissionsToRetrieve = queueToRetrieve.GetPermissions();

                AssertPermissionsEqual(permissions, permissionsToRetrieve);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set/get queue permissions with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueGetSetPermissionsAPM()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = queue.BeginGetPermissions(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    QueuePermissions emptyPermission = queue.EndGetPermissions(result);

                    Assert.AreEqual(emptyPermission.SharedAccessPolicies.Count, 0);

                    string id = Guid.NewGuid().ToString();
                    DateTime start = DateTime.UtcNow;
                    DateTime expiry = start.AddMinutes(30);
                    QueuePermissions permissions = new QueuePermissions();
                    SharedAccessQueuePermissions queuePerm = SharedAccessQueuePermissions.Add | SharedAccessQueuePermissions.ProcessMessages | SharedAccessQueuePermissions.Read | SharedAccessQueuePermissions.Update;
                    permissions.SharedAccessPolicies.Add(id, new SharedAccessQueuePolicy()
                    {
                        SharedAccessStartTime = start,
                        SharedAccessExpiryTime = expiry,
                        Permissions = queuePerm
                    });

                    result = queue.BeginSetPermissions(permissions, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queue.EndSetPermissions(result);

                    Thread.Sleep(30 * 1000);

                    CloudQueue queueToRetrieve = client.GetQueueReference(queue.Name);

                    result = queueToRetrieve.BeginGetPermissions(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    QueuePermissions permissionsToRetrieve = queueToRetrieve.EndGetPermissions(result);

                    AssertPermissionsEqual(permissions, permissionsToRetrieve);
                }
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Set/get queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueGetSetPermissionsTask()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.CreateAsync().Wait();
                QueuePermissions emptyPermission = queue.GetPermissionsAsync().Result;
                Assert.AreEqual(emptyPermission.SharedAccessPolicies.Count, 0);
                string id = Guid.NewGuid().ToString();
                DateTime start = DateTime.UtcNow;
                DateTime expiry = start.AddMinutes(30);
                QueuePermissions permissions = new QueuePermissions();

                SharedAccessQueuePermissions queuePerm = SharedAccessQueuePermissions.Add
                                                         | SharedAccessQueuePermissions.ProcessMessages
                                                         | SharedAccessQueuePermissions.Read
                                                         | SharedAccessQueuePermissions.Update;
                permissions.SharedAccessPolicies.Add(
                    id,
                    new SharedAccessQueuePolicy()
                        {
                            SharedAccessStartTime = start,
                            SharedAccessExpiryTime = expiry,
                            Permissions = queuePerm
                        });

                queue.SetPermissionsAsync(permissions).Wait();
                Thread.Sleep(30 * 1000);

                CloudQueue queueToRetrieve = client.GetQueueReference(queue.Name);
                QueuePermissions permissionsToRetrieve = queueToRetrieve.GetPermissionsAsync(null, null).Result;

                AssertPermissionsEqual(permissions, permissionsToRetrieve);
            }
            finally
            {
                queue.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Set/get a queue with metadata")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSetGetMetadata()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();

                CloudQueue queueToRetrieve = client.GetQueueReference(queue.Name);
                queueToRetrieve.FetchAttributes();
                Assert.AreEqual<int>(0, queueToRetrieve.Metadata.Count);

                queue.Metadata.Add("key1", "value1");
                queue.SetMetadata();

                queueToRetrieve.FetchAttributes();
                Assert.AreEqual(1, queueToRetrieve.Metadata.Count);
                Assert.AreEqual("value1", queueToRetrieve.Metadata["key1"]);

                CloudQueue listedQueue = client.ListQueues(queue.Name, QueueListingDetails.All, null, null).First();
                Assert.AreEqual(1, listedQueue.Metadata.Count);
                Assert.AreEqual("value1", listedQueue.Metadata["key1"]);

                queue.Metadata.Clear();
                queue.SetMetadata();

                queueToRetrieve.FetchAttributes();
                Assert.AreEqual<int>(0, queueToRetrieve.Metadata.Count);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set/get a queue with metadata with APM")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSetGetMetadataAPM()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();
                CloudQueue queueToRetrieve = client.GetQueueReference(queue.Name);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = queueToRetrieve.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queueToRetrieve.EndFetchAttributes(result);

                    Assert.AreEqual<int>(0, queueToRetrieve.Metadata.Count);

                    queue.Metadata.Add("key1", "value1");
                    result = queue.BeginSetMetadata(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queue.EndSetMetadata(result);

                    result = queueToRetrieve.BeginFetchAttributes(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queueToRetrieve.EndFetchAttributes(result);
                    Assert.AreEqual(1, queueToRetrieve.Metadata.Count);
                    Assert.AreEqual("value1", queueToRetrieve.Metadata["key1"]);

                    result = client.BeginListQueuesSegmented(queue.Name, QueueListingDetails.All, null, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    CloudQueue listedQueue = client.EndListQueuesSegmented(result).Results.First();
                    Assert.AreEqual(1, listedQueue.Metadata.Count);
                    Assert.AreEqual("value1", listedQueue.Metadata["key1"]);

                    queue.Metadata.Clear();
                    result = queue.BeginSetMetadata(null, new OperationContext(), ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queue.EndSetMetadata(result);

                    result = queueToRetrieve.BeginFetchAttributes(null, new OperationContext(), ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    queueToRetrieve.EndFetchAttributes(result);
                    Assert.AreEqual<int>(0, queueToRetrieve.Metadata.Count);
                }
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Set/get a queue with metadata")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSetGetMetadataTask()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.CreateAsync().Wait();

                CloudQueue queueToRetrieve = client.GetQueueReference(queue.Name);
                queueToRetrieve.FetchAttributesAsync().Wait();
                Assert.AreEqual<int>(0, queueToRetrieve.Metadata.Count);

                queue.Metadata.Add("key1", "value1");
                queue.SetMetadataAsync().Wait();

                queueToRetrieve.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, queueToRetrieve.Metadata.Count);
                Assert.AreEqual("value1", queueToRetrieve.Metadata["key1"]);

                CloudQueue listedQueue = client.ListQueuesSegmentedAsync(queue.Name, QueueListingDetails.All, null, null, null, null).Result.Results.First();
                Assert.AreEqual(1, listedQueue.Metadata.Count);
                Assert.AreEqual("value1", listedQueue.Metadata["key1"]);

                queue.Metadata.Clear();
                queue.SetMetadataAsync(null, new OperationContext()).Wait();

                queueToRetrieve.FetchAttributesAsync(null, new OperationContext()).Wait();
                Assert.AreEqual<int>(0, queueToRetrieve.Metadata.Count);
            }
            finally
            {
                queue.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Test queue sas")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void QueueSASTest()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();
                string messageContent = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageContent);
                queue.AddMessage(message);

                // Prepare SAS authentication with full permissions
                string id = Guid.NewGuid().ToString();
                DateTime start = DateTime.UtcNow;
                DateTime expiry = start.AddMinutes(30);
                QueuePermissions permissions = new QueuePermissions();
                SharedAccessQueuePermissions queuePerm = SharedAccessQueuePermissions.Add | SharedAccessQueuePermissions.ProcessMessages | SharedAccessQueuePermissions.Read | SharedAccessQueuePermissions.Update;
                permissions.SharedAccessPolicies.Add(id, new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = queuePerm
                });

                queue.SetPermissions(permissions);
                Thread.Sleep(30 * 1000);

                string sasTokenFromId = queue.GetSharedAccessSignature(null, id);
                StorageCredentials sasCredsFromId = new StorageCredentials(sasTokenFromId);

                CloudStorageAccount sasAcc = new CloudStorageAccount(sasCredsFromId, null /* blobEndpoint */, new Uri(TestBase.TargetTenantConfig.QueueServiceEndpoint), null /* tableEndpoint */, null /* fileEndpoint */);
                CloudQueueClient sasClient = sasAcc.CreateCloudQueueClient();

                CloudQueue sasQueueFromSasUri = new CloudQueue(sasClient.Credentials.TransformUri(queue.Uri));
                CloudQueueMessage receivedMessage = sasQueueFromSasUri.PeekMessage();
                Assert.AreEqual(messageContent, receivedMessage.AsString);

                CloudQueue sasQueueFromSasUri1 = new CloudQueue(new Uri(queue.Uri.ToString() + sasTokenFromId));
                CloudQueueMessage receivedMessage1 = sasQueueFromSasUri1.PeekMessage();
                Assert.AreEqual(messageContent, receivedMessage1.AsString);

                CloudQueue sasQueueFromId = new CloudQueue(queue.Uri, sasCredsFromId);
                CloudQueueMessage receivedMessage2 = sasQueueFromId.PeekMessage();
                Assert.AreEqual(messageContent, receivedMessage2.AsString);

                string sasTokenFromPolicy = queue.GetSharedAccessSignature(permissions.SharedAccessPolicies[id], null);
                StorageCredentials sasCredsFromPolicy = new StorageCredentials(sasTokenFromPolicy);
                CloudQueue sasQueueFromPolicy = new CloudQueue(queue.Uri, sasCredsFromPolicy);
                CloudQueueMessage receivedMessage3 = sasQueueFromPolicy.PeekMessage();
                Assert.AreEqual(messageContent, receivedMessage3.AsString);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test queue sas with Italy regional settings")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void QueueRegionalSASTest()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

            try
            {
                queue.Create();
                string messageContent = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageContent);
                queue.AddMessage(message);

                // Prepare SAS authentication with full permissions
                string id = Guid.NewGuid().ToString();
                DateTime start = DateTime.UtcNow;
                DateTime expiry = start.AddMinutes(30);
                QueuePermissions permissions = new QueuePermissions();
                SharedAccessQueuePermissions queuePerm = SharedAccessQueuePermissions.Add | SharedAccessQueuePermissions.ProcessMessages | SharedAccessQueuePermissions.Read | SharedAccessQueuePermissions.Update;
                permissions.SharedAccessPolicies.Add(id, new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = queuePerm
                });

                queue.SetPermissions(permissions);
                Thread.Sleep(30 * 1000);

                string sasTokenFromId = queue.GetSharedAccessSignature(null, id);
                StorageCredentials sasCredsFromId = new StorageCredentials(sasTokenFromId);
                CloudQueue sasQueueFromId = new CloudQueue(queue.Uri, sasCredsFromId);
                CloudQueueMessage receivedMessage1 = sasQueueFromId.PeekMessage();
                Assert.AreEqual(messageContent, receivedMessage1.AsString);

                string sasTokenFromPolicy = queue.GetSharedAccessSignature(permissions.SharedAccessPolicies[id], null);
                StorageCredentials sasCredsFromPolicy = new StorageCredentials(sasTokenFromPolicy);
                CloudQueue sasQueueFromPolicy = new CloudQueue(queue.Uri, sasCredsFromPolicy);
                CloudQueueMessage receivedMessage2 = sasQueueFromPolicy.PeekMessage();
                Assert.AreEqual(messageContent, receivedMessage2.AsString);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Set queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSetPermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();
                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                queue.SetPermissions(permissions);
                Thread.Sleep(30 * 1000);

                CloudQueue queue2 = queue.ServiceClient.GetQueueReference(queue.Name);
                permissions = queue2.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                Assert.AreEqual(SharedAccessQueuePermissions.Read, permissions.SharedAccessPolicies["key1"].Permissions);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Clear queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueClearPermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();

                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                queue.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);

                Assert.AreEqual(true, permissions.SharedAccessPolicies.Contains(sharedAccessPolicy));
                Assert.AreEqual(true, permissions.SharedAccessPolicies.ContainsKey("key1"));
                permissions.SharedAccessPolicies.Clear();
                queue.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Copy queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueCopyPermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();

                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry2 = start.AddMinutes(30);
                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessQueuePolicy>("key2", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessQueuePermissions.Read,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);

                KeyValuePair<String, SharedAccessQueuePolicy>[] sharedAccessPolicyArray = new KeyValuePair<string, SharedAccessQueuePolicy>[2];
                permissions.SharedAccessPolicies.CopyTo(sharedAccessPolicyArray, 0);
                Assert.AreEqual(2, sharedAccessPolicyArray.Length);
                Assert.AreEqual(sharedAccessPolicy, sharedAccessPolicyArray[0]);
                Assert.AreEqual(sharedAccessPolicy2, sharedAccessPolicyArray[1]);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Remove queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueRemovePermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();

                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessQueuePolicy>("key2", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessQueuePermissions.Read,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                queue.SetPermissions(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove(sharedAccessPolicy2);
                queue.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);

                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = queue.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);

                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                queue.SetPermissions(permissions);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions.SharedAccessPolicies.Remove("key2");
                queue.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                permissions = queue.GetPermissions();
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.AreEqual(sharedAccessPolicy.Key, permissions.SharedAccessPolicies.ElementAt(0).Key);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, permissions.SharedAccessPolicies.ElementAt(0).Value.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, permissions.SharedAccessPolicies.ElementAt(0).Value.SharedAccessExpiryTime);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("TryGetValue for queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueTryGetValuePermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();

                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessQueuePolicy>("key2", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessQueuePermissions.Read,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                queue.SetPermissions(permissions);
                Thread.Sleep(3 * 1000);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                permissions = queue.GetPermissions();
                SharedAccessQueuePolicy retrPolicy;
                permissions.SharedAccessPolicies.TryGetValue("key1", out retrPolicy);
                Assert.AreEqual(sharedAccessPolicy.Value.Permissions, retrPolicy.Permissions);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessStartTime, retrPolicy.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy.Value.SharedAccessExpiryTime, retrPolicy.SharedAccessExpiryTime);

                SharedAccessQueuePolicy retrPolicy2;
                permissions.SharedAccessPolicies.TryGetValue("key2", out retrPolicy2);
                Assert.AreEqual(sharedAccessPolicy2.Value.Permissions, retrPolicy2.Permissions);
                Assert.AreEqual(sharedAccessPolicy2.Value.SharedAccessStartTime, retrPolicy2.SharedAccessStartTime);
                Assert.AreEqual(sharedAccessPolicy2.Value.SharedAccessExpiryTime, retrPolicy2.SharedAccessExpiryTime);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetEnumerator for queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueGetEnumeratorPermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();

                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessQueuePolicy>("key2", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessQueuePermissions.Read,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                IEnumerator<KeyValuePair<string, SharedAccessQueuePolicy>> policies = permissions.SharedAccessPolicies.GetEnumerator();
                policies.MoveNext();
                Assert.AreEqual(sharedAccessPolicy, policies.Current);
                policies.MoveNext();
                Assert.AreEqual(sharedAccessPolicy2, policies.Current);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("GetValues for queue permissions")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueGetValuesPermissions()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();

                QueuePermissions permissions = queue.GetPermissions();
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy = new KeyValuePair<string, SharedAccessQueuePolicy>("key1", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                DateTime start2 = DateTime.UtcNow;
                start2 = new DateTime(start2.Year, start2.Month, start2.Day, start2.Hour, start2.Minute, start2.Second, DateTimeKind.Utc);
                DateTime expiry2 = start2.AddMinutes(30);
                KeyValuePair<String, SharedAccessQueuePolicy> sharedAccessPolicy2 = new KeyValuePair<string, SharedAccessQueuePolicy>("key2", new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = start2,
                    SharedAccessExpiryTime = expiry2,
                    Permissions = SharedAccessQueuePermissions.Read,
                });
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy);
                permissions.SharedAccessPolicies.Add(sharedAccessPolicy2);
                Assert.AreEqual(2, permissions.SharedAccessPolicies.Count);

                ICollection<SharedAccessQueuePolicy> values = permissions.SharedAccessPolicies.Values;
                Assert.AreEqual(2, values.Count);
                Assert.AreEqual(sharedAccessPolicy.Value, values.ElementAt(0));
                Assert.AreEqual(sharedAccessPolicy2.Value, values.ElementAt(1));
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Update queue sas")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void UpdateQueueSASTest()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();
                string messageContent = Guid.NewGuid().ToString();
                CloudQueueMessage message = new CloudQueueMessage(messageContent);
                queue.AddMessage(message);
                SharedAccessQueuePolicy policy = new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessQueuePermissions.Add | SharedAccessQueuePermissions.ProcessMessages,
                };
                string id = Guid.NewGuid().ToString();
                string sasToken = queue.GetSharedAccessSignature(policy, null);

                StorageCredentials sasCreds = new StorageCredentials(sasToken);
                CloudQueue sasQueue = new CloudQueue(queue.Uri, sasCreds);

                TestHelper.ExpectedException(
                    () => sasQueue.PeekMessage(),
                    "Peek when Sas does not allow Read access on the queue",
                    HttpStatusCode.Forbidden);

                sasQueue.AddMessage(message);

                SharedAccessQueuePolicy policy2 = new SharedAccessQueuePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessQueuePermissions.Add | SharedAccessQueuePermissions.ProcessMessages | SharedAccessQueuePermissions.Read,
                };

                string sasToken2 = queue.GetSharedAccessSignature(policy2, null);
                sasCreds.UpdateSASToken(sasToken2);
                sasQueue = new CloudQueue(queue.Uri, sasCreds);

                sasQueue.PeekMessage();
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        /// <summary>
        /// Helper function for testing the IPAddressOrRange funcitonality for queues
        /// </summary>
        /// <param name="generateInitialIPAddressOrRange">Function that generates an initial IPAddressOrRange object to use. This is expected to fail on the service.</param>
        /// <param name="generateFinalIPAddressOrRange">Function that takes in the correct IP address (according to the service) and returns the IPAddressOrRange object
        /// that should be accepted by the service</param>
        public void CloudQueueSASIPAddressHelper(Func<IPAddressOrRange> generateInitialIPAddressOrRange, Func<IPAddress, IPAddressOrRange> generateFinalIPAddressOrRange)
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());

            try
            {
                queue.Create();
                SharedAccessQueuePolicy policy = new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                string sampleMessageContent = "sample content";
                CloudQueueMessage message = new CloudQueueMessage(sampleMessageContent);
                queue.AddMessage(message);

                // The plan then is to use an incorrect IP address to make a call to the service
                // ensure that we get an error message
                // parse the error message to get my actual IP (as far as the service sees)
                // then finally test the success case to ensure we can actually make requests

                IPAddressOrRange ipAddressOrRange = generateInitialIPAddressOrRange();
                string queueToken = queue.GetSharedAccessSignature(policy, null, null, ipAddressOrRange);
                StorageCredentials queueSAS = new StorageCredentials(queueToken);
                Uri queueSASUri = queueSAS.TransformUri(queue.Uri);
                StorageUri queueSASStorageUri = queueSAS.TransformUri(queue.StorageUri);

                CloudQueue queueWithSAS = new CloudQueue(queueSASUri);
                OperationContext opContext = new OperationContext();
                IPAddress actualIP = null;
                opContext.ResponseReceived += (sender, e) =>
                {
                    Stream stream = e.Response.GetResponseStream();
                    stream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string text = reader.ReadToEnd();
                        XDocument xdocument = XDocument.Parse(text);
                        actualIP = IPAddress.Parse(xdocument.Descendants("SourceIP").First().Value);
                    }
                };

                bool exceptionThrown = false;
                CloudQueueMessage resultMessage;
                try
                {
                    resultMessage = queueWithSAS.PeekMessage(null, opContext);
                }
                catch (StorageException)
                {
                    exceptionThrown = true;
                    Assert.IsNotNull(actualIP);
                }

                Assert.IsTrue(exceptionThrown);
                ipAddressOrRange = generateFinalIPAddressOrRange(actualIP);
                queueToken = queue.GetSharedAccessSignature(policy, null, null, ipAddressOrRange);
                queueSAS = new StorageCredentials(queueToken);
                queueSASUri = queueSAS.TransformUri(queue.Uri);
                queueSASStorageUri = queueSAS.TransformUri(queue.StorageUri);

                queueWithSAS = new CloudQueue(queueSASUri);
                resultMessage = queue.PeekMessage();
                Assert.AreEqual(sampleMessageContent, resultMessage.AsString);
                Assert.IsTrue(queueWithSAS.StorageUri.PrimaryUri.Equals(queue.Uri));
                Assert.IsNull(queueWithSAS.StorageUri.SecondaryUri);

                queueWithSAS = new CloudQueue(queueSASStorageUri, null);
                resultMessage = queue.PeekMessage();
                Assert.AreEqual(sampleMessageContent, resultMessage.AsString);
                Assert.IsTrue(queueWithSAS.StorageUri.Equals(queue.StorageUri));

            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Perform a SAS request specifying an IP address or range and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSASIPAddressQueryParam()
        {
            CloudQueueSASIPAddressHelper(() =>
            {
                // We need an IP address that will never be a valid source
                IPAddress invalidIP = IPAddress.Parse("255.255.255.255");
                return new IPAddressOrRange(invalidIP.ToString());
            },
            (IPAddress actualIP) =>
            {
                return new IPAddressOrRange(actualIP.ToString());
            });
        }

        [TestMethod]
        [Description("Perform a SAS request specifying an IP address or range and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSASIPRangeQueryParam()
        {
            CloudQueueSASIPAddressHelper(() =>
            {
                // We need an IP address that will never be a valid source
                IPAddress invalidIPBegin = IPAddress.Parse("255.255.255.0");
                IPAddress invalidIPEnd = IPAddress.Parse("255.255.255.255");

                return new IPAddressOrRange(invalidIPBegin.ToString(), invalidIPEnd.ToString());
            },
                (IPAddress actualIP) =>
                {
                    byte[] actualAddressBytes = actualIP.GetAddressBytes();
                    byte[] initialAddressBytes = actualAddressBytes.ToArray();
                    initialAddressBytes[0]--;
                    byte[] finalAddressBytes = actualAddressBytes.ToArray();
                    finalAddressBytes[0]++;

                    return new IPAddressOrRange(new IPAddress(initialAddressBytes).ToString(), new IPAddress(finalAddressBytes).ToString());
                });
        }

        [TestMethod]
        [Description("Perform a SAS request specifying a shared protocol and ensure that everything works properly.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudQueueSASSharedProtocolsQueryParam()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(GenerateNewQueueName());
            try
            {
                queue.Create();
                SharedAccessQueuePolicy policy = new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.Read,
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                string sampleMessageContent = "sample content";
                CloudQueueMessage message = new CloudQueueMessage(sampleMessageContent);
                queue.AddMessage(message);

                foreach (SharedAccessProtocol? protocol in new SharedAccessProtocol?[] { null, SharedAccessProtocol.HttpsOrHttp, SharedAccessProtocol.HttpsOnly })
                {
                    string queueToken = queue.GetSharedAccessSignature(policy, null, protocol, null);
                    StorageCredentials queueSAS = new StorageCredentials(queueToken);
                    Uri queueSASUri = new Uri(queue.Uri + queueSAS.SASToken);
                    StorageUri queueSASStorageUri = new StorageUri(new Uri(queue.StorageUri.PrimaryUri + queueSAS.SASToken), new Uri(queue.StorageUri.SecondaryUri + queueSAS.SASToken));

                    int httpPort = queueSASUri.Port;
                    int securePort = 443;

                    if (!string.IsNullOrEmpty(TestBase.TargetTenantConfig.QueueSecurePortOverride))
                    {
                        securePort = Int32.Parse(TestBase.TargetTenantConfig.QueueSecurePortOverride);
                    }

                    var schemesAndPorts = new[] {
                        new { scheme = Uri.UriSchemeHttp, port = httpPort},
                        new { scheme = Uri.UriSchemeHttps, port = securePort}
                    };

                    CloudQueue queueWithSAS;
                    CloudQueueMessage resultMessage;

                    foreach (var item in schemesAndPorts)
                    {
                        queueSASUri = TransformSchemeAndPort(queueSASUri, item.scheme, item.port);
                        queueSASStorageUri = new StorageUri(TransformSchemeAndPort(queueSASStorageUri.PrimaryUri, item.scheme, item.port), TransformSchemeAndPort(queueSASStorageUri.SecondaryUri, item.scheme, item.port));

                        if (protocol.HasValue && protocol == SharedAccessProtocol.HttpsOnly && string.CompareOrdinal(item.scheme, Uri.UriSchemeHttp) == 0)
                        {
                            queueWithSAS = new CloudQueue(queueSASUri);
                            TestHelper.ExpectedException(() => queueWithSAS.PeekMessage(), "Access a queue using SAS with a shared protocols that does not match", HttpStatusCode.Unused);

                            queueWithSAS = new CloudQueue(queueSASStorageUri, null);
                            TestHelper.ExpectedException(() => queueWithSAS.PeekMessage(), "Access a queue using SAS with a shared protocols that does not match", HttpStatusCode.Unused);
                        }
                        else
                        {
                            queueWithSAS = new CloudQueue(queueSASUri);
                            resultMessage = queueWithSAS.PeekMessage();
                            Assert.AreEqual(sampleMessageContent, resultMessage.AsString);

                            queueWithSAS = new CloudQueue(queueSASStorageUri, null);
                            resultMessage = queueWithSAS.PeekMessage();
                            Assert.AreEqual(sampleMessageContent, resultMessage.AsString);
                        }
                    }
                }
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        private static Uri TransformSchemeAndPort(Uri input, string scheme, int port)
        {
            UriBuilder builder = new UriBuilder(input);
            builder.Scheme = scheme;
            builder.Port = port;
            return builder.Uri;
        }

        [TestMethod]
        [Description("Test queue listing")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListQueuesSegmentedTest()
        {
            String prefix = "pagingqueuetest" + Guid.NewGuid();
            CloudQueueClient client = GenerateCloudQueueClient();
            ///Create 20 queues
            for (int i = 1; i <= 20; i++)
            {
                CloudQueue myqueue = client.GetQueueReference(prefix + i);
                myqueue.CreateIfNotExists();
            }

            ///Segmented listing of queues.
            ///Return a page of 10 queues beginning with the specified prefix.
            ///Check with options and context as NULL
            QueueResultSegment resultSegment = client.ListQueuesSegmented(prefix, QueueListingDetails.None, 10, null, null, null);

            IEnumerable<CloudQueue> list = resultSegment.Results;
            int count = 0;
            foreach (CloudQueue item in list)
            {
                count++;
                item.Delete();
            }
            Assert.AreEqual(10, count);
            Assert.IsNotNull(resultSegment.ContinuationToken);

            OperationContext context = new OperationContext();
            QueueRequestOptions options = new QueueRequestOptions();

            ///Check with options and context having some value

            QueueResultSegment resultSegment2 = client.ListQueuesSegmented(prefix, QueueListingDetails.None, 10, resultSegment.ContinuationToken, options, context);
            IEnumerable<CloudQueue> list2 = resultSegment2.Results;
            foreach (CloudQueue item in list2)
            {
                item.Delete();
            }
            Assert.IsNull(resultSegment2.ContinuationToken);
        }

        [TestMethod]
        [Description("Test empty headers")]
        [TestCategory(ComponentCategory.Queue)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void QueueEmptyHeaderSigningTest()
        {
            CloudQueueClient client = GenerateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference(Guid.NewGuid().ToString("N"));
            OperationContext context = new OperationContext();
            try
            {
                context.UserHeaders = new Dictionary<string, string>();
                context.UserHeaders.Add("x-ms-foo", String.Empty);
                queue.Create(null, context);
                CloudQueueMessage message = new CloudQueueMessage("Hello Signing");
                queue.AddMessage(message, null, null, null, context);
            }
            finally
            {
                queue.DeleteIfExists();
            }
        }

        #region Test Helpers
        internal static void AssertPermissionsEqual(QueuePermissions permissions1, QueuePermissions permissions2)
        {
            Assert.AreEqual(permissions1.SharedAccessPolicies.Count, permissions2.SharedAccessPolicies.Count);

            foreach (KeyValuePair<string, SharedAccessQueuePolicy> pair in permissions1.SharedAccessPolicies)
            {
                SharedAccessQueuePolicy policy1 = pair.Value;
                SharedAccessQueuePolicy policy2 = permissions2.SharedAccessPolicies[pair.Key];

                Assert.IsNotNull(policy1);
                Assert.IsNotNull(policy2);

                Assert.AreEqual(policy1.Permissions, policy2.Permissions);
                if (policy1.SharedAccessStartTime != null)
                {
                    Assert.IsTrue(Math.Floor((policy1.SharedAccessStartTime.Value - policy2.SharedAccessStartTime.Value).TotalSeconds) == 0);
                }

                if (policy1.SharedAccessExpiryTime != null)
                {
                    Assert.IsTrue(Math.Floor((policy1.SharedAccessExpiryTime.Value - policy2.SharedAccessExpiryTime.Value).TotalSeconds) == 0);
                }
            }
        }
        #endregion
    }
}
