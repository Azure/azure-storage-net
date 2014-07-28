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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    public partial class FileTestBase : TestBase
    {
        public static async Task<List<string>> CreateFilesAsync(CloudFileShare share, int count)
        {
            string name;
            List<string> files = new List<string>();
            for (int i = 0; i < count; i++)
            {
                name = "ff" + Guid.NewGuid().ToString();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(name);
                await file.CreateAsync(0);
                files.Add(name);
            }

            return files;
        }

        public static async Task<IEnumerable<IListFileItem>> ListFilesAndDirectoriesAsync(CloudFileDirectory directory, int? maxResults, FileRequestOptions options, OperationContext operationContext)
        {
            List<IListFileItem> results = new List<IListFileItem>();
            FileContinuationToken token = null;
            do
            {
                FileResultSegment resultSegment = await directory.ListFilesAndDirectoriesSegmentedAsync(maxResults, token, options, operationContext);
                results.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;
            }
            while (token != null);
            return results;
        }
    }
}
