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

using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.File
{
    public partial class FileTestBase : TestBase
    {
        public static void WaitForCopy(CloudFile file)
        {
            bool copyInProgress = true;
            while (copyInProgress)
            {
                Thread.Sleep(1000);
                file.FetchAttributes();
                copyInProgress = (file.CopyState.Status == CopyStatus.Pending);
            }
        }

#if TASK
        public static void WaitForCopyTask(CloudFile file)
        {
            bool copyInProgress = true;
            while (copyInProgress)
            {
                Thread.Sleep(1000);
                file.FetchAttributesAsync().Wait();
                copyInProgress = (file.CopyState.Status == CopyStatus.Pending);
            }
        }
#endif

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

        public static void UploadText(CloudFile file, string text, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                file.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
                file.UploadFromStream(stream, accessCondition, options, operationContext);
            }
        }

        public static void UploadTextAPM(CloudFile file, string text, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);

                stream.Seek(0, SeekOrigin.Begin);
                file.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = file.BeginUploadFromStream(stream, accessCondition, options, operationContext,
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                    file.EndUploadFromStream(result);
                }
            }
        }

#if TASK
        public static void UploadTextTask(CloudFile file, string text, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(textAsBytes, 0, textAsBytes.Length);

                stream.Seek(0, SeekOrigin.Begin);
                file.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
                try
                {
                    file.UploadFromStreamAsync(stream, accessCondition, options, operationContext).Wait();
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

        public static string DownloadText(CloudFile file, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                file.DownloadToStream(stream, accessCondition, options, operationContext);
                return encoding.GetString(stream.ToArray());
            }
        }

        public static string DownloadTextAPM(CloudFile file, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = file.BeginDownloadToStream(stream, accessCondition, options, operationContext, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    file.EndDownloadToStream(result);
                    return encoding.GetString(stream.ToArray());
                }
            }
        }

#if TASK
        public static string DownloadTextTask(CloudFile file, Encoding encoding, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                try
                {
                    file.DownloadToStreamAsync(stream, accessCondition, options, operationContext).Wait();
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
