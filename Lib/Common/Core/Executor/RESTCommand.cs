//-----------------------------------------------------------------------
// <copyright file="RESTCommand.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Executor
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

#if WINDOWS_RT || NETCORE
    using System.Net.Http;
    using System.Threading.Tasks;
#else
    using System.Net;
#endif

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
    internal class RESTCommand<T> : StorageCommandBase<T>
    {
        #region Ctors
        public RESTCommand(StorageCredentials credentials, StorageUri storageUri)
            : this(credentials, storageUri, null)
        {
        }

        public RESTCommand(StorageCredentials credentials, StorageUri storageUri, UriQueryBuilder builder)
        {
            this.Credentials = credentials;
            this.StorageUri = storageUri;
            this.Builder = builder;
        }
        #endregion

        // Location mode of the request
        public LocationMode LocationMode = LocationMode.PrimaryOnly;

        // Location mode of the command
        public CommandLocationMode CommandLocationMode = CommandLocationMode.PrimaryOnly;

        public StorageCredentials Credentials;

        public StorageUri StorageUri;

        // The UriQueryBuilder used to create the request
        public UriQueryBuilder Builder;

        // Reference to hold stream from webresponse
        private Stream responseStream;

        public Stream ResponseStream
        {
            get
            {
                return this.responseStream;
            }

            set
            {
                this.responseStream =
#if WINDOWS_RT || NETCORE
                    value;
#else
                    value == null ? null : value.WrapWithByteCountingStream(this.CurrentResult);
#endif
            }
        }

        // Stream to potentially copy response into
        public Stream DestinationStream = null;

        // Stream to potentially copy error response into
        public Stream ErrorStream = null;

        // if true, the inStream will be set before processresponse is called.
        public bool RetrieveResponseStream = false;

        // if true the executor will calculate the md5 on retrieved data
        public bool CalculateMd5ForResponseStream = false;

        public Stream StreamToDispose { get; set; }
        
#if WINDOWS_RT || NETCORE
        public Func<RESTCommand<T>, OperationContext, HttpContent> BuildContent;

        public Func<RESTCommand<T>, Uri, UriQueryBuilder, HttpContent, int?, OperationContext, StorageRequestMessage> BuildRequest;

        // Pre-Stream Retrival func (i.e. if 409 no stream is retrieved), in some cases this method will return directly
        public Func<RESTCommand<T>, HttpResponseMessage, Exception, OperationContext, T> PreProcessResponse;

        // Post-Stream Retrieval Func ( if retreiveStream is true after ProcessResponse, the stream is retrieved and then PostProcess is called
        public Func<RESTCommand<T>, HttpResponseMessage, OperationContext, Task<T>> PostProcessResponse;

        // Delegate that will be executed if there is anything to be disposed.
        public Action<RESTCommand<T>> DisposeAction = null;
#else
        // Stream to send to server
        private Stream sendStream = null;

        public Stream SendStream
        {
            get
            {
                return this.sendStream;
            }

            set
            {
                this.sendStream = value;
            }
        }

        // Length of data to send to server from stream.
        public long? SendStreamLength = null;

        // Func to construct the request
        public Func<Uri, UriQueryBuilder, int?, bool, OperationContext, HttpWebRequest> BuildRequestDelegate = null;

        // Delegate to Set custom headers
        public Action<HttpWebRequest, OperationContext> SetHeaders = null;

        // Delegate to Sign headers - note this is important that it doesnt have a type dependency on StorageCredentials here
        // due to build issues and WinRT restrictions.  
        public Action<HttpWebRequest, OperationContext> SignRequest = null;

        // Pre-Stream Retrival func (i.e. if 409 no stream is retrieved), in some cases this method will return directly
        public Func<RESTCommand<T>, HttpWebResponse, Exception, OperationContext, T> PreProcessResponse = null;

        // Post-Stream Retrieval Func ( if retreiveStream is true after ProcessResponse, the stream is retrieved and then PostProcess is called
        public Func<RESTCommand<T>, HttpWebResponse, OperationContext, T> PostProcessResponse = null;

        // Delegate that will be executed if there is anything to be disposed.
        public Action<RESTCommand<T>> DisposeAction = null;
#endif
    }
}
