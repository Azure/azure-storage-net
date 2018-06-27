//-----------------------------------------------------------------------
// <copyright file="TableUtilities.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq.Expressions;
    using System.Net;
    using System.Text;

    internal static class TableUtilities
    {
        /// <summary>
        /// Look for an inner exception of type T. 
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The found exception or <c>null</c>.</returns>
        internal static T FindInnerExceptionOfType<T>(Exception exception) where T : Exception
        {
            T dsce = null;

            while (exception != null)
            {
                dsce = exception as T;

                if (dsce != null)
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return dsce;
        }

        /// <summary>
        /// Copies the headers and properties from a request into a different request.
        /// </summary>
        /// <param name="destinationRequest">The request to copy into.</param>
        /// <param name="sourceRequest">The request to copy from.</param>
        internal static void CopyRequestData(HttpWebRequest destinationRequest, HttpWebRequest sourceRequest)
        {
            // Copy the request properties
            destinationRequest.AllowAutoRedirect = sourceRequest.AllowAutoRedirect;
            destinationRequest.AllowWriteStreamBuffering = sourceRequest.AllowWriteStreamBuffering;
            destinationRequest.AuthenticationLevel = sourceRequest.AuthenticationLevel;
            destinationRequest.AutomaticDecompression = sourceRequest.AutomaticDecompression;
            destinationRequest.CachePolicy = sourceRequest.CachePolicy;
            destinationRequest.ClientCertificates = sourceRequest.ClientCertificates;
            destinationRequest.ConnectionGroupName = sourceRequest.ConnectionGroupName;
            destinationRequest.ContinueDelegate = sourceRequest.ContinueDelegate;
            destinationRequest.CookieContainer = sourceRequest.CookieContainer;
            destinationRequest.Credentials = sourceRequest.Credentials;
            destinationRequest.ImpersonationLevel = sourceRequest.ImpersonationLevel;
            destinationRequest.KeepAlive = sourceRequest.KeepAlive;
            destinationRequest.MaximumAutomaticRedirections = sourceRequest.MaximumAutomaticRedirections;
            destinationRequest.MaximumResponseHeadersLength = sourceRequest.MaximumResponseHeadersLength;
            destinationRequest.MediaType = sourceRequest.MediaType;
            destinationRequest.Method = sourceRequest.Method;
            destinationRequest.Pipelined = sourceRequest.Pipelined;
            destinationRequest.PreAuthenticate = sourceRequest.PreAuthenticate;
            destinationRequest.ProtocolVersion = sourceRequest.ProtocolVersion;
            destinationRequest.Proxy = sourceRequest.Proxy;
            destinationRequest.ReadWriteTimeout = sourceRequest.ReadWriteTimeout;
            destinationRequest.SendChunked = sourceRequest.SendChunked;
            destinationRequest.Timeout = sourceRequest.Timeout;
            destinationRequest.UnsafeAuthenticatedConnectionSharing = sourceRequest.UnsafeAuthenticatedConnectionSharing;
            destinationRequest.UseDefaultCredentials = sourceRequest.UseDefaultCredentials;

            // Copy the headers.
            // Some headers can't be copied over. We check for these headers.
            foreach (string headerName in sourceRequest.Headers)
            {
                switch (headerName)
                {
                    case "Accept":
                        destinationRequest.Accept = sourceRequest.Accept;
                        break;

                    case "Connection":
                        destinationRequest.Connection = sourceRequest.Connection;
                        break;

                    case "Content-Length":
                        destinationRequest.ContentLength = sourceRequest.ContentLength;
                        break;

                    case "Content-Type":
                        destinationRequest.ContentType = sourceRequest.ContentType;
                        break;

                    case "Expect":
                        destinationRequest.Expect = sourceRequest.Expect;
                        break;

                    case "If-Modified-Since":
                        destinationRequest.IfModifiedSince = sourceRequest.IfModifiedSince;
                        break;

                    case "Referer":
                        destinationRequest.Referer = sourceRequest.Referer;
                        break;

                    case "Transfer-Encoding":
                        destinationRequest.TransferEncoding = sourceRequest.TransferEncoding;
                        break;

                    case "User-Agent":
                        destinationRequest.UserAgent = sourceRequest.UserAgent;
                        break;

                    default:
                        destinationRequest.Headers.Add(headerName, sourceRequest.Headers[headerName]);
                        break;
                }
            }
        }
    }
#endif
}