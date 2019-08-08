// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileTest.cs" company="Microsoft">
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

using Microsoft.Azure.Storage.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;

#if NETCORE
using System.Security.Cryptography;
#else
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
#endif

namespace Microsoft.Azure.Storage.File
{
    [TestClass]
    public class CloudFileTest : FileTestBase
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

        [TestMethod]
        [Description("Create a zero-length file and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCreateAndDeleteTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");

                // Act
                await file.CreateAsync(0);

                // Assert
                Assert.IsNotNull(file.Properties.FilePermissionKey);
                Assert.IsNotNull(file.Properties.NtfsAttributes);
                Assert.IsNotNull(file.Properties.CreationTime);
                Assert.IsNotNull(file.Properties.LastWriteTime);
                Assert.IsNotNull(file.Properties.ChangeTime);
                Assert.IsNotNull(file.Properties.FileId);
                Assert.IsNotNull(file.Properties.ParentId);

                Assert.IsNull(file.Properties.filePermissionKeyToSet);
                Assert.IsNull(file.Properties.ntfsAttributesToSet);
                Assert.IsNull(file.Properties.creationTimeToSet);
                Assert.IsNull(file.Properties.lastWriteTimeToSet);
                Assert.IsNull(file.FilePermission);

                // Cleanup
                await file.DeleteAsync();
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Create a file with a file permission key")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCreateFilePermissionKeyTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();
                string permission = "O:S-1-5-21-2127521184-1604012920-1887927527-21560751G:S-1-5-21-2127521184-1604012920-1887927527-513D:AI(A;;FA;;;SY)(A;;FA;;;BA)(A;;0x1200a9;;;S-1-5-21-397955417-626881126-188441444-3053964)";
                string permissionKey = await share.CreateFilePermissionAsync(permission);
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Properties.FilePermissionKey = permissionKey;

                // Act
                await file.CreateAsync(0);

                // Assert
                Assert.IsNotNull(file.Properties.FilePermissionKey);
                Assert.IsNotNull(file.Properties.NtfsAttributes);
                Assert.IsNotNull(file.Properties.CreationTime);
                Assert.IsNotNull(file.Properties.LastWriteTime);
                Assert.IsNotNull(file.Properties.ChangeTime);
                Assert.IsNotNull(file.Properties.FileId);
                Assert.IsNotNull(file.Properties.ParentId);

                Assert.IsNull(file.Properties.filePermissionKeyToSet);
                Assert.IsNull(file.Properties.ntfsAttributesToSet);
                Assert.IsNull(file.Properties.creationTimeToSet);
                Assert.IsNull(file.Properties.lastWriteTimeToSet);
                Assert.IsNull(file.FilePermission);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Create a file with multiple parameters")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCreateMultibleParametersAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");

                string cacheControl = "no-transform";
                string contentDisposition = "attachment";
                string contentEncoding = "gzip";
                string contentLanguage = "tr,en";
                string contentMD5 = "MDAwMDAwMDA=";
                string contentType = "text/html";

                string permissions = "O:S-1-5-21-2127521184-1604012920-1887927527-21560751G:S-1-5-21-2127521184-1604012920-1887927527-513D:AI(A;;FA;;;SY)(A;;FA;;;BA)(A;;0x1200a9;;;S-1-5-21-397955417-626881126-188441444-3053964)";
                CloudFileNtfsAttributes attributes = CloudFileNtfsAttributes.Archive | CloudFileNtfsAttributes.NoScrubData | CloudFileNtfsAttributes.Offline;
                DateTimeOffset creationTime = DateTimeOffset.UtcNow.AddDays(-1);
                DateTimeOffset lastWriteTime = DateTimeOffset.UtcNow;

                file.Properties.CacheControl = cacheControl;
                file.Properties.ContentDisposition = contentDisposition;
                file.Properties.ContentEncoding = contentEncoding;
                file.Properties.ContentLanguage = contentLanguage;
                file.Properties.ContentMD5 = contentMD5;
                file.Properties.ContentType = contentType;

                file.FilePermission = permissions;
                file.Properties.CreationTime = creationTime;
                file.Properties.LastWriteTime = lastWriteTime;
                file.Properties.NtfsAttributes = attributes;

                // Act
                await file.CreateAsync(0);

                // Assert
                Assert.AreEqual(cacheControl, file.Properties.CacheControl);
                Assert.AreEqual(contentDisposition, file.Properties.ContentDisposition);
                Assert.AreEqual(contentEncoding, file.Properties.ContentEncoding);
                Assert.AreEqual(contentLanguage, file.Properties.ContentLanguage);
                Assert.AreEqual(contentMD5, file.Properties.ContentMD5);
                Assert.AreEqual(contentType, file.Properties.ContentType);

