// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileDirectoryTest.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;

    [TestClass]
    public class CloudFileDirectoryTest : FileTestBase
    {
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.FileBufferManager != null)
            {
                TestBase.FileBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.FileBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.FileBufferManager.OutstandingBufferCount);
            }
        }

        private bool CloudFileDirectorySetup(CloudFileShare share)
        {
            try
            {
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                for (int i = 1; i < 3; i++)
                {
                    CloudFileDirectory topDirectory = rootDirectory.GetDirectoryReference("TopDir" + i);
                    topDirectory.Create();

                    for (int j = 1; j < 3; j++)
                    {
                        CloudFileDirectory midDirectory = topDirectory.GetDirectoryReference("MidDir" + j);
                        midDirectory.Create();

                        for (int k = 1; k < 3; k++)
                        {
                            CloudFileDirectory endDirectory = midDirectory.GetDirectoryReference("EndDir" + k);
                            endDirectory.Create();

                            CloudFile file1 = endDirectory.GetFileReference("EndFile" + k);
                            file1.Create(0);
                        }
                    }

                    CloudFile file2 = topDirectory.GetFileReference("File" + i);
                    file2.Create(0);
                }

                return true;
            }
            catch (StorageException e)
            {
                throw e;
            }

        }

        [TestMethod]
        [Description("Test file directory name validation.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryNameValidation()
        {
            NameValidator.ValidateDirectoryName("alpha");
            NameValidator.ValidateDirectoryName("4lphanum3r1c");
            NameValidator.ValidateDirectoryName("middle-dash");
            NameValidator.ValidateDirectoryName("CAPS");
            NameValidator.ValidateDirectoryName("$root");
            NameValidator.ValidateDirectoryName("..");
            NameValidator.ValidateDirectoryName("CLOCK$");
            NameValidator.ValidateDirectoryName("endslash/");

            TestInvalidDirectoryHelper(null, "No null.", "Invalid directory name. The directory name may not be null, empty, or whitespace only.");
            TestInvalidDirectoryHelper("middle/slash", "Slashes only at the end.", "Invalid directory name. Check MSDN for more information about valid directory naming.");
            TestInvalidDirectoryHelper("illegal\"char", "Illegal character.", "Invalid directory name. Check MSDN for more information about valid directory naming.");
            TestInvalidDirectoryHelper("illegal:char?", "Illegal character.", "Invalid directory name. Check MSDN for more information about valid directory naming.");
            TestInvalidDirectoryHelper(string.Empty, "Between 1 and 255 characters.", "Invalid directory name. The directory name may not be null, empty, or whitespace only.");
            TestInvalidDirectoryHelper(new string('n', 256), "Between 1 and 255 characters.", "Invalid directory name length. The directory name must be between 1 and 255 characters long.");
        }

        private void TestInvalidDirectoryHelper(string directoryName, string failMessage, string exceptionMessage)
        {
            try
            {
                NameValidator.ValidateDirectoryName(directoryName);
                Assert.Fail(failMessage);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual(exceptionMessage, e.Message);
            }
        }

        [TestMethod]
        [Description("Get a directory reference using its constructor")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryConstructor()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
            CloudFileDirectory directory2 = new CloudFileDirectory(directory.StorageUri, null);
            Assert.AreEqual(directory.Name, directory2.Name);
            Assert.AreEqual(directory.StorageUri, directory2.StorageUri);
            Assert.AreEqual(directory.Share.StorageUri, directory2.Share.StorageUri);
            Assert.AreEqual(directory.ServiceClient.StorageUri, directory2.ServiceClient.StorageUri);
        }

        [TestMethod]
        [Description("Create a directory and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryCreateAndDelete()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory.Create();
                Assert.IsTrue(directory.Exists());
                directory.Delete();
                Assert.IsFalse(directory.Exists());
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Create a directory and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryCreateAndDeleteAPM()
        {
            CloudFileShare share = GetRandomShareReference();

            try
            {
                share.Create();
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                    IAsyncResult result = directory.BeginCreate(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndCreate(result);
                    Assert.IsTrue(directory.Exists());
                    result = directory.BeginDelete(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndDelete(result);
                    Assert.IsFalse(directory.Exists());
                }
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Try to create an existing directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryCreateIfNotExists()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                Assert.IsTrue(directory.CreateIfNotExists());
                Assert.IsFalse(directory.CreateIfNotExists());
                directory.Delete();
                Assert.IsTrue(directory.CreateIfNotExists());
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Try to create an existing directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryCreateIfNotExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                    IAsyncResult result = directory.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(directory.EndCreateIfNotExists(result));
                    result = directory.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(directory.EndCreateIfNotExists(result));
                    result = directory.BeginDelete(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndDelete(result);
                    result = directory.BeginCreateIfNotExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(directory.EndCreateIfNotExists(result));
                }
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryDeleteIfExists()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                Assert.IsFalse(directory.DeleteIfExists());
                directory.Create();
                Assert.IsTrue(directory.DeleteIfExists());
                Assert.IsFalse(directory.DeleteIfExists());
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryDeleteIfExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                    IAsyncResult result = directory.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(directory.EndDeleteIfExists(result));
                    result = directory.BeginCreate(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndCreate(result);
                    result = directory.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(directory.EndDeleteIfExists(result));
                    result = directory.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(directory.EndDeleteIfExists(result));
                }
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Verify that creating a file directory can also set its metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryCreateWithMetadata()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory.Metadata["key1"] = "value1";
                directory.Create();

                CloudFileDirectory directory2 = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory2.FetchAttributes();
                Assert.AreEqual(1, directory2.Metadata.Count);
                Assert.AreEqual("value1", directory2.Metadata["key1"]);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that a file directory's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectorySetMetadata()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory.Create();

                CloudFileDirectory directory2 = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory2.FetchAttributes();
                Assert.AreEqual(0, directory2.Metadata.Count);

                directory.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => directory.SetMetadata(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                directory.Metadata["key1"] = "";
                e = TestHelper.ExpectedException<StorageException>(
                    () => directory.SetMetadata(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                directory.Metadata["key1"] = "value1";
                directory.SetMetadata();

                directory2.FetchAttributes();
                Assert.AreEqual(1, directory2.Metadata.Count);
                Assert.AreEqual("value1", directory2.Metadata["key1"]);

                directory.Metadata.Clear();
                directory.SetMetadata();

                directory2.FetchAttributes();
                Assert.AreEqual(0, directory2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that a file directory's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectorySetMetadataAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFileDirectory directory2 = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                    IAsyncResult result = directory2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory2.EndFetchAttributes(result);
                    Assert.AreEqual(0, directory2.Metadata.Count);

                    directory.Metadata["key1"] = null;
                    result = directory.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Exception e = TestHelper.ExpectedException<StorageException>(
                        () => directory.EndSetMetadata(result),
                        "Metadata keys should have a non-null value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    directory.Metadata["key1"] = "";
                    result = directory.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => directory.EndSetMetadata(result),
                        "Metadata keys should have a non-empty value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    directory.Metadata["key1"] = "value1";
                    result = directory.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndSetMetadata(result);

                    result = directory2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory2.EndFetchAttributes(result);
                    Assert.AreEqual(1, directory2.Metadata.Count);
                    Assert.AreEqual("value1", directory2.Metadata["key1"]);

                    directory.Metadata.Clear();
                    result = directory.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory.EndSetMetadata(result);

                    result = directory2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    directory2.EndFetchAttributes(result);
                    Assert.AreEqual(0, directory2.Metadata.Count);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify that a file directory's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectorySetMetadataTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory.CreateAsync().Wait();

                CloudFileDirectory directory2 = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                directory2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, directory2.Metadata.Count);

                directory.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedExceptionTask<StorageException>(
                    directory.SetMetadataAsync(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                directory.Metadata["key1"] = "";
                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    directory.SetMetadataAsync(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                directory.Metadata["key1"] = "value1";
                directory.SetMetadataAsync().Wait();

                directory2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, directory2.Metadata.Count);
                Assert.AreEqual("value1", directory2.Metadata["key1"]);

                directory.Metadata.Clear();
                directory.SetMetadataAsync().Wait();

                directory2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, directory2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("CloudFileDirectory listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryListFilesAndDirectories()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                if (CloudFileDirectorySetup(share))
                {
                    CloudFileDirectory topDir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1");
                    IEnumerable<IListFileItem> list1 = topDir1.ListFilesAndDirectories();

                    List<IListFileItem> simpleList1 = list1.ToList();
                    ////Check if for 3 because if there were more than 3, the previous assert would have failed.
                    ////So the only thing we need to make sure is that it is not less than 3. 
                    Assert.IsTrue(simpleList1.Count == 3);

                    IListFileItem item11 = simpleList1.ElementAt(0);
                    Assert.IsTrue(item11.Uri.Equals(share.Uri + "/TopDir1/File1"));
                    Assert.AreEqual("File1", ((CloudFile)item11).Name);

                    IListFileItem item12 = simpleList1.ElementAt(1);
                    Assert.IsTrue(item12.Uri.Equals(share.Uri + "/TopDir1/MidDir1"));
                    Assert.AreEqual("MidDir1", ((CloudFileDirectory)item12).Name);

                    IListFileItem item13 = simpleList1.ElementAt(2);
                    Assert.IsTrue(item13.Uri.Equals(share.Uri + "/TopDir1/MidDir2"));
                    Assert.AreEqual("MidDir2", ((CloudFileDirectory)item13).Name);
                    CloudFileDirectory midDir2 = (CloudFileDirectory)item13;

                    IEnumerable<IListFileItem> list2 = midDir2.ListFilesAndDirectories();

                    List<IListFileItem> simpleList2 = list2.ToList();
                    Assert.IsTrue(simpleList2.Count == 2);

                    IListFileItem item21 = simpleList2.ElementAt(0);
                    Assert.IsTrue(item21.Uri.Equals(share.Uri + "/TopDir1/MidDir2/EndDir1"));
                    Assert.AreEqual("EndDir1", ((CloudFileDirectory)item21).Name);

                    IListFileItem item22 = simpleList2.ElementAt(1);
                    Assert.IsTrue(item22.Uri.Equals(share.Uri + "/TopDir1/MidDir2/EndDir2"));
                    Assert.AreEqual("EndDir2", ((CloudFileDirectory)item22).Name);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryListFilesAndDirectoriesAPM()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                if (CloudFileDirectorySetup(share))
                {
                    CloudFileDirectory topDir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1");
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        FileContinuationToken token = null;
                        List<IListFileItem> simpleList1 = new List<IListFileItem>();
                        do
                        {
                            IAsyncResult result = topDir1.BeginListFilesAndDirectoriesSegmented(
                                null,
                                null,
                                null,
                                null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            FileResultSegment segment = topDir1.EndListFilesAndDirectoriesSegmented(result);
                            simpleList1.AddRange(segment.Results);
                            token = segment.ContinuationToken;
                        }
                        while (token != null);

                        ////Check if for 3 because if there were more than 3, the previous assert would have failed.
                        ////So the only thing we need to make sure is that it is not less than 3. 
                        Assert.IsTrue(simpleList1.Count == 3);

                        IListFileItem item11 = simpleList1.ElementAt(0);
                        Assert.IsTrue(item11.Uri.Equals(share.Uri + "/TopDir1/File1"));
                        Assert.AreEqual("File1", ((CloudFile)item11).Name);

                        IListFileItem item12 = simpleList1.ElementAt(1);
                        Assert.IsTrue(item12.Uri.Equals(share.Uri + "/TopDir1/MidDir1"));
                        Assert.AreEqual("MidDir1", ((CloudFileDirectory)item12).Name);

                        IListFileItem item13 = simpleList1.ElementAt(2);
                        Assert.IsTrue(item13.Uri.Equals(share.Uri + "/TopDir1/MidDir2"));
                        Assert.AreEqual("MidDir2", ((CloudFileDirectory)item13).Name);
                        CloudFileDirectory midDir2 = (CloudFileDirectory)item13;

                        List<IListFileItem> simpleList2 = new List<IListFileItem>();
                        do
                        {
                            IAsyncResult result = midDir2.BeginListFilesAndDirectoriesSegmented(
                                token,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            FileResultSegment segment = midDir2.EndListFilesAndDirectoriesSegmented(result);
                            simpleList2.AddRange(segment.Results);
                            token = segment.ContinuationToken;
                        }
                        while (token != null);
                        Assert.IsTrue(simpleList2.Count == 2);

                        IListFileItem item21 = simpleList2.ElementAt(0);
                        Assert.IsTrue(item21.Uri.Equals(share.Uri + "/TopDir1/MidDir2/EndDir1"));
                        Assert.AreEqual("EndDir1", ((CloudFileDirectory)item21).Name);

                        IListFileItem item22 = simpleList2.ElementAt(1);
                        Assert.IsTrue(item22.Uri.Equals(share.Uri + "/TopDir1/MidDir2/EndDir2"));
                        Assert.AreEqual("EndDir2", ((CloudFileDirectory)item22).Name);
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory deleting a directory that has subdirectories and files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryWithFilesDelete()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                if (CloudFileDirectorySetup(share))
                {
                    CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir1");
                    CloudFile file1 = dir1.GetFileReference("EndFile1");
                    TestHelper.ExpectedException(
                        () => dir1.Delete(),
                        "Delete a non-empty directory",
                        HttpStatusCode.Conflict);

                    file1.Delete();
                    dir1.Delete();
                    Assert.IsFalse(file1.Exists());
                    Assert.IsFalse(dir1.Exists());
                }
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory deleting a directory that has subdirectories and files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryWithFilesDeleteAPM()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                if (CloudFileDirectorySetup(share))
                {
                    CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir1");
                    CloudFile file1 = dir1.GetFileReference("EndFile1");
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        IAsyncResult result = dir1.BeginDelete(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        TestHelper.ExpectedException(
                            () => dir1.EndDelete(result),
                            "Delete a non-empty directory",
                        HttpStatusCode.Conflict);

                        result = file1.BeginDelete(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file1.EndDelete(result);

                        result = dir1.BeginDelete(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        dir1.EndDelete(result);
                    }

                    Assert.IsFalse(file1.Exists());
                    Assert.IsFalse(dir1.Exists());
                }
            }
            finally
            {
                share.Delete();
            }
        }

        /*
        [TestMethod]
        [Description("CloudFileDirectory deleting a directory using conditional access")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryConditionalAccess()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                if (CloudFileDirectorySetup(share))
                {
                    CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir1");
                    CloudFile file1 = dir1.GetFileReference("EndFile1");
                    file1.Delete();
                    dir1.FetchAttributes();
                    string etag = dir1.Properties.ETag;

                    TestHelper.ExpectedException(
                        () => dir1.Delete(AccessCondition.GenerateIfNoneMatchCondition(etag), null),
                        "If none match on conditional test should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    string invalidETag = "\"0x10101010\"";

                    TestHelper.ExpectedException(
                        () => dir1.Delete(AccessCondition.GenerateIfMatchCondition(invalidETag), null),
                        "If none match on conditional test should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    dir1.Delete(AccessCondition.GenerateIfMatchCondition(etag), null);

                    // LastModifiedTime tests
                    CloudFileDirectory dir2 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir2");
                    CloudFile file2 = dir2.GetFileReference("EndFile2");
                    file2.Delete();
                    dir2.FetchAttributes();
                    DateTimeOffset currentModifiedTime = dir2.Properties.LastModified.Value;

                    TestHelper.ExpectedException(
                        () => dir2.Delete(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null),
                        "IfModifiedSince conditional on current modified time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                    TestHelper.ExpectedException(
                        () => dir2.Delete(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                        "IfNotModifiedSince conditional on past time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    DateTimeOffset ancientTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                    TestHelper.ExpectedException(
                        () => dir2.Delete(AccessCondition.GenerateIfNotModifiedSinceCondition(ancientTime), null),
                        "IfNotModifiedSince conditional on past time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    dir2.Delete(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null);
                }
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory deleting a directory using conditional access")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryConditionalAccessAPM()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                if (CloudFileDirectorySetup(share))
                {
                    CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir1");
                    CloudFile file1 = dir1.GetFileReference("EndFile1");
                    file1.Delete();
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        IAsyncResult result = dir1.BeginFetchAttributes(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        dir1.EndFetchAttributes(result);
                        string etag = dir1.Properties.ETag;

                        TestHelper.ExpectedException(
                        () => dir1.Delete(AccessCondition.GenerateIfNoneMatchCondition(etag), null),
                        "If none match on conditional test should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                        string invalidETag = "\"0x10101010\"";

                        TestHelper.ExpectedException(
                        () => dir1.Delete(AccessCondition.GenerateIfMatchCondition(invalidETag), null),
                        "If none match on conditional test should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                        dir1.Delete(AccessCondition.GenerateIfMatchCondition(etag), null);

                        // LMT tests
                        CloudFileDirectory dir2 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir2");
                        CloudFile file2 = dir2.GetFileReference("EndFile2");
                        file2.Delete();
                        IAsyncResult result2 = dir2.BeginFetchAttributes(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        dir1.EndFetchAttributes(result2);
                        DateTimeOffset currentModifiedTime = dir2.Properties.LastModified.Value;

                        TestHelper.ExpectedException(
                        () => dir2.Delete(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null),
                        "IfModifiedSince conditional on current modified time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                        DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                        TestHelper.ExpectedException(
                            () => dir2.Delete(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                            "IfNotModifiedSince conditional on past time should throw",
                            HttpStatusCode.PreconditionFailed,
                            "ConditionNotMet");

                        DateTimeOffset ancientTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                        TestHelper.ExpectedException(
                            () => dir2.Delete(AccessCondition.GenerateIfNotModifiedSinceCondition(ancientTime), null),
                            "IfNotModifiedSince conditional on past time should throw",
                            HttpStatusCode.PreconditionFailed,
                            "ConditionNotMet");

                        dir2.Delete(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null);
                    }
                }
            }
            finally
            {
                share.Delete();
            }
        }
        */

        [TestMethod]
        [Description("CloudFileDirectory creating a file without creating the directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryFileCreateWithoutDirectory()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("Dir1");
                CloudFile file = dir.GetFileReference("file1");
                TestHelper.ExpectedException(
                    () => file.Create(0),
                    "Creating a file when the directory has not been created should throw",
                    HttpStatusCode.NotFound,
                    "ParentNotFound");

                // File creation directly in the share should pass.
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file2");
                file2.Create(0);

                dir.Create();
                file.Create(0);
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory creating subdirectory when the parent directory ahsn't been created yet")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryCreateDirectoryUsingPrefix()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                share.Create();
                CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("Dir1");
                CloudFileDirectory dir2 = share.GetRootDirectoryReference().GetDirectoryReference("Dir1/Dir2");
                TestHelper.ExpectedException(
                    () => dir2.Create(),
                    "Try to create directory hierarchy by specifying prefix",
                    HttpStatusCode.NotFound);

                dir1.Create();
                dir2.Create();

            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory get parent of File")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryGetParent()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);
            
            CloudFile file = share.GetRootDirectoryReference().GetDirectoryReference("Dir1").GetFileReference("File1");
            Assert.AreEqual("File1", file.Name);

            // get the file's parent
            CloudFileDirectory parent = file.Parent;
            Assert.AreEqual(parent.Name, "Dir1");

            // get share as parent
            CloudFileDirectory root = parent.Parent;
            Assert.AreEqual(root.Name, "");

            // make sure the parent of the share dir is null
            CloudFileDirectory empty = root.Parent;
            Assert.IsNull(empty);

            // from share, get directory reference to share
            root = share.GetRootDirectoryReference();
            Assert.AreEqual("", root.Name);
            Assert.AreEqual(share.Uri.AbsoluteUri, root.Uri.AbsoluteUri);

            // make sure the parent of the share dir is null
            empty = root.Parent;
            Assert.IsNull(empty);
        }

        [TestMethod]
        [Description("Get subdirectory and then traverse back to parent")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryGetSubdirectoryAndTraverseBackToParent()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1");
            CloudFileDirectory subDirectory = directory.GetDirectoryReference("MidDir1");
            CloudFileDirectory parent = subDirectory.Parent;
            Assert.AreEqual(parent.Name, directory.Name);
            Assert.AreEqual(parent.Uri, directory.Uri);
        }

        [TestMethod]
        [Description("Get parent on root")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryGetParentOnRoot()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            CloudFileDirectory root = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1");
            CloudFileDirectory parent = root.Parent;
            Assert.IsNotNull(parent);

            CloudFileDirectory empty = parent.Parent;
            Assert.IsNull(empty);
        }

        [TestMethod]
        [Description("Hierarchical traversal")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryHierarchicalTraversal()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            ////Traverse hierarchically starting with length 1
            CloudFileDirectory directory1 = share.GetRootDirectoryReference().GetDirectoryReference("Dir1");
            CloudFileDirectory subdir1 = directory1.GetDirectoryReference("Dir2");
            CloudFileDirectory parent1 = subdir1.Parent;
            Assert.AreEqual(parent1.Name, directory1.Name);

            CloudFileDirectory subdir2 = subdir1.GetDirectoryReference("Dir3");
            CloudFileDirectory parent2 = subdir2.Parent;
            Assert.AreEqual(parent2.Name, subdir1.Name);

            CloudFileDirectory subdir3 = subdir2.GetDirectoryReference("Dir4");
            CloudFileDirectory parent3 = subdir3.Parent;
            Assert.AreEqual(parent3.Name, subdir2.Name);

            CloudFileDirectory subdir4 = subdir3.GetDirectoryReference("Dir5");
            CloudFileDirectory parent4 = subdir4.Parent;
            Assert.AreEqual(parent4.Name, subdir3.Name);
        }

        [TestMethod]
        [Description("Get directory parent for file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryFileParentValidate()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            CloudFile file = share.GetRootDirectoryReference().
                GetDirectoryReference("TopDir1").
                GetDirectoryReference("MidDir1").
                GetDirectoryReference("EndDir1").
                GetFileReference("EndFile1");
            CloudFileDirectory directory = file.Parent;
            Assert.AreEqual(directory.Name, "EndDir1");
            Assert.AreEqual(directory.Uri, share.Uri + "/TopDir1/MidDir1/EndDir1");
        }

        [TestMethod]
        [Description("Get a reference to an empty sub-directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryGetEmptySubDirectory()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            CloudFileDirectory root = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1");
            TestHelper.ExpectedException<ArgumentException>(
                () => root.GetDirectoryReference(string.Empty),
                "Try to get a reference to an empty sub-directory");
        }

        [TestMethod]
        [Description("Using absolute Uri string should just append to the base uri")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDirectoryAbsoluteUriAppended()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference(share.Uri.AbsoluteUri);
            Assert.AreEqual(NavigationHelper.AppendPathToSingleUri(share.Uri, share.Uri.AbsoluteUri), dir.Uri);
            Assert.AreEqual(new Uri(share.Uri + "/" + share.Uri.AbsoluteUri), dir.Uri);

            dir = share.GetRootDirectoryReference().GetDirectoryReference(share.Uri.AbsoluteUri + "/TopDir1");
            Assert.AreEqual(NavigationHelper.AppendPathToSingleUri(share.Uri, share.Uri.AbsoluteUri + "/TopDir1"), dir.Uri);
        }
    }
}
