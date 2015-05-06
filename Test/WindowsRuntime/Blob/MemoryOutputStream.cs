﻿// -----------------------------------------------------------------------------------------
// <copyright file="MemoryOutputStream.cs" company="Microsoft">
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

using System.IO;
#if WINDOWS_RT
using Windows.Foundation;
using Windows.Storage.Streams;
#endif

namespace Microsoft.WindowsAzure.Storage.Blob
{
#if ASPNET_K || PORTABLE
    internal sealed class MemoryOutputStream : MemoryStream
    {
        public MemoryStream UnderlyingStream {
            get
            {
                return this;
            }
        }
    }
#else
    internal sealed class MemoryOutputStream : IOutputStream
    {
        private IOutputStream outputStream;

        public MemoryStream UnderlyingStream { get; private set; }

        public MemoryOutputStream()
        {
            this.UnderlyingStream = new MemoryStream();
            this.outputStream = this.UnderlyingStream.AsOutputStream();
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            return this.outputStream.FlushAsync();
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            return this.outputStream.WriteAsync(buffer);
        }

        public void Dispose()
        {
            if (this.outputStream != null)
            {
                this.outputStream.Dispose();
                this.outputStream = null;
            }

            if (this.UnderlyingStream != null)
            {
                this.UnderlyingStream.Dispose();
                this.UnderlyingStream = null;
            }
        }
    }
#endif
}