                Assert.IsNotNull(file.Properties.FilePermissionKey);
                Assert.AreEqual(attributes, file.Properties.NtfsAttributes);
                Assert.AreEqual(creationTime, file.Properties.CreationTime);
                Assert.AreEqual(lastWriteTime, file.Properties.LastWriteTime);

                Assert.IsNotNull(file.Properties.ChangeTime);
                Assert.IsNotNull(file.Properties.FileId);
                Assert.IsNotNull(file.Properties.ParentId);

                Assert.IsNull(file.Properties.filePermissionKeyToSet);
                Assert.IsNull(file.Properties.ntfsAttributesToSet);
                Assert.IsNull(file.Properties.creationTimeToSet);
                Assert.IsNull(file.Properties.lastWriteTimeToSet);
                Assert.IsNull(file.FilePermission);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Resize a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileResizeAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                await file.CreateAsync(1024);
                Assert.AreEqual(1024, file.Properties.Length);
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1024, file2.Properties.Length);
                file2.Properties.ContentType = "text/plain";
                await file2.SetPropertiesAsync();
                await file.ResizeAsync(2048);
                Assert.AreEqual(2048, file.Properties.Length);
                await file.FetchAttributesAsync();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                await file2.FetchAttributesAsync();
                Assert.AreEqual(2048, file2.Properties.Length);

                // Resize to 0 length
                await file.ResizeAsync(0);
                Assert.AreEqual(0, file.Properties.Length);
                await file.FetchAttributesAsync();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                await file2.FetchAttributesAsync();
                Assert.AreEqual(0, file2.Properties.Length);
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDeleteIfExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                Assert.IsFalse(await file.DeleteIfExistsAsync());
                await file.CreateAsync(0);
                Assert.IsTrue(await file.DeleteIfExistsAsync());
                Assert.IsFalse(await file.DeleteIfExistsAsync());
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Check a file's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileExistsAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                Assert.IsFalse(await file2.ExistsAsync());

                await file.CreateAsync(2048);

                Assert.IsTrue(await file2.ExistsAsync());
                Assert.AreEqual(2048, file2.Properties.Length);

                CloudFileDirectory dir1 = share.GetRootDirectoryReference().GetDirectoryReference("file1");

                Assert.IsFalse(await dir1.ExistsAsync());

                await file.DeleteAsync();

                Assert.IsFalse(await file2.ExistsAsync());

                CloudFileDirectory dir2 = share.GetRootDirectoryReference().GetDirectoryReference("file1");

                Assert.IsFalse(await dir2.ExistsAsync());
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileFetchAttributesTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                // Act
                await file2.FetchAttributesAsync();

                // Assert
                Assert.AreEqual(1024, file2.Properties.Length);
                Assert.AreEqual(file.Properties.ETag, file2.Properties.ETag);
                Assert.AreEqual(file.Properties.LastModified, file2.Properties.LastModified);
#if WINDOWS_RT && !WINDOWS_PHONE
                Assert.IsNull(file2.Properties.CacheControl);
