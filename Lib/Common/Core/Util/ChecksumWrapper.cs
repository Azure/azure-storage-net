//-----------------------------------------------------------------------
// <copyright file="ChecksumWrapper.cs" company="Microsoft">
//    Copyright 2018 Microsoft Corporation
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
    using System;

    internal class ChecksumWrapper : IDisposable
    {
        public Crc64Wrapper CRC64
        {
            get;
            set;
        }

        public MD5Wrapper MD5
        {
            get;
            set;
        }

        internal ChecksumWrapper(bool calcMd5 = true, bool calcCrc64 = true)
        {
            if (calcCrc64)
            {
                this.CRC64 = new Crc64Wrapper();
            }

            if (calcMd5)
            {
                this.MD5 = new MD5Wrapper();
            }
        }

        internal void UpdateHash(byte[] input, int offset, int count)
        {
            if (this.CRC64 != null)
            {
                this.CRC64.UpdateHash(input, offset, count);
            }

            if (this.MD5 != null)
            {
                this.MD5.UpdateHash(input, offset, count);
            }
        }

        public bool HasAny => this.MD5 != default(MD5Wrapper) || this.CRC64 != default(Crc64Wrapper);

        #region IDisposable

        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (CRC64 != null)
                {
                    CRC64.Dispose();
                }
                if (MD5 != null)
                {
                    MD5.Dispose();
                }
            }

            disposed = true;
        }
        #endregion
    }
}