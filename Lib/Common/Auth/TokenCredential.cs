//-----------------------------------------------------------------------
// <copyright file="TokenCredential.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Auth
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This is the return type of <see cref="RenewTokenFuncAsync"/>.
    /// A new token and a new frequency is expected.
    /// </summary>
    public struct NewTokenAndFrequency
    {
        /// <summary>
        /// Create a new instance of <see cref="NewTokenAndFrequency"/>.
        /// </summary>
        /// <param name="newToken">The new token credential.</param>
        /// <param name="newFrequency">The new frequency to wait before calling <see cref="RenewTokenFuncAsync"/> again.</param>
        public NewTokenAndFrequency(String newToken, TimeSpan? newFrequency = null) : this()
        {
            Token = newToken;
            Frequency = newFrequency;
        }

        /// <summary>
        /// The new token credential. 
        /// </summary>
        public String Token { get; set; }

        /// <summary>
        /// The new frequency to wait before calling <see cref="RenewTokenFuncAsync"/> again.
        /// </summary>
        public TimeSpan? Frequency { get; set; }
    }

    /// <summary>
    /// This type of delegate is used to update the token credential periodically.
    /// </summary>
    /// <param name="state">A state object, which can be of any type.</param>
    /// <param name="cancellationToken">A cancellation token to receive the cancellation signal.</param>
    /// <returns></returns>
    public delegate Task<NewTokenAndFrequency> RenewTokenFuncAsync(Object state, CancellationToken cancellationToken);

    /// <summary>
    /// Represents a token that is used to authorize HTTPS requests.
    /// </summary>
    public sealed class TokenCredential: IDisposable
    {
        private volatile string token; // TODO why does this need to be volatile?  why can't the property be an autoprop?
        private readonly Timer timer = null;
        private readonly RenewTokenFuncAsync renewTokenFuncAsync;
        private readonly CancellationTokenSource cancellationTokenSource;
        private TimeSpan renewFrequency;

        /// <summary>
        /// The authorization token. It can be set by the user at any point in a thread-safe way.
        /// </summary>
        public string Token
        {
            get
            {
                return this.token;  
            }
            set // TODO private?
            {
                this.token = value;
            }
        }

        /// <summary>
        /// Create an instance of <see cref="TokenCredential"/>.
        /// </summary>
        /// <param name="initialToken">Initial value of the token credential.</param>
        public TokenCredential(String initialToken) : this(initialToken, null, null, default(TimeSpan)) { }

        /// <summary>
        /// Create an instance of <see cref="TokenCredential"/>.
        /// </summary>
        /// <param name="initialToken">Initial value of the token credential.</param>
        /// <param name="periodicTokenRenewer">If given, this delegate is called periodically to renew the token credential.</param>
        /// <param name="state">A state object is passed to the periodicTokenRenewer every time it is called.</param>
        /// <param name="renewFrequency">If periodicTokenRenewer is given, user should define a frequency to call the periodicTokenRenewer.</param>
        public TokenCredential(String initialToken,
            RenewTokenFuncAsync periodicTokenRenewer,
            Object state,
            TimeSpan renewFrequency)
        {
            this.token = initialToken;

            // if no renewer is given, then the token will not be updated automatically.
            if (periodicTokenRenewer == null) return;

            this.renewTokenFuncAsync = periodicTokenRenewer;
            this.renewFrequency = renewFrequency;

            // when "new Timer(...)" is called, it might call RenewTokenAsync before even being assigned to timer, if renewFrequency is very close to 0.
            // since RenewTokenAsync refers to timer, we need to make sure that before it is invoked, timer is defined.
            this.timer = new Timer(RenewTokenAsync, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            this.timer.Change(this.renewFrequency, Timeout.InfiniteTimeSpan);
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Calling Dispose stops the timer and periodicTokenRenewer.
        /// </summary>
        public void Dispose() { this.timer?.Dispose(); this.cancellationTokenSource?.Cancel(); } // TODO should probably track that we've already been disposed, and no-op

        /// <summary>
        /// This method is triggered by the timer. 
        /// It calls the renew function provided by the user, updates the token, and then restarts the timer.
        /// </summary>
        /// <param name="state"></param>
        private async void RenewTokenAsync(Object state)
        {
            try
            {
                var newTokenAndFrequency = await this.renewTokenFuncAsync(state, this.cancellationTokenSource.Token).ConfigureAwait(false);
                this.token = newTokenAndFrequency.Token;
                this.renewFrequency = newTokenAndFrequency.Frequency ?? this.renewFrequency;
                    // if nothing is given, use previous frequency.
            }
            // only catch the exception when it was caused by this credential object's cancellation token.
            catch (OperationCanceledException ex)
            {
                if (!ex.CancellationToken.Equals(this.cancellationTokenSource.Token))
                {
                    throw ex; // TODO should probably use throw, not throw ex, otherwise we lose call stack.
                }
            }
            finally
            {
                // the timer is restarted.
                if (!this.cancellationTokenSource.IsCancellationRequested)
                    this.timer.Change(this.renewFrequency, Timeout.InfiniteTimeSpan);
            }
        }
    }
}