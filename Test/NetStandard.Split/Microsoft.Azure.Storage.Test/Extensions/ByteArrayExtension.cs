﻿// -----------------------------------------------------------------------------------------
// <copyright file="ByteArrayExtension.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Test.Extensions
{
    /// <summary>
    /// byte array Extension to share code with WinRT calls
    /// </summary>
    internal static class ByteArrayExtension
    {
        /// <summary>
        /// return the byte array itself
        /// </summary>
        /// <param name="buffer">input buffer</param>
        /// <returns>The input buffer.</returns>
        public static byte[] AsBuffer(this byte[] buffer)
        {
            return buffer;
        }
    }
}
