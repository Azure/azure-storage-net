﻿// -----------------------------------------------------------------------------------------
// <copyright file="IBufferManager.cs" company="Microsoft">
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

using System;

namespace Microsoft.WindowsAzure.Storage
{
#if WINDOWS_RT
    using System.Runtime.InteropServices.WindowsRuntime;
#endif

    /// <summary>
    /// An interface that allows clients to provide a buffer manager to a given service client. This interface is patterned after
    /// the <see href="http://msdn.microsoft.com/en-us/library/system.servicemodel.channels.buffermanager.aspx">System.ServiceModel.Channels.BufferManager</see> class.
    /// </summary>
    public interface IBufferManager
    {
        /// <summary>
        /// Returns a buffer to the pool.
        /// </summary>
        /// <param name="buffer">A byte array segment specifying the buffer to return to the pool.</param>
        /// <exception cref="System.ArgumentNullException">Buffer reference cannot be null.</exception>
        /// <exception cref="System.ArgumentException">Length of buffer does not match the pool's buffer length property.</exception>
#if WINDOWS_RT
        void ReturnBuffer(ArraySegment<byte> buffer);
#else
        void ReturnBuffer(ArraySegment<byte> buffer);
#endif

        /// <summary>
        /// Gets a buffer of the specified size or larger from the pool.
        /// </summary>
        /// <param name="bufferSize">The size, in bytes, of the requested buffer.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">The value specified for <paramref name="bufferSize"/> cannot be less than zero.</exception>
        /// <returns>A byte array segment that is the requested size of the buffer.</returns>
        ArraySegment<byte> TakeBuffer(int bufferSize);

        /// <summary>
        /// Gets the size, in bytes, of the buffers managed by the given pool. Note that the buffer manager must return buffers of the exact size requested by the client.
        /// </summary>
        /// <returns>The size, in bytes, of the buffers managed by the given pool.</returns>
        int GetDefaultBufferSize();
    }
}
