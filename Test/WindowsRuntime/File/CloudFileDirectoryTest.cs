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
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    [TestClass]
    public class CloudFileDirectoryTest : FileTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public CloudFileDirectoryTest()
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

        private async Task<bool> CloudFileDirectorySetupAsync(CloudFileShare share)
        {
            try
            {
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                for (int i = 1; i < 3; i++)
                {
                    CloudFileDirectory topDirectory = rootDirectory.GetDirectoryReference("TopDir" + i);
                    await topDirectory.CreateAsync();

                    for (int j = 1; j < 3; j++)
                    {
                        CloudFileDirectory midDirectory = topDirectory.GetDirectoryReference("MidDir" + j);
                        await midDirectory.CreateAsync();

                        for (int k = 1; k < 3; k++)
                        {
                            CloudFileDirectory endDirectory = midDirectory.GetDirectoryReference("EndDir" + k);
                            await endDirectory.CreateAsync();

                            CloudFile file1 = endDirectory.GetFileReference("EndFile" + k);
                            await file1.CreateAsync(0);
                        }
                    }

                    CloudFile file2 = topDirectory.GetFileReference("File" + i);
                    await file2.CreateAsync(0);
                }

                return true;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        [TestMethod]
        [Description("Create a directory and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryCreateAndDeleteAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                await directory.CreateAsync();
                Assert.IsTrue(await directory.ExistsAsync());
                await directory.DeleteAsync();
                Assert.IsFalse(await directory.ExistsAsync());
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to create an existing directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryCreateIfNotExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                Assert.IsTrue(await directory.CreateIfNotExistsAsync());
                Assert.IsFalse(await directory.CreateIfNotExistsAsync());
                await directory.DeleteAsync();
                Assert.IsTrue(await directory.CreateIfNotExistsAsync());
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that a file directory's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectorySetMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                await directory.CreateAsync();

                CloudFileDirectory directory2 = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                await directory2.FetchAttributesAsync();
                Assert.AreEqual(0, directory2.Metadata.Count);

                directory.Metadata["key1"] = null;
                OperationContext context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await directory.SetMetadataAsync(null /* accessConditions */, null /* options */, context),
                    context,
                    "Metadata keys should have a non-null value",
                    HttpStatusCode.Unused);

                directory.Metadata["key1"] = "";
                await TestHelper.ExpectedExceptionAsync(
                    async () => await directory.SetMetadataAsync(null /* accessConditions */, null /* options */, context),
                    context,
                    "Metadata keys should have a non-empty value",
                    HttpStatusCode.Unused);

                directory.Metadata["key1"] = "value1";
                await directory.SetMetadataAsync(null /* accessConditions */, null /* options */, context);

                await directory2.FetchAttributesAsync();
                Assert.AreEqual(1, directory2.Metadata.Count);
                Assert.AreEqual("value1", directory2.Metadata["key1"]);

                directory.Metadata.Clear();
                await directory.SetMetadataAsync(null /* accessConditions */, null /* options */, context);

                await directory2.FetchAttributesAsync();
                Assert.AreEqual(0, directory2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryDeleteIfExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                CloudFileDirectory directory = share.GetRootDirectoryReference().GetDirectoryReference("directory1");
                Assert.IsFalse(await directory.DeleteIfExistsAsync());
                await directory.CreateAsync();
                Assert.IsTrue(await directory.DeleteIfExistsAsync());
                Assert.IsFalse(await directory.DeleteIfExistsAsync());
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory listing")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryListFilesAndDirectoriesAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                await share.CreateAsync();
                if (await CloudFileDirectorySetupAsync(share))
                {
                    CloudFileDirectory topDir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1");
                    IEnumerable<IListFileItem> list1 = await ListFilesAndDirectoriesAsync(topDir1, null, null, null);

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
                    CloudFileDirectory midDir2 = (CloudFileDirectory)item13;
                    Assert.AreEqual("MidDir2", ((CloudFileDirectory)item13).Name);

                    IEnumerable<IListFileItem> list2 = await ListFilesAndDirectoriesAsync(midDir2, null, null, null);

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
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory deleting a directory that has subdirectories and files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryWithFilesDeleteAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                await share.CreateAsync();
                if (await CloudFileDirectorySetupAsync(share))
                {
                    CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir1");
                    CloudFile file1 = dir1.GetFileReference("EndFile1");
                    OperationContext context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await dir1.DeleteAsync(null, null, context),
                        context,
                        "Delete a non-empty directory",
                        HttpStatusCode.Conflict);

                    await file1.DeleteAsync();
                    await dir1.DeleteAsync();
                    Assert.IsFalse(await file1.ExistsAsync());
                    Assert.IsFalse(await dir1.ExistsAsync());
                }
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        /*
        [TestMethod]
        [Description("CloudFileDirectory deleting a directory using conditional access")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryConditionalAccessAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                await share.CreateAsync();
                if (await CloudFileDirectorySetupAsync(share))
                {
                    CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir1/");
                    CloudFile file1 = dir1.GetFileReference("EndFile1");
                    await file1.DeleteAsync();
                    await dir1.FetchAttributesAsync();
                    string etag = dir1.Properties.ETag;

                    OperationContext context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await dir1.DeleteAsync(AccessCondition.GenerateIfNoneMatchCondition(etag), null, context),
                        context,
                        "If none match on conditional test should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    string invalidETag = "\"0x10101010\"";

                    context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await dir1.DeleteAsync(AccessCondition.GenerateIfMatchCondition(invalidETag), null, context),
                        context,
                        "If none match on conditional test should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    await dir1.DeleteAsync(AccessCondition.GenerateIfMatchCondition(etag), null, null);

                    // LastModifiedTime tests
                    CloudFileDirectory dir2 = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/MidDir1/EndDir2/");
                    CloudFile file2 = dir2.GetFileReference("EndFile2");
                    await file2.DeleteAsync();
                    await dir2.FetchAttributesAsync();
                    DateTimeOffset currentModifiedTime = dir2.Properties.LastModified.Value;

                    context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await dir2.DeleteAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, context),
                        context,
                        "IfModifiedSince conditional on current modified time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                    context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await dir2.DeleteAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, context),
                        context,
                        "IfNotModifiedSince conditional on past time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    DateTimeOffset ancientTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                    context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await dir2.DeleteAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(ancientTime), null, context),
                        context,
                        "IfNotModifiedSince conditional on past time should throw",
                        HttpStatusCode.PreconditionFailed,
                        "ConditionNotMet");

                    await dir2.DeleteAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null, null);
                }
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
        */

        [TestMethod]
        [Description("CloudFileDirectory creating a file without creating the directory")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryFileCreateWithoutDirectoryAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);
            CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

            try
            {
                await share.CreateAsync();
                CloudFileDirectory dir = rootDirectory.GetDirectoryReference("Dir1");
                CloudFile file = dir.GetFileReference("file1");
                OperationContext context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.CreateAsync(0, null, null, context),
                    context,
                    "Creating a file when the directory has not been created should throw",
                    HttpStatusCode.NotFound,
                    "ParentNotFound");

                // File creation directly in the share should pass.
                CloudFile file2 = rootDirectory.GetFileReference("file2");
                await file2.CreateAsync(0);

                await dir.CreateAsync();
                await file.CreateAsync(0);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory creating subdirectory when the parent directory ahsn't been created yet")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryCreateDirectoryUsingPrefixAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);

            try
            {
                await share.CreateAsync();
                CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("Dir1");
                CloudFileDirectory dir2 = share.GetRootDirectoryReference().GetDirectoryReference("Dir1/Dir2");
                OperationContext context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await dir2.CreateAsync(null, context),
                    context,
                    "Try to create directory hierarchy by specifying prefix",
                    HttpStatusCode.NotFound);

                await dir1.CreateAsync();
                await dir2.CreateAsync();
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("CloudFileDirectory get parent of File")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDirectoryGetParentAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);
            try
            {
                await share.CreateAsync();
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
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
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

            CloudFileDirectory root = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/");
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

            CloudFile file = share.GetRootDirectoryReference().GetFileReference("TopDir1/MidDir1/EndDir1/EndFile1");
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

            CloudFileDirectory root = share.GetRootDirectoryReference().GetDirectoryReference("TopDir1/");
            TestHelper.ExpectedException<ArgumentException>(
                () => root.GetDirectoryReference(String.Empty),
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
