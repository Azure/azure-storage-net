// -----------------------------------------------------------------------------------------
// <copyright file="FileTestBase.Common.cs" company="Microsoft">
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

#if WINDOWS_DESKTOP
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace Microsoft.WindowsAzure.Storage.File
{
    public partial class FileTestBase : TestBase
    {
        public static string GetRandomShareName()
        {
            return string.Concat("testc", Guid.NewGuid().ToString("N"));
        }

        public static CloudFileShare GetRandomShareReference()
        {
            CloudFileClient fileClient = GenerateCloudFileClient();

            string name = GetRandomShareName();
            CloudFileShare share = fileClient.GetShareReference(name);

            return share;
        }

        public static void AssertAreEqual(CloudFile expected, CloudFile actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
            }
            else
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Uri, actual.Uri);
                Assert.AreEqual(expected.StorageUri, actual.StorageUri);
                AssertAreEqual(expected.Properties, actual.Properties);
            }
        }

        public static void AssertAreEqual(FileProperties prop1, FileProperties prop2)
        {
            if (prop1 == null)
            {
                Assert.IsNull(prop2);
            }
            else
            {
                Assert.IsNotNull(prop2);
                Assert.AreEqual(prop1.CacheControl, prop2.CacheControl);
                Assert.AreEqual(prop1.ContentEncoding, prop2.ContentEncoding);
                Assert.AreEqual(prop1.ContentLanguage, prop2.ContentLanguage);
                Assert.AreEqual(prop1.ContentMD5, prop2.ContentMD5);
                Assert.AreEqual(prop1.ContentType, prop2.ContentType);
                Assert.AreEqual(prop1.ETag, prop2.ETag);
                Assert.AreEqual(prop1.LastModified, prop2.LastModified);
                Assert.AreEqual(prop1.Length, prop2.Length);
            }
        }
    }
}
