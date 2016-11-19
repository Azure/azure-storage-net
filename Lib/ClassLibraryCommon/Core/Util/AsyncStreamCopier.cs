//-----------------------------------------------------------------------
// <copyright file="AsyncStreamCopier.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    // Class to copy streams with potentially overlapping read / writes. This uses no waithandle, extra threads, but does contain a single lock
    internal class AsyncStreamCopier<T> : IDisposable
    {
        #region Ctors + Locals
        private StreamDescriptor streamCopyState = null;

        private int buffSize;

        private Stream src = null;
        private Stream dest = null;

        // Used for cooperative cancellation
        CancellationTokenSource cancellationTokenSourceAbort;

        // Used for timeouts
        CancellationTokenSource cancellationTokenSourceTimeout;

        CancellationTokenSource cancellationTokenSourceCombined;

        private ExecutionState<T> state = null;
        private Action previousCancellationDelegate = null;

        private bool disposed = false;

        /// <summary>
        /// Creates and initializes a new asynchronous copy operation.
        /// </summary>
        /// <param name="src">The source stream.</param>
        /// <param name="dest">The destination stream.</param>
        /// <param name="state">An ExecutionState used to coordinate copy operation.</param>
        /// <param name="buffSize">Size of read and write buffers used to move data.</param>
        /// <param name="calculateMd5">Boolean value indicating whether the MD-5 should be calculated.</param>
        /// <param name="streamCopyState">An object that represents the state for the current operation.</param>
        public AsyncStreamCopier(Stream src, Stream dest, ExecutionState<T> state, int buffSize, bool calculateMd5, StreamDescriptor streamCopyState)
        {
            this.src = src;
            this.dest = dest;
            this.state = state;
            this.buffSize = buffSize;
            this.streamCopyState = streamCopyState;

            if (streamCopyState != null && calculateMd5 && streamCopyState.Md5HashRef == null)
            {
                streamCopyState.Md5HashRef = new MD5Wrapper();
            }
        }
        #endregion

        #region Publics

        /// <summary>
        /// Begins a stream copy operation.
        /// 
        /// This method wraps the StartCopyStreamAsync method, presenting a different API for it.
        /// As we update the library to be more task-based, callers should gradually move to StartCopyStreamAsync.
        /// </summary>
        /// <param name="completedDelegate">Callback delegate</param>
        /// <param name="copyLength">Number of bytes to copy from source stream to destination stream. Cannot pass in both copyLength and maxLength.</param>
        /// <param name="maxLength">Maximum length of the source stream. Cannot pass in both copyLength and maxLength.</param>
        public void StartCopyStream(Action<ExecutionState<T>> completedDelegate, long? copyLength, long? maxLength)
        {
            Task streamCopyTask = this.StartCopyStreamAsync(copyLength, maxLength);
            streamCopyTask.ContinueWith(completedStreamCopyTask => 
            {
                this.state.CancelDelegate = this.previousCancellationDelegate;
                if (completedStreamCopyTask.IsFaulted)
                {
                    this.state.ExceptionRef = completedStreamCopyTask.Exception.InnerException;
                }
                else if (completedStreamCopyTask.IsCanceled)
                {
                    bool timedOut = !this.cancellationTokenSourceAbort.IsCancellationRequested;
                    if (!timedOut)
                    {
                        // Free up the OS timer as soon as possible.
                        this.cancellationTokenSourceTimeout.Dispose();
                    }
                    if (state != null)
                    {
                        if (state.Req != null)
                        {
                            try
                            {
                                state.ReqTimedOut = timedOut;

                                // Note: the old logic had the following line happen on different threads for Desktop and Phone.  I don't think 
                                // we still need to do that, but I'm not sure.
#if WINDOWS_DESKTOP || WINDOWS_PHONE
                                state.Req.Abort();
#endif
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(state.OperationContext, "Aborting the request failed with exception: {0}", ex);
                            }
                        }

                        this.state.ExceptionRef = timedOut ?
                            Exceptions.GenerateTimeoutException(state.Cmd != null ? state.Cmd.CurrentResult : null, null) :
                            Exceptions.GenerateCancellationException(state.Cmd != null ? state.Cmd.CurrentResult : null, null);
                    }
                }

                try
                {
                    completedDelegate(this.state);
                }
                catch (Exception ex)
                {
                    this.state.ExceptionRef = ex;
                }

                this.Dispose();
            });

            return;
        }

        /// <summary>
        /// Cleans up references. To end a copy operation, call Cancel() on the ExecutionState.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.cancellationTokenSourceAbort != null)
                    {
                        this.cancellationTokenSourceAbort.Dispose();
                    }

                    if (this.cancellationTokenSourceTimeout != null)
                    {
                        this.cancellationTokenSourceTimeout.Dispose();
                        this.cancellationTokenSourceCombined.Dispose();
                    }

                    this.state = null;
                }
            }
        }

        /// <summary>
        /// This method performs the stream copy in an asynchronous, task-based manner.
        /// 
        /// To do the stream copy in a begin-with-callback style (the old style), use StartCopyStream, which wraps this method,
        /// does appropriate cancellation/exception handling, and calls the callback.
        ///
        /// This method sets up cancellation, and will cancel if either the timeout on the execution state expires, or cancel() is called 
        /// directly (on the ExecutionState).
        /// 
        /// This method does not set the ExceptionRef on the ExecutionState, or abort the request.
        /// </summary>
        /// <param name="copyLength">Number of bytes to copy from source stream to destination stream. Cannot pass in both copyLength and maxLength.</param>
        /// <param name="maxLength">Maximum length of the source stream. Cannot pass in both copyLength and maxLength.</param>
        /// <returns>A Task representing the asynchronous stream copy.</returns>
        public async Task StartCopyStreamAsync(long? copyLength, long? maxLength)
        {
            this.cancellationTokenSourceAbort = new CancellationTokenSource();

            // Set up the cancellation tokens and the timeout
            lock (this.state.CancellationLockerObject)
            {
                this.previousCancellationDelegate = this.state.CancelDelegate;
                this.state.CancelDelegate = this.cancellationTokenSourceAbort.Cancel;
            }

            if (this.state.OperationExpiryTime.HasValue)
            {
                this.cancellationTokenSourceTimeout = new CancellationTokenSource(this.state.RemainingTimeout);
                this.cancellationTokenSourceCombined = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSourceAbort.Token, this.cancellationTokenSourceTimeout.Token);
            }
            else
            {
                this.cancellationTokenSourceCombined = this.cancellationTokenSourceAbort;
            }

            await this.StartCopyStreamAsyncHelper(copyLength, maxLength, this.cancellationTokenSourceCombined.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// This method does the actual internal logic of copying one stream to another.
        /// </summary>
        /// <param name="copyLength">Number of bytes to copy from source stream to destination stream. Cannot pass in both copyLength and maxLength.</param>
        /// <param name="maxLength">Maximum length of the source stream. Cannot pass in both copyLength and maxLength.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A Task representing the asynchronous stream copy.</returns>
        private async Task StartCopyStreamAsyncHelper(long? copyLength, long? maxLength, CancellationToken token)
        {
            // Validate arguments
            if (copyLength.HasValue && maxLength.HasValue)
            {
                throw new ArgumentException(SR.StreamLengthMismatch);
            }

            if (this.src.CanSeek && maxLength.HasValue && (this.src.Length - this.src.Position > maxLength))
            {
                throw new InvalidOperationException(SR.StreamLengthError);
            }

            if (this.src.CanSeek && copyLength.HasValue && (this.src.Length - this.src.Position < copyLength))
            {
                throw new ArgumentOutOfRangeException("copyLength", SR.StreamLengthShortError);
            }

            token.ThrowIfCancellationRequested();

            byte[] readBuff = new byte[this.buffSize];
            byte[] writeBuff = new byte[this.buffSize];
            byte[] swap;

            int bytesToCopy = CalculateBytesToCopy(copyLength, 0);
            int bytesCopied = await this.src.ReadAsync(readBuff, 0, bytesToCopy, token).ConfigureAwait(false);

            long totalBytes = bytesCopied;
            CheckMaxLength(maxLength, totalBytes);

            swap = readBuff;
            readBuff = writeBuff;
            writeBuff = swap;

            while (bytesCopied > 0)
            {
                token.ThrowIfCancellationRequested();

                Task writeTask = this.dest.WriteAsync(writeBuff, 0, bytesCopied, token);

                bytesToCopy = CalculateBytesToCopy(copyLength, totalBytes);
                Task<int> readTask;
                if (bytesToCopy > 0)
                {
                    readTask = this.src.ReadAsync(readBuff, 0, bytesToCopy, token);
                }
                else
                {
                    readTask = Task.FromResult<int>(0);
                }

                await writeTask.ConfigureAwait(false);

                UpdateStreamCopyState(writeBuff, bytesCopied);

                bytesCopied = await readTask.ConfigureAwait(false);
                totalBytes = totalBytes + bytesCopied;

                CheckMaxLength(maxLength, totalBytes);

                swap = readBuff;
                readBuff = writeBuff;
                writeBuff = swap;
            }

            if (copyLength.HasValue && totalBytes != copyLength.Value)
            {
                throw new ArgumentOutOfRangeException("copyLength", SR.StreamLengthShortError);
            }

            FinalizeStreamCopyState();
        }
#endregion

#region Privates
        private void FinalizeStreamCopyState()
        {
            if (this.streamCopyState != null && this.streamCopyState.Md5HashRef != null)
            {
                try
                {
                    this.streamCopyState.Md5 = this.streamCopyState.Md5HashRef.ComputeHash();
                }
                catch (Exception)
                {
                    // no op
                }
                finally
                {
                    this.streamCopyState.Md5HashRef = null;
                }
            }
        }

        private static void CheckMaxLength(long? maxLength, long totalBytes)
        {
            if (maxLength.HasValue && totalBytes > maxLength.Value)
            {
                throw new InvalidOperationException(SR.StreamLengthError);
            }
        }

        private void UpdateStreamCopyState(byte[] writeBuff, int bytesCopied)
        {
            if (this.streamCopyState != null)
            {
                this.streamCopyState.Length += bytesCopied;
                if (this.streamCopyState.Md5HashRef != null)
                {
                    this.streamCopyState.Md5HashRef.UpdateHash(writeBuff, 0, bytesCopied);
                }
            }
        }

        private int CalculateBytesToCopy(long? copyLength, long totalBytes)
        {
            int bytesToCopy = this.buffSize;
            if (copyLength.HasValue)
            {
                if (totalBytes > copyLength.Value)
                {
                    throw new InvalidOperationException(String.Format(SR.NegativeBytesRequestedInCopy, copyLength.Value, totalBytes, this.streamCopyState.Length));
                }

                bytesToCopy = (int)Math.Min(bytesToCopy, copyLength.Value - totalBytes);
            }

            return bytesToCopy;
        }
#endregion
    }
}