#endif
                Assert.IsNull(file2.Properties.ContentDisposition);
                Assert.IsNull(file2.Properties.ContentEncoding);
                Assert.IsNull(file2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", file2.Properties.ContentType);
                Assert.IsNull(file2.Properties.ContentMD5);

                Assert.AreEqual(file.Properties.FilePermissionKey, file2.Properties.FilePermissionKey);
                Assert.AreEqual(file.Properties.NtfsAttributes, file2.Properties.NtfsAttributes);
                Assert.AreEqual(file.Properties.CreationTime, file2.Properties.CreationTime);
                Assert.AreEqual(file.Properties.LastWriteTime, file2.Properties.LastWriteTime);
                Assert.AreEqual(file.Properties.ChangeTime, file2.Properties.ChangeTime);
                Assert.AreEqual(file.Properties.FileId, file2.Properties.FileId);
                Assert.AreEqual(file.Properties.ParentId, file2.Properties.ParentId);

                Assert.IsNull(file2.Properties.filePermissionKeyToSet);
                Assert.IsNull(file2.Properties.ntfsAttributesToSet);
                Assert.IsNull(file2.Properties.creationTimeToSet);
                Assert.IsNull(file2.Properties.lastWriteTimeToSet);
                Assert.IsNull(file2.FilePermission);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSetPropertiesTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                string eTag = file.Properties.ETag;
                DateTimeOffset lastModified = file.Properties.LastModified.Value;

                Thread.Sleep(1000);

                string cacheControl = "no-transform";
                string contentDisposition = "attachment";
                string contentEncoding = "gzip";
                string contentLanguage = "tr,en";
                string contentMD5 = "MDAwMDAwMDA=";
                string contentType = "text/html";

                file.Properties.CacheControl = cacheControl;
                file.Properties.ContentDisposition = contentDisposition;
                file.Properties.ContentEncoding = contentEncoding;
                file.Properties.ContentLanguage = contentLanguage;
                file.Properties.ContentMD5 = contentMD5;
                file.Properties.ContentType = contentType;

                await file.SetPropertiesAsync();

                Assert.IsTrue(file.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, file.Properties.ETag);
                Assert.AreEqual(cacheControl, file.Properties.CacheControl);
                Assert.AreEqual(contentDisposition, file.Properties.ContentDisposition);
                Assert.AreEqual(contentEncoding, file.Properties.ContentEncoding);
                Assert.AreEqual(contentLanguage, file.Properties.ContentLanguage);
                Assert.AreEqual(contentMD5, file.Properties.ContentMD5);
                Assert.AreEqual(contentType, file.Properties.ContentType);

                Assert.IsNull(file.Properties.filePermissionKeyToSet);
                Assert.IsNull(file.Properties.creationTimeToSet);
                Assert.IsNull(file.Properties.lastWriteTimeToSet);
                Assert.IsNull(file.Properties.ntfsAttributesToSet);
                Assert.IsNull(file.FilePermission);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                await file2.FetchAttributesAsync();

                Assert.AreEqual(cacheControl, file2.Properties.CacheControl);
                Assert.AreEqual(contentDisposition, file2.Properties.ContentDisposition);
                Assert.AreEqual(contentEncoding, file2.Properties.ContentEncoding);
                Assert.AreEqual(contentLanguage, file2.Properties.ContentLanguage);
                Assert.AreEqual(contentMD5, file2.Properties.ContentMD5);
                Assert.AreEqual(contentType, file2.Properties.ContentType);

                Assert.AreEqual(file.Properties.FilePermissionKey, file2.Properties.FilePermissionKey);
                Assert.AreEqual(file.Properties.NtfsAttributes, file2.Properties.NtfsAttributes);
                Assert.AreEqual(file.Properties.CreationTime, file2.Properties.CreationTime);
                Assert.AreEqual(file.Properties.LastWriteTime, file2.Properties.LastWriteTime);

                CloudFile file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream stream = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    await file3.DownloadToStreamAsync(stream, null, options, null);
                }
                AssertAreEqual(file2.Properties, file3.Properties);

                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                CloudFile file4 = (CloudFile)rootDirectory.ListFilesAndDirectoriesSegmentedAsync(null).Result.Results.First();
                Assert.AreEqual(file2.Properties.Length, file4.Properties.Length);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a file with file permissions key")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSetPropertiesFilePermissionsKeyTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);

                Thread.Sleep(1000);

                string permission = "O:S-1-5-21-2127521184-1604012920-1887927527-21560751G:S-1-5-21-2127521184-1604012920-1887927527-513D:AI(A;;FA;;;SY)(A;;FA;;;BA)(A;;0x1200a9;;;S-1-5-21-397955417-626881126-188441444-3053964)";
                string permissionKey = await share.CreateFilePermissionAsync(permission);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                file2.Properties.FilePermissionKey = permissionKey;

                // Act
                await file2.SetPropertiesAsync();

                // Assert
                Assert.IsNotNull(file2.Properties.FilePermissionKey);
                Assert.IsNull(file2.Properties.filePermissionKeyToSet);

                // Act
                CloudFile file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file3.FetchAttributesAsync();

                // Assert - also making sure attributes, creation time, and last-write time were preserved
                Assert.AreEqual(permissionKey, file3.Properties.FilePermissionKey);
                Assert.AreEqual(file2.Properties.filePermissionKey, file3.Properties.FilePermissionKey);
                Assert.AreEqual(file.Properties.NtfsAttributes, file3.Properties.NtfsAttributes);
                Assert.AreEqual(file.Properties.CreationTime, file3.Properties.CreationTime);
                Assert.AreEqual(file.Properties.LastWriteTime, file3.Properties.LastWriteTime);

                // This block is just for checking that file permission is preserved
                // Arrange
                file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                DateTimeOffset creationTime = DateTime.UtcNow.AddDays(-2);
                file2.Properties.creationTime = creationTime;

                // Act
                await file2.SetPropertiesAsync();
                file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file3.FetchAttributesAsync();

                // Assert
                Assert.AreEqual(permissionKey, file3.Properties.FilePermissionKey);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

