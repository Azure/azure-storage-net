// -----------------------------------------------------------------------------------------
// <copyright file="HttpContentFactory.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using System;
    using System.IO;
    using System.Net.Http;

    internal static class HttpContentFactory
    {
        public static HttpContent BuildContentFromStream<T>(Stream stream, long offset, long? length, Checksum checksum, RESTCommand<T> cmd, OperationContext operationContext)
        {
            stream.Seek(offset, SeekOrigin.Begin);

#if !(WINDOWS_RT || NETCORE)
            stream = stream.WrapWithByteCountingStream(cmd.CurrentResult, true);
#endif
            HttpContent retContent = new StreamContent(new NonCloseableStream(stream));
            retContent.Headers.ContentLength = length;
            if (checksum?.MD5 != null)
            {
                retContent.Headers.ContentMD5 = Convert.FromBase64String(checksum.MD5);
            }
            if (checksum?.CRC64 != null)
            {
                retContent.Headers.Add(Constants.HeaderConstants.ContentCrc64Header, checksum.CRC64);
            }

            return retContent;
        }
    }
}
