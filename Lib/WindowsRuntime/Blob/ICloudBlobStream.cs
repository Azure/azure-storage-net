//-----------------------------------------------------------------------
// <copyright file="ICloudBlobStream.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using System.IO;
    using System.Threading.Tasks;
#if WINDOWS_RT
    using Windows.Storage.Streams;
#endif

    /// <summary>
    /// Represents a stream for writing to a blob.
    /// </summary>
    public abstract class CloudBlobStream : Stream
    {
        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying blob, and commits the blob.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        public abstract Task CommitAsync();

    }
}
