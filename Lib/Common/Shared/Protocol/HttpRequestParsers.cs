// -----------------------------------------------------------------------------------------
// <copyright file="HttprequestParsers.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    using System.Net.Http;
    using System.Linq;
    using System;
    using System.Collections.Generic;

    internal static partial class HttpRequestParsers
    {
        internal static string GetAuthorization(HttpRequestMessage request)
        {
            return request.Headers.Authorization != null ? request.Headers.Authorization.ToString() : null;
        }

        internal static string GetDate(HttpRequestMessage request)
        {
            return request.Headers.Date != null ? request.Headers.Date.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentType(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentType != null ? request.Content.Headers.ContentType.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentRange(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentRange != null ? request.Content.Headers.ContentRange.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentMD5(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentMD5 != null ? Convert.ToBase64String(request.Content.Headers.ContentMD5) : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentLocation(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentLocation != null ? request.Content.Headers.ContentLocation.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentLength(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentLength != null ? request.Content.Headers.ContentLength.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentLanguage(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentLanguage != null ? request.Content.Headers.ContentLanguage.ToString() : null;
        }


        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentEncoding(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentEncoding != null ? request.Content.Headers.ContentEncoding.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a request.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentDisposition(HttpRequestMessage request)
        {
            return request.Content.Headers.ContentDisposition != null ? request.Content.Headers.ContentDisposition.ToString() : null;
        }

        internal static string GetIfMatch(HttpRequestMessage request)
        {
            return request.Headers.IfMatch != null ? request.Headers.IfMatch.ToString() : null;
        }

        internal static string GetIfNoneMatch(HttpRequestMessage request)
        {
            return request.Headers.IfNoneMatch != null ? request.Headers.IfNoneMatch.ToString() : null;
        }
        internal static string GetIfModifiedSince(HttpRequestMessage request)
        {
            return request.Headers.IfModifiedSince != null ? request.Headers.IfModifiedSince.ToString() : null;
        }

        internal static string GetIfUnModifiedSince(HttpRequestMessage request)
        {
            return request.Headers.IfUnmodifiedSince != null ? request.Headers.IfUnmodifiedSince.ToString() : null;
        }

        internal static string GetCacheControl(HttpRequestMessage request)
        {
            return request.Headers.CacheControl != null ? request.Headers.CacheControl.ToString() : null;
        }

        internal static List<string> GetAllHeaders(HttpRequestMessage request)
        {
            if (request.Content != null)
            {
                return request.Headers.Concat(request.Content.Headers).Select(r => r.Key).ToList();
            }

            return request.Headers.Select(r => r.Key).ToList();
        }

        internal static string GetHeader(HttpRequestMessage request, string headerName)
        {
            if (request.Content != null && request.Content.Headers.Contains(headerName))
            {
                return request.Content.Headers.GetValues(headerName).First();
            }
            else if (request.Headers.Contains(headerName))
            {
                return request.Headers.GetValues(headerName).First();
            }
            else
            {
                return null;
            }
        }
    }
}
