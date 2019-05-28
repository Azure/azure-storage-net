//-----------------------------------------------------------------------
// <copyright file="StreamExtensions.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides stream helper methods that allow us to copy streams and measure the stream size.
    /// </summary>
    internal static class StreamExtensions
    {
        /// <summary>
        /// Return input stream itself.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns>the input stream</returns>
        internal static Stream AsInputStream(this Stream stream)
        {
            return stream;
        }

        [DebuggerNonUserCode]
        internal static int GetBufferSize(Stream inStream)
        {
            if (inStream.CanSeek && inStream.Length - inStream.Position > 0)
            {
                return (int)Math.Min(inStream.Length - inStream.Position, (long)Constants.DefaultBufferSize);
            }
            else
            {
                return Constants.DefaultBufferSize;
            }
        }

        /// <summary>
        /// Position the stream with offset from beginning.
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <param name="offset">offset from beginning</param>
        /// <returns>stream position</returns>
        public static long Seek(this Stream stream, long offset)
        {
            return stream.Seek(offset, SeekOrigin.Begin);
        }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// Reads synchronously the specified content of the stream and writes it to the given output stream.
        /// </summary>
        /// <param name="stream">The origin stream.</param>
        /// <param name="toStream">The destination stream.</param>    
        /// <param name="copyLength">Number of bytes to copy from source stream to destination stream. Cannot be passed with a value for maxLength.</param>
        /// <param name="maxLength">Maximum length of the stream to write.</param>        
        /// <param name="calculateChecksum">A value indicating whether the checksums should be calculated.</param>
        /// <param name="syncRead">A boolean indicating whether the write happens synchronously.</param>
        /// <param name="executionState">An object that stores state of the operation.</param>
        /// <param name="streamCopyState">State of the stream copy.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">stream</exception>
        [DebuggerNonUserCode]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Reviewed")]
        internal static void WriteToSync<T>(this Stream stream, Stream toStream, long? copyLength, long? maxLength, ChecksumRequested calculateChecksum, bool syncRead, ExecutionState<T> executionState, StreamDescriptor streamCopyState)
        {
            if (copyLength.HasValue && maxLength.HasValue)
            {
                throw new ArgumentException(SR.StreamLengthMismatch);
            }

            if (stream.CanSeek && maxLength.HasValue && stream.Length - stream.Position > maxLength)
            {
                throw new InvalidOperationException(SR.StreamLengthError);
            }

            if (stream.CanSeek && copyLength.HasValue && stream.Length - stream.Position < copyLength)
            {
                throw new ArgumentOutOfRangeException("copyLength", SR.StreamLengthShortError);
            }
            
            byte[] buffer = new byte[GetBufferSize(stream)];

            if (streamCopyState != null && calculateChecksum.HasAny && streamCopyState.ChecksumWrapper == null)
            {
                streamCopyState.ChecksumWrapper = new ChecksumWrapper(calculateChecksum.MD5, calculateChecksum.CRC64);
            }

            RegisteredWaitHandle waitHandle = null;
            ManualResetEvent completedEvent = null;
            if (!syncRead && executionState.OperationExpiryTime.HasValue)
            {
                completedEvent = new ManualResetEvent(false);
                waitHandle = ThreadPool.RegisterWaitForSingleObject(
                    completedEvent,
                    StreamExtensions.MaximumCopyTimeCallback<T>,
                    executionState,
                    executionState.RemainingTimeout,
                    true);
            }
            
            try
            {
                long? bytesRemaining = copyLength;
                int readCount;
                do
                {
                    if (executionState.OperationExpiryTime.HasValue && DateTime.Now.CompareTo(executionState.OperationExpiryTime.Value) > 0)
                    {
                        throw Exceptions.GenerateTimeoutException(executionState.Cmd != null ? executionState.Cmd.CurrentResult : null, null);
                    }

                    // Determine how many bytes to read this time so that no more than copyLength bytes are read
                    int bytesToRead = MinBytesToRead(bytesRemaining, buffer.Length);

                    if (bytesToRead == 0)
                    {
                        break;
                    }

                    // Read synchronously or asynchronously
                    readCount = syncRead
                                    ? stream.Read(buffer, 0, bytesToRead)
                                    : stream.EndRead(stream.BeginRead(buffer, 0, bytesToRead, null /* Callback */, null /* State */));

                    // Decrement bytes to write from bytes read
                    if (bytesRemaining.HasValue)
                    {
                        bytesRemaining -= readCount;
                    }

                    // Write
                    if (readCount > 0)
                    {
                        toStream.Write(buffer, 0, readCount);

                        // Update the StreamDescriptor after the bytes are successfully committed to the output stream
                        if (streamCopyState != null)
                        {
                            streamCopyState.Length += readCount;

                            if (maxLength.HasValue && streamCopyState.Length > maxLength.Value)
                            {
                                throw new InvalidOperationException(SR.StreamLengthError);
                            }

                            if (streamCopyState.ChecksumWrapper != null)
                            {
                                streamCopyState.ChecksumWrapper.UpdateHash(buffer, 0, readCount);
                            }
                        }
                    }
                }
                while (readCount != 0);

                if (bytesRemaining.HasValue && bytesRemaining != 0)
                {
                    throw new ArgumentOutOfRangeException("copyLength", SR.StreamLengthShortError);
                }
            }
            catch (Exception)
            {
                if (executionState.OperationExpiryTime.HasValue && DateTime.Now.CompareTo(executionState.OperationExpiryTime.Value) > 0)
                {
                    throw Exceptions.GenerateTimeoutException(executionState.Cmd != null ? executionState.Cmd.CurrentResult : null, null);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (waitHandle != null)
                {
                    waitHandle.Unregister(null);
                }

                if (completedEvent != null)
                {
                    completedEvent.Close();
                }
            }

            if (streamCopyState != null && streamCopyState.ChecksumWrapper != null)
            {
                if (streamCopyState.ChecksumWrapper.CRC64 != null)
                {
                    streamCopyState.Crc64 = streamCopyState.ChecksumWrapper.CRC64.ComputeHash();
                }
                if (streamCopyState.ChecksumWrapper.MD5 != null)
                {
                    streamCopyState.Md5 = streamCopyState.ChecksumWrapper.MD5.ComputeHash();
                }
                streamCopyState.ChecksumWrapper = null;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If aborting the request fails, we can still continue executing it.")]
        private static void MaximumCopyTimeCallback<T>(object state, bool timedOut)
        {
            ExecutionState<T> executionState = (ExecutionState<T>)state;

            if (timedOut)
            {
                if (executionState.Req != null)
                {
                    try
                    {
                        executionState.ReqTimedOut = true;
                        executionState.CancellationTokenSource.Cancel();
                    }
                    catch (Exception)
                    {
                        // no op
                    }
                }
            }
        }

        private static int MinBytesToRead(long? val1, int val2)
        {
            if (val1.HasValue && val1 < val2)
            {
                return (int)val1;
            }

            return val2;
        }

        /// <summary>
        /// Associates a given stream to a given RequestResult such that the RequestResult byte counters are accurately updated as data is read or written.
        /// </summary>
        /// <param name="stream">A reference to the original stream</param>
        /// <param name="result">An object that represents the result of a physical request.</param>
        /// <returns></returns>
        [DebuggerNonUserCode]
        internal static Stream WrapWithByteCountingStream(this Stream stream, RequestResult result)
        {
            return new ByteCountingStream(stream, result);
        }
#endif

#if NETCORE || WINDOWS_RT
        /// <summary>
        /// Asynchronously reads the entire content of the stream and writes it to the given output stream.
        /// </summary>
        /// <param name="stream">The origin stream.</param>
        /// <param name="toStream">The destination stream.</param>
        /// <param name="bufferManager">IBufferManager instance to use. May be null.</param>
        /// <param name="copyLength">Number of bytes to copy from source stream to destination stream. Cannot be passed with a value for maxLength.</param>
        /// <param name="maxLength">Maximum length of the source stream. Cannot be passed with a value for copyLength.</param>
        /// <param name="calculateChecksum">A value indicating whether the checksums should be calculated.</param>
        /// <param name="executionState">An object that stores state of the operation.</param>
        /// <param name="streamCopyState">An object that represents the current state for the copy operation.</param>
        /// <param name="token">A CancellationToken to observe while waiting for the copy to complete.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        internal static async Task WriteToAsync<T>(this Stream stream, Stream toStream, IBufferManager bufferManager, long? copyLength, long? maxLength, ChecksumRequested calculateChecksum, ExecutionState<T> executionState, StreamDescriptor streamCopyState, CancellationToken token)
        {
            if (copyLength.HasValue && maxLength.HasValue)
            {
                throw new ArgumentException(SR.StreamLengthMismatch);
            }

            if (stream.CanSeek && maxLength.HasValue && stream.Length - stream.Position > maxLength)
            {
                throw new InvalidOperationException(SR.StreamLengthError);
            }

            if (stream.CanSeek && copyLength.HasValue && stream.Length - stream.Position < copyLength)
            {
                throw new ArgumentOutOfRangeException("copyLength", SR.StreamLengthShortError);
            }

            if (streamCopyState != null && calculateChecksum.HasAny && streamCopyState.ChecksumWrapper == null)
            {
                streamCopyState.ChecksumWrapper = new ChecksumWrapper(calculateChecksum.MD5, calculateChecksum.CRC64);
            }

            CancellationTokenSource cts = null;

            byte[] buffer = bufferManager != null ? bufferManager.TakeBuffer(GetBufferSize(stream)) : new byte[GetBufferSize(stream)];

            try
            {
                if (executionState.OperationExpiryTime.HasValue)
                {
                    // Setup token for timeout
                    cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    cts.CancelAfter(executionState.RemainingTimeout);

                    // Switch tokens
                    token = cts.Token;
                }

                long? bytesRemaining = copyLength;
                int readCount;
                do
                {
                    // Determine how many bytes to read this time so that no more than count bytes are read
                    int bytesToRead = bytesRemaining.HasValue && bytesRemaining < buffer.Length ? (int)bytesRemaining : buffer.Length;

                    if (bytesToRead == 0)
                    {
                        break;
                    }

                    readCount = await stream.ReadAsync(buffer, 0, bytesToRead, token).ConfigureAwait(false);

                    if (bytesRemaining.HasValue)
                    {
                        bytesRemaining -= readCount;
                    }

                    if (readCount > 0)
                    {
                        await toStream.WriteAsync(buffer, 0, readCount, token).ConfigureAwait(false);

                        // Update the StreamDescriptor after the bytes are successfully committed to the output stream
                        if (streamCopyState != null)
                        {
                            streamCopyState.Length += readCount;

                            if (maxLength.HasValue && streamCopyState.Length > maxLength.Value)
                            {
                                throw new InvalidOperationException(SR.StreamLengthError);
                            }

                            if (streamCopyState.ChecksumWrapper != null)
                            {
                                streamCopyState.ChecksumWrapper.UpdateHash(buffer, 0, readCount);
                            }
                        }
                    }
                }
                while (readCount > 0);

                if (bytesRemaining.HasValue && bytesRemaining != 0)
                {
                    throw new ArgumentOutOfRangeException("copyLength", SR.StreamLengthShortError);
                }
            }
            finally
            {
                if (cts != null)
                {
                    cts.Dispose();
                    cts = null;
                }

                if (buffer != null && bufferManager != null)
                {
                    bufferManager.ReturnBuffer(buffer);
                }
            }

            // Streams opened with AsStreamForWrite extension need to be flushed
            // to write all buffered data to the underlying Windows Runtime stream.
            await toStream.FlushAsync().ConfigureAwait(false);

            if (streamCopyState != null && streamCopyState.ChecksumWrapper != null)
            {
                if (streamCopyState.ChecksumWrapper.CRC64 != null)
                {
                    streamCopyState.Crc64 = streamCopyState.ChecksumWrapper.CRC64.ComputeHash();
                }
                if (streamCopyState.ChecksumWrapper.MD5 != null)
                {
                    streamCopyState.Md5 = streamCopyState.ChecksumWrapper.MD5.ComputeHash();
                }
                streamCopyState.ChecksumWrapper = null;
            }
        }
#endif
#if TASK

        /// <summary>
        /// Asynchronously reads the entire content of the stream and writes it to the given output stream.
        /// </summary>
        /// <typeparam name="T">The result type of the ExecutionState</typeparam>
        /// <param name="stream">The origin stream.</param>
        /// <param name="toStream">The destination stream.</param>
        /// <param name="bufferManager">IBufferManager instance to use.</param>
        /// <param name="copyLength">Number of bytes to copy from source stream to destination stream. Cannot be passed with a value for maxLength.</param>
        /// <param name="maxLength">Maximum length of the source stream. Cannot be passed with a value for copyLength.</param>
        /// <param name="calculateChecksum">A value indicating whether the checksums should be calculated.</param>
        /// <param name="executionState">An object that stores state of the operation.</param>
        /// <param name="streamCopyState">State of the stream copy.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <param name="completed">The action taken when the execution is completed.</param>
        [DebuggerNonUserCode]
        internal static Task WriteToAsync<T>(this Stream stream, Stream toStream, IBufferManager bufferManager, long? copyLength, long? maxLength, ChecksumRequested calculateChecksum, ExecutionState<T> executionState, StreamDescriptor streamCopyState, CancellationToken cancellationToken, Action<ExecutionState<T>> completed = null)
        {
            AsyncStreamCopier<T> copier = new AsyncStreamCopier<T>(stream, toStream, executionState, bufferManager, GetBufferSize(stream), calculateChecksum, streamCopyState);
            return copier.StartCopyStream(completed, copyLength, maxLength, cancellationToken);
        }
#endif

    }
}
