// -----------------------------------------------------------------------------------------
// <copyright file="FileMD5FlagsTest.cs" company="Microsoft">
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
using System.IO;
using System.Net;
using System.Threading.Tasks;

#if !NETCORE
using Windows.Storage.Streams;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileMD5FlagsTest : FileTestBase
    {
        [TestMethod]
        /// [Description("Test StoreFileContentMD5 flag with UploadFromStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileStoreContentMD5TestAsync()
        {
            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                StoreFileContentMD5 = false,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                StoreFileContentMD5 = true,
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file4");
                using (Stream stream = new MemoryStream())
                {
                    await file.UploadFromStreamAsync(stream, null, optionsWithMD5, null);
                }
                await file.FetchAttributesAsync();
                Assert.IsNotNull(file.Properties.ContentMD5);

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                using (Stream stream = new MemoryStream())
                {
                    await file.UploadFromStreamAsync(stream, null, optionsWithNoMD5, null);
                }
                await file.FetchAttributesAsync();
                Assert.IsNull(file.Properties.ContentMD5);

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                using (Stream stream = new MemoryStream())
                {
                    await file.UploadFromStreamAsync(stream);
                }
                await file.FetchAttributesAsync();
                Assert.IsNull(file.Properties.ContentMD5);
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        /// [Description("Test DisableContentMD5Validation flag with DownloadToStream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileDisableContentMD5ValidationTestAsync()
        {
            byte[] buffer = new byte[1024];
            Random random = new Random();
            random.NextBytes(buffer);

            FileRequestOptions optionsWithNoMD5 = new FileRequestOptions()
            {
                DisableContentMD5Validation = true,
                StoreFileContentMD5 = true,
            };
            FileRequestOptions optionsWithMD5 = new FileRequestOptions()
            {
                DisableContentMD5Validation = false,
                StoreFileContentMD5 = true,
            };

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                using (Stream stream = new MemoryStream(buffer))
                {
                    await file.UploadFromStreamAsync(stream, null, optionsWithMD5, null);
                }

                using (Stream stream = new MemoryStream())
                {
                    await file.DownloadToStreamAsync(stream, null, optionsWithMD5, null);
                    await file.DownloadToStreamAsync(stream, null, optionsWithNoMD5, null);

                    using (var fileStream = await file.OpenReadAsync(null, optionsWithMD5, null))
                    {
                        Stream fileStreamForRead = fileStream;
                        int read;
                        do
                        {
                            read = await fileStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    using (var fileStream = await file.OpenReadAsync(null, optionsWithNoMD5, null))
                    {
                        Stream fileStreamForRead = fileStream;
                        int read;
                        do
                        {
                            read = await fileStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }

                    file.Properties.ContentMD5 = "MDAwMDAwMDA=";
                    await file.SetPropertiesAsync();

                    OperationContext opContext = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await file.DownloadToStreamAsync(stream, null, optionsWithMD5, opContext),
                        opContext,
                        "Downloading a file with invalid MD5 should fail",
                        HttpStatusCode.OK);
                    await file.DownloadToStreamAsync(stream, null, optionsWithNoMD5, null);

                    using (var fileStream = await file.OpenReadAsync(null, optionsWithMD5, null))
                    {
                        Stream fileStreamForRead = fileStream;
                        TestHelper.ExpectedException<IOException>(
                            () =>
                            {
                                int read;
                                do
                                {
                                    read = fileStreamForRead.Read(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            },
                            "Downloading a file with invalid MD5 should fail");
                    }

                    using (var fileStream = await file.OpenReadAsync(null, optionsWithNoMD5, null))
                    {
                        Stream fileStreamForRead = fileStream;
                        int read;
                        do
                        {
                            read = await fileStreamForRead.ReadAsync(buffer, 0, buffer.Length);
                        }
                        while (read > 0);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
