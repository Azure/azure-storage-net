// -----------------------------------------------------------------------------------------
// <copyright file="BlobTestBase.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    public partial class BlobTestBase : TestBase
    {
        public static async Task WaitForCopyAsync(CloudBlob blob)
        {
            bool copyInProgress = true;
            while (copyInProgress)
            {
                await Task.Delay(1000);
                await blob.FetchAttributesAsync();
                copyInProgress = (blob.CopyState.Status == CopyStatus.Pending);
            }
        }

        public static async Task<List<string>> CreateBlobsAsync(CloudBlobContainer container, int count, BlobType type)
        {
            string name;
            List<string> blobs = new List<string>();
            for (int i = 0; i < count; i++)
            {
                switch (type)
                {
                    case BlobType.BlockBlob:
                        name = "bb" + Guid.NewGuid().ToString();
                        CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
                        await blockBlob.PutBlockListAsync(new string[] { });
                        blobs.Add(name);
                        break;

                    case BlobType.PageBlob:
                        name = "pb" + Guid.NewGuid().ToString();
                        CloudPageBlob pageBlob = container.GetPageBlobReference(name);
                        await pageBlob.CreateAsync(0);
                        blobs.Add(name);
                        break;

                    case BlobType.AppendBlob:
                        name = "ab" + Guid.NewGuid().ToString();
                        CloudAppendBlob appendBlob = container.GetAppendBlobReference(name);
                        await appendBlob.CreateOrReplaceAsync();
                        blobs.Add(name);
                        break;
                }
            }
            return blobs;
        }

        public static async Task UploadTextAsync(CloudBlob blob, string text, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                await stream.WriteAsync(textAsBytes, 0, textAsBytes.Length);
                if (blob.BlobType == BlobType.PageBlob)
                {
                    int lastPageSize = (int)(stream.Length % 512);
                    if (lastPageSize != 0)
                    {
                        byte[] padding = new byte[512 - lastPageSize];
                        await stream.WriteAsync(padding, 0, padding.Length);
                    }
                }

                stream.Seek(0, SeekOrigin.Begin);
                blob.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

                if (blob.BlobType == BlobType.AppendBlob)
                {
                    CloudAppendBlob blob1 = blob as CloudAppendBlob;
                    await blob1.CreateOrReplaceAsync();
                    await blob1.AppendBlockAsync(stream, null);
                }
                else if (blob.BlobType == BlobType.PageBlob)
                {
                    CloudPageBlob pageBlob = blob as CloudPageBlob;
                    await pageBlob.UploadFromStreamAsync(stream, accessCondition, options, operationContext);
                }
                else
                {
                    CloudBlockBlob blockBlob = blob as CloudBlockBlob;
                    await blockBlob.UploadFromStreamAsync(stream, accessCondition, options, operationContext);
                }
            }
        }

        public static async Task<string> DownloadTextAsync(CloudBlob blob, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream, accessCondition, options, operationContext);
                byte[] buffer = stream.ToArray();
                return encoding.GetString(buffer, 0, buffer.Length);
            }
        }
    }
}
