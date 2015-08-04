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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File
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
        [Description("Test file name validation.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileNameValidation()
        {
            NameValidator.ValidateFileName("alpha");
            NameValidator.ValidateFileName("4lphanum3r1c");
            NameValidator.ValidateFileName("middle-dash");
            NameValidator.ValidateFileName("CAPS");
            NameValidator.ValidateFileName("$root");

            TestInvalidFileHelper(null, "No null.", "Invalid file name. The file name may not be null, empty, or whitespace only.");
            TestInvalidFileHelper("..", "Reserved.", "Invalid file name. This file name is reserved.");
            TestInvalidFileHelper("Clock$", "Reserved.", "Invalid file name. This file name is reserved.");
            TestInvalidFileHelper("endslash/", "No slashes.", "Invalid file name. Check MSDN for more information about valid file naming.");
            TestInvalidFileHelper("middle/slash", "No slashes.", "Invalid file name. Check MSDN for more information about valid file naming.");
            TestInvalidFileHelper("illegal\"char", "Illegal characters.", "Invalid file name. Check MSDN for more information about valid file naming.");
            TestInvalidFileHelper("illegal:char?", "Illegal characters.", "Invalid file name. Check MSDN for more information about valid file naming.");
            TestInvalidFileHelper(string.Empty, "Between 1 and 255 characters.", "Invalid file name. The file name may not be null, empty, or whitespace only.");
            TestInvalidFileHelper(new string('n', 256), "Between 1 and 255 characters.", "Invalid file name length. The file name must be between 1 and 255 characters long.");
        }

        private void TestInvalidFileHelper(string fileName, string failMessage, string exceptionMessage)
        {
            try
            {
                NameValidator.ValidateFileName(fileName);
                Assert.Fail(failMessage);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual(exceptionMessage, e.Message);
            }
        }

        [TestMethod]
        [Description("Create a zero-length file and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateAndDelete()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(0);
                Assert.IsTrue(file.Exists());
                file.Delete();
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Create a zero-length file and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateAndDeleteAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                IAsyncResult result;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                    result = file.BeginCreate(0, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    file.EndCreate(result);

                    result = file.BeginExists(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(file.EndExists(result));

                    result = file.BeginDelete(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    file.EndDelete(result);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Create a zero-length file and then delete it")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateAndDeleteTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(0).Wait();
                Assert.IsTrue(file.ExistsAsync().Result);
                file.DeleteAsync().Wait();
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Get a file reference using its constructor")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileConstructor()
        {
            CloudFileShare share = GetRandomShareReference();
            CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
            CloudFile file2 = new CloudFile(file.StorageUri, null);
            Assert.AreEqual(file.Name, file2.Name);
            Assert.AreEqual(file.StorageUri, file2.StorageUri);
            Assert.AreEqual(file.Share.StorageUri, file2.Share.StorageUri);
            Assert.AreEqual(file.ServiceClient.StorageUri, file2.ServiceClient.StorageUri);
        }

        [TestMethod]
        [Description("Resize a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileResize()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                file.Create(1024);
                Assert.AreEqual(1024, file.Properties.Length);
                file2.FetchAttributes();
                Assert.AreEqual(1024, file2.Properties.Length);
                file2.Properties.ContentType = "text/plain";
                file2.SetProperties();
                file.Resize(2048);
                Assert.AreEqual(2048, file.Properties.Length);
                file.FetchAttributes();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                file2.FetchAttributes();
                Assert.AreEqual(2048, file2.Properties.Length);

                // Resize to 0 length
                file.Resize(0);
                Assert.AreEqual(0, file.Properties.Length);
                file.FetchAttributes();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                file2.FetchAttributes();
                Assert.AreEqual(0, file2.Properties.Length);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Resize a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileResizeAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = file.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndCreate(result);
                    Assert.AreEqual(1024, file.Properties.Length);
                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    file2.Properties.ContentType = "text/plain";
                    result = file2.BeginSetProperties(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndSetProperties(result);
                    Assert.AreEqual(1024, file2.Properties.Length);
                    result = file.BeginResize(2048,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndResize(result);
                    Assert.AreEqual(2048, file.Properties.Length);
                    result = file.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndFetchAttributes(result);
                    Assert.AreEqual("text/plain", file.Properties.ContentType);
                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual(2048, file2.Properties.Length);

                    // Resize to 0 length
                    result = file.BeginResize(0,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndResize(result);
                    Assert.AreEqual(0, file.Properties.Length);
                    result = file.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndFetchAttributes(result);
                    Assert.AreEqual("text/plain", file.Properties.ContentType);
                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual(0, file2.Properties.Length);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Resize a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileResizeTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                file.CreateAsync(1024).Wait();
                Assert.AreEqual(1024, file.Properties.Length);
                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1024, file2.Properties.Length);
                file2.Properties.ContentType = "text/plain";
                file2.SetPropertiesAsync().Wait();
                file.ResizeAsync(2048).Wait();
                Assert.AreEqual(2048, file.Properties.Length);
                file.FetchAttributesAsync().Wait();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(2048, file2.Properties.Length);

                // Resize to 0 length
                file.ResizeAsync(0).Wait();
                Assert.AreEqual(0, file.Properties.Length);
                file.FetchAttributesAsync().Wait();
                Assert.AreEqual("text/plain", file.Properties.ContentType);
                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, file2.Properties.Length);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("File creation should fail with invalid size")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateInvalidSize()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                TestHelper.ExpectedException(
                    () => file.Create(-1),
                    "Creating a file with size<0 should fail",
                    HttpStatusCode.BadRequest);
                TestHelper.ExpectedException(
                    () => file.Create(1L * 1024 * 1024 * 1024 * 1024 + 1),
                    "Creating a file with size>1TB should fail",
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDeleteIfExists()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                Assert.IsFalse(file.DeleteIfExists());
                file.Create(0);
                Assert.IsTrue(file.DeleteIfExists());
                Assert.IsFalse(file.DeleteIfExists());
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDeleteIfExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                    IAsyncResult result = file.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(file.EndDeleteIfExists(result));
                    result = file.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndCreate(result);
                    result = file.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(file.EndDeleteIfExists(result));
                    result = file.BeginDeleteIfExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(file.EndDeleteIfExists(result));
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Try to delete a non-existing file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDeleteIfExistsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                Assert.IsFalse(file.DeleteIfExistsAsync().Result);
                file.CreateAsync(0).Wait();
                Assert.IsTrue(file.DeleteIfExistsAsync().Result);
                Assert.IsFalse(file.DeleteIfExistsAsync().Result);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Check a file's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileExists()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                Assert.IsFalse(file2.Exists());

                file.Create(2048);

                Assert.IsTrue(file2.Exists());
                Assert.AreEqual(2048, file2.Properties.Length);

                file.Delete();

                Assert.IsFalse(file2.Exists());
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Check a file's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileExistsAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();

            try
            {
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = file2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(file2.EndExists(result));

                    file.Create(2048);

                    result = file2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsTrue(file2.EndExists(result));
                    Assert.AreEqual(2048, file2.Properties.Length);

                    file.Delete();

                    result = file2.BeginExists(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Assert.IsFalse(file2.EndExists(result));
                }
            }
            finally
            {
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Check a file's existence")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileExistsTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();

            try
            {
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");

                Assert.IsFalse(file2.ExistsAsync().Result);

                file.CreateAsync(2048).Wait();

                Assert.IsTrue(file2.ExistsAsync().Result);
                Assert.AreEqual(2048, file2.Properties.Length);

                file.DeleteAsync().Wait();

                Assert.IsFalse(file2.ExistsAsync().Result);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
#endif
        [TestMethod]
        [Description("Verify the attributes of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileFetchAttributes()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);
                Assert.AreEqual(1024, file.Properties.Length);
                Assert.IsNotNull(file.Properties.ETag);
                Assert.IsTrue(file.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(file.Properties.CacheControl);
                Assert.IsNull(file.Properties.ContentDisposition);
                Assert.IsNull(file.Properties.ContentEncoding);
                Assert.IsNull(file.Properties.ContentLanguage);
                Assert.IsNull(file.Properties.ContentType);
                Assert.IsNull(file.Properties.ContentMD5);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributes();
                Assert.AreEqual(1024, file2.Properties.Length);
                Assert.AreEqual(file.Properties.ETag, file2.Properties.ETag);
                Assert.AreEqual(file.Properties.LastModified, file2.Properties.LastModified);
                Assert.IsNull(file2.Properties.CacheControl);
                Assert.IsNull(file2.Properties.ContentDisposition);
                Assert.IsNull(file2.Properties.ContentEncoding);
                Assert.IsNull(file2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", file2.Properties.ContentType);
                Assert.IsNull(file2.Properties.ContentMD5);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileFetchAttributesAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                    IAsyncResult result = file.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndCreate(result);
                    Assert.AreEqual(1024, file.Properties.Length);
                    Assert.IsNotNull(file.Properties.ETag);
                    Assert.IsTrue(file.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                    Assert.IsNull(file.Properties.CacheControl);
                    Assert.IsNull(file.Properties.ContentDisposition);
                    Assert.IsNull(file.Properties.ContentEncoding);
                    Assert.IsNull(file.Properties.ContentLanguage);
                    Assert.IsNull(file.Properties.ContentType);
                    Assert.IsNull(file.Properties.ContentMD5);

                    CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual(1024, file2.Properties.Length);
                    Assert.AreEqual(file.Properties.ETag, file2.Properties.ETag);
                    Assert.AreEqual(file.Properties.LastModified, file2.Properties.LastModified);
                    Assert.IsNull(file2.Properties.CacheControl);
                    Assert.IsNull(file2.Properties.ContentDisposition);
                    Assert.IsNull(file2.Properties.ContentEncoding);
                    Assert.IsNull(file2.Properties.ContentLanguage);
                    Assert.AreEqual("application/octet-stream", file2.Properties.ContentType);
                    Assert.IsNull(file2.Properties.ContentMD5);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify the attributes of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileFetchAttributesTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(1024).Wait();
                Assert.AreEqual(1024, file.Properties.Length);
                Assert.IsNotNull(file.Properties.ETag);
                Assert.IsTrue(file.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(file.Properties.CacheControl);
                Assert.IsNull(file.Properties.ContentEncoding);
                Assert.IsNull(file.Properties.ContentLanguage);
                Assert.IsNull(file.Properties.ContentType);
                Assert.IsNull(file.Properties.ContentMD5);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1024, file2.Properties.Length);
                Assert.AreEqual(file.Properties.ETag, file2.Properties.ETag);
                Assert.AreEqual(file.Properties.LastModified, file2.Properties.LastModified);
                Assert.IsNull(file2.Properties.CacheControl);
                Assert.IsNull(file2.Properties.ContentEncoding);
                Assert.IsNull(file2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", file2.Properties.ContentType);
                Assert.IsNull(file2.Properties.ContentMD5);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Verify setting the properties of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSetProperties()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);
                string eTag = file.Properties.ETag;
                DateTimeOffset lastModified = file.Properties.LastModified.Value;

                Thread.Sleep(1000);

                file.Properties.CacheControl = "no-transform";
                file.Properties.ContentDisposition = "attachment";
                file.Properties.ContentEncoding = "gzip";
                file.Properties.ContentLanguage = "tr,en";
                file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                file.Properties.ContentType = "text/html";
                file.SetProperties();
                Assert.IsTrue(file.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, file.Properties.ETag);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributes();
                Assert.AreEqual("no-transform", file2.Properties.CacheControl);
                Assert.AreEqual("attachment", file2.Properties.ContentDisposition);
                Assert.AreEqual("gzip", file2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", file2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", file2.Properties.ContentMD5);
                Assert.AreEqual("text/html", file2.Properties.ContentType);

                CloudFile file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream stream = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    file3.DownloadToStream(stream, null, options);
                }
                AssertAreEqual(file2.Properties, file3.Properties);

                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                CloudFile file4 = (CloudFile)rootDirectory.ListFilesAndDirectories().First();
                Assert.AreEqual(file2.Properties.Length, file4.Properties.Length);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSetPropertiesAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                    IAsyncResult result = file.BeginCreate(1024,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndCreate(result);
                    string eTag = file.Properties.ETag;
                    DateTimeOffset lastModified = file.Properties.LastModified.Value;

                    Thread.Sleep(1000);

                    file.Properties.CacheControl = "no-transform";
                    file.Properties.ContentDisposition = "attachment";
                    file.Properties.ContentEncoding = "gzip";
                    file.Properties.ContentLanguage = "tr,en";
                    file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    file.Properties.ContentType = "text/html";
                    result = file.BeginSetProperties(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndSetProperties(result);
                    Assert.IsTrue(file.Properties.LastModified > lastModified);
                    Assert.AreNotEqual(eTag, file.Properties.ETag);

                    CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual("no-transform", file2.Properties.CacheControl);
                    Assert.AreEqual("attachment", file2.Properties.ContentDisposition);
                    Assert.AreEqual("gzip", file2.Properties.ContentEncoding);
                    Assert.AreEqual("tr,en", file2.Properties.ContentLanguage);
                    Assert.AreEqual("MDAwMDAwMDA=", file2.Properties.ContentMD5);
                    Assert.AreEqual("text/html", file2.Properties.ContentType);

                    CloudFile file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                    using (MemoryStream stream = new MemoryStream())
                    {
                        FileRequestOptions options = new FileRequestOptions()
                        {
                            DisableContentMD5Validation = true,
                        };
                        result = file3.BeginDownloadToStream(stream, null, options, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file3.EndDownloadToStream(result);
                    }
                    AssertAreEqual(file2.Properties, file3.Properties);

                    CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                    result = rootDirectory.BeginListFilesAndDirectoriesSegmented(null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    FileResultSegment results = rootDirectory.EndListFilesAndDirectoriesSegmented(result);
                    CloudFile file4 = (CloudFile)results.Results.First();
                    Assert.AreEqual(file2.Properties.Length, file4.Properties.Length);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify setting the properties of a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSetPropertiesTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(1024).Wait();
                string eTag = file.Properties.ETag;
                DateTimeOffset lastModified = file.Properties.LastModified.Value;

                Thread.Sleep(1000);

                file.Properties.CacheControl = "no-transform";
                file.Properties.ContentEncoding = "gzip";
                file.Properties.ContentLanguage = "tr,en";
                file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                file.Properties.ContentType = "text/html";
                file.SetPropertiesAsync().Wait();
                Assert.IsTrue(file.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, file.Properties.ETag);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual("no-transform", file2.Properties.CacheControl);
                Assert.AreEqual("gzip", file2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", file2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", file2.Properties.ContentMD5);
                Assert.AreEqual("text/html", file2.Properties.ContentType);

                CloudFile file3 = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream stream = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    file3.DownloadToStreamAsync(stream, null, options, null).Wait();
                }
                AssertAreEqual(file2.Properties, file3.Properties);

                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                CloudFile file4 = (CloudFile)rootDirectory.ListFilesAndDirectoriesSegmentedAsync(null).Result.Results.First();
                Assert.AreEqual(file2.Properties.Length, file4.Properties.Length);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Verify that creating a file can also set its metadata")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileCreateWithMetadata()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Metadata["key1"] = "value1";
                file.Create(1024);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributes();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that a file's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSetMetadata()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributes();
                Assert.AreEqual(0, file2.Metadata.Count);

                file.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedException<StorageException>(
                    () => file.SetMetadata(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "";
                e = TestHelper.ExpectedException<StorageException>(
                    () => file.SetMetadata(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "value1";
                file.SetMetadata();

                file2.FetchAttributes();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);

                file.Metadata.Clear();
                file.SetMetadata();

                file2.FetchAttributes();
                Assert.AreEqual(0, file2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Verify that a file's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSetMetadataAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                    IAsyncResult result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual(0, file2.Metadata.Count);

                    file.Metadata["key1"] = null;
                    result = file.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    Exception e = TestHelper.ExpectedException<StorageException>(
                        () => file.EndSetMetadata(result),
                        "Metadata keys should have a non-null value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    file.Metadata["key1"] = "";
                    result = file.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    e = TestHelper.ExpectedException<StorageException>(
                        () => file.EndSetMetadata(result),
                        "Metadata keys should have a non-empty value");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                    file.Metadata["key1"] = "value1";
                    result = file.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndSetMetadata(result);

                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual(1, file2.Metadata.Count);
                    Assert.AreEqual("value1", file2.Metadata["key1"]);

                    file.Metadata.Clear();
                    result = file.BeginSetMetadata(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndSetMetadata(result);

                    result = file2.BeginFetchAttributes(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file2.EndFetchAttributes(result);
                    Assert.AreEqual(0, file2.Metadata.Count);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Verify that a file's metadata can be updated")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileSetMetadataTask()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(1024).Wait();

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file1");
                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, file2.Metadata.Count);

                file.Metadata["key1"] = null;
                StorageException e = TestHelper.ExpectedExceptionTask<StorageException>(
                    file.SetMetadataAsync(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "";
                e = TestHelper.ExpectedExceptionTask<StorageException>(
                    file.SetMetadataAsync(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                file.Metadata["key1"] = "value1";
                file.SetMetadataAsync().Wait();

                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(1, file2.Metadata.Count);
                Assert.AreEqual("value1", file2.Metadata["key1"]);

                file.Metadata.Clear();
                file.SetMetadataAsync().Wait();

                file2.FetchAttributesAsync().Wait();
                Assert.AreEqual(0, file2.Metadata.Count);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Upload/clear range in a file and then verify ranges")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListRanges()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    file.WriteRange(memoryStream, 512);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    file.WriteRange(memoryStream, 3 * 1024);
                }

                file.ClearRange(1024, 1024);
                file.ClearRange(0, 512);

                IEnumerable<FileRange> ranges = file.ListRanges();
                List<string> expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (FileRange range in ranges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                ranges = file.ListRanges(1024, 1024);
                Assert.AreEqual(0, ranges.Count());

                ranges = file.ListRanges(512, 3 * 1024);
                expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (FileRange range in ranges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                Exception e = TestHelper.ExpectedException<StorageException>(
                    () => file.ListRanges(1024),
                    "List Ranges with an offset but no count should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload/clear range in a file and then verify ranges")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListRangesAPM()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    file.WriteRange(memoryStream, 512);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    file.WriteRange(memoryStream, 3 * 1024);
                }

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = file.BeginClearRange(1024, 1024, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    file.EndClearRange(result);

                    result = file.BeginClearRange(0, 512, null, null, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    file.EndClearRange(result);

                    result = file.BeginListRanges(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    IEnumerable<FileRange> ranges = file.EndListRanges(result);
                    List<string> expectedFileRanges = new List<string>()
                    {
                        new FileRange(512, 1023).ToString(),
                        new FileRange(3 * 1024, 4 * 1024 - 1).ToString(),
                    };
                    foreach (FileRange range in ranges)
                    {
                        Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                    }
                    Assert.AreEqual(0, expectedFileRanges.Count);

                    result = file.BeginListRanges(1024,
                        1024,
                        null,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    ranges = file.EndListRanges(result);
                    expectedFileRanges = new List<string>();
                    foreach (FileRange range in ranges)
                    {
                        Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                    }
                    Assert.AreEqual(0, expectedFileRanges.Count);

                    result = file.BeginListRanges(512,
                        3 * 1024,
                        null,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    ranges = file.EndListRanges(result);
                    expectedFileRanges = new List<string>()
                    {
                        new FileRange(512, 1023).ToString(),
                        new FileRange(3 * 1024, 7 * 512 - 1).ToString(),
                    };
                    foreach (FileRange range in ranges)
                    {
                        Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                    }
                    Assert.AreEqual(0, expectedFileRanges.Count);

                    result = file.BeginListRanges(1024,
                        null,
                        null,
                        null,
                        null,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    StorageException e = TestHelper.ExpectedException<StorageException>(
                        () => file.EndListRanges(result),
                        "List Ranges with an offset but no count should fail");
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Upload/clear range in a file and then verify ranges")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileListRangesTask()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(4 * 1024).Wait();

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    file.WriteRangeAsync(memoryStream, 512, null).Wait();
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    file.WriteRangeAsync(memoryStream, 3 * 1024, null).Wait();
                }

                file.ClearRangeAsync(1024, 1024).Wait();
                file.ClearRangeAsync(0, 512).Wait();

                IEnumerable<FileRange> ranges = file.ListRangesAsync().Result;
                List<string> expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (FileRange range in ranges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                ranges = file.ListRangesAsync(1024, 1024, null, null, null).Result;
                Assert.AreEqual(0, ranges.Count());

                ranges = file.ListRangesAsync(512, 3 * 1024, null, null, null).Result;
                expectedFileRanges = new List<string>()
                {
                    new FileRange(512, 1023).ToString(),
                    new FileRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (FileRange range in ranges)
                {
                    Assert.IsTrue(expectedFileRanges.Remove(range.ToString()));
                }
                Assert.AreEqual(0, expectedFileRanges.Count);

                Exception e = TestHelper.ExpectedExceptionTask<StorageException>(
                    file.ListRangesAsync(1024, null, null, null, null),
                    "List Ranges with an offset but no count should fail");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Upload range to a file and then verify the contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileWriteRange()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(4 * 1024 * 1024);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => file.WriteRange(memoryStream, 0),
                        "Zero-length WriteRange should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        TestHelper.ExpectedException(
                            () => file.WriteRange(memoryStream, 512),
                            "Writing out-of-range range should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        file.WriteRange(memoryStream, 0, contentMD5);
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        TestHelper.ExpectedException(
                            () => file.WriteRange(memoryStream, 0, contentMD5),
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        file.WriteRange(memoryStream, 0);
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        file.WriteRange(memoryStream, 1024);
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream fileData = new MemoryStream())
                    {
                        file.DownloadToStream(fileData);
                        Assert.AreEqual(resultingData.Length, fileData.Length);

                        Assert.IsTrue(fileData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToStreamAPM()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = file.BeginUploadFromStream(originalFile,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        file.EndUploadFromStream(result);

                        using (MemoryStream downloadedFile = new MemoryStream())
                        {
                            OperationContext context = new OperationContext();
                            result = file.BeginDownloadRangeToStream(downloadedFile,
                                0, /* offset */
                                buffer.Length, /* Length */
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            file.EndDownloadRangeToStream(result);
                            TestHelper.AssertStreamsAreEqual(originalFile, downloadedFile);
                        }
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload range to a file and then verify the contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileWriteRangeAPM()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(4 * 1024 * 1024);

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                            () => file.BeginWriteRange(memoryStream, 0, null, null, null),
                            "Zero-length WriteRange should fail");
                    }

                    using (MemoryStream resultingData = new MemoryStream())
                    {
                        using (MemoryStream memoryStream = new MemoryStream(buffer))
                        {
                            result = file.BeginWriteRange(memoryStream, 512, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            TestHelper.ExpectedException(
                                () => file.EndWriteRange(result),
                                "Writing out-of-range range should fail",
                                HttpStatusCode.RequestedRangeNotSatisfiable,
                                "InvalidRange");

                            memoryStream.Seek(0, SeekOrigin.Begin);
                            result = file.BeginWriteRange(memoryStream, 0, contentMD5,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            file.EndWriteRange(result);
                            resultingData.Write(buffer, 0, buffer.Length);

                            int offset = buffer.Length - 1024;
                            memoryStream.Seek(offset, SeekOrigin.Begin);
                            result = file.BeginWriteRange(memoryStream, 0, contentMD5,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            TestHelper.ExpectedException(
                                () => file.EndWriteRange(result),
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                            memoryStream.Seek(offset, SeekOrigin.Begin);
                            result = file.BeginWriteRange(memoryStream, 0, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            file.EndWriteRange(result);
                            resultingData.Seek(0, SeekOrigin.Begin);
                            resultingData.Write(buffer, offset, buffer.Length - offset);

                            offset = buffer.Length - 2048;
                            memoryStream.Seek(offset, SeekOrigin.Begin);
                            result = file.BeginWriteRange(memoryStream, 1024, null,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            file.EndWriteRange(result);
                            resultingData.Seek(1024, SeekOrigin.Begin);
                            resultingData.Write(buffer, offset, buffer.Length - offset);
                        }

                        using (MemoryStream fileData = new MemoryStream())
                        {
                            file.DownloadToStream(fileData);
                            Assert.AreEqual(resultingData.Length, fileData.Length);

                            Assert.IsTrue(fileData.ToArray().SequenceEqual(resultingData.ToArray()));
                        }
                    }
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileDownloadToStreamTask()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream originalFile = new MemoryStream(buffer))
                {
                    file.UploadFromStreamAsync(originalFile).Wait();

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        OperationContext context = new OperationContext();
                        file.DownloadRangeToStreamAsync(downloadedFile, 0, buffer.Length).Wait();
                        TestHelper.AssertStreamsAreEqual(originalFile, downloadedFile);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload range to a file and then verify the contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileWriteRangeTask()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);
            MD5 md5 = MD5.Create();
            string contentMD5 = Convert.ToBase64String(md5.ComputeHash(buffer));

            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.CreateAsync().Wait();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(4 * 1024 * 1024).Wait();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                        () => file.WriteRangeAsync(memoryStream, 0, null),
                        "Zero-length WriteRange should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        TestHelper.ExpectedExceptionTask(
                            file.WriteRangeAsync(memoryStream, 512, null),
                            "Writing out-of-range range should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        file.WriteRangeAsync(memoryStream, 0, contentMD5).Wait();
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        TestHelper.ExpectedExceptionTask(
                            file.WriteRangeAsync(memoryStream, 0, contentMD5),
                            "Invalid MD5 should fail with mismatch",
                            HttpStatusCode.BadRequest,
                            "Md5Mismatch");

                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        file.WriteRangeAsync(memoryStream, 0, null).Wait();
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        file.WriteRangeAsync(memoryStream, 1024, null).Wait();
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream fileData = new MemoryStream())
                    {
                        file.DownloadToStreamAsync(fileData).Wait();
                        Assert.AreEqual(resultingData.Length, fileData.Length);

                        Assert.IsTrue(fileData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
#endif

        /*
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamWithAccessCondition()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, false, true);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, false, true),
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, false, true);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                file.Create(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, false, true),
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, false, true);
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamAPMWithAccessCondition()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("\"*\"");
                this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, true, true);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, true, true),
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, true, true);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                file.Create(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, true, true),
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                this.CloudFileUploadFromStream(share, 6 * 512, null, accessCondition, 0, true, true);
            }
            finally
            {
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamWithAccessConditionTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                this.CloudFileUploadFromStreamTask(share, 6 * 512, null, accessCondition, 0, true);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.CreateAsync(1024).Wait();
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudFileUploadFromStreamTask(share, 6 * 512, null, accessCondition, 0, true),
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                this.CloudFileUploadFromStreamTask(share, 6 * 512, null, accessCondition, 0, true);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                file.CreateAsync(1024).Wait();
                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                TestHelper.ExpectedException(
                    () => this.CloudFileUploadFromStreamTask(share, 6 * 512, null, accessCondition, 0, true),
                    "Uploading a file on top of an existing file should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                this.CloudFileUploadFromStreamTask(share, 6 * 512, null, accessCondition, 0, true);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
#endif
        */

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStream()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                this.CloudFileUploadFromStream(share, 6 * 512, null, null, 0, false, true);
                this.CloudFileUploadFromStream(share, 6 * 512, null, null, 1024, false, true);
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                this.CloudFileUploadFromStream(share, 6 * 512, null, null, 0, true, true);
                this.CloudFileUploadFromStream(share, 6 * 512, null, null, 1024, true, true);
            }
            finally
            {
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            try
            {
                this.CloudFileUploadFromStreamTask(share, 6 * 512, null, null, 0, true);
                this.CloudFileUploadFromStreamTask(share, 6 * 512, null, null, 1024, true);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamLength()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                // Upload half of the stream
                this.CloudFileUploadFromStream(share, 6 * 512, 3 * 512, null, 0, false, true);
                this.CloudFileUploadFromStream(share, 6 * 512, 3 * 512, null, 1024, false, true);

                // Upload full stream
                this.CloudFileUploadFromStream(share, 6 * 512, 6 * 512, null, 0, false, true);
                this.CloudFileUploadFromStream(share, 6 * 512, 4 * 512, null, 1024, false, true);

                // Exclude last range
                this.CloudFileUploadFromStream(share, 6 * 512, 5 * 512, null, 0, false, true);
                this.CloudFileUploadFromStream(share, 6 * 512, 3 * 512, null, 1024, false, true);
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamLengthAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                // Upload half of the stream
                this.CloudFileUploadFromStream(share, 6 * 512, 3 * 512, null, 0, true, true);
                this.CloudFileUploadFromStream(share, 6 * 512, 3 * 512, null, 1024, true, true);

                // Upload full stream
                this.CloudFileUploadFromStream(share, 6 * 512, 6 * 512, null, 0, true, true);
                this.CloudFileUploadFromStream(share, 6 * 512, 4 * 512, null, 1024, true, true);

                // Exclude last range
                this.CloudFileUploadFromStream(share, 6 * 512, 5 * 512, null, 0, true, true);
                this.CloudFileUploadFromStream(share, 6 * 512, 3 * 512, null, 1024, true, true);
            }
            finally
            {
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamLengthTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            try
            {
                // Upload half of the stream
                this.CloudFileUploadFromStreamTask(share, 6 * 512, 3 * 512, null, 0, true);
                this.CloudFileUploadFromStreamTask(share, 6 * 512, 3 * 512, null, 1024, true);

                // Upload full stream
                this.CloudFileUploadFromStreamTask(share, 6 * 512, 6 * 512, null, 0, true);
                this.CloudFileUploadFromStreamTask(share, 6 * 512, 4 * 512, null, 1024, true);

                // Exclude last range
                this.CloudFileUploadFromStreamTask(share, 6 * 512, 5 * 512, null, 0, true);
                this.CloudFileUploadFromStreamTask(share, 6 * 512, 3 * 512, null, 1024, true);
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
#endif

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamLengthInvalid()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudFileUploadFromStream(share, 3 * 512, 3 * 512 + 1, null, 0, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudFileUploadFromStream(share, 3 * 512, 3 * 512 + 1025, null, 1024, false, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                share.Delete();
            }
        }

        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamLengthInvalidAPM()
        {
            CloudFileShare share = GetRandomShareReference();
            share.Create();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudFileUploadFromStream(share, 3 * 512, 3 * 512 + 1, null, 0, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudFileUploadFromStream(share, 3 * 512, 3 * 512 + 1025, null, 1024, true, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                share.Delete();
            }
        }

#if TASK
        [TestMethod]
        [Description("Single put file and get file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadFromStreamLengthInvalidTask()
        {
            CloudFileShare share = GetRandomShareReference();
            share.CreateAsync().Wait();
            try
            {
                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudFileUploadFromStreamTask(share, 3 * 512, 3 * 512 + 1, null, 0, false),
                    "The given stream does not contain the requested number of bytes from its given position.");

                TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                    () => this.CloudFileUploadFromStreamTask(share, 3 * 512, 3 * 512 + 1025, null, 1024, false),
                    "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                share.DeleteAsync().Wait();
            }
        }
#endif

        private void CloudFileUploadFromStream(CloudFileShare share, int size, long? copyLength, AccessCondition accessCondition, int startOffset, bool isAsync, bool testMd5)
        {
            byte[] buffer = GetRandomBuffer(size);

            MD5 hasher = MD5.Create();
            string md5 = string.Empty;
            if (testMd5)
            {
                md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
            }

            CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
            file.StreamWriteSizeInBytes = 512;

            using (MemoryStream originalFile = new MemoryStream())
            {
                originalFile.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };
                    if (isAsync)
                    {
                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {
                            if (copyLength.HasValue)
                            {
                                ICancellableAsyncResult result = file.BeginUploadFromStream(
                                    sourceStream, copyLength.Value, accessCondition, options, null, ar => waitHandle.Set(), null);
                                waitHandle.WaitOne();
                                file.EndUploadFromStream(result);
                            }
                            else
                            {
                                ICancellableAsyncResult result = file.BeginUploadFromStream(
                                    sourceStream, accessCondition, options, null, ar => waitHandle.Set(), null);
                                waitHandle.WaitOne();
                                file.EndUploadFromStream(result);
                            }
                        }
                    }
                    else
                    {
                        if (copyLength.HasValue)
                        {
                            file.UploadFromStream(sourceStream, copyLength.Value, accessCondition, options);
                        }
                        else
                        {
                            file.UploadFromStream(sourceStream, accessCondition, options);
                        }
                    }
                }

                file.FetchAttributes();
                if (testMd5)
                {
                    Assert.AreEqual(md5, file.Properties.ContentMD5);
                }

                using (MemoryStream downloadedFile = new MemoryStream())
                {
                    if (isAsync)
                    {
                        using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                        {
                            ICancellableAsyncResult result = file.BeginDownloadToStream(downloadedFile,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            file.EndDownloadToStream(result);
                        }
                    }
                    else
                    {
                        file.DownloadToStream(downloadedFile);
                    }

                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalFile,
                        downloadedFile,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalFile.Length);
                }
            }
        }

#if TASK
        private void CloudFileUploadFromStreamTask(CloudFileShare share, int size, long? copyLength, AccessCondition accessCondition, int startOffset, bool testMd5)
        {
            try
            {
                byte[] buffer = GetRandomBuffer(size);

                MD5 hasher = MD5.Create();
                string md5 = string.Empty;
                if (testMd5)
                {
                    md5 = Convert.ToBase64String(hasher.ComputeHash(buffer, startOffset, copyLength.HasValue ? (int)copyLength : buffer.Length - startOffset));
                }

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 512;

                using (MemoryStream originalFile = new MemoryStream())
                {
                    originalFile.Write(buffer, startOffset, buffer.Length - startOffset);

                    using (MemoryStream sourceStream = new MemoryStream(buffer))
                    {
                        sourceStream.Seek(startOffset, SeekOrigin.Begin);
                        FileRequestOptions options = new FileRequestOptions()
                        {
                            StoreFileContentMD5 = true,
                        };

                        if (copyLength.HasValue)
                        {
                            file.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, options, null).Wait();
                        }
                        else
                        {
                            file.UploadFromStreamAsync(sourceStream, accessCondition, options, null).Wait();
                        }
                    }

                    file.FetchAttributesAsync().Wait();
                    if (testMd5)
                    {
                        Assert.AreEqual(md5, file.Properties.ContentMD5);
                    }

                    using (MemoryStream downloadedFile = new MemoryStream())
                    {
                        file.DownloadToStreamAsync(downloadedFile).Wait();

                        TestHelper.AssertStreamsAreEqualAtIndex(
                            originalFile,
                            downloadedFile,
                            0,
                            0,
                            copyLength.HasValue ? (int)copyLength : (int)originalFile.Length);
                    }
                }
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
#endif

        /*
        [TestMethod]
        [Description("Test conditional access on a file")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileConditionalAccess()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.Create(1024);
                file.FetchAttributes();

                string currentETag = file.Properties.ETag;
                DateTimeOffset currentModifiedTime = file.Properties.LastModified.Value;

                // ETag conditional tests
                file.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                file.SetMetadata(AccessCondition.GenerateIfMatchCondition(currentETag), null);

                file.FetchAttributes();
                string newETag = file.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                file.Metadata["ETagConditionalName"] = "ETagConditionalValue2";

                TestHelper.ExpectedException(
                    () => file.SetMetadata(AccessCondition.GenerateIfNoneMatchCondition(newETag), null),
                    "If none match on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                string invalidETag = "\"0x10101010\"";
                TestHelper.ExpectedException(
                    () => file.SetMetadata(AccessCondition.GenerateIfMatchCondition(invalidETag), null),
                    "Invalid ETag on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                currentETag = file.Properties.ETag;
                file.SetMetadata(AccessCondition.GenerateIfNoneMatchCondition(invalidETag), null);

                file.FetchAttributes();
                newETag = file.Properties.ETag;

                // LastModifiedTime tests
                currentModifiedTime = file.Properties.LastModified.Value;

                file.Metadata["DateConditionalName"] = "DateConditionalValue";

                TestHelper.ExpectedException(
                    () => file.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null),
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                file.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                file.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                file.SetMetadata(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null);

                currentModifiedTime = file.Properties.LastModified.Value;

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                TestHelper.ExpectedException(
                    () => file.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                TestHelper.ExpectedException(
                    () => file.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                TestHelper.ExpectedException(
                    () => file.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null),
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                file.Metadata["DateConditionalName"] = "DateConditionalValue2";

                currentETag = file.Properties.ETag;
                file.SetMetadata(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null);

                file.FetchAttributes();
                newETag = file.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
        */

        [TestMethod]
        [Description("Test different size files")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileAlignment()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");

                file.Create(511);
                file.Create(512);
                file.Create(513);

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(511);
                    file.WriteRange(stream, 0);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    file.WriteRange(stream, 0);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(513);
                    file.WriteRange(stream, 0);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload and download null/empty data")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileUploadDownloadNoData()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                TestHelper.ExpectedException<ArgumentNullException>(
                    () => file.UploadFromStream(null),
                    "Uploading from a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    file.UploadFromStream(stream);
                }

                TestHelper.ExpectedException<ArgumentNullException>(
                    () => file.DownloadToStream(null),
                    "Downloading to a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    file.DownloadToStream(stream);
                    Assert.AreEqual(0, stream.Length);
                }
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Use IASyncResult's WaitHandle")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void IAsyncWaitHandleTest()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                share.Create();

                IAsyncResult result;

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                result = file.BeginCreate(0, null, null);
                result.AsyncWaitHandle.WaitOne();
                file.EndCreate(result);

                result = file.BeginExists(null, null);
                result.AsyncWaitHandle.WaitOne();
                Assert.IsTrue(file.EndExists(result));

                result = file.BeginDelete(null, null);
                result.AsyncWaitHandle.WaitOne();
                file.EndDelete(result);
            }
            finally
            {
                share.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Try operations with an invalid Sas")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudFileInvalidSas()
        {
            // Sas token creds.
            string token = "?sp=abcde&sig=1";
            StorageCredentials creds = new StorageCredentials(token);
            Assert.IsTrue(creds.IsSAS);

            // Client with shared key access.
            CloudFileClient fileClient = GenerateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(GetRandomShareName());
            try
            {
                share.Create();

                SharedAccessFilePolicy policy = new SharedAccessFilePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                };
                string sasToken = share.GetSharedAccessSignature(policy);

                string fileUri = share.Uri.AbsoluteUri + "/file1" + sasToken;
                TestHelper.ExpectedException<ArgumentException>(
                    () => new CloudFile(new Uri(fileUri), share.ServiceClient.Credentials),
                    "Try to use SAS creds in Uri on a shared key client");

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.UploadFromStream(new MemoryStream(GetRandomBuffer(10)));
            }
            finally
            {
                share.DeleteIfExists();
            }
        }
    }
}
