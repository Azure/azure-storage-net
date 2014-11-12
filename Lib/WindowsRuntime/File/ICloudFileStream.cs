//-----------------------------------------------------------------------
// <copyright file="ICloudFileStream.cs" company="Microsoft">
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
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.File
{
#if ASPNET_K
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CloudFileStream : Stream
    {
        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying file, and commits the file.
        /// </summary>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        public abstract Task CommitAsync();

        internal Stream AsStreamForWrite()
        {
            return this;
        }
    }
#else
    using Windows.Foundation;
    using Windows.Storage.Streams;

    public interface ICloudFileStream : IRandomAccessStream
    {
        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying file, and commits the file.
        /// </summary>
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction CommitAsync();
    }
#endif
}
