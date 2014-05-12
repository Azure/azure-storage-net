// -----------------------------------------------------------------------------------------
// <copyright file="FileTestBase.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    public partial class FileTestBase : TestBase
    {
        public static List<string> CreateFiles(CloudFileShare share, int count)
        {
            string name;
            List<string> files = new List<string>();
            for (int i = 0; i < count; i++)
            {
                name = "ff" + Guid.NewGuid().ToString();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(name);
                file.Create(0);
                files.Add(name);
            }
            return files;
        }

#if TASK
        public static List<string> CreateFilesTask(CloudFileShare share, int count)
        {
            string name;
            List<string> files = new List<string>();
            for (int i = 0; i < count; i++)
            {
                name = "ff" + Guid.NewGuid().ToString();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(name);
                file.CreateAsync(0).Wait();
                files.Add(name);
            }
            return files;
        }
#endif
    }
}
