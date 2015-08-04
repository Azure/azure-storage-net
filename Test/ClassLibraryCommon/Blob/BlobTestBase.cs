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
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    public partial class BlobTestBase : TestBase
    {
        public static void WaitForCopy(CloudBlob blob)
        {
            bool copyInProgress = true;
            while (copyInProgress)
            {
                Thread.Sleep(1000);
                blob.FetchAttributes();
                copyInProgress = (blob.CopyState.Status == CopyStatus.Pending);
            }
        }

#if TASK
        public static void WaitForCopyTask(CloudBlob blob)
        {
            bool copyInProgress = true;
            while (copyInProgress)
            {
                Thread.Sleep(1000);
                blob.FetchAttributesAsync().Wait();
                copyInProgress = (blob.CopyState.Status == CopyStatus.Pending);
            }
        }
#endif

        public static List<string> CreateBlobs(CloudBlobContainer container, int count, BlobType type)
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
                        blockBlob.PutBlockList(new string[] { });
                        blobs.Add(name);
                        break;

                    case BlobType.PageBlob:
                        name = "pb" + Guid.NewGuid().ToString();
                        CloudPageBlob pageBlob = container.GetPageBlobReference(name);
                        pageBlob.Create(0);
                        blobs.Add(name);
                        break;

                    case BlobType.AppendBlob:
                        name = "ab" + Guid.NewGuid().ToString();
                        CloudAppendBlob appendBlob = container.GetAppendBlobReference(name);
                        appendBlob.CreateOrReplace();
                        blobs.Add(name);
                        break;
                }
            }
            return blobs;
        }

#if TASK
        public static List<string> CreateBlobsTask(CloudBlobContainer container, int count, BlobType type)
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
                        blockBlob.PutBlockListAsync(new string[] { }).Wait();
                        blobs.Add(name);
                        break;

                    case BlobType.PageBlob:
                        name = "pb" + Guid.NewGuid().ToString();
                        CloudPageBlob pageBlob = container.GetPageBlobReference(name);
                        pageBlob.CreateAsync(0).Wait();
                        blobs.Add(name);
                        break;

                    case BlobType.AppendBlob:
                        name = "ab" + Guid.NewGuid().ToString();
                        CloudAppendBlob appendBlob = container.GetAppendBlobReference(name);
                        appendBlob.CreateOrReplaceAsync().Wait();
                        blobs.Add(name);
                        break;
                }
            }
            return blobs;
        }
#endif

        public static void UploadText(CloudBlob blob, string text, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);
                if (blob.BlobType == BlobType.PageBlob)
                {
                    int lastPageSize = (int)(stream.Length % 512);
                    if (lastPageSize != 0)
                    {
                        byte[] padding = new byte[512 - lastPageSize];
                        stream.Write(padding, 0, padding.Length);
                    }
                }
                

                stream.Seek(0, SeekOrigin.Begin);
                blob.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

                if (blob.BlobType == BlobType.AppendBlob)
                {
                    CloudAppendBlob blob1 = blob as CloudAppendBlob;
                    blob1.CreateOrReplace();
                    blob1.AppendBlock(stream, null);
                }
                else if (blob.BlobType == BlobType.PageBlob)
                {
                    CloudPageBlob pageBlob = blob as CloudPageBlob;
                    pageBlob.UploadFromStream(stream, accessCondition, options, operationContext);
                }
                else
                {
                    CloudBlockBlob blockBlob = blob as CloudBlockBlob;
                    blockBlob.UploadFromStream(stream, accessCondition, options, operationContext);
                }
            }
        }

        public static void UploadTextAPM(CloudBlob blob, string text, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);
                if (blob.BlobType == BlobType.PageBlob)
                {
                    int lastPageSize = (int)(stream.Length % 512);
                    if (lastPageSize != 0)
                    {
                        byte[] padding = new byte[512 - lastPageSize];
                        stream.Write(padding, 0, padding.Length);
                    }
                }

                stream.Seek(0, SeekOrigin.Begin);
                blob.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    if (blob.BlobType == BlobType.AppendBlob)
                    {
                        CloudAppendBlob blob1 = blob as CloudAppendBlob;

                        IAsyncResult result = blob1.BeginCreateOrReplace(
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob1.EndCreateOrReplace(result);

                        result = blob1.BeginAppendBlock(stream, null,
                            ar => waitHandle.Set(),
                            null);
                        waitHandle.WaitOne();
                        blob1.EndAppendBlock(result);
                    }
                    else if (blob.BlobType == BlobType.PageBlob)
                    {
                        CloudPageBlob pageBlob = blob as CloudPageBlob;
                        IAsyncResult result = pageBlob.BeginUploadFromStream(stream, accessCondition, options, operationContext,
                                               ar => waitHandle.Set(),
                                               null);
                        waitHandle.WaitOne();
                        pageBlob.EndUploadFromStream(result);
                    }
                    else
                    {
                        CloudBlockBlob blockBlob = blob as CloudBlockBlob;
                        IAsyncResult result = blockBlob.BeginUploadFromStream(stream, accessCondition, options, operationContext,
                                                ar => waitHandle.Set(),
                                                null);
                        waitHandle.WaitOne();
                        blockBlob.EndUploadFromStream(result);
                    }
                }
            }
        }

#if TASK
        public static void UploadTextTask(CloudBlob blob, string text, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);
                if (blob.BlobType == BlobType.PageBlob)
                {
                    int lastPageSize = (int)(stream.Length % 512);
                    if (lastPageSize != 0)
                    {
                        byte[] padding = new byte[512 - lastPageSize];
                        stream.Write(padding, 0, padding.Length);
                    }
                }

                stream.Seek(0, SeekOrigin.Begin);
                blob.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

                try
                {
                    if (blob.BlobType == BlobType.AppendBlob)
                    {
                        CloudAppendBlob blob1 = blob as CloudAppendBlob;
                        blob1.CreateOrReplaceAsync().Wait();
                        blob1.AppendBlock(stream, null);
                    }
                    else if (blob.BlobType == BlobType.PageBlob)
                    {
                        CloudPageBlob pageBlob = blob as CloudPageBlob;
                        pageBlob.UploadFromStreamAsync(stream, accessCondition, options, operationContext).Wait();
                    }
                    else
                    {
                        CloudBlockBlob blockBlob = blob as CloudBlockBlob;
                        blockBlob.UploadFromStreamAsync(stream, accessCondition, options, operationContext).Wait();
                    }
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    throw;
                }
            }
        }
#endif

        public static string DownloadText(CloudBlob blob, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                blob.DownloadToStream(stream, accessCondition, options, operationContext);
                return encoding.GetString(stream.ToArray());
            }
        }

        public static string DownloadTextAPM(CloudBlob blob, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = blob.BeginDownloadToStream(stream, accessCondition, options, operationContext, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndDownloadToStream(result);
                    return encoding.GetString(stream.ToArray());
                }
            }
        }

#if TASK
        public static string DownloadTextTask(CloudBlob blob, Encoding encoding, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                try
                {
                    blob.DownloadToStreamAsync(stream, accessCondition, options, operationContext).Wait();
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    throw;
                }
                return encoding.GetString(stream.ToArray());
            }
        }
#endif
    }
}
