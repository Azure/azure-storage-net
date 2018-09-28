 // <copyright file="HttpResponseParsers.cs" company="Microsoft">
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
 
 namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
 {
     using System.Net.Http;
     using System.Linq;
     using Microsoft.WindowsAzure.Storage.Core.Util;
     using System;
 
     internal static partial class HttpRequestHandler
     {
         internal static void SetExpect100Continue(HttpRequestMessage request, bool value)
         {
             request.Headers.ExpectContinue = value;
         }
 
         internal static void SetKeepAlive(HttpRequestMessage request, bool alive)
         {
             if (alive)
             {
                 request.Headers.Connection.Add("Keep-Alive");
             }
             else
             {
                 request.Headers.Connection.Remove("Keep-Alive");
             }
         }
 
         internal static void SetAccept(HttpRequestMessage request, string accept)
         {
             request.Headers.Accept.ParseAdd(accept);
         }
 
         internal static void SetContentLength(HttpRequestMessage request, long contentLength)
         {
            request.Content.Headers.ContentLength = contentLength;
        }
 
         internal static void SetContentType(HttpRequestMessage request, string contentType)
         {
             SetContentHeader(request, Constants.ContentTypeElement, contentType);
         }
 
         internal static void SetCacheControl(HttpRequestMessage request, string cacheControl)
         {
             SetContentHeader(request, Constants.CacheControlElement, cacheControl);
         }
 
         internal static void SetContentMd5(HttpRequestMessage request, string cacheControl)
         {
             SetContentHeader(request, Constants.ContentMD5Element, cacheControl);
         }
 
         internal static void SetContentLanguage(HttpRequestMessage request, string cacheControl)
         {
             SetContentHeader(request, Constants.ContentLanguageElement, cacheControl);
         }
 
         internal static void SetContentEncoding(HttpRequestMessage request, string cacheControl)
         {
             SetContentHeader(request, Constants.ContentEncodingElement, cacheControl);
         }
 
         internal static void SetIfMatch(HttpRequestMessage request, string ifMatchETag)
         {
             request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(ifMatchETag));
         }
 
         internal static void SetIfNoneMatch(HttpRequestMessage request, string ifNoneMatch)
         {
             request.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(ifNoneMatch));
         }
         internal static void SetIfModifiedSince(HttpRequestMessage request, DateTimeOffset ifModifiedSince)
         {
             request.Headers.IfModifiedSince = ifModifiedSince;
         }
 
         internal static void SetIfUnModifiedSince(HttpRequestMessage request, DateTimeOffset ifUnModifiedSince)
         {
             request.Headers.IfUnmodifiedSince = ifUnModifiedSince;
         }
 
         internal static void SetUserAgent(HttpRequestMessage request, string userAgent)
         {
             request.Headers.UserAgent.ParseAdd(userAgent);
         }

        internal static void SetContentHeader(HttpRequestMessage request, string headerKey, string headerValue)
        {
            request.Content.Headers.TryAddWithoutValidation(headerKey, headerValue);
        }

        internal static void SetHeader(HttpRequestMessage request, string headerKey, string headerValue)
        {
            request.Headers.TryAddWithoutValidation(headerKey, headerValue);
        }
    }
 }