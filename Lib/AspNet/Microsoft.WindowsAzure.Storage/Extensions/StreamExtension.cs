// -----------------------------------------------------------------------------------------
// <copyright file="StreamExtensinon.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Stream extension to share code with WinRT calls
    /// </summary>
    internal static class StreamExtensinon
    {
        /// <summary>
        /// Return input stream itself.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns>the input stream</returns>
        public static Stream AsInputStream(this Stream stream)
        {
            return stream;
        }

        /// <summary>
        /// Return input stream itself.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns>the input stream</returns>
        public static Stream AsOutputStream(this Stream stream)
        {
            return stream;
        }

        /// <summary>
        /// Return input stream itself.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns>the input stream</returns>
        public static Stream AsStreamForRead(this Stream stream)
        {
            return stream;
        }

        /// <summary>
        /// Return input stream itself.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns>the input stream</returns>
        public static Stream AsStreamForWrite(this Stream stream)
        {
            return stream;
        }

        /// <summary>
        /// Write buffer to the stream.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <param name="buffer">buffer to write to the stream</param>
        /// <returns>Async task</returns>
        public static async Task WriteAsync(this Stream stream, byte[] buffer)
        {
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Position the stream with offset from beginning.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <param name="offset">offset from beginning</param>
        /// <returns>stream position</returns>
        public static long Seek(this Stream stream, ulong offset)
        {
            return stream.Seek((long)offset, SeekOrigin.Begin);
        }
    }
}