#if NETCORE
        [TestMethod]
        [Description("Verify setting the properties of a file with spacial characters such as '<' and getting them")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSetPropertiesSpecialCharactersAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                string eTag = file.Properties.ETag;
                DateTimeOffset lastModified = file.Properties.LastModified.Value;

                await Task.Delay(1000);

                file.Properties.CacheControl = "no-tr>ansform";
                file.Properties.ContentEncoding = "gzi<p";
                file.Properties.ContentLanguage = "tr,e>n";
                file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                file.Properties.ContentType = "text/html>";

                file.Properties.ContentDisposition = "in<aliContentDisposition";
                await file.SetPropertiesAsync();
                Assert.IsTrue(file.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, file.Properties.ETag);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual("no-tr>ansform", file2.Properties.CacheControl);
                Assert.AreEqual("gzi<p", file2.Properties.ContentEncoding);
                Assert.AreEqual("tr,e>n", file2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", file2.Properties.ContentMD5);
                Assert.AreEqual("text/html>", file2.Properties.ContentType);
                Assert.AreEqual("in<aliContentDisposition", file2.Properties.ContentDisposition);

            }
            finally
            {
                await share.DeleteAsync();
            }
        }
#endif

        [TestMethod]
        [Description("Verify that creating a file can also set its metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCreateWithMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Metadata["key1"] = "value1";
                file.Properties.CacheControl = "no-transform";
                file.Properties.ContentDisposition = "attachment";
                file.Properties.ContentEncoding = "gzip";
                file.Properties.ContentLanguage = "tr,en";
                file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                file.Properties.ContentType = "text/html";
                await file.CreateAsync(1024);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);
                // Metadata keys should be case-insensitive
                Assert.AreEqual("value1", file2.Metadata["KEY1"]);
                Assert.AreEqual("no-transform", file2.Properties.CacheControl);
                Assert.AreEqual("attachment", file2.Properties.ContentDisposition);
                Assert.AreEqual("gzip", file2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", file2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", file2.Properties.ContentMD5);
                Assert.AreEqual("text/html", file2.Properties.ContentType);
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Verify that a file's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileSetMetadataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(0, file2.Metadata.Count);

                OperationContext operationContext = new OperationContext();
                file.Metadata["key1"] = null;

                Assert.ThrowsException<AggregateException>(
                    () => file.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "";
                Assert.ThrowsException<AggregateException>(
                    () => file.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "value1";
                await file.SetMetadataAsync();

                await file2.FetchAttributesAsync();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);
                // Metadata keys should be case-insensitive
                Assert.AreEqual("value1", file2.Metadata["KEY1"]);

                file.Metadata.Clear();
                await file.SetMetadataAsync();

                await file2.FetchAttributesAsync();
                Assert.AreEqual(0, file2.Metadata.Count);
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload/clear ranges in a file and then verify ranges")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileListRangesAsync()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await file.WriteRangeAsync(memoryStream, 512, null);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await file.WriteRangeAsync(memoryStream, 3 * 1024, null);
                }

                await file.ClearRangeAsync(1024, 1024);
                await file.ClearRangeAsync(0, 512);

                IEnumerable<FileRange> fileRanges = await file.ListRangesAsync();
                List<string> expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (FileRange fileRange in fileRanges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(fileRange.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                fileRanges = await file.ListRangesAsync(1024, 1024, null, null, null);
                Assert.AreEqual(0, fileRanges.Count());

                fileRanges = await file.ListRangesAsync(512, 3 * 1024, null, null, null);
                expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (FileRange fileRange in fileRanges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(fileRange.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.ListRangesAsync(1024, null, null, null, opContext),
                    opContext,
                    "List Ranges with an offset but no count should fail",
                    HttpStatusCode.Unused);
                Assert.IsInstanceOfType(opContext.LastResult.Exception.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload ranges to a file and then verify the contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileWriteRangeAsync()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
#if NETCORE
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));
#else
            CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
            hasher.Append(buffer.AsBuffer());
            string contentMD5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset());
#endif

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(4 * 1024 * 1024);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await file.WriteRangeAsync(memoryStream, 0, null),
                        "Zero-length WriteRange should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        OperationContext opContext = new OperationContext();
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await file.WriteRangeAsync(memoryStream, 512, null, null, null, opContext),
                            opContext,
                            "Writing out-of-range ranges should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await file.WriteRangeAsync(memoryStream, 0, contentMD5);
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await file.WriteRangeAsync(memoryStream, 0, contentMD5, null, null, opContext),
                            opContext,
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await file.WriteRangeAsync(memoryStream, 0, null);
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await file.WriteRangeAsync(memoryStream, 1024, null);
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream fileData = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(fileData);
                        Assert.AreEqual(resultingData.Length, fileData.Length);

                        Assert.IsTrue(fileData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileDownloadToStreamTask()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(originalFile);

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                        // Act
                        await file2.DownloadRangeToStreamAsync(downloadedFile, 0, buffer.Length);

                        // Assert
                        TestHelper.AssertStreamsAreEqual(originalFile, downloadedFile);
                        Assert.IsNotNull(file2.Properties.LastModified);
                        Assert.IsNotNull(file2.Properties.ETag);
                        Assert.IsTrue(file2.Properties.IsServerEncrypted);
                        Assert.IsNotNull(file2.Properties.ChangeTime);
                        Assert.IsNotNull(file2.Properties.LastWriteTime);
                        Assert.IsNotNull(file2.Properties.CreationTime);
                        Assert.IsNotNull(file2.Properties.FilePermissionKey);
                        Assert.AreEqual(CloudFileNtfsAttributes.Archive, file2.Properties.NtfsAttributes);
                        Assert.IsNotNull(file2.Properties.FileId);
                        Assert.IsNotNull(file2.Properties.ParentId);
                    }
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [Description("Writes range from source file with invalid parameters")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileWriteRangeFromUrlInvalidParametersAsync()
        {
            CloudFileShare share = GetRandomShareReference();

            try
            {
                // Arrange
                await share.CreateAsync();

                CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("dir");
                await dir.CreateAsync();

                CloudFile destFile = dir.GetFileReference("dest1");
                await destFile.CreateAsync(1024);

                // Act
                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                    () => destFile.WriteRangeAsync(destFile.Uri, sourceOffset: -1, count: 512, destOffset: 0),
                    "CloudFileWriteRangeFromUrlInvalidParametersAsync");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                    () => destFile.WriteRangeAsync(destFile.Uri, sourceOffset: 512, count: -1, destOffset: 0),
                    "CloudFileWriteRangeFromUrlInvalidParametersAsync");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                    () => destFile.WriteRangeAsync(destFile.Uri, sourceOffset: 512, count: 5 * Constants.MB, destOffset: 0),
                    "CloudFileWriteRangeFromUrlInvalidParametersAsync");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                    () => destFile.WriteRangeAsync(destFile.Uri, sourceOffset: 512, count: 512, destOffset: -1),
                    "CloudFileWriteRangeFromUrlInvalidParametersAsync");
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Writes range from source file min parameters")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileWriteRangeFromUrlMinAsync()
        {
            CloudFileShare share = GetRandomShareReference();

            try
            {
                // Arrange
                await share.CreateAsync();

                CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("dir");
                await dir.CreateAsync();

                CloudFile sourceFile = dir.GetFileReference("source");

                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    await sourceFile.UploadFromStreamAsync(stream);
                }

                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessFilePermissions.Read
                };
                string sasToken = sourceFile.GetSharedAccessSignature(policy, null, null);
                Uri sourceUri = new Uri(sourceFile.Uri.ToString() + sasToken);

                CloudFile destFile = dir.GetFileReference("dest1");
                await destFile.CreateAsync(1024);
                FileRange sourceRange = new FileRange(512, 1023);

                // Act
                await destFile.WriteRangeAsync(sourceUri, sourceOffset: 512, count: 512, destOffset: 0);

                using (MemoryStream sourceStream = new MemoryStream())
                using (MemoryStream destStream = new MemoryStream())
                {
                    // Assert
                    await sourceFile.DownloadRangeToStreamAsync(sourceStream, offset: 512, length: 512);
                    await destFile.DownloadRangeToStreamAsync(destStream, offset: 0, length: 512);

                    Assert.IsTrue(sourceStream.ToArray().SequenceEqual(destStream.ToArray()));
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Writes range from source file with source CRC")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileWriteRangeFromUrlSourceCrcAsync()
        {
            CloudFileShare share = GetRandomShareReference();

            try
            {
                // Arrange
                await share.CreateAsync();

                CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("dir");
                await dir.CreateAsync();

                CloudFile sourceFile = dir.GetFileReference("source");

                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    await sourceFile.UploadFromStreamAsync(stream);
                }

                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessFilePermissions.Read
                };
                string sasToken = sourceFile.GetSharedAccessSignature(policy, null, null);
                Uri sourceUri = new Uri(sourceFile.Uri.ToString() + sasToken);

                Crc64Wrapper hasher = new Crc64Wrapper();
                hasher.UpdateHash(buffer.Skip(512).ToArray(), 0, 512);
                string crc64 = hasher.ComputeHash();

                CloudFile destFile = dir.GetFileReference("dest1");
                await destFile.CreateAsync(1024);
                FileRange sourceRange = new FileRange(512, 1023);
                Checksum sourceChecksum = new Checksum(crc64: crc64);

                // Act
                await destFile.WriteRangeAsync(sourceUri, sourceOffset: 512, count: 512, destOffset: 0, sourceContentChecksum: sourceChecksum);

                using (MemoryStream sourceStream = new MemoryStream())
                using (MemoryStream destStream = new MemoryStream())
                {
                    // Assert
                    await sourceFile.DownloadRangeToStreamAsync(sourceStream, 512, 512);
                    await destFile.DownloadRangeToStreamAsync(destStream, 0, 512);

                    Assert.IsTrue(sourceStream.ToArray().SequenceEqual(destStream.ToArray()));
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Writes range from source file with source CRC access conditions")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileWriteRangeFromUrlSourceCrcMatchAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                // Arrange
                await share.CreateAsync();
                CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("dir");
                await dir.CreateAsync();

                CloudFile sourceFile = dir.GetFileReference("source");

                byte[] buffer = GetRandomBuffer(1024);
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    await sourceFile.UploadFromStreamAsync(stream);
                }

                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessFilePermissions.Read
                };
                string sasToken = sourceFile.GetSharedAccessSignature(policy, null, null);
                Uri sourceUri = new Uri(sourceFile.Uri.ToString() + sasToken);

                Crc64Wrapper hasher = new Crc64Wrapper();
                hasher.UpdateHash(buffer.Skip(512).ToArray(), 0, 512);
                string crc64 = hasher.ComputeHash();

                CloudFile destFile = dir.GetFileReference("dest1");
                await destFile.CreateAsync(1024);
                AccessCondition sourceAccessCondition = new AccessCondition()
                {
                    IfNoneMatchContentCrc = crc64
                };

                // Act
                await TestHelper.ExpectedExceptionAsync<StorageException>(
                    () => destFile.WriteRangeAsync(sourceUri, sourceOffset: 512, count: 512, destOffset: 0, sourceAccessCondition: sourceAccessCondition),
                    "CloudFileWriteRangeFromUrlSourceCrcMatch");

                // Arrange
                sourceAccessCondition = new AccessCondition()
                {
                    IfMatchContentCrc = crc64
                };

                // Act
                await destFile.WriteRangeAsync(sourceUri, sourceOffset: 512, count: 512, destOffset: 0, sourceAccessCondition: sourceAccessCondition);

                using (MemoryStream sourceStream = new MemoryStream())
                using (MemoryStream destStream = new MemoryStream())
                {
                    // Assert
                    await sourceFile.DownloadRangeToStreamAsync(sourceStream, 512, 512);
                    await destFile.DownloadRangeToStreamAsync(destStream, 0, 512);

                    Assert.IsTrue(sourceStream.ToArray().SequenceEqual(destStream.ToArray()));
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Create a file and verify its SMB handles can be checked.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileListHandlesNullCaseTask()
        {
            CloudFileShare share = GetRandomShareReference();

            try
            {
                await share.CreateAsync();

                var fileName = "file" + Guid.NewGuid().ToString();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(fileName);
                await file.CreateAsync(512);

                file = share.GetRootDirectoryReference().GetFileReference(fileName);

                FileContinuationToken token = null;
                List<FileHandle> handles = new List<FileHandle>();

                do
                {
                    FileHandleResultSegment response = await file.ListHandlesSegmentedAsync(token);
                    handles.AddRange(response.Results);
                    token = response.ContinuationToken;
                } while (token != null && token.NextMarker != null);

                Assert.AreEqual(0, handles.Count);
            }
            //TODO: create a disposable share
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Create a file and verify its SMB handles can be closed.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCloseAllHandlesTask()
        {
            byte[] buffer = GetRandomBuffer(512);
            CloudFileShare share = GetRandomShareReference();

            try
            {
                await share.CreateAsync();

                var fileName = "file" + Guid.NewGuid().ToString();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(fileName);
                await file.CreateAsync(512);

                share = await share.SnapshotAsync();
                file = share.GetRootDirectoryReference().GetFileReference(fileName);

                FileContinuationToken token = null;
                int handlesClosed = 0;

                do
                {
                    CloseFileHandleResultSegment response = await file.CloseAllHandlesSegmentedAsync(token, null, null, null, CancellationToken.None);
                    handlesClosed += response.NumHandlesClosed;
                    token = response.ContinuationToken;
                } while (token != null && token.NextMarker != null);

                Assert.AreEqual(handlesClosed, 0);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        [TestMethod]
        [Description("Create a file and verify its SMB handles can be closed.")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileCloseHandleTask()
        {
            byte[] buffer = GetRandomBuffer(512);
            CloudFileShare share = GetRandomShareReference();

            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file" + Guid.NewGuid().ToString());
                await file.CreateAsync(512);

                FileContinuationToken token = null;
                int handlesClosed = 0;
                const string nonexistentHandle = "12345";

                do
                {
                    CloseFileHandleResultSegment response = await file.CloseHandleSegmentedAsync(nonexistentHandle, token, null, null, null, CancellationToken.None);
                    handlesClosed += response.NumHandlesClosed;
                    token = response.ContinuationToken;
                } while (token != null && token.NextMarker != null);

                Assert.AreEqual(handlesClosed, 0);
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        /*
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamWithAccessConditionAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("\"*\"");
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0),
                    operationContext,
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                await file.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0),
                    operationContext,
                    "Uploading a file on top of an non-existing file should fail when the ETag doesn't match",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                await this.CloudFileUploadFromStreamAsync(share, 6 * 512, null, accessCondition, operationContext, 0);
            }
            finally
            {
                await share.DeleteAsync();
            }
        }
        */

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, null, null, null, 0);
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, null, null, null, 1024);
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamLengthAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                // Upload half
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, 3 * 512, null, null, 0);
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, 3 * 512, null, null, 1024);

                // Upload full stream
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, 6 * 512, null, null, 0);
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, 4 * 512, null, null, 1024);

                // Exclude last range
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, 5 * 512, null, null, 0);
                await this.CloudFileUploadFromStreamAsyncInternal(share, 6 * 512, 3 * 512, null, null, 1024);
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadFromStreamLengthInvalidAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();
            try
            {
                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await this.CloudFileUploadFromStreamAsyncInternal(share, 3 * 512, 4 * 512, null, null, 0),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await this.CloudFileUploadFromStreamAsyncInternal(share, 3 * 512, 2 * 512, null, null, 1024),
                        "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        private async Task CloudFileUploadFromStreamAsyncInternal(CloudFileShare share, int size, long? copyLength, AccessCondition accessCondition, OperationContext operationContext, int startOffset)
        {
            byte[] buffer = GetRandomBuffer(size);
#if NETCORE
            MD5 hasher = MD5.Create();
            string md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
#else
            CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
            hasher.Append(buffer.AsBuffer(startOffset, copyLength.HasValue ? (int)copyLength.Value : buffer.Length - startOffset));
            string md5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset());
#endif

            CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
            file.StreamWriteSizeInBytes = 512;

            using (MemoryStream originalFileStream = new MemoryStream())
            {
                originalFileStream.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };
                    if (copyLength.HasValue)
                    {
                        await file.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, options, operationContext);
                    }
                    else
                    {
                        await file.UploadFromStreamAsync(sourceStream, accessCondition, options, operationContext);
                    }
                }

                await file.FetchAttributesAsync();
                Assert.AreEqual(md5, file.Properties.ContentMD5);

                using (MemoryStream downloadedFileStream = new MemoryStream())
                {
                    await file.DownloadToStreamAsync(downloadedFileStream);
                    Assert.AreEqual(copyLength ?? originalFileStream.Length, downloadedFileStream.Length);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalFileStream,
                        downloadedFileStream,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalFileStream.Length);
                }
            }
        }

        /*
        [TestMethod]
        [Description("Test conditional access on a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                await file.CreateAsync(1024);
                await file.FetchAttributesAsync();

                string currentETag = file.Properties.ETag;
                DateTimeOffset currentModifiedTime = file.Properties.LastModified.Value;

                // ETag conditional tests
                file.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await file.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(currentETag), null, null);

                await file.FetchAttributesAsync();
                string newETag = file.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                file.Metadata["ETagConditionalName"] = "ETagConditionalValue2";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(newETag), null, operationContext),
                    operationContext,
                    "If none match on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                string invalidETag = "\"0x10101010\"";
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(invalidETag), null, operationContext),
                    operationContext,
                    "Invalid ETag on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                currentETag = file.Properties.ETag;
                await file.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(invalidETag), null, null);

                await file.FetchAttributesAsync();
                newETag = file.Properties.ETag;

                // LastModifiedTime tests
                currentModifiedTime = file.Properties.LastModified.Value;

                file.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await file.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                currentModifiedTime = file.Properties.LastModified.Value;

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                file.Metadata["DateConditionalName"] = "DateConditionalValue2";

                currentETag = file.Properties.ETag;
                await file.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null, null);

                await file.FetchAttributesAsync();
                newETag = file.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                await share.DeleteAsync();
            }
        }
        */

        [TestMethod]
        [Description("Test file sizes")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileAlignmentAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                OperationContext operationContext = new OperationContext();

                await file.CreateAsync(511);
                await file.CreateAsync(512);
                await file.CreateAsync(513);

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(511);
                    await file.WriteRangeAsync(stream, 0, null);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await file.WriteRangeAsync(stream, 0, null);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(513);
                    await file.WriteRangeAsync(stream, 0, null);
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Upload and download null/empty data")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileUploadDownloadNoDataAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await file.UploadFromStreamAsync(null),
                    "Uploading from a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await file.UploadFromStreamAsync(stream);
                }

                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await file.DownloadToStreamAsync(null),
                    "Downloading to a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await file.DownloadToStreamAsync(stream);
                    Assert.AreEqual(0, stream.Length);
                }
            }
            finally
            {
                await share.DeleteAsync();
            }
        }

        [TestMethod]
        [Description("Test CloudFile APIs within a share snapshot")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileApisInShareSnapshotAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);
            await share.CreateAsync();
            CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("dir1");
            await dir.CreateAsync();

            CloudFile file = dir.GetFileReference("file");
            await file.CreateAsync(1024);
            file.Metadata["key1"] = "value1";
            await file.SetMetadataAsync();
            CloudFileShare snapshot = share.SnapshotAsync().Result;
            CloudFile snapshotFile = snapshot.GetRootDirectoryReference().GetDirectoryReference("dir1").GetFileReference("file");
            file.Metadata["key2"] = "value2";
            await file.SetMetadataAsync();
            await snapshotFile.FetchAttributesAsync();

            Assert.IsTrue(snapshotFile.Metadata.Count == 1 && snapshotFile.Metadata["key1"].Equals("value1"));
            // Metadata keys should be case-insensitive
            Assert.IsTrue(snapshotFile.Metadata["KEY1"].Equals("value1"));
            Assert.IsNotNull(snapshotFile.Properties.ETag);

            await file.FetchAttributesAsync();
            Assert.IsTrue(file.Metadata.Count == 2 && file.Metadata["key2"].Equals("value2"));
            Assert.IsTrue(file.Metadata["KEY2"].Equals("value2"));
            Assert.IsNotNull(file.Properties.ETag);
            Assert.AreNotEqual(file.Properties.ETag, snapshotFile.Properties.ETag);

            CloudFile snapshotFile2 = new CloudFile(snapshotFile.SnapshotQualifiedStorageUri, client.Credentials);
            Assert.IsTrue(await snapshotFile2.ExistsAsync());
            Assert.IsTrue(snapshotFile2.Share.SnapshotTime.HasValue);

            await snapshot.DeleteAsync();
            await share.DeleteAsync();
        }

        [TestMethod]
        [Description("Test invalid CloudFile APIs within a share snapshot - TASK")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudFileInvalidApisInShareSnapshotAsync()
        {
            CloudFileClient client = GenerateCloudFileClient();
            string name = GetRandomShareName();
            CloudFileShare share = client.GetShareReference(name);
            await share.CreateAsync();

            CloudFileShare snapshot = await share.SnapshotAsync();
            CloudFile file = snapshot.GetRootDirectoryReference().GetDirectoryReference("dir1").GetFileReference("file");
            try
            {
                await file.CreateAsync(1024);
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                await file.DeleteAsync();
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                await file.SetMetadataAsync();
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                await file.AbortCopyAsync(null);
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                await file.ClearRangeAsync(0, 1024);
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                await file.StartCopyAsync(file);
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }
            try
            {
                await file.UploadFromByteArrayAsync(new byte[1024], 0, 1024);
                Assert.Fail("API should fail in a snapshot");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual(SR.CannotModifyShareSnapshot, e.Message);
            }

            await snapshot.DeleteAsync();
            await share.DeleteAsync();
        }
    }
}
