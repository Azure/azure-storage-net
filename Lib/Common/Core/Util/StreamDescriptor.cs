//-----------------------------------------------------------------------
// <copyright file="StreamDescriptor.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Core.Util
{
    using System.Threading;

    /// <summary>
    /// Provides properties to keep track of checksum hash / Length of a stream as it is being copied.
    /// </summary>
    internal class StreamDescriptor
    {
        private long length = 0;

        public long Length
        {
            get { return Interlocked.Read(ref this.length); }
            set { Interlocked.Exchange(ref this.length, value); }
        }

        private volatile string md5 = null;

        public string Md5
        {
            get { return this.md5; }
            set { this.md5 = value; }
        }

        private volatile string crc64 = null;

        public string Crc64
        {
            get { return this.crc64; }
            set { this.crc64 = value; }
        }

        private volatile ChecksumWrapper checksumWrapper = null;

        public ChecksumWrapper ChecksumWrapper
        {
            get { return this.checksumWrapper; }
            set { this.checksumWrapper = value; }
        }
    }
}