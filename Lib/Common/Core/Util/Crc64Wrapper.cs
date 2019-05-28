//-----------------------------------------------------------------------
// <copyright file="Crc64Wrapper.cs" company="Microsoft">
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

    /// <summary>
    /// Wrapper class for CRC64.
    /// </summary>
    internal class Crc64Wrapper : IDisposable
    {
        ulong uCRC = 0;

        internal Crc64Wrapper()
        {

        }

        /// <summary>
        /// Calculates an on-going hash using the input byte array.
        /// </summary>
        /// <param name="input">The input array used for calculating the hash.</param>
        /// <param name="offset">The offset in the input buffer to calculate from.</param>
        /// <param name="count">The number of bytes to use from input.</param>
        internal void UpdateHash(byte[] input, int offset, int count)
        {
            if (offset != 0)
            {
                throw new NotImplementedException("non-zero offset for Crc64Wrapper update not supported");
            }
            if (count > 0)
            {
                this.uCRC = Crc64.ComputeSlicedSafe(input, count, this.uCRC);
            }
        }

        /// <summary>
        /// Retrieves the string representation of the hash. (Completes the creation of the hash).
        /// </summary>
        /// <returns>String representation of the computed hash value.</returns>
        internal string ComputeHash()
        {
            return Convert.ToBase64String(BitConverter.GetBytes(this.uCRC));
        }

        public void Dispose()
        {
        }
    }
}