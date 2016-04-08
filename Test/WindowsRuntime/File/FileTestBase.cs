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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.WindowsAzure.Storage.File
{
    public partial class FileTestBase : TestBase
    {
        public static async Task WaitForCopyAsync(CloudFile file)
        {
            bool copyInProgress = true;
            while (copyInProgress)
            {
                await Task.Delay(1000);
                await file.FetchAttributesAsync();
                copyInProgress = (file.CopyState.Status == CopyStatus.Pending);
            }
        }

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

        public static async Task UploadTextAsync(CloudFile file, string text, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                file.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
                await file.UploadFromStreamAsync(stream, accessCondition, options, operationContext);
            }
        }

        public static async Task<string> DownloadTextAsync(CloudFile file, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                await file.DownloadToStreamAsync(stream, accessCondition, options, operationContext);
                return encoding.GetString(stream.ToArray(), 0, (int)stream.Length);
            }
        }
    }
}
